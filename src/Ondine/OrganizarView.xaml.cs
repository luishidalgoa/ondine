using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

/// <summary>Tarjeta de catálogo de la pantalla de inicio.</summary>
public sealed class CatalogoCard
{
    public required CatalogoGuardado Cat { get; init; }
    public bool Seleccionado { get; init; }
    public bool NoSeleccionado => !Seleccionado;
    public Brush Fondo => Seleccionado
        ? (Brush)Application.Current.FindResource("Accent900")
        : (Brush)Application.Current.FindResource("Surface");
    public Brush Borde => Seleccionado
        ? (Brush)Application.Current.FindResource("Accent700")
        : (Brush)Application.Current.FindResource("Divider");
}

/// <summary>
/// Página «Organizar»: identifica los ficheros de una carpeta contra un catálogo y propone
/// el nombre canónico. La inteligencia vive en <see cref="ReindexEngine"/>; aquí solo se
/// orquesta y se pinta.
/// </summary>
public partial class OrganizarView : UserControl
{
    private readonly ObservableCollection<OrganizarRow> _filas = new();
    private bool _ordenManual;   // hay un orden por cabecera activo (oculta las bandas de temporada)
    private readonly List<CatalogoGuardado> _catalogos = new();
    private CatalogoGuardado? _catalogoElegido;
    private bool _sincronizandoModo;   // evita re-analizar mientras se pone el modo al cargar catálogo
    private ReindexCatalog? _catalogoCargado;
    private LibraryTemplate _plantilla = new();
    private Dictionary<string, ReindexOverride> _decisiones = new();

    /// <summary>Los ficheros apartados para mirar con calma. Vive en disco entre sesiones.</summary>
    private ColaRevision _revision = new();
    private LoteJournal? _ultimoLote;
    private string[] _ficheros = Array.Empty<string>();
    private bool _cargando;

    /// <summary>Se avisa al anfitrión para que lo escriba en el registro compartido.</summary>
    public event Action<string>? Log;

    /// <summary>
    /// «Llévate este fichero a Recortes». Lo pide la fila y lo resuelve la ventana: esta
    /// página no sabe que existe una pestaña, y así sigue sin saberlo.
    /// </summary>
    public event Action<string, bool>? AbrirEnRecortes;

    private readonly PasosVisual _pasos;

