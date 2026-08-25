using Ondine.Objetivo;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La aceleración de la DECODIFICACIÓN: qué se le pasa a ffmpeg antes de la entrada, cuál se
/// elige y qué se hace cuando falla.
///
/// <para>
/// <b>De dónde sale.</b> Ondine codificaba en la GPU y decodificaba siempre en la CPU. Un
/// usuario lo midió con Prometheus sobre media hora de compresión: NVDEC al <b>0,0 % incluso de
/// máximo</b> —en treinta minutos no se usó ni una vez— con la CPU al 88,6 %. La cadena
/// «hwaccel» no existía en el repositorio entero.
/// </para>
/// <para>
/// <b>Y lo que decide el diseño se midió también</b>, porque la intuición se equivocaba. Con
/// <c>-hwaccel X</c> hay dos fallos distintos y no se parecen en nada:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>El dispositivo no está</b> —«cuda» en una máquina sin NVIDIA, «vaapi» sin libva— y ffmpeg
/// se muere: código 127, «Cannot load nvcuda.dll». NO cae a software. Por eso la lista de
/// <c>ffmpeg -hwaccels</c> no vale como respuesta: en la máquina donde se probó esto salían
/// siete métodos y solo tres funcionaban. Hay que <b>probarlos de verdad</b>, uno a uno, como ya
/// se hacía con los codificadores.
/// </item>
/// <item>
/// <b>El códec no lo traga el dispositivo</b> —un DivX viejo, un WMV— y ahí sí: ffmpeg cae al
/// decodificador de software solo, sin error y sin decir nada (código 0). Se comprobó con cuatro
/// códecs raros. Por eso este diseño NO necesita una lista blanca de códecs: la necesitaría el
/// otro, el que se queda los fotogramas en la GPU con <c>-hwaccel_output_format</c>, donde un
/// códec no soportado mata la orden entera.
/// </item>
/// </list>
/// </summary>
public static class AceleracionDeVideoTests
{
    public static void Todas()
    {
        Program.Seccion("La aceleración de la decodificación");

        SoloLasQueSabemosUsar();
        CadaSistemaLaSuya();
        LaAutomaticaVaConElCodificador();
        LoGuardadoManda();
        LosArgumentos();
        LaSondaPasaPorDondePasaElTrabajo();
        DistinguirUnFalloDeAceleracion();
    }

    /// <summary>La salida de verdad de un ffmpeg de Windows, con sus siete métodos.</summary>
    private const string SieteMetodos = """
        Hardware acceleration methods:
        cuda
        vaapi
        dxva2
        qsv
        d3d11va
        d3d12va
        amf
        """;

    /// <summary>
    /// De lo que ffmpeg ofrece se cogen solo las que son un DECODIFICADOR y sabemos manejar.
    /// «amf» es la que enseña por qué: está en la lista de aceleraciones, AMD la usa para
    /// codificar, y como decodificador no existe — pedirla devuelve 171.
    /// </summary>
    private static void SoloLasQueSabemosUsar()
    {
        var c = AceleracionDeVideo.Candidatas(SieteMetodos, windows: true, mac: false);

        Program.Assert(!c.Contains("amf"), "«amf» no entra: es un codificador, no un decodificador");
        Program.Assert(c.Contains("cuda") && c.Contains("qsv") && c.Contains("d3d11va"),
            $"y sí las que se usan en Windows ({string.Join(", ", c)})");
        Program.Assert(!c.Contains("vaapi"),
            "vaapi no se ofrece en Windows aunque ffmpeg la liste: ahí no hay libva que conectar");

        // Y si ffmpeg no ofrece ninguna, no se inventa nada.
        var vacia = AceleracionDeVideo.Candidatas("Hardware acceleration methods:", true, false);
        Program.Assert(vacia.Count == 0, $"sin métodos no hay candidatas ({vacia.Count})");
    }

