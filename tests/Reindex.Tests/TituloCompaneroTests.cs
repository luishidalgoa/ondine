using System.IO;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Leer el título del <c>.nfo</c> que acompaña al vídeo, ANTES de identificar.
///
/// <para>
/// Hasta ahora el <c>.nfo</c> solo se leía de las filas que ya habían quedado en
/// duda. Y ahí está el problema que se pagó caro: un fichero puede quedar
/// <b>seguro y equivocado</b> —identificado por su número contra el episodio que
/// no era— y entonces nadie mira el <c>.nfo</c> que lo habría desmentido. Medido
/// en una carpeta real: los 53 dudosos tenían <c>.nfo</c> y los 53 dieron título.
/// </para>
/// <para>
/// Cuesta 91 ms leer los 59 de esa carpeta. Ese era el motivo de no hacerlo, y no
/// se sostiene.
/// </para>
/// </summary>
public static class TituloCompaneroTests
{
    public static void Todas()
    {
        Program.Seccion("El título del .nfo que acompaña al vídeo");

        var dir = Path.Combine(Path.GetTempPath(), "ondine-nfo-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            var video = Path.Combine(dir, "Serie S01E534.mp4");
            File.WriteAllText(video, "");

            // Sin compañero no hay título, y eso no es un error.
            Program.Assert(TituloCompanero.Leer(video) == null, "sin .nfo al lado, no hay título");

            File.WriteAllText(Path.ChangeExtension(video, ".nfo"),
                "<episodedetails><title>¡Kasukabetti Western! (I)</title></episodedetails>");
            Program.Assert(TituloCompanero.Leer(video) == "¡Kasukabetti Western! (I)",
                "con .nfo al lado, sale su título");

            // Un .nfo roto no puede tumbar la identificación de toda la carpeta:
            // como mucho, ese fichero se queda sin ayuda.
            File.WriteAllText(Path.ChangeExtension(video, ".nfo"), "esto no es XML <<<");
            Program.Assert(TituloCompanero.Leer(video) == null, "un .nfo roto no aporta, pero no revienta");

            // Y uno vacío tampoco inventa nada.
            File.WriteAllText(Path.ChangeExtension(video, ".nfo"), "<episodedetails><title>   </title></episodedetails>");
            Program.Assert(TituloCompanero.Leer(video) == null, "un título en blanco no cuenta como título");

            Program.Assert(TituloCompanero.Leer(null) == null, "ni null");
            Program.Assert(TituloCompanero.Leer("") == null, "ni una ruta vacía");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
