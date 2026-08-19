using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Dónde le toca vivir a una película, y cómo se llama cuando llega.
///
/// <para>
/// La convención es <c>Título (Año)/Título (Año).ext</c>, que es lo que esperan
/// Plex y Jellyfin. La <b>carpeta propia</b> no es capricho: es lo que les deja
/// meter dentro la carátula, los subtítulos y otras versiones del mismo montaje
/// sin que se confundan con los de la película de al lado.
/// </para>
/// <para>
/// Como <see cref="DestinoDeTemporada"/>, aquí no se toca el disco: son cuentas
/// con rutas. La decisión es lo que hay que poder probar, y es donde están los
/// errores que cuestan caro.
/// </para>
/// </summary>
public static class DestinoDePelicula
{
    /// <summary>
    /// El nombre canónico del fichero, ya sin lo que no vale en un nombre de
    /// Windows. La limpieza es la <b>misma</b> que usa la plantilla de los
    /// episodios: dos limpiezas distintas dejarían el mismo título escrito de dos
    /// formas según por dónde hubiera pasado.
    /// </summary>
    public static string Nombre(TituloDePelicula.Ficha ficha, string extension)
        => LibraryTemplate.LimpiarNombre(TituloDePelicula.Canonico(ficha)) + extension;

    /// <summary>La carpeta que le toca, dentro de la raíz de la biblioteca.</summary>
    public static string Carpeta(string raiz, TituloDePelicula.Ficha ficha)
        => Path.Combine(raiz, LibraryTemplate.LimpiarNombre(TituloDePelicula.Canonico(ficha)));

    /// <summary>
    /// A dónde hay que llevar este fichero, o <c>null</c> si no hay nada que
    /// hacer —porque ya está donde debe y con el nombre que debe, o porque de su
    /// nombre no sale ningún título—.
    ///
    /// <para>
    /// Estar en la carpeta correcta <b>no basta</b>: el escáner lee también el
    /// nombre del fichero, así que un «blade.runner.1982.1080p.mkv» dentro de
    /// «Blade Runner (1982)» sigue teniendo trabajo pendiente.
    /// </para>
    /// </summary>
    public static string? HayQueMover(string origen, string raiz)
    {
        var ficha = TituloDePelicula.Leer(Path.GetFileName(origen));

        // De un nombre del que no sale título no sale destino. Mover eso a una
        // carpeta inventada es la forma más rápida de perder un fichero.
        if (string.IsNullOrWhiteSpace(ficha.Titulo)) return null;

        var destino = Path.Combine(Carpeta(raiz, ficha), Nombre(ficha, Path.GetExtension(origen)));

        return string.Equals(Path.GetFullPath(destino), Path.GetFullPath(origen),
                             StringComparison.OrdinalIgnoreCase)
            ? null
            : destino;
    }
}
