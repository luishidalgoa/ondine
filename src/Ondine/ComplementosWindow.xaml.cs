using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ondine.Complementos;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// La pantalla de complementos.
///
/// <para>
/// <b>Maestro-detalle y no una tabla ancha.</b> Una tabla sirve para comparar
/// muchas filas por sus columnas; aquí hay pocos complementos y de cada uno
/// importa lo SUYO, no cómo se compara con los demás. La lista de la izquierda
/// es el índice; a la derecha vive el que estés mirando.
/// </para>
/// <para>
/// <b>No es modal.</b> Esto es un sitio donde se está un rato -mirar qué hay,
/// instalar, listar, elegir- y bloquear la ventana principal mientras tanto es
/// tratarlo como un diálogo de sí/no, que no lo es.
/// </para>
/// </summary>
public partial class ComplementosWindow : Window
{
    /// <summary>Un elemento de la fuente.</summary>
    public sealed class Fila : INotifyPropertyChanged
    {
        public required string Id { get; init; }
        public required string Titulo { get; init; }
        public string? Miniatura { get; init; }
        public TimeSpan? Duracion { get; init; }

        public string Veredicto { get; set; } = "";
        public string Detalle { get; set; } = "";
        public Brush ColorFondo { get; set; } = Brushes.Transparent;
        public Brush ColorTexto { get; set; } = Brushes.Gray;

        /// <summary>
        /// El hueco de la miniatura solo se reserva si ALGUNA la trae. Un
        /// rectángulo gris en todas las filas parece roto y no informa de nada.
        /// </summary>
        public Visibility HuecoMiniatura { get; set; } = Visibility.Collapsed;

        public bool Falta { get; set; }

        private bool _marcado;
        public bool Marcado
        {
            get => _marcado;
            set { if (_marcado == value) return; _marcado = value; Avisar(nameof(Marcado)); Cambio?.Invoke(); }
        }

