using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibVLCSharp.Shared;
using Ondine.Localizacion;
using Avalonia.Platform.Storage;
using Ondine.Recortes;
using Ondine.Reindex;
using Path = System.IO.Path;
using Forma = Avalonia.Controls.Shapes.Path;
using Visual = Avalonia.Visual;


namespace Ondine.Ava;

/// <summary>Un tramo, tal como se ve en la lista de la derecha.</summary>
public sealed class TramoFila : INotifyPropertyChanged
{
    private string _nombre = "";
    public double Inicio { get; set; }
    public double Fin { get; set; }
    public int Numero { get; set; }
    public string Nombre
    {
        get => _nombre;
        set { _nombre = value; PropertyChanged?.Invoke(this, new(nameof(Nombre))); }
    }
    private bool _enCurso;
    /// <summary>Se está exportando ahora mismo: la tarjeta lo enseña.</summary>
    public bool EnCurso
    {
        get => _enCurso;
        set { _enCurso = value; PropertyChanged?.Invoke(this, new(nameof(EnCurso))); }
    }
    public double Duracion => Fin - Inicio;
    public string Rango => $"{Reloj(Inicio)} – {Reloj(Fin)}  ({Reloj(Duracion)})";

    /// <summary>
    /// Los extremos han cambiado (se ha arrastrado una junta). Se avisa a mano porque
    /// arrastrando se tocan sesenta veces por segundo y rehacer la lista entera a ese ritmo
    /// le quita el foco al campo del nombre en mitad de una palabra.
    /// </summary>
    public void Refrescar()
    {
        PropertyChanged?.Invoke(this, new(nameof(Rango)));
        PropertyChanged?.Invoke(this, new(nameof(Duracion)));
    }

    public static string Reloj(double s) =>
        s >= 3600 ? $"{(int)(s / 3600)}:{(int)(s % 3600 / 60):00}:{(int)(s % 60):00}"
                  : $"{(int)(s / 60)}:{(int)(s % 60):00}";

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Página «Recortes»: parte un vídeo en los trozos que hagan falta.
///
/// El modelo es UNO: la lista de tramos (ver <see cref="Tramos"/>). Se arranca con el vídeo
/// entero, «Cortar aquí» parte en dos el tramo donde va la reproducción, y quitar un tramo
/// lo descarta. Así «esto son dos capítulos» y «quítale este cacho» son la misma herramienta.
///
/// La salida NO reinventa nada: los mismos desplegables (<see cref="OpcionesSalida"/>), la
/// misma estimación (<see cref="Estimator"/>) y el mismo codificador que Comprimir — un tramo
/// se le pide al motor como unas opciones con «Desde» y «Duración».
/// </summary>
public partial class RecortesView : UserControl
{
    private readonly ObservableCollection<TramoFila> _tramos = new();
    private readonly Engine _engine = new();
    private readonly VideoDeLaPista _video = null!;

    /// <summary>
    /// Windows no sabe decodificar este vídeo, así que la previa grande son fotogramas
    /// sacados con ffmpeg. Cortar funciona igual: eso nunca pasó por aquí.
    /// </summary>
    private bool _modoFotogramas;

    private bool _sacandoFotogramaGrande;
    private int _fotogramaGrandePedido = -1;
    private readonly DispatcherTimer _esperaFotogramaGrande =
        new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly DispatcherTimer _reloj;
    private VideoRow? _fuente;
    private double _duracion;
    private bool _pausado = true;
    private CancellationTokenSource? _cancelar;
    private string _tramoActual = "";
    private string? _destino;      // null = junto al vídeo original
    private bool _exportando;
    private string? _tempMiniaturas;
    private bool _partirAlSaberDuracion;
    /// <summary>Cancela la descarga de un vídeo que estaba solo en la nube (Esc).</summary>
    private CancellationTokenSource? _cancelaDescarga;

    // ── ampliación de la línea de tiempo ──
    private const double ZoomMin = 1, ZoomMax = 40;
    private double _zoom = 1;
    // Fotogramas ya sacados, por segundo redondeado al hueco. Es la unica cache que hay y
    // se vacia entera al cambiar de video o al salir de la pagina.
    private readonly Dictionary<int, Bitmap> _previas = new();
    private readonly DispatcherTimer _esperaPrevia;
    private int _previaPedida = -1;
    private bool _sacandoPrevia;
    // El fondo de la pista pide sus fotogramas por esta cola; el que mira el cursor se cuela
    // por delante, porque es el unico que el usuario esta esperando de verdad.
    private readonly Queue<int> _colaFondo = new();
    private readonly List<(Image Celda, int Hueco)> _celdas = new();
    // Puntos de los que ffmpeg no ha sabido sacar fotograma. Se anotan para no pedirlos en bucle.
    private readonly HashSet<int> _sinFotograma = new();
    private readonly DispatcherTimer _esperaPista;

    /// <summary>Qué se está arrastrando con el ratón sobre la pista.</summary>
    private enum Agarre { Nada, Cabezal, Junta }

    private Agarre _agarre = Agarre.Nada;
    private int _juntaTramo;
    private Extremo _juntaExtremo;

    /// <summary>Se avisa al anfitrión para que lo escriba en el registro compartido.</summary>
    public event Action<string>? Log;

    /// <summary>
    /// Late al anfitrión el estado del export (activo, etiqueta corta). Con esto la ventana
    /// puede mostrar el indicador global de proceso desde cualquier pestaña: si estás en
    /// «Comprimir» u «Organizar» mientras Recortes exporta, sigues viendo que sigue vivo.
    /// </summary>
    internal event Action<bool, string>? EstadoProceso;

