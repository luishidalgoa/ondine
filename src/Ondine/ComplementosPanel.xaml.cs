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
public partial class ComplementosPanel : UserControl
{
    /// <summary>Un elemento de la fuente.</summary>
    public sealed class Fila : INotifyPropertyChanged
    {
        public required string Id { get; init; }
        public required string Titulo { get; init; }
        public string? Miniatura { get; init; }
        public TimeSpan? Duracion { get; init; }

        /// <summary>
        /// Lo que enseña el ToolTip: el título entero y, debajo, con qué casó.
        ///
        /// <para>
        /// El título de la fila se recorta con puntos suspensivos en cuanto el
        /// panel se estrecha, y ahí se pierde lo único que identifica el vídeo.
        /// Al pasar por encima vuelve entero, con el veredicto al lado para no
        /// tener que cruzar la vista hasta el distintivo.
        /// </para>
        /// </summary>
        public string TituloEntero => string.IsNullOrWhiteSpace(Detalle)
            ? Titulo
            : $"{Titulo}\n{Detalle}  ·  {Veredicto}";

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

    /// <summary>La ruta del catálogo abierto: la clave con la que se recuerda su lista.</summary>
    private readonly string _rutaCatalogo = "";
    private readonly IReadOnlyList<ReindexResolution> _loQueHay;
    private CancellationTokenSource? _corte;
    private bool _enTienda;
    private Action? _accionVacio;

    /// <summary>
    /// Se trajo algo, y dónde. Lo escucha quien abrió la ventana para llevar a
    /// Organizar: sin modal ya no vale devolverlo al cerrarse.
    /// </summary>
    public event Action<string>? Traido;

    /// <summary>Han pedido cerrar el panel desde dentro.</summary>
    public event Action? Cerrar;

    public ComplementosPanel(ReindexCatalog? catalogo, IReadOnlyList<ReindexResolution>? loQueHay,
                              Complemento? elegido = null, string rutaCatalogo = "")
    {
        InitializeComponent();
        _catalogo = catalogo;
        _loQueHay = loQueHay ?? Array.Empty<ReindexResolution>();
        _rutaCatalogo = rutaCatalogo;

        // Va acoplado a la ventana, no flotando: no hay nada que arrastrar ni
        // ventana que cerrar. Pedir el cierre es cosa de quien lo aloja, que es
        // el unico que sabe si se esconde, se encoge o se queda.
        btnX.Click += (_, _) => Cerrar?.Invoke();
        btnCerrar.Click += (_, _) => Cerrar?.Invoke();
        Unloaded += (_, _) => _corte?.Cancel();

        listaInstalados.ItemsSource = _puestos;
        lista.ItemsSource = _filas;
        listaTienda.ItemsSource = _tienda;

        listaInstalados.SelectionChanged += (_, _) => MostrarElElegido();
        chkPermisoModelo.Checked += (_, _) => GuardarPermisoModelo();
        chkPermisoModelo.Unchecked += (_, _) => GuardarPermisoModelo();
        btnListar.Click += async (_, _) => await ListarAsync();
        btnSoloFaltan.Click += (_, _) => { foreach (var f in _filas) f.Marcado = f.Falta; };
        btnNinguno.Click += (_, _) => { foreach (var f in _filas) f.Marcado = false; };
        btnTraer.Click += (_, _) => Traer();
        btnDisponibles.Click += async (_, _) => await AlternarTiendaAsync();
        btnAccionVacio.Click += (_, _) => _accionVacio?.Invoke();

        CargarInstalados();

        Enfocar(elegido);

        SizeChanged += (_, _) => AjustarAlAncho();
    }

