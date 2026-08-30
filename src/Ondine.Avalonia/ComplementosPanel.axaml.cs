using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Ondine.Complementos;
using Ondine.Localizacion;
using Ondine.Reindex;
using GridLength = Avalonia.Controls.GridLength;

namespace Ondine.Ava;

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

        private string _veredicto = "";
        public string Veredicto
        {
            get => _veredicto;
            set { if (_veredicto == value) return; _veredicto = value; Avisar(nameof(Veredicto)); Avisar(nameof(TituloEntero)); }
        }

        private string _detalle = "";
        public string Detalle
        {
            get => _detalle;
            set { if (_detalle == value) return; _detalle = value; Avisar(nameof(Detalle)); Avisar(nameof(TituloEntero)); }
        }

        private IBrush _colorFondo = Brushes.Transparent;
        public IBrush ColorFondo
        {
            get => _colorFondo;
            set { if (ReferenceEquals(_colorFondo, value)) return; _colorFondo = value; Avisar(nameof(ColorFondo)); }
        }

        private IBrush _colorTexto = Brushes.Gray;
        public IBrush ColorTexto
        {
            get => _colorTexto;
            set { if (ReferenceEquals(_colorTexto, value)) return; _colorTexto = value; Avisar(nameof(ColorTexto)); }
        }

        /// <summary>
        /// El hueco de la miniatura solo se reserva si ALGUNA la trae. Un
        /// rectángulo gris en todas las filas parece roto y no informa de nada.
        /// </summary>
        private bool _huecoMiniatura;
        public bool HuecoMiniatura
        {
            get => _huecoMiniatura;
            set { if (_huecoMiniatura == value) return; _huecoMiniatura = value; Avisar(nameof(HuecoMiniatura)); }
        }

        /// <summary>
        /// La imagen ya cargada, o <c>null</c> mientras llega o si no se pudo.
        ///
        /// <para>
        /// En WPF esto no existia: se ataba <c>Image.Source</c> a la cadena y WPF resolvia
        /// sola la ruta —y tambien la descarga, si era una URL—. Avalonia no hace esa
        /// conversion, asi que la imagen se pide a mano. Ver <see cref="Miniaturas"/>.
        /// </para>
        /// </summary>
        private Bitmap? _imagen;
        private bool _pedida;
        public Bitmap? Imagen
        {
            get
            {
                if (_imagen is null && !_pedida)
                {
                    _pedida = true;
                    Miniaturas.Pedir(Miniatura, m => { _imagen = m; Avisar(nameof(Imagen)); });
                }
                return _imagen;
            }
        }

        private bool _falta;
        public bool Falta
        {
            get => _falta;
            set { if (_falta == value) return; _falta = value; Avisar(nameof(Falta)); }
        }

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

    /// <summary>
    /// Lo que hay AHORA MISMO en Organizar. No se guarda: se pregunta.
    ///
    /// <para>
    /// El panel se conserva entre aperturas a propósito —para no tirar una lista
    /// que costó minutos traer—, y por eso guardarse el catálogo al construirlo
    /// era quedarse con el primero que hubiera puesto: cargabas otro catálogo,
    /// volvías al panel, y seguía cotejando contra el anterior. Si al abrirlo la
    /// primera vez no había ninguno, no cotejaba nunca y <b>salía todo como que
    /// no lo tienes</b>. Preguntarlo cada vez hace que eso no pueda pasar.
    /// </para>
    /// </summary>
    public sealed record EstadoDeOrganizar(
        ReindexCatalog? Catalogo,
        IReadOnlyList<ReindexResolution> LoQueHay,
        string RutaCatalogo);

    private readonly Func<EstadoDeOrganizar> _ahora;

    /// <summary>La ruta del catálogo abierto: la clave con la que se recuerda su lista.</summary>
    private CancellationTokenSource? _corte;
    private bool _enTienda;
    private Action? _accionVacio;

    /// <summary>
    /// Se trajo algo, y dónde. Lo escucha quien abrió la ventana para llevar a
    /// Organizar: sin modal ya no vale devolverlo al cerrarse.
    /// </summary>
    public event Action<string>? Traido;

    /// <summary>Entrega los ficheros exactos a la revisión que ya esté abierta.</summary>
    public Func<IReadOnlyList<string>, Task<bool>>? IncorporarDescarga { get; set; }

    /// <summary>Han pedido cerrar el panel desde dentro.</summary>
    public event Action? Cerrar;

    /// <summary>
    /// Una línea para el Registro de la aplicación.
    ///
    /// <para>
    /// Existe porque el error de un complemento se pintaba en el panel y se
    /// perdía al cerrarlo. Cuando hizo falta saber POR QUÉ había fallado una
    /// lectura semanas antes, no había forma: el texto exacto no estaba en
    /// ninguna parte. Lo que se ve una vez y no se guarda, no se puede
    /// diagnosticar después.
    /// </para>
    /// </summary>
    public event Action<string>? Log;

    public ComplementosPanel(Func<EstadoDeOrganizar> ahora, Complemento? elegido = null)
    {
        AvaloniaXamlLoader.Load(this);
        _ahora = ahora;

        // Va acoplado a la ventana, no flotando: no hay nada que arrastrar ni
        // ventana que cerrar. Pedir el cierre es cosa de quien lo aloja, que es
        // el unico que sabe si se esconde, se encoge o se queda.
        Btn("btnX").Click += (_, _) => Cerrar?.Invoke();
        Btn("btnCerrar").Click += (_, _) => Cerrar?.Invoke();
        DetachedFromVisualTree += (_, _) => _corte?.Cancel();

        Lst("listaInstalados").ItemsSource = _puestos;
        Lst("lista").ItemsSource = _filas;
        Lst("listaTienda").ItemsSource = _tienda;

        Lst("listaInstalados").SelectionChanged += (_, _) => MostrarElElegido();
        Chk("chkPermisoModelo").IsCheckedChanged += (_, _) => GuardarPermisoModelo();
        Btn("btnListar").Click += async (_, _) => await ListarAsync();
        Btn("btnSoloFaltan").Click += (_, _) => { foreach (var f in _filas) f.Marcado = f.Falta; };
        Btn("btnNinguno").Click += (_, _) => { foreach (var f in _filas) f.Marcado = false; };
        Btn("btnTraer").Click += async (_, _) => await Traer();
        Btn("btnDisponibles").Click += async (_, _) => await AlternarTiendaAsync();
        Btn("btnAccionVacio").Click += (_, _) => _accionVacio?.Invoke();

        CargarInstalados();

        Enfocar(elegido);

        SizeChanged += (_, _) => AjustarAlAncho();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private TextBox Txt(string n) => this.FindControl<TextBox>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private ListBox Lst(string n) => this.FindControl<ListBox>(n)!;
    private Control Zona(string n) => this.FindControl<Control>(n)!;
    private ProgressBar Barra() => this.FindControl<ProgressBar>("barProgresoComplemento")!;
    private Window? Ventana => TopLevel.GetTopLevel(this) as Window;

    /// <summary>
    /// El nombre y la version, en el mismo TextBlock y compartiendo linea base.
    ///
    /// <para>
    /// En WPF eran dos Run con nombre dentro del XAML. Aqui un Run no es un control, asi
    /// que FindControl no lo alcanza —y ponerle nombre en el XAML no daria error, solo no se
    /// encontraria—. Se montan los dos trozos aqui, que ademas deja el motivo a la vista:
    /// dos TextBlock en fila se alinean por su caja y bailan cuando cambia el tamano de uno.
    /// </para>
    /// </summary>
    private void PintarFicha(Complemento c)
    {
        var ficha = Lbl("lblFicha");
        ficha.Inlines ??= [];
        ficha.Inlines.Clear();
        ficha.Inlines.Add(new Avalonia.Controls.Documents.Run(c.Nombre)
        {
            FontSize = 21,
            FontWeight = FontWeight.Medium,
        });
        if (!string.IsNullOrWhiteSpace(c.Version))
            ficha.Inlines.Add(new Avalonia.Controls.Documents.Run("  " + c.Version)
            {
                FontSize = 11,
                Foreground = Recurso("Neutral600"),
            });
    }

    private IBrush Recurso(string clave) =>
        this.TryFindResource(clave, out var v) && v is IBrush b ? b : Brushes.Gray;

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
        var estrecho = Bounds.Width > 0 && Bounds.Width < 620;
        this.FindControl<Grid>("rejilla")!.ColumnDefinitions[0].Width =
            estrecho ? new GridLength(0) : new GridLength(290);
        Zona("cajaIndice").IsVisible = !estrecho;
    }

    /// <summary>
    /// Deja mirando al complemento que se pide, sin rehacer nada. El panel se
    /// queda puesto entre aperturas: volver a montarlo tiraría la lista recién
    /// traída, que es justo lo que costó minutos.
    /// </summary>
    public void Enfocar(Complemento? elegido)
    {
        // Se relee el disco al volver a abrir. El panel se conserva entre aperturas
        // -para no tirar una lista que costó minutos traer-, y con él se conservaba
        // la lista de instalados: uno quitado a mano, o desde otra ventana, seguía
        // apareciendo hasta reiniciar. La carpeta es la verdad; esto es su reflejo.
        RecordarYRecargar();

        if (elegido is not null)
        {
            var suyo = _puestos.FirstOrDefault(p => p.Cual.Id == elegido.Id);
            if (suyo is not null) Lst("listaInstalados").SelectedItem = suyo;
        }
        else if (Lst("listaInstalados").SelectedItem is null && _puestos.Count > 0)
            Lst("listaInstalados").SelectedIndex = 0;

        MostrarElElegido();

        // Y se vuelve a cotejar: entre una apertura y la siguiente puedes haber
        // cargado otro catálogo o analizado otra carpeta, y las etiquetas de la
        // lista que ya está traída tienen que decir la verdad de AHORA.
        Cotejar();
        RefrescarPie();
    }

    /// <summary>
    /// Relee la carpeta sin perder de vista al que estabas mirando. Recargar a
    /// secas vacía la lista, y con ella la selección: volverías a la ficha del
    /// primero cada vez que se abre el panel.
    /// </summary>
    private void RecordarYRecargar()
    {
        var mirando = Elegido?.Id;
        CargarInstalados();
        if (mirando is null) return;

        var suyo = _puestos.FirstOrDefault(p =>
            string.Equals(p.Cual.Id, mirando, StringComparison.OrdinalIgnoreCase));
        if (suyo is not null) Lst("listaInstalados").SelectedItem = suyo;
    }

    private Complemento? Elegido => (Lst("listaInstalados").SelectedItem as Puesto)?.Cual;

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

        // LO APAGADO SE RECUERDA. El interruptor de cada complemento no se leía en ningún
        // sitio: se movía, se veía moverse, y al recargar la lista volvía a encendido. Se
        // guarda lo apagado y no lo encendido, así uno recién instalado nace encendido sin
        // que nadie tenga que apuntarlo.
        var apagados = new HashSet<string>(SettingsStore.Load().ComplementosApagados,
                                           StringComparer.OrdinalIgnoreCase);

        foreach (var c in h.Bueno)
        {
            var puesto = new Puesto { Cual = c, Activo = !apagados.Contains(c.Id) };
            puesto.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(Puesto.Activo)) return;
                GuardarApagados();
            };
            _puestos.Add(puesto);
        }

        Zona("cajaDescartados").IsVisible = h.Descartado.Count > 0;
        this.FindControl<ItemsControl>("listaDescartados")!.ItemsSource = h.Descartado.Select(d => $"{d.Cual.Id} · {d.Motivo}").ToList();
    }

    /// <summary>
    /// Escribe en los ajustes qué complementos están apagados.
    ///
    /// <para>
    /// Se guarda la lista entera cada vez en vez de ir añadiendo y quitando: son cuatro
    /// cadenas y así no hay forma de que se desincronice con lo que se ve en pantalla.
    /// </para>
    /// </summary>
    private void GuardarApagados()
    {
        var s = SettingsStore.Load();
        s.ComplementosApagados = _puestos.Where(p => !p.Activo).Select(p => p.Cual.Id).ToList();
        SettingsStore.Save(s);
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
        Lst("listaTienda").IsVisible = false;
        Btn("btnDisponibles").Content = Textos.Instancia.ComplementosDisponibles;

        _filas.Clear();
        Lst("lista").IsVisible = false;

        var c = Elegido;
        Zona("ficha").IsVisible = c is not null;
        Zona("zonaFuente").IsVisible = c is not null && c.Puede(Complemento.CapacidadImportar);

        Btn("btnSoloFaltan").IsEnabled = Btn("btnNinguno").IsEnabled = Btn("btnTraer").IsEnabled = false;
        Txt("lblEstado").Text = "";
        OcultarProgreso();

        if (c is null)
        {
            Estado(Textos.Instancia.ComplementosNingunoTitulo,
                   string.Format(Textos.Instancia.ComplementosNinguno, Descubridor.Carpeta),
                   Textos.Instancia.ComplementosDisponibles, async () => await AlternarTiendaAsync());
            return;
        }

        PintarFicha(c);
        Lbl("lblDescripcion").Text = c.Descripcion;
        // La lista de la que salió lo último que se cotejó con ESTE catálogo. Es
        // lo que evita volver a pegar el mismo enlace cada vez, y va por catálogo
        // porque la lista de una serie no vale para otra.
        Txt("txtFuente").Text = ReindexStore.CargarFuente(_ahora().RutaCatalogo, c.Id);
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
        Zona("cajaPermisoModelo").IsVisible = c.PideModelo;
        if (!c.PideModelo) return;

        var s = SettingsStore.Load();
        bool hayModelo = s.Ia.Listo;

        // Sin la bandera, poner IsChecked dispararía el manejador y guardaría el
        // permiso al simple hecho de mirar el complemento.
        _pintandoPermiso = true;
        Chk("chkPermisoModelo").IsChecked = s.PuedeUsarModelo(c.Id);
        _pintandoPermiso = false;

        Chk("chkPermisoModelo").IsEnabled = hayModelo;
        Lbl("lblPermisoModelo").Text = hayModelo
            ? Textos.Instancia.ComplementoPermisoModeloAyuda
            : Textos.Instancia.ComplementoPermisoModeloSinConfigurar;
    }

    private void PintarDesinstalar(Complemento c)
    {
        Btn("btnDesinstalar").IsVisible = true;
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
    public async void OnDesinstalar(object? sender, RoutedEventArgs e)
    {
        // El complemento sale del complemento elegido y no de un Tag: el boton vive en la
        // ficha, que ya esta mirando a uno. En WPF se le colgaba el objeto al Tag al pintar.
        if (Elegido is not { } c || Ventana is not { } duena) return;

        if (!await Dialogo.Confirmar(duena,
                string.Format(Textos.Instancia.ComplementosDesinstalarPregunta, c.Nombre),
                Textos.Instancia.ComplementosDesinstalarDetalle)) return;

        var r = Instalador.Desinstalar(c.Id, Descubridor.Carpeta);
        Txt("lblEstado").Text = r.Ok
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
        if (Chk("chkPermisoModelo").IsChecked == true) s.ComplementosConModelo.Add(c.Id);
        SettingsStore.Save(s);
    }

    /// <summary>El estado de la zona central: título, explicación y, si aplica, una salida.</summary>
    private void Estado(string titulo, string detalle, string? boton = null, Action? alPulsar = null)
    {
        Zona("cajaEstado").IsVisible = true;
        Lbl("lblVacioTitulo").Text = titulo;
        Lbl("lblVacio").Text = detalle;

        _accionVacio = alPulsar;
        Btn("btnAccionVacio").Content = boton ?? "";
        Btn("btnAccionVacio").IsVisible = boton is not null;
    }

    private void PintarProgreso(Mensaje m)
    {
        if (!string.IsNullOrWhiteSpace(m.Texto)) Txt("lblEstado").Text = m.Texto;
        if (m.AvanceSano is not { } avance) return;
        Barra().IsVisible = true;
        Barra().Value = avance;
    }

    private void OcultarProgreso()
    {
        Barra().Value = 0;
        Barra().IsVisible = false;
    }

    // ── listar ──────────────────────────────────────────────────────────────

    private async Task ListarAsync()
    {
        if (Elegido is not { } c) return;

        // Sin catálogo abierto no se lista siquiera. Traer cuarenta títulos que
        // no se pueden cotejar contra nada es devolver la lista tal cual está en
        // la web: todo el valor de esto es responder «¿qué me falta?», y esa
        // pregunta no existe sin una biblioteca contra la que hacerla.
        if (_ahora().Catalogo is null)
        {
            Estado(Textos.Instancia.ComplementosHaceFaltaCatalogo, "");
            return;
        }

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        _filas.Clear();
        Lst("lista").IsVisible = false;
        OcultarProgreso();
        Estado(Textos.Instancia.ComplementosListando, "");
        Btn("btnListar").IsEnabled = false;

        var fuente = Txt("txtFuente").Text?.Trim() ?? "";
        string? error = null;

        // Se apunta ANTES de listar, no después: si la lista tarda o falla, el
        // enlace que acabas de pegar no se pierde. Y va atada a ESTE catálogo.
        ReindexStore.GuardarFuente(_ahora().RutaCatalogo, c.Id, fuente);

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
                    PintarProgreso(m);
                    if (!string.IsNullOrWhiteSpace(m.Texto))
                        Estado(m.AvanceSano is { } a and > 0 and < 1
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
                Zona("cajaEstado").IsVisible = false;
                Lst("lista").IsVisible = true;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Btn("btnListar").IsEnabled = true;
            OcultarProgreso();
        }

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
        foreach (var f in _filas) f.HuecoMiniatura = hay;

        Cotejar();
        Btn("btnSoloFaltan").IsEnabled = Btn("btnNinguno").IsEnabled = true;
        RefrescarPie();
        if (error is not null)
        {
            Txt("lblEstado").Text = error;
            Log?.Invoke(string.Format(Textos.Instancia.ComplementosLogError,
                                      Elegido?.Nombre ?? "?", error));
        }
    }

    private void Cotejar()
    {
        if (_filas.Count == 0) return;

        if (_ahora().Catalogo is null)
        {
            foreach (var f in _filas)
            {
                f.Veredicto = Textos.Instancia.ComplementosSinCatalogo;
                f.ColorFondo = Pincel("#20222A");
                f.ColorTexto = Pincel("#8A8FA3");
                f.Detalle = Reloj(f);
                f.Falta = false;
            }
            return;
        }

        var estado = _ahora();
        var veredictos = CotejoDeLista.Cotejar(_filas.Select(f => f.Titulo),
                                               estado.Catalogo!, estado.LoQueHay);

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

        // En WPF hacia falta un lista.Items.Refresh() aqui y en la rama de arriba. Aqui no:
        // las propiedades de Fila avisan de sus cambios y la lista se repinta sola. El
        // Refresh de WPF era el cinturon por si alguna no avisaba — y era el sitio donde un
        // aviso olvidado pasaba desapercibido.
    }

    private static string Reloj(Fila f) => f.Duracion is { } d
        ? (d.TotalHours >= 1 ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}" : $"{d.Minutes}:{d.Seconds:00}")
        : "";

    private static IBrush Pincel(string hex) => new SolidColorBrush(Color.Parse(hex));

    private void RefrescarPie()
    {
        int marcados = _filas.Count(f => f.Marcado);

        // El botón solo existe si el complemento DECLARA que baja ficheros. Uno
        // que solo lee una fuente y la coteja no puede ofrecer una descarga: el
        // de YouTube dice de sí mismo «no descarga: lee», y aun así se le pintaba
        // el botón. Pulsarlo devolvía un error que además salía recortado, así
        // que por fuera parecía que la app no hacía nada.
        var baja = Elegido?.PuedeDescargar == true;
        Btn("btnTraer").IsVisible = baja;
        Btn("btnTraer").IsEnabled = baja && marcados > 0;
        Txt("lblEstado").Text = _filas.Count == 0
            ? ""
            : string.Format(Textos.Instancia.ComplementosResumen, marcados, _filas.Count,
                _filas.Count(f => f.Falta));
    }

    // ── los disponibles ─────────────────────────────────────────────────────

    private async Task AlternarTiendaAsync()
    {
        if (_enTienda) { MostrarElElegido(); return; }

        _enTienda = true;
        Btn("btnDisponibles").Content = Textos.Instancia.ComplementosVolver;
        Zona("ficha").IsVisible = Zona("zonaFuente").IsVisible = Lst("lista").IsVisible = false;
        Btn("btnSoloFaltan").IsEnabled = Btn("btnNinguno").IsEnabled = Btn("btnTraer").IsEnabled = false;

        _tienda.Clear();
        Lst("listaTienda").IsVisible = false;
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

        Zona("cajaEstado").IsVisible = false;
        Lst("listaTienda").IsVisible = true;
    }

    public async void OnInstalar(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not Oferta o) return;

        o.SePuede = false;
        o.Accion = Textos.Instancia.ComplementosInstalando;

        var paquete = await Tienda.TraerPaqueteAsync(o.Entrada);
        if (paquete.Bytes is null) { Rehabilitar(o, paquete.Error); return; }

        // La verificación y el descomprimido, fuera del hilo de la interfaz: son
        // un sha256 y un zip, y con un paquete grande se nota.
        var r = await Task.Run(() => Instalador.Instalar(o.Entrada, paquete.Bytes, Descubridor.Carpeta));
        if (!r.Ok) { Rehabilitar(o, r.Motivo); return; }

        o.Accion = Textos.Instancia.ComplementosYaInstalado;
        Txt("lblEstado").Text = string.Format(Textos.Instancia.ComplementosInstalado, o.Nombre);
        CargarInstalados();
    }

    private void Rehabilitar(Oferta o, string? error)
    {
        Txt("lblEstado").Text = error ?? "";
        o.Accion = Textos.Instancia.ComplementosInstalar;
        o.SePuede = true;
    }

    // ── traer ───────────────────────────────────────────────────────────────

    private async Task Traer()
    {
        if (Elegido is not { } c || Ventana is not { } duena) return;

        var marcados = _filas.Where(f => f.Marcado).Select(f => f.Id).ToList();
        if (marcados.Count == 0) return;

        // La carpeta la elige quien trae, no el complemento: es SU biblioteca, y un destino
        // decidido por un programa de fuera es lo ultimo que se quiere de algo que baja
        // ficheros.
        //
        // El selector cambia de sitio al portar: en WPF era Microsoft.Win32.OpenFolderDialog,
        // que es de Windows; aqui es el StorageProvider de la ventana, que en cada sistema
        // abre el selector de ese sistema. Y devuelve una tarea, asi que este metodo pasa a
        // ser un Task de verdad en vez de un async void.
        var elegidas = await duena.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Textos.Instancia.ComplementosDondeDejarlos,
            AllowMultiple = false,
        });
        if (elegidas.Count == 0) return;
        var destino = elegidas[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(destino)) return;

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        Btn("btnTraer").IsEnabled = Btn("btnListar").IsEnabled = false;
        OcultarProgreso();
        var traidos = new List<string>();
        string? error = null;

        try
        {
            await foreach (var m in Invocador.CorrerAsync(
                c, Invocador.ComandoTraer,
                marcados.Concat(new[] { "--destino", destino }), _corte.Token,
                Puente(c), destino))
            {
                switch (m.Tipo)
                {
                    // El avance se dice con palabras y no solo con un porcentaje:
                    // «bajando 3 de 7» sitúa, y un número suelto no.
                    case Mensaje.TipoProgreso: PintarProgreso(m); break;
                    // Los ficheros vienen ya filtrados por el invocador: solo los que estén
                    // de verdad dentro de la carpeta elegida. Lo que un complemento diga haber
                    // dejado en otra parte del disco no entra aquí -y de aquí se va derecho al
                    // flujo que renombra y mueve-.
                    case Mensaje.TipoHecho: traidos.AddRange(m.Ficheros); break;
                    case Mensaje.TipoError: error = m.MensajeError; break;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Btn("btnTraer").IsEnabled = Btn("btnListar").IsEnabled = true;
            OcultarProgreso();
        }

        if (error is not null)
        {
            Txt("lblEstado").Text = error;
            Log?.Invoke(string.Format(Textos.Instancia.ComplementosLogError,
                                      c.Nombre, error));
            return;
        }

        Txt("lblEstado").Text = string.Format(Textos.Instancia.ComplementosTraidos, traidos.Count);

        // Una revisión abierta ya tiene catálogo, decisiones y filas. Se añaden
        // solo los ficheros nuevos y después se vuelve a cotejar esta lista.
        if (traidos.Count > 0 && IncorporarDescarga is not null &&
            await IncorporarDescarga(traidos))
        {
            Cotejar();
            RefrescarPie();
            Txt("lblEstado").Text = string.Format(
                Textos.Instancia.ComplementosIncorporados, traidos.Count);
            return;
        }

        // Se avisa del destino elegido y no de la carpeta del primer fichero: un
        // complemento puede repartir por temporadas, y entonces la del primero
        // sería solo una parte de lo traído.
        if (await Dialogo.Confirmar(duena, Textos.Instancia.ComplementosTraer,
                string.Format(Textos.Instancia.ComplementosLlevarAOrganizar, traidos.Count, destino)))
            Traido?.Invoke(destino);
    }
}
