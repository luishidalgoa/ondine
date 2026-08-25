namespace Ondine.Objetivo;

/// <summary>
/// La aceleración por hardware de la DECODIFICACIÓN: qué le pasamos a ffmpeg antes de la
/// entrada para que el original lo descomprima la tarjeta y no la CPU.
///
/// <para>
/// <b>El agujero que tapa.</b> Ondine codificaba en la GPU y decodificaba siempre en la CPU. Lo
/// midió un usuario con Prometheus sobre media hora de compresión de una temporada: NVDEC al
/// <b>0,0 % también de máximo</b> —en treinta minutos no se usó ni una vez— con la CPU al 88,6 %
/// y tres trabajos en paralelo. La cadena «hwaccel» no aparecía en el repositorio entero.
/// </para>
///
/// <para><b>Lo que decide este diseño está medido, porque la intuición se equivocaba.</b></para>
/// <list type="number">
/// <item>
/// <b>Que ffmpeg liste un método no significa que funcione.</b> En la máquina donde se probó
/// esto, <c>ffmpeg -hwaccels</c> ofrecía siete y solo tres arrancaban: pedir «cuda» sin NVIDIA
/// devuelve 127 con «Cannot load nvcuda.dll», y «vaapi» sin libva, otro 127. <b>No cae a
/// software</b>: se muere. Así que la lista se usa para saber qué PROBAR, y lo que se ofrece al
/// usuario es lo que de verdad arrancó — igual que ya se hacía con los codificadores.
/// </item>
/// <item>
/// <b>Un códec que el dispositivo no traga sí cae a software, y en silencio.</b> Probado con
/// cuatro códecs viejos (DivX, MS-MPEG4, WMV2, FLV1) sobre QSV: código 0 y decodificación por
/// CPU sin una palabra. Por eso aquí <b>no hace falta ninguna lista blanca de códecs</b>. La
/// necesitaría el otro diseño —el que se queda los fotogramas en la tarjeta con
/// <c>-hwaccel_output_format</c> y escala con <c>scale_cuda</c>—, donde un códec no soportado
/// mata la orden entera. Ese ahorra además el paseo de los fotogramas, y es otro trabajo.
/// </item>
/// <item>
/// <b>Los fotogramas BAJAN a memoria de sistema.</b> Si se quedaran en la tarjeta, el
/// <c>scale=-2:H</c> de la orden —que es un filtro de CPU— no los aceptaría y la orden moriría al
/// montar el grafo. Se paga una copia y se gana el decodificador entero. Y hay una excepción que
/// solo se descubrió comprimiendo de verdad: <b>a QSV hay que pedírselo</b>, porque con
/// «-hwaccel qsv» a secas ffmpeg se los queda por su cuenta. Está en <see cref="Argumentos"/>.
/// </item>
/// </list>
/// </summary>
public static class AceleracionDeVideo
{
    /// <summary>El ajuste guardado que significa «decide tú». También lo significa la cadena vacía.</summary>
    public const string Auto = "auto";

    /// <summary>El ajuste guardado que apaga la aceleración a propósito.</summary>
    public const string Ninguna = "ninguna";

    /// <summary>
    /// Las que sabemos usar como decodificador, en orden de preferencia y por sistema.
    ///
    /// <para>
    /// <b>«amf» no está</b>, y es la que explica la lista: ffmpeg la ofrece entre las
    /// aceleraciones, AMD la usa para CODIFICAR, y como decodificador no existe — pedirla
    /// devuelve 171. Que algo salga en <c>-hwaccels</c> no dice para qué sirve.
    /// </para>
    /// <para>
    /// «d3d12va» tampoco: funciona en la máquina donde se probó, pero es muy nueva y no aporta
    /// nada sobre «d3d11va» en lo que aquí se hace. «vulkan», «opencl» y «drm» son para filtros,
    /// no para decodificar. «vdpau» está sustituida por VAAPI en todo lo que no es antiguo.
    /// </para>
    /// </summary>
    private static string[] Conocidas(bool windows, bool mac) =>
        mac ? ["videotoolbox"]
        : windows ? ["cuda", "qsv", "d3d11va", "dxva2"]
        : ["cuda", "qsv", "vaapi"];

    /// <summary>
    /// Las que merece la pena probar: lo que este ffmpeg dice que trae, cruzado con lo que
    /// sabemos usar en este sistema, en nuestro orden y no en el suyo.
    /// </summary>
    /// <param name="salidaDeHwaccels">La salida cruda de <c>ffmpeg -hwaccels</c>.</param>
    public static IReadOnlyList<string> Candidatas(string? salidaDeHwaccels, bool windows, bool mac)
    {
        var ofrecidas = (salidaDeHwaccels ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. Conocidas(windows, mac).Where(ofrecidas.Contains)];
    }