    /// <summary>
    /// El índice de la izquierda se retira cuando el panel se estrecha.
    ///
    /// <para>
    /// Son 290 px fijos, y en un panel de 460 dejan al detalle 170: ahí la
    /// descripción cae a seis líneas, la casilla de la fuente se queda en un
    /// muñón y los elementos se ven en miniaturas sin título. El índice sirve
    /// para SALTAR entre complementos, y con dos o tres instalados eso se hace
    /// una vez; lo que se mira todo el rato es el detalle. Cuando hay que
    /// elegir, gana lo que se usa.
    /// </para>
    /// </summary>
    private void AjustarAlAncho()
    {
        var estrecho = ActualWidth > 0 && ActualWidth < 620;
        colIndice.Width = estrecho ? new GridLength(0) : new GridLength(290);
        cajaIndice.Visibility = estrecho ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Deja mirando al complemento que se pide, sin rehacer nada. El panel se
    /// queda puesto entre aperturas: volver a montarlo tiraría la lista recién
    /// traída, que es justo lo que costó minutos.
    /// </summary>
    public void Enfocar(Complemento? elegido)
    {
        if (elegido is not null)
        {
            var suyo = _puestos.FirstOrDefault(p => p.Cual.Id == elegido.Id);
            if (suyo is not null) listaInstalados.SelectedItem = suyo;
        }
        else if (listaInstalados.SelectedItem is null && _puestos.Count > 0)
            listaInstalados.SelectedIndex = 0;

        MostrarElElegido();
    }

    private Complemento? Elegido => (listaInstalados.SelectedItem as Puesto)?.Cual;

    /// <summary>
    /// El puente al modelo para este complemento, o <c>null</c> si no lo pide.
    ///
    /// <para>
    /// Los ajustes se leen AQUÍ, en cada ejecución, y no se guardan al abrir el
    /// panel: quitar el permiso tiene que surtir efecto en la siguiente llamada y
    /// no en la próxima vez que se abra la app. Quitar un permiso y que siga
    /// concedido es peor que no poder quitarlo.
    /// </para>
    /// </summary>
    private static PuenteDelModelo? Puente(Complemento c)
    {
        if (!c.PideModelo) return null;
        var s = SettingsStore.Load();
        return new PuenteDelModelo(s.Ia, s.PuedeUsarModelo(c.Id));
    }

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
        // La lista de la que salió lo último que se cotejó con ESTE catálogo. Es
        // lo que evita volver a pegar el mismo enlace cada vez, y va por catálogo
        // porque la lista de una serie no vale para otra.
        txtFuente.Text = ReindexStore.CargarFuente(_rutaCatalogo, c.Id);
        PintarPermisoModelo(c);
        PintarDesinstalar(c);

        Estado(Textos.Instancia.ComplementosListoTitulo, Textos.Instancia.ComplementosVacio);
    }

    /// <summary>
    /// El interruptor del permiso, solo para los complementos que lo declaran.
    ///
    /// <para>
    /// Si no hay ningún modelo conectado se enseña igualmente, pero apagado y
    /// diciendo dónde se conecta. Esconderlo dejaría al complemento anunciando
    /// una capacidad que no se ve por ninguna parte.
    /// </para>
    /// </summary>
    private void PintarPermisoModelo(Complemento c)
    {
        cajaPermisoModelo.Visibility = c.PideModelo ? Visibility.Visible : Visibility.Collapsed;
        if (!c.PideModelo) return;

        var s = SettingsStore.Load();
        bool hayModelo = s.Ia.Listo;

        // Sin la bandera, poner IsChecked dispararía el manejador y guardaría el
        // permiso al simple hecho de mirar el complemento.
        _pintandoPermiso = true;
        chkPermisoModelo.IsChecked = s.PuedeUsarModelo(c.Id);
        _pintandoPermiso = false;

        chkPermisoModelo.IsEnabled = hayModelo;
        lblPermisoModelo.Text = hayModelo
            ? Textos.Instancia.ComplementoPermisoModeloAyuda
            : Textos.Instancia.ComplementoPermisoModeloSinConfigurar;
    }

    private void PintarDesinstalar(Complemento c)
    {
        btnDesinstalar.Visibility = Visibility.Visible;
        btnDesinstalar.Tag = c;
    }

    /// <summary>
    /// Quita el complemento elegido, preguntando antes.
    ///
    /// <para>
    /// Se pregunta porque borra una carpeta, y un complemento puede traer ajustes
    /// dentro. Se dice además que se puede volver a instalar: sin eso, «se borra
    /// su carpeta» suena a definitivo y frena una decisión que no lo es.
    /// </para>
    /// </summary>
    private void OnDesinstalar(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Complemento c }) return;

        if (!DialogWindow.Confirmar(Window.GetWindow(this),
                string.Format(Textos.Instancia.ComplementosDesinstalarPregunta, c.Nombre),
                Textos.Instancia.ComplementosDesinstalarDetalle)) return;

        var r = Instalador.Desinstalar(c.Id, Descubridor.Carpeta);
        lblEstado.Text = r.Ok
            ? string.Format(Textos.Instancia.ComplementosDesinstalado, c.Nombre)
            : r.Motivo ?? "";