    public OrganizarView()
    {
        InitializeComponent();

        tabla.ItemsSource = _filas;
        listaCatalogos.ItemsSource = new ObservableCollection<CatalogoCard>();

        txtPlantilla.Text = LibraryTemplate.PatronPorDefecto;

        btnCarpeta.Click += (_, _) => ElegirCarpeta();
        btnImportar.Click += (_, _) => ImportarCatalogo();
        btnCatalogos.Click += (_, _) => ImportarCatalogo();
        btnExplorar.Click += (_, _) => AbrirExplorador();
        btnFormato.Click += (_, _) => AbrirEspecificacion();
        btnEjemplo.Click += (_, _) => GuardarEjemplo();
        btnPrompt.Click += (_, _) => AbrirGeneradorDePrompt();
        btnVolver.Click += (_, _) => VolverAlInicio();
        btnSimular.Click += (_, _) => Simular();
        btnSimularGrande.Click += (_, _) => Simular();
        btnAplicar.Click += (_, _) => PedirConfirmacion();
        btnPartirSegmentos.Click += OnPartirSegmentos;
        btnQueFalta.Click += (_, _) =>
        {
            if (_catalogoCargado == null || _filas.Count == 0) return;
            new FaltantesWindow(_catalogoCargado, _filas.Select(f => f.Res).ToList())
            { Owner = Window.GetWindow(this) }.ShowDialog();
        };
        btnDeshacer.Click += (_, _) => DeshacerUltimoLote();
        btnDeshacerBanda.Click += (_, _) => DeshacerUltimoLote();
        btnMemoria.Click += (_, _) => AbrirMemoria();
        btnAceptarVerdes.Click += (_, _) => AceptarVerdes();
        btnConfirmarEspeciales.Click += (_, _) => FiltrarSolo(ReindexEstado.Especial);
        listaMarcas.ItemsSource = LibraryTemplate.Marcas;

        btnConfCancelar.Click += (_, _) => overlayConfirmar.Visibility = Visibility.Collapsed;
        btnConfAceptar.Click += (_, _) => { overlayConfirmar.Visibility = Visibility.Collapsed; Aplicar(); };

        txtCarpeta.LostFocus += (_, _) => RevisarCarpeta();
        txtCarpeta.KeyDown += (_, e) => { if (e.Key == Key.Enter) RevisarCarpeta(); };
        txtPlantilla.LostFocus += (_, _) => CambiarPlantilla();
        txtPlantilla.KeyDown += (_, e) => { if (e.Key == Key.Enter) CambiarPlantilla(); };
        // La vista previa se refresca mientras escribes, no al confirmar: es lo que hace que
        // se entienda qué produce cada marca.
        txtPlantilla.TextChanged += (_, _) => RefrescarVistaPrevia();

        cboSerie.SelectionChanged += (_, _) => ElegirCatalogo(cboSerie.SelectedItem as CatalogoGuardado);
        cboModo.SelectionChanged += (_, _) =>
        {
            if (_sincronizandoModo || _catalogoElegido == null) return;
            ReindexStore.GuardarModo(_catalogoElegido.Ruta,
                ModoActual() == ModoPrioridad.NumeroPorTemporada ? "numero" : "auto");
            // Cambiar el modo re-identifica al momento si ya hay análisis en pantalla.
            if (_catalogoCargado != null && _ficheros.Length > 0) Simular();
        };
        // Ctrl+Z deshace el último envío a la papelera de la app (p. ej. la copia repetida borrada).
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Z
                && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                DeshacerBorrado();
                e.Handled = true;
            }
        };

        foreach (var chip in new[] { chipLimpios, chipCorregidos, chipEspeciales, chipConflictos, chipErrores, chipDudas })
        {
            chip.Checked += (_, _) => AplicarFiltro();
            chip.Unchecked += (_, _) => AplicarFiltro();
        }

        txtBuscarTabla.TextChanged += (_, _) => AplicarFiltro();
        txtBuscarTabla.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { txtBuscarTabla.Text = ""; tabla.Focus(); e.Handled = true; }
        };
        // Ctrl+K desde cualquier punto de la página: el estándar de «buscar aquí dentro»
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control &&
                vistaRevision.Visibility == Visibility.Visible)
            {
                txtBuscarTabla.Focus();
                txtBuscarTabla.SelectAll();
                e.Handled = true;
            }
        };

        // Menú contextual: se resuelve la fila bajo el puntero y se selecciona, para que
        // no quede duda de sobre cuál se va a actuar. Fuera de una fila no se abre: un menú
        // que aparece sobre el vacío y actúa sobre «lo último seleccionado» es una trampa.
        tabla.ContextMenuOpening += (_, e) =>
        {
            // Con la tecla Menú no hay puntero al que preguntar: WPF lo indica poniendo la
            // posición en negativo, y entonces manda la fila seleccionada. Sin esto el menú
            // quedaría muerto para quien no use el ratón.
            bool conTeclado = e.CursorLeft < 0 && e.CursorTop < 0;
            var r = conTeclado
                ? tabla.SelectedItem as OrganizarRow
                : Ascender<DataGridRow>(Mouse.DirectlyOver as DependencyObject)?.Item as OrganizarRow;
            if (r == null) { e.Handled = true; return; }
            tabla.SelectedItem = r;
            miReproducir.IsEnabled = File.Exists(r.RutaActual);
            miRecortar.IsEnabled = miReproducir.IsEnabled;
            miUbicacion.IsEnabled = miReproducir.IsEnabled;
            // El mismo sitio sirve para apartar y para desapartar: el rótulo dice cuál de
            // las dos toca ahora, así no hay que recordar el estado de la fila.
            miRevisar.Header = r.Apartada
                ? Textos.Instancia.OrganizarQuitarDeLaCola
                : Textos.Instancia.OrganizarAnadirALaCola;

            // Se puede corregir cualquier fila, esté como esté: lo único que hace falta es un
            // catálogo cargado y que no se haya renombrado ya. El rótulo dice si hay historias
            // que repartir, que es lo que permite decir «este fichero es solo la b y la c».
            int historias = r.Res.Episodio?.TitulosSalida.Count ?? 0;
            miElegirEpisodio.IsEnabled = _catalogoCargado != null && !r.Aplicado;
            miElegirEpisodio.Header = historias > 1
                ? string.Format(Textos.Instancia.OrganizarElegirEpisodioHistoriasN, historias)
                : Textos.Instancia.OrganizarElegirOtroEpisodio;
            miDejarComoEsta.IsEnabled = !r.Aplicado;
            miAnadirHistoria.IsEnabled = _catalogoCargado != null && !r.Aplicado && r.Res.Episodio != null;
            miQuitarHistorias.IsEnabled = r.Tambien.Count > 0;
            miQuitarHistorias.Header = r.Tambien.Count > 0
                ? string.Format(Textos.Instancia.OrganizarQuitarHistoriasN, r.Tambien.Count)
                : Textos.Instancia.OrganizarQuitarHistorias;
        };
        miElegirEpisodio.Click += (_, _) => OnElegirAMano(tabla, new RoutedEventArgs());
        miDejarComoEsta.Click += (_, _) => OnDejarComoEsta(tabla, new RoutedEventArgs());
        miAnadirHistoria.Click += (_, _) => AnadirHistoriaDeOtroEpisodio();
        miQuitarHistorias.Click += (_, _) =>
        {
            if (tabla.SelectedItem is not OrganizarRow f) return;
            f.QuitarHistorias();
            ActualizarContadores();
            Escribir(string.Format(Textos.Instancia.OrganizarLogEpisodioNormal, f.Original));
        };
        miReproducir.Click += (_, _) => ReproducirFila(tabla.SelectedItem as OrganizarRow);
        miRecortar.Click += (_, _) =>
        {
            if (tabla.SelectedItem is OrganizarRow f && File.Exists(f.RutaActual))
                AbrirEnRecortes?.Invoke(f.RutaActual, false);
        };
        miUbicacion.Click += (_, _) => AbrirUbicacion(tabla.SelectedItem as OrganizarRow);
        miRevisar.Click += (_, _) => AlternarApartada(tabla.SelectedItem as OrganizarRow);
        btnCola.Checked += (_, _) => PintarCola();
        btnVaciarCola.Click += (_, _) => VaciarCola();

        tabla.PreviewKeyDown += OnTablaKeyDown;
        tabla.PreviewMouseLeftButtonDown += OnTablaClic;
        tabla.PreviewMouseMove += OnTablaArrastre;
        tabla.PreviewMouseLeftButtonUp += (_, _) => _pintando = null;
        tabla.MouseLeave += (_, _) => _pintando = null;

        // Con la ventana estrecha, el rótulo de la sección se recortaba a un par de puntos
        // suspensivos, que queda peor que no estar: los botones de al lado ya dicen de qué
        // va la columna. Por debajo de ese ancho se retira entero.
        //
        // El umbral sale de la cuenta real, no a ojo: el panel de catálogos ocupa la mitad
        // de la página, los tres botones piden unos 330 px y el rótulo necesita ~140 para
        // leerse entero. Media página ≥ 470 ⇒ página ≥ 990.
        SizeChanged += (_, _) =>
            lblTituloCatalogos.Visibility = ActualWidth >= 990 ? Visibility.Visible : Visibility.Collapsed;

        // Las tres etapas de la identificación, en el panel de ficheros. Son las fases
        // REALES del trabajo, no decorado: cada una se enciende cuando su fase corre.
        _pasos = new PasosVisual(
            Textos.Instancia.OrganizarPaso1,
            Textos.Instancia.OrganizarPaso2,
            Textos.Instancia.OrganizarPaso3);
        panelEtapas.Children.Add(_pasos.Raiz);

        Loaded += (_, _) =>
        {
            if (!_cargando) Recargar();

            // Al VOLVER a la app (alt-tab desde el Explorador, por ejemplo) se relee la
            // lista de catálogos: si borraste o moviste un JSON fuera, la tarjeta se va
            // sola en vez de quedarse enseñando algo que ya no existe. Solo en la pantalla
            // de inicio — en plena revisión no se le mueve el suelo a nadie.
            if (Window.GetWindow(this) is { } w)
                w.Activated += (_, _) =>
                {
                    if (vistaInicio.Visibility == Visibility.Visible && !_cargando)
                        CargarCatalogos();
                };
        };
    }

    // ─────────────────────────── arranque ───────────────────────────

    private void Recargar()
    {
        _cargando = true;
        try
        {
            _decisiones = ReindexStore.CargarDecisiones();
            _revision = ReindexStore.CargarRevision();
            CargarCatalogos();
            RefrescarUltimoLote();
        }
        finally { _cargando = false; }
        ActualizarEstado();
        RefrescarVistaPrevia();
    }

    private void CargarCatalogos()
    {
        _catalogos.Clear();
        _catalogos.AddRange(ReindexStore.ListarCatalogos());

        cboSerie.ItemsSource = null;
        cboSerie.ItemsSource = _catalogos;

        if (_catalogoElegido != null)
            _catalogoElegido = _catalogos.FirstOrDefault(c => c.Ruta == _catalogoElegido.Ruta);

        // La de la última vez antes que «la primera de la lista»: con dos catálogos, arrancar
        // siempre en el alfabéticamente primero significa elegir a mano en cada arranque.
        if (_catalogoElegido == null)
        {
            var ultima = ReindexStore.CargarUltimoCatalogo();
            _catalogoElegido = _catalogos.FirstOrDefault(c => c.Ruta == ultima);
        }
        _catalogoElegido ??= _catalogos.FirstOrDefault();

        // Al arrancar con el último catálogo, pre-rellena su carpeta reciente (no pasa por
        // ElegirCatalogo, así que hay que hacerlo aquí también).
        if (_catalogoElegido != null && string.IsNullOrWhiteSpace(txtCarpeta.Text))
        {
            var cs = ReindexStore.CargarCarpetasDeCatalogo(_catalogoElegido.Ruta);
            if (cs.Count > 0)
            {
                txtCarpeta.Text = cs[0];
                // Contar los ficheros SE DIFIERE: al arrancar, recorrer cientos de ficheros en
                // OneDrive es un viaje de red por carpeta y dejaría la ventana en blanco. Se
                // pinta primero y se cuenta en cuanto la interfaz está libre.
                Dispatcher.BeginInvoke(new Action(RevisarCarpeta),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        cboSerie.SelectedItem = _catalogoElegido;
        panelSinCatalogos.Visibility = _catalogos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PintarTarjetas();
        CargarCatalogoElegido();
        ActualizarVinculo();
    }

    private void PintarTarjetas()
    {
        listaCatalogos.ItemsSource = _catalogos
            .Select(c => new CatalogoCard { Cat = c, Seleccionado = c.Ruta == _catalogoElegido?.Ruta })
            .ToList();
    }

    /// <summary>El modo de prioridad elegido para este catálogo (el motor lo usa en Resolve).</summary>
    private ModoPrioridad ModoActual() =>
        cboModo.SelectedIndex == 1 ? ModoPrioridad.NumeroPorTemporada : ModoPrioridad.Automatico;

    private void CargarCatalogoElegido()
    {
        _catalogoCargado = null;
        if (_catalogoElegido == null) return;
        // El modo es propiedad del catálogo: se restaura el suyo sin disparar un re-análisis.
        _sincronizandoModo = true;
        cboModo.SelectedIndex = ReindexStore.CargarModo(_catalogoElegido.Ruta) == "numero" ? 1 : 0;
        _sincronizandoModo = false;
        try { _catalogoCargado = ReindexCatalog.Load(_catalogoElegido.Ruta); }
        catch (Exception ex) { Aviso(string.Format(Textos.Instancia.OrganizarNoSeLeyoCatalogo, ex.Message)); }
    }

    private void ElegirCatalogo(CatalogoGuardado? cat)
    {
        if (cat == null || cat.Ruta == _catalogoElegido?.Ruta) return;
        _catalogoElegido = cat;
        ReindexStore.GuardarUltimoCatalogo(cat.Ruta);
        // Al CAMBIAR de catálogo se pone su carpeta vinculada, aunque el campo ya tuviera una:
        // esa era la del catálogo anterior y aquí ya no pinta nada. Antes solo se rellenaba si
        // el campo estaba vacío, así que en la práctica no se veía nunca.
        var carpetas = ReindexStore.CargarCarpetasDeCatalogo(cat.Ruta);
        cboSerie.SelectedItem = cat;
        PintarTarjetas();
        CargarCatalogoElegido();
        // La carpeta se pone DESPUÉS de cargar el catálogo y con su escaneo: si solo se escribe
        // el texto, «Analizar» se queda apagado — lo que lo habilita es la cuenta de ficheros.
        if (carpetas.Count > 0) PonerCarpeta(carpetas[0]);
        ActualizarEstado();
        ActualizarVinculo();
        RefrescarVistaPrevia();
    }

    /// <summary>
    /// Deja a la vista si la carpeta del campo está vinculada al catálogo elegido. Sin esto, el
    /// vínculo es invisible: no hay forma de saber si la próxima vez vendrá sola.
    /// </summary>
    private void ActualizarVinculo()
    {
        if (lblVinculo == null) return;
        var carpeta = txtCarpeta.Text?.Trim() ?? "";
        if (_catalogoElegido == null || carpeta.Length == 0)
        {
            lblVinculo.Text = "";
            return;
        }
        var vinculadas = ReindexStore.CargarCarpetasDeCatalogo(_catalogoElegido.Ruta);
        bool esta = vinculadas.Any(c => string.Equals(c, carpeta, StringComparison.OrdinalIgnoreCase));
        lblVinculo.Text = esta
            ? string.Format(Textos.Instancia.OrganizarVinculada, _catalogoElegido.Serie)
            : vinculadas.Count > 0
                ? string.Format(Textos.Instancia.OrganizarSinVincularConOtras,
                                _catalogoElegido.Serie, vinculadas.Count)
                : Textos.Instancia.OrganizarSinVincular;
    }

    /// <summary>
    /// Menú de las carpetas vinculadas a este catálogo: saltar a una, vincular la actual o
    /// quitarla. El vínculo se guardaba solo al analizar y no se podía ni ver ni tocar.
    /// </summary>
    private void OnVinculos(object sender, RoutedEventArgs e)
    {
        if (_catalogoElegido == null)
        {
            Aviso(Textos.Instancia.OrganizarVinculosSinCatalogo);
            return;
        }
        var actual = txtCarpeta.Text?.Trim() ?? "";
        var vinculadas = ReindexStore.CargarCarpetasDeCatalogo(_catalogoElegido.Ruta);
        var menu = new ContextMenu();

        menu.Items.Add(new MenuItem
        {
            Header = string.Format(Textos.Instancia.OrganizarVinculosCabecera, _catalogoElegido.Serie),
            IsEnabled = false,
        });
        if (vinculadas.Count == 0)
            menu.Items.Add(new MenuItem
            { Header = Textos.Instancia.OrganizarVinculosNinguna, IsEnabled = false });
        foreach (var c in vinculadas)
        {
            var destino = c;
            var it = new MenuItem
            {
                Header = destino,
                IsChecked = string.Equals(destino, actual, StringComparison.OrdinalIgnoreCase),
                IsCheckable = true,
            };
            it.Click += (_, _) => PonerCarpeta(destino);
            menu.Items.Add(it);
        }

        menu.Items.Add(new Separator());
        bool yaEsta = vinculadas.Any(c => string.Equals(c, actual, StringComparison.OrdinalIgnoreCase));
        if (actual.Length > 0 && !yaEsta)
        {
            var add = new MenuItem { Header = Textos.Instancia.OrganizarVincularActual };
            add.Click += (_, _) =>
            {
                ReindexStore.GuardarCarpetaDeCatalogo(_catalogoElegido.Ruta, actual);
                Escribir(string.Format(Textos.Instancia.OrganizarLogCarpetaVinculada,
                                       _catalogoElegido.Serie, actual));
                ActualizarVinculo();
            };
            menu.Items.Add(add);
        }
        if (yaEsta)
        {
            var quitar = new MenuItem { Header = Textos.Instancia.OrganizarQuitarVinculo };
            quitar.Click += (_, _) =>
            {
                ReindexStore.OlvidarCarpetaDeCatalogo(_catalogoElegido.Ruta, actual);
                Escribir(string.Format(Textos.Instancia.OrganizarLogVinculoQuitado, actual));
                ActualizarVinculo();
            };
            menu.Items.Add(quitar);
        }
        if (actual.Length == 0)
            menu.Items.Add(new MenuItem
            { Header = Textos.Instancia.OrganizarVincularSinCarpeta, IsEnabled = false });

        menu.PlacementTarget = btnVinculos;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OnUsarCatalogo(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string ruta)
            ElegirCatalogo(_catalogos.FirstOrDefault(c => c.Ruta == ruta));
    }

    /// <summary>Abre la carpeta del JSON del catálogo con el fichero seleccionado.</summary>
    private void OnUbicacionCatalogo(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string ruta) AbrirCarpetaDe(ruta);
    }

    private void OnCopiarRutaCatalogo(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string ruta) return;
        try { Clipboard.SetText(ruta); Escribir(Textos.Instancia.MainRutaCopiada); }
        catch { /* el portapapeles puede estar cogido por otra app; no es grave */ }
    }

    /// <summary>
    /// Saca un catálogo de la app. Se pregunta antes porque no hay «deshacer» para esto, y se
    /// dice de dónde salió: si el JSON original sigue en su sitio, volver a importarlo es
    /// trivial, y saberlo cambia por completo lo que cuesta decir que sí.
    /// </summary>
    private void OnQuitarCatalogo(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string ruta) return;

        var cat = _catalogos.FirstOrDefault(c => c.Ruta == ruta);
        if (cat == null) return;

        // La copia interna SÍ se borra (es de la app); el JSON del usuario, nunca. El aviso
        // dice la verdad de cada caso — antes prometía «NO se borra» y con las copias legadas
        // eso era mentira.
        var deDonde = string.Format(cat.EsCopiaInterna
            ? Textos.Instancia.OrganizarQuitarCatalogoInterno
            : Textos.Instancia.OrganizarQuitarCatalogoExterno, cat.Ruta);

        if (!DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarQuitarCatalogoTitulo,
                string.Format(Textos.Instancia.OrganizarQuitarCatalogoPregunta, cat.Serie, deDonde))) return;

        if (ReindexStore.BorrarCatalogo(ruta))
        {
            Escribir(string.Format(Textos.Instancia.OrganizarLogCatalogoQuitado, cat.Serie));
            if (_catalogoElegido?.Ruta == ruta) _catalogoElegido = null;
            Recargar();
        }
        else Aviso(Textos.Instancia.OrganizarCatalogoYaNoEstaba);
    }

    // ─────────────────────────── carpeta ───────────────────────────

    private void ElegirCarpeta()
    {
        var dlg = new OpenFolderDialog { Title = Textos.Instancia.OrganizarCarpeta };
        if (!string.IsNullOrWhiteSpace(txtCarpeta.Text) && Directory.Exists(txtCarpeta.Text))
            dlg.InitialDirectory = txtCarpeta.Text;
        if (dlg.ShowDialog() == true)
        {
            txtCarpeta.Text = dlg.FolderName;
            RevisarCarpeta();
        }
    }

    /// <summary>
    /// Cuenta los vídeos de la carpeta Y DE SUS SUBCARPETAS. No lee metadatos: eso es «Simular».
    ///
    /// El recorrido baja porque así está montada una biblioteca: se apunta a la carpeta de la
    /// serie y las temporadas cuelgan dentro. Quedándose en el primer nivel, una serie entera
    /// se veía como «no hay vídeos».
    /// </summary>
    /// <summary>
    /// Pone una carpeta en el campo Y la cuenta. Escribir solo el texto no basta: lo que habilita
    /// «Analizar» es la cuenta de ficheros, así que una carpeta puesta por la app —la vinculada al
    /// catálogo, o la elegida en el menú de vínculos— dejaba el botón apagado y la pantalla
    /// diciendo «Elige una carpeta para empezar» con la carpeta delante.
    /// </summary>
    private void PonerCarpeta(string ruta)
    {
        txtCarpeta.Text = ruta;
        RevisarCarpeta();
    }

    private void RevisarCarpeta()
    {
        var carpeta = txtCarpeta.Text?.Trim() ?? "";
        _ficheros = Array.Empty<string>();

        try { _ficheros = LibraryScan.Escanear(carpeta, Engine.VideoExtensions); }
        catch (Exception ex) { Aviso(string.Format(Textos.Instancia.OrganizarNoSeLeyoCarpeta, ex.Message)); }

        int carpetas = _ficheros.Select(f => LibraryScan.Grupo(carpeta, f)).Distinct().Count();

        lblFicheros.Text = _ficheros.Length switch
        {
            0 when !Directory.Exists(carpeta) => Textos.Instancia.OrganizarElegirCarpeta,
            0 => Textos.Instancia.OrganizarSinVideos,
            1 => string.Format(Textos.Instancia.OrganizarUnFichero, carpeta),
            // Decir en cuántas carpetas están confirma que el recorrido ha bajado: si sale «1»
            // sobre una serie con temporadas, se sabe al momento que se apuntó demasiado adentro.
            _ when carpetas > 1 => string.Format(Textos.Instancia.OrganizarFicherosEnCarpetas,
                                                 _ficheros.Length, carpetas, carpeta),
            _ => string.Format(Textos.Instancia.OrganizarFicherosEn, _ficheros.Length, carpeta),
        };

        // Volver a elegir carpeta invalida lo que hubiera en la tabla
        MostrarInicio();
        ActualizarEstado();
        ActualizarVinculo();
    }

    private void CambiarPlantilla()
    {
        var nueva = new LibraryTemplate(txtPlantilla.Text);
        if (nueva.Patron == _plantilla.Patron) return;
        _plantilla = nueva;
        txtPlantilla.Text = nueva.Patron;

        // La plantilla cambia el nombre propuesto de cada fila ya calculada
        foreach (var f in _filas) f.Recalcular();
        ActualizarContadores();
        RefrescarVistaPrevia();
    }

    /// <summary>Inserta la marca donde esté el cursor, no al final: se está editando un patrón.</summary>
    private void OnInsertarMarca(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string marca) return;

        int pos = txtPlantilla.SelectionStart;
        txtPlantilla.Text = txtPlantilla.Text.Remove(pos, txtPlantilla.SelectionLength).Insert(pos, marca);
        txtPlantilla.SelectionStart = pos + marca.Length;

        btnMarcas.IsChecked = false;
        txtPlantilla.Focus();
        CambiarPlantilla();
    }

    /// <summary>
    /// Enseña cómo queda la plantilla con un episodio DE VERDAD del catálogo elegido. Con un
    /// ejemplo inventado no se ve el problema de siempre: que el título real trae dos puntos,
    /// interrogaciones o tres segmentos encadenados.
    /// </summary>
    private void RefrescarVistaPrevia()
    {
        if (lblVistaPrevia == null) return;

        var ejemplo = _catalogoCargado?.Episodios.FirstOrDefault(x => x.TitulosSalida.Count > 0)
                      ?? _catalogoCargado?.Episodios.FirstOrDefault();

        if (_catalogoCargado == null || ejemplo == null)
        {
            lblVistaPrevia.Text = Textos.Instancia.OrganizarVistaPreviaSinCatalogo;
            return;
        }

        var muestra = _filas.FirstOrDefault()?.Res.Archivo ?? SignalExtractor.Extract("ejemplo.mkv", "");
        var nombre = new LibraryTemplate(txtPlantilla.Text).Render(_catalogoCargado, ejemplo, muestra);
        lblVistaPrevia.Text = nombre == null
            ? Textos.Instancia.OrganizarVistaPreviaSinNombre
            : Textos.Instancia.OrganizarVistaPreviaQuedaria + nombre;

        // El «Quedaría:» se corta casi siempre —estos títulos son larguísimos— así que el
        // ejemplo entero va también al globo, que es donde cabe entero.
        var globo = nombre == null
            ? Textos.Instancia.OrganizarPlantillaAyuda
            : Textos.Instancia.OrganizarPlantillaAyuda + "\n\n" +
              string.Format(Textos.Instancia.OrganizarPlantillaGlobo, _catalogoCargado.Serie, nombre);

        txtPlantilla.ToolTip = Globo(globo);
        lblVistaPrevia.ToolTip = Globo(globo);
    }

    /// <summary>
    /// Un globo con el texto ajustado. Cada llamada crea el suyo: un mismo elemento visual no
    /// puede colgar de dos sitios, así que compartirlo dejaría el segundo en blanco.
    /// </summary>
    private static TextBlock Globo(string texto) => new()
    {
        Text = texto,
        MaxWidth = 460,
        TextWrapping = TextWrapping.Wrap,
    };

    // ─────────────────────────── catálogos ───────────────────────────

    /// <summary>Abre el explorador del catálogo elegido, para verificar propuestas.</summary>
    private void AbrirExplorador()
    {
        if (_catalogoCargado == null)
        {
            Aviso(Textos.Instancia.OrganizarElegirCatalogoPrimero);
            return;
        }
        new CatalogoWindow(_catalogoCargado) { Owner = Window.GetWindow(this) }.Show();
    }

    private void ImportarCatalogo()
    {
        var dlg = new OpenFileDialog
        {
            Title = Textos.Instancia.OrganizarImportarTitulo,
            Filter = Textos.Instancia.OrganizarFiltroAbrir,
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var guardado = ReindexStore.ImportarCatalogo(dlg.FileName);
            _catalogoElegido = guardado;
            // Importar TAMBIÉN es elegir: sin esto, quitar un catálogo y reimportarlo dejaba
            // la preferencia vacía y el siguiente arranque caía al primero por alfabeto.
            ReindexStore.GuardarUltimoCatalogo(guardado.Ruta);
            CargarCatalogos();
            Escribir(string.Format(Textos.Instancia.OrganizarLogCatalogoImportado,
                                   guardado.Serie, guardado.Episodios));
            ActualizarEstado();
        }
        catch (ReindexCatalogException ex) { Aviso(ex.Message); }
        catch (Exception ex) { Aviso(string.Format(Textos.Instancia.OrganizarNoSePudoImportar, ex.Message)); }
    }

    /// <summary>
    /// Especificación del formato. Va al repositorio y no a un texto embebido a propósito:
    /// así se corrige sin publicar una versión, y siempre se lee la vigente.
    /// </summary>
    private const string UrlEspecificacion =
        "https://github.com/luishidalgoa/ondine/blob/main/docs/catalogo-reindex.md";

    private void AbrirEspecificacion()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UrlEspecificacion)
            { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Aviso(string.Format(Textos.Instancia.OrganizarNoSeAbrioDoc, ex.Message, UrlEspecificacion));
        }
    }

    /// <summary>
    /// Guarda un catálogo de ejemplo VÁLIDO para editar. Partir de algo que ya funciona
    /// evita el peor arranque posible: escribir el JSON a ciegas y que el primer intento
    /// de importar sea una lista de errores.
    /// </summary>
    /// <summary>
    /// Abre el generador del encargo para la IA. Se le sugiere el nombre de la serie que
    /// tengas elegida, que es el caso más común: ampliar un catálogo que ya usas.
    /// </summary>
    private void AbrirGeneradorDePrompt()
    {
        var ventana = new PromptWindow(_catalogoElegido?.Serie ?? "")
        { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
    }

    private void GuardarEjemplo()
    {
        var dlg = new SaveFileDialog
        {
            Title = Textos.Instancia.OrganizarGuardarEjemploTitulo,
            Filter = Textos.Instancia.OrganizarFiltroGuardar,
            FileName = "mi-serie.reindex.json",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dlg.FileName, ReindexCatalog.Ejemplo, new System.Text.UTF8Encoding(false));
            Escribir(string.Format(Textos.Instancia.OrganizarLogEjemploGuardado, dlg.FileName));

            var r = DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarTitulo,
                Textos.Instancia.OrganizarEjemploGuardado);
            if (r) AbrirEspecificacion();
        }
        catch (Exception ex) { Aviso(string.Format(Textos.Instancia.OrganizarNoSeGuardoEjemplo, ex.Message)); }
    }

    // ─────────────────────────── simulación ───────────────────────────

    private async void Simular()
    {
        // Se RE-ESCANEA siempre: tras aplicar, la lista vieja apunta a nombres que ya no
        // existen, y simular sobre ella re-resolvía el pasado — la tabla enseñaba los
        // mismos «Corregido» de antes como si aplicar no hubiera hecho nada.
        var carpetaActual = txtCarpeta.Text?.Trim() ?? "";
        // En un hilo aparte: enumerar cientos de ficheros sobre OneDrive es un viaje de red
        // por carpeta, y hecho en el hilo de interfaz congelaba el clic de «Analizar».
        try { _ficheros = await Task.Run(() => LibraryScan.Escanear(carpetaActual, Engine.VideoExtensions)); }
        catch { /* la carpeta puede haber desaparecido; el guard de abajo lo dice */ }

        if (_catalogoCargado == null || _ficheros.Length == 0) return;

        // Los que el catálogo marca como «déjalo como está» NO se filtran: siguen en la tabla,
        // pero el motor los saca en verde y sin propuesta. Quitarlos de la lista daría a entender
        // que el fichero ya no está en la carpeta, y sí está.
        int dejadosComoEstan = _catalogoCargado.DejarComoEsta.Count == 0
            ? 0
            : _ficheros.Count(_catalogoCargado.SeDejaComoEsta);
        if (dejadosComoEstan > 0)
            Escribir(string.Format(Textos.Instancia.OrganizarLogDejadosComoEstan, dejadosComoEstan));

        // Se recuerda esta carpeta para este catálogo: la próxima vez que lo elijas se pre-rellena
        // sola y no tienes que volver a emparejar carpeta y catálogo.
        if (_catalogoElegido != null)
        {
            ReindexStore.GuardarCarpetaDeCatalogo(_catalogoElegido.Ruta, carpetaActual);
            ActualizarVinculo();   // que se vea al momento que ha quedado vinculada
        }

        // Las etapas viven en la pantalla de inicio: al re-simular desde la revisión (botón
        // de abajo) no hay dónde pintarlas y se va directo al resultado.
        bool animar = vistaInicio.Visibility == Visibility.Visible;

        btnSimular.IsEnabled = btnSimularGrande.IsEnabled = btnCarpeta.IsEnabled = false;
        lblEstadoOrg.Text = Textos.Instancia.OrganizarIdentificando;

        if (animar)
        {
            panelReposo.Visibility = Visibility.Collapsed;
            panelEtapas.Visibility = Visibility.Visible;
            _pasos.Reiniciar();
            EncenderHaz();
        }

        var catalogo = _catalogoCargado;
        var ficheros = _ficheros;
        var decisiones = _decisiones;
        var modo = ModoActual();   // se lee AQUÍ: dentro del Task.Run no se puede tocar el ComboBox

        try
        {
            // ── Etapa 1: leer las señales de los nombres ──
            if (animar) _pasos.EnCurso(0);
            var señales = await ConTiempoDeVerse(Task.Run(() =>
                // Nota: el título del metadato del contenedor (titulo_meta) todavía no se lee.
                // Exigiría un ffprobe por fichero y «Simular» dejaría de ser inmediato en
                // bibliotecas de cientos. El motor ya lo admite cuando se enganche.
                ficheros
                    .Select(f => SignalExtractor.Extract(f, new DirectoryInfo(Path.GetDirectoryName(f)!).Name))
                    .ToList()), animar);
            if (animar)
                _pasos.Hecha(0, señales.Count == 1
                    ? Textos.Instancia.OrganizarPasoUnNombre
                    : string.Format(Textos.Instancia.OrganizarPasoNombres, señales.Count));

            // ── Etapa 2: el motor, fuera del hilo de interfaz ──
            if (animar) _pasos.EnCurso(1);
            var resoluciones = await ConTiempoDeVerse(
                Task.Run(() => ReindexEngine.Resolve(señales, catalogo, decisiones, modo)), animar);

            // ── Etapa 2b: metadatos SOLO de los dudosos ──
            // El contenedor suele llevar el título grabado («title» del MKV) aunque el
            // nombre no lo traiga — el caso de los S2018E01 pelados. Se lee únicamente de
            // los que quedaron en duda, y de esos solo los que están en el disco: abrir un
            // fichero sincronizado «bajo demanda» lo descarga entero. Tope de 80.
            _dudososEnNube = 0;   // cada simulación parte de cero
            var dudosos = resoluciones
                .Where(x => x.EsDuda && string.IsNullOrEmpty(x.Archivo.TituloMeta) &&
                            string.IsNullOrEmpty(x.Archivo.Error))
                .Select(x => x.Archivo.Path)
                .Take(80)
                .ToList();
            if (dudosos.Count > 0)
            {
                Escribir(string.Format(Textos.Instancia.OrganizarLogBuscandoTitulos, dudosos.Count));
                var metadatos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int enNube = 0;
                await Task.Run(async () =>
                {
                    // De ocho en ocho: cada sondeo levanta un proceso de ffprobe, y ochenta
                    // a la vez castigan el disco sin acabar antes.
                    using var turno = new SemaphoreSlim(8);
                    var tareas = dudosos.Select(async ruta =>
                    {
                        await turno.WaitAsync();
                        try
                        {
                            // El .nfo compañero primero: es un XML minúsculo y trae el título
                            // en limpio.
                            var nfo = Path.ChangeExtension(ruta, ".nfo");
                            string? t = null;
                            if (File.Exists(nfo))
                                try { t = NfoTitulo.Extraer(File.ReadAllText(nfo)); } catch { }

                            // Y si no lo hay, el contenedor... salvo que el vídeo sea un
                            // marcador de «Archivos a petición»: abrirlo para mirar una
                            // etiqueta se lo descargaría ENTERO (medido: 277 MB en 18 s).
                            // Identificar una carpeta no puede gastarle a nadie gigabytes
                            // sin avisar, así que ahí se para.
                            if (t == null)
                            {
                                if (Nube.EsMarcador(ruta)) Interlocked.Increment(ref enNube);
                                else t = await Engine.LeerTituloAsync(ruta);
                            }
                            if (t != null) lock (metadatos) metadatos[ruta] = t;
                        }
                        finally { turno.Release(); }
                    });
                    await Task.WhenAll(tareas);
                });

                _dudososEnNube = enNube;
                if (enNube > 0)
                    Escribir(string.Format(Textos.Instancia.OrganizarLogSoloEnLaNube, enNube));

                if (metadatos.Count > 0)
                {
                    // Con los títulos en la mano, se re-resuelve TODO el lote: las reglas de
                    // deduplicación miran al conjunto y parchear filas sueltas las esquivaría.
                    for (int i = 0; i < señales.Count; i++)
                        if (metadatos.TryGetValue(señales[i].Path, out var titulo))
                            señales[i] = SignalExtractor.Extract(señales[i].Path, señales[i].Carpeta, titulo);
                    resoluciones = await Task.Run(() => ReindexEngine.Resolve(señales, catalogo, decisiones, modo));
                    Escribir(string.Format(Textos.Instancia.OrganizarLogTitulosMetadatos, metadatos.Count));
                }
            }

            if (animar) _pasos.Hecha(1, string.Format(Textos.Instancia.OrganizarPasoContra, catalogo.Serie));

            // ── Etapa 3: montar la tabla ──
            // El respiro de antes deja al arco pintarse: montar filas bloquea el hilo de
            // interfaz y sin él esta etapa pasaría de pendiente a hecha sin verse en curso.
            if (animar) { _pasos.EnCurso(2); await Task.Delay(220); }

            var raiz = txtCarpeta.Text?.Trim() ?? "";
            _filas.Clear();
            // Un análisis nuevo empieza en el orden natural por temporada: se retira cualquier
            // orden por cabecera que hubiera quedado de antes.
            QuitarOrdenManual();
            foreach (var r in resoluciones)
            {
                var fila = new OrganizarRow(r, catalogo, _plantilla,
                    LibraryScan.Etiqueta(LibraryScan.Grupo(raiz, r.Archivo.Path)));
                // Lo que apartaste la última vez sigue apartado: es toda la razón de ser de
                // la cola — no volver a buscarlo entre cientos al reabrir la app.
                if (_revision.Tiene(fila.RutaActual)) fila.Apartada = true;
                _filas.Add(fila);
            }

            int temporadas = RecalcularSeparadores();
            int listos = _filas.Count(f => f.ListoParaAplicar);
            if (animar)
            {
                _pasos.Hecha(2);
                // La fusión final: los tres pasos se funden en un solo check con destello.
                // Merece verse entera antes de saltar a la tabla — es la recompensa.
                _pasos.Terminado(Textos.Instancia.OrganizarPasoListo,
                    listos == 1
                        ? Textos.Instancia.OrganizarPasoUnListo
                        : string.Format(Textos.Instancia.OrganizarPasoListos, listos));
                await Task.Delay(1100);
            }

            MostrarRevision();
            ActualizarContadores();
            Escribir(string.Format(Textos.Instancia.OrganizarLogAnalisis, _filas.Count, catalogo.Serie) +
                     (temporadas > 0
                         ? string.Format(Textos.Instancia.OrganizarLogAnalisisTemporadas, temporadas)
                         : "."));
        }
        catch (Exception ex) { Aviso(string.Format(Textos.Instancia.OrganizarAnalisisFallo, ex.Message)); }
        finally
        {
            if (animar)
            {
                ApagarHaz();
                panelEtapas.Visibility = Visibility.Collapsed;
                panelReposo.Visibility = Visibility.Visible;
            }
            btnCarpeta.IsEnabled = true;
            ActualizarEstado();
        }
    }

    /// <summary>
    /// Espera la tarea, y con animación le garantiza un mínimo en pantalla: una etapa que
    /// entra y sale en 40 ms no informa, parpadea.
    /// </summary>
    private static async Task<T> ConTiempoDeVerse<T>(Task<T> tarea, bool animar)
    {
        if (animar) await Task.WhenAll(tarea, Task.Delay(300));
        return await tarea;
    }

    // ── el haz que rodea el panel mientras se identifica ──

    private void EncenderHaz()
    {
        hazFicheros.BeginAnimation(OpacityProperty,
            new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromMilliseconds(180)));
        if (hazFicheros.BorderBrush is System.Windows.Media.LinearGradientBrush b &&
            b.RelativeTransform is System.Windows.Media.RotateTransform rt)
            rt.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty,
                new System.Windows.Media.Animation.DoubleAnimation(0, 360, TimeSpan.FromSeconds(2.8))
                { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever });
    }

    private void ApagarHaz()
    {
        hazFicheros.BeginAnimation(OpacityProperty,
            new System.Windows.Media.Animation.DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
        if (hazFicheros.BorderBrush is System.Windows.Media.LinearGradientBrush b &&
            b.RelativeTransform is System.Windows.Media.RotateTransform rt)
            rt.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
    }

    /// <summary>
    /// Descarta la simulación y vuelve a la pantalla de inicio.
    ///
    /// No se pregunta nada porque no se pierde nada: las decisiones que hayas tomado a mano
    /// se guardan en disco en cuanto las tomas, y al volver a simular se reaplican solas
    /// («Lo decidiste tú antes»). Lo único que se tira es el cálculo, que se rehace en
    /// segundos.
    /// </summary>
    private void VolverAlInicio()
    {
        MostrarInicio();
        ActualizarContadores();
        ActualizarEstado();
    }

    private void MostrarInicio()
    {
        _filas.Clear();
        vistaInicio.Visibility = Visibility.Visible;
        vistaRevision.Visibility = Visibility.Collapsed;
        filaChips.Visibility = Visibility.Collapsed;
        bannerAplicado.Visibility = Visibility.Collapsed;
    }

    private void MostrarRevision()
    {
        vistaInicio.Visibility = Visibility.Collapsed;
        vistaRevision.Visibility = Visibility.Visible;
        filaChips.Visibility = Visibility.Visible;
        bannerAplicado.Visibility = Visibility.Collapsed;
    }

    // ─────────────────────────── contadores y filtro ───────────────────────────

    private void ActualizarContadores()
    {
        // EstadoVisible, no Res.Estado: lo ya aplicado cuenta como limpio — está bien en el
        // disco y no queda nada que hacerle.
        int limpios = _filas.Count(f => f.EstadoVisible == ReindexEstado.Limpio);
        int corregidos = _filas.Count(f => f.EstadoVisible == ReindexEstado.Corregido);
        int especiales = _filas.Count(f => f.EstadoVisible == ReindexEstado.Especial);
        int conflictos = _filas.Count(f => f.EstadoVisible == ReindexEstado.Conflicto);
        int errores = _filas.Count(f => f.EstadoVisible == ReindexEstado.Error);

        runLimpios.Text = string.Format(Textos.Instancia.OrganizarChipCorrectos, limpios);
        runCorregidos.Text = string.Format(Textos.Instancia.OrganizarChipConCambios, corregidos);
        runEspeciales.Text = string.Format(Textos.Instancia.OrganizarChipEspeciales, especiales);
        runConflictos.Text = string.Format(Textos.Instancia.OrganizarChipConflictos, conflictos);
        runErrores.Text = string.Format(Textos.Instancia.OrganizarChipErrores, errores);

        ActualizarBotonCola();

        chipEspeciales.IsEnabled = especiales > 0;
        chipConflictos.IsEnabled = conflictos > 0;
        chipErrores.IsEnabled = errores > 0;
        btnConfirmarEspeciales.IsEnabled = especiales > 0;

        int listos = _filas.Count(f => f.ListoParaAplicar);
        int marcados = _filas.Count(f => f.ListoParaAplicar && f.Marcado);
        int dudas = _filas.Count(f => f.EsDuda);

        // El botón dice EXACTAMENTE cuántos va a tocar. Si hay listos sin marcar, se nota
        // en el propio texto («12 de 400»): aplicar nunca lleva sorpresa dentro.
        lblAplicar.Text = marcados == 0 ? Textos.Instancia.Aplicar
            : marcados == listos ? string.Format(Textos.Instancia.OrganizarAplicarMarcados, marcados)
            : string.Format(Textos.Instancia.OrganizarAplicarDe, marcados, listos);
        btnAplicar.IsEnabled = marcados > 0;
        btnAplicar.ToolTip = Textos.Instancia.OrganizarAplicarAyudaDetalle;
        btnAceptarVerdes.IsEnabled = listos > 0;

        // Partir solo tiene sentido sobre lo YA identificado y con más de una historia dentro.
        // Comparar con el catálogo solo tiene sentido cuando hay algo analizado.
        btnQueFalta.IsEnabled = _catalogoCargado != null && _filas.Count > 0;

        int partibles = FilasPartibles().Count;
        btnPartirSegmentos.IsEnabled = partibles > 0;
        btnPartirSegmentos.Content = partibles > 0
            ? string.Format(Textos.Instancia.OrganizarPartirSegmentosN, partibles)
            : Textos.Instancia.OrganizarPartirSegmentos;

        // Los que ya estaban bien se dicen aparte: si no, «383 listos · 165 por despachar» sobre
        // 548 deja 0 sin explicar y parece que se han perdido por el camino.
        int hechos = _filas.Count(f => f.SinCambios);
        lblEstadoOrg.Text = string.Format(Textos.Instancia.OrganizarResumen, _filas.Count, listos, dudas)
                            + (hechos > 0
                                ? string.Format(Textos.Instancia.OrganizarResumenYaBien, hechos) : "")
                            + (_dudososEnNube > 0
                                ? string.Format(Textos.Instancia.OrganizarResumenNube, _dudososEnNube) : "");

        // Si la mayoría son dudas, se dice de frente en vez de dejar que lo descubra fila a fila
        if (_filas.Count > 0 && dudas > _filas.Count / 2)
        {
            lblBannerAviso.Text = string.Format(Textos.Instancia.OrganizarBannerDudas, dudas, _filas.Count)
                                  + ExplicarPorQueTantasDudas();
            bannerAviso.Visibility = Visibility.Visible;
        }
        else bannerAviso.Visibility = Visibility.Collapsed;
    }

    private string ExplicarPorQueTantasDudas()
    {
        var avisos = _catalogoCargado?.Advertencias ?? Array.Empty<string>();
        // Se compara con el aviso ENTERO, no con un trozo suelto de su texto: los avisos
        // del catálogo ya vienen traducidos, y un «Contains("Sin fechas")» solo acertaba
        // con la app en castellano — en inglés esta explicación no salía nunca.
        if (avisos.Contains(Textos.Instancia.ReindexAvisoSinNingunaFecha))
            return Textos.Instancia.OrganizarDudasSinFechas;
        return Textos.Instancia.OrganizarDudasRevisa;
    }

    /// <summary>
    /// Marca qué fila abre cada temporada, que es la que lleva encima la banda separadora.
    ///
    /// Se calcula sobre la vista YA FILTRADA, no sobre <c>_filas</c>: si escondes las limpias,
    /// la banda tiene que salir sobre la primera que quede —no sobre una que no se ve— y el
    /// recuento tiene que ser el de las visibles.
    ///
    /// Solo se separa si hay más de una carpeta: con una sola, la banda repetiría lo que ya
    /// dice el cuadro de la carpeta y se comería una fila por nada.
    /// </summary>
    /// <summary>
    /// Volver a pinchar una fila ya abierta la cierra. Sin esto, el resolutor se queda
    /// desplegado y la única forma de recogerlo es abrir otra fila.
    ///
    /// Solo cuenta el clic sobre una CELDA: dentro del desplegable hay botones («Cambiar a
    /// E318»), y si el clic ahí también cerrara la fila, elegir un candidato sería imposible.
    /// </summary>
    private void OnTablaClic(object sender, MouseButtonEventArgs e)
    {
        // Doble clic = ver el video en el reproductor del sistema: ante la duda de que
        // capitulo es, mirarlo gana a cualquier metadato. Va antes que el cierre de fila
        // para que el segundo clic no la recoja.
        if (e.ClickCount == 2 &&
            Ascender<CheckBox>(e.OriginalSource as DependencyObject) == null &&
            Ascender<DataGridRow>(e.OriginalSource as DependencyObject)?.Item is OrganizarRow filaVideo)
        {
            // Reproductor INTEGRADO en modo focus, no una ventana del sistema: la pregunta
            // es «¿qué capítulo es?» y la respuesta debe estar a un Esc de distancia. Si el
            // códec no está soportado, la propia ventana ofrece el reproductor del sistema.
            ReproducirFila(filaVideo);
            e.Handled = true;
            return;
        }

        // Un clic que nace en una casilla es PARA la casilla. Este manejador oye el clic
        // antes que ella (los Preview van de fuera adentro), y sin esta salida se lo comía
        // cuando la fila estaba seleccionada: la casilla parecía muerta con el ratón — y la
        // verificación por accesibilidad no lo cazó porque conmuta sin pasar por el ratón.
        // De paso, aquí arranca el marcado por arrastre: se anota el valor del primer
        // toque y el movimiento lo va contagiando a las filas que cruces.
        if (Ascender<CheckBox>(e.OriginalSource as DependencyObject) is
            { DataContext: OrganizarRow filaMarca } && filaMarca.ListoParaAplicar)
        {
            _pintando = !filaMarca.Marcado;
            filaMarca.Marcado = _pintando.Value;
            ActualizarContadores();
            e.Handled = true;   // que el CheckBox no vuelva a conmutar lo ya conmutado
            return;
        }

        var celda = Ascender<DataGridCell>(e.OriginalSource as DependencyObject);
        if (celda == null) return;

        var fila = Ascender<DataGridRow>(celda);
        if (fila is not { IsSelected: true }) return;

        tabla.SelectedItem = null;
        e.Handled = true;
    }

    /// <summary>Valor que se está «pintando» al arrastrar sobre las casillas. Null = no hay arrastre.</summary>
    private bool? _pintando;

    /// <summary>
    /// Marca en tanda: con el botón pulsado, cada fila que cruces recibe el valor del primer
    /// toque (no se alterna fila a fila, que dejaría un patrón de ajedrez si pasas dos veces).
    /// </summary>
    private void OnTablaArrastre(object sender, MouseEventArgs e)
    {
        if (_pintando is not bool valor) return;
        if (e.LeftButton != MouseButtonState.Pressed) { _pintando = null; return; }

        var fila = Ascender<DataGridRow>(e.OriginalSource as DependencyObject);
        if (fila?.Item is OrganizarRow f && f.ListoParaAplicar && f.Marcado != valor)
        {
            f.Marcado = valor;
            ActualizarContadores();
        }
    }

    private static T? Ascender<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d != null && d is not T) d = VisualTreeHelper.GetParent(d);
        return d as T;
    }

    /// <summary>
    /// Orden por cabecera con tri-estado: 1.º clic ascendente, 2.º descendente, 3.º sin orden.
    /// «Sin orden» devuelve el orden natural por temporada —el único en que las bandas de
    /// separación tienen sentido—, de modo que mientras hay orden manual esas bandas se ocultan.
    /// </summary>
    private void OnTablaSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;   // gestionamos el ciclo a mano para poder «quitar el orden»
        var col = e.Column;
        if (string.IsNullOrEmpty(col.SortMemberPath)) return;

        var vista = CollectionViewSource.GetDefaultView(_filas);
        if (vista == null) return;

        ListSortDirection? siguiente = col.SortDirection switch
        {
            null => ListSortDirection.Ascending,
            ListSortDirection.Ascending => ListSortDirection.Descending,
            _ => null,   // descendente → se quita el orden
        };

        foreach (var c in tabla.Columns) if (c != col) c.SortDirection = null;
        col.SortDirection = siguiente;
        vista.SortDescriptions.Clear();

        if (siguiente is { } dir)
            vista.SortDescriptions.Add(new SortDescription(col.SortMemberPath, dir));

        _ordenManual = siguiente != null;
        RecalcularSeparadores();   // reordena las bandas (o las oculta si hay orden manual)
    }

    /// <summary>Retira el orden por cabecera y vuelve al orden natural por temporada.</summary>
    private void QuitarOrdenManual()
    {
        _ordenManual = false;
        foreach (var c in tabla.Columns) c.SortDirection = null;
        var vista = CollectionViewSource.GetDefaultView(_filas);
        vista?.SortDescriptions.Clear();
    }

    /// <returns>Cuántas temporadas han quedado separadas. 0 = no hay nada que separar.</returns>
    private int RecalcularSeparadores()
    {
        var vista = CollectionViewSource.GetDefaultView(_filas);
        if (vista == null) return 0;

        var visibles = vista.Cast<OrganizarRow>().ToList();
        foreach (var f in visibles) { f.PrimeraDeGrupo = false; f.GrupoConteo = ""; }

        // Con un orden por cabecera activo las temporadas se entremezclan, así que las bandas
        // de separación dejan de significar nada: se ocultan hasta que se quite el orden.
        if (_ordenManual) return 0;

        if (visibles.Select(f => f.Grupo).Distinct().Count() <= 1) return 0;

        int bandas = 0;
        for (int i = 0; i < visibles.Count;)
        {
            int j = i;
            while (j < visibles.Count && visibles[j].Grupo == visibles[i].Grupo) j++;

            visibles[i].PrimeraDeGrupo = true;
            visibles[i].GrupoConteo = j - i == 1
                ? Textos.Instancia.OrganizarGrupoUnFichero
                : string.Format(Textos.Instancia.OrganizarGrupoFicheros, j - i);
            bandas++;
            i = j;
        }
        return bandas;
    }

    private void AplicarFiltro()
    {
        var vista = CollectionViewSource.GetDefaultView(_filas);
        if (vista == null) return;

        bool soloDudas = chipDudas.IsChecked == true;
        var estados = new List<ReindexEstado>();
        if (chipLimpios.IsChecked == true) estados.Add(ReindexEstado.Limpio);
        if (chipCorregidos.IsChecked == true) estados.Add(ReindexEstado.Corregido);
        if (chipEspeciales.IsChecked == true) estados.Add(ReindexEstado.Especial);
        if (chipConflictos.IsChecked == true) estados.Add(ReindexEstado.Conflicto);
        if (chipErrores.IsChecked == true) estados.Add(ReindexEstado.Error);

        // El texto filtra con la normalización del identificador: «sonrisa» encuentra
        // «¡En busca de una sonrisa!» aunque el nombre lleve signos y tildes.
        var q = TitleMatch.Norm(txtBuscarTabla.Text);
        bool PasaTexto(OrganizarRow f)
        {
            if (q.Length == 0) return true;
            // Una fila YA APLICADA solo se encuentra por su nombre nuevo: el viejo ya no
            // existe en disco, y que siguiera apareciendo al buscarlo hacía dudar de si el
            // renombrado había ocurrido de verdad.
            if (f.Aplicado)
                return TitleMatch.Norm(f.NombreNuevo ?? f.Original).Contains(q, StringComparison.Ordinal);
            return TitleMatch.Norm(f.Original).Contains(q, StringComparison.Ordinal)
                || TitleMatch.Norm(f.Propuesta).Contains(q, StringComparison.Ordinal);
        }

        if (estados.Count == 0 && !soloDudas && q.Length == 0)
        { vista.Filter = null; RecalcularSeparadores(); return; }

        vista.Filter = o =>
        {
            if (o is not OrganizarRow f) return false;
            if (soloDudas && !f.EsDuda) return false;
            if (!PasaTexto(f)) return false;
            return estados.Count == 0 || estados.Contains(f.EstadoVisible);
        };

        // Las bandas dependen de lo que quede visible, así que se rehacen tras cada filtro
        RecalcularSeparadores();
    }

    /// <summary>Una entrada de la cola, tal como se ve en el desplegable.</summary>
    private sealed class EnLaCola
    {
        public required string Ruta { get; init; }
        public required string Nombre { get; init; }
        /// <summary>Dónde está o en qué estado: para reconocerlo sin abrir nada.</summary>
        public required string Detalle { get; init; }
    }

    /// <summary>
    /// Mete la fila en la cola, o la saca si ya estaba. UN CLIC: no pregunta nada.
    ///
    /// Es una cola de las de «esto lo miro luego», no un formulario. Cada pregunta de más
    /// entre el impulso y el resultado es una razón para no usarla.
    /// </summary>
    private void AlternarApartada(OrganizarRow? f)
    {
        if (f == null) return;

        if (f.Apartada)
        {
            _revision.Sacar(f.RutaActual);
            f.Apartada = false;
            Escribir(string.Format(Textos.Instancia.OrganizarLogSaleDeLaCola, f.Original));
        }
        else
        {
            _revision.Meter(f.RutaActual);
            f.Apartada = true;
            Escribir(string.Format(Textos.Instancia.OrganizarLogALaCola, f.Original));
        }

        GuardarRevision();
        ActualizarBotonCola();
        if (btnCola.IsChecked == true) PintarCola();
    }

    private void GuardarRevision()
    {
        try { ReindexStore.GuardarRevision(_revision); }
        catch (Exception ex)
        { Escribir(string.Format(Textos.Instancia.OrganizarLogNoSeGuardoCola, ex.Message)); }
    }

    /// <summary>El botón lleva el número puesto: sin abrirlo ya sabes si hay algo dentro.</summary>
    private void ActualizarBotonCola()
    {
        int n = _revision.Cuantos;
        runCola.Text = n == 0
            ? Textos.Instancia.OrganizarCola
            : string.Format(Textos.Instancia.OrganizarColaN, n);
        btnCola.IsEnabled = true;   // se puede abrir vacía: dice cómo se llena
    }

    /// <summary>
    /// Rehace la lista del desplegable. Se hace al ABRIRLO y no en cada cambio: es el único
    /// momento en que se mira, y así una cola larga no cuesta nada mientras trabajas.
    /// </summary>
    private void PintarCola()
    {
        var enTabla = _filas.ToDictionary(f => f.RutaActual, f => f, StringComparer.OrdinalIgnoreCase);

        var items = _revision.Todos.Select(a =>
        {
            enTabla.TryGetValue(a.Ruta, out var fila);
            return new EnLaCola
            {
                Ruta = a.Ruta,
                Nombre = Path.GetFileName(a.Ruta),
                // Si el fichero no está en esta simulación se dice, en vez de dejar que
                // pulses y no pase nada: casi siempre es que estás en otra carpeta.
                Detalle = fila != null
                    ? $"{fila.EstadoTexto} · {Path.GetFileName(Path.GetDirectoryName(a.Ruta)) ?? ""}"
                    : (File.Exists(a.Ruta)
                        ? Textos.Instancia.OrganizarColaNoEnAnalisis
                        : Textos.Instancia.OrganizarColaNoEstaEnDisco),
            };
        }).ToList();

        listaCola.ItemsSource = items;
        bool hay = items.Count > 0;
        visorCola.Visibility = hay ? Visibility.Visible : Visibility.Collapsed;
        lblColaVacia.Visibility = hay ? Visibility.Collapsed : Visibility.Visible;
        btnVaciarCola.IsEnabled = hay;
    }

    /// <summary>
    /// Saltar al fichero: se selecciona y se trae a la vista. Si no está en esta simulación
    /// —otra carpeta, otra serie— se abre su carpeta, que es lo único útil que queda.
    /// </summary>
    private void OnIrAFicheroDeLaCola(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string ruta) return;

        var fila = _filas.FirstOrDefault(f =>
            string.Equals(f.RutaActual, ruta, StringComparison.OrdinalIgnoreCase));

        if (fila == null)
        {
            btnCola.IsChecked = false;
            if (File.Exists(ruta)) AbrirCarpetaDe(ruta);
            else Aviso(string.Format(Textos.Instancia.OrganizarColaYaNoEsta, Path.GetFileName(ruta)));
            return;
        }

        btnCola.IsChecked = false;
        // Un filtro puesto puede estar escondiendo justo esta fila, y entonces «ir» no
        // llevaría a ninguna parte. Se quita: has pedido ver ESTE fichero.
        if (CollectionViewSource.GetDefaultView(_filas)?.Filter != null &&
            !CollectionViewSource.GetDefaultView(_filas)!.Filter(fila))
            LimpiarFiltros();

        tabla.SelectedItem = fila;
        tabla.ScrollIntoView(fila);
        tabla.Focus();
    }

    private void OnQuitarDeLaCola(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string ruta) return;
        _revision.Sacar(ruta);
        foreach (var f in _filas)
            if (string.Equals(f.RutaActual, ruta, StringComparison.OrdinalIgnoreCase))
                f.Apartada = false;
        GuardarRevision();
        ActualizarBotonCola();
        PintarCola();
    }

    private void VaciarCola()
    {
        if (_revision.Cuantos == 0) return;
        if (!DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarVaciarCola,
                string.Format(Textos.Instancia.OrganizarVaciarColaMensaje, _revision.Cuantos)))
            return;

        foreach (var a in _revision.Todos.ToList()) _revision.Sacar(a.Ruta);
        foreach (var f in _filas) f.Apartada = false;
        GuardarRevision();
        ActualizarBotonCola();
        PintarCola();
        Escribir(Textos.Instancia.OrganizarLogColaVaciada);
    }

    /// <summary>Mete rutas en la cola y abre el desplegable. Solo para las pruebas.</summary>
    internal void ColaDePrueba(params string[] rutas)
    {
        foreach (var r in rutas) _revision.Meter(r);
        ActualizarBotonCola();
        btnCola.IsChecked = true;
        PintarCola();
    }

    private void LimpiarFiltros()
    {
        chipLimpios.IsChecked = chipCorregidos.IsChecked = chipEspeciales.IsChecked =
            chipConflictos.IsChecked = chipErrores.IsChecked = chipDudas.IsChecked = false;
        txtBuscarTabla.Text = "";
        AplicarFiltro();
    }

    private void FiltrarSolo(ReindexEstado estado)
    {
        chipLimpios.IsChecked = chipCorregidos.IsChecked = chipConflictos.IsChecked = chipErrores.IsChecked = false;
        chipDudas.IsChecked = false;
        chipEspeciales.IsChecked = estado == ReindexEstado.Especial;
        AplicarFiltro();
    }

    /// <summary>Da por buenas las filas verdes: no cambia nada, solo confirma lo evidente.</summary>
    private void AceptarVerdes()
    {
        int n = _filas.Count(f => f.ListoParaAplicar);
        Escribir(string.Format(Textos.Instancia.OrganizarLogVerdesAceptadas, n));
        chipDudas.IsChecked = true;   // lo interesante ya es solo lo que falta por decidir
    }

    // ─────────────────────────── resolutor ───────────────────────────

    private void OnElegirCandidato(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not CatalogEpisode ep) return;
        if (tabla.SelectedItem is not OrganizarRow fila) return;

        fila.ElegirEpisodio(ep);
        RecordarDecision(fila, ep);
        ActualizarContadores();
        Escribir(string.Format(Textos.Instancia.OrganizarLogElegidoAMano, fila.Original, ep.Num));
    }

    /// <summary>
    /// Abre el explorador en modo elegir, arrancando con el título del fichero ya buscado:
    /// lo normal es que el episodio correcto esté a un golpe de vista.
    /// </summary>
    private void OnElegirAMano(object sender, RoutedEventArgs e)
    {
        if (tabla.SelectedItem is not OrganizarRow fila || _catalogoCargado == null) return;

        var win = new CatalogoWindow(_catalogoCargado, fila.Res.Archivo.TituloNombre, modoElegir: true)
        { Owner = Window.GetWindow(this) };

        if (win.ShowDialog() != true || win.Elegido is not { } ep) return;

        fila.ElegirEpisodio(ep, win.SegElegido);
        RecordarDecision(fila, ep, win.SegElegido);
        ActualizarContadores();
        Escribir(win.SegElegido == null
            ? string.Format(Textos.Instancia.OrganizarLogElegidoExplorador, fila.Original, ep.Num)
            : string.Format(Textos.Instancia.OrganizarLogElegidaHistoria,
                            fila.Original, win.SegElegido, ep.Num));
    }

    // ───────────────── Partir un episodio en sus mini-historias ─────────────────

    /// <summary>
    /// Las filas que se pueden partir: identificadas, sin tocar todavía y cuyo episodio trae más
    /// de una historia. Un fichero que ya es una sola historia no se parte.
    /// </summary>
    private List<OrganizarRow> FilasPartibles() =>
        _filas.Where(f => !f.Aplicado
                          && f.Res.Episodio is { } ep && ep.TitulosSalida.Count > 1
                          && f.Res.Archivo.SubSegmento == null
                          && f.Res.Confianza == ReindexConfianza.Alta)
              .ToList();

    /// <summary>
    /// Deja un fichero por mini-historia, numeradas «1a», «1b», «1c». El reparto lo decide
    /// <see cref="SegmentSplitter"/> con los fundidos a negro y el número de historias que dice
    /// el catálogo; el corte va sin recodificar, así que no pierde calidad y tarda un suspiro.
    ///
    /// Los que no tengan un corte claro se dejan como están y se listan al final: cortar a ojo
    /// partiría una escena por la mitad.
    /// </summary>
    private async void OnPartirSegmentos(object sender, RoutedEventArgs e)
    {
        var candidatas = FilasPartibles();
        if (candidatas.Count == 0 || _catalogoCargado == null) return;

        int trozos = candidatas.Sum(f => f.Res.Episodio!.TitulosSalida.Count);
        if (!DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarPartirTitulo,
                string.Format(Textos.Instancia.OrganizarPartirMensaje, candidatas.Count, trozos),
                Textos.Instancia.OrganizarPartirBoton, Textos.Instancia.Cancelar))
            return;

        btnPartirSegmentos.IsEnabled = false;
        var plantilla = new LibraryTemplate(txtPlantilla.Text);
        int hechos = 0;
        var sinCorte = new List<string>();

        foreach (var fila in candidatas)
        {
            var ruta = fila.RutaActual;
            var ep = fila.Res.Episodio!;
            int n = ep.TitulosSalida.Count;
            Escribir(string.Format(Textos.Instancia.OrganizarLogPartiendo, Path.GetFileName(ruta), n));
            try
            {
                var duracion = await SegmentSplitRunner.DuracionAsync(ruta);
                var negros = await SegmentSplitRunner.DetectarNegrosAsync(ruta);
                var plan = SegmentSplitter.Planificar(duracion, n, negros);
                if (!plan.Fiable)
                {
                    sinCorte.Add(Path.GetFileName(ruta));
                    Escribir(string.Format(Textos.Instancia.OrganizarLogSinCorteClaro, plan.Motivo));
                    continue;
                }

                // Se escriben con nombre temporal y solo al final se dejan con el suyo: si algo
                // falla a medias, no queda una mezcla de trozos buenos con nombres definitivos.
                var carpeta = Path.GetDirectoryName(ruta)!;
                var ext = Path.GetExtension(ruta);
                var salidas = new List<(string tmp, string final)>();
                bool todo = true;
                for (int i = 0; i < plan.Trozos.Count; i++)
                {
                    var seg = SegmentSplitter.Letra(i);
                    var nombre = plantilla.Render(_catalogoCargado, ep, fila.Res.Archivo.ConSegmento(seg))
                                 ?? $"{Path.GetFileNameWithoutExtension(ruta)}{seg}{ext}";
                    var tmp = Path.Combine(carpeta, $"~part{i}_{Guid.NewGuid():N}{ext}");
                    if (!await SegmentSplitRunner.ExtraerAsync(ruta, tmp, plan.Trozos[i]))
                    { todo = false; break; }
                    salidas.Add((tmp, Path.Combine(carpeta, nombre)));
                }

                if (!todo)
                {
                    foreach (var s in salidas) try { File.Delete(s.tmp); } catch { }
                    sinCorte.Add(Path.GetFileName(ruta));
                    continue;
                }

                // El original a la papelera ANTES de renombrar: uno de los trozos puede querer
                // llamarse igual que él, y entonces el destino estaría ocupado.
                PapeleraApp.Enviar(ruta);
                foreach (var s in salidas)
                {
                    try { if (File.Exists(s.final)) File.Delete(s.final); } catch { }
                    File.Move(s.tmp, s.final);
                }
                _filas.Remove(fila);
                hechos++;
            }
            catch (Exception ex)
            {
                sinCorte.Add(Path.GetFileName(ruta));
                Escribir(string.Format(Textos.Instancia.OrganizarLogNoSePudoPartir, ex.Message));
            }
        }

        ActualizarContadores();
        // La lista de los que se quedaron sin cortar se monta aparte —cinco nombres y unos
        // puntos suspensivos si hay más— para que la frase entera quepa en una sola clave.
        var lista = string.Join(", ", sinCorte.Take(5)) + (sinCorte.Count > 5 ? "…" : "");
        Escribir(sinCorte.Count == 0
            ? string.Format(Textos.Instancia.OrganizarLogPartidosOk, hechos)
            : string.Format(Textos.Instancia.OrganizarLogPartidosConFallos,
                            hechos, sinCorte.Count, lista));
        btnPartirSegmentos.IsEnabled = FilasPartibles().Count > 0;
    }

    private static OrganizarRow? FilaDe(object sender) =>
        (sender as FrameworkElement)?.DataContext as OrganizarRow;

    /// <summary>«Enviar este a la Papelera»: manda a la Papelera ESTE fichero (la copia repetida).</summary>
    private void OnBorrarEste(object sender, RoutedEventArgs e)
    {
        if (FilaDe(sender) is not { } fila) return;
        EnviarRepetidoAPapelera(fila, fila.RutaActual, esOtro: false);
    }

    /// <summary>«Enviar el otro a la Papelera»: manda el fichero rival y deja esta fila como la copia buena.</summary>
    private void OnBorrarOtro(object sender, RoutedEventArgs e)
    {
        if (FilaDe(sender) is not { } fila) return;
        if (string.IsNullOrEmpty(fila.Res.RutaPareja)) return;
        EnviarRepetidoAPapelera(fila, fila.Res.RutaPareja!, esOtro: true);
    }

    private void OnAbrirCarpetaEste(object sender, RoutedEventArgs e)
    {
        if (FilaDe(sender) is { } fila) AbrirEnCarpeta(fila.RutaActual);
    }

    private void OnAbrirCarpetaOtro(object sender, RoutedEventArgs e)
    {
        if (FilaDe(sender) is { } fila && !string.IsNullOrEmpty(fila.Res.RutaPareja))
            AbrirEnCarpeta(fila.Res.RutaPareja!);
    }

    private static void AbrirEnCarpeta(string ruta)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "explorer.exe", $"/select,\"{ruta}\"") { UseShellExecute = true });
        }
        catch { /* si el fichero ya no está, no pasa nada: el usuario ve la carpeta o un aviso del SO */ }
    }

    /// <summary>
    /// Fichero repetido (#128): envía a la Papelera de la app uno de los dos ficheros implicados
    /// —el de la fila (<paramref name="esOtro"/>=false) o su pareja (=true)— y deja la lista
    /// coherente. Nunca borra nada sin confirmar; Ctrl+Z lo restaura.
    /// </summary>
    private void EnviarRepetidoAPapelera(OrganizarRow fila, string ruta, bool esOtro)
    {
        var nombre = System.IO.Path.GetFileName(ruta);
        var quien = esOtro
            ? Textos.Instancia.OrganizarPapeleraOtro
            : Textos.Instancia.OrganizarPapeleraEste;
        if (!DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarPapeleraTitulo,
                string.Format(Textos.Instancia.OrganizarPapeleraPregunta, quien, nombre),
                Textos.Instancia.OrganizarPapeleraTitulo, Textos.Instancia.Cancelar))
            return;

        if (PapeleraApp.Enviar(ruta) == null)
        {
            DialogWindow.Aviso(Window.GetWindow(this), Textos.Instancia.OrganizarPapeleraFalloTitulo,
                Textos.Instancia.OrganizarPapeleraFallo);
            return;
        }

        if (esOtro)
        {
            // Se fue el «ganador»: quita su fila si estaba en la lista y esta pasa a ser la copia buena.
            var otra = _filas.FirstOrDefault(f =>
                string.Equals(f.RutaActual, ruta, StringComparison.OrdinalIgnoreCase));
            if (otra != null) _filas.Remove(otra);
            fila.MarcarRecuperadaDeDuplicado();
            Escribir(string.Format(Textos.Instancia.OrganizarLogOtroALaPapelera, nombre, fila.Original));
        }
        else
        {
            _filas.Remove(fila);
            Escribir(string.Format(Textos.Instancia.OrganizarLogRepetidaALaPapelera, nombre));
        }
        ActualizarContadores();
    }

    /// <summary>Ctrl+Z: restaura el último fichero enviado a la papelera de la app a su sitio.</summary>
    private void DeshacerBorrado()
    {
        if (!PapeleraApp.PuedeDeshacer) return;
        var nombre = PapeleraApp.DeshacerUltimo();
        Escribir(nombre != null
            ? string.Format(Textos.Instancia.OrganizarLogRestaurado, nombre)
            : Textos.Instancia.OrganizarLogNoSeRestauro);
    }

    /// <summary>
    /// Apunta que este fichero trae TAMBIÉN una historia de otro episodio. Es el caso raro —un
    /// fichero que no encaja en ningún episodio—, así que el nombre lo dice con el código
    /// compuesto («E1b+2b») en vez de disimularlo detrás de uno de los dos.
    /// </summary>
    private void AnadirHistoriaDeOtroEpisodio()
    {
        if (tabla.SelectedItem is not OrganizarRow fila || _catalogoCargado == null) return;
        if (fila.Res.Episodio == null)
        {
            Aviso(Textos.Instancia.OrganizarElegirEpisodioPrincipal);
            return;
        }

        var win = new CatalogoWindow(_catalogoCargado, fila.Res.Archivo.TituloNombre, modoElegir: true)
        { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true || win.Elegido is not { } ep) return;

        fila.AnadirHistoria(ep, win.SegElegido);
        ActualizarContadores();
        Escribir(string.Format(Textos.Instancia.OrganizarLogHistoriaAnadida,
                               fila.Original, $"{ep.Num}{win.SegElegido}"));
    }

    /// <summary>
    /// «Este fichero ya está bien; no lo toques.» Y que no haya que repetirlo: la decisión se
    /// apunta en el CATÁLOGO, así que al reanalizar la fila sale en verde en vez de volver a
    /// pedir que la despaches.
    ///
    /// El caso real son los capítulos especiales que SÍ son de la serie pero no aparecen en
    /// ningún anexo, así que no están en la lista de episodios y sin esto son conflicto eterno.
    /// Va al JSON del catálogo —no a los ajustes de la app— para que la decisión viaje con él.
    /// </summary>
    private void OnDejarComoEsta(object sender, RoutedEventArgs e)
    {
        if (tabla.SelectedItem is not OrganizarRow fila) return;
        fila.Res.Estado = ReindexEstado.Limpio;
        fila.Res.Confianza = ReindexConfianza.Alta;
        fila.Res.Episodio = null;
        fila.Res.Motivo = Textos.Instancia.OrganizarMotivoDejadoComoEstaba;
        fila.Res.Alternativas = Array.Empty<ReindexCandidato>();
        fila.Recalcular();
        ActualizarContadores();

        // Sin catálogo elegido no hay dónde apuntarlo: la fila queda en verde para esta sesión y
        // ya está. No es un error que merezca un aviso.
        if (_catalogoElegido == null) return;
        var nombre = Path.GetFileName(fila.RutaActual);
        try
        {
            if (ReindexCatalog.AnadirADejarComoEsta(_catalogoElegido.Ruta, nombre))
            {
                // El catálogo en memoria es otro objeto: se recarga para que valga ya.
                CargarCatalogoElegido();
                Escribir(string.Format(Textos.Instancia.OrganizarLogQuedaComoEsta, nombre));
            }
        }
        catch (Exception ex)
        {
            Escribir(string.Format(Textos.Instancia.OrganizarLogQuedaComoEstaSinApuntar,
                                   nombre, ex.Message));
        }
    }

    private void RecordarDecision(OrganizarRow fila, CatalogEpisode ep, string? seg = null)
    {
        _decisiones[fila.Res.Archivo.Fingerprint] = new ReindexOverride
        {
            Num = ep.Num,
            Seg = seg,
            Temporada = ep.Temporada,
            Serie = _catalogoCargado?.Serie ?? "",
            Origen = "usuario",
            FechaDecision = DateTime.Now.ToString("yyyy-MM-dd"),
            NombreOriginal = fila.Original,
        };
        try { ReindexStore.GuardarDecisiones(_decisiones); }
        catch (Exception ex)
        { Escribir(string.Format(Textos.Instancia.OrganizarLogNoSeGuardoDecision, ex.Message)); }
    }

    /// <summary>Triaje por teclado: Enter abre el resolutor, 1/2 eligen candidato.</summary>
    private void OnTablaKeyDown(object sender, KeyEventArgs e)
    {
        if (tabla.SelectedItem is not OrganizarRow fila) return;

        if (e.Key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2)
        {
            int idx = e.Key is Key.D1 or Key.NumPad1 ? 0 : 1;
            if (fila.Res.Alternativas.Count > idx)
            {
                var ep = fila.Res.Alternativas[idx].Episodio;
                fila.ElegirEpisodio(ep);
                RecordarDecision(fila, ep);
                ActualizarContadores();
                e.Handled = true;
            }
        }
    }

    // ─────────────────────────── aplicar ───────────────────────────

    // OJO en ambos: la casilla de la cabecera nace con IsChecked="True", así que su Checked
    // dispara DURANTE InitializeComponent, cuando el resto de controles aún no existe.
    // Sin la guarda, la página revienta al construirse — se aprendió a las malas.
    private void OnMarcarFila(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        ActualizarContadores();
    }

    /// <summary>La casilla de la cabecera marca o desmarca todos los listos de golpe.</summary>
    private void OnMarcarTodos(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        if (sender is not CheckBox chk) return;
        bool valor = chk.IsChecked == true;
        foreach (var f in _filas.Where(f => f.ListoParaAplicar)) f.Marcado = valor;
        ActualizarContadores();
    }

    private void PedirConfirmacion()
    {
        var listos = _filas.Where(f => f.ListoParaAplicar && f.Marcado).ToList();
        if (listos.Count == 0) return;

        int dudas = _filas.Count(f => f.EsDuda);
        int desmarcados = _filas.Count(f => f.ListoParaAplicar && !f.Marcado);

        lblConfSeRenombra.Text = listos.Count == 1
            ? Textos.Instancia.OrganizarConfirmarUno
            : string.Format(Textos.Instancia.OrganizarConfirmarVarios, listos.Count);

        if (dudas > 0 || desmarcados > 0)
        {
            // Contar tambien lo desmarcado a proposito: el miedo de «aplicar» es no saber
            // qué toca, y este cuadro existe para que no quede nada sin contar.
            var trozos = new List<string>();
            if (dudas > 0)
                trozos.Add(dudas == 1
                    ? Textos.Instancia.OrganizarConfirmarUnaDuda
                    : string.Format(Textos.Instancia.OrganizarConfirmarDudas, dudas));
            if (desmarcados > 0)
                trozos.Add(desmarcados == 1
                    ? Textos.Instancia.OrganizarConfirmarUnDesmarcado
                    : string.Format(Textos.Instancia.OrganizarConfirmarDesmarcados, desmarcados));
            lblConfNoSeToca.Text = string.Format(Textos.Instancia.OrganizarConfirmarNoSeToca,
                string.Join(Textos.Instancia.OrganizarConfirmarY, trozos));
            filaNoSeToca.Visibility = Visibility.Visible;
        }
        else filaNoSeToca.Visibility = Visibility.Collapsed;

        overlayConfirmar.Visibility = Visibility.Visible;
    }

    private async void Aplicar()
    {
        var listos = _filas.Where(f => f.ListoParaAplicar && f.Marcado).ToList();
        if (listos.Count == 0) return;

        var ahora = DateTime.Now;
        var lote = new LoteJournal
        {
            Id = ahora.ToString("yyyyMMdd-HHmmss"),
            Fecha = ahora.ToString("yyyy-MM-dd"),
            Hora = ahora.ToString("HH:mm"),
            Serie = _catalogoCargado?.Serie ?? "",
            Carpeta = txtCarpeta.Text?.Trim() ?? "",
        };

        // Se resuelven los destinos ANTES de mover nada, para detectar colisiones sin
        // haber tocado el disco.
        var ocupados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var planeados = new List<(OrganizarRow fila, string destino)>();
        var companeros = new List<(string de, string a)>();
        foreach (var f in listos)
        {
            var carpeta = Path.GetDirectoryName(f.Res.Archivo.Path)!;
            var destino = Path.Combine(carpeta, f.NombreNuevo!);
            if (string.Equals(destino, f.Res.Archivo.Path, StringComparison.OrdinalIgnoreCase)) continue;
            if (ocupados.Contains(destino) || File.Exists(destino))
            {
                Escribir(string.Format(Textos.Instancia.OrganizarLogSeOmite, f.Original, f.NombreNuevo));
                continue;
            }
            ocupados.Add(destino);
            planeados.Add((f, destino));
            lote.Movimientos.Add(new MovimientoJournal { De = f.Res.Archivo.Path, A = destino });

            // Sus compañeros (.nfo, .srt…) viajan con él: un .nfo con el nombre viejo queda
            // huérfano y el reproductor de biblioteca deja de asociarlo. Van al MISMO diario,
            // así que «Deshacer» también los devuelve.
            try
            {
                var vecinos = Directory.EnumerateFiles(carpeta);
                foreach (var (de, a) in SidecarPlanner.Planear(f.Res.Archivo.Path, destino, vecinos))
                {
                    if (ocupados.Contains(a) || File.Exists(a)) continue;
                    ocupados.Add(a);
                    companeros.Add((de, a));
                    lote.Movimientos.Add(new MovimientoJournal { De = de, A = a });
                }
            }
            catch { /* una carpeta ilegible no impide renombrar el vídeo */ }
        }

        if (planeados.Count == 0) { Aviso(Textos.Instancia.OrganizarNadaQueRenombrar); return; }

        // El diario va a disco ANTES del primer renombrado: si esto se corta a la mitad,
        // el «deshacer» sigue existiendo.
        try { ReindexStore.EscribirJournal(lote); }
        catch (Exception ex)
        {
            Aviso(string.Format(Textos.Instancia.OrganizarNoSeGuardoRegistro, ex.Message));
            return;
        }

        // Los movimientos van FUERA del hilo de interfaz: 462 renombrados en OneDrive
        // tardan, y con la ventana congelada parecía que aplicar no hacía nada.
        btnAplicar.IsEnabled = btnSimular.IsEnabled = false;
        lblEstadoOrg.Text = string.Format(Textos.Instancia.OrganizarRenombrando, planeados.Count);

        var companerosMovidos = 0;
        var resultados = await Task.Run(() =>
        {
            var lista = new List<(OrganizarRow fila, string? error)>();
            foreach (var (fila, destino) in planeados)
            {
                try { File.Move(fila.Res.Archivo.Path, destino); lista.Add((fila, null)); }
                catch (Exception ex) { lista.Add((fila, ex.Message)); }
            }
            foreach (var (de, a) in companeros)
                try { File.Move(de, a); companerosMovidos++; } catch { /* se cuenta abajo */ }
            return lista;
        });

        int hechos = 0, fallos = 0;
        foreach (var (fila, error) in resultados)
        {
            if (error == null)
            {
                fila.Aplicado = true;
                hechos++;
                // La marca se va con el fichero: si no, aplicar borraría de la cola justo
                // lo que estabas arreglando, que es lo contrario de lo que la cola sirve.
                if (fila.Apartada) _revision.Renombrado(fila.Res.Archivo.Path, fila.RutaActual);
            }
            else
            {
                fallos++;
                Escribir(string.Format(Textos.Instancia.OrganizarLogNoSeRenombro, fila.Original, error));
            }
        }
        btnAplicar.IsEnabled = btnSimular.IsEnabled = true;

        if (resultados.Any(r => r.error == null && r.fila.Apartada)) GuardarRevision();

        _ultimoLote = lote;
        RefrescarUltimoLote();
        ActualizarContadores();
        // Y se rehace el filtro: lo aplicado ya cuenta como limpio, así que con el chip de
        // «corregidos» puesto las filas hechas tienen que salir de la vista solas.
        AplicarFiltro();

        var extra = companerosMovidos > 0
            ? string.Format(Textos.Instancia.OrganizarAplicadoCompaneros, companerosMovidos) : "";
        lblBannerAplicado.Text = fallos == 0
            ? string.Format(Textos.Instancia.OrganizarAplicadoOk, hechos, extra)
            : string.Format(Textos.Instancia.OrganizarAplicadoConFallos, hechos, extra, fallos);
        bannerAplicado.Visibility = Visibility.Visible;
        Escribir(lblBannerAplicado.Text);
    }

    private void RefrescarUltimoLote()
    {
        _ultimoLote ??= ReindexStore.UltimoLote();
        btnDeshacer.IsEnabled = _ultimoLote is { Movimientos.Count: > 0 };
        lblDeshacer.Text = _ultimoLote is { Movimientos.Count: > 0 }
            ? _ultimoLote.Etiqueta
            : Textos.Instancia.OrganizarDeshacerLote;
    }

    private void DeshacerUltimoLote()
    {
        if (_ultimoLote == null) return;

        var (devueltos, fallidos) = ReindexStore.Deshacer(_ultimoLote);
        Escribir(fallidos == 0
            ? string.Format(Textos.Instancia.OrganizarLogLoteDeshecho, devueltos)
            : string.Format(Textos.Instancia.OrganizarLogLoteDeshechoConFallos, devueltos, fallidos));

        var lote = _ultimoLote;

        // La marca de revisión sigue al fichero también hacia atrás: al deshacer, el fichero
        // recupera su nombre anterior y el apartado apuntaría a uno que ya no existe. Se
        // comprueba en disco cuál de los dos nombres está, porque deshacer puede fallar a
        // medias y entonces cada fichero está en un sitio distinto.
        bool tocada = false;
        foreach (var m in lote.Movimientos)
            if (_revision.Tiene(m.A) && File.Exists(m.De))
            { _revision.Renombrado(m.A, m.De); tocada = true; }
        if (tocada) GuardarRevision();

        ReindexStore.OlvidarLote(_ultimoLote);
        _ultimoLote = null;
        bannerAplicado.Visibility = Visibility.Collapsed;
        RefrescarUltimoLote();

        // Deshacer NO te saca del contexto. Si la tabla está a la vista, las filas del lote
        // vuelven de «Hecho» a su estado anterior EN EL SITIO — con su casilla y su
        // propuesta intactas, listas para re-aplicar si era eso lo que se quería.
        if (vistaRevision.Visibility == Visibility.Visible && _filas.Count > 0)
        {
            var deshechos = new HashSet<string>(
                lote.Movimientos.Select(m => m.De), StringComparer.OrdinalIgnoreCase);
            foreach (var f in _filas)
                if (f.Aplicado && deshechos.Contains(f.Res.Archivo.Path))
                    f.Aplicado = false;
            ActualizarContadores();

            // La lista de disco vuelve a los nombres de antes; se refresca sin tocar la vista
            try { _ficheros = LibraryScan.Escanear(txtCarpeta.Text?.Trim() ?? "", Engine.VideoExtensions); }
            catch { /* si la carpeta no se puede releer, la próxima simulación lo dirá */ }
        }
        else RevisarCarpeta();   // desde la pantalla de inicio, solo refrescar el recuento
    }

    /// <summary>
    /// Reproductor INTEGRADO en modo focus, no una ventana del sistema: la pregunta es
    /// «¿qué capítulo es?» y la respuesta debe estar a un Esc de distancia.
    /// </summary>
    private void ReproducirFila(OrganizarRow? fila)
    {
        if (fila == null || !File.Exists(fila.RutaActual)) return;
        new ReproductorWindow(fila.RutaActual) { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    /// <summary>
    /// Abre el explorador con el fichero ya seleccionado. No lo abre: seleccionarlo no
    /// descarga nada, así que sirve igual para los que están solo en la nube.
    /// </summary>
    private void AbrirUbicacion(OrganizarRow? fila)
    {
        if (fila != null) AbrirCarpetaDe(fila.RutaActual);
    }

    private void AbrirCarpetaDe(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "explorer.exe", $"/select,\"{ruta}\"") { UseShellExecute = true });
            else
            {
                // Si el fichero ya no está (renombrado fuera, movido), al menos la carpeta
                var carpeta = Path.GetDirectoryName(ruta);
                if (Directory.Exists(carpeta))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(carpeta)
                        { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        { Escribir(string.Format(Textos.Instancia.OrganizarLogNoSeAbrioUbicacion, ex.Message)); }
    }

    /// <summary>
    /// «Partirlo en dos»: lo lleva a Recortes con un corte ya puesto por la mitad. No se
    /// sabe dónde acaba la primera historia —eso lo decide quien mira—, pero llegar con la
    /// junta puesta y arrastrarla es otra cosa que llegar a una pista en blanco.
    /// </summary>
    private void OnPartirEnDos(object remitente, RoutedEventArgs e)
    {
        if (tabla.SelectedItem is not OrganizarRow fila || !File.Exists(fila.RutaActual)) return;
        AbrirEnRecortes?.Invoke(fila.RutaActual, true);
    }

    private void AbrirMemoria()
    {
        if (_decisiones.Count == 0) { Aviso(Textos.Instancia.OrganizarSinDecisiones); return; }

        var r = DialogWindow.Confirmar(Window.GetWindow(this), Textos.Instancia.OrganizarMemoriaTitulo,
            string.Format(Textos.Instancia.OrganizarMemoriaPregunta, _decisiones.Count));
        if (!r) return;

        _decisiones.Clear();
        ReindexStore.OlvidarDecisiones();
        Escribir(Textos.Instancia.OrganizarLogMemoriaVaciada);
    }

    // ─────────────────────────── varios ───────────────────────────

    private void ActualizarEstado()
    {
        bool puede = _catalogoCargado != null && _ficheros.Length > 0;
        btnSimular.IsEnabled = btnSimularGrande.IsEnabled = puede;

        if (_filas.Count > 0) { ActualizarContadores(); return; }

        lblEstadoOrg.Text = (_catalogoCargado, _ficheros.Length) switch
        {
            (null, _) => Textos.Instancia.OrganizarImportaCatalogo,
            (_, 0) => Textos.Instancia.OrganizarElegirCarpetaVideos,
            var (c, n) => string.Format(Textos.Instancia.OrganizarCatalogoListo, c!.Serie, n),
        };
    }

    private void Escribir(string linea) => Log?.Invoke(linea);

    /// <summary>
    /// Dudosos que no se llegaron a sondear porque el vídeo está solo en la nube. Sale en
    /// el resumen, no en el registro: el panel del registro va plegado, así que contarlo
    /// solo ahí es no contarlo.
    /// </summary>
    private int _dudososEnNube;

    private void Aviso(string mensaje)
    {
        Escribir(mensaje);
        DialogWindow.Aviso(Window.GetWindow(this), Textos.Instancia.OrganizarTitulo, mensaje);
    }
}
