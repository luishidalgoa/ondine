using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// Nombres de idioma a partir del código que traen los ficheros.
///
/// <para>
/// Existe porque «spa», «eng», «por» no le dicen nada a casi nadie: mirando una lista de pistas
/// así no puedes saber qué doblaje es cuál, que es justo lo que necesitas para elegir.
/// </para>
/// <para>
/// Los nombres ya no se escriben aquí: los pone <see cref="IsoLanguages"/>, que es la lista de
/// la interfaz, y de ahí salen también los códigos de tres letras (ISO 639-2) que mezclan los
/// ficheros. Antes había dos tablas con los mismos idiomas escritos dos veces; con dos idiomas
/// de interfaz habrían sido cuatro, y una lista repetida es una lista que acaba diciendo cosas
/// distintas en cada sitio.
/// </para>
/// <para>
/// Se enseña el nombre A SECAS. Una pista etiquetada «spa» no dice si el doblaje es de España o
/// de Hispanoamérica, y escribirlo sería afirmar algo que el fichero no cuenta.
/// </para>
/// </summary>
public static class Idiomas
{
    /// <summary>
    /// Lo único que no puede salir de la lista común. En un catálogo, «lat» es como la app
    /// vieja escribía el español de Hispanoamérica; en una pista de vídeo es el código ISO
    /// 639-2 del latín, que es lo que de verdad trae el fichero.
    /// </summary>
    private static readonly Dictionary<string, string> Excepciones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lat"] = "la",
    };

    /// <summary>
    /// El nombre del idioma, o el código tal cual si no lo conocemos — vale más un «zzz» raro
    /// que perder el dato. Devuelve vacío para «und» y para los que no declaran idioma: no es un
    /// idioma, es la ausencia de uno, y enseñarlo solo mete ruido.
    /// </summary>
    public static string Nombre(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "";
        var c = codigo.Trim();
        if (c.Equals("und", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "";
        if (Excepciones.TryGetValue(c, out var iso)) c = iso;
        return IsoLanguages.NombreLlano(c);
    }
}