    private static void CadaSistemaLaSuya()
    {
        const string linux = "Hardware acceleration methods:\nvdpau\ncuda\nvaapi\nqsv\ndrm\nopencl\nvulkan";
        var enLinux = AceleracionDeVideo.Candidatas(linux, windows: false, mac: false);
        Program.Assert(enLinux.Contains("vaapi") && enLinux.Contains("cuda"),
            $"en Linux entran vaapi y cuda ({string.Join(", ", enLinux)})");
        Program.Assert(!enLinux.Contains("d3d11va") && !enLinux.Contains("vulkan") && !enLinux.Contains("drm"),
            "y no las de Windows ni las que no son decodificadores de vídeo al uso");

        const string mac = "Hardware acceleration methods:\nvideotoolbox";
        var enMac = AceleracionDeVideo.Candidatas(mac, windows: false, mac: true);
        Program.Assert(enMac.SequenceEqual(["videotoolbox"]),
            $"en Mac, la de Apple ({string.Join(", ", enMac)})");
    }

    /// <summary>
    /// «Automática» no es «la primera de la lista»: es la que hace pareja con el codificador que
    /// se va a usar. Decodificar en la Intel para codificar en la NVIDIA obliga a pasear cada
    /// fotograma de una tarjeta a la otra por la memoria del sistema.
    /// </summary>
    private static void LaAutomaticaVaConElCodificador()
    {
        string[] probadas = ["cuda", "qsv", "d3d11va"];

        Program.Assert(AceleracionDeVideo.Automatica(probadas, "hevc_nvenc") == "cuda",
            "con NVENC codificando, se decodifica en CUDA");
        Program.Assert(AceleracionDeVideo.Automatica(probadas, "hevc_qsv") == "qsv",
            "y con QSV, en QSV");

        // Codificando por software, la aceleración sigue valiendo: quita el decode de la CPU
        // aunque el resto siga ahí. Se coge la primera que haya funcionado.
        Program.Assert(AceleracionDeVideo.Automatica(probadas, "libx265") == "cuda",
            "codificando por software también se decodifica en la GPU: es trabajo que se quita igual");

        // Y si su pareja no funcionó en esta máquina, se usa lo que haya.
        Program.Assert(AceleracionDeVideo.Automatica(["d3d11va"], "hevc_nvenc") == "d3d11va",
            "si CUDA no arrancó, se usa la que sí");
        Program.Assert(AceleracionDeVideo.Automatica([], "hevc_nvenc") is null,
            "y sin ninguna probada, ninguna: null es «decodifica en la CPU»");
    }

    /// <summary>
    /// Lo que el usuario haya elegido en Preferencias manda — salvo que en esta máquina no
    /// funcione, y entonces no se le miente: se cae a la automática.
    /// </summary>
    private static void LoGuardadoManda()
    {
        string[] probadas = ["cuda", "qsv"];

        Program.Assert(AceleracionDeVideo.Elegida("qsv", probadas, "hevc_nvenc") == "qsv",
            "elegir QSV a mano manda sobre la automática, que habría dicho CUDA");
        Program.Assert(AceleracionDeVideo.Elegida(AceleracionDeVideo.Ninguna, probadas, "hevc_nvenc") is null,
            "«Ninguna» apaga la aceleración aunque haya de sobra");
        Program.Assert(AceleracionDeVideo.Elegida(AceleracionDeVideo.Auto, probadas, "hevc_nvenc") == "cuda",
            "«Automática» decide con el codificador");
        Program.Assert(AceleracionDeVideo.Elegida("", probadas, "hevc_nvenc") == "cuda",
            "y un ajuste vacío -el de quien viene de una versión anterior- es «Automática»");

        // Lo guardado que ya no vale: un portátil al que se le cambió la gráfica, o un ajuste
        // copiado de otra máquina. Se cae a la automática en vez de dar 127 en cada fichero.
        Program.Assert(AceleracionDeVideo.Elegida("vaapi", probadas, "hevc_nvenc") == "cuda",
            "y una elegida que aquí no funciona no se usa: se cae a la automática");
    }

