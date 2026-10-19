using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Ondine.Ava;

/// <summary>
/// Una superficie de vídeo que es un control de Avalonia normal: LibVLC decodifica en memoria y
/// aquí se pinta el fotograma.
///
/// <para>
/// <b>Por qué existe.</b> El <c>VideoView</c> de LibVLCSharp mete una ventana nativa dentro de
/// la nuestra, y una ventana nativa se pinta por encima de todo lo que dibuja Avalonia. Para
/// poder poner controles sobre el vídeo, el <c>VideoView</c> los lleva en una SEGUNDA ventana
/// transparente colocada encima. En Windows funciona. En Linux (Cinnamon, probado) el gestor de
/// ventanas deja esa segunda ventana <b>por debajo</b> de la del reproductor: el vídeo se ve,
/// pero ni los controles, ni el botón de cerrar, ni el clic para pausar existen. Y aunque se
/// forzara a subir, el teclado iría a la ventana equivocada.
/// </para>
/// <para>
/// Con este control no hay ventana nativa ni overlay: el vídeo es un hijo más y los controles
/// van encima con las reglas de siempre. El precio es el cálculo: el fotograma pasa por la
/// CPU, y por eso se limita a 720p (VLC lo reduce antes): medido, un 1080p pasa de ~57 % a ~38 %
/// de un núcleo y un 4K de ~78 % a ~58 %.
/// Para contestar «¿qué capítulo es?» sobra.
/// </para>
/// <para>
/// Tres búferes y no uno: VLC escribe el siguiente fotograma mientras la interfaz copia el
/// anterior, y con uno solo se vería a medias.
/// </para>
/// </summary>
internal sealed class VideoEnSoftware : Control
{
    private const int MaxAncho = 1280, MaxAlto = 720;
    private const int Buffers = 3;

    private readonly object _candado = new();
    private readonly IntPtr[] _memoria = new IntPtr[Buffers];
    private int _anchoVlc, _altoVlc, _pitch;
    private int _escribiendo = -1, _listo = -1, _leyendo = -1, _siguiente;
    private WriteableBitmap? _bitmap;
    private volatile bool _pintando, _cerrado;

    // Los delegados se guardan: si el recolector se los lleva, LibVLC llama a memoria libre.
    private readonly MediaPlayer.LibVLCVideoFormatCb _formato;
    private readonly MediaPlayer.LibVLCVideoCleanupCb _limpieza;
    private readonly MediaPlayer.LibVLCVideoLockCb _bloquear;
    private readonly MediaPlayer.LibVLCVideoUnlockCb _desbloquear;
    private readonly MediaPlayer.LibVLCVideoDisplayCb _mostrar;

