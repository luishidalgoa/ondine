using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué hay de cada episodio del catálogo, y en qué fichero.
///
/// <para>
/// Es la misma cuenta que «qué falta», mirada del derecho en vez de del revés:
/// allí interesa la lista de huecos, y aquí poder preguntar por UNO —porque lo
/// tienes delante en el explorador— y que además te diga dónde está.
/// </para>
/// </summary>
public static class TenenciaTests
{
    private const string Json = """
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 1, "temporada": 1, "titulos": { "es": ["Uno a", "Uno b"] } },
        { "num": 2, "temporada": 1, "titulos": { "es": ["Dos a", "Dos b"] } },
        { "num": 3, "temporada": 1, "titulos": { "es": ["Tres"] } }
      ]
    }
    """;

    public static void Todas()
    {
        Program.Seccion("Qué tengo de cada episodio");

        var cat = ReindexCatalog.Parse(Json);

        var entero = new ReindexResolution
            { Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", "uno.mkv"), "T1") }.Con(cat, 1);
        var medias = new ReindexResolution
            { Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", "dos-a.mkv"), "T1")
                                       .ConSegmento("a") }.Con(cat, 2);

        var q = CoberturaCatalogo.PorEpisodio(cat, new[] { entero, medias });

        // Un fichero sin letra cubre el episodio ENTERO aunque traiga dos historias:
        // es la biblioteca normal, sin partir.
        Program.Assert(q[1].Que == CoberturaCatalogo.Tengo.Entero, "el que está entero, entero");
        Program.Assert(q[2].Que == CoberturaCatalogo.Tengo.AMedias, "el que solo trae la «a», a medias");
        Program.Assert(q[3].Que == CoberturaCatalogo.Tengo.Nada, "del que no hay nada, nada");

        // Lo que pidió el usuario: que el distintivo APUNTE al fichero. Sin la ruta
        // el distintivo dice «lo tienes» y te deja a ti la tarea de encontrarlo.
        Program.Assert(q[1].Ficheros.Count == 1 &&
                       q[1].Ficheros[0].EndsWith("uno.mkv", StringComparison.OrdinalIgnoreCase),
            "y dice en qué fichero está");
        Program.Assert(q[3].Ficheros.Count == 0, "del que no hay nada no hay fichero que señalar");

        // Un episodio del catálogo SIEMPRE tiene respuesta, aunque no se haya
        // analizado nada: el explorador pinta una fila por episodio y una clave
        // que falta sería una fila sin distintivo, que se lee como «no lo tienes».
        var vacio = CoberturaCatalogo.PorEpisodio(cat, Array.Empty<ReindexResolution>());
        Program.Assert(vacio.Count == 3 && vacio.Values.All(v => v.Que == CoberturaCatalogo.Tengo.Nada),
            "sin analizar nada, todos salen como que no están");

        // Dos ficheros para el mismo episodio (una historia cada uno) lo completan
        // entre los dos, y los dos se pueden señalar.
        var a = new ReindexResolution
            { Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", "dos-a.mkv"), "T1")
                                       .ConSegmento("a") }.Con(cat, 2);
        var b = new ReindexResolution
            { Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", "dos-b.mkv"), "T1")
                                       .ConSegmento("b") }.Con(cat, 2);
        var dos = CoberturaCatalogo.PorEpisodio(cat, new[] { a, b });
        Program.Assert(dos[2].Que == CoberturaCatalogo.Tengo.Entero,
            "dos ficheros con una historia cada uno completan el episodio");
        Program.Assert(dos[2].Ficheros.Count == 2, "y se señalan los dos");
    }
}