    /// <summary>
    /// La sonda tiene que ejercitar el MISMO camino que la orden de verdad, y eso incluye el
    /// escalado por CPU.
    ///
    /// <para>
    /// La primera versión sondeaba sin filtros y daba QSV por bueno; al comprimir de verdad, con
    /// un <c>scale</c> en medio, la orden se moría. Una sonda que no pasa por donde pasa el
    /// trabajo real no está probando el trabajo real.
    /// </para>
    /// </summary>
    private static void LaSondaPasaPorDondePasaElTrabajo()
    {
        var args = AceleracionDeVideo.ArgumentosDeSonda("qsv", "x.avi");

        Program.Assert(args.Contains("-vf") && args.Any(a => a.StartsWith("scale=")),
            $"la sonda lleva un escalado de CPU, como la orden de verdad ({string.Join(" ", args)})");
        Program.Assert(args.Contains("-hwaccel_output_format"),
            "y los mismos argumentos de aceleración que se van a usar, no unos parecidos");
        Program.Assert(args.Contains("null") && args.Contains("-f"),
            "y tira el resultado: lo que se prueba es que la cadena arranque");
    }

    private static void LosArgumentos()
    {
        Program.Assert(AceleracionDeVideo.Argumentos("cuda").SequenceEqual(["-hwaccel", "cuda"]),
            "los argumentos son dos y van antes de la entrada");
        Program.Assert(AceleracionDeVideo.Argumentos(null).Count == 0,
            "sin aceleración no se manda nada: un «-hwaccel» de más es ruido que alguien depurará");

        // NUNCA se le pide que los fotogramas se QUEDEN en la tarjeta: el «scale» de CPU que
        // lleva la orden no los aceptaría y se moriría al montar el grafo. Ese es el otro
        // diseño, el de la cadena completa, y pide mucho más que esto.
        foreach (var a in new[] { "cuda", "qsv", "vaapi", "d3d11va" })
            Program.Assert(!AceleracionDeVideo.Argumentos(a).Contains(a + "_frames")
                           && AceleracionDeVideo.Argumentos(a).Count(x => x == a) == 1,
                $"«{a}» no se pide como formato de salida, solo como aceleración");

        // QSV ES LA EXCEPCIÓN, y hay que decirlo explícitamente. Medido: «-hwaccel qsv» a secas
        // hace que ffmpeg ponga «hwaccel_output_format qsv» POR SU CUENTA —lo avisa y dice que
        // está en desuso— y entonces el escalado de CPU no puede con los fotogramas:
        // «Impossible to convert between the formats supported by the filter…». La orden entera
        // se muere. Con «nv12» bajan a memoria de sistema y funciona.
        var qsv = AceleracionDeVideo.Argumentos("qsv");
        Program.Assert(qsv.SequenceEqual(["-hwaccel", "qsv", "-hwaccel_output_format", "nv12"]),
            $"QSV pide bajar los fotogramas a nv12, o ffmpeg se los queda en la GPU ({string.Join(" ", qsv)})");

        // Y las demás no lo llevan: forzar un formato donde no hace falta puede romper un
        // original de 10 bits, que baja en p010le y no en nv12.
        Program.Assert(!AceleracionDeVideo.Argumentos("cuda").Contains("nv12"),
            "y CUDA no fuerza formato: un original de 10 bits baja en p010le, no en nv12");
    }

    /// <summary>
    /// Para poder reintentar sin aceleración hay que saber que el fallo fue de la aceleración.
    /// Los textos son los de ffmpeg de verdad, copiados de las pruebas a mano.
    /// </summary>
    private static void DistinguirUnFalloDeAceleracion()
    {
        string[] suyos =
        [
            "[CUDA @ 000002] Cannot load nvcuda.dll\n[CUDA @ 000002] Could not dynamically load CUDA",
            "[VAAPI @ 0002] Failed to initialise VAAPI connection: -1 (unknown libva error). Device creation failed: -5.",
            "Device setup failed for decoder on input stream #0:0 : Generic error in an external library",
            "Failed setup for format cuda: hwaccel initialisation returned error.",
        ];
        foreach (var e in suyos)
            Program.Assert(AceleracionDeVideo.EsFalloDeAceleracion(e),
                $"se reconoce como fallo de aceleración: «{e.Split('\n')[0][..Math.Min(52, e.Split('\n')[0].Length)]}…»");

        string[] ajenos =
        [
            "Could not open file : Permission denied",
            "No space left on device",
            "Invalid data found when processing input",
            "",
        ];
        foreach (var e in ajenos)
            Program.Assert(!AceleracionDeVideo.EsFalloDeAceleracion(e),
                $"y NO se confunde con otra cosa: «{e}»");
    }
}
