using Ondine.Complementos;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Un fichero puede traer historias de <b>episodios distintos</b>, y Ondine lo
/// escribe con un «+» en el número: <c>[1262+1264]</c>. Esto comprueba que
/// también sepa LEERLO.
///
/// <para>
/// Salió de un caso real: «te falta Autobús intergaláctico» sobre un episodio
/// que el usuario tenía, dentro de un fichero llamado
/// <c>Doraemon (1979) S1993E1262 [1262+1264] - Conservas de noche + Autobús
/// intergaláctico.mp4</c>. La app escribe ese nombre y luego solo leía el 1262,
/// así que la historia del 1264 no contaba como cubierta: <b>no sabía leer lo
/// que ella misma había escrito</b>.
/// </para>
/// </summary>
public static class NombreCompuestoTests
{
    public static void Todas()
    {
        Program.Seccion("Un fichero con historias de varios episodios");

        var nombre = "Doraemon (1979) S1993E1262 [1262+1264] - Conservas de noche + Autobús intergaláctico.mp4";
        var s = SignalExtractor.Extract(nombre, "Season 1993");

        Program.Assert(s.Indice == 1262,
            $"el número propio se lee · salió {s.Indice?.ToString() ?? "nada"}");

        Program.Assert(s.TambienEpisodios.Count == 1,
            $"y el episodio añadido también · salieron {s.TambienEpisodios.Count}");

        Program.Assert(s.TambienEpisodios.Count == 1 && s.TambienEpisodios[0].Num == 1264,
            "el añadido es el 1264, que es lo que dice el propio nombre");

        // Con letra de historia, que es la forma completa: «+1264b».
        var conLetra = SignalExtractor.Extract("Serie S01E12 [12a+14b] - Una + Otra.mkv", "Season 1");
        Program.Assert(conLetra.Indice == 12 && conLetra.SubSegmento == "a",
            "el propio conserva su letra");
        Program.Assert(conLetra.TambienEpisodios.Count == 1
                       && conLetra.TambienEpisodios[0].Num == 14
                       && conLetra.TambienEpisodios[0].Segmento == "b",
            "y el añadido trae su número y su letra");

        // Varios añadidos.
        var tres = SignalExtractor.Extract("Serie S01E12 [12+14+16] - A + B + C.mkv", "Season 1");
        Program.Assert(tres.TambienEpisodios.Count == 2,
            $"tres historias dan dos añadidos · salieron {tres.TambienEpisodios.Count}");

        // ── Y sobre todo: eso tiene que CONTAR como que lo tienes ─────────────
        // Es el fallo tal y como se vio: «te falta Autobús intergaláctico» sobre
        // un episodio que estaba dentro de ese mismo fichero.
        var catalogo = new ReindexCatalog
        {
            Serie = "Doraemon (1979)",
            Episodios =
            {
                new CatalogEpisode { Num = 1262, Temporada = 1993, Titulos = { ["es"] = new() { "Conservas de noche" } } },
                new CatalogEpisode { Num = 1264, Temporada = 1993, Titulos = { ["es"] = new() { "Autobús intergaláctico" } } },
            },
        };

        var loQueHay = new[]
        {
            new ReindexResolution
            {
                Archivo = s,
                Estado = ReindexEstado.Limpio,
                Episodio = catalogo.Episodios[0],
            },
        };

        // Lo que de verdad decide el «te falta»: qué cubre este fichero.
        var cubre = CoberturaCatalogo.LoQueCubre(loQueHay[0], catalogo).ToList();

        Program.Assert(cubre.Count == 2,
            $"el fichero cubre DOS episodios, no uno · salieron {cubre.Count}");
        Program.Assert(cubre.Any(c => c.Num == 1262) && cubre.Any(c => c.Num == 1264),
            "el suyo y el añadido: el 1262 y el 1264");

        // Sin el arreglo, «Autobús intergaláctico» (1264) no salía aquí y por eso
        // la app lo daba por ausente.
        Program.Assert(cubre.First(c => c.Num == 1264).Historias.Count > 0,
            "y del añadido se cubre su historia, que es lo que se decía que faltaba");

        // ── La OTRA forma en que la app escribe lo mismo ─────────────────────
        // La plantilla con <num> pega el añadido AL NÚMERO, sin corchetes:
        // «S2004E9042f+9044». Es un fichero real de una biblioteca real. La lectura
        // solo entendía la forma con corchetes, así que de este solo veía el 9042 —
        // el mismo fallo de arriba, en la otra escritura.
        var pegado = SignalExtractor.Extract(
            "Crayon Shin-Chan - S2004E9042f+9044 - Nevado lo pasa fatal + Soy un perro + Papá se pone a régimen.mp4",
            "Season 01");

        Program.Assert(pegado.Indice == 9042,
            $"el número propio se lee · salió {pegado.Indice?.ToString() ?? "nada"}");
        Program.Assert(pegado.SubSegmento == "f",
            $"y su letra de historia · salió «{pegado.SubSegmento}»");
        Program.Assert(pegado.TambienEpisodios.Count == 1,
            $"y el añadido pegado al número también · salieron {pegado.TambienEpisodios.Count}");
        Program.Assert(pegado.TambienEpisodios.Count == 1 && pegado.TambienEpisodios[0].Num == 9044,
            "el añadido es el 9044, que es lo que dice el propio nombre");

        // Con letra en el añadido, que es como lo escribe la plantilla cuando la hay.
        var pegadoConLetra = SignalExtractor.Extract("Serie - S01E12a+14b - Una + Otra.mkv", "Season 1");
        Program.Assert(pegadoConLetra.Indice == 12 && pegadoConLetra.SubSegmento == "a",
            "el propio conserva su letra");
        Program.Assert(pegadoConLetra.TambienEpisodios.Count == 1
                       && pegadoConLetra.TambienEpisodios[0].Num == 14
                       && pegadoConLetra.TambienEpisodios[0].Segmento == "b",
            "y el añadido trae número y letra");

        // Y lo de siempre sigue igual: sin «+» no hay añadidos.
        var simple = SignalExtractor.Extract("Serie S01E12 [12a] - Una.mkv", "Season 1");
        Program.Assert(simple.Indice == 12 && simple.TambienEpisodios.Count == 0,
            "un fichero normal no inventa episodios añadidos");
    }
}