        CargarInstalados();
        MostrarElElegido();
    }

    private bool _pintandoPermiso;

    private void GuardarPermisoModelo()
    {
        if (_pintandoPermiso || Elegido is not { } c) return;

        var s = SettingsStore.Load();
        s.ComplementosConModelo.RemoveAll(x => string.Equals(x, c.Id, StringComparison.OrdinalIgnoreCase));
        if (chkPermisoModelo.IsChecked == true) s.ComplementosConModelo.Add(c.Id);
        SettingsStore.Save(s);
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

        // Sin catálogo abierto no se lista siquiera. Traer cuarenta títulos que
        // no se pueden cotejar contra nada es devolver la lista tal cual está en
        // la web: todo el valor de esto es responder «¿qué me falta?», y esa
        // pregunta no existe sin una biblioteca contra la que hacerla.
        if (_catalogo is null)
        {
            Estado(Textos.Instancia.ComplementosHaceFaltaCatalogo, "");
            return;
        }

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        _filas.Clear();
        lista.Visibility = Visibility.Collapsed;
        Estado(Textos.Instancia.ComplementosListando, "");
        btnListar.IsEnabled = false;

        var fuente = txtFuente.Text?.Trim() ?? "";
        string? error = null;

        // Se apunta ANTES de listar, no después: si la lista tarda o falla, el
        // enlace que acabas de pegar no se pierde. Y va atada a ESTE catálogo.
        ReindexStore.GuardarFuente(_rutaCatalogo, c.Id, fuente);

        try
        {
            await foreach (var m in Invocador.CorrerAsync(
                c, Invocador.ComandoListar,
                fuente.Length > 0 ? new[] { fuente } : Array.Empty<string>(), _corte.Token,
                Puente(c)))
            {
                if (m.Tipo == Mensaje.TipoError) { error = m.MensajeError; continue; }

                // Lo que el complemento diga que está haciendo, tal cual, y con
                // el porcentaje si lo sabe. Se tiraba a la basura: leer las
                // descripciones de una lista tarda medio minuto largo, y durante
                // todo ese rato la pantalla decía «listando» sin moverse. Un
                // proceso que no cuenta lo que hace no se distingue de uno
                // colgado, y quien mira acaba cerrando algo que iba bien.
                if (m.Tipo == Mensaje.TipoProgreso)
                {
                    if (!string.IsNullOrWhiteSpace(m.Texto))
                        Estado(m.Avance is { } a and > 0 and < 1
                            ? $"{m.Texto}   {a:P0}"
                            : m.Texto!, "");
                    continue;
                }

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
                // Por su NOMBRE cuando se sabe. «Te falta la b» obliga a abrir el
                // catálogo a mirar qué era la b, justo cuando hay que decidir si
                // interesa traerlo; con el título se ve de un vistazo.
                CotejoDeLista.Estado.AMedias =>
                    (string.Format(Textos.Instancia.ComplementosAMedias,
                        string.Join(", ", v.TitulosQueFaltan.Count > 0
                            ? v.TitulosQueFaltan
                            : v.HistoriasQueFaltan.Count > 0 ? v.HistoriasQueFaltan : v.SegmentosSinCasar)),
                     Pincel("#35301C"), Pincel("#E0C07A"), true),
                CotejoDeLista.Estado.Falta =>
                    (Textos.Instancia.ComplementosFalta, Pincel("#262042"), Pincel("#B5ABFC"), true),
                _ =>
                    (Textos.Instancia.ComplementosDesconocido, Pincel("#20222A"), Pincel("#8A8FA3"), false),
            };

            // El número SOLO cuando se sabe. Enseñar «episodio 1734» al lado de «no
            // se sabe» es contradecirse: ese número es el mejor parecido que se
            // encontró, no una conclusión, y ponerlo invita a creérselo.
            f.Detalle = v.Estado != CotejoDeLista.Estado.Desconocido && v.Episodio is { } ep
                ? $"{Reloj(f)}  ·  {Textos.Instancia.ComplementosEpisodio} {ep.Num}"
                : Reloj(f);

            // Un trozo que el catálogo no reconoce se dice. Es una historia que
            // el vídeo trae y que no está contada ni entre las que tienes ni
            // entre las que faltan: callarla es darla por inexistente.
            if (v.SegmentosSinCasar.Count > 0 && v.Episodio is not null)
                f.Detalle += $"  ·  {string.Join(", ", v.SegmentosSinCasar)} ({Textos.Instancia.ComplementosNoEnCatalogo})";
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
            var yaEsta = _puestos.FirstOrDefault(p =>
                string.Equals(p.Cual.Id, e.Id, StringComparison.OrdinalIgnoreCase))?.Cual;

            if (yaEsta is not null)
            {
                // Instalado, pero ¿viejo? Reinstalar encima ES la actualización: el
                // instalador borra y repone. Solo cambia lo que dice el botón.
                if (Indice.EsMasNueva(yaEsta.Version, e.Version))
                    o.Accion = string.Format(Textos.Instancia.ComplementosHayVersion, e.Version);
                else
                {
                    o.Accion = Textos.Instancia.ComplementosYaInstalado;
                    o.SePuede = false;
                }
            }
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
        if (elegir.ShowDialog(Window.GetWindow(this)) != true) return;
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
                marcados.Concat(new[] { "--destino", destino }), _corte.Token,
                Puente(c)))
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
        if (DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.ComplementosTraer,
                string.Format(Textos.Instancia.ComplementosLlevarAOrganizar, traidos.Count, destino)))
            Traido?.Invoke(destino);
    }
}
