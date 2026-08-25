using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ondine.Localizacion;

namespace Ondine;

// ---------- modelos de ffprobe ----------
internal sealed class FfProbe
{
    [JsonPropertyName("streams")] public List<FfStream> Streams { get; set; } = new();
    [JsonPropertyName("format")] public FfFormat? Format { get; set; }
}
internal sealed class FfStream
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("codec_type")] public string CodecType { get; set; } = "";
    [JsonPropertyName("codec_name")] public string CodecName { get; set; } = "";
    [JsonPropertyName("width")] public int? Width { get; set; }
    [JsonPropertyName("height")] public int? Height { get; set; }
    [JsonPropertyName("bit_rate")] public string? BitRate { get; set; }
    [JsonPropertyName("r_frame_rate")] public string? RFrameRate { get; set; }
    [JsonPropertyName("channels")] public int? Channels { get; set; }
    [JsonPropertyName("tags")] public FfTags? Tags { get; set; }
    [JsonPropertyName("disposition")] public FfDisposition? Disposition { get; set; }
    public string Lang => Tags?.Language ?? "";
    /// <summary>El título puesto a mano en la pista («Castellano AMZN»): lo que mejor la identifica.</summary>
    public string Titulo => Tags?.Title ?? "";
}
internal sealed class FfTags
{
    [JsonPropertyName("language")] public string? Language { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
}
internal sealed class FfDisposition
{
    [JsonPropertyName("default")] public int Default { get; set; }
    [JsonPropertyName("forced")] public int Forced { get; set; }
}

/// <summary>
/// Contexto de serialización generado en compilación. Permite publicar el binario
/// recortado (PublishTrimmed) sin que el recortador se lleve por delante los tipos de
/// ffprobe, que sin esto solo se descubren por reflexión.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(FfProbe))]
internal partial class FfProbeJsonContext : JsonSerializerContext { }
internal sealed class FfFormat
{
    [JsonPropertyName("bit_rate")] public string? BitRate { get; set; }
    [JsonPropertyName("duration")] public string? Duration { get; set; }
    [JsonPropertyName("size")] public string? Size { get; set; }
}

/// <summary>Info resumida de pistas para la lista de la UI y la estimación.</summary>
public sealed class ProbeInfo
{
    public string Codec { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public int DurationSec { get; set; }
    public int VideoBitrateKbps { get; set; }
    public int AudioBitrateKbps { get; set; }
    public int Channels { get; set; }
    public string AudioCodec { get; set; } = "";
    public List<string> AudioLangs { get; set; } = new();
    public List<string> SubLangs { get; set; } = new();
}

/// <summary>Resultado de comprimir un archivo.</summary>
public sealed class FileResult
{
    public string Name { get; set; } = "";
    public long InBytes { get; set; }
    public long? OutBytes { get; set; }
    public string Status { get; set; } = "";
    public string SourcePath { get; set; } = "";   // ruta del original
    public string OutputPath { get; set; } = "";   // ruta del comprimido resultante
    /// <summary>Si se perdió algún subtítulo por el contenedor elegido, el motivo (para avisar en la UI).</summary>
    public string? SubtitleWarning { get; set; }

    /// <summary>
    /// Por qué falló, cuando falló. Lo que ffmpeg escribió, ya limpio.
    ///
    /// <para>
    /// Existe porque no existía: la fila decía «Error» y el registro un número, y la salida de
    /// error de ffmpeg —que dice el motivo— se tiraba. Aquí viaja hasta la tabla, que la enseña
    /// al pasar el ratón por encima del estado.
    /// </para>
    /// </summary>
    public string? Detalle { get; set; }
    public bool Ok => OutBytes is > 0;
}

/// <summary>Reporta el avance de la compresión a la UI.</summary>
public interface IEngineReporter
{
    void Log(string line);
    void FileStart(int index, int total, string name, double durationSec);
    void FileProgress(double fraction, string rawLine);   // 0..1 del archivo actual
    void FileDone(FileResult result);
    void DiskFull(bool paused) { }                        // disco lleno: en pausa esperando espacio

    /// <summary>
    /// Un archivo se salta y por qué. Antes esto solo iba al registro, así que la tabla
    /// no podía contar el motivo («ya está en HEVC», «ya hecho»…).
    /// </summary>
    void FileSkipped(string sourcePath, string reason) { }

}

/// <summary>
/// Motor de compresión: recodifica a HEVC/H.264/AV1 con aceleración por hardware,
/// conserva los idiomas de audio elegidos con el preferido por defecto, permite
/// pausar/reanudar y detener limpiamente, y nunca toca los originales.
/// </summary>
public sealed class Engine
{
    private static readonly string[] CoverCodecs = { "png", "mjpeg", "bmp", "gif" };
    // Lo que MP4 admite por copia NO se repite aqui: lo sabe Audio.CodecDeAudio, y era la
    // tercera copia de la misma lista -esta ya estaba muerta, y otra, la del estimador,
    // decia algo distinto que el motor-. Tres copias de una lista son tres verdades.
    // Subtítulos "de imagen": mapas de bits, no texto. MP4 no tiene dónde meterlos
    // (mov_text es texto), así que al pasar a MP4 hay que descartarlos a propósito.
    private static readonly string[] ImageSubs =
        { "hdmv_pgs_subtitle", "pgssub", "dvd_subtitle", "dvdsub", "dvb_subtitle", "dvbsub", "xsub" };
    // Nota: .ts (MPEG-TS) se omite a propósito: colisiona con TypeScript y llenaría
    // la lista de archivos de código en carpetas de desarrollo.
    public static readonly string[] VideoExtensions =
        { ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".webm", ".mpg", ".mpeg", ".flv" };

    private readonly Dictionary<string, string> _cachedEncoder = new();

    /// <summary>
    /// Lo que dio la sonda de aceleraciones, para no repetirla. De instancia y no estática: el
    /// hardware no cambia a mitad de sesión, pero un proceso distinto (la CLI, el servidor MCP)
    /// tiene que poder preguntarlo por su cuenta.
    /// </summary>
    private IReadOnlyList<string>? _aceleracionesProbadas;

    // ---------- localización de ffmpeg/ffprobe ----------
    private static string ResolveTool(string exe)
    {
        var baseDir = AppContext.BaseDirectory;
        foreach (var rel in new[] { $"{exe}.exe", $"ffmpeg\\{exe}.exe", $"ffmpeg\\bin\\{exe}.exe" })
        {
            var p = Path.Combine(baseDir, rel);
            if (File.Exists(p)) return p;
        }

        // En un Mac hay que ir a buscarla, y no es por gusto: una aplicación abierta desde el
        // Finder NO hereda el PATH del terminal -recibe uno mínimo, sin las carpetas de
        // Homebrew-. Fiarse del PATH ahí significa decir «ffmpeg no está instalado» a quien
        // acaba de instalarlo y lo ve funcionar en su terminal.
        if (OperatingSystem.IsMacOS() && HerramientaEnMac(exe, File.Exists) is { } enMac)
            return enMac;

        return exe; // en el PATH
    }

    /// <summary>
    /// La herramienta en las carpetas donde los gestores de paquetes de macOS la ponen, o
    /// <c>null</c> si no está en ninguna.
    ///
    /// <para>
    /// El orden es el de la arquitectura nativa primero. Un Mac con chip de Apple puede tener
    /// las dos —el Homebrew de Intel bajo Rosetta y el nativo—, y coger el de Intel haría que
    /// cada compresión pasara por la traducción: más lenta, y sin nada que lo dijera.
    /// </para>
    /// <para>
    /// Toma el «existe» como argumento para poder comprobar el orden y las rutas desde
    /// cualquier sistema, que es lo único de esto que no depende de tener un Mac delante.
    /// </para>
    /// </summary>
    internal static string? HerramientaEnMac(string exe, Func<string, bool> existe)
    {
        // Con barras a mano: son rutas de macOS, y Path.Combine usa el separador del sistema
        // que ejecuta -armar una ruta de Mac desde Windows saldría con barras invertidas-.
        string[] carpetas = ["/opt/homebrew/bin", "/usr/local/bin", "/opt/local/bin"];
        return carpetas.Select(c => c + "/" + exe).FirstOrDefault(existe);
    }
    private static string Ffmpeg => ResolveTool("ffmpeg");
    private static string Ffprobe => ResolveTool("ffprobe");

    /// <summary>Las mismas herramientas ya resueltas, para quien las invoque por su cuenta.</summary>
    public static string FfmpegPath => Ffmpeg;
    public static string FfprobePath => Ffprobe;

