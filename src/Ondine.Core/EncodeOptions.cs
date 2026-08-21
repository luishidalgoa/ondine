namespace Ondine;

/// <summary>Opciones de compresión. Espejo de los parámetros del motor original.</summary>
public sealed class EncodeOptions
{
    public string? Output { get; set; }
    public string Container { get; set; } = "mkv";   // mkv | mp4 | webm
    public string VideoCodec { get; set; } = "hevc"; // hevc | h264 | av1
    public bool AudioOnly { get; set; }              // extraer solo el audio (sin vídeo)
    public string AudioFormat { get; set; } = "mp3"; // mp3 | m4a | flac | opus (cuando AudioOnly)
    public string Lang { get; set; } = "spa";
    public List<string> KeepLangs { get; set; } = new();   // vacío = preferido + eng
    public List<string>? SubLangs { get; set; }            // null = todos
    public bool NoSubs { get; set; }
    public int Quality { get; set; }        // 0 = automático
    public int MaxHeight { get; set; }      // 0 = sin reescalar
    public int AudioBitrate { get; set; }   // 0 = copiar audio original
    /// <summary>
    /// Bitrate de video objetivo, en kbps. Cero = calidad constante, que es el modo de
    /// siempre.
    ///
    /// <para>
    /// Manda sobre <see cref="Quality"/> cuando se pone, y NO se combinan: con las dos
    /// puestas ffmpeg no obedece a ninguna. Lo decide <c>ArgumentosDeBitrate</c>.
    /// </para>
    /// </summary>
    public int BitrateVideoKbps { get; set; }

    /// <summary>
    /// Tamano al que se quiere llegar, en bytes. Cero = sin objetivo.
    ///
    /// <para>
    /// Es POR FICHERO, no por lote: cada uno dura distinto, asi que el bitrate que hace
    /// falta lo deriva el motor cuando ya sabe la duracion. Por eso esto no se puede
    /// convertir a <see cref="BitrateVideoKbps"/> desde la pantalla.
    /// </para>
    /// </summary>
    public long TamanoObjetivoBytes { get; set; }

    /// <summary>
    /// Cuanto se esmera el codificador. Lo que habia antes de que esto se pudiera elegir es
    /// <see cref="Objetivo.Velocidad.Equilibrado"/>, que es el valor por defecto: quien no
    /// toque nada sigue obteniendo lo de siempre.
    /// </summary>
    public Objetivo.Velocidad Velocidad { get; set; } = Objetivo.Velocidad.Equilibrado;

    /// <summary>
    /// Que hacer con el audio. Por defecto <see cref="Audio.AudioElegido.Copiar"/>, que es
    /// lo que la app venia haciendo cuando no se pedia un bitrate.
    /// </summary>
    public Audio.AudioElegido AudioCodec { get; set; } = Audio.AudioElegido.Copiar;

    public bool Force { get; set; }
    public bool DryRun { get; set; }
    public RenameRule? NameRule { get; set; }   // renombrado del archivo de salida (estilo PowerRename)

    // ── Recortes ──
    // Un tramo del original en vez del fichero entero. Se pide aquí, en las MISMAS opciones,
    // para que recortar herede tal cual el mapeo de pistas, los idiomas, los subtítulos y el
    // nombrado de la compresión: nada de una segunda ruta de codificación que mantener.
    public double? Desde { get; set; }          // null = desde el principio
    public double? Duracion { get; set; }       // null = hasta el final
    public string? NombreSalida { get; set; }   // sin extensión; null = el del original
}
