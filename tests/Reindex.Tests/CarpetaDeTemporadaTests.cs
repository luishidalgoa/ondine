using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Cómo se llama la carpeta donde va cada capítulo.
///
/// <para>
/// El nombre lo lee <b>Plex o Jellyfin</b>, no el usuario, y ahí está el juicio:
/// «Season 01» es la convención que los dos detectan sin fallar. Se permite
/// castellano porque se pidió, pero NO se hereda del idioma de la app — que la
/// interfaz esté en castellano no dice nada sobre qué entiende el escáner, y
/// derivarlo de ahí rompería en silencio justo lo que Ondine promete arreglar.
/// </para>
/// </summary>
public static class CarpetaDeTemporadaTests
{
    public static void Todas()
    {
        Program.Seccion("El nombre de la carpeta de temporada");

        Program.Assert(CarpetaDeTemporada.Nombre(1, false) == "Season 01",
            "la convención que Plex y Jellyfin reconocen siempre");
        Program.Assert(CarpetaDeTemporada.Nombre(1, true) == "Temporada 01",
            "en castellano cuando se pide a propósito");

        // Dos cifras: «Season 1» lo entienden, pero «Season 01» ordena bien en el
        // explorador -si no, la 10 se cuela entre la 1 y la 2-.
        Program.Assert(CarpetaDeTemporada.Nombre(9, false) == "Season 09",
            "con cero delante, para que el explorador no ponga la 10 tras la 1");
        Program.Assert(CarpetaDeTemporada.Nombre(10, false) == "Season 10",
            "y a partir de diez ya no se rellena");

        // Los especiales van al 00 por convención de los dos reproductores.
        Program.Assert(CarpetaDeTemporada.Nombre(0, false) == "Season 00",
            "los especiales son la temporada cero");
        Program.Assert(CarpetaDeTemporada.Nombre(0, true) == "Temporada 00",
            "y en castellano igual");

        // Series numeradas por AÑO -Doraemon (1979) va así-. Rellenar a dos cifras
        // aquí no aplica: «Season 1979» ya tiene cuatro.
        Program.Assert(CarpetaDeTemporada.Nombre(1979, false) == "Season 1979",
            "una temporada que es un año se queda tal cual");

        // Una temporada negativa no existe. Devolver «Season -1» crearía una
        // carpeta con un nombre imposible a partir de un dato corrupto.
        Program.Assert(CarpetaDeTemporada.Nombre(-3, false) is null,
            "una temporada imposible no da nombre de carpeta: no se inventa una");

        // Y reconocerse a sí misma: lo que Ondine escribe, Ondine tiene que volver
        // a leerlo como esa temporada, o al segundo pase movería el fichero otra vez.
        foreach (var t in new[] { 0, 1, 9, 10, 1979 })
            foreach (var es in new[] { false, true })
            {
                var nombre = CarpetaDeTemporada.Nombre(t, es)!;
                Program.Assert(SignalExtractor.TemporadaDeCarpeta(nombre) == t,
                    $"«{nombre}» se relee como la temporada {t}");
            }
    }
}