    /// <summary>
    /// El título grabado DENTRO del contenedor (la etiqueta «title» del MKV/MP4), o null.
    /// Existe para los ficheros sin título en el nombre: el metadato suele conservarlo.
    /// Barato a propósito: solo cabecera de formato, sin analizar pistas.
    /// </summary>
    public static async Task<string?> LeerTituloAsync(string path)
    {
        try
        {
            var (code, stdout, _) = await RunAsync(Ffprobe, new[]
            {
                "-v", "quiet", "-show_entries", "format_tags=title", "-of", "json", path,
            });
            if (code != 0) return null;

            using var doc = System.Text.Json.JsonDocument.Parse(stdout);
            if (doc.RootElement.TryGetProperty("format", out var f) &&
                f.TryGetProperty("tags", out var tags))
                foreach (var prop in tags.EnumerateObject())
                    if (prop.Name.Equals("title", StringComparison.OrdinalIgnoreCase))
                    {
                        var t = prop.Value.GetString();
                        return string.IsNullOrWhiteSpace(t) ? null : t.Trim();
                    }
            return null;
        }
        catch { return null; }   // sin ffprobe o fichero ilegible: simplemente no hay metadato
    }

    public static async Task<bool> ToolsAvailableAsync()
    {
        try
        {
            var (code, _, _) = await RunAsync(Ffmpeg, new[] { "-version" });
            return code == 0;
        }
        catch { return false; }
    }

    // ---------- elección de codificador ----------
    // Candidatos por códec: primero los de hardware, con fallback a software.
    // Candidatos por códec: hardware (se prueban en vivo) y software (se usa el primero que exista).
    private static (string[] hw, string[] sw) Candidates(string codec) => codec switch
    {
        "h264" => (new[] { "h264_qsv", "h264_nvenc", "h264_amf" }, new[] { "libx264" }),
        "av1" => (new[] { "av1_qsv", "av1_nvenc", "av1_amf" }, new[] { "libsvtav1", "libaom-av1" }),
        "vp9" => (Array.Empty<string>(), new[] { "libvpx-vp9" }),   // VP9 por software: fiable entre equipos
        _ => (new[] { "hevc_qsv", "hevc_nvenc", "hevc_amf" }, new[] { "libx265" }),
    };

    public async Task<string> SelectEncoderAsync(string codec = "hevc")
    {
        if (_cachedEncoder.TryGetValue(codec, out var cached)) return cached;
        var (hw, sw) = Candidates(codec);
        var (_, encList, _) = await RunAsync(Ffmpeg, new[] { "-hide_banner", "-encoders" });
        foreach (var cand in AllowHardware ? hw : Array.Empty<string>())
        {
            if (!encList.Contains(cand)) continue;
            var (code, _, _) = await RunAsync(Ffmpeg, new[]
            {
                "-hide_banner", "-loglevel", "error", "-f", "lavfi",
                "-i", "testsrc=size=640x480:duration=0.1", "-c:v", cand, "-f", "null", "-"
            });
            if (code == 0) return _cachedEncoder[codec] = cand;
        }
        // primer codificador software que realmente exista en esta build de FFmpeg
        foreach (var s in sw) if (encList.Contains(s)) return _cachedEncoder[codec] = s;
        return _cachedEncoder[codec] = sw[0];
    }

    public static bool IsHardware(string encoder) => !encoder.StartsWith("lib");

    /// <summary>
    /// ¿Este vídeo ya está bien comprimido y no merece la pena tocarlo? Misma regla que
    /// aplica CompressAsync al saltárselo; se expone para que la tabla pueda avisar al
    /// analizar, en vez de descubrirlo el usuario cuando ya ha lanzado la tanda.
    /// </summary>
    public static bool AlreadyCompressed(string codec, int totalKbps) =>
        (codec is "hevc" or "av1") && totalKbps > 0 && totalKbps < 2500;

    /// <summary>
    /// Un fotograma del segundo pedido. <paramref name="ancho"/> por defecto es el del
    /// globo de la barra; el visor de fotogramas lo pide grande porque llena la ventana.
    /// </summary>
    /// <returns>true si lo consiguió.</returns>
    public static async Task<bool> MakeThumbnailAsync(string video, string destJpg, int atSec, int ancho = 480)
    {
        var (code, _, _) = await RunAsync(Ffmpeg, new[]
        {
            "-v", "error", "-ss", $"{atSec}", "-i", video, "-frames:v", "1",
            "-vf", $"scale={ancho}:-2", "-q:v", "4", "-y", destJpg
        });
        return code == 0 && File.Exists(destJpg);
    }

    // ---------- previsualización de 10 s con los ajustes actuales ----------

    /// <summary>Args de codificación para la preview: mismo códec, pero preset lo MÁS rápido posible
    /// (es solo una vista de la calidad; no vale la pena esperar minutos con un encoder de software).</summary>
    private static List<string> PreviewEncoderArgs(string encoder, int quality) => encoder switch
    {
        "libx264" or "libx265" => new() { "-c:v", encoder, "-crf", $"{quality}", "-preset", "ultrafast" },
        "libsvtav1" => new() { "-c:v", "libsvtav1", "-crf", $"{quality}", "-preset", "12" },
        "libaom-av1" => new() { "-c:v", "libaom-av1", "-crf", $"{quality}", "-b:v", "0", "-cpu-used", "8", "-usage", "realtime" },
        "libvpx-vp9" => new() { "-c:v", "libvpx-vp9", "-crf", $"{quality}", "-b:v", "0", "-deadline", "realtime", "-cpu-used", "8", "-row-mt", "1" },
        _ => EncoderArgs(encoder, quality),   // hardware ya es rápido
    };

    /// <summary>
    /// Renderiza 10 s desde `startSec` con el códec/calidad/resolución elegidos (preset rápido),
    /// a un archivo temporal, para comprobar el resultado antes de comprimir. Devuelve la ruta o null.
    /// </summary>
    public async Task<string?> PreviewAsync(string input, EncodeOptions opt, int startSec, string dest, IEngineReporter rep, CancellationToken ct)
    {
        string vcodec = opt.Container == "webm" ? "vp9" : opt.VideoCodec;
        var encoder = await SelectEncoderAsync(vcodec);
        int quality = opt.Quality > 0 ? opt.Quality : (IsHardware(encoder) ? 27 : 23);
        var pr = await ProbeFullAsync(input);
        var video = pr?.Streams.FirstOrDefault(s => s.CodecType == "video" && !CoverCodecs.Contains(s.CodecName));
        var allAudio = pr?.Streams.Where(s => s.CodecType == "audio").ToList() ?? new();
        var pickAudio = allAudio.FirstOrDefault(s => s.Lang == opt.Lang) ?? allAudio.FirstOrDefault();  // idioma preferido

        var a = new List<string>
        {
            "-hide_banner", "-loglevel", "warning", "-stats", "-y",
            "-ss", startSec.ToString(), "-t", "10", "-i", input,
            "-map", "0:v:0",
        };
        a.AddRange(pickAudio != null ? new[] { "-map", $"0:{pickAudio.Index}" } : new[] { "-map", "0:a:0?" });
        if (opt.MaxHeight > 0 && (video?.Height ?? 0) > opt.MaxHeight)
            a.AddRange(new[] { "-vf", $"scale=-2:{opt.MaxHeight}" });
        a.AddRange(PreviewEncoderArgs(encoder, quality));
        int abr = opt.AudioBitrate > 0 ? opt.AudioBitrate : 192;
        a.AddRange(new[] { "-c:a", "aac", "-b:a", $"{abr}k", dest });

        var (code, _) = await RunFfmpegAsync(a, 10, rep, ct);
        return code == 0 && File.Exists(dest) ? dest : null;
    }

    /// <summary>
    /// Lo que se va a llevar el audio, para descontarlo del objetivo.
    ///
    /// <para>
    /// Recodificando se sabe exacto. COPIANDO no: habria que sumar el bitrate real de cada
    /// pista, y en ese caso se usa una cifra tipica por pista. Es una estimacion y se dice:
    /// quedarse corto aqui hace que el fichero se pase un poco del objetivo, que es
    /// preferible a recortar el video de mas por un audio que igual pesaba menos.
    /// </para>
    /// </summary>
    private static int AudioKbpsEstimado(EncodeOptions opt, int pistas)
    {
        var porPista = opt.AudioBitrate > 0 ? opt.AudioBitrate : 192;
        return porPista * Math.Max(1, pistas);
    }

