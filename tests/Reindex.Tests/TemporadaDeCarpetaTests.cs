using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué temporada dice el nombre de una carpeta.
///
/// <para>
/// Ya existía, pero exigía que el nombre fuera <b>solo</b> la temporada: «Season 3» sí,
/// «Season 3 (2011)» no. En una biblioteca de verdad los añadidos son la norma —el año, la
/// resolución, «Completa»— y cada carpeta no reconocida deja sus capítulos sin temporada.
/// </para>
/// <para>
/// <b>Y aquí es donde hacerlo «más listo» se hace mal.</b> Aflojar el patrón para que
/// acepte añadidos empieza a tragarse cualquier carpeta con un número dentro: «Los 4
/// Fantásticos» pasaría a ser la temporada 4. Un falso positivo es peor que no detectar
/// nada, porque no detectar deja el hueco a la vista y detectar mal manda los capítulos a
/// una carpeta equivocada con toda la confianza. Por eso la mitad de esta prueba son las
/// que NO deben reconocerse.
/// </para>
/// </summary>
public static class TemporadaDeCarpetaTests
{
    public static void Todas()
    {
        Program.Seccion("Qué temporada dice el nombre de una carpeta");

        // ── Lo que ya funcionaba y tiene que seguir funcionando ───────────────────
        foreach (var (carpeta, esperado) in new (string, int)[]
        {
            ("Season 3", 3), ("Temporada 3", 3), ("S03", 3), ("3", 3),
            ("season 12", 12), ("TEMPORADA 7", 7), ("2005", 2005),
        })
            Program.Assert(SignalExtractor.TemporadaDeCarpeta(carpeta) == esperado,
                $"«{carpeta}» sigue siendo la temporada {esperado}");

        // ── Lo que se añade: los adornos de una biblioteca real ───────────────────
        foreach (var (carpeta, esperado) in new (string, int)[]
        {
            ("Season 3 (2011)", 3),
            ("Season 03 - Complete", 3),
            ("Temporada 2 [1080p]", 2),
            ("Temporada 1 - Completa", 1),
            ("S02 - 720p", 2),
            ("Season 1 x264", 1),
        })
            Program.Assert(SignalExtractor.TemporadaDeCarpeta(carpeta) == esperado,
                $"«{carpeta}» es la temporada {esperado}: los adornos no la esconden");

        // ══ LO QUE NO DEBE RECONOCERSE ═══════════════════════════════════════════
        // Cada uno de estos es una carpeta que existe en bibliotecas reales y que un
        // patrón demasiado suelto convertiría en una temporada inventada.
        foreach (var carpeta in new[]
        {
            "Los 4 Fantásticos",     // el número es del título, no una temporada
            "1080p",                 // una carpeta de calidad
            "Season Finale",         // lleva la palabra pero no el número
            "Temporada de caza",     // «temporada» como palabra normal
            "Extras",
            "Películas",
            "",
            "   ",
        })
            Program.Assert(SignalExtractor.TemporadaDeCarpeta(carpeta) is null,
                $"«{carpeta}» NO es una temporada: inventarla manda los capítulos a otro sitio con toda la confianza");

        // ── Los especiales son la temporada 0 ─────────────────────────────────────
        // Es la convención de Plex y Jellyfin, y es lo que Ondine viene a servir. Antes
        // daban null, así que esos capítulos se quedaban sin temporada.
        foreach (var carpeta in new[] { "Specials", "Especiales", "specials", "ESPECIALES" })
            Program.Assert(SignalExtractor.TemporadaDeCarpeta(carpeta) == 0,
                $"«{carpeta}» es la temporada 0, que es como la llaman Plex y Jellyfin");

        // ── El año no se confunde con la temporada cuando hay las dos ─────────────
        Program.Assert(SignalExtractor.TemporadaDeCarpeta("Season 2 - 2011") == 2,
            "con temporada Y año, manda la temporada: el año es un adorno más");

        // Pero un año a secas sigue valiendo: hay series que se organizan así.
        Program.Assert(SignalExtractor.TemporadaDeCarpeta("2005") == 2005,
            "y un año a secas sigue siendo una temporada, que es como se numeran algunas series");
    }
}
