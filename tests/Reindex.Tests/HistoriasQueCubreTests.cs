using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué historias de un episodio tapa REALMENTE un fichero.
///
/// <para>
/// La regla vieja era «sin letra de segmento, tapa el episodio entero», y es
/// buena para una biblioteca sin partir. Pero se rompe con el caso más común de
/// todos: un fichero llamado <c>S1986E985 - El controlador del mar</c> cuando el
/// episodio 985 trae DOS historias. No lleva letra, así que se daba por
/// completo — y el cotejo de una lista de fuera contestaba «ya lo tienes» sobre
/// un vídeo que traía la historia que falta.
/// </para>
/// <para>
/// El nombre ya lo dice: nombra una de las dos. Eso es evidencia, y usarla es lo
/// que distingue «lo tengo» de «tengo la mitad».
/// </para>
/// <para>
/// Vive en un solo sitio porque la misma cuenta la hacían tres: el informe de
/// «qué falta», el distintivo del explorador y el cotejo de listas. Tres copias
/// de la misma regla son tres criterios en cuanto alguien toca uno.
/// </para>
/// </summary>
public static class HistoriasQueCubreTests
{
    private static readonly ReindexCatalog Cat = ReindexCatalog.Parse("""
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 985, "temporada": 1986,
          "titulos": { "es": ["El controlador del mar", "Alquiler estilo futurista"] } },
        { "num": 3, "temporada": 1986, "titulos": { "es": ["Historia sola"] } },
        { "num": 7, "temporada": 1986,
          "titulos": { "es": ["Uno de tres", "Dos de tres", "Tres de tres"] } }
      ]
    }
    """);

    private static ReindexResolution R(string nombre, int num, string? seg = null)
    {
        var a = SignalExtractor.Extract(Path.Combine("C:", "tv", nombre), "T1");
        if (seg is not null) a = a.ConSegmento(seg);
        return new ReindexResolution { Archivo = a, Episodio = Cat.PorNum(num) };
    }

    private static string Letras(ReindexResolution r) =>
        new(CoberturaCatalogo.HistoriasQueCubre(r, r.Episodio!)
            .OrderBy(i => i).Select(i => (char)('a' + i)).ToArray());

    public static void Todas()
    {
        Program.Seccion("Qué historias tapa un fichero");

        // La letra explícita manda siempre: si alguien la escribió, sabe más que
        // cualquier deducción a partir del título.
        Program.Eq("a", Letras(R("Serie - S1986E985a - El controlador del mar.avi", 985, "a")),
            "con letra «a», solo la a");
        Program.Eq("ac", Letras(R("Serie - S1986E7ac - Dos historias.avi", 7, "ac")),
            "con «ac», la a y la c");

        // Un episodio de una sola historia: no hay nada que deducir.
        Program.Eq("a", Letras(R("Serie - S1986E3 - Historia sola.avi", 3)),
            "un episodio de una historia lo tapa entero");

        // El nombre nombra las DOS: tapa el episodio entero, como siempre.
        Program.Eq("ab", Letras(R(
            "Serie - S1986E985 - El controlador del mar + Alquiler estilo futurista.avi", 985)),
            "si el nombre trae las dos historias, las tapa las dos");

        // ── EL FALLO ──
        // Sin letra, pero el nombre solo dice UNA de las dos. Antes se daba por
        // completo y el cotejo contestaba «ya lo tienes» sobre un vídeo que traía
        // justo la historia que falta.
        Program.Eq("a", Letras(R("Serie - S1986E985 - El controlador del mar.avi", 985)),
            "sin letra pero nombrando solo la primera, solo tapa la primera");
        Program.Eq("b", Letras(R("Serie - S1986E985 - Alquiler estilo futurista.avi", 985)),
            "y si nombra solo la segunda, solo la segunda");

        // Dos de tres, en cualquier orden.
        Program.Eq("ac", Letras(R("Serie - S1986E7 - Uno de tres + Tres de tres.avi", 7)),
            "nombrando dos de las tres, tapa esas dos");

        // ── LA RED DE SEGURIDAD ──
        // Si el nombre no se parece a ninguna de las historias, NO se deduce nada
        // y se vuelve a la regla de siempre. Deducir de un nombre que no dice nada
        // convertiría un fichero completo en «te falta la mitad», que es el error
        // contrario y también cuesta: te bajas otra vez lo que ya tienes.
        Program.Eq("ab", Letras(R("Serie - S1986E985 - vhsrip buena calidad.avi", 985)),
            "un nombre que no dice nada no deduce: tapa el episodio entero");
        Program.Eq("ab", Letras(R("Serie - S1986E985.avi", 985)),
            "y uno sin título tampoco");
    }
}
