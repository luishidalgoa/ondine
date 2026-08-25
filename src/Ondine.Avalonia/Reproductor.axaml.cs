using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Path = System.IO.Path;
using Forma = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using LibVLCSharp.Shared;
using Ondine.Localizacion;

namespace Ondine.Ava;

/// <summary>
/// Reproductor integrado, portado de <c>ReproductorWindow</c>: una ventana oscura para
/// contestar «¿qué capítulo es?» sin salir de la app. Los controles flotan sobre el vídeo y
/// se apartan solos cuando no se usan — la imagen es lo que importa.
///
/// <para>
/// <b>La pieza central cambia, y no es un cambio de nombre.</b> Donde había un
/// <c>MediaElement</c> —los códecs del sistema— hay LibVLC. En Windows, un AV1 o un HEVC
/// daban pantalla negra porque no vienen de fábrica, así que el original tenía dos cosas que
/// aquí <b>no se portan</b>: un aviso que recomendaba instalar la extensión de la tienda, y
/// un «modo fotogramas» que recorría el vídeo sacando cuadros con ffmpeg —sin sonido ni
/// reproducción seguida— para al menos contestar la pregunta. LibVLC decodifica los tres, así
/// que ese plan B pierde su razón de ser. Lo que sí queda es el panel de fallo genérico.
/// </para>
/// <para>
/// <b>Y una clase entera de fallo desaparece.</b> En WPF había que llevar una bandera
/// <c>_cerrado</c> mirada en cada evento que pudiera llegar tarde, porque un
/// <c>BufferingStarted</c> posterior al cierre rearrancaba una animación infinita y dejaba la
/// app gastando ~11 % de un núcleo para siempre. Aquí la espera se corta con un testigo de
/// cancelación al salir del árbol, y los eventos de LibVLC llegan en otro hilo y se
/// devuelven al de la interfaz con <c>Dispatcher.UIThread.Post</c>, que no hace nada si la
/// ventana ya no está.
/// </para>
/// </summary>
public partial class Reproductor : Window
{
    private readonly string _ruta = "";
    private readonly bool _eraMarcador;   // ¿estaba solo en la nube antes de abrirlo?

    private LibVLC? _vlc;
    private MediaPlayer? _mp;

    private readonly DispatcherTimer _apagon = new() { Interval = TimeSpan.FromSeconds(2.6) };
    private bool _visible = true;
    private bool _pausado;
    private bool _desdeReloj;             // el reloj mueve la barra: eso no es buscar
    private bool _mudo;
    private double _volumenPrevio = 0.8;
    private Avalonia.Point _ultimoRaton;

    // ── la previa de la barra ── mismo patrón que en WPF: hueco de 5 s, caché y rebote
    private const int HuecoPrevia = 5;
    private readonly Dictionary<int, Bitmap> _previas = [];
    private readonly DispatcherTimer _esperaPrevia = new() { Interval = TimeSpan.FromMilliseconds(140) };
    private int _previaPedida = -1;
    private bool _sacandoPrevia;
    private string? _tempPrevias;
    private readonly CancellationTokenSource _corte = new();

    private const string GlifoPausa = "M6,3 L6,17 M13,3 L13,17";
    private const string GlifoPlay = "M6.5,3.5 L17,10 L6.5,16.5 Z";
    private const string GlifoAltavoz =
        "M4,7.5 L7,7.5 L11,4 L11,16 L7,12.5 L4,12.5 Z M13.5,7.5 A 3.5,3.5 0 0 1 13.5,12.5";
    private const string GlifoMudo =
        "M4,7.5 L7,7.5 L11,4 L11,16 L7,12.5 L4,12.5 Z M13,8 L17,12 M17,8 L13,12";
    private const string GlifoExpandir =
        "M3,7.5 L3,3 L7.5,3 M12.5,3 L17,3 L17,7.5 M17,12.5 L17,17 L12.5,17 M7.5,17 L3,17 L3,12.5";
    private const string GlifoContraer =
        "M7.5,3 L7.5,7.5 L3,7.5 M17,7.5 L12.5,7.5 L12.5,3 M12.5,17 L12.5,12.5 L17,12.5 M3,12.5 L7.5,12.5 L7.5,17";

    public Reproductor() => AvaloniaXamlLoader.Load(this);

