using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// Parte un episodio en sus mini-historias («segmentos») SIN recodificar.
///
/// El reparto lo decide <see cref="SegmentSplitter"/> a partir de dos datos: cuántas historias
/// trae el episodio (lo dice el catálogo) y dónde están los fundidos a negro (los busca ffmpeg).
/// Aquí solo se ejecutan las herramientas.
///
/// El corte va con «-c copy»: se copian los paquetes tal cual, sin volver a codificar. Además de
/// ser instantáneo —menos de un segundo frente a varios minutos— es la única forma de no perder
/// calidad, que es lo que importa cuando lo que se parte es la copia buena.
/// </summary>
public static class SegmentSplitRunner
{
    /// <summary>Duración del vídeo en segundos, o 0 si no se ha podido leer.</summary>
    public static async Task<double> DuracionAsync(string path, CancellationToken ct = default)
    {
        var salida = await CorrerAsync(Engine.FfprobePath, new[]
        {
            "-v", "error", "-show_entries", "format=duration",
            "-of", "default=noprint_wrappers=1:nokey=1", path,
        }, ct);
        return double.TryParse(salida.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static readonly Regex RxNegro = new(
        @"black_start:(?<a>[0-9.]+)\s+black_end:(?<b>[0-9.]+)", RegexOptions.Compiled);

    /// <summary>
    /// Busca los fundidos a negro. Se analiza a 160x120: el filtro solo mira brillo, así que
    /// reducir no cambia lo que encuentra y recorta mucho el tiempo de decodificación.
    /// </summary>
    public static async Task<IReadOnlyList<SegmentSplitter.Negro>> DetectarNegrosAsync(
        string path, CancellationToken ct = default)
    {
        // d=0.15 a propósito, más permisivo que el mínimo que exige el planificador: mejor
        // recoger candidatos de sobra y que sea él quien descarte, con su criterio ya probado.
        var salida = await CorrerAsync(Engine.FfmpegPath, new[]
        {
            "-hide_banner", "-nostats", "-i", path,
            "-vf", "scale=160:120,blackdetect=d=0.15:pix_th=0.10",
            "-an", "-sn", "-f", "null", "-",
        }, ct);

        var lista = new List<SegmentSplitter.Negro>();
        foreach (Match m in RxNegro.Matches(salida))
        {
            if (double.TryParse(m.Groups["a"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) &&
                double.TryParse(m.Groups["b"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
                lista.Add(new SegmentSplitter.Negro(a, b));
        }
        return lista;
    }

    /// <summary>
    /// Saca un trozo a <paramref name="destino"/> copiando los paquetes, sin recodificar.
    /// «-ss» va ANTES de «-i» para que ffmpeg salte por índice y no decodifique lo anterior.
    /// </summary>
    public static async Task<bool> ExtraerAsync(string origen, string destino,
        SegmentSplitter.Trozo trozo, CancellationToken ct = default)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error" };
        if (trozo.Desde > 0)
        {
            args.Add("-ss");
            args.Add(trozo.Desde.ToString("0.###", CultureInfo.InvariantCulture));
        }
        args.Add("-i"); args.Add(origen);
        args.Add("-t"); args.Add(trozo.Duracion.ToString("0.###", CultureInfo.InvariantCulture));
        // Se copian TODAS las pistas: los packs traen varios idiomas de audio y subtítulos.
        //
        // NADA de «-avoid_negative_ts make_zero» aquí, aunque sea lo que se lee por ahí. Con
        // «-c copy» el corte arranca en el fotograma clave ANTERIOR al punto pedido; sin ese
        // parámetro, los fotogramas de más quedan con marca de tiempo negativa y el reproductor
        // los descarta, así que el trozo empieza donde toca. Poniéndolo se desplazan a cero y
        // pasan a verse: medido sobre un episodio real, el trozo arrancaba 5 segundos antes y
        // enseñaba el final de la historia anterior en vez de su tarjeta de título.
        args.AddRange(new[] { "-map", "0", "-c", "copy", destino, "-y" });

        await CorrerAsync(Engine.FfmpegPath, args, ct);
        return File.Exists(destino) && new FileInfo(destino).Length > 0;
    }

    /// <summary>Lanza la herramienta y devuelve lo que escriba (ffmpeg informa por stderr).</summary>
    private static async Task<string> CorrerAsync(string exe, IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"No se pudo lanzar {exe}");
        var salida = p.StandardOutput.ReadToEndAsync(ct);
        var error = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return (await salida) + (await error);
    }
}
