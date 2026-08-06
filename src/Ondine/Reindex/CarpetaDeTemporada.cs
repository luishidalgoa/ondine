namespace Ondine.Reindex;

/// <summary>
/// Cómo se llama la carpeta donde vive cada capítulo.
///
/// <para>
/// Ese nombre no lo lee quien usa Ondine: lo lee <b>Plex o Jellyfin</b> al
/// escanear. Por eso el idioma de la carpeta NO se hereda del idioma de la
/// aplicación —que la interfaz esté en castellano no dice nada sobre qué entiende
/// el escáner—, sino que se elige a propósito. Derivarlo del idioma rompería en
/// silencio justo lo que Ondine promete arreglar.
/// </para>
/// <para>
/// «Season NN» es la convención que ambos reconocen sin fallar. El castellano
/// está disponible porque se pidió, con esa advertencia delante.
/// </para>
/// </summary>
public static class CarpetaDeTemporada
{
    /// <summary>
    /// El nombre de la carpeta, o <c>null</c> si la temporada no es un número
    /// posible: de un dato corrupto no sale una carpeta, sale nada.
    /// </summary>
    public static string? Nombre(int temporada, bool enCastellano)
    {
        if (temporada < 0) return null;

        // Dos cifras. «Season 1» también se entiende, pero con el cero delante el
        // explorador ordena bien: sin él, la 10 se cuela entre la 1 y la 2. A
        // partir de dos cifras no se toca -las series numeradas por año, como
        // Doraemon (1979), ya traen cuatro-.
        var num = temporada < 10 ? $"0{temporada}" : temporada.ToString();

        // Los especiales son la temporada cero en los dos reproductores.
        return (enCastellano ? "Temporada " : "Season ") + num;
    }
}
