using System.IO;
using System.Text.Json;
using Ondine.Reindex;

namespace Ondine.Peliculas;

/// <summary>
/// Lo que ya se le ha preguntado a TMDb, guardado en disco.
///
/// <para>
/// Dos motivos. Uno: <b>no preguntar dos veces lo mismo</b> — la misma carpeta se
/// analiza muchas veces y la ficha de una película de 1972 no va a cambiar—. Y
/// dos: <b>que funcione sin red</b> con lo ya consultado, que es el caso de una
/// biblioteca en un equipo sin conexión.
/// </para>
/// <para>
/// No caduca a propósito. Lo que se guarda es qué película es, no cuánto la
/// votan; eso no cambia. Y una caducidad convertiría «funciona sin red» en
/// «funcionó sin red durante un mes».
/// </para>
/// </summary>
public sealed class CacheDePeliculas
{
    private readonly string _fichero;
    private readonly Dictionary<string, List<Tmdb.Candidato>> _lo;
    private bool _sucio;

    private CacheDePeliculas(string fichero, Dictionary<string, List<Tmdb.Candidato>> lo)
    {
        _fichero = fichero;
        _lo = lo;
    }

    /// <summary>Donde vive por defecto: con el resto de los datos del usuario.</summary>
    public static string Predeterminada() => Path.Combine(DatosDeUsuario.Raiz, "tmdb-cache.json");

    /// <summary>
    /// Abre la caché. Si el fichero no está, o no se entiende, se empieza de
    /// cero: <b>esto es una caché, no datos del usuario</b>, y volver a preguntar
    /// cuesta una consulta mientras que tumbar la app cuesta la sesión.
    /// </summary>
    public static CacheDePeliculas Abrir(string fichero)
    {
        try
        {
            if (File.Exists(fichero))
            {
                var leido = JsonSerializer.Deserialize<Dictionary<string, List<Tmdb.Candidato>>>(
                    File.ReadAllText(fichero));
                if (leido is not null) return new(fichero, new(leido, StringComparer.Ordinal));
            }
        }
        catch { /* explicado arriba: una caché ilegible se descarta, no se llora */ }

        return new(fichero, new(StringComparer.Ordinal));
    }

    /// <summary>
    /// La pregunta, hecha llave. El título va normalizado con el mismo criterio
    /// que el resto del motor —sin mayúsculas ni acentos—, así que «El pasajero»
    /// y «  el PASAJERO » son la misma pregunta. El año y el idioma forman parte
    /// de ella: la respuesta trae el título traducido.
    /// </summary>
    public static string Llave(string titulo, int? anio, string idioma)
        => TitleMatch.Norm(titulo) + "|" + (anio?.ToString() ?? "") + "|"
           + (idioma ?? "").Trim().ToLowerInvariant();

    /// <summary>
    /// Lo guardado, o <c>null</c> si esto no se ha preguntado nunca.
    ///
    /// <para>
    /// «Nunca se preguntó» y «se preguntó y no había nada» son distintos, y por
    /// eso esto devuelve nulo y no una lista vacía. Sin la diferencia, cada
    /// análisis vuelve a preguntar por las que nunca se van a encontrar — que
    /// son justo las que más se repiten en una carpeta.
    /// </para>
    /// </summary>
    public IReadOnlyList<Tmdb.Candidato>? Buscar(string titulo, int? anio, string idioma)
        => _lo.TryGetValue(Llave(titulo, anio, idioma), out var v) ? v : null;

    /// <summary>Guarda una respuesta, incluso si fue «no hay nada».</summary>
    public void Guardar(string titulo, int? anio, string idioma, IReadOnlyList<Tmdb.Candidato> candidatos)
    {
        _lo[Llave(titulo, anio, idioma)] = candidatos.ToList();
        _sucio = true;
    }

    /// <summary>Cuántas preguntas hay guardadas.</summary>
    public int Cuantas => _lo.Count;

    /// <summary>
    /// Escribe a disco lo que haya cambiado. Si falla no se avisa a nadie: no
    /// poder guardar una caché no es un problema del usuario, y el precio es
    /// volver a preguntar.
    /// </summary>
    public void Volcar()
    {
        if (!_sucio) return;

        try
        {
            var dir = Path.GetDirectoryName(_fichero);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(_fichero, JsonSerializer.Serialize(_lo,
                new JsonSerializerOptions { WriteIndented = false }));
            _sucio = false;
        }
        catch { /* explicado arriba */ }
    }
}
