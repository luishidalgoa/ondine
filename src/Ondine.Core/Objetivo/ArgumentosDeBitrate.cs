namespace Ondine.Objetivo;

/// <summary>
/// Cómo se le pide a cada codificador un bitrate concreto.
///
/// <para>
/// <b>CRF y bitrate objetivo se excluyen.</b> Si van los dos, cada codificador hace una cosa
/// distinta —unos ignoran el bitrate, otros el CRF, x265 avisa y sigue— y el resultado es
/// que no obedece a ninguno: el fichero sale con un tamaño que no es el pedido y una calidad
/// que tampoco elegiste. Y no falla, así que hay que darse cuenta mirando el resultado.
/// Por eso esto vive aparte de <c>EncoderArgs</c> y no como una bandera más dentro.
/// </para>
/// <para>
/// El techo y el colchón no son adorno: sin ellos un VBR se dispara en las escenas movidas
/// y el fichero se pasa del objetivo justo donde el objetivo importaba.
/// </para>
/// </summary>
public static class ArgumentosDeBitrate
{
    /// <summary>
    /// Cuánto puede subir por encima del objetivo en un tramo movido. Una vez y media deja
    /// sitio para la acción sin desbocar el total.
    /// </summary>
    public const double FactorTecho = 1.5;

    /// <summary>
    /// El colchón con el que el codificador reparte entre escenas. El doble del objetivo es
    /// lo habitual: más pequeño y no puede compensar, más grande y se le va el control.
    /// </summary>
    public const double FactorColchon = 2.0;

    public static IReadOnlyList<string> Para(string encoder, int kbps)
    {
        // Pedir cero -o menos- no es pedir un bitrate. Devolver algo aquí dejaría a ffmpeg
        // con un «-b:v 0k», que en varios codificadores significa «calidad constante» y es
        // justo lo contrario de lo que se venía a hacer.
        if (kbps <= 0) return [];

        var techo = (int)(kbps * FactorTecho);
        var colchon = (int)(kbps * FactorColchon);

        List<string> comunes = ["-b:v", $"{kbps}k", "-maxrate", $"{techo}k", "-bufsize", $"{colchon}k"];

        return encoder switch
        {
            // NVENC necesita que se le diga el modo; si no, se queda en el suyo por defecto
            // y el «-b:v» pasa a ser una sugerencia que no cumple.
            "hevc_nvenc" or "h264_nvenc" or "av1_nvenc" =>
                ["-c:v", encoder, "-rc", "vbr", .. comunes, "-preset", "p6", "-tune", "hq"],

            // AMF, lo mismo con su propio nombre para el modo.
            "hevc_amf" or "h264_amf" or "av1_amf" =>
                ["-c:v", encoder, "-rc", "vbr_peak", .. comunes, "-quality", "quality"],

            // QSV entiende «-b:v» directamente en cuanto no se le da «-global_quality».
            "hevc_qsv" or "h264_qsv" or "av1_qsv" or "vp9_qsv" =>
                ["-c:v", encoder, .. comunes, "-preset", "slow"],

            "libsvtav1" => ["-c:v", "libsvtav1", .. comunes, "-preset", "6"],
            "libvpx-vp9" => ["-c:v", "libvpx-vp9", .. comunes, "-row-mt", "1"],
            "libaom-av1" => ["-c:v", "libaom-av1", .. comunes, "-cpu-used", "6", "-row-mt", "1"],

            _ => ["-c:v", encoder, .. comunes, "-preset", "medium"],   // libx264 / libx265
        };
    }
}