    public VideoEnSoftware()
    {
        _formato = Formato;
        _limpieza = Limpiar;
        _bloquear = Bloquear;
        _desbloquear = (_, _, _) => { };
        _mostrar = Mostrar;
        ClipToBounds = true;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.MediumQuality);
    }

    /// <summary>Se llama ANTES de reproducir: los callbacks no se pueden cambiar con el vídeo en marcha.</summary>
    public void Enganchar(MediaPlayer mp)
    {
        mp.SetVideoFormatCallbacks(_formato, _limpieza);
        mp.SetVideoCallbacks(_bloquear, _desbloquear, _mostrar);
    }

    /// <summary>Deja de pintar. La memoria la suelta LibVLC al parar (<see cref="Limpiar"/>).</summary>
    public void Desenganchar()
    {
        _cerrado = true;
        lock (_candado) { _bitmap?.Dispose(); _bitmap = null; }
    }

    // ───────────────────── hilo de LibVLC ─────────────────────

    private uint Formato(ref IntPtr opaque, IntPtr chroma, ref uint ancho, ref uint alto,
                         ref uint pitches, ref uint lineas)
    {
        // BGRA de 32 bits: el que entiende Avalonia sin convertir.
        Marshal.WriteByte(chroma, 0, (byte)'R'); Marshal.WriteByte(chroma, 1, (byte)'V');
        Marshal.WriteByte(chroma, 2, (byte)'3'); Marshal.WriteByte(chroma, 3, (byte)'2');

        // Si es más grande que 720p, se pide más pequeño y VLC escala él antes de dárnoslo.
        double f = Math.Min(1.0, Math.Min((double)MaxAncho / ancho, (double)MaxAlto / alto));
        uint w = Math.Max(2, (uint)(ancho * f)) & ~1u, h = Math.Max(2, (uint)(alto * f)) & ~1u;
        ancho = w; alto = h;
        pitches = w * 4;
        lineas = h;

        lock (_candado)
        {
            LiberarMemoria();
            _anchoVlc = (int)w; _altoVlc = (int)h; _pitch = (int)(w * 4);
            for (int i = 0; i < Buffers; i++) _memoria[i] = Marshal.AllocHGlobal(_pitch * _altoVlc);
            _escribiendo = _listo = _leyendo = -1;
        }
        return 1;
    }

    private void Limpiar(ref IntPtr opaque)
    {
        lock (_candado) LiberarMemoria();
    }

    private void LiberarMemoria()
    {
        for (int i = 0; i < Buffers; i++)
            if (_memoria[i] != IntPtr.Zero) { Marshal.FreeHGlobal(_memoria[i]); _memoria[i] = IntPtr.Zero; }
        _listo = _escribiendo = _leyendo = -1;
    }

    private IntPtr Bloquear(IntPtr opaque, IntPtr planes)
    {
        lock (_candado)
        {
            // Uno que no sea ni el último terminado ni el que la interfaz está copiando.
            int i = _siguiente;
            for (int n = 0; n < Buffers; n++, i = (i + 1) % Buffers)
                if (i != _listo && i != _leyendo) break;
            _siguiente = (i + 1) % Buffers;
            _escribiendo = i;
            Marshal.WriteIntPtr(planes, _memoria[i]);
        }
        return IntPtr.Zero;
    }

    private void Mostrar(IntPtr opaque, IntPtr picture)
    {
        lock (_candado) { if (_escribiendo >= 0) _listo = _escribiendo; }
        if (_cerrado || _pintando) return;
        _pintando = true;
        Dispatcher.UIThread.Post(Copiar, DispatcherPriority.Render);
    }

    // ───────────────────── hilo de la interfaz ─────────────────────

    private void Copiar()
    {
        _pintando = false;
        if (_cerrado) return;

        lock (_candado)
        {
            if (_listo < 0 || _memoria[_listo] == IntPtr.Zero) return;
            _leyendo = _listo;

            if (_bitmap is null || _bitmap.PixelSize.Width != _anchoVlc || _bitmap.PixelSize.Height != _altoVlc)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(new PixelSize(_anchoVlc, _altoVlc), new Vector(96, 96),
                                              PixelFormat.Bgra8888, AlphaFormat.Opaque);
            }

            using (var fb = _bitmap.Lock())
            {
                int fila = Math.Min(_pitch, fb.RowBytes);
                for (int y = 0; y < _altoVlc; y++)
                    unsafe
                    {
                        Buffer.MemoryCopy((byte*)_memoria[_leyendo] + (long)y * _pitch,
                                          (byte*)fb.Address + (long)y * fb.RowBytes, fb.RowBytes, fila);
                    }
            }
            _leyendo = -1;
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext ctx)
    {
        ctx.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

        WriteableBitmap? b;
        lock (_candado) b = _bitmap;
        if (b is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // Cabe entero y centrado, sin deformarse: los lados sobrantes quedan negros.
        double escala = Math.Min(Bounds.Width / b.PixelSize.Width, Bounds.Height / b.PixelSize.Height);
        double w = b.PixelSize.Width * escala, h = b.PixelSize.Height * escala;
        var destino = new Rect((Bounds.Width - w) / 2, (Bounds.Height - h) / 2, w, h);
        ctx.DrawImage(b, new Rect(0, 0, b.PixelSize.Width, b.PixelSize.Height), destino);
    }
}