    public RecortesView()
    {
        // InitializeComponent y no AvaloniaXamlLoader.Load: es el que rellena los campos de
        // los elementos con nombre. Con Load, la pantalla revienta en la primera linea que
        // toca un control. Lo mismo que en Organizar.
        InitializeComponent();

        // El VideoView del XAML, envuelto para que la pantalla siga diciendo lo que decia:
        // _video.Position, _video.Play(), _video.Source. Ver VideoDeLaPista.
        _video = new VideoDeLaPista(this.FindControl<LibVLCSharp.Avalonia.VideoView>("video")!);
        listaTramos.ItemsSource = _tramos;


        LlenarDesplegables();
        chkSinRecodificar.IsCheckedChanged += (_, _) => AlCambiarSinRecodificar();

        foreach (var c in new[] { cboFmt, cboCodec, cboQ, cboRes, cboAud })
        {
            c.SelectedIndex = 0;
            c.SelectionChanged += (_, _) => RefrescarEstimacion();
        }
        // Los rótulos de estos desplegables se ponen desde código, así que no
        // se enteran solos del cambio de idioma como los del XAML: se vuelven
        // a llenar aquí. Sin desuscribir a propósito: esta vista es una sola y
        // vive lo que la ventana.
        Idioma.Cambio += (_, _) => LlenarDesplegables();

        btnElegir.Click += async (_, _) => await ElegirVideo();
        btnVaciar.Click += async (_, _) =>
        {
            if (_exportando) return;
            if (_tramos.Count > 1 && !(await Dialogo.Confirmar(Ventana,
                    Textos.Instancia.RecortesVaciarTitulo,
                    string.Format(Textos.Instancia.RecortesVaciarPregunta, _tramos.Count))))
                return;
            VaciarRecortes();
        };
        btnPlay.Click += (_, _) => Alternar();
        btnAtras.Click += (_, _) => Saltar(-10);
        btnAdelante.Click += (_, _) => Saltar(10);
        btnCortar.Click += (_, _) => CortarAqui();
        btnExportar.Click += async (_, _) => await ExportarAsync();
        btnPausarExp.Click += (_, _) => AlternarPausaExp();
        btnDetenerExp.Click += (_, _) => DetenerExportacion();
        btnDestino.Click += async (_, _) => await ElegirDestino();

        // Soltar un fichero encima. En Avalonia hay que declarar que se aceptan y los
        // datos se piden por su nombre, no por un formato con constantes de WPF.
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, (object? _, DragEventArgs e) =>
        {
            // Lo que llega son ficheros de almacenamiento, no un formato con constantes
            // como en WPF, y de ahi se saca la ruta — que puede no existir si lo soltado no
            // esta en el disco, asi que se filtra en vez de pasar una cadena vacia.
            //
            // En Avalonia 12 el contenido de un arrastre va en DataTransfer, igual que el
            // portapapeles: IDataObject y DataFormats estan jubilados.
            var ruta = e.DataTransfer?.TryGetFiles()?
                        .Select(x => x.TryGetLocalPath())
                        .FirstOrDefault(x => !string.IsNullOrEmpty(x));
            if (ruta is not null) Cargar(ruta);
        });

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _cancelaDescarga != null)
            { _cancelaDescarga.Cancel(); e.Handled = true; return; }
            if (_fuente == null) return;
            // Escribiendo el nombre de un tramo las teclas son letras, no atajos. Sin esto,
            // en ese campo no se podía ni poner un espacio ni escribir una «c»: se los comía
            // el atajo de cortar antes de que el cuadro de texto los viera.
            // Si el foco esta en una caja de texto, las teclas son suyas. En WPF se
            // preguntaba a Keyboard; aqui el propio arbol sabe quien tiene el foco.
            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox) return;
            switch (e.Key)
            {
                case Key.Space: Alternar(); break;
                case Key.Left: Saltar(-5); break;
                case Key.Right: Saltar(5); break;
                case Key.C: CortarAqui(); break;
                default: return;
            }
            e.Handled = true;
        };

        _video.DuracionConocida += _ =>
        {
            if (_video.Duracion <= TimeSpan.Zero) return;
            Duracion(_video.Duracion.TotalSeconds);
        };
        _video.Fallo += detalle =>
        {
            // Windows no sabe decodificar esto, pero ffmpeg sí: en vez de un fondo vacío
            // se enseñan fotogramas. Cortar nunca dependió de la previsualización -eso lo
            // hace ffmpeg también-, lo que faltaba era poder ver dónde cortas.
            _modoFotogramas = true;
            imgFotograma.IsVisible = true;

            // EL MOTIVO QUE LLEGA, y antes se tiraba. Esto decía siempre «este vídeo usa el
            // códec X y el reproductor de dentro no sabe decodificarlo», que en Linux o macOS
            // sin VLC es culpar al fichero de lo que le pasa al sistema. El mensaje bueno
            // -con la orden para instalar VLC- venía en el evento y no se leía.
            var codec = _fuente?.Codec ?? "";
            lblSinVideo.Text = _video.MotorAusente ? detalle
                : codec.Length > 0
                    ? string.Format(Textos.Instancia.RecortesModoFotogramas, codec)
                    : Textos.Instancia.RecortesSinPrevisualizacion;

            // La pastilla baja: ahora hay imagen detrás y taparla sería absurdo.
            chipSinVideo.VerticalAlignment = VerticalAlignment.Bottom;
            chipSinVideo.Margin = new Thickness(0, 0, 0, 16);
            chipSinVideo.IsVisible = true;

            PedirFotogramaGrande(_video.Position.TotalSeconds);
        };

        _esperaFotogramaGrande.Tick += async (_, _) =>
        {
            _esperaFotogramaGrande.Stop();
            await SacarFotogramaGrandeAsync();
        };

        _reloj = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _reloj.Tick += (_, _) =>
        {
            // Arrastrando manda el ratón: si el reloj sigue recolocando el cabezal, el
            // tirador y el cabezal se pelean por la misma línea y da tirones.
            if (_fuente == null || _exportando || _agarre != Agarre.Nada) return;
            Cabezal(_video.Position.TotalSeconds);
        };
        _reloj.Start();

        // Rebote: arrastrando se disparan decenas de posiciones por segundo y sacar un
        // fotograma cuesta ~200 ms. Se pide el ultimo, no todos.
        _esperaPrevia = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _esperaPrevia.Tick += async (_, _) => { _esperaPrevia.Stop(); await SacarPreviaAsync(); };

        // Al redimensionar la ventana la pista se repinta al momento (es barato), pero los
        // fotogramas del fondo esperan a que el usuario suelte: cada uno es una llamada a
        // ffmpeg y arrastrando el borde saldrían cientos.
        _esperaPista = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _esperaPista.Tick += (_, _) => { _esperaPista.Stop(); TenderFotogramas(); };

        pista.PointerPressed += (_, e) =>
        {
            if (_fuente == null || _duracion <= 0) return;
            _agarre = Agarre.Cabezal;
            // Avalonia captura sola el puntero mientras el boton esta pulsado, asi que
            // aqui no hay que pedirlo. En WPF si: sin CaptureMouse el arrastre se perdia al
            // salirse del control.
            Buscar(SegundoEn(e.GetPosition(pista).X));
        };
        pista.PointerMoved += AlPasarPorLaPista;
        pista.PointerReleased += (_, _) => SoltarLaPista();
        // En WPF se soltaba el agarre al perder la captura del raton. Avalonia captura
        // sola mientras el boton esta pulsado, asi que el equivalente util es soltar cuando
        // el puntero se va del control.
        pista.PointerCaptureLost += (_, _) => _agarre = Agarre.Nada;
        pista.PointerExited += (_, _) =>
        {
            if (_agarre == Agarre.Nada) globoPrevia.IsVisible = false;
        };

        SizeChanged += (_, _) => { AjustarAnchoPista(); PintarPista(); _esperaPista.Stop(); _esperaPista.Start(); };
        visorPista.SizeChanged += (_, _) => AjustarAnchoPista();
        visorPista.ScrollChanged += (_, e) => { if (Math.Abs(e.OffsetDelta.X) > 0.5) AlDesplazarPista(); };

        // Ctrl + rueda amplía; la rueda sola sigue siendo desplazarse, como en cualquier
        // editor. PointerWheelChanged para adelantarse al desplazamiento del propio visor.
        visorPista.PointerWheelChanged += (_, e) =>
        {
            if (e.KeyModifiers != KeyModifiers.Control) return;
            Ampliar(e.Delta.Y > 0 ? 1.25 : 1 / 1.25, e.GetPosition(visorPista).X);
            e.Handled = true;
        };
        // Ctrl+Z / Ctrl+Y para el historial; Ctrl + y Ctrl − para el aumento (Ctrl+0, entero).
        //
        // UN SOLO MANEJADOR PARA LOS DOS «DESHACER», y antes eran dos peleándose. Había otro
        // registrado ANTES que se quedaba el Ctrl+Z para recuperar el original de la papelera
        // y lo marcaba atendido, así que deshacer un corte no funcionaba nunca — y Ctrl+Mayús+Z
        // tampoco, porque ese manejador tampoco miraba el Mayús. Ahora el orden es el que
        // espera cualquiera: Ctrl+Z deshace lo último que hiciste, y solo cuando no queda nada
        // que deshacer se entiende como «recupera el original».
        //
        // Y EN TÚNEL: en burbuja, el control con el foco se los queda. Con el cursor en una
        // caja de texto -el nombre de un tramo- Ctrl+Z deshacía el texto y no el corte.
        AddHandler(KeyDownEvent, (object? _, KeyEventArgs e) =>
        {
            // HasFlag y no «==»: con «==» exacto, Ctrl+Mayús+Z (el rehacer de toda la vida)
            // no entraba nunca aquí.
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
            switch (e.Key)
            {
                case Key.Z when e.KeyModifiers.HasFlag(KeyModifiers.Shift): RehacerAccion(); e.Handled = true; break;
                // El historial primero; si está vacío, lo que se quiere deshacer es la
                // exportación, o sea recuperar el original de la papelera propia.
                case Key.Z when _atras.Count > 0: Deshacer(); e.Handled = true; break;
                case Key.Z: _ = DeshacerPapelera(); e.Handled = true; break;
                case Key.Y: RehacerAccion(); e.Handled = true; break;
                case Key.OemPlus or Key.Add: Ampliar(1.25); e.Handled = true; break;
                case Key.OemMinus or Key.Subtract: Ampliar(1 / 1.25); e.Handled = true; break;
                case Key.D0 or Key.NumPad0: Ampliar(ZoomMin / Math.Max(_zoom, 0.0001)); e.Handled = true; break;
            }
        }, RoutingStrategies.Tunnel);
        // Al salir de la página no se deja nada en memoria ni en disco temporal.
        Unloaded += (_, _) => LiberarMiniaturas();
    }

    /// <summary>
    /// Pone (o repone) los rótulos de los cinco desplegables de salida. Lo que
    /// el usuario tuviera elegido se respeta: al cambiar de idioma cambia el
    /// texto, no el ajuste.
    /// </summary>
    private void LlenarDesplegables()
    {
        Rellenar(cboFmt, OpcionesSalida.Formatos);
        Rellenar(cboCodec, OpcionesSalida.Codecs);
        Rellenar(cboQ, OpcionesSalida.Calidades);
        Rellenar(cboRes, OpcionesSalida.Resoluciones);
        Rellenar(cboAud, OpcionesSalida.Audios);

        static void Rellenar(ComboBox c, string[] opciones)
        {
            int elegido = c.SelectedIndex;
            c.ItemsSource = opciones;
            if (elegido >= 0) c.SelectedIndex = Math.Min(elegido, opciones.Length - 1);
        }
    }

    private bool _ajustandoAncho;

    // ── deshacer / rehacer ──
    // Rehacer() es el ÚNICO sitio por el que cambian los tramos, así que basta con guardar
    // ahí la foto anterior. Las dos pilas se manejan como en cualquier editor: una acción
    // nueva invalida la rama de rehacer.
    private readonly Stack<List<Tramo>> _atras = new();
    private readonly Stack<List<Tramo>> _adelante = new();
    private bool _pausadaExp;

    /// <summary>
    /// La pista mide el ancho del visor por el aumento. A 1× cabe entera; a 8× hay que
    /// desplazarse, y cada segundo ocupa ocho veces más — que es de lo que se trata.
    /// </summary>
    private void AjustarAnchoPista()
    {
        // Pestillo: cambiar el ancho fuerza un relayout, ese relayout puede mover el
        // ViewportWidth (aparece o desaparece la barra de desplazamiento) y eso vuelve a
        // llamar aquí. Sin el pestillo se realimenta hasta reventar la pila — la primera
        // versión mataba el proceso sin dejar ni un mensaje.
        if (_ajustandoAncho) return;
        double visible = visorPista.Viewport.Width;
        if (visible <= 0) return;
        double nuevo = Math.Round(visible * _zoom);
        if (Math.Abs(pista.Width - nuevo) < 0.5) return;

        _ajustandoAncho = true;
        try { pista.Width = nuevo; pista.UpdateLayout(); }
        finally { _ajustandoAncho = false; }
    }

    /// <summary>
    /// Amplía o reduce dejando quieto el segundo que hay bajo <paramref name="anclaX"/> (una
    /// x del VISOR). Es lo que hace que ampliar con la rueda se sienta natural: el punto que
    /// estás mirando no se te escapa de debajo del cursor.
    /// </summary>
    internal void Ampliar(double factor, double? anclaX = null)
    {
        double antes = _zoom;
        double nuevo = Math.Clamp(_zoom * factor, ZoomMin, ZoomMax);
        if (Math.Abs(nuevo - antes) < 0.001) return;

        double x = anclaX ?? visorPista.Viewport.Width / 2;
        // El segundo del ancla, ANTES de cambiar nada.
        double segAncla = SegundoEn(visorPista.Offset.X + x);

        _zoom = nuevo;
        AjustarAnchoPista();

        // Y se recoloca el desplazamiento para que ese segundo vuelva a caer en la misma x.
        // El UpdateLayout no es un adorno: ScrollToHorizontalOffset no surte efecto hasta el
        // siguiente pase de diseño, y girando la rueda deprisa llegan varios pasos dentro
        // del mismo fotograma — el segundo llamaría leyendo un desplazamiento viejo y el
        // punto anclado se iría de debajo del cursor (medido: 389 s de deriva en 5 pasos).
        visorPista.Offset = new Vector(Math.Max(0, XDe(segAncla) - x), visorPista.Offset.Y);
        visorPista.UpdateLayout();

        PintarPista();
        _esperaPista.Stop();
        _esperaPista.Start();
        lblZoom.Text = _zoom <= 1.01 ? "" : $"×{_zoom:0.#}";
    }

    /// <summary>
    /// Ya se sabe cuánto dura: la pista puede dibujarse y pedir sus fotogramas. La duración
    /// llega por dos caminos —el reproductor al abrir y el sondeo— y además el reproductor
    /// se recarga al acabar una exportación, así que esto se ejecuta varias veces sobre el
    /// mismo vídeo. Los tramos solo se rehacen si de verdad es otro material; si no, volver
    /// de exportar borraría los cortes que el usuario acaba de hacer.
    /// </summary>
    private void Duracion(double segundos)
    {
        if (segundos <= 0) return;
        bool otroVideo = Math.Abs(_duracion - segundos) > 0.01;
        _duracion = segundos;
        lblDur.Text = TramoFila.Reloj(_duracion);
        if (otroVideo || _tramos.Count == 0)
        {
            var inicial = Tramos.Entero(_duracion);
            // La junta solo se puede poner cuando ya se sabe cuánto dura el vídeo.
            if (_partirAlSaberDuracion)
            {
                _partirAlSaberDuracion = false;
                inicial = Tramos.Partir(inicial, _duracion / 2);
            }
            Rehacer(inicial);
        }
        else PintarPista();
        Cabezal(_video.Position.TotalSeconds);
        TenderFotogramas();
    }

    // ─────────────────────────── cargar ───────────────────────────

    private async Task ElegirVideo()
    {
        var elegido = await Selector.FicheroAsync(
            Ventana, Textos.Instancia.RecortesElegirVideoTitulo, "vídeo",
            [.. Engine.VideoExtensions.Select(e => "*" + e)]);
        if (elegido is not null) Cargar(elegido);
    }

    /// <summary>
    /// Carga un vídeo. Con <paramref name="partirPorLaMitad"/> llega ya con una junta puesta
    /// en el centro: es el caso de «este fichero trae dos episodios», donde lo único que
    /// falta es arrastrarla al sitio exacto.
    /// <para>Público porque Organizar abre aquí el fichero de una fila.</para>
    /// </summary>
    public async void Cargar(string ruta, bool partirPorLaMitad = false)
    {
        if (!File.Exists(ruta)) return;

        // Cargar otro vídeo mientras se exporta dejaría la exportación hablando de un
        // fichero que ya no está en pantalla. Y si hay cortes hechos, se van a perder: eso
        // se pregunta, no se hace.
        var dueno = Ventana;
        if (_exportando)
        {
            await Dialogo.Aviso(dueno, Textos.Instancia.RecortesTitulo,
                Textos.Instancia.RecortesExportandoAviso);
            return;
        }
        if (_fuente != null && !string.Equals(_fuente.Path, ruta, StringComparison.OrdinalIgnoreCase)
            && _tramos.Count > 1
            && !(await Dialogo.Confirmar(dueno, Textos.Instancia.RecortesTitulo,
                    string.Format(Textos.Instancia.RecortesCargarOtroPregunta,
                        _tramos.Count, Path.GetFileName(_fuente.Path), Environment.NewLine))))
            return;
        _partirAlSaberDuracion = partirPorLaMitad;

        var fi = new FileInfo(ruta);

        // Un vídeo que está solo en la nube se baja ENTERO antes de abrir nada. Trabajar
        // sobre el marcador a medias era la causa de dos males a la vez: las miniaturas y
        // la codificación iban a velocidad de red (la app parecía ahogada), y la
        // comprobación de «¿está libre?» del motor tropezaba con la propia descarga — el
        // export decía «SALTADO: Descargando» una y otra vez. Va ANTES de tocar ningún
        // estado: cancelar con Esc deja el proyecto anterior como estaba.
        if (NubeLocal.EsMarcador(ruta))
        {
            Ocupado(true, Textos.Instancia.RecortesDescargandoNube);
            barCarga.IsIndeterminate = false;
            lblCargaDet.Text = string.Format(Textos.Instancia.RecortesEscCancelar, Humano(fi.Length));
            _cancelaDescarga = new CancellationTokenSource();
            try
            {
                var avance = new Progress<double>(pr =>
                {
                    barCarga.Value = pr;
                    lblCarga.Text = string.Format(
                        Textos.Instancia.RecortesDescargandoNubePorcentaje, pr * 100);
                });
                await NubeLocal.DescargarAsync(ruta, avance, _cancelaDescarga.Token);
            }
            catch (OperationCanceledException)
            {
                Ocupado(false);
                Log?.Invoke(Textos.Instancia.RecortesLogDescargaCancelada);
                return;
            }
            catch (Exception ex)
            {
                Ocupado(false);
                Log?.Invoke(string.Format(
                    Textos.Instancia.RecortesLogNoSePudoDescargar, fi.Name, ex.Message));
                return;
            }
            finally { _cancelaDescarga = null; }
            fi.Refresh();
            Log?.Invoke(string.Format(
                Textos.Instancia.RecortesLogDescargado, fi.Name, Humano(fi.Length)));
        }
        _fuente = new VideoRow { Path = ruta, Bytes = fi.Length };
        lblVideo.Text = fi.Name;
        lblVideoDet.Text = Textos.Instancia.Analizando;
        chipSinVideo.IsVisible = false;
        _tramos.Clear();
        _duracion = 0;            // la del vídeo anterior no vale, y sin esto no se recalcula
        LiberarMiniaturas();      // los fotogramas del vídeo anterior no valen para este

        _video.Source = ruta;
        _video.Play();
        _video.Pause();          // primer fotograma a la vista, sin arrancar la reproducción
        _pausado = true;

        Ocupado(true, Textos.Instancia.RecortesAnalizandoVideo);
        _importando = true;   // el vigía anota a nombre del import cualquier atasco de aquí
        try
        {
            var info = await _engine.ProbeAsync(ruta);
            _fuente.Width = info.Width; _fuente.Height = info.Height; _fuente.Fps = info.Fps;
            _fuente.DurationSec = info.DurationSec;
            _fuente.VideoBitrateKbps = info.VideoBitrateKbps;
            _fuente.AudioBitrateKbps = info.AudioBitrateKbps;
            _fuente.Channels = info.Channels; _fuente.AudioCodec = info.AudioCodec;
            _fuente.Codec = info.Codec; _fuente.Probed = true;

            // Si el reproductor no ha sabido abrirlo, la duración la da el sondeo: cortar tiene
            // que seguir siendo posible aunque no se pueda previsualizar.
            if (_duracion <= 0) Duracion(info.DurationSec);

            lblVideoDet.Text = $"{info.Codec.ToUpperInvariant()} · {info.Width}×{info.Height} · " +
                               $"{Humano(fi.Length)}";
            lblDuracionTotal.Text = TramoFila.Reloj(_duracion);
            MostrarDestino();
        }
        finally { _importando = false; }

        Ocupado(false);
        btnCortar.IsEnabled = true;
        btnVaciar.IsVisible = true;   // ya hay algo cargado que se puede vaciar
        RefrescarEstimacion();
    }

    /// <summary>
    /// Deshabilita la página mientras se prepara el material. Trabajar con el vídeo a medio
    /// cargar no lleva a ningún sitio bueno, y sin aviso parece que la app se ha colgado.
    /// </summary>
    private void Ocupado(bool si, string que = "")
    {
        capaCarga.IsVisible = si;
        lblCarga.Text = que;
        lblCargaDet.Text = "";
        barCarga.Value = 0;
        barCarga.IsIndeterminate = si;
        btnElegir.IsEnabled = !si;
        btnCortar.IsEnabled = !si && _fuente != null;
        btnExportar.IsEnabled = false;      // lo recalcula RefrescarEstimacion al terminar
        listaTramos.IsEnabled = !si;
        pista.IsEnabled = !si;
        foreach (var b in new[] { btnPlay, btnAtras, btnAdelante }) b.IsEnabled = !si;
    }

    /// <summary>Cada cuántos segundos se guarda un fotograma. Más fino sería más lento.</summary>
    private const int HuecoPrevia = 5;

    /// <summary>El segundo del vídeo que hay bajo una x de la pista, y la vuelta.</summary>
    private double SegundoEn(double x) =>
        _duracion <= 0 ? 0 : Math.Clamp(x / Math.Max(1, pista.Bounds.Width) * _duracion, 0, _duracion);

    private double XDe(double segundo) =>
        _duracion <= 0 ? 0 : segundo / _duracion * pista.Bounds.Width;

    /// <summary>Cuántos tramos hay preparados. Solo para las pruebas.</summary>
    internal int NumTramos => _tramos.Count;

    /// <summary>El segundo que hay en una x de la pista. Solo para las pruebas.</summary>
    internal double SegundoBajo(double xPista) => SegundoEn(xPista);

    /// <summary>Enciende la capa de «exportando» sin exportar. Solo para las pruebas.</summary>
    internal void MostrarCapaExportando(bool si) => PintarExportando(si);

    /// <summary>
    /// De una x de la PISTA a una x de lo que se ve. Con la pista ampliada las dos dejan de
    /// coincidir, y el globo y el botón de cortar viven fuera del visor: sin restar el
    /// desplazamiento saldrían corridos justo cuando más precisión hace falta.
    /// </summary>
    private double XVisible(double xPista) => xPista - visorPista.Offset.X;

    /// <summary>
    /// Todo lo que pasa moviendo el ratón por la pista. La posición se saca del ratón y no
    /// del estado de ningún control: así funciona igual pasando por encima que arrastrando.
    ///
    /// Los tiradores y los bloques están DENTRO de la pista, así que este manejador recibe
    /// también sus movimientos al burbujear — que es lo que se quiere: arrastrando una junta
    /// el globo sigue enseñando el fotograma exacto por donde va a partir.
    /// </summary>
    private void AlPasarPorLaPista(object remitente, PointerEventArgs e)
    {
        if (_fuente == null || _duracion <= 0) { globoPrevia.IsVisible = false; return; }

        double x = Math.Clamp(e.GetPosition(pista).X, 0, pista.Bounds.Width);
        double seg = SegundoEn(x);

        switch (_agarre)
        {
            case Agarre.Junta: ArrastrarJunta(seg); break;
            case Agarre.Cabezal: Buscar(seg); break;
        }

        globoPrevia.IsVisible = true;

        // Centrado con el ancho REAL, y sujeto a los bordes de la pista: un globo medio
        // salido de la ventana no se lee.
        double ancho = globoPrevia.Bounds.Width > 0 ? globoPrevia.Bounds.Width : 200;
        double alto = globoPrevia.Bounds.Height > 0 ? globoPrevia.Bounds.Height : 132;
        Canvas.SetLeft(globoPrevia, Math.Clamp(XVisible(x) - ancho / 2, 0,
            Math.Max(0, visorPista.Viewport.Width - ancho)));
        Canvas.SetTop(globoPrevia, -alto - 8);
        lblPrevia.Text = TramoFila.Reloj(seg);

        int hueco = (int)(seg / HuecoPrevia) * HuecoPrevia;
        if (_previas.TryGetValue(hueco, out var ya))
        {
            imgPrevia.Source = ya;
            lblPreviaCargando.IsVisible = false;
            return;
        }
        _previaPedida = hueco;
        lblPreviaCargando.IsVisible = imgPrevia.Source == null;
        _esperaPrevia.Stop();
        _esperaPrevia.Start();
    }

    /// <summary>Se suelta el ratón: se acaba el arrastre.</summary>
    private void SoltarLaPista()
    {
        _agarre = Agarre.Nada;
        if (!pista.IsPointerOver) globoPrevia.IsVisible = false;
    }

    /// <summary>
    /// Dónde se dejan los jpg mientras se cargan. UNA por ejecución: cada uno se borra nada
    /// más leerlo, pero con una carpeta por vídeo cargado la carpeta vacía se quedaba en el
    /// temporal, y ahora la pista pide fotogramas siempre (antes solo si pasabas el ratón),
    /// así que se acumularían de verdad.
    /// </summary>
    private string CarpetaDeFotogramas()
    {
        if (_tempMiniaturas != null) return _tempMiniaturas;
        _tempMiniaturas = Path.Combine(Path.GetTempPath(), $"ondine-previa-{Environment.ProcessId}");
        Directory.CreateDirectory(_tempMiniaturas);
        BarrerCarpetasHuerfanas();
        return _tempMiniaturas;
    }

    /// <summary>
    /// Las carpetas que dejaran ejecuciones anteriores que se fueron sin recoger (un cierre
    /// forzado, un cuelgue). Si el proceso dueño sigue vivo no se toca: puede haber dos
    /// Ondine abiertos y no se le va a quitar el suelo al otro.
    /// </summary>
    private static void BarrerCarpetasHuerfanas()
    {
        try
        {
            foreach (var d in Directory.EnumerateDirectories(Path.GetTempPath(), "ondine-previa-*"))
            {
                // Las viejas llevaban un Guid en vez de un pid: esas no tienen dueño posible.
                if (int.TryParse(Path.GetFileName(d).Split('-')[^1], out var pid))
                {
                    if (pid == Environment.ProcessId) continue;
                    try { using (Process.GetProcessById(pid)) continue; }   // vive: no es huérfana
                    catch (ArgumentException) { }
                }
                try { Directory.Delete(d, true); } catch { }
            }
        }
        catch { /* barrer es cortesía: si no se puede, no rompe nada */ }
    }

    /// <summary>Ya se intentó: o está en la caché, o se sabe que de ahí no sale fotograma.</summary>
    private bool Intentado(int hueco) => _previas.ContainsKey(hueco) || _sinFotograma.Contains(hueco);

    /// <summary>
    /// El siguiente fotograma que hace falta. Manda el que está mirando el cursor: el fondo
    /// de la pista puede esperar, pero el globo lo tiene el usuario delante de los ojos.
    /// Devuelve -1 si no queda nada por sacar.
    ///
    /// MIRA la cola sin vaciarla, porque esto se llama también para preguntar «¿queda algo?»
    /// y una consulta no puede tirar trabajo pendiente.
    /// </summary>
    private int SiguienteHueco()
    {
        if (_previaPedida >= 0 && !Intentado(_previaPedida)) return _previaPedida;
        while (_colaFondo.Count > 0 && Intentado(_colaFondo.Peek())) _colaFondo.Dequeue();
        return _colaFondo.Count > 0 ? _colaFondo.Peek() : -1;
    }

    /// <summary>
    /// Saca el fotograma pedido. De uno en uno: encadenar ffmpeg por cada píxel del arrastre
    /// solo consigue una cola que llega tarde. El jpg se carga ENTERO en memoria y se borra
    /// al momento, así que en disco no queda nada.
    /// </summary>
    private async Task SacarPreviaAsync()
    {
        if (_exportando) return;   // ídem: nada de ffmpegs de miniaturas durante el export
        if (_sacandoPrevia || _fuente == null) return;
        int hueco = SiguienteHueco();
        if (hueco < 0) return;

        _sacandoPrevia = true;
        try
        {
            var jpg = Path.Combine(CarpetaDeFotogramas(), $"{hueco}.jpg");

            if (await Engine.MakeThumbnailAsync(_fuente.Path, jpg, hueco))
            {
                try
                {
                    // Se lee entera a memoria y el fichero queda libre. En WPF eran seis lineas con
                // BeginInit/EndInit/Freeze; aqui basta con abrir el flujo y cerrarlo.
                Bitmap bmp;
                await using (var fs = File.OpenRead(jpg)) bmp = new Bitmap(fs);
                    _previas[hueco] = bmp;
                    if (_previaPedida == hueco)
                    {
                        imgPrevia.Source = bmp;
                        lblPreviaCargando.IsVisible = false;
                    }
                    // El mismo fotograma puede alimentar varias celdas del fondo si el vídeo
                    // es corto y dos celdas caen en el mismo hueco.
                    foreach (var (celda, suyo) in _celdas)
                        if (suyo == hueco) celda.Source = bmp;
                }
                catch { _sinFotograma.Add(hueco); }
                finally { try { File.Delete(jpg); } catch { } }
            }
            // Si de ese punto no sale fotograma se anota y no se vuelve a pedir: sin esto el
            // globo lo reintenta cada 90 ms y no para nunca.
            else _sinFotograma.Add(hueco);
        }
        finally { _sacandoPrevia = false; }

        // Mientras se sacaba ese, el cursor ya estará en otro sitio y el fondo seguirá a
        // medias: se sigue con lo que quede.
        if (SiguienteHueco() >= 0) _esperaPrevia.Start();
    }

    /// <summary>Suelta los fotogramas y borra lo que quedara en disco. Nada se acumula.</summary>
    private void LiberarMiniaturas()
    {
        globoPrevia.IsVisible = false;
        imgPrevia.Source = null;

        // Otro vídeo, otra historia: el que venga puede decodificarse perfectamente.
        _modoFotogramas = false;
        _fotogramaGrandePedido = -1;
        _esperaFotogramaGrande.Stop();
        imgFotograma.Source = null;
        imgFotograma.IsVisible = false;
        chipSinVideo.VerticalAlignment = VerticalAlignment.Center;
        chipSinVideo.Margin = new Thickness(0);
        _previas.Clear();
        _previaPedida = -1;
        _colaFondo.Clear();
        _celdas.Clear();
        _sinFotograma.Clear();
        capaFotogramas.Children.Clear();
        if (_tempMiniaturas != null)
        {
            try { Directory.Delete(_tempMiniaturas, true); } catch { }
            _tempMiniaturas = null;
        }
    }

    // ─────────────────────────── tramos ───────────────────────────

    private List<Tramo> Actuales() =>
        _tramos.Select(t => new Tramo(t.Inicio, t.Fin, t.Nombre)).ToList();

    private void CortarAqui() => Rehacer(Tramos.Partir(Actuales(), _video.Position.TotalSeconds));

    private void OnQuitarTramo(object remitente, RoutedEventArgs e)
    {
        if (remitente is not Button { Tag: TramoFila f }) return;
        var i = _tramos.IndexOf(f);
        if (i >= 0) Rehacer(Tramos.Quitar(Actuales(), i));
    }

    /// <summary>
    /// Vuelca la lista de tramos a la vista. Los nombres que el usuario haya escrito se
    /// respetan; los que sigan siendo los sugeridos se recalculan, porque al cambiar el
    /// número de tramos el reparto de historias del nombre ya no es el mismo.
    /// </summary>
    private void Rehacer(IReadOnlyList<Tramo> nuevos, bool registrar = true)
    {
        // La foto de ANTES, para poder volver. Cargar un vídeo o deshacer no se registran:
        // el primero no tiene «antes» y el segundo ya gestiona las pilas él mismo.
        if (registrar && _tramos.Count > 0)
        {
            _atras.Push(_tramos.Select(f => new Tramo(f.Inicio, f.Fin, f.Nombre)).ToList());
            _adelante.Clear();
        }
        var escritos = _tramos.ToDictionary(t => (t.Inicio, t.Fin), t => t.Nombre);
        var baseNombre = _fuente != null
            ? Path.GetFileNameWithoutExtension(_fuente.Path) : "recorte";
        var sugeridos = Tramos.Nombrar(baseNombre, nuevos.Count);

        _tramos.Clear();
        for (int i = 0; i < nuevos.Count; i++)
        {
            var t = nuevos[i];
            escritos.TryGetValue((t.Inicio, t.Fin), out var previo);
            var fila = new TramoFila
            {
                Inicio = t.Inicio,
                Fin = t.Fin,
                Numero = i + 1,
                Nombre = string.IsNullOrWhiteSpace(previo) ? sugeridos[i] : previo,
            };
            // El bloque de la pista lleva escrito el nombre del fichero que va a salir: si se
            // reescribe en la lista de la derecha, la pista tiene que decir lo mismo.
            fila.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(TramoFila.Nombre)) PintarPista(); };
            _tramos.Add(fila);
        }
        btnExportar.Content = _tramos.Count switch
        {
            0 => Textos.Instancia.RecortesExportar,
            1 => Textos.Instancia.RecortesExportarUnTramo,
            _ => string.Format(Textos.Instancia.RecortesExportarVariosTramos, _tramos.Count),
        };
        lblAyudaTramos.Text = _tramos.Count switch
        {
            0 => Textos.Instancia.RecortesAyudaSinTramos,
            1 => Textos.Instancia.RecortesAyudaUnTramo,
            _ => string.Format(Textos.Instancia.RecortesAyudaVariosTramos, _tramos.Count),
        };
        PintarPista();
        RefrescarEstimacion();
    }

    // ─────────────────────────── la pista ───────────────────────────

    /// <summary>
    /// Dibuja la pista: lo que se descarta oscurecido, un bloque por tramo y un tirador por
    /// junta. Se rehace entera en cada cambio — son una docena de elementos y así no hay que
    /// llevar la cuenta de qué había antes, que es de donde salen los dibujos a medias.
    /// </summary>
    private void PintarPista()
    {
        capaBloques.Children.Clear();
        capaTiradores.Children.Clear();
        if (_duracion <= 0 || pista.Bounds.Width <= 0) return;
        double alto = pista.Bounds.Height;

        // Lo que NO se exporta, oscurecido: un hueco es un trozo que el usuario ha quitado y
        // tiene que verse de un vistazo que no va a salir en ningún fichero.
        double llevado = 0;
        foreach (var t in _tramos)
        {
            if (t.Inicio > llevado) Sombra(llevado, t.Inicio, alto);
            llevado = Math.Max(llevado, t.Fin);
        }
        if (llevado < _duracion) Sombra(llevado, _duracion, alto);

        for (int i = 0; i < _tramos.Count; i++) Bloque(i, alto);

        // Un tirador por junta. Dos tramos que se tocan comparten UNA, no dos: dibujar dos
        // tiradores pegados invita a separarlos y a abrir un agujero sin querer.
        for (int i = 0; i < _tramos.Count; i++)
        {
            bool pegadoAlAnterior = i > 0 && Math.Abs(_tramos[i - 1].Fin - _tramos[i].Inicio) < 1e-6;
            if (!pegadoAlAnterior) Tirador(i, Extremo.Inicio, _tramos[i].Inicio, alto);
            Tirador(i, Extremo.Fin, _tramos[i].Fin, alto);
        }
    }

    private void Sombra(double desde, double hasta, double alto)
    {
        var s = new Rectangle
        {
            Width = Math.Max(0, XDe(hasta) - XDe(desde)), Height = alto,
            Fill = new SolidColorBrush(Color.FromArgb(0xC4, 0x06, 0x07, 0x0D)),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(s, XDe(desde));
        capaBloques.Children.Add(s);
    }

    /// <summary>
    /// Un tramo. Lleva su número y su nombre encima porque la pista y la lista de la derecha
    /// hablan de lo mismo, y saber cuál es cuál sin contar bloques es media faena. La ✕ solo
    /// asoma al pasar por encima: fija en cada bloque, la pista se llena de aspas.
    /// </summary>
    private void Bloque(int indice, double alto)
    {
        var f = _tramos[indice];
        double x = XDe(f.Inicio), ancho = Math.Max(3, XDe(f.Fin) - XDe(f.Inicio));
        var dentro = new Grid();

        // Número y nombre van juntos arriba, como el rótulo de un clip: así el resto del
        // bloque queda despejado y se siguen viendo los fotogramas, que es para lo que están.
        var rotulo = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(6, 5, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (ancho >= 30)
            rotulo.Children.Add(new Border
            {
                Background = Pincel("Accent800"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Child = new TextBlock
                {
                    Text = f.Numero.ToString(), FontSize = 10, FontWeight = FontWeight.SemiBold,
                    Foreground = Pincel("Accent200"),
                },
            });

        // El hueco descontado deja sitio al número, a los márgenes y a la ✕ de quitar.
        if (ancho >= 130)
            rotulo.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xB0, 0, 0, 0)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(4, 0, 0, 0),
                MaxWidth = ancho - 68,
                Child = new TextBlock
                {
                    Text = f.Nombre, FontSize = 10, Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                },
            });
        dentro.Children.Add(rotulo);

        Button? quitar = null;
        if (ancho >= 46)
        {
            quitar = new Button
            {
                Theme = this.TryFindResource("QuitarBloque", out var tq) ? tq as ControlTheme : null,
                Tag = f,
                // Separada del borde para no pisar el tirador de la junta, que va justo ahí.
                Margin = new Thickness(0, 5, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                // En WPF era Hidden: invisible pero ocupando su hueco. Aqui IsVisible lo
                // quita tambien del hueco, y da igual porque el aspa va colocada por pixel
                // encima del bloque y su tamano no empuja a nadie.
                IsVisible = false,

            };
            ToolTip.SetTip(quitar, Textos.Instancia.RecortesQuitarTramoTip);
            quitar.Click += OnQuitarTramo;
            dentro.Children.Add(quitar);
        }

        var bloque = new Border
        {
            Width = ancho, Height = alto,
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(2),
            BorderBrush = Pincel("Accent"),
            Background = new SolidColorBrush(Color.FromArgb(0x22, 0x96, 0x8A, 0xE0)),
            Child = dentro,
        };
        if (quitar != null)
        {
            bloque.PointerEntered += (_, _) => quitar.IsVisible = true;
            bloque.PointerExited += (_, _) => quitar.IsVisible = false;
        }
        Canvas.SetLeft(bloque, x);
        capaBloques.Children.Add(bloque);
    }

    /// <summary>
    /// El tirador de una junta. La captura del ratón se la queda la PISTA, no el tirador:
    /// arrastrando se repinta la pista entera y el tirador de debajo del cursor deja de
    /// existir a media pasada — con la captura en él, el arrastre se cortaba en seco.
    /// </summary>
    private void Tirador(int indice, Extremo extremo, double segundo, double alto)
    {
        var rayas = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        for (int k = 0; k < 2; k++)
            rayas.Children.Add(new Rectangle
            {
                Width = 1.4, Height = Math.Min(14, alto - 20), RadiusX = 1, RadiusY = 1,
                Fill = Brushes.White, Opacity = 0.9, Margin = new Thickness(1.1, 0, 1.1, 0),
            });

        var t = new Border
        {
            Width = 10, Height = alto,
            CornerRadius = new CornerRadius(3),
            Background = Pincel("Accent"),
            Cursor = new Cursor(StandardCursorType.SizeWestEast),
            Child = rayas,

        };
        // Sujeto a la pista: los de los dos extremos del vídeo caen justo en el borde y, sin
        // esto, se quedan medio recortados y con la mitad de sitio donde agarrarlos.
        Canvas.SetLeft(t, Math.Clamp(XDe(segundo) - 5, 0, Math.Max(0, pista.Bounds.Width - 10)));
        t.PointerPressed += (_, e) =>
        {
            _agarre = Agarre.Junta;
            _juntaTramo = indice;
            _juntaExtremo = extremo;
            // Avalonia captura sola el puntero mientras el boton esta pulsado, asi que
            // aqui no hay que pedirlo. En WPF si: sin CaptureMouse el arrastre se perdia al
            // salirse del control.
            e.Handled = true;      // agarrar la junta no es además saltar la reproducción ahí
        };
        capaTiradores.Children.Add(t);
    }

    /// <summary>
    /// Arrastre en curso. Se tocan los números del tramo EN EL SITIO y se repinta la pista,
    /// pero no se rehace la lista de la derecha: reconstruirla sesenta veces por segundo le
    /// quita el foco al campo del nombre y no se podría ni escribir mientras.
    /// </summary>
    private void ArrastrarJunta(double segundo)
    {
        var nuevos = Tramos.MoverJunta(Actuales(), _juntaTramo, _juntaExtremo, segundo, _duracion);
        if (nuevos.Count != _tramos.Count) return;
        for (int i = 0; i < nuevos.Count; i++)
        {
            _tramos[i].Inicio = nuevos[i].Inicio;
            _tramos[i].Fin = nuevos[i].Fin;
            _tramos[i].Refrescar();
        }
        PintarPista();
        RefrescarEstimacion();
    }

    /// <summary>
    /// El cabezal, y con él el botón de cortar: la tijera va donde va a partir, no en una
    /// esquina de la pantalla.
    /// </summary>
    private void Cabezal(double segundo)
    {
        lblPos.Text = TramoFila.Reloj(segundo);
        if (_duracion <= 0 || pista.Bounds.Width <= 0) return;

        double x = XDe(Math.Clamp(segundo, 0, _duracion));
        cabezal.IsVisible = true;
        Canvas.SetLeft(cabezal, x - 2);

        btnCortar.IsVisible = true;
        double ancho = btnCortar.Bounds.Width > 0 ? btnCortar.Bounds.Width : 24;
        Canvas.SetLeft(btnCortar, Math.Clamp(XVisible(x) - ancho / 2, 0,
            Math.Max(0, visorPista.Viewport.Width - ancho)));
        Canvas.SetTop(btnCortar, 1);
    }

    /// <summary>Mueve la reproducción, y con ella el cabezal.</summary>
    private void Buscar(double segundo)
    {
        if (_fuente == null) return;
        var donde = Math.Clamp(segundo, 0, _duracion);
        _video.Position = TimeSpan.FromSeconds(donde);
        Cabezal(donde);

        // Es el único sitio por el que se salta, así que es el único que necesita
        // enterarse de que aquí la imagen la pinta ffmpeg.
        if (_modoFotogramas) PedirFotogramaGrande(donde);
    }

    private void PedirFotogramaGrande(double segundo)
    {
        _fotogramaGrandePedido = (int)Math.Max(0, segundo);
        _esperaFotogramaGrande.Stop();
        _esperaFotogramaGrande.Start();
    }

    /// <summary>
    /// Saca UN fotograma, el último pedido. Misma disciplina que la tira del fondo y que
    /// el globo: los de en medio se descartan, el jpg se carga entero en memoria y se
    /// borra del disco al momento.
    /// </summary>
    private async Task SacarFotogramaGrandeAsync()
    {
        if (_sacandoFotogramaGrande || _fuente == null || _fotogramaGrandePedido < 0) return;
        if (_exportando) return;   // nada de ffmpegs de previa mientras se exporta

        int seg = _fotogramaGrandePedido;
        _sacandoFotogramaGrande = true;
        try
        {
            var jpg = Path.Combine(CarpetaDeFotogramas(), $"grande-{seg}.jpg");
            if (await Engine.MakeThumbnailAsync(_fuente.Path, jpg, seg, 1280))
            {
                // Se lee entera a memoria y el fichero queda libre. En WPF eran seis lineas con
                // BeginInit/EndInit/Freeze; aqui basta con abrir el flujo y cerrarlo.
                Bitmap bmp;
                await using (var fs = File.OpenRead(jpg)) bmp = new Bitmap(fs);
                imgFotograma.Source = bmp;
                try { File.Delete(jpg); } catch { }
            }
        }
        catch { /* un fotograma que no sale no rompe nada: se sigue pudiendo cortar */ }
        finally
        {
            _sacandoFotogramaGrande = false;
            if (_fotogramaGrandePedido != seg) { _esperaFotogramaGrande.Stop(); _esperaFotogramaGrande.Start(); }
        }
    }

    /// <summary>
    /// Tiende los fotogramas del fondo. Van por la MISMA caché y la misma disciplina que el
    /// globo: cada jpg se carga entero en memoria, se borra del disco al momento y todo se
    /// suelta al cambiar de vídeo. Redondear al mismo hueco de 5 s hace que el fondo y el
    /// globo se aprovechen los fotogramas el uno al otro en vez de sacarlos dos veces.
    /// </summary>
    private void TenderFotogramas()
    {
        if (_exportando) return;   // no robarle lecturas al fichero que se está codificando
        capaFotogramas.Children.Clear();
        _celdas.Clear();
        _colaFondo.Clear();
        if (_fuente == null || _duracion <= 0 || pista.Bounds.Width <= 0) return;

        double alto = pista.Bounds.Height;
        double ancho = Math.Round(alto * 16 / 9.0);          // una celda ≈ un fotograma 16:9
        int total = Math.Max(1, (int)Math.Ceiling(pista.Bounds.Width / ancho));

        // Solo se tienden las celdas que se VEN, más un margen a cada lado para que al
        // desplazarse no aparezcan vacías. A 40× la pista mide decenas de miles de píxeles:
        // tenderla entera serían cientos de extracciones de fotograma para nada.
        const int margen = 3;
        int desde = Math.Max(0, (int)(visorPista.Offset.X / ancho) - margen);
        int hasta = Math.Min(total, (int)Math.Ceiling(
            (visorPista.Offset.X + Math.Max(visorPista.Viewport.Width, 1)) / ancho) + margen);

        for (int k = desde; k < hasta; k++)
        {
            double centro = (k + 0.5) * ancho / pista.Bounds.Width * _duracion;
            int hueco = (int)(Math.Clamp(centro, 0, _duracion) / HuecoPrevia) * HuecoPrevia;

            var celda = new Image
            {
                Width = ancho, Height = alto, Stretch = Stretch.UniformToFill,
                Opacity = 0.85,     // el fondo sitúa; los bloques y el cabezal son lo que se lee
            };
            _previas.TryGetValue(hueco, out var ya);
            celda.Source = ya;
            Canvas.SetLeft(celda, k * ancho);
            capaFotogramas.Children.Add(celda);
            _celdas.Add((celda, hueco));
            if (ya == null) _colaFondo.Enqueue(hueco);
        }

        if (SiguienteHueco() >= 0) _esperaPrevia.Start();
    }

    /// <summary>Suspende o reanuda ffmpeg donde va. Reanudar sigue, no reempieza.</summary>
    private void AlternarPausaExp()
    {
        if (!_exportando) return;
        _pausadaExp = !_pausadaExp;
        if (_pausadaExp)
        {
            _engine.Pause();
            btnPausarExp.Content = Textos.Instancia.RecortesReanudar;
            lblProgreso.Text = Textos.Instancia.Pausado;
        }
        else
        {
            _engine.Resume();
            btnPausarExp.Content = Textos.Instancia.RecortesPausar;
            lblProgreso.Text = _tramoActual;
        }
    }

    /// <summary>
    /// Corta la exportación. Si estaba en pausa se reanuda ANTES de cancelar: un proceso
    /// suspendido no puede atender su propia muerte, y quedaría vivo de fondo — que es
    /// exactamente lo que no se quiere.
    /// </summary>
    private void DetenerExportacion()
    {
        if (!_exportando) return;
        if (_pausadaExp)
        {
            _engine.Resume();
            _pausadaExp = false;
            btnPausarExp.Content = Textos.Instancia.RecortesPausar;
        }
        lblProgreso.Text = Textos.Instancia.RecortesDeteniendo;
        btnDetenerExp.IsEnabled = false;
        _cancelar?.Cancel();
    }

    // ── chivato de fluidez ──
    // «La app va a tirones al exportar» es real para quien lo ve e invisible en un banco: aquí
    // no se reprodujo nunca (arrastrar sin exportar y arrastrar exportando dan lo MISMO). Así
    // que se mide en la máquina de verdad, en los DOS hilos que pueden causar un tirón:
    //   · entrada (Dispatcher): si se para >200 ms, se anota al momento.
    //   · render (CompositionTarget): se acumulan los fotogramas y al terminar se resume qué
    //     tan fluido fue. Un tirón que no salga en ninguno de los dos es de FUERA del proceso
    //     (grabador de pantalla, memoria, DWM), y ahí la pista está fuera de la app.
    private DispatcherTimer? _vigia;
    private readonly System.Diagnostics.Stopwatch _vigiaCrono = new();
    private readonly System.Diagnostics.Stopwatch _marcoCrono = new();
    private readonly List<double> _marcos = new();
    private bool _midiendoRender;

    // Vigía SIEMPRE activo (mientras la página se ve), no solo al exportar: mide si el hilo de
    // la interfaz se queda sin responder durante el IMPORT o en reposo — el síntoma que el
    // usuario reporta («la interfaz responde tarde»). Durante el export no duplica: ese lo
    // cubre VigilarBloqueos con su propio umbral.
    private DispatcherTimer? _vigiaSiempre;
    private readonly System.Diagnostics.Stopwatch _vigiaSiempreCrono = new();
    private bool _importando;
    private bool _tierRegistrado;

    private void VigilarBloqueos(bool si)
    {
        if (si)
        {
            _vigia ??= CrearVigia();
            _vigiaCrono.Restart();
            _vigia.Start();
            _marcos.Clear();
            _marcoCrono.Restart();
            _midiendoRender = true;
            PedirFotogramaDeMedida();
        }
        else
        {
            _vigia?.Stop();
            if (_midiendoRender)
            {
                _midiendoRender = false;
                ResumirRender();
            }
        }
    }

    /// <summary>
    /// Mide cuánto tarda en pintarse cada fotograma.
    ///
    /// <para>
    /// En WPF se enganchaba a <c>CompositionTarget.Rendering</c>, que avisa en cada fotograma
    /// hasta que te desengaches. Avalonia lo hace al revés: se PIDE un fotograma y avisa una
    /// vez, así que hay que volver a pedirlo.
    /// </para>
    /// <para>
    /// Sale mejor de lo que parece: una medición que se olvide de pararse <b>deja de medir
    /// sola</b> en vez de seguir despertando a la interfaz para siempre — que es la forma en
    /// la que esta clase de vigilancia se convierte en el problema que venía a medir.
    /// </para>
    /// </summary>
    private void PedirFotogramaDeMedida()
    {
        if (!_midiendoRender) return;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ =>
        {
            if (!_midiendoRender) return;
            _marcos.Add(_marcoCrono.Elapsed.TotalMilliseconds);
            _marcoCrono.Restart();
            PedirFotogramaDeMedida();
        });
    }

    /// <summary>
    /// Vigía del hilo de la interfaz mientras la página está a la vista. El timer late cada
    /// 40 ms; si un tick se retrasa más de 120 ms es que el hilo estuvo bloqueado ese tiempo
    /// —y ahí es donde se siente que «responde tarde»—. Anota cuándo pasó y qué se estaba
    /// haciendo, para saber si el freno es el import, el reposo o algo de fuera.
    /// </summary>
    private void VigilanciaContinua(bool si)
    {
        if (si)
        {
            _vigiaSiempre ??= CrearVigiaContinuo();
            _vigiaSiempreCrono.Restart();
            _vigiaSiempre.Start();
        }
        else _vigiaSiempre?.Stop();
    }

    /// <summary>
    /// Anota UNA vez el nivel de aceleración gráfica de WPF. Si sale 0, la app está pintando
    /// por SOFTWARE (sin GPU) y por eso todo va lento pase lo que pase — es una causa distinta
    /// del ffmpeg y hay que descartarla. 2 = GPU completa; 1 = parcial.
    /// </summary>
    private void RegistrarAceleracion()
    {
        if (_tierRegistrado) return;
        _tierRegistrado = true;

        // AQUÍ NO SE ANOTA NADA, Y ES A PROPÓSITO.
        //
        // En WPF esto leía RenderCapability.Tier, que dice si se está pintando por software
        // (0), con GPU parcial (1) o completa (2). Es útil: un 0 explica que todo vaya lento
        // pase lo que pase, y es una causa distinta del ffmpeg que hay que poder descartar.
        //
        // Avalonia no expone nada equivalente — su motor de dibujo no publica un nivel. Se
        // podría mirar qué plataforma de render hay cargada y traducirlo a un número, pero
        // eso sería INVENTARSE la medida, y un cero falso en el registro manda a alguien a
        // buscar un problema de GPU que no existe. Mejor no decir nada que decir algo que no
        // se ha medido.
        //
        // Lo que sí queda es el resto de la vigilancia: los tiempos de fotograma y los
        // bloqueos del hilo, que se miden de verdad.
    }

    private DispatcherTimer CrearVigiaContinuo()
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        t.Tick += (_, _) =>
        {
            var gap = _vigiaSiempreCrono.ElapsedMilliseconds;
            // El export tiene su propio vigía (VigilarBloqueos); aquí no se duplica.
            if (!_exportando && gap > 120)
            {
                string donde = _importando
                    ? Textos.Instancia.RecortesLogImportando
                    : Textos.Instancia.RecortesLogEnReposo;
                Log?.Invoke(string.Format(
                    Textos.Instancia.RecortesLogInterfazSinResponder, gap, donde));
            }
            _vigiaSiempreCrono.Restart();
        };
        return t;
    }

    private void ResumirRender()
    {
        if (_marcos.Count < 30) return;   // muy corto para decir nada
        _marcos.Sort();
        double P(double q) => _marcos[Math.Min(_marcos.Count - 1, (int)(_marcos.Count * q))];
        int lentos = _marcos.Count(x => x > 33.4);   // por debajo de 30 fps
        double pct = 100.0 * lentos / _marcos.Count;
        // Solo se anota si de verdad hubo tela: si fue fluido, no se ensucia el Registro.
        if (pct >= 15 || P(.99) > 80)
            Log?.Invoke(string.Format(Textos.Instancia.RecortesLogFluidez, P(.5), P(.99), pct));
    }

    private DispatcherTimer CrearVigia()
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        t.Tick += (_, _) =>
        {
            var gap = _vigiaCrono.ElapsedMilliseconds;
            if (gap > 200)
                Log?.Invoke(string.Format(Textos.Instancia.RecortesLogEntradaBloqueada, gap));
            _vigiaCrono.Restart();
        };
        return t;
    }

    /// <summary>La cara de «se está exportando»: la capa sobre el vídeo y los botones.</summary>
    private void PintarExportando(bool si)
    {
        capaExportando.IsVisible = si;
        btnPausarExp.IsVisible = btnDetenerExp.IsVisible = si;
        btnPausarExp.IsEnabled = btnDetenerExp.IsEnabled = si;
        btnPausarExp.Content = Textos.Instancia.RecortesPausar;
        VigilarBloqueos(si);
    }

    /// <summary>Ctrl+Z: vuelve al estado anterior de los tramos.</summary>
    private void Deshacer()
    {
        if (_exportando || _atras.Count == 0) return;
        _adelante.Push(_tramos.Select(f => new Tramo(f.Inicio, f.Fin, f.Nombre)).ToList());
        Rehacer(_atras.Pop(), registrar: false);
        Log?.Invoke(Textos.Instancia.RecortesLogDeshecho);
    }

    /// <summary>Ctrl+Y (o Ctrl+Mayús+Z): vuelve a aplicar lo deshecho.</summary>
    private void RehacerAccion()
    {
        if (_exportando || _adelante.Count == 0) return;
        _atras.Push(_tramos.Select(f => new Tramo(f.Inicio, f.Fin, f.Nombre)).ToList());
        Rehacer(_adelante.Pop(), registrar: false);
        Log?.Invoke(Textos.Instancia.RecortesLogRehecho);
    }

    /// <summary>Al desplazarse hay celdas nuevas a la vista: se tienden con un respiro.</summary>
    private void AlDesplazarPista()
    {
        Cabezal(_video.Position.TotalSeconds);
        _esperaPista.Stop();
        _esperaPista.Start();
    }

    // ─────────────────────────── reproducción ───────────────────────────

    private void Alternar()
    {
        if (_fuente == null) return;
        if (_pausado) { _video.Play(); glifoPlay.Data = Geometry.Parse("M6,3 L6,17 M13,3 L13,17"); glifoPlay.Fill = null; }
        else { _video.Pause(); glifoPlay.Data = Geometry.Parse("M6.5,3.5 L17,10 L6.5,16.5 Z"); glifoPlay.Fill = Brushes.White; }
        _pausado = !_pausado;
    }

    /// <summary>
    /// La página entra o sale de pantalla (cambio de pestaña). Un tab oculto ya no se dibuja
    /// —de eso se encarga WPF con false—, pero SÍ seguiría trabajando: el reloj
    /// del cabezal late, los previsualizadores gotean fotogramas y, si dejaste el vídeo en
    /// marcha, sigue decodificando y sonando. Al ocultarse se para todo eso; al volver se
    /// reanuda lo que toque.
    /// </summary>
    public void EnPantalla(bool visible)
    {
        if (visible)
        {
            RegistrarAceleracion();
            VigilanciaContinua(true);
            // Siempre, no solo si ya hay vídeo: si entras a la página y cargas uno después,
            // el reloj tiene que estar ya corriendo. Su tick se ignora solo cuando no hay
            // vídeo, así que dejarlo activo no cuesta nada.
            _reloj.Start();
        }
        else
        {
            VigilanciaContinua(false);
            _reloj.Stop();
            _esperaPrevia.Stop();
            _esperaPista.Stop();
            // Un vídeo reproduciéndose en un tab que ya no miras solo gasta: se pausa (y el
            // botón queda en «play», que es lo coherente al volver).
            if (!_pausado && _fuente != null)
            {
                _video.Pause();
                glifoPlay.Data = Geometry.Parse("M6.5,3.5 L17,10 L6.5,16.5 Z");
                glifoPlay.Fill = Brushes.White;
                _pausado = true;
            }
        }
    }

    private void Saltar(double s)
    {
        if (_fuente == null) return;
        Buscar(_video.Position.TotalSeconds + s);
    }

    // ─────────────────────────── salida ───────────────────────────

    private EncodeOptions Opciones() => OpcionesSalida.Construir(
        cboFmt.SelectedIndex, cboCodec.SelectedIndex, cboQ.SelectedIndex,
        cboRes.SelectedIndex, cboAud.SelectedIndex);

    /// <summary>Si se va a copiar en vez de recodificar.</summary>
    private bool SinRecodificar => chkSinRecodificar.IsChecked == true;

    /// <summary>
    /// La extension que tendria la salida con los ajustes de la fila. Copiar exige que sea la
    /// misma del original: los mismos paquetes en otra caja no siempre caben.
    /// </summary>
    private string ExtensionElegida() => "." + OpcionesSalida.Formatos[cboFmt.SelectedIndex].ToLowerInvariant();

    private void AlCambiarSinRecodificar()
    {
        // Copiando no se aplica NINGUNO de esos ajustes. Dejarlos encendidos haria creer
        // que si, y el trozo saldria distinto de lo que la fila prometia.
        filaAjustes.IsEnabled = !SinRecodificar;
        RefrescarEstimacion();
        _ = AvisarDelDesfaseAsync();
    }

    /// <summary>
    /// Dice de antemano cuanto se va a mover el arranque del primer tramo.
    ///
    /// <para>
    /// Es la mitad del valor de esta funcion: cortar sin recodificar es rapido y exacto en
    /// calidad, pero el corte solo cae en un fotograma clave. Enterarse DESPUES, mirando el
    /// fichero, es lo que hace desconfiar de una herramienta.
    /// </para>
    /// </summary>
    private async Task AvisarDelDesfaseAsync()
    {
        if (!SinRecodificar || _fuente == null || _tramos.Count == 0)
        { lblSinRecodificar.Text = ""; return; }

        if (!CorteSinRecodificar.SePuedeCopiar(Path.GetExtension(_fuente.Path), ExtensionElegida()))
        { lblSinRecodificar.Text = Textos.Instancia.RecortesSinRecodificarOtroFormato; return; }

        var inicio = _tramos[0].Inicio;
        var ruta = _fuente.Path;

        // El indice se lee de una ventana, no del fichero entero, asi que esto es inmediato
        // incluso en una pelicula larga.
        var claves = await Task.Run(() => FotogramasClave.AntesDeAsync(ruta, inicio));

        // Mientras se leia, el usuario ha podido desmarcar o mover el tramo.
        if (!SinRecodificar || _tramos.Count == 0 || _tramos[0].Inicio != inicio) return;

        var cae = CorteSinRecodificar.DondeCae(claves, inicio);
        lblSinRecodificar.Text = !cae.SeSabe ? ""
            : cae.SeMueve
                ? string.Format(Textos.Instancia.RecortesSinRecodificarDesfase, cae.Desfase.ToString("0.0"))
                : Textos.Instancia.RecortesSinRecodificarSinDesfase;
    }

    /// <summary>
    /// La estimación es la MISMA que la de Comprimir, escalada a lo que se va a exportar: si
    /// de 20 minutos se guardan 10, sale la mitad. Nada de una segunda fórmula.
    /// </summary>
    private void RefrescarEstimacion()
    {
        btnExportar.IsEnabled = _fuente is { Probed: true } && _tramos.Count > 0 && _cancelar == null;
        if (_fuente is not { Probed: true } || _duracion <= 0 || _tramos.Count == 0)
        {
            lblEst.Text = "—";
            lblEstDet.Text = Textos.Instancia.RecortesEstimacionSinVideo;
            return;
        }

        // Copiando no hay nada que estimar: el trozo pesa lo que pesa su parte del original.
        // Enseñar aquí una estimación de compresión sería inventarse un número.
        if (SinRecodificar)
        {
            double trozo = _tramos.Sum(t => t.Duracion) / _duracion;
            lblEst.Text = "≈ " + Humano((long)((_fuente.Bytes) * trozo));
            lblEstDet.Text = Textos.Instancia.RecortesSinRecodificarActivo;
            return;
        }

        var est = Estimator.Compute(_fuente, Opciones());
        if (!est.Valid)
        {
            lblEst.Text = "—";
            lblEstDet.Text = Textos.Instancia.RecortesEstimacionImposible;
            return;
        }

        double parte = _tramos.Sum(t => t.Duracion) / _duracion;
        long bytes = (long)(est.EstBytes * parte);
        lblEst.Text = "≈ " + Humano(bytes);
        lblEstDet.Text = string.Format(
            _tramos.Count == 1
                ? Textos.Instancia.RecortesEstimacionDetalleUno
                : Textos.Instancia.RecortesEstimacionDetalle,
            _tramos.Count, TramoFila.Reloj(_tramos.Sum(t => t.Duracion)));
    }

    private async Task ElegirDestino()
    {
        var elegida = await Selector.CarpetaAsync(
            Ventana, Textos.Instancia.RecortesDondeDejarTitulo, CarpetaDestino());
        if (elegida is null) return;

        _destino = elegida;
        MostrarDestino();
    }

    /// <summary>Por defecto, junto al original: es donde el usuario está mirando.</summary>
    private string CarpetaDestino() =>
        _destino ?? (_fuente != null ? Path.GetDirectoryName(_fuente.Path)! : "");

    private void MostrarDestino()
    {
        var c = CarpetaDestino();
        lblDestino.Text = c.Length == 0 ? ""
            : string.Format(Textos.Instancia.RecortesSeGuardaraEn, c)
              + (_destino == null ? Textos.Instancia.RecortesJuntoAlOriginal : "");
    }

    /// <summary>
    /// Ctrl+Z: recupera el último original enviado a la Papelera propia (lo devuelve a su sitio en
    /// disco). El proyecto ya se cerró, así que solo restaura el fichero — se vuelve a abrir para editarlo.
    /// </summary>
    private async Task DeshacerPapelera()
    {
        var nombre = Reindex.PapeleraApp.DeshacerUltimo();
        lblProgreso.Text = nombre != null
            ? string.Format(Textos.Instancia.RecortesRecuperado, nombre)
            : Textos.Instancia.RecortesNadaQueRecuperar;
        if (nombre != null)
            Log?.Invoke(string.Format(Textos.Instancia.RecortesLogRecuperado, nombre));
    }

    /// <summary>
    /// Con los tramos ya en disco, ofrece deshacerse del original — que es lo que casi
    /// siempre quieres tras partir un vídeo, y buscarlo a mano después es una lata.
    ///
    /// Va a la PAPELERA, no a borrado directo: la comprobación de que los tramos existen
    /// dice que los ficheros están, no que estén bien. Si al verlos algo falla, el original
    /// sigue ahí. Devuelve true solo si el fichero se ha ido de verdad.
    /// </summary>
    private async Task<bool> OfrecerBorrarOriginal(string ruta, int cuantos, string destino)
    {
        if (!File.Exists(ruta)) return false;

        var fi = new FileInfo(ruta);
        var dueno = Ventana;
        if (!await Dialogo.Confirmar(dueno, Textos.Instancia.RecortesBorrarOriginalTitulo,
                string.Format(Textos.Instancia.RecortesBorrarOriginalPregunta,
                    cuantos, destino, fi.Name, Humano(fi.Length), Environment.NewLine),
                Textos.Instancia.RecortesSiPapelera, Textos.Instancia.RecortesNoConservar))
            return false;

        // Papelera PROPIA de la app (no la de Windows): recuperable con Ctrl+Z de forma fiable;
        // al acumularse o cerrar la app, se finaliza en la Papelera del sistema.
        if (Reindex.PapeleraApp.Enviar(ruta) == null)
        {
            Log?.Invoke(string.Format(Textos.Instancia.RecortesLogNoSePudoEnviarPapelera, fi.Name));
            await Dialogo.Aviso(dueno, Textos.Instancia.RecortesTitulo,
                string.Format(Textos.Instancia.RecortesNoSePudoBorrar, fi.Name));
            return false;
        }

        Log?.Invoke(string.Format(Textos.Instancia.RecortesLogAPapelera, fi.Name, cuantos));
        lblProgreso.Text = string.Format(Textos.Instancia.RecortesListoOriginalPapelera, cuantos);

        // Sin fichero no hay proyecto: dejar la línea de tiempo con los cortes de un vídeo
        // que ya no existe solo lleva a exportar de nuevo y no entender por qué falla.
        VaciarRecortes();
        return true;
    }

    /// <summary>
    /// Deja Recortes como recién abierto: suelta el vídeo, borra los tramos y el historial,
    /// libera las miniaturas y devuelve la memoria. Es la acción «Vaciar» que pidió el usuario
    /// —liberar recursos sin cerrar la app— y también el reseteo tras borrar el original.
    /// </summary>
    internal void VaciarRecortes()
    {
        if (_exportando) return;   // exportando no se toca; primero se detiene

        // Soltar el reproductor con Source=null (NO Close(): en WPF Close() filtra handles).
        // Esto libera el fichero y la memoria de decodificación del vídeo.
        _pausado = true;
        try { _video.Soltar(); } catch { }
        _video.Source = null;
        glifoPlay.Data = Geometry.Parse("M6.5,3.5 L17,10 L6.5,16.5 Z");
        glifoPlay.Fill = Brushes.White;

        _fuente = null;
        _tramos.Clear();
        _duracion = 0;
        _zoom = 1;
        LiberarMiniaturas();          // suelta las Bitmap y borra el temporal
        _esperaPrevia.Stop();
        _esperaPista.Stop();
        _atras.Clear();
        _adelante.Clear();

        lblVideo.Text = Textos.Instancia.RecortesSinVideo;
        lblVideoDet.Text = Textos.Instancia.RecortesArrastraVideo;
        lblDuracionTotal.Text = "";
        lblDur.Text = "0:00";
        lblPos.Text = "0:00";
        lblZoom.Text = "";
        lblSinVideo.Text = Textos.Instancia.RecortesElegirParaEmpezar;
        chipSinVideo.IsVisible = true;
        cabezal.IsVisible = false;
        btnCortar.IsVisible = false;
        btnCortar.IsEnabled = false;
        btnVaciar.IsVisible = false;

        PintarPista();
        RefrescarEstimacion();

        // Se pidió expresamente «liberar recursos»: al vaciar a mano, se le devuelve al SO la
        // memoria que tenían las miniaturas y el vídeo, en vez de esperar a la próxima GC.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private async Task ExportarAsync()
    {
        // Reentrada: exportar tarda, y sin cerrojo cada clic lanza otra tanda entera sobre
        // los mismos ficheros. Pasó: cinco tandas solapadas y ni un fichero.
        if (_exportando || _fuente == null || _tramos.Count == 0) return;
        _exportando = true;

        var destino = CarpetaDestino();
        Directory.CreateDirectory(destino);

        // El reproductor de esta página tiene el fichero abierto, y el motor comprueba que
        // nadie lo tenga cogido para no pillar una descarga a medias: con el vídeo cargado
        // se saltaba el fichero entero y no salía nada. Se suelta antes de codificar.
        var rutaOriginal = _fuente.Path;   // en texto: Uri.LocalPath tropieza con '#' en el nombre
        var fuente = _fuente.Path;
        _video.Soltar();
        // OJO: NO usar _video.Soltar() aquí. Close() del MediaElement de WPF filtra ~20 handles
        // nativos por llamada (medido); con un export por ciclo, la app iba acumulando handles
        // hasta arrastrarse. Source=null libera el fichero para el codificador igual, sin fuga.
        _video.Source = null;

        _cancelar = new CancellationTokenSource();
        _pausadaExp = false;
        btnExportar.IsEnabled = false;
        btnCortar.IsEnabled = false;
        PintarExportando(true);
        EstadoProceso?.Invoke(true, Textos.Instancia.RecortesExportandoEstado);
        // Las miniaturas se paran: leerían el mismo fichero que se está codificando, y con
        // uno recién bajado de la nube ese goteo de ffmpegs era parte de la lentitud.
        _esperaPrevia.Stop();
        _esperaPista.Stop();
        _colaFondo.Clear();
        var rep = new Reportero(this);
        var hechos = new List<string>();
        var fallidos = new List<string>();
        // Los nombres que esta tanda ya ha pedido. Dos tramos llamados igual no se pisan.
        var reservadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int n = 0;
        bool salioTodo = false;

        try
        {
            // Cinturón y tirantes: si el sistema volvió a dejar el vídeo solo en la nube
            // («liberar espacio») desde que se cargó, se baja ahora, con progreso visible.
            // Codificar leyendo de la red parece la app colgada.
            if (NubeLocal.EsMarcador(_fuente.Path))
            {
                var avance = new Progress<double>(pr => lblProgreso.Text = string.Format(
                    Textos.Instancia.RecortesDescargandoNubePorcentaje, pr * 100));
                await NubeLocal.DescargarAsync(_fuente.Path, avance, _cancelar.Token);
            }

            foreach (var t in _tramos.ToList())
            {
                n++;
                _tramoActual = string.Format(
                    Textos.Instancia.RecortesTramoEnCurso, n, _tramos.Count, t.Nombre);
                lblProgreso.Text = _tramoActual;
                foreach (var f in _tramos) f.EnCurso = ReferenceEquals(f, t);
                // ── Copiando: ni motor, ni estimacion, ni opciones ──────────────
                // Es otro camino entero a proposito. Pasarlo por CompressAsync con un
                // «-c copy» metido dentro obligaria a que el motor supiera de esto, y el
                // motor es de comprimir. Aqui se corta y se copia, que es otra cosa.
                if (SinRecodificar)
                {
                    var salidaCopia = RutaDeSalida.Libre(
                        destino, t.Nombre, Path.GetExtension(rutaOriginal),
                        File.Exists, reservadas);

                    var rc = await CortadorSinRecodificar.CortarAsync(
                        rutaOriginal, salidaCopia, t.Inicio, t.Duracion, _cancelar.Token);

                    if (rc.Ok) hechos.Add(Path.GetFileName(rc.Salida));
                    else
                    {
                        fallidos.Add(t.Nombre);
                        if (rc.Error is { Length: > 0 }) Log?.Invoke(rc.Error);
                    }
                    continue;
                }

                var opt = Opciones();
                opt.Output = destino;
                opt.Desde = t.Inicio;
                opt.Duracion = t.Duracion;

                // EL NOMBRE SE RESERVA AQUI, con la misma bolsa que la rama de copiar.
                //
                // Sin esto, dos tramos con el mismo nombre se pisaban y quedaba UN fichero. El
                // motor tiene su propia proteccion contra nombres repetidos, pero es un
                // conjunto en memoria que nace vacio en CADA llamada -y aqui se le llama una
                // vez por tramo-, y su unica comprobacion de disco la anula el Force que hay
                // dos lineas mas abajo. Encima la cuenta de «hechos» cuadraba, asi que la
                // pantalla ofrecia mandar el original a la papelera con un tramo de menos.
                //
                // La rama de copiar ya lo hacia bien; esta no. Ahora comparten la reserva, asi
                // que tampoco se pisan entre ellas.
                var extSalida = "." + opt.Container;
                var rutaLibre = RutaDeSalida.Libre(destino, t.Nombre, extSalida, File.Exists, reservadas);
                opt.NombreSalida = Path.GetFileNameWithoutExtension(rutaLibre);
                // Force: el original puede estar ya en H.265 y aquí no se comprime por
                // comprimir — se está cortando, así que hay que procesarlo igual.
                opt.Force = true;

                // Task.Run: el motor es async, pero sus trozos SÍNCRONOS (abrir el fichero
                // para el candado, sondearlo, mirar el espacio…) corrían en el hilo de
                // interfaz — y sobre OneDrive cada uno es un viaje de red. Era la razón de
                // que la ventana fuera a tirones al exportar con la CPU libre.
                var tk = _cancelar.Token;
                var salidos = await Task.Run(
                    () => _engine.CompressAsync(new[] { _fuente.Path }, opt, rep, tk), tk);

                // El nombre real lo dice EL MOTOR, no se recalcula: si la carpeta ya tenía
                // un fichero igual (de un intento anterior), el motor saca la salida con
                // sufijo y el nombre recalculado no existía — contaba «1 de 2 sin salir»
                // con los dos tramos en el disco. Y se sigue comprobando en disco, porque
                // el motor puede saltarse un fichero y devolver lista vacía.
                var salida = salidos.FirstOrDefault();
                if (salida is { Ok: true } && File.Exists(salida.OutputPath))
                    hechos.Add(Path.GetFileName(salida.OutputPath));
                else fallidos.Add(t.Nombre);
            }

            // Solo cuenta como completo si TODOS los tramos están en disco. Es la condición
            // que habilita ofrecer el borrado del original: con un tramo sin salir, borrarlo
            // perdería ese trozo para siempre.
            salioTodo = fallidos.Count == 0 && hechos.Count == _tramos.Count && hechos.Count > 0;

            lblProgreso.Text = fallidos.Count == 0
                ? string.Format(hechos.Count == 1
                        ? Textos.Instancia.RecortesListoUnFichero
                        : Textos.Instancia.RecortesListoFicheros,
                    hechos.Count)
                : string.Format(Textos.Instancia.RecortesSinSalir,
                    hechos.Count, _tramos.Count, fallidos.Count);
            Log?.Invoke(fallidos.Count == 0
                ? string.Format(Textos.Instancia.RecortesLogFicherosCreados, hechos.Count, destino)
                : string.Format(Textos.Instancia.RecortesLogNoSalieron,
                    fallidos.Count, _tramos.Count, string.Join(", ", fallidos)));
        }
        catch (OperationCanceledException) { lblProgreso.Text = Textos.Instancia.RecortesCancelado; }
        catch (Exception ex)
        {
            lblProgreso.Text = Textos.Instancia.Error;
            Log?.Invoke(string.Format(Textos.Instancia.RecortesLogFallo, ex.Message));
        }
        finally
        {
            _cancelar = null;
            _exportando = false;
            _pausadaExp = false;
            _tramoActual = "";
            PintarExportando(false);
            EstadoProceso?.Invoke(false, "");
            foreach (var f in _tramos) f.EnCurso = false;

            // La pregunta va ANTES de reabrir el vídeo: el reproductor mantiene el fichero
            // cogido y con él abierto el borrado falla en silencio.
            bool borrado = salioTodo && await OfrecerBorrarOriginal(rutaOriginal, hechos.Count, destino);

            if (!borrado)
            {
                // En ApplicationIdle, no en el mismo fotograma: abrir el medio cuesta
                // 100-200 ms de hilo de interfaz, y sumado al desmontaje de la capa era EL
                // tirón medible del final del export (medido: bloqueos de 98-227 ms justo
                // al terminar; durante la codificación el hilo va limpio, p99 = 31 ms).
                Dispatcher.UIThread.Post(() =>
                {
                    // CLAVE: entre que termina el export y salta este callback (que es de
                    // baja prioridad), el usuario puede haber cargado OTRO vídeo —el caso de
                    // «Partir en dos» de otro fichero justo después de exportar—. Sin esta
                    // guarda, reabríamos el vídeo VIEJO encima del nuevo durante su carga y
                    // la partición salía sobre el material equivocado. Solo reabrimos si el
                    // vídeo actual sigue siendo el que se acaba de exportar.
                    if (_exportando) return;
                    if (_fuente == null ||
                        !string.Equals(_fuente.Path, rutaOriginal, StringComparison.OrdinalIgnoreCase))
                        return;
                    _video.Source = fuente;      // vuelve la previsualización
                    _video.Play();
                    _video.Pause();
                });
            }
            btnCortar.IsEnabled = true;
            RefrescarEstimacion();
        }
    }

    private static string Humano(long b) =>
        b >= 1L << 30 ? $"{b / (double)(1L << 30):0.##} GB"
        : b >= 1L << 20 ? $"{b / (double)(1L << 20):0.#} MB"
        : $"{b / 1024.0:0} KB";

    /// <summary>Puente entre el motor y la barra de estado de esta página.</summary>
    private sealed class Reportero : IEngineReporter
    {
        private readonly RecortesView _v;
        public Reportero(RecortesView v) => _v = v;
        // BeginInvoke y no Invoke, como en Comprimir: Invoke BLOQUEA al hilo que lee la
        // salida de ffmpeg y encola por encima del dibujado y de la entrada de teclado.
        public void Log(string linea) => Dispatcher.UIThread.Post(() => _v.Log?.Invoke(linea));
        public void FileStart(int i, int total, string nombre, double dur) { }
        // El porcentaje se cuelga del rótulo guardado del tramo, no de recomponer el texto
        // de la etiqueta: con un nombre que llevara « · » se comía media frase.
        public void FileProgress(double fraccion, string cruda) =>
            Dispatcher.UIThread.Post(() =>
                {
                    _v.lblProgreso.Text = $"{_v._tramoActual} · {fraccion * 100:0} %";
                    _v.EstadoProceso?.Invoke(true, string.Format(
                        Textos.Instancia.RecortesExportandoPorcentaje, fraccion * 100));
                });
        public void FileDone(FileResult r) { }
        public void FileSkipped(string ruta, string porque) =>
            Dispatcher.UIThread.Post(() =>
            {
                _v.lblProgreso.Text = $"{_v._tramoActual} · {Textos.Instancia.RecortesSaltado}: {porque}";
                _v.Log?.Invoke(string.Format(Textos.Instancia.RecortesLogMotorSalto, porque));
            });
    }

    // ─────────────────────────── lo que la interfaz presta ───────────────────────────

    /// <summary>La ventana que nos aloja. En WPF era <c>Window.GetWindow(this)</c>.</summary>
    private Window? Ventana => TopLevel.GetTopLevel(this) as Window;

    /// <summary>
    /// Un pincel del tema. En WPF era <c>FindResource</c>, que lanza si no está; aquí se
    /// pregunta y se cae a gris.
    /// </summary>
    private IBrush Pincel(string clave) =>
        this.TryFindResource(clave, out var v) && v is IBrush b ? b : Brushes.Gray;

    private static object? RecursoDeLaApp(string clave) =>
        Avalonia.Application.Current is { } app && app.TryFindResource(clave, out var v) ? v : null;
}
