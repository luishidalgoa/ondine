using System.IO;
using System.Text.Json;

namespace Ondine.Peliculas;

/// <summary>
/// Lo que <b>tú</b> has decidido sobre una película, guardado en disco.
///
/// <para>
/// La cascada se planta cuando dos candidatas encajan igual de bien —«Psicosis»
/// de 1960 y la de 1998, sin año en el fichero— y eso está bien: acertar la mitad
/// de las veces no es identificar. Pero si no puedes resolverlo tú, se planta
/// <b>para siempre</b>, y volver a resolverlo en cada análisis no es resolverlo.
/// </para>
/// <para>
/// Es lo mismo que ya hacen las series con sus decisiones, y por el mismo motivo.
/// Lo que decides manda sobre lo que la app habría deducido: es tu biblioteca y la
/// has mirado tú.
/// </para>
/// </summary>
public sealed class DecisionesDePelicula
{
    /// <summary>Una decisión tomada: qué película es, y cuándo lo dijiste.</summary>
    public sealed record Decidida(int Id, string Titulo, string? Original, int? Anio, string Cuando)
    {
        public Tmdb.Candidato Candidato() => new(Id, Titulo, Original, Anio);
    }

    private readonly string _fichero;
    private readonly Dictionary<string, Decidida> _lo;
    private bool _sucio;

    private DecisionesDePelicula(string fichero, Dictionary<string, Decidida> lo)
    {
        _fichero = fichero;
        _lo = lo;
    }

    /// <summary>Donde viven por defecto: con el resto de los datos del usuario.</summary>
    public static string Predeterminada()
        => Path.Combine(DatosDeUsuario.Raiz, "peliculas-decididas.json");

    /// <summary>
    /// Abre las decisiones. Un fichero ilegible se empieza de cero en vez de
    /// reventar: se pierde lo decidido, que es malo, pero menos que no arrancar.
    /// </summary>
    public static DecisionesDePelicula Abrir(string fichero)
    {
        try
        {
            if (File.Exists(fichero))
            {
                var leido = JsonSerializer.Deserialize<Dictionary<string, Decidida>>(
                    File.ReadAllText(fichero));
                if (leido is not null)
                    return new(fichero, new(leido, StringComparer.OrdinalIgnoreCase));
            }
        }
        catch { /* explicado arriba */ }

        return new(fichero, new(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Lo que decidiste para este fichero, o <c>null</c> si no dijiste nada.</summary>
    public Tmdb.Candidato? Para(string ruta)
        => _lo.TryGetValue(ruta, out var d) ? d.Candidato() : null;

    /// <summary>
    /// Apunta que esta película es esa. Decidir otra cosa <b>sustituye</b> a lo
    /// anterior: no se acumulan dos verdades sobre el mismo fichero.
    /// </summary>
    public void Recordar(string ruta, Tmdb.Candidato quien)
    {
        _lo[ruta] = new Decidida(quien.Id, quien.Titulo, quien.Original, quien.Anio,
                                 DateTime.Now.ToString("yyyy-MM-dd"));
        _sucio = true;
    }

    public void Olvidar(string ruta)
    {
        if (_lo.Remove(ruta)) _sucio = true;
    }

    /// <summary>
    /// La decisión sigue al fichero cuando se renombra o se mueve.
    ///
    /// <para>
    /// Sin esto se perdería <b>justo al aplicar</b> lo que acabas de decidir, que es
    /// cuando más falta hace conservarla: aplicar es lo que cambia el nombre.
    /// </para>
    /// </summary>
    public void Renombrado(string vieja, string nueva)
    {
        if (!_lo.Remove(vieja, out var d)) return;
        _lo[nueva] = d;
        _sucio = true;
    }

    /// <summary>Cuántas decisiones hay guardadas.</summary>
    public int Cuantas => _lo.Count;

    /// <summary>
    /// Escribe a disco lo que haya cambiado. A diferencia de la caché, esto <b>sí</b>
    /// son datos del usuario: si no se puede guardar, se pierde una decisión suya.
    /// </summary>
    public void Volcar()
    {
        if (!_sucio) return;

        try
        {
            var dir = Path.GetDirectoryName(_fichero);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(_fichero, JsonSerializer.Serialize(_lo));
            _sucio = false;
        }
        catch { /* quien llama decide si avisar; aquí no hay a quién */ }
    }
}