    /// <summary>
    /// La automática: la que hace pareja con el codificador que se va a usar.
    ///
    /// <para>
    /// No es «la primera de la lista». Decodificar en la Intel para codificar en la NVIDIA
    /// obliga a pasear cada fotograma de una tarjeta a la otra por la memoria del sistema, y eso
    /// se come parte de lo que se venía a ganar. Si su pareja no funcionó aquí, se usa la que
    /// haya: incluso codificando por software, quitarle el decodificador a la CPU es trabajo que
    /// se quita igual.
    /// </para>
    /// </summary>
    /// <param name="probadas">Las que han arrancado de verdad en esta máquina.</param>
    /// <param name="codificador">El codificador ya elegido, p. ej. «hevc_nvenc».</param>
    public static string? Automatica(IReadOnlyList<string> probadas, string? codificador)
    {
        if (probadas.Count == 0) return null;

        var pareja = (codificador ?? "") switch
        {
            var e when e.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase) => "cuda",
            var e when e.EndsWith("_qsv", StringComparison.OrdinalIgnoreCase) => "qsv",
            var e when e.EndsWith("_videotoolbox", StringComparison.OrdinalIgnoreCase) => "videotoolbox",
            var e when e.EndsWith("_vaapi", StringComparison.OrdinalIgnoreCase) => "vaapi",
            _ => null,
        };

        return pareja is not null && probadas.Contains(pareja) ? pareja : probadas[0];
    }

    /// <summary>
    /// La que se va a usar: lo guardado en Preferencias, resuelto contra lo que funciona aquí.
    ///
    /// <para>
    /// Una elección guardada que en esta máquina no arranca <b>no se usa</b>: se cae a la
    /// automática. Pasa de verdad —un ajuste copiado de otro equipo, una gráfica cambiada— y la
    /// alternativa sería un 127 por cada fichero de la tanda.
    /// </para>
    /// </summary>
    /// <returns>El nombre de la aceleración, o <c>null</c> para decodificar en la CPU.</returns>
    public static string? Elegida(string? guardada, IReadOnlyList<string> probadas, string? codificador)
    {
        var g = (guardada ?? "").Trim();

        if (string.Equals(g, Ninguna, StringComparison.OrdinalIgnoreCase)) return null;
        if (g.Length == 0 || string.Equals(g, Auto, StringComparison.OrdinalIgnoreCase))
            return Automatica(probadas, codificador);

        return probadas.FirstOrDefault(p => string.Equals(p, g, StringComparison.OrdinalIgnoreCase))
               ?? Automatica(probadas, codificador);
    }

    /// <summary>
    /// Los argumentos, que van ANTES de la entrada porque son opciones de entrada.
    ///
    /// <para>
    /// <b>QSV es la excepción y hay que decírselo.</b> Con «-hwaccel qsv» a secas, ffmpeg pone
    /// «-hwaccel_output_format qsv» por su cuenta —lo avisa, y dice que ese comportamiento está
    /// en desuso—, así que los fotogramas se quedan en memoria de la tarjeta y el
    /// <c>scale=-2:H</c> de la orden, que es un filtro de CPU, no puede con ellos: «Impossible
    /// to convert between the formats supported by the filter…» y la orden entera se muere.
    /// Pidiendo <c>nv12</c> bajan a memoria de sistema y funciona. Medido con un fichero real:
    /// sin esto fallaba y con esto no.
    /// </para>
    /// <para>
    /// A las demás <b>no</b> se les fuerza formato. El de por defecto ya es bajarlos, y forzar
    /// «nv12» donde no hace falta rompería un original de 10 bits, que baja en <c>p010le</c>.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Argumentos(string? aceleracion) =>
        string.IsNullOrEmpty(aceleracion) ? []
        : string.Equals(aceleracion, "qsv", StringComparison.OrdinalIgnoreCase)
            ? ["-hwaccel", "qsv", "-hwaccel_output_format", "nv12"]
            : ["-hwaccel", aceleracion];

    /// <summary>
    /// Los argumentos con los que se PRUEBA una aceleración: decodificar un ficherito, escalarlo
    /// y tirar el resultado.
    ///
    /// <para>
    /// Hace falta un fichero de verdad —comprimido— porque lo que se prueba es el
    /// <b>decodificador</b>; una fuente sintética de <c>lavfi</c> no pasa por él, que es el
    /// agujero que tiene la sonda de los codificadores.
    /// </para>
    /// <para>
    /// Y lleva el <b>escalado por CPU</b>, que es el mismo camino que la orden de verdad. La
    /// primera versión sondeaba sin filtros: QSV pasaba la sonda y luego, al comprimir con un
    /// <c>scale</c> en medio, la orden se moría. Una sonda que no pasa por donde pasa el trabajo
    /// no prueba el trabajo.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ArgumentosDeSonda(string aceleracion, string ficherito) =>
        ["-hide_banner", "-loglevel", "error",
         .. Argumentos(aceleracion),
         "-i", ficherito, "-vf", "scale=-2:64", "-f", "null", "-"];

    /// <summary>
    /// Si un fallo de ffmpeg fue de la aceleración, para poder reintentar sin ella en vez de dar
    /// el fichero por perdido.
    ///
    /// <para>
    /// Los textos son los de ffmpeg de verdad, de las pruebas a mano. Y esto tiene que ser
    /// ESTRECHO: reintentar sin aceleración un fallo de permisos o de disco no arregla nada y
    /// tarda el doble en decirlo.
    /// </para>
    /// </summary>
    public static bool EsFalloDeAceleracion(string? salidaDeError)
    {
        var e = salidaDeError ?? "";
        if (e.Length == 0) return false;

        string[] señas =
        [
            "hwaccel",              // «Failed setup for format cuda: hwaccel initialisation returned error.»
            "Device creation failed",
            "Device setup failed",
            "Cannot load nvcuda",
            "Could not dynamically load CUDA",
            "Failed to initialise VAAPI",
            "No VA display found",
            "Error creating a NVDEC decoder",
            "Cannot load libcuda",
        ];

        return señas.Any(s => e.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
