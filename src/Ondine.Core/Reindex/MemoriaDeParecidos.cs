using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Ondine.Reindex;

/// <summary>
/// Lo que ya se calculó de «¿a qué episodio se parece este nombre?», para no calcularlo
/// dos veces.
///
/// <para>
/// <b>Por qué existe.</b> Identificar un fichero es compararlo con TODOS los episodios del
/// catálogo, y esa comparación es lo único caro de «Analizar»: en una serie de 1.800
/// episodios con 1.400 ficheros son 7,4 millones de comparaciones de texto. Cada vez que se
/// analiza se rehace entera aunque no haya cambiado nada.
/// </para>
/// <para>
/// <b>Qué guarda.</b> Por cada juego de títulos de un fichero, el parecido con cada episodio
/// del catálogo y cuántos trozos del nombre casan. NADA MÁS: ni el veredicto, ni las
/// alternativas, ni textos de interfaz. El veredicto se vuelve a calcular siempre con las
/// reglas de hoy, el número de temporada que dice el fichero y las decisiones que tomaste
/// ayer; lo único que se evita es la parte que no depende de nada de eso.
/// </para>
/// <para>
/// <b>Por qué no se queda vieja.</b> Es una función pura, y se identifica por todo lo que la
/// determina: <i>el texto de los títulos del fichero</i> (la clave de cada fila) y <i>los
/// títulos de cada episodio del catálogo, el algoritmo de parecido y el umbral de segmento</i>
/// (la <see cref="Huella"/> de todo el conjunto). Editar el catálogo, cambiar el algoritmo o
/// renombrar el fichero produce otra clave o otra huella, y lo guardado deja de valer solo:
/// no hay que acordarse de borrar nada ni de subir ningún número de versión. Una memoria con
/// otra huella se descarta entera al cargarla.
/// </para>
/// <para>
/// Es un objeto en memoria y no toca disco: eso lo hace <c>ReindexStore</c>, como con el
/// resto de lo que la app guarda —el motor no toca disco—.
/// </para>
/// </summary>
public sealed class MemoriaDeParecidos
{
    private const uint Magia = 0x4D50444F;   // «ODPM» en little endian
    private const int VersionDelFormato = 1;
    private const int BytesPorFila = 16;     // la clave

    /// <summary>Tope del fichero en disco. Pasado esto se sueltan primero las filas más viejas.</summary>
    public const long TopeEnBytes = 64L * 1024 * 1024;

    /// <summary>De qué catálogo y de qué algoritmo es lo que hay aquí dentro.</summary>
    public string Huella { get; }

    internal int Episodios { get; }

    private sealed class Fila
    {
        public required double[] Parecido;
        public required byte[] Casados;
        public bool UsadaAhora;
    }

    private readonly ConcurrentDictionary<Guid, Fila> _filas = new();

    private MemoriaDeParecidos(string huella, int episodios)
    {
        Huella = huella;
        Episodios = episodios;
    }

    /// <summary>Una memoria vacía para este catálogo.</summary>
    public static MemoriaDeParecidos Para(ReindexCatalog catalogo) =>
        new(IndiceTitulos.HuellaDe(catalogo), catalogo.Episodios.Count);

    /// <summary>Cuántos ficheros distintos recuerda.</summary>
    public int Filas => _filas.Count;

    /// <summary>
    /// Cuántas de esas filas se han usado o añadido desde que se creó. Sirve para saber si
    /// merece la pena guardar: si no ha cambiado nada, no hay que reescribir el fichero.
    /// </summary>
    public int FilasUsadas => _filas.Values.Count(f => f.UsadaAhora);

    /// <summary>Si se ha añadido algo que el disco no tiene.</summary>
    public bool HayNovedades { get; private set; }

    // ───────────────────────── uso desde el motor ─────────────────────────

    /// <summary>La clave de un juego de títulos: su texto, en orden, resumido.</summary>
    internal static Guid ClaveDe(IReadOnlyList<TitleBag> titulos)
    {
        var sb = new StringBuilder();
        foreach (var t in titulos) sb.Append(t.Text).Append('\u0001');
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()), hash);
        return new Guid(hash[..16]);
    }

    internal bool TryGet(Guid clave, out double[] parecido, out byte[] casados)
    {
        if (_filas.TryGetValue(clave, out var f))
        {
            f.UsadaAhora = true;
            parecido = f.Parecido;
            casados = f.Casados;
            return true;
        }
        parecido = []; casados = [];
        return false;
    }

    internal void Add(Guid clave, double[] parecido, byte[] casados)
    {
        if (_filas.TryAdd(clave, new Fila { Parecido = parecido, Casados = casados, UsadaAhora = true }))
            HayNovedades = true;
    }

    // ───────────────────────── a disco y de vuelta ─────────────────────────

    /// <summary>
    /// El contenido listo para escribir. Primero van las filas usadas en esta sesión y luego
    /// las demás, y se corta al llegar al tope: lo que no se ha vuelto a pedir es lo primero
    /// en irse.
    /// </summary>
    public byte[] Serializar()
    {
        long porFila = BytesPorFila + (long)Episodios * (sizeof(double) + sizeof(byte));
        int cabe = (int)Math.Max(1, TopeEnBytes / Math.Max(1, porFila));

        var elegidas = _filas
            .OrderByDescending(kv => kv.Value.UsadaAhora)
            .Take(cabe)
            .ToList();

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            w.Write(Magia);
            w.Write(VersionDelFormato);
            w.Write(Huella);
            w.Write(Episodios);
            w.Write(elegidas.Count);
            foreach (var (clave, fila) in elegidas)
            {
                w.Write(clave.ToByteArray());
                w.Write(MemoryMarshal.AsBytes(fila.Parecido.AsSpan()));
                w.Write(fila.Casados);
            }
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Mete lo guardado en disco. Devuelve si valía: con otra huella, otro formato o el
    /// fichero cortado, no se toma NADA y la memoria sigue vacía —mejor recalcular que
    /// fiarse de algo a medias—.
    /// </summary>
    public bool Absorber(byte[] datos)
    {
        try
        {
            using var ms = new MemoryStream(datos);
            using var r = new BinaryReader(ms, Encoding.UTF8);
            if (r.ReadUInt32() != Magia) return false;
            if (r.ReadInt32() != VersionDelFormato) return false;
            if (r.ReadString() != Huella) return false;
            if (r.ReadInt32() != Episodios) return false;
            int cuantas = r.ReadInt32();
            if (cuantas < 0) return false;

            // Se lee a un cajón aparte y solo se vuelca si el fichero entero está bien.
            var leidas = new List<(Guid, Fila)>(Math.Min(cuantas, 100_000));
            for (int i = 0; i < cuantas; i++)
            {
                var clave = new Guid(r.ReadBytes(16));
                var parecido = new double[Episodios];
                var casados = new byte[Episodios];
                ms.ReadExactly(MemoryMarshal.AsBytes(parecido.AsSpan()));
                ms.ReadExactly(casados);
                leidas.Add((clave, new Fila { Parecido = parecido, Casados = casados }));
            }
            foreach (var (clave, fila) in leidas) _filas.TryAdd(clave, fila);
            return true;
        }
        catch { return false; }   // cortado o corrupto: se ignora, no se arregla
    }
}
