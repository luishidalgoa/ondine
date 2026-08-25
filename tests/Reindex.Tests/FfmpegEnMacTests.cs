namespace Ondine.Reindex.Tests;

/// <summary>
/// Encontrar ffmpeg en un Mac, que no es lo mismo que tenerlo instalado.
///
/// <para>
/// <b>Este es un fallo que solo se da en la app empaquetada</b>, y por eso no lo habría visto
/// nadie probando. Ondine busca <c>ffmpeg</c> al lado de su ejecutable y, si no está, se fía
/// del PATH. En Windows y en Linux eso vale. En macOS, no: una aplicación abierta desde el
/// Finder <b>no hereda el PATH del terminal</b> — recibe uno mínimo, con
/// <c>/usr/bin:/bin:/usr/sbin:/sbin</c> y nada más.
/// </para>
/// <para>
/// Y ahí está el problema: Homebrew, que es como se instala ffmpeg en un Mac, lo pone en
/// <c>/opt/homebrew/bin</c> (chip de Apple) o <c>/usr/local/bin</c> (Intel). Ninguna de las
/// dos está en ese PATH mínimo. Resultado: <c>brew install ffmpeg</c>, funciona en el
/// terminal, y Ondine abierta con doble clic dice que no está. Desde el terminal sí — que es
/// la clase de fallo que hace dudar de si uno se ha vuelto loco.
/// </para>
/// </summary>
public static class FfmpegEnMacTests
{
    public static void Todas()
    {
        Program.Seccion("Encontrar ffmpeg en un Mac");

        LasCarpetasDeHomebrew();
        ElOrdenImporta();
        SinNadaNoSeInventaNada();
    }

    private static void LasCarpetasDeHomebrew()
    {
        var chipApple = Engine.HerramientaEnMac("ffmpeg", r => r == "/opt/homebrew/bin/ffmpeg");
        Program.Assert(chipApple == "/opt/homebrew/bin/ffmpeg",
            $"la de Homebrew en los Mac con chip de Apple ({chipApple})");

        var intel = Engine.HerramientaEnMac("ffmpeg", r => r == "/usr/local/bin/ffmpeg");
        Program.Assert(intel == "/usr/local/bin/ffmpeg",
            $"y la de los Mac Intel, que Homebrew pone en otra ({intel})");

        var macports = Engine.HerramientaEnMac("ffprobe", r => r == "/opt/local/bin/ffprobe");
        Program.Assert(macports == "/opt/local/bin/ffprobe",
            "y la de MacPorts, que también existe y también queda fuera del PATH mínimo");
    }

    /// <summary>
    /// Con las dos instaladas gana la de la arquitectura nativa. Un Mac con chip de Apple
    /// puede tener las dos —Homebrew de Intel bajo Rosetta y el nativo— y coger el de Intel
    /// significa que cada compresión pasa por la traducción, más lenta y sin decirlo.
    /// </summary>
    private static void ElOrdenImporta()
    {
        var r = Engine.HerramientaEnMac("ffmpeg", _ => true);
        Program.Assert(r == "/opt/homebrew/bin/ffmpeg",
            $"con las dos, gana la nativa y no la de Rosetta ({r})");
    }

    private static void SinNadaNoSeInventaNada()
    {
        Program.Assert(Engine.HerramientaEnMac("ffmpeg", _ => false) is null,
            "sin ffmpeg por ningún lado se devuelve nada, y quien pregunta se queda con el PATH");
    }
}