        public event Action? Cambio;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Avisar(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    /// <summary>Un complemento instalado, tal y como se enseña en el índice de la izquierda.</summary>
    public sealed class Puesto : INotifyPropertyChanged
    {
        public required Complemento Cual { get; init; }
        public string Nombre => Cual.Nombre;

        public string Donde => Cual.EsGlobal
            ? Textos.Instancia.ComplementosSaleEnTodo
            : string.Format(Textos.Instancia.ComplementosSaleEn, string.Join(" · ", Cual.Ambito));

        private bool _activo = true;
        public bool Activo
        {
            get => _activo;
            set { if (_activo == value) return; _activo = value; Avisar(nameof(Activo)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Avisar(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    /// <summary>Una entrada del índice remoto.</summary>
    public sealed class Oferta : INotifyPropertyChanged
    {
        public required Indice.Entrada Entrada { get; init; }
        public string Nombre => Entrada.Nombre;
        public string Version => Entrada.Version;
        public string Descripcion => Entrada.Descripcion;

        public string Donde => Entrada.Ambito.Count == 0 ||
                               Entrada.Ambito.Contains(Complemento.AmbitoGlobal, StringComparer.OrdinalIgnoreCase)
            ? Textos.Instancia.ComplementosSaleEnTodo
            : string.Format(Textos.Instancia.ComplementosSaleEn, string.Join(" · ", Entrada.Ambito));

        private string _accion = Textos.Instancia.ComplementosInstalar;
        public string Accion { get => _accion; set { _accion = value; Avisar(nameof(Accion)); } }

        private bool _sePuede = true;
        public bool SePuede { get => _sePuede; set { _sePuede = value; Avisar(nameof(SePuede)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Avisar(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    private readonly ObservableCollection<Fila> _filas = new();
    private readonly ObservableCollection<Puesto> _puestos = new();
    private readonly ObservableCollection<Oferta> _tienda = new();

    private readonly ReindexCatalog? _catalogo;
    private readonly IReadOnlyList<ReindexResolution> _loQueHay;
    private CancellationTokenSource? _corte;
    private bool _enTienda;
    private Action? _accionVacio;

    /// <summary>
    /// Se trajo algo, y dónde. Lo escucha quien abrió la ventana para llevar a
    /// Organizar: sin modal ya no vale devolverlo al cerrarse.
    /// </summary>
    public event Action<string>? Traido;

    public ComplementosWindow(ReindexCatalog? catalogo, IReadOnlyList<ReindexResolution>? loQueHay,
                              Complemento? elegido = null)
    {
        InitializeComponent();
        _catalogo = catalogo;
        _loQueHay = loQueHay ?? Array.Empty<ReindexResolution>();

        header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        btnX.Click += (_, _) => Close();
        btnCerrar.Click += (_, _) => Close();
        Closed += (_, _) => _corte?.Cancel();

        listaInstalados.ItemsSource = _puestos;
        lista.ItemsSource = _filas;
        listaTienda.ItemsSource = _tienda;

        listaInstalados.SelectionChanged += (_, _) => MostrarElElegido();
        btnListar.Click += async (_, _) => await ListarAsync();
        btnSoloFaltan.Click += (_, _) => { foreach (var f in _filas) f.Marcado = f.Falta; };
        btnNinguno.Click += (_, _) => { foreach (var f in _filas) f.Marcado = false; };
        btnTraer.Click += (_, _) => Traer();
        btnDisponibles.Click += async (_, _) => await AlternarTiendaAsync();
        btnAccionVacio.Click += (_, _) => _accionVacio?.Invoke();

        CargarInstalados();

        if (elegido is not null)
        {
            var suyo = _puestos.FirstOrDefault(p => p.Cual.Id == elegido.Id);
            if (suyo is not null) listaInstalados.SelectedItem = suyo;
        }
        else if (_puestos.Count > 0) listaInstalados.SelectedIndex = 0;

        MostrarElElegido();
    }

    private Complemento? Elegido => (listaInstalados.SelectedItem as Puesto)?.Cual;

    private void CargarInstalados()
    {
        _puestos.Clear();
        var h = Descubridor.Buscar();
        foreach (var c in h.Bueno) _puestos.Add(new Puesto { Cual = c });

        cajaDescartados.Visibility = h.Descartado.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        listaDescartados.ItemsSource = h.Descartado.Select(d => $"{d.Cual.Id} · {d.Motivo}").ToList();
    }

    // ── el detalle ──────────────────────────────────────────────────────────

    /// <summary>
    /// Pinta la parte derecha para el complemento elegido.
    ///
    /// <para>
    /// UN solo sitio decide qué se ve. Con cada botón tocando visibilidades por su
    /// cuenta se acaba con dos cosas encima a la vez, y eso solo se descubre
    /// pulsando en el orden que a nadie se le ocurrió probar.
    /// </para>
    /// </summary>
    private void MostrarElElegido()
    {
        _enTienda = false;
        listaTienda.Visibility = Visibility.Collapsed;
        btnDisponibles.Content = Textos.Instancia.ComplementosDisponibles;

        _filas.Clear();
        lista.Visibility = Visibility.Collapsed;

        var c = Elegido;
        ficha.Visibility = c is null ? Visibility.Collapsed : Visibility.Visible;
        zonaFuente.Visibility = c is not null && c.Puede(Complemento.CapacidadImportar)
            ? Visibility.Visible : Visibility.Collapsed;

        btnSoloFaltan.IsEnabled = btnNinguno.IsEnabled = btnTraer.IsEnabled = false;
        lblEstado.Text = "";

        if (c is null)
        {
            Estado(Textos.Instancia.ComplementosNingunoTitulo,
                   string.Format(Textos.Instancia.ComplementosNinguno, Descubridor.Carpeta),
                   Textos.Instancia.ComplementosDisponibles, async () => await AlternarTiendaAsync());
            return;
        }

        lblNombre.Text = c.Nombre;
        lblVersion.Text = string.IsNullOrWhiteSpace(c.Version) ? "" : "  " + c.Version;
        lblDescripcion.Text = c.Descripcion;
        txtFuente.Text = "";

        Estado(Textos.Instancia.ComplementosListoTitulo, Textos.Instancia.ComplementosVacio);
    }

    /// <summary>El estado de la zona central: título, explicación y, si aplica, una salida.</summary>
    private void Estado(string titulo, string detalle, string? boton = null, Action? alPulsar = null)
    {
        cajaEstado.Visibility = Visibility.Visible;
        lblVacioTitulo.Text = titulo;
        lblVacio.Text = detalle;

        _accionVacio = alPulsar;
        btnAccionVacio.Content = boton ?? "";
        btnAccionVacio.Visibility = boton is null ? Visibility.Collapsed : Visibility.Visible;
    }

    // ── listar ──────────────────────────────────────────────────────────────

    private async Task ListarAsync()
    {
        if (Elegido is not { } c) return;

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        _filas.Clear();
        lista.Visibility = Visibility.Collapsed;
        Estado(Textos.Instancia.ComplementosListando, "");
        btnListar.IsEnabled = false;

        var fuente = txtFuente.Text?.Trim() ?? "";
        string? error = null;

        try
        {
            await foreach (var m in Invocador.CorrerAsync(
                c, Invocador.ComandoListar,
                fuente.Length > 0 ? new[] { fuente } : Array.Empty<string>(), _corte.Token))
            {
                if (m.Tipo == Mensaje.TipoError) { error = m.MensajeError; continue; }
                if (m.Tipo != Mensaje.TipoElemento) continue;

                var f = new Fila
                {
                    Id = m.Id, Titulo = m.Titulo,
                    Miniatura = m.Miniatura, Duracion = m.ComoDuracion,
                };
                f.Cambio += RefrescarPie;
                _filas.Add(f);

                // Se va pintando según llega: con listas largas, esperar al final
                // deja la ventana en blanco todo el rato que tarde.
                cajaEstado.Visibility = Visibility.Collapsed;
                lista.Visibility = Visibility.Visible;
            }
        }
        catch (OperationCanceledException) { }
        finally { btnListar.IsEnabled = true; }

        if (_filas.Count == 0)
        {
            Estado(error is null ? Textos.Instancia.ComplementosNadaTitulo
                                 : Textos.Instancia.ComplementosFalloTitulo,
                   error ?? Textos.Instancia.ComplementosNadaEnLaFuente);
            return;
        }

        // El hueco de la miniatura se decide para TODAS a la vez: o lo reservan
        // todas o ninguna, o las filas quedan desalineadas entre sí.
        var hay = _filas.Any(f => !string.IsNullOrWhiteSpace(f.Miniatura));
        foreach (var f in _filas) f.HuecoMiniatura = hay ? Visibility.Visible : Visibility.Collapsed;

        Cotejar();
        btnSoloFaltan.IsEnabled = btnNinguno.IsEnabled = true;
        RefrescarPie();
        if (error is not null) lblEstado.Text = error;
    }

    private void Cotejar()
    {
        if (_filas.Count == 0) return;

        if (_catalogo is null)
        {
            foreach (var f in _filas)
            {
                f.Veredicto = Textos.Instancia.ComplementosSinCatalogo;
                f.ColorFondo = Pincel("#20222A");
                f.ColorTexto = Pincel("#8A8FA3");
                f.Detalle = Reloj(f);
                f.Falta = false;
            }
            lista.Items.Refresh();
            return;
        }

        var veredictos = CotejoDeLista.Cotejar(_filas.Select(f => f.Titulo), _catalogo, _loQueHay);

        for (int i = 0; i < _filas.Count && i < veredictos.Count; i++)
        {
            var f = _filas[i];
            var v = veredictos[i];

            (f.Veredicto, f.ColorFondo, f.ColorTexto, f.Falta) = v.Estado switch
            {
                CotejoDeLista.Estado.YaEsta =>
                    (Textos.Instancia.ComplementosYaEsta, Pincel("#17301F"), Pincel("#7FD1A6"), false),
                CotejoDeLista.Estado.AMedias =>
                    (string.Format(Textos.Instancia.ComplementosAMedias, string.Join(", ", v.HistoriasQueFaltan)),
                     Pincel("#35301C"), Pincel("#E0C07A"), true),
                CotejoDeLista.Estado.Falta =>
                    (Textos.Instancia.ComplementosFalta, Pincel("#262042"), Pincel("#B5ABFC"), true),
                _ =>
                    (Textos.Instancia.ComplementosDesconocido, Pincel("#20222A"), Pincel("#8A8FA3"), false),
            };

            f.Detalle = v.Episodio is { } ep
                ? $"{Reloj(f)}  ·  {Textos.Instancia.ComplementosEpisodio} {ep.Num}"
                : Reloj(f);
        }

        lista.Items.Refresh();
    }

    private static string Reloj(Fila f) => f.Duracion is { } d
        ? (d.TotalHours >= 1 ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}" : $"{d.Minutes}:{d.Seconds:00}")
        : "";

    private static Brush Pincel(string hex) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private void RefrescarPie()
    {
        int marcados = _filas.Count(f => f.Marcado);
        btnTraer.IsEnabled = marcados > 0;
        lblEstado.Text = _filas.Count == 0
            ? ""
            : string.Format(Textos.Instancia.ComplementosResumen, marcados, _filas.Count,
                _filas.Count(f => f.Falta));
    }

    // ── los disponibles ─────────────────────────────────────────────────────

    private async Task AlternarTiendaAsync()
    {
        if (_enTienda) { MostrarElElegido(); return; }

        _enTienda = true;
        btnDisponibles.Content = Textos.Instancia.ComplementosVolver;
        ficha.Visibility = zonaFuente.Visibility = lista.Visibility = Visibility.Collapsed;
        btnSoloFaltan.IsEnabled = btnNinguno.IsEnabled = btnTraer.IsEnabled = false;

        _tienda.Clear();
        listaTienda.Visibility = Visibility.Collapsed;
        Estado(Textos.Instancia.ComplementosListando, "");

        var traida = await Tienda.TraerIndiceAsync(Tienda.IndiceOficial);
        if (!_enTienda) return;   // se salió mientras se traía

        if (traida.Indice is null)
        {
            Estado(Textos.Instancia.ComplementosFalloTitulo, traida.Error ?? "");
            return;
        }

        var puestos = _puestos.Select(p => p.Cual.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var e in traida.Indice.Complementos.Where(e => e.Reparo() is null))
        {
            var o = new Oferta { Entrada = e };
            if (puestos.Contains(e.Id)) { o.Accion = Textos.Instancia.ComplementosYaInstalado; o.SePuede = false; }
            _tienda.Add(o);
        }

        if (_tienda.Count == 0)
        {
            Estado(Textos.Instancia.ComplementosTiendaVaciaTitulo, Textos.Instancia.ComplementosTiendaVacia);
            return;
        }

        cajaEstado.Visibility = Visibility.Collapsed;
        listaTienda.Visibility = Visibility.Visible;
    }

    private async void OnInstalar(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not Oferta o) return;

        o.SePuede = false;
        o.Accion = Textos.Instancia.ComplementosInstalando;

        var paquete = await Tienda.TraerPaqueteAsync(o.Entrada);
        if (paquete.Bytes is null) { Rehabilitar(o, paquete.Error); return; }

        // La verificación y el descomprimido, fuera del hilo de la interfaz: son
        // un sha256 y un zip, y con un paquete grande se nota.
        var r = await Task.Run(() => Instalador.Instalar(o.Entrada, paquete.Bytes, Descubridor.Carpeta));
        if (!r.Ok) { Rehabilitar(o, r.Motivo); return; }

        o.Accion = Textos.Instancia.ComplementosYaInstalado;
        lblEstado.Text = string.Format(Textos.Instancia.ComplementosInstalado, o.Nombre);
        CargarInstalados();
    }

    private void Rehabilitar(Oferta o, string? error)
    {
        lblEstado.Text = error ?? "";
        o.Accion = Textos.Instancia.ComplementosInstalar;
        o.SePuede = true;
    }

    // ── traer ───────────────────────────────────────────────────────────────

    private async void Traer()
    {
        if (Elegido is not { } c) return;

        var marcados = _filas.Where(f => f.Marcado).Select(f => f.Id).ToList();
        if (marcados.Count == 0) return;

        // La carpeta la elige quien trae, no el complemento: es SU biblioteca, y
        // un destino decidido por un programa de fuera es lo último que se quiere
        // de algo que baja ficheros.
        var elegir = new Microsoft.Win32.OpenFolderDialog { Title = Textos.Instancia.ComplementosDondeDejarlos };
        if (elegir.ShowDialog(this) != true) return;
        var destino = elegir.FolderName;

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        btnTraer.IsEnabled = btnListar.IsEnabled = false;
        var traidos = new List<string>();
        string? error = null;

        try
        {
            await foreach (var m in Invocador.CorrerAsync(
                c, Invocador.ComandoTraer,
                marcados.Concat(new[] { "--destino", destino }), _corte.Token))
            {
                switch (m.Tipo)
                {
                    // El avance se dice con palabras y no solo con un porcentaje:
                    // «bajando 3 de 7» sitúa, y un número suelto no.
                    case Mensaje.TipoProgreso: lblEstado.Text = m.Texto ?? ""; break;
                    case Mensaje.TipoHecho: traidos.AddRange(m.Ficheros); break;
                    case Mensaje.TipoError: error = m.MensajeError; break;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally { btnTraer.IsEnabled = btnListar.IsEnabled = true; }

        if (error is not null) { lblEstado.Text = error; return; }

        lblEstado.Text = string.Format(Textos.Instancia.ComplementosTraidos, traidos.Count);

        // Se avisa del destino elegido y no de la carpeta del primer fichero: un
        // complemento puede repartir por temporadas, y entonces la del primero
        // sería solo una parte de lo traído.
        if (DialogWindow.Confirmar(this, Textos.Instancia.ComplementosTraer,
                string.Format(Textos.Instancia.ComplementosLlevarAOrganizar, traidos.Count, destino)))
            Traido?.Invoke(destino);
    }
}
