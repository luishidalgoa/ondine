using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ondine;

/// <summary>Una fila de la lista de vídeos, con notificación de cambios para la UI.</summary>
public sealed class VideoRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    private string _estado = "…", _codec = "", _audio = "", _subs = "", _dur = "";

    public string Estado
    {
        get => _estado;
        set
        {
            _estado = value;
            N();
            N(nameof(EstadoBrush));
            // Las tres juntas, y no solo el color: una fila que cambia de «Comprimiendo» a
            // «Error» y no avisa de estas se queda con la clase anterior puesta. Todas las
            // filas pasan por aquí -la lista se llena mientras se trabaja-, así que
            // olvidarse de una es un fallo permanente, no un caso raro.
            N(nameof(EstadoEsOk));
            N(nameof(EstadoEsError));
            N(nameof(EstadoEsEnMarcha));
        }
    }

    /// <summary>Ya está en un códec eficiente con bitrate bajo: no merece la pena recomprimirlo.</summary>
    public bool YaComprimido { get; set; }

    /// <summary>
    /// Color del estado. Verde = terminado con ahorro · rojo = error · morado = en curso ·
    /// apagado = saltado o pendiente. El texto ya distingue por sí solo, así que el color
    /// solo refuerza (no se depende de él).
    /// </summary>
    public string EstadoBrush => _estado switch
    {
        var s when s.StartsWith('−') || s.StartsWith('-') => "Ok",
        var s when s.StartsWith("Error") || s.StartsWith("error") => "Err",
        var s when s.StartsWith("Comprimiendo") || s.StartsWith("En pausa") => "Live",
        _ => "Muted",
    };
    /// <summary>
    /// El mismo estado, dicho en tres síes o noes.
    ///
    /// <para>
    /// Sale de <see cref="EstadoBrush"/> para que no haya dos tablas de estados: si alguna
    /// vez cambia lo que cuenta como «error», cambia en un sitio.
    /// </para>
    /// <para>
    /// Existen porque las dos interfaces piden lo mismo de forma distinta. WPF comparaba el
    /// texto del color con un <c>DataTrigger</c>; Avalonia no tiene disparadores por dato y
    /// pinta con clases, y una clase se ata a un booleano. Que ninguna sea verdad es una
    /// respuesta válida: lo pendiente y lo saltado se quedan con el color apagado.
    /// </para>
    /// </summary>
    public bool EstadoEsOk => EstadoBrush == "Ok";
    public bool EstadoEsError => EstadoBrush == "Err";
    public bool EstadoEsEnMarcha => EstadoBrush == "Live";

    public string Codec { get => _codec; set { _codec = value; N(); } }
    public string Audio { get => _audio; set { _audio = value; N(); } }
    public string Subs { get => _subs; set { _subs = value; N(); } }
    public string Dur { get => _dur; set { _dur = value; N(); } }

    public string Name { get; init; } = "";
    public string Dir { get; init; } = "";
    public string SizeMB { get; init; } = "";
    public string Path { get; init; } = "";
    public long Bytes { get; init; }

    // Datos del análisis, para la estimación de ahorro
    public bool Probed { get; set; }

    /// <summary>
    /// El fichero es un marcador de la nube: está en el índice pero no en el disco.
    ///
    /// <para>
    /// Se pregunta una sola vez, al enumerar, aprovechando que ahí ya se leen sus
    /// atributos. Importa porque <b>abrirlo lo descarga entero</b>: sondear con
    /// ffprobe una biblioteca que vive en OneDrive se traía decenas de gigas sin
    /// que nadie lo hubiera pedido.
    /// </para>
    /// </summary>
    public bool EnLaNube { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Fps { get; set; }
    public int DurationSec { get; set; }
    public int VideoBitrateKbps { get; set; }
    public int AudioBitrateKbps { get; set; }
    public int Channels { get; set; }
    public string AudioCodec { get; set; } = "";
}
