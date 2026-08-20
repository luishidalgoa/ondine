using Ondine.Complementos;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Un vídeo de una lista puede traer DOS episodios del catálogo, no dos historias
/// de uno.
///
/// <para>
/// Medido con un caso real: el vídeo «El controlador del mar + Alquiler estilo
/// futurista» junta el episodio 985 y el 1237, que en el catálogo son entradas
/// separadas de una historia cada una. El cotejo elegía el que mejor casaba
/// —985—, veía que estaba completo y contestaba <b>«ya lo tienes»</b>. El 1237,
/// que no estaba, no aparecía en ninguna cuenta: ni en la de lo que tienes ni en
/// la de lo que falta.
/// </para>
/// <para>
/// El fallo no era el umbral. «Alquiler estilo futurista» casaba con el 1237 al
/// 0,94, muy por encima; simplemente se descartaba por no ser el mejor. Un trozo
/// que casa bien con OTRO episodio no es ruido: es una segunda cosa que trae el
/// vídeo.
/// </para>
/// </summary>
public static class CotejoDeDosEpisodiosTests
{
    private static readonly ReindexCatalog Cat = ReindexCatalog.Parse("""
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 985,  "temporada": 1988, "titulos": { "es": ["El controlador del mar"] } },
        { "num": 1237, "temporada": 1998, "titulos": { "es": ["Alquiler estilo futurista"] } },
        { "num": 500,  "temporada": 1984, "titulos": { "es": ["Parte una", "Parte dos"] } }
      ]
    }
    """);

    private static ReindexResolution Tengo(int num, string titulo) => new()
    {
        Archivo = SignalExtractor.Extract(
            Path.Combine("C:", "tv", $"Serie - S1988E{num} - {titulo}.avi"), "T1"),
        Episodio = Cat.PorNum(num),
    };

    public static void Todas()
    {
        Program.Seccion("Un vídeo con dos episodios dentro");

        var lista = new[] { "El controlador del mar + Alquiler estilo futurista" };

        // ── EL FALLO ──
        // Tengo el 985 y NO tengo el 1237. El vídeo trae los dos.
        var solo985 = CotejoDeLista.Cotejar(lista, Cat,
            new[] { Tengo(985, "El controlador del mar") });

        Program.Eq(CotejoDeLista.Estado.AMedias, solo985[0].Estado,
            "teniendo solo uno de los dos episodios, el vídeo está A MEDIAS");
        Program.Assert(solo985[0].TitulosQueFaltan.Any(t => t.Contains("Alquiler")),
            "y dice que lo que falta es «Alquiler estilo futurista»");

        // Con los dos, sí está entero.
        var losDos = CotejoDeLista.Cotejar(lista, Cat, new[]
        {
            Tengo(985, "El controlador del mar"),
            Tengo(1237, "Alquiler estilo futurista"),
        });
        Program.Eq(CotejoDeLista.Estado.YaEsta, losDos[0].Estado,
            "con los dos episodios, ya lo tienes entero");
        Program.Eq(0, losDos[0].TitulosQueFaltan.Count, "y no falta nada");

        // Sin ninguno, falta entero.
        var ninguno = CotejoDeLista.Cotejar(lista, Cat, Array.Empty<ReindexResolution>());
        Program.Eq(CotejoDeLista.Estado.Falta, ninguno[0].Estado,
            "sin ninguno de los dos, falta entero");
        Program.Eq(2, ninguno[0].TitulosQueFaltan.Count, "y faltan las dos cosas");

        // ── Lo de siempre sigue funcionando ──
        // Un episodio de DOS historias del que tienes una: sigue saliendo a medias,
        // que es el caso que ya se resolvía antes.
        var unaDeDos = CotejoDeLista.Cotejar(new[] { "Parte una + Parte dos" }, Cat,
            new[] { Tengo(500, "Parte una") });
        Program.Eq(CotejoDeLista.Estado.AMedias, unaDeDos[0].Estado,
            "un episodio de dos historias con una sola sigue saliendo a medias");

        // Un vídeo de una sola cosa que tienes: entero, sin inventar mitades.
        var simple = CotejoDeLista.Cotejar(new[] { "El controlador del mar" }, Cat,
            new[] { Tengo(985, "El controlador del mar") });
        Program.Eq(CotejoDeLista.Estado.YaEsta, simple[0].Estado,
            "un vídeo de una sola historia que tienes sale entero");

        // Y el episodio que se enseña sigue siendo el que mejor casa, para que el
        // «episodio 985» del detalle no cambie de significado.
        Program.Eq(985, solo985[0].Episodio?.Num,
            "el número que se enseña es el del trozo que mejor casó");

        // yt-dlp conserva en el nombre las barras verticales de ancho completo
        // que usa YouTube. El análisis inyectado y el cotejo de la playlist deben
        // entender el mismo título para que «te falta» cambie al terminar.
        var catYoutube = ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0", "serie": "Shin chan",
          "episodios": [
            { "num": 513, "temporada": 2004,
              "titulos": { "es": ["Así son los 24 minutos de papá", "Celebramos una competición en la escuela"] } }
          ]
        }
        """);
        var nombreYoutube = "Shin chan ｜ ¡Eh, que celebramos una competición en la escuela! ｜ Episodio 551 en español [6OjZVHt4QHQ].mp4";
        var señalYoutube = SignalExtractor.Extract(Path.Combine("C:", "tv", nombreYoutube), "Descargas");
        var resYoutube = ReindexEngine.Resolve(new[] { señalYoutube }, catYoutube)[0];
        Program.Eq(513, resYoutube.Episodio?.Num,
            "el fichero descargado con barras de YouTube se inyecta en su episodio real");
        var actualizado = CotejoDeLista.Cotejar(
            new[] { "¡Eh, que celebramos una competición en la escuela!" },
            catYoutube, new[] { resYoutube });
        Program.Eq(CotejoDeLista.Estado.YaEsta, actualizado[0].Estado,
            "tras inyectarlo, la playlist deja de decir «te falta»");
    }
}