    public Reproductor(string ruta) : this()
    {
        _ruta = ruta;
        Lbl("lblTitulo").Text = Path.GetFileName(ruta);

        // Se anota ANTES de tocar el fichero: reproducirlo lo descarga, y después ya no
        // habría forma de saber si estaba en el disco porque el usuario quiso.
        _eraMarcador = Ondine.Reindex.Nube.EsMarcador(ruta);

        Cargando().Texto = _eraMarcador
            ? Textos.Instancia.ReproductorDescargandoNube   // puede tardar: baja el fichero entero
            : Textos.Instancia.ReproductorAbriendoVideo;

        Btn("btnCerrar").Click += (_, _) => Close();
        Btn("btnPlay").Click += (_, _) => Alternar();
        Btn("btnAtras").Click += (_, _) => Saltar(-10);
        Btn("btnAdelante").Click += (_, _) => Saltar(10);
        Btn("btnPantalla").Click += (_, _) => AlternarPantalla();
        Btn("btnMudo").Click += (_, _) => AlternarMudo();
        Btn("btnSistema").Click += (_, _) => AbrirEnSistema();

        Sld("volumen").PropertyChanged += (_, e) =>
        {
            if (e.Property.Name != nameof(Slider.Value)) return;
            var v = Sld("volumen").Value;
            if (_mp is not null) _mp.Volume = (int)Math.Round(v * 100);
            _mudo = v <= 0.02;
            Glifo("glifoVol", _mudo ? GlifoMudo : GlifoAltavoz);
        };

        var barra = Sld("barra");
        barra.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name != nameof(Slider.Value) || _desdeReloj || _mp is null) return;
            _mp.Time = (long)(barra.Value * 1000);
            Lbl("lblPos").Text = Fmt(TimeSpan.FromSeconds(barra.Value));
        };
        barra.PointerMoved += AlPasarPorLaBarra;
        barra.PointerExited += (_, _) => Globo().IsVisible = false;

        // El clic sobre el vídeo alterna, el doble clic va a pantalla completa.
        var lienzo = this.FindControl<Panel>("lienzo")!;
        lienzo.PointerPressed += (_, _) => Alternar();
        lienzo.DoubleTapped += (_, _) => AlternarPantalla();

        this.FindControl<Grid>("raiz")!.PointerMoved += AlMoverRaton;
        KeyDown += AlPulsarTecla;

        _apagon.Tick += (_, _) => { _apagon.Stop(); if (!RatonEnControles()) Ocultar(); };
        _esperaPrevia.Tick += async (_, _) => { _esperaPrevia.Stop(); await SacarPreviaAsync(); };

        RevisarNube();
        Opened += (_, _) => Arrancar();
        Closed += (_, _) => Apagar();

        Mostrar();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private Slider Sld(string n) => this.FindControl<Slider>(n)!;
    private Border Globo() => this.FindControl<Border>("globoPrevia")!;
    private CirculosCargando Cargando() => this.FindControl<CirculosCargando>("cargando")!;
    private void Glifo(string n, string data) =>
        this.FindControl<Forma>(n)!.Data = Geometry.Parse(data);

    private static string Fmt(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
                          : $"{t.Minutes}:{t.Seconds:00}";

    // ─────────────────────────── arranque y apagado ───────────────────────────

    private void Arrancar()
    {
        try
        {
            MotorDeVideo.Arrancar();
            _vlc = new LibVLC();
            _mp = new MediaPlayer(_vlc);
            this.FindControl<LibVLCSharp.Avalonia.VideoView>("video")!.MediaPlayer = _mp;

            // Los eventos de LibVLC llegan en SU hilo, no en el de la interfaz. Tocar un
            // control desde ahí revienta; con Post se devuelven al hilo bueno, y si la
            // ventana ya se cerró simplemente no se ejecuta nada.
            _mp.LengthChanged += (_, e) => EnLaInterfaz(() => PonerDuracion(e.Length));
            _mp.TimeChanged += (_, e) => EnLaInterfaz(() => PonerPosicion(e.Time));
            _mp.EncounteredError += (_, _) => EnLaInterfaz(() => Fallo());
            _mp.Playing += (_, _) => EnLaInterfaz(() =>
            {
                Cargando().IsVisible = false;
                _pausado = false;
                Glifo("glifoPlay", GlifoPausa);
            });
            _mp.EndReached += (_, _) => EnLaInterfaz(() =>
            {
                _pausado = true;
                Glifo("glifoPlay", GlifoPlay);
                Mostrar();
            });

            _mp.Volume = (int)Math.Round(Sld("volumen").Value * 100);
            _mp.Play(new Media(_vlc, new Uri(_ruta)));
        }
        catch (Exception ex)
        {
            // Si LibVLC no esta, se dice CON EL NOMBRE DEL PAQUETE. En Windows viene dentro
            // de la app, asi que esto es de Linux: la primera vez es normal no tenerlo, y
            // se arregla con una linea. «No se pudo inicializar el reproductor» seria
            // verdad y no serviria de nada; «sudo apt install vlc» se teclea y ya esta.
            //
            // Se reconoce por el tipo y no por el texto del mensaje, que llega en el idioma
            // del sistema y cambia entre versiones.
            bool falta = ex is DllNotFoundException or TypeInitializationException
                             { InnerException: DllNotFoundException }
                         || ex is VLCException;

            Fallo(falta ? Textos.Instancia.ReproductorFaltaLibVlc : ex.Message);
        }
    }

    private static void EnLaInterfaz(Action a) => Dispatcher.UIThread.Post(a);

    /// <summary>
    /// Cerrar el reproductor sin que la app se quede colgada.
    ///
    /// <para>
    /// <b>Esto costo encontrarlo y no se deduce leyendo nada.</b> Parar y tirar el
    /// <c>MediaPlayer</c> desde el hilo de la interfaz —que es donde llega <c>Closed</c>—
    /// <b>se bloquea</b>: LibVLC espera a que sus hilos de decodificacion terminen, y esos
    /// necesitan que el hilo de la interfaz siga atendiendo. La aplicacion se queda quieta
    /// para siempre, sin excepcion y sin mensaje. Se descubrio porque la comprobacion de
    /// arranque dejo de terminar.
    /// </para>
    /// <para>
    /// La salida son dos pasos: primero se DESENGANCHA el reproductor de la vista —asi deja
    /// de haber nada que pintar— y luego se apaga en un hilo de fondo, donde bloquear no
    /// molesta a nadie. La ventana se cierra al momento y LibVLC recoge a su ritmo.
    /// </para>
    /// </summary>
    private void Apagar()
    {
        _corte.Cancel();
        _apagon.Stop();
        _esperaPrevia.Stop();
        BorrarPrevias();

        var mp = _mp;
        var vlc = _vlc;
        _mp = null;
        _vlc = null;
        if (mp is null) { vlc?.Dispose(); return; }

        var vista = this.FindControl<LibVLCSharp.Avalonia.VideoView>("video");
        if (vista is not null) vista.MediaPlayer = null;

        Task.Run(() =>
        {
            try { mp.Stop(); } catch { }
            try { mp.Dispose(); } catch { }
            try { vlc?.Dispose(); } catch { }
        });
    }

    private void PonerDuracion(long ms)
    {
        if (ms <= 0) return;
        var seg = ms / 1000.0;
        Sld("barra").Maximum = seg;
        Lbl("lblDur").Text = Fmt(TimeSpan.FromSeconds(seg));
    }

    private void PonerPosicion(long ms)
    {
        // No hace falta una bandera de «se esta arrastrando»: mientras el raton mueve el
        // tirador, cada cambio de Value hace su propia busqueda, y la que llega del reloj
        // viene marcada con _desdeReloj para no rebotar. En WPF si hacia falta porque el
        // reloj y el arrastre se pisaban.
        var seg = ms / 1000.0;
        _desdeReloj = true;
        Sld("barra").Value = seg;
        _desdeReloj = false;
        Lbl("lblPos").Text = Fmt(TimeSpan.FromSeconds(seg));
    }

    private void Fallo(string? detalle = null)
    {
        Cargando().IsVisible = false;
        this.FindControl<StackPanel>("panelFallo")!.IsVisible = true;
        Lbl("lblFallo").Text = string.IsNullOrWhiteSpace(detalle)
            ? Textos.Instancia.ReproductorCodecSinSaber
            : detalle;
        Mostrar();   // con el fallo delante los controles no se esconden
    }

    // ─────────────────────────── reproducción ───────────────────────────

    private void Alternar()
    {
        if (_mp is null) return;
        if (_pausado) { _mp.Play(); Glifo("glifoPlay", GlifoPausa); }
        else { _mp.Pause(); Glifo("glifoPlay", GlifoPlay); }
        _pausado = !_pausado;
        Mostrar();
    }

    private void Saltar(double segundos)
    {
        var barra = Sld("barra");
        if (_mp is null || barra.Maximum <= 0) return;

        var destino = Math.Clamp(barra.Value + segundos, 0, barra.Maximum);
        _mp.Time = (long)(destino * 1000);
        _desdeReloj = true;
        barra.Value = destino;
        _desdeReloj = false;
        Lbl("lblPos").Text = Fmt(TimeSpan.FromSeconds(destino));
        Mostrar();
    }

    private void AlternarMudo()
    {
        var v = Sld("volumen");
        if (_mudo) v.Value = _volumenPrevio > 0.02 ? _volumenPrevio : 0.8;
        else { _volumenPrevio = v.Value; v.Value = 0; }
    }

    private void AlternarPantalla()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal
                                                            : WindowState.FullScreen;
        Glifo("glifoPantalla", WindowState == WindowState.FullScreen ? GlifoContraer : GlifoExpandir);
        Mostrar();
    }

    private void AlPulsarTecla(object? remitente, KeyEventArgs e)
    {
        var salto = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 30 : 5;
        var vol = Sld("volumen");
        switch (e.Key)
        {
            case Key.Space: Alternar(); break;
            case Key.Left: Saltar(-salto); break;
            case Key.Right: Saltar(salto); break;
            case Key.Up: vol.Value = Math.Min(1, vol.Value + 0.05); Mostrar(); break;
            case Key.Down: vol.Value = Math.Max(0, vol.Value - 0.05); Mostrar(); break;
            case Key.F: AlternarPantalla(); break;
            case Key.M: AlternarMudo(); Mostrar(); break;
            case Key.Escape:
                if (WindowState == WindowState.FullScreen) AlternarPantalla(); else Close();
                break;
            default: return;
        }
        e.Handled = true;
    }

    // ─────────────────────────── controles que se apartan ───────────────────────────

    private void AlMoverRaton(object? remitente, PointerEventArgs e)
    {
        // Se filtran los movimientos de menos de 2 px: el ratón «se mueve» también cuando
        // se repinta bajo el cursor, y sin esto los controles no llegarían a esconderse.
        var p = e.GetPosition(this.FindControl<Grid>("raiz")!);
        if (Math.Abs(p.X - _ultimoRaton.X) + Math.Abs(p.Y - _ultimoRaton.Y) < 2) return;
        _ultimoRaton = p;
        Mostrar();
    }

    private bool RatonEnControles() =>
        this.FindControl<Grid>("capaInferior")!.IsPointerOver ||
        this.FindControl<Grid>("capaSuperior")!.IsPointerOver;

    private void Mostrar()
    {
        _apagon.Stop();
        _apagon.Start();
        if (_visible) return;
        _visible = true;
        Cursor = Cursor.Default;
        Fundir("capaSuperior", 1);
        Fundir("capaInferior", 1);
    }

    private void Ocultar()
    {
        if (!_visible || this.FindControl<StackPanel>("panelFallo")!.IsVisible) return;
        _visible = false;
        Cursor = new Cursor(StandardCursorType.None);
        Fundir("capaSuperior", 0);
        Fundir("capaInferior", 0);
    }

    private void Fundir(string capa, double destino)
    {
        var c = this.FindControl<Grid>(capa)!;
        c.IsHitTestVisible = destino > 0;
        // Transiciones y no una animación suelta: se declaran una vez y el propio control
        // interpola cada cambio de opacidad. Nada que arrancar ni que parar.
        c.Transitions ??= [new DoubleTransition
        {
            Property = OpacityProperty,
            Duration = TimeSpan.FromMilliseconds(180),
        }];
        c.Opacity = destino;
    }

    // ─────────────────────────── ficheros en la nube ───────────────────────────

    /// <summary>
    /// Con la sincronización «bajo demanda» el vídeo puede no estar en el disco: se descarga
    /// mientras se reproduce. Decirlo evita que parezca que el reproductor está roto. Da
    /// igual el proveedor — se miran los atributos, no quién los puso.
    /// </summary>
    private void RevisarNube()
    {
        if (!_eraMarcador) return;
        this.FindControl<Border>("chipNube")!.IsVisible = true;
        Lbl("lblNube").Text = Textos.Instancia.ReproductorChipNube;
    }

    // ─────────────────────────── la previa de la barra ───────────────────────────

    private void AlPasarPorLaBarra(object? remitente, PointerEventArgs e)
    {
        var barra = Sld("barra");
        var globo = Globo();
        if (barra.Maximum <= 0) { globo.IsVisible = false; return; }

        double x = Math.Clamp(e.GetPosition(barra).X, 0, barra.Bounds.Width);

        // Con la MISMA cuenta que usa la barra al colocarse, no con una regla de tres. El
        // recorrido util empieza y acaba a medio tirador de los bordes, y calcularlo a ojo
        // hacia que el globo prometiera un segundo y el clic cayera antes -hasta diecisiete
        // segundos en la primera mitad de un capitulo de media hora-. Ya paso una vez.
        double pulgar = barra.GetVisualDescendants().OfType<Thumb>().FirstOrDefault()?.Bounds.Width ?? 0;
        double seg = PosicionEnLaBarra.SegundosDeX(x, barra.Bounds.Width, pulgar, barra.Maximum);

        globo.IsVisible = true;
        // Centrado con el ancho REAL y sujeto a los bordes: un globo medio salido de la
        // ventana no se lee.
        double ancho = globo.Bounds.Width > 0 ? globo.Bounds.Width : 200;
        double alto = globo.Bounds.Height > 0 ? globo.Bounds.Height : 122;
        Canvas.SetLeft(globo, Math.Clamp(x - ancho / 2, 0, Math.Max(0, barra.Bounds.Width - ancho)));
        Canvas.SetTop(globo, -alto - 10);
        Lbl("lblPrevia").Text = Fmt(TimeSpan.FromSeconds(seg));

        int hueco = (int)(seg / HuecoPrevia) * HuecoPrevia;
        var img = this.FindControl<Image>("imgPrevia")!;
        if (_previas.TryGetValue(hueco, out var ya))
        {
            img.Source = ya;
            Lbl("lblPreviaCargando").IsVisible = false;
            return;
        }
        _previaPedida = hueco;
        Lbl("lblPreviaCargando").IsVisible = img.Source is null;
        _esperaPrevia.Stop();
        _esperaPrevia.Start();
    }

    /// <summary>
    /// Saca UN fotograma, el último que se pidió. Los de en medio se descartan a propósito:
    /// recorriendo la barra se piden decenas por segundo, y sacarlos todos dejaría la previa
    /// siempre por detrás del ratón.
    /// </summary>
    private async Task SacarPreviaAsync()
    {
        if (_corte.IsCancellationRequested || _sacandoPrevia || _previaPedida < 0) return;

        // Un fichero que aún está solo en la nube no se abre para sacarle un fotograma: eso
        // lo descargaría entero. Cuando termine de bajar, las previas salen solas.
        if (Ondine.Reindex.Nube.EsMarcador(_ruta))
        {
            Lbl("lblPreviaCargando").Text = Textos.Instancia.ReproductorPreviaEnNube;
            return;
        }

        int hueco = _previaPedida;
        _sacandoPrevia = true;
        try
        {
            var jpg = Path.Combine(CarpetaDePrevias(), $"{hueco}.jpg");
            if (await Engine.MakeThumbnailAsync(_ruta, jpg, hueco))
            {
                // Se lee a memoria y se borra el fichero: igual que en WPF, el temporal no
                // se queda abierto ni ocupando.
                Bitmap bmp;
                await using (var fs = File.OpenRead(jpg)) bmp = new Bitmap(fs);
                try { File.Delete(jpg); } catch { }

                _previas[hueco] = bmp;
                if (Globo().IsVisible && _previaPedida == hueco)
                {
                    this.FindControl<Image>("imgPrevia")!.Source = bmp;
                    Lbl("lblPreviaCargando").IsVisible = false;
                }
            }
        }
        catch { /* sin ffmpeg o fichero ilegible: la previa simplemente no sale */ }
        finally
        {
            _sacandoPrevia = false;
            // Mientras se sacaba esa, el ratón puede haberse ido a otro sitio. Salvo que la
            // ventana se haya cerrado por el camino: volver a arrancar el reloj aquí
            // lanzaba un ffmpeg con el reproductor ya cerrado y, si el fichero acababa de
            // devolverse a la nube, lo hacía bajar otra vez entero.
            if (!_corte.IsCancellationRequested && _previaPedida != hueco)
            {
                _esperaPrevia.Stop();
                _esperaPrevia.Start();
            }
        }
    }

    private string CarpetaDePrevias()
    {
        if (_tempPrevias != null) return _tempPrevias;
        _tempPrevias = Path.Combine(Path.GetTempPath(),
            $"ondine-repro-{Environment.ProcessId}-{Environment.TickCount}");
        Directory.CreateDirectory(_tempPrevias);
        return _tempPrevias;
    }

    /// <summary>Al cerrar: fuera los fotogramas de memoria y su carpeta del temporal.</summary>
    private void BorrarPrevias()
    {
        foreach (var b in _previas.Values) b.Dispose();
        _previas.Clear();
        if (_tempPrevias == null) return;
        try { Directory.Delete(_tempPrevias, recursive: true); } catch { }
        _tempPrevias = null;
    }

    private void AbrirEnSistema()
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(_ruta) { UseShellExecute = true });
        }
        catch { /* sin reproductor asociado no hay nada más que ofrecer */ }
    }
}