    /// <summary>
    /// Los argumentos del codificador. Con bitrate objetivo puesto manda ESE y se olvida la
    /// calidad constante: las dos juntas no las obedece ffmpeg.
    /// </summary>
    private static List<string> EncoderArgs(
        string encoder, int quality, int bitrateKbps, Objetivo.Velocidad velocidad)
    {
        var args = bitrateKbps > 0
            ? new List<string>(Objetivo.ArgumentosDeBitrate.Para(encoder, bitrateKbps))
            : EncoderArgs(encoder, quality);

        // La velocidad SUSTITUYE al valor fijo que traia cada familia, no se anade: dos
        // «-preset» en la misma orden y ffmpeg se queda con uno, y no siempre el ultimo.
        var (bandera, valor) = (Objetivo.VelocidadDelCodificador.Para(encoder, velocidad) is var v2)
            ? (v2[0], v2[1]) : ("", "");

        var i = args.IndexOf(bandera);
        if (i >= 0 && i + 1 < args.Count) args[i + 1] = valor;
        else { args.Add(bandera); args.Add(valor); }

        return args;
    }

    private static List<string> EncoderArgs(string encoder, int quality) => encoder switch
    {
        "hevc_qsv" or "h264_qsv" or "av1_qsv" =>
            new() { "-c:v", encoder, "-global_quality", $"{quality}", "-preset", "slow" },
        "hevc_nvenc" or "h264_nvenc" or "av1_nvenc" =>
            new() { "-c:v", encoder, "-rc", "vbr", "-cq", $"{quality}", "-preset", "p6", "-tune", "hq" },
        "hevc_amf" or "h264_amf" or "av1_amf" =>
            new() { "-c:v", encoder, "-rc", "cqp", "-qp_i", $"{quality}", "-qp_p", $"{quality}", "-quality", "quality" },
        "vp9_qsv" => new() { "-c:v", "vp9_qsv", "-global_quality", $"{quality}", "-preset", "slow" },
        "libsvtav1" => new() { "-c:v", "libsvtav1", "-crf", $"{quality}", "-preset", "6" },
        "libvpx-vp9" => new() { "-c:v", "libvpx-vp9", "-crf", $"{quality}", "-b:v", "0", "-row-mt", "1" },
        "libaom-av1" => new() { "-c:v", "libaom-av1", "-crf", $"{quality}", "-b:v", "0", "-cpu-used", "6", "-row-mt", "1" },
        _ => new() { "-c:v", encoder, "-crf", $"{quality}", "-preset", "medium" },   // libx264 / libx265
    };

    private static string AudioExt(string fmt) => fmt switch
    {
        "m4a" => ".m4a", "flac" => ".flac", "opus" => ".opus", _ => ".mp3",
    };

    /// <summary>Extensión del archivo de salida según el formato elegido (la usa también la vista previa de renombrado).</summary>
    public static string OutputExtension(EncodeOptions opt) => opt.AudioOnly ? AudioExt(opt.AudioFormat)
        : opt.Container == "mp4" ? ".mp4"
        : opt.Container == "webm" ? ".webm" : ".mkv";
    private static List<string> AudioOnlyArgs(EncodeOptions opt)
    {
        int br = opt.AudioBitrate > 0 ? opt.AudioBitrate : 192;
        return opt.AudioFormat switch
        {
            "flac" => new() { "-c:a", "flac" },
            "opus" => new() { "-c:a", "libopus", "-b:a", $"{br}k" },
            "m4a" => new() { "-c:a", "aac", "-b:a", $"{br}k" },
            _ => new() { "-c:a", "libmp3lame", "-b:a", $"{br}k" },   // mp3
        };
    }

    // ---------- análisis de pistas (para el scan de la UI) ----------
    /// <summary>
    /// Todas las pistas del fichero, con su índice absoluto, para poder quitar alguna sin
    /// recomprimir (ver <see cref="SelectorDePistas"/>). Las portadas incrustadas se dejan fuera:
    /// son imágenes que ffprobe declara como vídeo y no son una pista que nadie quiera tocar.
    /// </summary>
    public async Task<(IReadOnlyList<Pista> Pistas, int DuracionSeg)> PistasDeAsync(string path)
    {
        var pr = await ProbeFullAsync(path);
        if (pr == null) return (Array.Empty<Pista>(), 0);

        int dur = double.TryParse(pr.Format?.Duration, System.Globalization.CultureInfo.InvariantCulture,
                                  out var d) ? (int)d : 0;
        var lista = new List<Pista>();
        foreach (var s in pr.Streams)
        {
            if (s.CodecType == "video" && CoverCodecs.Contains(s.CodecName)) continue;
            var tipo = s.CodecType switch
            {
                "video" => TipoPista.Video,
                "audio" => TipoPista.Audio,
                "subtitle" => TipoPista.Subtitulo,
                _ => TipoPista.Otro,
            };
            long? bps = long.TryParse(s.BitRate, out var b) ? b : null;
            lista.Add(new Pista(s.Index, tipo, s.CodecName, s.Lang, s.Channels, bps)
            {
                Titulo = s.Titulo,
                EsPredeterminada = s.Disposition?.Default == 1,
                EsForzada = s.Disposition?.Forced == 1,
            });
        }
        return (lista, dur);
    }

    /// <summary>
    /// Reempaqueta el fichero dejando fuera las pistas marcadas, SIN recodificar («-c copy»).
    /// Escribe a un temporal y solo al final sustituye al original: si algo falla a medias, el
    /// fichero de partida sigue intacto.
    /// </summary>
    public async Task<(bool Ok, string Mensaje, long BytesAntes, long BytesDespues)> QuitarPistasAsync(
        string path, SelectorDePistas.Plan plan, CancellationToken ct = default)
    {
        if (!plan.HayCambios) return (false, Textos.Instancia.MotorPistasSinMarcar, 0, 0);
        if (!File.Exists(path)) return (false, Textos.Instancia.MotorPistasFicheroDesaparecido, 0, 0);

        long antes = new FileInfo(path).Length;
        var tmp = Path.Combine(Path.GetDirectoryName(path)!,
            $"~pistas_{Guid.NewGuid():N}{Path.GetExtension(path)}");
        try
        {
            var (code, _, err) = await RunAsync(Ffmpeg, plan.Argumentos(path, tmp).ToArray());
            if (code != 0 || !File.Exists(tmp) || new FileInfo(tmp).Length == 0)
            {
                try { File.Delete(tmp); } catch { }
                var razon = err.Split('\n').LastOrDefault(l => l.Trim().Length > 0)?.Trim();
                return (false, string.IsNullOrEmpty(razon) ? Textos.Instancia.MotorPistasRemuxFallido : razon, antes, 0);
            }

            long despues = new FileInfo(tmp).Length;

            // El original se manda a la papelera propia en vez de borrarlo: si el remux salió
            // mal de una forma que no detectamos, con Ctrl+Z se recupera.
            //
            // Y SE COMPRUEBA QUE SE HAYA IDO. Esto tiraba el resultado de Enviar y a la línea
            // siguiente sobrescribía el original. Enviar devuelve null cuando no puede -y puede
            // no poder a menudo: mueve el fichero a la carpeta de datos de la app, que casi
            // nunca está en el mismo disco que la biblioteca, así que es una copia entera que
            // falla si no hay sitio-. En ese caso se sobrescribía el original sin red, y el
            // Ctrl+Z que la propia línea de arriba promete no tenía nada que deshacer.
            //
            // Es el único de los llamantes de Enviar que no miraba la respuesta.
            if (Reindex.PapeleraApp.Enviar(path) is null)
            {
                try { File.Delete(tmp); } catch { }
                return (false, Textos.Instancia.MotorPistasSinRed, antes, 0);
            }

            File.Move(tmp, path, overwrite: true);
            return (true, "", antes, despues);
        }
        catch (Exception ex)
        {
            try { File.Delete(tmp); } catch { }
            return (false, ex.Message, antes, 0);
        }
    }

