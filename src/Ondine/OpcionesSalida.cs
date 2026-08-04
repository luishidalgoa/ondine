using Ondine.Localizacion;

namespace Ondine;

/// <summary>
/// El único sitio donde se traduce «lo que eligió el usuario en los desplegables» a
/// <see cref="EncodeOptions"/>.
///
/// Existe porque Comprimir y Recortes ofrecen los MISMOS ajustes de salida: con dos copias
/// del switch, el día que se añada un formato o cambie un CRF una de las dos se queda atrás
/// y nadie se entera hasta que el fichero sale distinto. Aquí también vive la LISTA de
/// opciones de los desplegables, para que las dos páginas ni siquiera puedan ofrecer
/// opciones distintas; los rótulos los pone la localización (claves <c>Main…</c>), las
/// mismas que usa Comprimir en su XAML.
/// </summary>
public static class OpcionesSalida
{
    // Los rótulos son PROPIEDADES, no arrays estáticos: un `static readonly`
    // se rellena una vez, con el idioma que hubiera al arrancar, y Recortes se
    // quedaba en castellano con la aplicación en inglés. Evaluándolos al
    // pedirlos, quien los muestre los vuelve a leer traducidos.
    //
    // Y son las MISMAS claves que usa Comprimir en su XAML (Main…): el ajuste
    // es el mismo, así que el texto tiene que ser el mismo. Con dos juegos de
    // claves, cambiar «27 · equilibrada» en una pantalla lo dejaba distinto en
    // la otra.
    private static Textos Txt => Textos.Instancia;

    public static string[] Formatos =>
    [
        Txt.MainFormatoMkv, Txt.MainFormatoMp4, Txt.MainFormatoWebm,
        Txt.MainFormatoMp3, Txt.MainFormatoM4a, Txt.MainFormatoFlac, Txt.MainFormatoOpus,
    ];

    public static string[] Codecs => [Txt.MainCodecH265, Txt.MainCodecH264, Txt.MainCodecAv1];

    public static string[] Calidades =>
    [
        Txt.MainCalidadAuto, Txt.MainCalidad22, Txt.MainCalidad24, Txt.MainCalidad27, Txt.MainCalidad30,
    ];

    public static string[] Resoluciones =>
    [
        Txt.MainResolucionSinCambio, Txt.MainResolucion1080, Txt.MainResolucion720, Txt.MainResolucion480,
    ];

    public static string[] Audios =>
    [
        Txt.MainAudioCopiar, Txt.MainAudioAac192, Txt.MainAudioAac160, Txt.MainAudioAac128, Txt.MainAudioAac96,
    ];

    public static string CodecDe(int i) => i switch { 1 => "h264", 2 => "av1", _ => "hevc" };
    public static int CalidadDe(int i) => i switch { 1 => 22, 2 => 24, 3 => 27, 4 => 30, _ => 0 };
    public static int AlturaDe(int i) => i switch { 1 => 1080, 2 => 720, 3 => 480, _ => 0 };
    public static int AudioDe(int i) => i switch { 1 => 192, 2 => 160, 3 => 128, 4 => 96, _ => 0 };

    /// <summary>Aplica el formato elegido sobre unas opciones ya empezadas.</summary>
    public static void PonerFormato(EncodeOptions opt, int i)
    {
        switch (i)
        {
            case 1: opt.Container = "mp4"; break;
            case 2: opt.Container = "webm"; break;
            case 3: opt.AudioOnly = true; opt.AudioFormat = "mp3"; break;
            case 4: opt.AudioOnly = true; opt.AudioFormat = "m4a"; break;
            case 5: opt.AudioOnly = true; opt.AudioFormat = "flac"; break;
            case 6: opt.AudioOnly = true; opt.AudioFormat = "opus"; break;
            default: opt.Container = "mkv"; break;
        }
    }

    /// <summary>Las cinco elecciones, ya convertidas en opciones de codificación.</summary>
    public static EncodeOptions Construir(int formato, int codec, int calidad, int resolucion, int audio)
    {
        var opt = new EncodeOptions
        {
            VideoCodec = CodecDe(codec),
            Quality = CalidadDe(calidad),
            MaxHeight = AlturaDe(resolucion),
            AudioBitrate = AudioDe(audio),
        };
        PonerFormato(opt, formato);
        return opt;
    }
}
