namespace Ondine.Audio;

/// <summary>Qué se quiere que pase con el audio.</summary>
public enum AudioElegido
{
    /// <summary>Los bytes tal cual, sin tocar. Lo mejor cuando el contenedor lo admite.</summary>
    Copiar,
    Aac,
    /// <summary>Lo que entiende un receptor de salón antiguo.</summary>
    Ac3,
    Eac3,
    /// <summary>La mitad de peso con la misma calidad, si el contenedor lo admite.</summary>
    Opus,
    /// <summary>Sin pérdida. No lleva bitrate.</summary>
    Flac,
}

/// <summary>Por qué lo que va a pasar no es lo que se pidió.</summary>
public enum PorQueSeCambio
{
    SeHizoLoPedido,
    /// <summary>El contenedor elegido no admite ese códec.</summary>
    ContenedorNoLoAdmite,
    /// <summary>Se pidió copiar, pero el códec de origen no cabe en ese contenedor.</summary>
    ElOrigenNoSePuedeCopiar,
}

/// <summary>Qué se va a hacer con una pista de audio, y por qué.</summary>
public readonly record struct DecisionDeAudio(
    bool Copiar, string Codec, PorQueSeCambio Porque, AudioElegido Pedido);

/// <summary>
/// Decide el códec de audio de cada pista.
///
/// <para>
/// <b>No todo cabe en todo.</b> WebM solo admite Opus y Vorbis; MP4 acepta FLAC sobre el
/// papel y no lo reproduce casi nada. Pasarle a ffmpeg una combinación imposible falla unas
/// veces y otras produce un fichero que no abre en ningún sitio — y eso segundo es peor,
/// porque parece que salió bien.
/// </para>
/// <para>
/// Por eso se decide antes, se cambia lo que no cabe, y <b>se dice que se cambió</b>:
/// hacerlo en silencio deja al usuario creyendo que tiene AC3 cuando tiene AAC.
/// </para>
/// </summary>
public static class CodecDeAudio
{
    /// <summary>Lo que MP4 sabe guardar de forma que se reproduzca en todas partes.</summary>
    private static readonly HashSet<string> CabenEnMp4 =
        new(StringComparer.OrdinalIgnoreCase) { "aac", "ac3", "eac3", "mp3", "alac" };

    /// <summary>WebM: solo estos dos, y no es una recomendación sino el formato.</summary>
    private static readonly HashSet<string> CabenEnWebm =
        new(StringComparer.OrdinalIgnoreCase) { "opus", "vorbis" };

    private static string Codificador(AudioElegido e) => e switch
    {
        AudioElegido.Ac3 => "ac3",
        AudioElegido.Eac3 => "eac3",
        AudioElegido.Opus => "libopus",
        AudioElegido.Flac => "flac",
        _ => "aac",
    };

    /// <summary>Si el contenedor admite lo que se ha pedido codificar.</summary>
    private static bool Admite(string contenedor, AudioElegido e) => contenedor switch
    {
        "webm" => e == AudioElegido.Opus,
        "mp4" => e is AudioElegido.Aac or AudioElegido.Ac3 or AudioElegido.Eac3,
        _ => true,   // mkv se lo traga todo
    };

    public static DecisionDeAudio Decidir(string contenedor, AudioElegido pedido, string codecOrigen)
    {
        if (pedido == AudioElegido.Copiar)
        {
            var cabe = contenedor switch
            {
                "webm" => CabenEnWebm.Contains(codecOrigen),
                "mp4" => CabenEnMp4.Contains(codecOrigen),
                _ => true,
            };

            if (cabe) return new(true, "copy", PorQueSeCambio.SeHizoLoPedido, pedido);

            // No cabe: hay que recodificar. Se va al que el contenedor pide, no a uno
            // cualquiera — en WebM eso es Opus y en MP4, AAC.
            var deRespaldo = contenedor == "webm" ? AudioElegido.Opus : AudioElegido.Aac;
            return new(false, Codificador(deRespaldo),
                       PorQueSeCambio.ElOrigenNoSePuedeCopiar, pedido);
        }

        if (Admite(contenedor, pedido))
            return new(false, Codificador(pedido), PorQueSeCambio.SeHizoLoPedido, pedido);

        var forzado = contenedor == "webm" ? AudioElegido.Opus : AudioElegido.Aac;
        return new(false, Codificador(forzado), PorQueSeCambio.ContenedorNoLoAdmite, pedido);
    }

    /// <summary>
    /// Los argumentos de UNA pista, con su índice.
    ///
    /// <para>
    /// El índice no es un detalle: sin él, el ajuste de la primera pista se aplicaría a
    /// todas, y un pack con castellano e inglés saldría con los dos iguales.
    /// </para>
    /// <para>
    /// Y al FLAC no se le pone bitrate. Es sin pérdida: un «-b:a 192k» sobre FLAC no lo
    /// comprime a 192, o lo ignora o falla según la versión — y si lo ignora, el usuario
    /// cree haber pedido algo que no ha pedido.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> Argumentos(int indice, DecisionDeAudio d, int kbps)
    {
        if (d.Copiar) return [$"-c:a:{indice}", "copy"];

        List<string> args = [$"-c:a:{indice}", d.Codec];

        var sinPerdida = d.Codec is "flac" or "alac";
        if (!sinPerdida && kbps > 0) args.AddRange([$"-b:a:{indice}", $"{kbps}k"]);

        return args;
    }
}