    public async Task<ProbeInfo> ProbeAsync(string path)
    {
        var pr = await ProbeFullAsync(path);
        var info = new ProbeInfo();
        if (pr == null) return info;

        var vid = pr.Streams.FirstOrDefault(s => s.CodecType == "video" && !CoverCodecs.Contains(s.CodecName));
        info.Codec = vid?.CodecName ?? "";
        info.Width = vid?.Width ?? 0;
        info.Height = vid?.Height ?? 0;
        info.Fps = ParseFps(vid?.RFrameRate);

        double durSec = double.TryParse(pr.Format?.Duration, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
        info.DurationSec = (int)durSec;

        var firstAudio = pr.Streams.FirstOrDefault(s => s.CodecType == "audio");
        info.Channels = firstAudio?.Channels ?? 2;
        info.AudioCodec = firstAudio?.CodecName ?? "";
        int audioKbps = ParseKbps(firstAudio?.BitRate);
        int vidKbps = ParseKbps(vid?.BitRate);

        // MKV rara vez expone bitrate por pista: derivarlo del total del contenedor.
        int overallKbps = ParseKbps(pr.Format?.BitRate);
        if (overallKbps == 0 && long.TryParse(pr.Format?.Size, out var sz) && durSec > 0)
            overallKbps = (int)(sz * 8 / durSec / 1000);
        if (audioKbps == 0) audioKbps = info.Channels >= 6 ? 448 : 192;
        if (vidKbps == 0 && overallKbps > 0)
            vidKbps = Math.Max(overallKbps - audioKbps, (int)(overallKbps * 0.85));

        info.VideoBitrateKbps = vidKbps;
        info.AudioBitrateKbps = audioKbps;

        info.AudioLangs = pr.Streams.Where(s => s.CodecType == "audio")
            .Select(s => string.IsNullOrEmpty(s.Lang) ? "?" : s.Lang).Distinct().ToList();
        info.SubLangs = pr.Streams.Where(s => s.CodecType == "subtitle")
            .Select(s => string.IsNullOrEmpty(s.Lang) ? "?" : s.Lang).Distinct().ToList();
        return info;
    }

    private static double ParseFps(string? r)
    {
        if (string.IsNullOrEmpty(r)) return 0;
        var parts = r.Split('/');
        if (parts.Length == 2 && double.TryParse(parts[0], out var n)
            && double.TryParse(parts[1], out var den) && den > 0) return n / den;
        return double.TryParse(r, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0;
    }
    private static int ParseKbps(string? bitrate) => int.TryParse(bitrate, out var b) && b > 0 ? b / 1000 : 0;

    private static async Task<FfProbe?> ProbeFullAsync(string path)
    {
        var (code, stdout, _) = await RunAsync(Ffprobe, new[]
        {
            "-v", "error",
            // El «title» y el «disposition» son para poder distinguir las pistas al quitarlas:
            // «spa» a secas no dice si es el doblaje bueno, y en subtítulos importa saber si son
            // los forzados o los completos.
            "-show_entries", "stream=index,codec_type,codec_name,width,height,bit_rate,r_frame_rate,channels,disposition:stream_tags=language,title:format=bit_rate,duration,size",
            "-of", "json", "--", path
        });
        if (code != 0 || string.IsNullOrWhiteSpace(stdout)) return null;
        try
        {
            // Se usa el contexto generado en compilación (ver FfProbeJsonContext): con la
            // versión por reflexión, al recortar el binario se perdían los tipos y ffprobe
            // devolvía datos vacíos (duración 0, resolución 0x0).
            return JsonSerializer.Deserialize(stdout, FfProbeJsonContext.Default.FfProbe);
        }
        catch { return null; }
    }

    // ---------- ¿aún descargando? ----------
    private static bool StillDownloading(string path)
    {
        foreach (var ext in new[] { ".part", ".crdownload", ".!ut", ".downloading", ".tmp", "!qB" })
            if (File.Exists(path + ext)) return true;
        // FileShare.Read, NO FileShare.None: lo que delata una descarga a medias es que
        // alguien lo tenga abierto para ESCRIBIR. La apertura exclusiva también fallaba con
        // cualquier LECTOR — OneDrive hidratando, el indexador, o el propio reproductor de
        // Recortes, que suelta el fichero con retraso — y en Recortes eso montaba un bucle:
        // salto por «descargando», el finally reabría el vídeo, y el siguiente intento
        // volvía a encontrarlo cogido. Solo se salía reiniciando la app.
        try { using var s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read); }
        catch { return true; }   // retenido por un ESCRITOR: eso sí es una descarga en curso
        return false;
    }

    /// <summary>
    /// Las aceleraciones de DECODIFICACIÓN que arrancan de verdad en esta máquina.
    ///
    /// <para>
    /// Se prueban una a una porque la lista de <c>ffmpeg -hwaccels</c> no sirve de respuesta: en
    /// la máquina donde se escribió esto ofrecía siete y solo tres funcionaban. Pedir «cuda» sin
    /// NVIDIA no cae a software, se muere con código 127 («Cannot load nvcuda.dll»), y lo mismo
    /// «vaapi» sin libva. Es el mismo motivo por el que <see cref="SelectEncoderAsync"/> prueba
    /// los codificadores en vivo en vez de creerse la lista.
    /// </para>
    /// <para>
    /// La sonda necesita un fichero COMPRIMIDO —lo que se prueba es un decodificador—, así que se
    /// fabrica uno de diez kilobytes con <c>mpeg4</c>, que va dentro de ffmpeg y no depende de
    /// ninguna biblioteca externa. La pasada entera cuesta menos de dos segundos y se recuerda
    /// para toda la sesión.
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<string>> AceleracionesDisponiblesAsync()
    {
        if (_aceleracionesProbadas is not null) return _aceleracionesProbadas;

        var (_, salida, err) = await RunAsync(Ffmpeg, new[] { "-hide_banner", "-hwaccels" });
        var candidatas = Objetivo.AceleracionDeVideo.Candidatas(
            salida + "\n" + err,                      // según la build, la lista sale por una o por otra
            OperatingSystem.IsWindows(), OperatingSystem.IsMacOS());

        if (candidatas.Count == 0) return _aceleracionesProbadas = Array.Empty<string>();

        var sonda = Path.Combine(Path.GetTempPath(), $"ondine-sonda-{Guid.NewGuid():N}.avi");
        try
        {
            var (codigoSonda, _, _) = await RunAsync(Ffmpeg, new[]
            {
                "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi",
                "-i", "testsrc=size=128x128:duration=0.2:rate=10", "-c:v", "mpeg4", sonda,
            });
            // Sin ficherito no hay sonda, y sin sonda no se arriesga: decodificar en la CPU
            // funciona siempre, y un 127 por cada fichero de la tanda no.
            if (codigoSonda != 0 || !File.Exists(sonda)) return _aceleracionesProbadas = Array.Empty<string>();

            var funcionan = new List<string>();
            foreach (var a in candidatas)
            {
                var (codigo, _, _) = await RunAsync(Ffmpeg,
                    Objetivo.AceleracionDeVideo.ArgumentosDeSonda(a, sonda).ToArray());
                if (codigo == 0) funcionan.Add(a);
            }
            return _aceleracionesProbadas = funcionan;
        }
        finally { try { if (File.Exists(sonda)) File.Delete(sonda); } catch { } }
    }

    // ---------- compresión ----------
    public async Task<List<FileResult>> CompressAsync(
        IReadOnlyList<string> files, EncodeOptions opt, IEngineReporter rep, CancellationToken ct)
    {
        var results = new List<FileResult>();
        string vcodec = opt.Container == "webm" ? "vp9" : opt.VideoCodec;   // WebM: VP9 (más compatible entre builds de FFmpeg)
        var encoder = await SelectEncoderAsync(vcodec);
        int quality = opt.Quality > 0 ? opt.Quality : (IsHardware(encoder) ? 27 : 23);
        var encArgs = EncoderArgs(encoder, quality, opt.BitrateVideoKbps, opt.Velocidad);
        var t = Textos.Instancia;
        if (opt.AudioOnly) rep.Log(string.Format(t.MotorSoloAudioModo, opt.AudioFormat.ToUpperInvariant()));
        else
        {
            rep.Log(string.Format(t.MotorCodificador, encoder,
                IsHardware(encoder) ? t.MotorCodificadorHardware : t.MotorCodificadorSoftware, quality));
            if (encoder is "libaom-av1")
                rep.Log(t.MotorAvisoAv1Lento);
        }

        // La aceleración de la DECODIFICACIÓN, una vez por tanda. Y se dice, porque hasta ahora
        // el registro contaba el codificador y callaba que el original lo descomprimía la CPU:
        // desde fuera parecía que todo iba por la tarjeta.
        var aceleracion = opt.AudioOnly ? null : Objetivo.AceleracionDeVideo.Elegida(
            AceleracionPedida, await AceleracionesDisponiblesAsync(), encoder);
        bool aceleracionCaida = false;   // si falla una vez, no se reintenta con el resto de la tanda
        if (!opt.AudioOnly)
            rep.Log(aceleracion is null ? t.MotorDecodificaCpu
                                        : string.Format(t.MotorDecodificaGpu, aceleracion));

        var keepLangs = opt.KeepLangs.Count > 0 ? opt.KeepLangs : new List<string> { opt.Lang, "eng" };
        bool keepAll = keepLangs.Contains("all");
        bool subsAll = opt.SubLangs == null || opt.SubLangs.Count == 0 || opt.SubLangs.Contains("all");

        int total = files.Count, n = 0;
        int renamedCount = 0;                                              // contador de la regla de renombrado
        var usedOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();
            n++;
            var fi = new FileInfo(f);
            string name = fi.Name;
            string outDir = opt.Output ?? Path.Combine(fi.DirectoryName!, "comprimido");
            string ext = OutputExtension(opt);

            // nombre de salida, con la regla de renombrado (estilo PowerRename) si la hay
            string outName = (opt.NombreSalida is { Length: > 0 } propio
                ? Reindex.LibraryTemplate.LimpiarNombre(propio)
                : Path.GetFileNameWithoutExtension(name)) + ext;
            string? renamedTo = null;
            if (opt.NameRule is { } rule && rule.HasEffect)
            {
                DateTime created;
                try { created = fi.CreationTime; } catch { created = DateTime.Now; }
                if (rule.Apply(outName, renamedCount, created) is { } nuevo)
                {
                    outName = renamedTo = nuevo;
                    renamedCount++;
                }
            }
            string outPath = UniqueOutput(Path.Combine(outDir, outName), usedOutputs);

            if (File.Exists(outPath) && !opt.Force) { rep.Log(string.Format(t.MotorSaltoYaHecho, n, total, name)); rep.FileSkipped(f, t.MotorMotivoYaHecho); continue; }
            if (StillDownloading(f)) { rep.Log(string.Format(t.MotorSaltoDescargando, n, total, name)); rep.FileSkipped(f, t.MotorMotivoDescargando); continue; }

            var pr = await ProbeFullAsync(f);
            if (pr == null) { rep.Log(string.Format(t.MotorSaltoIlegible, n, total, name)); rep.FileSkipped(f, t.MotorMotivoIlegible); continue; }

            var video = pr.Streams.FirstOrDefault(s => s.CodecType == "video" && !CoverCodecs.Contains(s.CodecName));
            if (video == null) { rep.Log(string.Format(t.MotorSaltoSinVideo, n, total, name)); rep.FileSkipped(f, t.MotorMotivoSinVideo); continue; }

            int kbps = int.TryParse(pr.Format?.BitRate, out var br) ? br / 1000 : 0;
            if (!opt.AudioOnly && !opt.Force && (video.CodecName is "hevc" or "av1") && kbps > 0 && kbps < 2500)
            { rep.Log(string.Format(t.MotorSaltoYaComprimido, n, total, name, video.CodecName, kbps)); rep.FileSkipped(f, string.Format(t.MotorMotivoYaEn, video.CodecName!.ToUpperInvariant())); continue; }

            var allAudio = pr.Streams.Where(s => s.CodecType == "audio").ToList();
            if (allAudio.Count == 0) { rep.Log(string.Format(t.MotorSaltoSinAudio, n, total, name)); rep.FileSkipped(f, t.MotorMotivoSinAudio); continue; }
            var pref = allAudio.Where(s => s.Lang == opt.Lang).ToList();
            var other = allAudio.Where(s => s.Lang != opt.Lang && (keepAll || keepLangs.Contains(s.Lang))).ToList();
            var audio = pref.Concat(other).ToList();
            if (audio.Count == 0) audio = allAudio;   // ningún idioma coincide: conservar todo

            var subs = opt.NoSubs ? new List<FfStream>()
                : pr.Streams.Where(s => s.CodecType == "subtitle" && (subsAll || (opt.SubLangs?.Contains(s.Lang) ?? true))).ToList();

            // Qué subtítulos caben de verdad en el contenedor elegido:
            //   · MKV admite texto e imagen, y se copian tal cual.
            //   · MP4 solo admite texto (convertido a mov_text); los de imagen
            //     (PGS, VobSub, DVB…) no tienen equivalente y se descartan aquí,
            //     a propósito, en vez de hacer fallar toda la codificación.
            //   · WebM no lleva subtítulos en esta versión.
            bool webmOut = opt.Container == "webm";
            bool mp4Out = opt.Container == "mp4";
            var keptSubs = webmOut ? new List<FfStream>()
                         : mp4Out ? subs.Where(s => IsTextSubtitle(s.CodecName)).ToList()
                         : subs;
            var lostSubs = subs.Where(s => !keptSubs.Contains(s)).ToList();

            double durSec = double.TryParse(pr.Format?.Duration, System.Globalization.CultureInfo.InvariantCulture, out var dd) ? dd : 0;
            // Con un tramo, lo que se codifica es SU duración: si no, la barra de progreso
            // mediría contra el vídeo entero y se quedaría clavada al 10 %.
            if (opt.Duracion is > 0) durSec = Math.Min(opt.Duracion.Value, durSec > 0 ? durSec : opt.Duracion.Value);

            // ---- modo solo audio: extraer sin vídeo ----
            if (opt.AudioOnly)
            {
                if (opt.DryRun) { rep.Log(string.Format(t.MotorSecoSoloAudio, n, total, name, opt.AudioFormat)); continue; }
                rep.Log($"[{n}/{total}] {name}");
                rep.Log(string.Format(t.MotorExtrayendoAudio, audio.Count, opt.AudioFormat));
                if (renamedTo != null) rep.Log(string.Format(t.MotorRenombrado, Path.GetFileName(outPath)));
                rep.FileStart(n, total, name, durSec);
                Directory.CreateDirectory(outDir);
                string atmp = outPath + ".tmp" + ext;
                try { if (File.Exists(atmp)) File.Delete(atmp); } catch { }
                var aargs = new List<string> { "-hide_banner", "-loglevel", "warning", "-stats", "-y", "-i", f, "-vn" };
                foreach (var au in audio) aargs.AddRange(new[] { "-map", $"0:{au.Index}" });
                aargs.AddRange(AudioOnlyArgs(opt));
                aargs.Add(atmp);
                try
                {
                    var (acode, _) = await RunFfmpegAsync(aargs, durSec, rep, ct);
                    if (acode == 0 && File.Exists(atmp))
                    {
                        try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                        File.Move(atmp, outPath);
                        long ob = new FileInfo(outPath).Length;
                        rep.Log(string.Format(t.MotorAudioListo, $"{ob / 1048576.0:n1}"));
                        var ar = new FileResult { Name = name, InBytes = fi.Length, OutBytes = ob, Status = t.MotorEstadoSoloAudio, SourcePath = f, OutputPath = outPath };
                        results.Add(ar); rep.FileDone(ar);
                    }
                    else { try { if (File.Exists(atmp)) File.Delete(atmp); } catch { } rep.Log(string.Format(t.MotorErrorExtraerAudio, acode)); }
                }
                catch (OperationCanceledException) { try { if (File.Exists(atmp)) File.Delete(atmp); } catch { } rep.Log(t.MotorDetenido); throw; }
                continue;
            }

            // ---- construir argumentos ----
            List<string> BuildArgs(bool withSubs)
            {
                var (ssAntes, tDespues) = Reindex.Tramos.ArgsFfmpeg(opt.Desde, opt.Duracion);
                var a = new List<string> { "-hide_banner", "-loglevel", "warning", "-stats", "-y" };

                // Aceleración de la decodificación: es una opción de ENTRADA, así que va antes
                // del -ss y del -i. Sin -hwaccel_output_format a propósito: los fotogramas bajan
                // a memoria de sistema para que el «scale» de CPU de más abajo pueda con ellos.
                // Con él, la orden moriría al montar el grafo de filtros.
                if (!aceleracionCaida)
                    a.AddRange(Objetivo.AceleracionDeVideo.Argumentos(aceleracion));
                a.AddRange(ssAntes);        // el salto, ANTES de la entrada: busca por índice
                a.AddRange(new[] { "-i", f });
                a.AddRange(tDespues);
                bool webm = opt.Container == "webm";
                a.AddRange(new[] { "-map", $"0:{video.Index}" });
                foreach (var au in audio) a.AddRange(new[] { "-map", $"0:{au.Index}" });
                if (withSubs) foreach (var s in keptSubs) a.AddRange(new[] { "-map", $"0:{s.Index}" });
                if (opt.MaxHeight > 0 && (video.Height ?? 0) > opt.MaxHeight)
                    a.AddRange(new[] { "-vf", $"scale=-2:{opt.MaxHeight}" });
                // Con tamano objetivo, el bitrate se calcula AQUI y no fuera del bucle:
                // depende de la duracion, y cada fichero dura lo suyo. Fuera saldria el
                // mismo bitrate para un corto y para una pelicula.
                a.AddRange(opt.TamanoObjetivoBytes > 0
                    ? EncoderArgs(encoder, quality,
                        Objetivo.TamanoObjetivo.Calcular(
                            opt.TamanoObjetivoBytes, durSec,
                            AudioKbpsEstimado(opt, audio.Count), kbps).VideoKbps, opt.Velocidad)
                    : encArgs);
                bool mp4 = opt.Container == "mp4";
                // Que hacer con cada pista lo decide Audio.CodecDeAudio, que sabe que no
                // todo cabe en todo y DICE cuando ha tenido que cambiar lo pedido. Antes
                // esto era una cadena de condiciones aqui dentro que decidia en silencio.
                for (int i = 0; i < audio.Count; i++)
                {
                    // TODA la decision vive en el nucleo, en PlanDeAudio: copiar o
                    // recodificar, con cuantos canales y a que caudal. Antes estaba aqui
                    // dentro, en una funcion local del bucle de ficheros, y desde ahi no se
                    // podia probar — la regla mas delicada del motor no tenia ni una prueba,
                    // y el fallo que la tenia rota (un «Sin tocar» que salia en AAC 128)
                    // convivio con seis suites de argumentos en verde.
                    var plan = Audio.PlanDeAudio.Para(
                        opt.Container, opt.AudioCodec, opt.AudioMezcla, opt.AudioBitrate,
                        audio[i].CodecName ?? "", audio[i].Channels ?? 2, i);

                    // Se avisa UNA vez por fichero, no por pista: con seis idiomas dentro,
                    // seis lineas iguales en el registro tapan todo lo demas.
                    if (i == 0 && plan.Codec.Porque != Audio.PorQueSeCambio.SeHizoLoPedido)
                        rep.Log(string.Format(t.MotorAudioCambiado, plan.Codec.Pedido, plan.Codec.Codec, opt.Container));
                    if (i == 0 && plan.Mezcla.HayQueMezclar)
                        rep.Log(string.Format(t.MotorAudioMezclado, audio[i].Channels ?? 2));
                    // Y se dice tambien cuando el caudal elegido no se usa porque se esta
                    // copiando. Un ajuste que no hace nada y no avisa es la misma clase de
                    // silencio que el atajo que esto vino a quitar, en la otra direccion.
                    if (i == 0 && plan.CaudalIgnorado)
                        rep.Log(string.Format(t.MotorAudioCaudalSinUsar, opt.AudioBitrate));

                    a.AddRange(plan.Argumentos);
                }
                if (withSubs && keptSubs.Count > 0) a.AddRange(new[] { "-c:s", mp4 ? "mov_text" : "copy" });
                a.AddRange(new[] { "-disposition:a:0", "default" });
                for (int i = 1; i < audio.Count; i++) a.AddRange(new[] { $"-disposition:a:{i}", "0" });
                a.AddRange(new[] { "-map_metadata", "0" });
                return a;
            }

            string langs = string.Join("+", audio.Select(x => string.IsNullOrEmpty(x.Lang) ? "?" : x.Lang));
            int dropped = allAudio.Count - audio.Count;
            string infoLine = string.Format(t.MotorInfoAudio, langs)
                + (dropped > 0 ? string.Format(t.MotorInfoDescartadas, dropped) : "")
                + (keptSubs.Count > 0 ? string.Format(t.MotorInfoSubtitulos, keptSubs.Count) : "")
                + (opt.MaxHeight > 0 && (video.Height ?? 0) > opt.MaxHeight ? string.Format(t.MotorInfoReescalado, opt.MaxHeight) : "");

            // Aviso de subtítulos que este contenedor no puede llevar. No basta con el
            // registro: el usuario los marcó en la UI y da por hecho que van dentro.
            var lostSoFar = new List<FfStream>(lostSubs);
            if (lostSubs.Count > 0)
                rep.Log(string.Format(t.MotorAvisoPrefijo, SubtitleLossMessage(lostSubs, opt.Container)));

            if (opt.DryRun) { rep.Log($"[{n}/{total}] {name} → {infoLine}"); continue; }

            rep.Log($"[{n}/{total}] {name}");
            rep.Log($"    {infoLine}");
            if (renamedTo != null) rep.Log(string.Format(t.MotorRenombrado, Path.GetFileName(outPath)));
            rep.FileStart(n, total, name, durSec);

            Directory.CreateDirectory(outDir);

            // ANTES DE TOCAR NADA: ¿se puede escribir ahí? Si no, no tiene sentido analizar
            // pistas, decidir codificador y lanzar ffmpeg para que falle — y menos hacerlo una
            // vez por fichero. Doce capítulos dieron doce veces el mismo «Permission denied»,
            // cada uno después de su espera.
            if (SePuedeEscribirEn(outDir) is { } porQueNo)
            {
                rep.Log(string.Format(Textos.Instancia.MotorNoSePuedeEscribir, outDir, porQueNo));
                break;
            }

            await WaitForSpaceAsync(outDir, MinFreeBytes, rep, ct);   // no empezar si el disco ya está lleno
            // El temporal DEBE llevar la extensión del contenedor elegido: ffmpeg escoge
            // el muxer por la extensión, así que un ".tmp.mkv" producía un Matroska aunque
            // luego se renombrara a .mp4 (y con él, mov_text fallaba siempre).
            string tmp = outPath + ".tmp" + ext;
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }

            try
            {
                int code; string err;
                while (true)
                {
                    (code, err) = await RunFfmpegAsync(BuildArgs(true).Append(tmp).ToList(), durSec, rep, ct);

                    // PRIMERO la aceleración, y el orden importa. Si este brazo fuera después
                    // del de los subtítulos, un fallo de la GPU se reintentaría sin subtítulos y,
                    // al salir bien, se apuntarían como perdidos unos subtítulos que sí están
                    // dentro: el aviso acabaría mintiendo.
                    if (code != 0 && aceleracion is not null && !aceleracionCaida
                        && Objetivo.AceleracionDeVideo.EsFalloDeAceleracion(err)
                        && !ct.IsCancellationRequested)
                    {
                        // Y no se reintenta con el resto de la tanda: si la tarjeta no está,
                        // no va a estar en el fichero siguiente, y son doce capítulos.
                        aceleracionCaida = true;
                        rep.Log(string.Format(t.MotorAceleracionCaida, aceleracion));
                        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                        (code, err) = await RunFfmpegAsync(BuildArgs(true).Append(tmp).ToList(), durSec, rep, ct);
                    }

                    // red de seguridad: si aun así el subtítulo no entra (formato raro que
                    // no supimos clasificar), sacar el vídeo sin subtítulos antes que nada.
                    if (code != 0 && !IsDiskFull(err) && keptSubs.Count > 0 && !ct.IsCancellationRequested)
                    {
                        rep.Log(t.MotorReintentoSinSubtitulos);
                        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                        (code, err) = await RunFfmpegAsync(BuildArgs(false).Append(tmp).ToList(), durSec, rep, ct);
                        if (code == 0) lostSoFar.AddRange(keptSubs);
                    }

                    if (code != 0 && IsDiskFull(err))
                    {
                        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }   // liberar el temporal a medias
                        await WaitForSpaceAsync(outDir, MinFreeBytes, rep, ct);      // pausa hasta que haya espacio
                        continue;                                                   // reintentar el MISMO archivo (la cola se mantiene)
                    }
                    break;
                }

                if (code == 0 && File.Exists(tmp))
                {
                    try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
                    File.Move(tmp, outPath);
                    long inB = fi.Length, outB = new FileInfo(outPath).Length;
                    int pct = (int)Math.Round(100 - (outB / (double)Math.Max(inB, 1) * 100));
                    rep.Log(string.Format(t.MotorVideoListo, inB / 1048576, outB / 1048576, pct));
                    var r = new FileResult
                    {
                        Name = name, InBytes = inB, OutBytes = outB, Status = $"-{pct}%",
                        SourcePath = f, OutputPath = outPath,
                        SubtitleWarning = lostSoFar.Count > 0 ? SubtitleLossMessage(lostSoFar, opt.Container) : null,
                    };
                    results.Add(r); rep.FileDone(r);
                }
                else
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                    // EL MOTIVO, no el número. «err» ya estaba aquí -se usa dos líneas arriba
                    // para detectar el disco lleno- y en cualquier otro fallo se tiraba: doce
                    // capítulos fallaron con doce «código 243» y ninguna pista de que era un
                    // permiso de escritura.
                    var motivo = MotivoDeFfmpeg.De(code, err);
                    rep.Log(string.Format(t.MotorErrorCodificar, motivo));
                    var r = new FileResult { Name = name, InBytes = fi.Length, OutBytes = null, Status = t.Error, SourcePath = f, OutputPath = outPath, Detalle = motivo };
                    results.Add(r); rep.FileDone(r);
                }
            }
            catch (OperationCanceledException)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }   // al detener no dejamos el temporal a medias
                rep.Log(t.MotorDetenidoConTemporal);
                throw;
            }
        }
        return results;
    }

    /// <summary>
    /// Mide el bitrate de vídeo REAL codificando varias muestras cortas repartidas por el
    /// vídeo con los ajustes elegidos. Es la única forma fiable de anticipar el tamaño:
    /// CRF fija la calidad, no el tamaño, así que el peso depende del contenido y ninguna
    /// fórmula lo adivina. Devuelve kbps (0 si no se pudo medir).
    /// </summary>
    public async Task<int> MeasureVideoBitrateAsync(string input, EncodeOptions opt, IEngineReporter rep,
                                                    CancellationToken ct, int samples = 3, int secondsEach = 8)
    {
        var pr = await ProbeFullAsync(input);
        var video = pr?.Streams.FirstOrDefault(s => s.CodecType == "video" && !CoverCodecs.Contains(s.CodecName));
        if (pr == null || video == null) return 0;
        double dur = double.TryParse(pr.Format?.Duration, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
        if (dur < 4) return 0;

        string vcodec = opt.Container == "webm" ? "vp9" : opt.VideoCodec;
        var encoder = await SelectEncoderAsync(vcodec);
        int quality = opt.Quality > 0 ? opt.Quality : (IsHardware(encoder) ? 27 : 23);
        var encArgs = EncoderArgs(encoder, quality, opt.BitrateVideoKbps, opt.Velocidad);

        // repartimos las muestras por el 90% central (evita cabecera y créditos)
        samples = Math.Max(1, samples);
        double start0 = dur * 0.05, usable = dur * 0.90;
        if (usable < (double)samples * secondsEach) secondsEach = Math.Max(2, (int)(usable / samples));

        string dir = Path.Combine(Path.GetTempPath(), "shrinkvideo_measure");
        Directory.CreateDirectory(dir);
        long totalBytes = 0; double totalSecs = 0;
        try
        {
            for (int i = 0; i < samples; i++)
            {
                ct.ThrowIfCancellationRequested();
                double at = start0 + usable * (i + 0.5) / samples - secondsEach / 2.0;
                at = Math.Clamp(at, 0, Math.Max(0, dur - secondsEach));
                string tmp = Path.Combine(dir, $"m{i}_{Guid.NewGuid():N}.mkv");
                var a = new List<string>
                {
                    "-hide_banner", "-loglevel", "error", "-y",
                    "-ss", at.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                    "-i", input,
                    "-t", secondsEach.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "-map", $"0:{video.Index}", "-an", "-sn",
                };
                if (opt.MaxHeight > 0 && (video.Height ?? 0) > opt.MaxHeight)
                    a.AddRange(new[] { "-vf", $"scale=-2:{opt.MaxHeight}" });
                a.AddRange(encArgs);
                a.Add(tmp);

                var (code, _) = await RunFfmpegAsync(a, 0, rep, ct);
                if (code == 0 && File.Exists(tmp))
                {
                    totalBytes += new FileInfo(tmp).Length;
                    totalSecs += secondsEach;
                }
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                rep.FileProgress((i + 1.0) / samples, "");
            }
        }
        finally { try { Directory.Delete(dir, true); } catch { } }

        if (totalSecs <= 0 || totalBytes <= 0) return 0;
        // Cada muestra empieza con un fotograma clave, así que salen algo "caras"
        // respecto a una codificación continua: descontamos ese sesgo.
        const double SampleKeyframeBias = 0.94;
        return (int)Math.Round(totalBytes * 8.0 / totalSecs / 1000.0 * SampleKeyframeBias);
    }

    /// <summary>¿Ese códec de subtítulo es de texto (y por tanto convertible a mov_text para MP4)?</summary>
    public static bool IsTextSubtitle(string codec) =>
        !ImageSubs.Contains(codec, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Explica en cristiano qué subtítulos se han quedado fuera y por qué, para poder
    /// enseñarlo tanto en el registro como en el aviso final de la ventana.
    /// </summary>
    private static string SubtitleLossMessage(IEnumerable<FfStream> lost, string container) =>
        SubtitleLossMessage(
            lost.Select(s => (s.CodecName, string.IsNullOrEmpty(s.Lang) ? "?" : s.Lang)).ToList(),
            container);

    internal static string SubtitleLossMessage(IReadOnlyList<(string codec, string lang)> lost, string container)
    {
        if (lost.Count == 0) return "";
        var t = Textos.Instancia;
        string cont = container.ToUpperInvariant();
        string langs = string.Join(", ", lost.Select(x => x.lang).Distinct());
        bool allImage = lost.All(x => !IsTextSubtitle(x.codec));
        bool una = lost.Count == 1;

        // Cuatro frases enteras, no trozos concatenados: el castellano concuerda
        // género y número («incluirla», «las necesitas») y montar la frase por
        // partes deja al traductor sin manera de reordenarla.
        if (allImage)
        {
            string tipos = string.Join("/", lost.Select(x => FriendlyCodec(x.codec)).Distinct());
            return una
                ? string.Format(t.MotorSubsFueraImagenUna, langs, tipos, cont)
                : string.Format(t.MotorSubsFueraImagenVarias, lost.Count, langs, tipos, cont);
        }
        return una
            ? string.Format(t.MotorSubsFueraUna, langs, cont)
            : string.Format(t.MotorSubsFueraVarias, lost.Count, langs, cont);
    }

    private static string FriendlyCodec(string codec) => codec.ToLowerInvariant() switch
    {
        "hdmv_pgs_subtitle" or "pgssub" => "PGS",
        "dvd_subtitle" or "dvdsub" => "VobSub",
        "dvb_subtitle" or "dvbsub" => "DVB",
        "xsub" => "XSUB",
        _ => codec,
    };

    /// <summary>
    /// Evita que dos vídeos distintos acaben escribiendo en el mismo archivo dentro de
    /// la misma tanda (posible al renombrar): al segundo se le añade " (2)", " (3)"…
    /// </summary>
    private static string UniqueOutput(string path, HashSet<string> used)
    {
        if (used.Add(path)) return path;
        string dir = Path.GetDirectoryName(path) ?? "";
        string bn = Path.GetFileNameWithoutExtension(path);
        string ex = Path.GetExtension(path);
        for (int i = 2; ; i++)
        {
            var cand = Path.Combine(dir, $"{bn} ({i}){ex}");
            if (used.Add(cand)) return cand;
        }
    }

    // ---------- pausa / reanudación del FFmpeg en curso ----------
    private Process? _active;
    public void Pause()  { var p = _active; if (p is { HasExited: false }) ProcessControl.Suspend(p); }
    public void Resume() { var p = _active; if (p is { HasExited: false }) ProcessControl.Resume(p); }

    // ---------- ejecutar ffmpeg con progreso + cancelación ----------
    private static readonly Regex TimeRx =
        new(@"time=(\d+):(\d+):(\d+)\.(\d+)", RegexOptions.Compiled);

    // ---------- detección y espera por disco lleno ----------
    // Margen mínimo de disco antes de pausar (configurable desde Preferencias). Por defecto 200 MB.
    internal static long MinFreeBytes = 200L * 1024 * 1024;

    // ¿Se permite usar codificadores por hardware? (configurable desde Preferencias)
    /// <summary>
    /// Que aceleracion de DECODIFICACION pidio el usuario en Preferencias: «auto», «ninguna» o
    /// el nombre de una (cuda, qsv, vaapi, d3d11va, videotoolbox).
    ///
    /// <para>
    /// Separada de <see cref="AllowHardware"/> a proposito: decodificar y codificar por hardware
    /// no fallan por lo mismo -uno depende del decodificador de la tarjeta y el otro de las
    /// sesiones de codificacion-, y quien apague una por un problema concreto no tiene por que
    /// perder la otra.
    /// </para>
    /// </summary>
    public static string AceleracionPedida { get; set; } = Objetivo.AceleracionDeVideo.Auto;

    public static bool AllowHardware = true;

    /// <summary>
    /// ¿Debe enviarse el original a la Papelera tras comprimir? Solo si está activado, la
    /// compresión fue correcta, y el comprimido NO es el propio original (evita autoborrado
    /// cuando la salida coincide con la entrada).
    /// </summary>
    public static bool ShouldRecycleSource(bool enabled, FileResult r) =>
        enabled && r.Ok
        && !string.IsNullOrEmpty(r.SourcePath)
        && !string.Equals(
            Path.GetFullPath(r.SourcePath),
            string.IsNullOrEmpty(r.OutputPath) ? "\0" : Path.GetFullPath(r.OutputPath),
            StringComparison.OrdinalIgnoreCase);

    internal static bool IsDiskFull(string err) =>
        err.Contains("No space left", StringComparison.OrdinalIgnoreCase) ||
        err.Contains("ENOSPC", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// ¿Se puede escribir en esa carpeta? Devuelve <c>null</c> si sí, y el motivo si no.
    ///
    /// <para>
    /// <b>Escribiendo, no mirando los permisos.</b> En un montaje de red, en un disco montado
    /// de solo lectura o con otro dueño, los bits dicen una cosa y el sistema hace otra. La
    /// única forma honesta de saber si se puede escribir es escribir.
    /// </para>
    /// <para>
    /// Existe por un caso real: doce capítulos, cada uno con su análisis de pistas y su
    /// reintento sin subtítulos, y los doce fallando con el mismo «Permission denied» en la
    /// carpeta de destino. El fallo era del sistema del usuario, pero <b>la forma de contarlo
    /// era nuestra</b>: nada impedía saberlo antes de tocar el primero. Esto cuesta
    /// milisegundos y responde la pregunta entera.
    /// </para>
    /// <para>
    /// La carpeta se crea si no está, porque es lo que la compresión iba a hacer de todas
    /// formas: comprobar sobre una carpeta que aún no existe y responder «no se puede» sería
    /// un falso negativo en el caso más normal, la primera vez.
    /// </para>
    /// </summary>
    public static string? SePuedeEscribirEn(string carpeta)
    {
        var prueba = Path.Combine(carpeta, $".ondine-prueba-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(carpeta);
            using (File.Create(prueba)) { }
            return null;
        }
        catch (Exception ex) { return ex.Message; }
        finally
        {
            // Recoger siempre: un fichero de prueba olvidado en la carpeta de la biblioteca es
            // basura que parece contenido.
            try { if (File.Exists(prueba)) File.Delete(prueba); } catch { }
        }
    }

    // Inyectable para pruebas (simular disco lleno). En producción consulta el disco real.
    internal static Func<string, long> FreeSpaceProvider = DefaultFreeSpace;
    /// <summary>
    /// El espacio libre del disco DE DESTINO.
    ///
    /// <para>
    /// Esto medía <c>Path.GetPathRoot(dir)</c>, que en Windows da la letra de unidad y acierta,
    /// pero en Linux y macOS <b>devuelve «/» para cualquier ruta absoluta</b>: se medía siempre
    /// la partición raíz. Con la raíz llena y el disco de destino vacío, la compresión se
    /// quedaba esperando para siempre a que se liberara un sitio que no necesitaba; al revés,
    /// escribía hasta llenar el destino sin avisar. Ver <see cref="PuntoDeMontaje"/>.
    /// </para>
    /// <para>
    /// Y cuando no se puede medir se devuelve <c>long.MaxValue</c>, que es «adelante». Es a
    /// propósito: una unidad de red que no sabe decir su espacio libre no debe impedir
    /// comprimir. El que avisa de verdad es ffmpeg, con su ENOSPC.
    /// </para>
    /// </summary>
    private static long DefaultFreeSpace(string dir)
    {
        try
        {
            var completa = Path.GetFullPath(dir);
            var montaje = PuntoDeMontaje.De(completa, PuntoDeMontaje.DeEstaMaquina());
            return new DriveInfo(montaje ?? Path.GetPathRoot(completa)!).AvailableFreeSpace;
        }
        catch { return long.MaxValue; }
    }
    private static long FreeSpace(string dir) => FreeSpaceProvider(dir);

    /// <summary>Espera (sin bloquear ni cancelar) hasta que haya al menos `need` bytes libres.</summary>
    private static async Task WaitForSpaceAsync(string dir, long need, IEngineReporter rep, CancellationToken ct)
    {
        bool notified = false;
        while (FreeSpace(dir) < need)
        {
            ct.ThrowIfCancellationRequested();
            if (!notified) { rep.DiskFull(true); rep.Log(Textos.Instancia.MotorDiscoLleno); notified = true; }
            await Task.Delay(2500, ct);
        }
        if (notified) { rep.DiskFull(false); rep.Log(Textos.Instancia.MotorDiscoConEspacio); }
    }

    /// <summary>
    /// ffmpeg codifica con TODOS los núcleos y ahoga a la propia app. Bajar la prioridad a
    /// «por debajo de lo normal» no bastaba: medido en esta máquina (8 hilos, x265), con la
    /// CPU al 100 % la ventana seguía recibiendo solo un ~4,9 % de CPU y la interfaz respondía
    /// tarde. La razón es que «por debajo de lo normal» aún COMPITE por los ocho núcleos; si
    /// los ocho están llenos, el hilo de la interfaz espera su turno.
    ///
    /// La cura de verdad es RESERVAR núcleos: se le prohíbe a ffmpeg usar uno (o unos pocos)
    /// núcleos con la afinidad de proceso, así la interfaz SIEMPRE tiene un núcleo libre,
    /// pase lo que pase con la codificación. Cuesta ese tanto por ciento de velocidad de
    /// encode (un núcleo de ocho ≈ 12 %), que en una tarea de fondo no se nota y en la
    /// interfaz —que es lo que el usuario está mirando— se nota muchísimo.
    ///
    /// Se mantiene además «por debajo de lo normal»: sobre los núcleos que sí comparte, la
    /// interfaz (prioridad normal) le gana el turno igualmente.
    /// </summary>
    private static void ApartarDelPasoDeLaInterfaz(Process proc)
    {
        // Puede haber terminado ya (un ffmpeg que falla al instante) o negarse por permisos:
        // no poder apartarlo no es motivo para no codificar.
        try { proc.PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

        // La afinidad de proceso solo existe en Windows y Linux; en macOS ni se intenta (y
        // así el analizador CA1416 no protesta al compilar la versión de macOS del CLI).
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux()) return;

        try
        {
            int cores = Environment.ProcessorCount;
            // Por debajo de 3 núcleos no se reserva: quitarle uno a una máquina de 2 dejaría
            // la codificación coja sin ganar una interfaz fluida de verdad.
            if (cores < 3) return;

            // Se reserva ~1 de cada 4 (mínimo 1) para la app. En una máquina de 8 son 2, que
            // en equipos con HyperThreading libera un núcleo físico entero para la interfaz.
            int reservados = Math.Max(1, cores / 4);
            int usables = cores - reservados;
            // Máscara con los `usables` núcleos bajos encendidos; los altos quedan libres para
            // la interfaz, el hilo de render y todo lo demás de la app.
            nint mascara = (nint)((1L << usables) - 1);
            proc.ProcessorAffinity = mascara;
        }
        catch { /* la afinidad puede negarse (permisos, >64 núcleos): la prioridad ya ayuda */ }
    }

    private async Task<(int code, string err)> RunFfmpegAsync(
        List<string> args, double durSec, IEngineReporter rep, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(Ffmpeg)
        {
            RedirectStandardError = true,
            // stdout NO se redirige: el encode escribe al fichero de salida, no a stdout, así
            // que ese pipe no se usa. Redirigirlo y leerlo en fire-and-forget dejaba un handle
            // de tubería colgando por cada exportación — parte de la fuga de handles por ciclo.
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        ApartarDelPasoDeLaInterfaz(proc);
        _active = proc;
        var err = new StringBuilder();
        try
        {
            var stderrTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await proc.StandardError.ReadLineAsync()) != null)
                {
                    var m = TimeRx.Match(line);
                    if (m.Success && durSec > 0)
                    {
                        double sec = int.Parse(m.Groups[1].Value) * 3600
                                   + int.Parse(m.Groups[2].Value) * 60
                                   + int.Parse(m.Groups[3].Value)
                                   + double.Parse("0." + m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
                        rep.FileProgress(Math.Clamp(sec / durSec, 0, 1), line);
                    }
                    else if (line.Length > 0)
                    {
                        lock (err) { if (err.Length < 4000) err.AppendLine(line); }   // guardar líneas de error
                    }
                }
            });

            try
            {
                await proc.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                ProcessControl.Resume(proc);                     // si estaba en pausa, reanudar para poder matarlo
                try { proc.Kill(entireProcessTree: true); } catch { }
                try { await stderrTask; } catch { }
                throw;
            }
            try { await stderrTask; } catch { }
            string errStr; lock (err) errStr = err.ToString();
            return (proc.ExitCode, errStr);
        }
        finally { _active = null; }
    }

    // ---------- ejecutar un proceso y capturar salida ----------
    private static async Task<(int code, string stdout, string stderr)> RunAsync(string exe, string[] args)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = new Process { StartInfo = psi };
        proc.Start();
        // El sondeo y las miniaturas del import también van por aquí: un ffmpeg que busca y
        // decodifica un fotograma de un vídeo grande da un tirón a la interfaz si corre a
        // prioridad normal. Se aparta igual que la codificación.
        ApartarDelPasoDeLaInterfaz(proc);
        var so = proc.StandardOutput.ReadToEndAsync();
        var se = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (proc.ExitCode, await so, await se);
    }
}
