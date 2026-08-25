using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Ondine.Ava;

/// <summary>
/// El vídeo de la pantalla de Recortes, con la misma forma que tenía el <c>MediaElement</c>.
///
/// <para>
/// <b>Existe para que cambiar de motor no obligue a reescribir la pantalla.</b> Recortes son
/// mil ochocientas líneas que hablan de <c>video.Position</c>, <c>video.Play()</c> y
/// <c>video.Source</c> — el vocabulario del <c>MediaElement</c> de WPF—. LibVLC dice lo mismo
/// con otras palabras y en otras unidades: milisegundos en vez de <c>TimeSpan</c>, un
/// <c>Media</c> en vez de una <c>Uri</c>. Traducir eso en cada uno de los sitios donde
/// aparece sería tocar la pantalla entera para no cambiar nada de lo que hace.
/// </para>
/// <para>
/// Así que se traduce <b>aquí, una vez</b>, y la pantalla sigue diciendo lo que decía. Es la
/// misma idea que bajar reglas al motor, aplicada al revés: lo que no cambia se queda quieto
/// y lo que cambia se encierra en un sitio.
/// </para>
/// <para>
/// Lo que <b>no</b> se esconde: los eventos de LibVLC llegan en su propio hilo, y quien los
/// escuche tiene que volver al de la interfaz. Eso se hace aquí —los que salen de esta clase
/// ya vienen en el hilo bueno— porque olvidarlo revienta lejos de donde se causó.
/// </para>
/// </summary>
internal sealed class VideoDeLaPista : IDisposable
{
    private readonly LibVLCSharp.Avalonia.VideoView _vista;
    private LibVLC? _vlc;
    private MediaPlayer? _mp;

    /// <summary>Se abrió y ya se sabe cuánto dura.</summary>
    public event Action<TimeSpan>? DuracionConocida;

    /// <summary>No se pudo abrir. El texto es de LibVLC: es un diagnóstico, no un rótulo.</summary>
    public event Action<string>? Fallo;

    public VideoDeLaPista(LibVLCSharp.Avalonia.VideoView vista) => _vista = vista;

    /// <summary>Si el motor llegó a arrancar. Sin libVLC instalado, no.</summary>
    public bool Listo => _mp is not null;

    private bool Arrancar()
    {
        if (_mp is not null) return true;
        try
        {
            Core.Initialize();
            _vlc = new LibVLC();
            _mp = new MediaPlayer(_vlc);
            _vista.MediaPlayer = _mp;

            _mp.LengthChanged += (_, e) => EnLaInterfaz(
                () => DuracionConocida?.Invoke(TimeSpan.FromMilliseconds(e.Length)));
            _mp.EncounteredError += (_, _) => EnLaInterfaz(
                () => Fallo?.Invoke(Ondine.Localizacion.Textos.Instancia.ReproductorCodecSinSaber));
            return true;
        }
        catch (Exception ex)
        {
            // Si falta libVLC se dice con el nombre del paquete, igual que en el reproductor:
            // en Linux es lo normal la primera vez y se arregla con una línea.
            bool falta = ex is DllNotFoundException or VLCException
                         or TypeInitializationException { InnerException: DllNotFoundException };
            Fallo?.Invoke(falta ? Ondine.Localizacion.Textos.Instancia.ReproductorFaltaLibVlc : ex.Message);
            return false;
        }
    }

    /// <summary>El fichero que se está viendo. Ponerlo lo abre, como el <c>Source</c> de antes.</summary>
    public string? Source
    {
        get => _ruta;
        set
        {
            _ruta = value;
            if (string.IsNullOrEmpty(value) || !Arrancar()) return;
            try { _mp!.Play(new Media(_vlc!, new Uri(value))); }
            catch (Exception ex) { Fallo?.Invoke(ex.Message); }
        }
    }
    private string? _ruta;

    /// <summary>
    /// Dónde está. LibVLC lo lleva en milisegundos; la pantalla habla en <c>TimeSpan</c>,
    /// que es lo que hacía el <c>MediaElement</c>.
    /// </summary>
    public TimeSpan Position
    {
        get => _mp is null ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_mp.Time);
        set { if (_mp is not null) _mp.Time = (long)value.TotalMilliseconds; }
    }

    public TimeSpan Duracion =>
        _mp is null || _mp.Length <= 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_mp.Length);

    /// <summary>Volumen de 0 a 1, como el del <c>MediaElement</c>. LibVLC lo quiere de 0 a 100.</summary>
    public double Volume
    {
        get => _mp is null ? 0 : _mp.Volume / 100.0;
        set { if (_mp is not null) _mp.Volume = (int)Math.Round(Math.Clamp(value, 0, 1) * 100); }
    }

    public void Play() { if (Arrancar()) _mp!.Play(); }

    public void Pause() => _mp?.Pause();

    /// <summary>
    /// Suelta el vídeo sin cerrar el motor.
    ///
    /// <para>
    /// El original avisaba de que <c>MediaElement.Close()</c> filtraba una veintena de
    /// descriptores por fichero y había que evitarlo. Aquí el problema no existe, pero sí
    /// otro: parar LibVLC desde el hilo de la interfaz se bloquea. Por eso se para en un hilo
    /// de fondo, igual que en el reproductor.
    /// </para>
    /// </summary>
    public void Soltar()
    {
        _ruta = null;
        var mp = _mp;
        if (mp is null) return;
        Task.Run(() => { try { mp.Stop(); } catch { } });
    }

    public void Dispose()
    {
        var mp = _mp;
        var vlc = _vlc;
        _mp = null;
        _vlc = null;
        _vista.MediaPlayer = null;

        if (mp is null) { vlc?.Dispose(); return; }
        Task.Run(() =>
        {
            try { mp.Stop(); } catch { }
            try { mp.Dispose(); } catch { }
            try { vlc?.Dispose(); } catch { }
        });
    }

    private static void EnLaInterfaz(Action a) => Dispatcher.UIThread.Post(a);
}
