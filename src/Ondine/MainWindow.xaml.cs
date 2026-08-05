using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ondine.Localizacion;

namespace Ondine;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<VideoRow> _rows = new();

    private readonly Engine _engine = new();
    private readonly string _thumbDir = Path.Combine(Path.GetTempPath(), "shrinkvideo_thumbs");
    private readonly string _previewDir = Path.Combine(Path.GetTempPath(), "shrinkvideo_preview");
    private readonly System.Windows.Threading.DispatcherTimer _scrubTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };
    private CancellationTokenSource? _scrubCts;
    private CancellationTokenSource? _previewCts;
    private object? _previewBtnContent;
    private CancellationTokenSource? _cts;
    private bool _running;
    private Pagina _paginaActual = Pagina.Comprimir;   // la pestaña que se está viendo
    // Filas enviadas a la Papelera propia (Comprimir), en orden, para deshacer con Ctrl+Z: al
    // restaurar el fichero también vuelve su fila a la lista, sin re-analizarlo.
    private readonly Stack<VideoRow> _pilaPapelera = new();
    private bool _paused;
    private bool _applyingPreset;
    private Settings _settings = new();

    // banda de selección (rubber-band). El ancla se guarda en coordenadas de CONTENIDO (viewport
    // + desplazamiento del scroll), no de viewport: así, al auto-desplazar durante el arrastre, la
    // banda sigue anclada a la misma fila y selecciona también lo que quedaba fuera de la vista.
    private Point _marqueeStart;         // coordenadas de CONTENIDO
    private Point _marqueeUltimoV;       // última posición del ratón en coordenadas de viewport
    private bool _marqueeActive;
    private bool _marqueeDragging;
    private readonly HashSet<object> _marqueeBase = new();
    private ScrollViewer? _lstScroll;
    private const double MarqueeBorde = 22;   // franja del borde que dispara el auto-scroll
    private const double MarqueePaso = 26;     // píxeles por tic de auto-scroll
    private readonly System.Windows.Threading.DispatcherTimer _marqueeAutoScroll =
        new() { Interval = TimeSpan.FromMilliseconds(30) };

    // orden por cabecera de la tabla de «Comprimir»
    private GridViewColumnHeader? _lstSortHeader;
    private ListSortDirection _lstSortDir = ListSortDirection.Ascending;

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(_thumbDir);
        // La papelera de la app manda los borrados VIEJOS a la Papelera del sistema; los recientes
        // se pueden deshacer al instante. Aquí se le da la vía a la Papelera real y sus triggers de
        // finalizado: al cerrar (todo) y cada pocos minutos (los que hayan envejecido).
        Reindex.PapeleraApp.EnviarASistema = RecycleBin.Send;
        Closed += (_, _) => Reindex.PapeleraApp.VaciarAlCerrar();
        var relojPapelera = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        relojPapelera.Tick += (_, _) => Reindex.PapeleraApp.FinalizarViejos();
        relojPapelera.Start();
        lst.ItemsSource = _rows;
        // Clic en una cabecera de la tabla = ordenar por esa columna (asc/desc alterno).
        lst.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnCabeceraTablaClick));
#if DEV_BUILD
        // Compilación local/dev: se marca «dev» + hora del build para no confundirla con la
        // release de GitHub (misma versión) y saber si es la última que se acaba de compilar.
        string marcaDev = " · dev";
        try { marcaDev += " " + System.IO.File.GetLastWriteTime(Environment.ProcessPath!).ToString("dd/MM HH:mm"); }
        catch { /* sin ruta de proceso: basta con «dev» */ }
        lblVersion.Text = "v" + Updater.Current + marcaDev;
        lblVersion.ToolTip = Textos.Instancia.MainVersionDevTip;
#else
        lblVersion.Text = "v" + Updater.Current;
#endif
        _previewBtnContent = btnPreview.Content;   // para restaurar tras "Cancelar"

        // Valores de partida que llevan dentro un dato (el segundo del timeline) o un código
        // que no se traduce (el idioma de la pista): se escriben aquí y no en el XAML.
        TextosDeArranque();

        // Lo escrito a mano NO es un enlace, así que cambiar de idioma no lo
        // toca. Los textos que se reescriben al hacer algo se arreglan solos la
        // próxima vez; estos no, porque solo se ponen al abrir, y se quedaban en
        // el idioma viejo para siempre.
        Idioma.Cambio += (_, _) => Dispatcher.BeginInvoke(TextosDeArranque);

        _settings = SettingsStore.Load();
        ApplySettings();

        // el estado vacío solo se ve cuando no hay nada en la lista
        _rows.CollectionChanged += (_, _) =>
            emptyState.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // barra de título propia
        btnMin.Click += (_, _) => WindowState = WindowState.Minimized;
        btnMax.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        btnClose.Click += (_, _) => Close();
        StateChanged += (_, _) => rootBorder.Padding = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);

        btnSrc.Click += (_, _) => PickFolder(txtSrc);
        btnOut.Click += (_, _) => PickFolder(txtOut);
        btnSrcFile.Click += async (_, _) => await AddFilesDialogAsync();
        btnOpen.Click += (_, _) => OpenDestination();
        btnScan.Click += async (_, _) => await ScanAsync();
        btnRun.Click += async (_, _) => await RunAsync();
        btnCancel.Click += (_, _) => _cts?.Cancel();
        btnPause.Click += (_, _) => TogglePause();
        btnMarkAll.Click += (_, _) => lst.SelectAll();
        btnMarkNone.Click += (_, _) => lst.UnselectAll();
        btnDelSel.Click += (_, _) => DeleteSelected();
        btnDelDir.Click += (_, _) => DeleteFolders();
        btnCheckUpdate.Click += async (_, _) => await CheckUpdateAsync(manual: true);
        btnUpdateLater.Click += (_, _) => updateBar.Visibility = Visibility.Collapsed;

        // barra de menú
        miPickSrc.Click += (_, _) => PickFolder(txtSrc);
        miAddFiles.Click += async (_, _) => await AddFilesDialogAsync();
        miOpenDest.Click += (_, _) => OpenDestination();
        miExit.Click += (_, _) => Close();
        miSelAll.Click += (_, _) => lst.SelectAll();
        miSelNone.Click += (_, _) => lst.UnselectAll();
        miSelInvert.Click += (_, _) => InvertSelection();
        miDelSel.Click += (_, _) => DeleteSelected();
        miRename.Click += (_, _) => OpenRenameDialog();
        btnRename.Click += (_, _) => OpenRenameDialog();
        // El catálogo y lo resuelto se le pasan a la ventana en vez de que ella
        // los busque: así la pantalla de complementos no sabe nada de Organizar,
        // solo recibe dos datos, y se puede abrir desde otro sitio mañana.
        miComplementos.Click += (_, _) => AbrirComplementos(null);

        miPrefs.Click += (_, _) => OpenPreferences();
        miCheckUpd.Click += async (_, _) => await CheckUpdateAsync(manual: true);
        miAbout.Click += (_, _) => ShowAbout();
        miTutoriales.Click += (_, _) => new AyudaWindow { Owner = this }.Show();

        // «Subcarpetas» es el mismo ajuste que el de Preferencias: se mantienen en sync
        chkRec.Checked += (_, _) => PersistRecurse();
        chkRec.Unchecked += (_, _) => PersistRecurse();

        // arrastrar vídeos (o carpetas) desde el Explorador y soltarlos en la ventana
        AllowDrop = true;
        DragOver += OnDragOver;
        Drop += OnDropFiles;

        // quitar de la lista con Supr + menú contextual de la tabla
        lst.PreviewKeyDown += Lst_KeyDown;
        lst.PreviewMouseRightButtonDown += Lst_RightButtonDown;
        ctxTable.Opened += (_, _) => UpdateContextMenu();
        miCtxRemove.Click += (_, _) => RemoveSelectedRows();
        miCtxRecycle.Click += (_, _) => DeleteSelected();
        miCtxOpenFolder.Click += (_, _) => OpenContainingFolder();
        miCtxCopyPath.Click += (_, _) => CopySelectedPaths();
        miCtxPistas.Click += async (_, _) => await QuitarPistasDeSeleccionado();
        miCtxSelectAll.Click += (_, _) => lst.SelectAll();
        miCtxInvert.Click += (_, _) => InvertSelection();

        // banda de selección estilo explorador
        lst.PreviewMouseLeftButtonDown += Lst_MouseDown;
        lst.PreviewMouseMove += Lst_MouseMove;
        lst.PreviewMouseLeftButtonUp += Lst_MouseUp;
        _marqueeAutoScroll.Tick += (_, _) => MarqueeAutoScrollPaso();

        // El panel lateral se pliega solo cuando la ventana se queda estrecha
        SizeChanged += (_, _) => AjustarAAncho();

        // conmutador de páginas «Comprimir | Organizar»
        tabComprimir.Checked += (_, _) => CambiarPagina(Pagina.Comprimir);
        tabOrganizar.Checked += (_, _) => CambiarPagina(Pagina.Organizar);
        tabRecortes.Checked += (_, _) => CambiarPagina(Pagina.Recortes);
        pageOrganizar.Log += AppendLog;
        pageRecortes.Log += AppendLog;
        // Recortes reporta su export al indicador global: se ve desde cualquier pestaña.
        pageRecortes.EstadoProceso += (activo, etiqueta) =>
        {
            _recortesEtiqueta = activo ? etiqueta : null;
            ActualizarIndicadorGlobal();
        };
        pageOrganizar.AbrirEnRecortes += (ruta, partirPorLaMitad) =>
        {
            tabRecortes.IsChecked = true;
            pageRecortes.Cargar(ruta, partirPorLaMitad);
        };
        // La píldora lleva a la pestaña de la tarea que muestra, no siempre a «Comprimir».
        pillFondo.MouseLeftButtonUp += (_, _) => IrAPestaña(_pillDestino);

        tabDetalle.MouseLeftButtonUp += (_, _) => ShowSideTab("detalle");
        tabEstim.MouseLeftButtonUp += (_, _) => ShowSideTab("estim");

        foreach (var c in new[] { cboFmt, cboCodec, cboQ, cboRes, cboAud })
            c.SelectionChanged += (_, _) => UpdateEstimate();

        btnPreview.Click += async (_, _) => await OnPreviewAsync();
        btnMeasure.Click += async (_, _) => await OnMeasureAsync();
        _scrubTimer.Tick += async (_, _) => { _scrubTimer.Stop(); await ShowScrubFrameAsync(); };
        sldPreview.ValueChanged += (_, _) =>
        {
            lblPreviewAt.Text = string.Format(Textos.Instancia.MainPrevisualizarDesde, FmtTime((int)sldPreview.Value));
            _scrubTimer.Stop(); _scrubTimer.Start();   // debounce: muestra el fotograma al soltar/pausar
        };

        cboFmt.SelectionChanged += (_, _) =>
        {
            bool audioOnly = cboFmt.SelectedIndex >= 3;   // MP3/M4A/FLAC/Opus
            cboCodec.IsEnabled = cboQ.IsEnabled = cboRes.IsEnabled = !audioOnly;
        };
        btnSavePreset.Click += (_, _) => SavePreset();
        cboPreset.SelectionChanged += (_, _) => ApplyPreset();
        ReloadPresets(_settings.DefaultPreset);   // aplica el preset por defecto si lo hay
        if (string.IsNullOrEmpty(_settings.DefaultPreset)) cboLang.Text = _settings.DefaultLang;

        Loaded += async (_, _) =>
        {
            if (cboPreset.SelectedItem == null) cboLang.Text = _settings.DefaultLang;
            if (!await Engine.ToolsAvailableAsync())
                DialogWindow.Aviso(this, Textos.Instancia.MainFaltaFfmpegTitulo, Textos.Instancia.MainFaltaFfmpegTexto);
            if (_settings.CheckUpdatesOnStart) await CheckUpdateAsync(manual: false);
        };
    }

    // ---------- preferencias / ajustes ----------
    private void ApplySettings()
    {
        Engine.MinFreeBytes = _settings.MinFreeMb * 1024L * 1024;
        Engine.AllowHardware = _settings.UseHardware;
        Estimator.ComplexityFactor = Math.Clamp(_settings.ComplexityFactor, 0.15, 4.0);
        chkRec.IsChecked = _settings.Recurse;
        UpdateRenameStatus();
    }

    /// <summary>La casilla «Subcarpetas» y la preferencia son lo mismo: al cambiarla, se guarda.</summary>
    private void PersistRecurse()
    {
        bool v = chkRec.IsChecked == true;
        if (v == _settings.Recurse) return;   // evita reescribir al aplicar los ajustes
        _settings.Recurse = v;
        SettingsStore.Save(_settings);
    }

    private void OpenPreferences()
    {
        var names = (cboPreset.ItemsSource as IEnumerable<Preset>)?.Select(p => p.Name) ?? Enumerable.Empty<string>();
        var dlg = new PreferencesWindow(_settings, names) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            _settings = dlg.Result;
            SettingsStore.Save(_settings);
            ApplySettings();
            lblProg.Text = Textos.Instancia.MainPreferenciasGuardadas;
        }
    }

    // ---------- renombrado de la salida (estilo PowerRename) ----------
    private void OpenRenameDialog()
    {
        string ext = Engine.OutputExtension(BuildOptions());
        var rows = SelectedRows();
        if (rows.Count == 0) rows = _rows.ToList();   // sin selección: previsualiza con toda la lista

        var items = rows
            .Select(r => (name: Path.GetFileNameWithoutExtension(r.Name) + ext, created: SafeCreated(r.Path)))
            .ToList();

        var dlg = new RenameWindow(_settings.Rename, items,
                                   _settings.RenameSearchHistory, _settings.RenameReplaceHistory) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            _settings.Rename = dlg.Result;   // los historiales se mutan dentro del diálogo
            SettingsStore.Save(_settings);
            UpdateRenameStatus();
            lblProg.Text = _settings.Rename.HasEffect
                ? Textos.Instancia.MainRenombradoActivo
                : Textos.Instancia.MainRenombradoDesactivado;
        }
    }

    private static DateTime SafeCreated(string path)
    {
        try { return File.GetCreationTime(path); } catch { return DateTime.Now; }
    }

    /// <summary>Refleja en el botón si hay una regla de renombrado activa (para que nunca sea silenciosa).</summary>
    private void UpdateRenameStatus()
    {
        bool on = _settings.Rename.HasEffect;
        lblRename.Text = on ? Textos.Instancia.MainRenombrarSalidaActiva : Textos.Instancia.MainRenombrarSalida;
        lblRename.Foreground = on ? (Brush)FindResource("Accent300") : (Brush)FindResource("Text");
    }

    /// <summary>
    /// Los textos que se escriben a mano y NO los vuelve a tocar nadie.
    ///
    /// <para>
    /// El resto de lo imperativo se reescribe al analizar, al seleccionar o al
    /// mover el deslizador, así que un cambio de idioma se le nota a la
    /// siguiente. Estos dos solo se ponen al abrir la ventana: si no se
    /// rehacen aquí, se quedan en el idioma con el que arrancó la app.
    /// </para>
    /// </summary>
    private void TextosDeArranque()
    {
        cboLang.Text = Textos.Instancia.MainIdiomaPistaPorDefecto;
        lblPreviewAt.Text = string.Format(
            Textos.Instancia.MainPrevisualizarDesde, FmtTime((int)sldPreview.Value));
    }

    private void ShowAbout() => DialogWindow.Aviso(this, Textos.Instancia.MainMenuAcercaDe,
        string.Format(Textos.Instancia.MainAcercaDeTexto, Updater.Current));

    /// <summary>Modal antes de comprimir: ¿enviar cada original a la Papelera al terminar? Devuelve (proceder, borrar, recordar).</summary>
    private (bool proceed, bool delete, bool remember) AskAfterCompress(int count)
    {
        var win = new Window
        {
            Title = Textos.Instancia.MainPaginaComprimir, Width = 470, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None, Background = Brushes.Transparent, AllowsTransparency = true,
        };
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock
        {
            Text = string.Format(Textos.Instancia.MainAvisoComprimirCuantos, count),
            FontSize = 14, FontWeight = FontWeights.Medium,
            Foreground = (Brush)FindResource("Text"), Margin = new Thickness(0, 0, 0, 14),
        });
        var chkDel = new CheckBox
        {
            Content = Textos.Instancia.MainAvisoComprimirPapelera,
            Foreground = (Brush)FindResource("Neutral300"), Margin = new Thickness(0, 0, 0, 9),
        };
        var chkRemember = new CheckBox
        {
            Content = Textos.Instancia.MainAvisoComprimirNoPreguntar,
            Foreground = (Brush)FindResource("Neutral500"),
        };
        panel.Children.Add(chkDel);
        panel.Children.Add(chkRemember);
        panel.Children.Add(new TextBlock
        {
            Text = Textos.Instancia.MainAvisoComprimirNota,
            FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("Neutral600"),
            Margin = new Thickness(0, 12, 0, 16),
        });
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = Textos.Instancia.Cancelar, Width = 100, Style = (Style)FindResource("BtnSecondary"), Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
        var okb = new Button { Content = Textos.Instancia.MainPaginaComprimir, Width = 110, Style = (Style)FindResource("BtnPrimary"), IsDefault = true };
        row.Children.Add(cancel);
        row.Children.Add(okb);
        panel.Children.Add(row);
        win.Content = new Border
        {
            Background = (Brush)FindResource("Bg"), BorderBrush = (Brush)FindResource("Divider"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = panel,
        };
        bool proceed = false;
        okb.Click += (_, _) => { proceed = true; win.DialogResult = true; };
        win.ShowDialog();
        return (proceed, chkDel.IsChecked == true, chkRemember.IsChecked == true);
    }

    // ---------- selección de rutas ----------
    private void PickFolder(TextBox target)
    {
        var d = new OpenFolderDialog { Title = Textos.Instancia.MainElegirCarpetaTitulo };
        if (d.ShowDialog(this) == true) target.Text = d.FolderName;
    }
    private async Task AddFilesDialogAsync()
    {
        var d = new OpenFileDialog
        {
            Multiselect = true,
            Title = Textos.Instancia.MainElegirVideosTitulo,
            Filter = Textos.Instancia.MainFiltroVideos
        };
        if (d.ShowDialog(this) == true) await AddFilesAsync(d.FileNames);
    }

    /// <summary>Añade archivos sueltos a la lista (sin reemplazarla) y analiza los nuevos.</summary>
    private async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var nuevos = new List<VideoRow>();
        foreach (var f in paths.Where(File.Exists))
        {
            if (_rows.Any(r => string.Equals(r.Path, f, StringComparison.OrdinalIgnoreCase))) continue;
            var fi = new FileInfo(f);
            var row = new VideoRow
            {
                Name = fi.Name,
                Dir = fi.Directory?.Name ?? "",
                Path = fi.FullName,
                Bytes = fi.Length,
                SizeMB = $"{fi.Length / 1048576.0:n0} MB",
            };
            _rows.Add(row);
            nuevos.Add(row);
        }
        if (nuevos.Count == 0) return;
        foreach (var row in nuevos) lst.SelectedItems.Add(row);   // los recién añadidos entran seleccionados
        lblLangHint.Text = Textos.Instancia.MainDetectando;
        await ProbeRowsAsync(nuevos);
    }
    // ---------- arrastrar y soltar desde el Explorador ----------
    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDropFiles(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
        var vids = ShellIntegration.ExpandVideos(paths, chkRec.IsChecked == true);
        if (vids.Count == 0) { lblProg.Text = Textos.Instancia.MainSinVideosQueAnadir; return; }
        AddFilesFromShell(vids);
    }

    /// <summary>
    /// Añade a la tabla los vídeos que llegan desde el Explorador (menú contextual,
    /// «Enviar a», arrastrar y soltar), y deja la ventana lista para trabajar.
    /// </summary>
    public async void AddFilesFromShell(IReadOnlyList<string> paths)
    {
        var nuevos = paths.Where(File.Exists).ToList();
        if (nuevos.Count == 0) return;

        // si aún no hay origen, se toma la carpeta del primer archivo (para «Abrir destino»)
        if (string.IsNullOrWhiteSpace(txtSrc.Text))
            txtSrc.Text = Path.GetDirectoryName(nuevos[0]) ?? "";

        int antes = _rows.Count;
        lblProg.Text = string.Format(Textos.Instancia.MainAnadiendoDesdeExplorador, nuevos.Count);
        await AddFilesAsync(nuevos);

        int añadidos = _rows.Count - antes;
        lblProg.Text = añadidos == 0
            ? Textos.Instancia.MainYaEstabanEnLista
            : string.Format(Textos.Instancia.MainAnadidosDesdeExplorador, añadidos);
    }

    private string EffectiveOutput()
    {
        if (!string.IsNullOrWhiteSpace(txtOut.Text)) return txtOut.Text.Trim();
        var src = txtSrc.Text.Trim();
        if (string.IsNullOrEmpty(src)) return "";
        try
        {
            var baseDir = Directory.Exists(src) ? src : Path.GetDirectoryName(src) ?? "";
            return Path.Combine(baseDir, "comprimido");
        }
        catch { return ""; }
    }
    private void OpenDestination()
    {
        var o = EffectiveOutput();
        if (string.IsNullOrEmpty(o)) return;
        Directory.CreateDirectory(o);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{o}\"") { UseShellExecute = true });
    }

    // ---------- analizar ----------
    private async Task ScanAsync()
    {
        var src = txtSrc.Text.Trim();
        if (string.IsNullOrEmpty(src) || (!Directory.Exists(src) && !File.Exists(src)))
        {
            DialogWindow.Aviso(this, Textos.Instancia.MainOrigen, Textos.Instancia.MainOrigenInvalido);
            return;
        }
        _rows.Clear(); pnlALang.Children.Clear(); pnlSLang.Children.Clear();
        lblLangHint.Text = Textos.Instancia.MainDetectando;

        var outDir = EffectiveOutput();
        List<string> files;
        if (Directory.Exists(src))
        {
            var opt = chkRec.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files = Directory.EnumerateFiles(src, "*.*", opt)
                .Where(f => Engine.VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => !string.Equals(Path.GetDirectoryName(f), outDir, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f).ToList();
        }
        else files = new() { src };

        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            _rows.Add(new VideoRow
            {
                Name = fi.Name,
                Dir = fi.Directory?.Name ?? "",
                Path = fi.FullName,
                Bytes = fi.Length,
                SizeMB = $"{fi.Length / 1048576.0:n0} MB",
            });
        }
        lblProg.Text = string.Format(Textos.Instancia.MainVideosEncontrados, _rows.Count);
        if (_rows.Count == 0) { lblLangHint.Text = Textos.Instancia.MainNadaQueAnalizar; return; }
        lst.SelectAll();   // por defecto se procesan todos; el usuario acota con la selección

        await ProbeRowsAsync(_rows.ToList());
        lblLangHint.Text = pnlSLang.Children.Count == 0 ? Textos.Instancia.MainSinSubtitulosDetectados : "";
    }

    /// <summary>Lee las pistas de cada fila con ffprobe y va poblando los idiomas detectados.</summary>
    private async Task ProbeRowsAsync(IReadOnlyList<VideoRow> rows)
    {
        foreach (var row in rows)
        {
            var info = await _engine.ProbeAsync(row.Path);
            row.Codec = info.Codec;
            row.Dur = $"{info.DurationSec / 3600:D1}:{info.DurationSec % 3600 / 60:D2}:{info.DurationSec % 60:D2}";
            row.Audio = string.Join("+", info.AudioLangs);
            row.Subs = string.Join("+", info.SubLangs);
            // El estado dice algo útil desde el análisis: si ya está bien comprimido, se
            // avisa aquí y no se preselecciona, en vez de descubrirlo al lanzar la tanda.
            int totalKbps = row.DurationSec > 0 ? (int)(row.Bytes * 8.0 / row.DurationSec / 1000.0) : 0;
            row.YaComprimido = Engine.AlreadyCompressed(info.Codec, totalKbps);
            row.Estado = row.YaComprimido
                ? string.Format(Textos.Instancia.MainEstadoYaEn, info.Codec.ToUpperInvariant())
                : Textos.Instancia.Pendiente;
            row.Width = info.Width; row.Height = info.Height; row.Fps = info.Fps;
            row.DurationSec = info.DurationSec;
            row.VideoBitrateKbps = info.VideoBitrateKbps; row.AudioBitrateKbps = info.AudioBitrateKbps;
            row.Channels = info.Channels; row.AudioCodec = info.AudioCodec; row.Probed = true;
            foreach (var l in info.AudioLangs) if (l != "?") EnsureLangChip(pnlALang, l);
            foreach (var l in info.SubLangs) if (l != "?") EnsureLangChip(pnlSLang, l);
        }
        UpdateLangCombo();

        // Preselección inteligente: se marcan solo los que conviene comprimir. Los que ya
        // están en un códec eficiente se dejan fuera, que era justo lo que prometía el
        // texto de la lista vacía y no cumplía nadie.
        int utiles = _rows.Count(r => !r.YaComprimido);
        if (utiles > 0 && utiles < _rows.Count)
        {
            lst.UnselectAll();
            foreach (var r in _rows.Where(r => !r.YaComprimido)) lst.SelectedItems.Add(r);
            lblProg.Text = string.Format(Textos.Instancia.MainResumenPreseleccion,
                                         _rows.Count, utiles, _rows.Count - utiles);
        }
        else lblProg.Text = utiles == 0 && _rows.Count > 0
            ? string.Format(Textos.Instancia.MainResumenTodosComprimidos, _rows.Count)
            : string.Format(Textos.Instancia.MainResumenEnLista, _rows.Count);
    }

    /// <summary>Llena el combo de idioma principal con los idiomas de audio realmente detectados.</summary>
    private void UpdateLangCombo()
    {
        var detected = pnlALang.Children.OfType<CheckBox>().Select(c => (string)c.Content).Distinct().ToList();
        if (detected.Count == 0) return;
        var current = cboLang.Text;
        cboLang.ItemsSource = detected;
        cboLang.Text = detected.Contains(current) ? current : (detected.Contains("spa") ? "spa" : detected[0]);
    }

    private void EnsureLangChip(WrapPanel panel, string code)
    {
        if (panel.Children.OfType<CheckBox>().Any(c => (string)c.Content == code)) return;
        AddLangChip(panel, code);
    }

    private void AddLangChip(WrapPanel panel, string code)
    {
        var cb = new CheckBox { Content = code, IsChecked = true, Style = (Style)FindResource("ChipStyle") };
        cb.Checked += (_, _) => UpdateEstimate();
        cb.Unchecked += (_, _) => UpdateEstimate();
        panel.Children.Add(cb);
    }
    private static List<string> CheckedLangs(WrapPanel panel) =>
        panel.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => (string)c.Content).ToList();

    // ---------- previsualización ----------
    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_marqueeDragging) return;   // durante el arrastre no recalculamos la vista (una vez al soltar)
        await UpdateForSelectionAsync();
    }

    private async Task UpdateForSelectionAsync()
    {
        if (lst.SelectedItem is not VideoRow r) return;
        UpdateEstimate();
        sldPreview.Maximum = Math.Max(0, r.DurationSec - 10);
        if (sldPreview.Value > sldPreview.Maximum) sldPreview.Value = 0;
        lblPreviewAt.Text = string.Format(Textos.Instancia.MainPrevisualizarDesde, FmtTime((int)sldPreview.Value));
        lblPrevName.Text = r.Name;
        lblPrevInfo.Text = string.Format(Textos.Instancia.MainDetalleInfo, r.Dir, r.SizeMB, r.Dur, r.Codec, r.Audio)
                         + (string.IsNullOrEmpty(r.Subs)
                                ? ""
                                : string.Format(Textos.Instancia.MainDetalleSubs, r.Subs));

        await ShowScrubFrameAsync();   // muestra el fotograma del punto de previsualización
    }

    // ---------- selección estilo explorador (rubber-band) ----------
    private ScrollViewer? LstScroll => _lstScroll ??= FindDescendant<ScrollViewer>(lst);

    /// <summary>Punto del ratón en coordenadas de CONTENIDO (viewport + desplazamiento del scroll).</summary>
    private Point PuntoContenido(Point viewport)
    {
        var sv = LstScroll;
        return sv == null ? viewport
            : new Point(viewport.X + sv.HorizontalOffset, viewport.Y + sv.VerticalOffset);
    }

    private void Lst_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // ignorar si el click es sobre la barra de desplazamiento
        if (FindAncestor<ScrollBar>(e.OriginalSource as DependencyObject) != null) return;
        _marqueeStart = PuntoContenido(e.GetPosition(lst));
        _marqueeActive = true;
        _marqueeDragging = false;
        // no capturamos ni marcamos manejado aún: un click simple debe seleccionar la fila con normalidad
    }

    private void Lst_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_marqueeActive || e.LeftButton != MouseButtonState.Pressed) return;
        var curV = e.GetPosition(lst);            // viewport (para el borde del auto-scroll)
        _marqueeUltimoV = curV;
        var cur = PuntoContenido(curV);           // contenido
        if (!_marqueeDragging)
        {
            if (Math.Abs(cur.X - _marqueeStart.X) < 5 && Math.Abs(cur.Y - _marqueeStart.Y) < 5) return;   // umbral
            _marqueeDragging = true;
            _marqueeBase.Clear();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)   // Ctrl = añadir a lo ya seleccionado
                foreach (var it in lst.SelectedItems) _marqueeBase.Add(it);
            lst.CaptureMouse();
            marquee.Visibility = Visibility.Visible;
        }
        PintarYSeleccionar(new Rect(_marqueeStart, cur));
        // Si el ratón roza el borde superior/inferior, desplazar solo para alcanzar lo de fuera.
        double h = lst.ActualHeight;
        if (curV.Y < MarqueeBorde || curV.Y > h - MarqueeBorde) _marqueeAutoScroll.Start();
        else _marqueeAutoScroll.Stop();
        e.Handled = true;
    }

    private void Lst_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _marqueeAutoScroll.Stop();
        if (_marqueeDragging)
        {
            marquee.Visibility = Visibility.Collapsed;
            lst.ReleaseMouseCapture();
            _marqueeDragging = false;
            _marqueeActive = false;
            _ = UpdateForSelectionAsync();   // refresca la vista de detalle una sola vez
            e.Handled = true;
            return;
        }
        _marqueeActive = false;   // fue un click simple: lo gestiona el ListView
    }

    /// <summary>Un tic de auto-scroll: desplaza hacia el borde tocado y rehace la banda con el nuevo offset.</summary>
    private void MarqueeAutoScrollPaso()
    {
        var sv = LstScroll;
        if (sv == null || !_marqueeDragging) { _marqueeAutoScroll.Stop(); return; }
        double v = _marqueeUltimoV.Y, h = lst.ActualHeight;
        if (v < MarqueeBorde && sv.VerticalOffset > 0)
            sv.ScrollToVerticalOffset(Math.Max(0, sv.VerticalOffset - MarqueePaso));
        else if (v > h - MarqueeBorde && sv.VerticalOffset < sv.ScrollableHeight)
            sv.ScrollToVerticalOffset(Math.Min(sv.ScrollableHeight, sv.VerticalOffset + MarqueePaso));
        else { _marqueeAutoScroll.Stop(); return; }
        // el ratón no se ha movido, pero el contenido sí: recomputar el punto de contenido
        PintarYSeleccionar(new Rect(_marqueeStart, PuntoContenido(_marqueeUltimoV)));
    }

    /// <summary>Dibuja la banda (convertida a viewport) y selecciona lo que abarca, en coordenadas de contenido.</summary>
    private void PintarYSeleccionar(Rect band)
    {
        var sv = LstScroll;
        double hoff = sv?.HorizontalOffset ?? 0, voff = sv?.VerticalOffset ?? 0;
        Canvas.SetLeft(marquee, band.X - hoff);
        Canvas.SetTop(marquee, band.Y - voff);
        marquee.Width = band.Width;
        marquee.Height = band.Height;
        ApplyMarqueeSelection(band);
    }

    /// <summary>Selecciona las filas cuyo rectángulo (en coordenadas de contenido) intersecta la banda.</summary>
    private void ApplyMarqueeSelection(Rect band)
    {
        var sv = LstScroll;
        double hoff = sv?.HorizontalOffset ?? 0, voff = sv?.VerticalOffset ?? 0;
        for (int i = 0; i < lst.Items.Count; i++)
        {
            if (lst.ItemContainerGenerator.ContainerFromIndex(i) is not ListViewItem lvi) continue;
            Rect r;
            try
            {
                var tlV = lvi.TranslatePoint(new Point(0, 0), lst);   // viewport
                r = new Rect(new Point(tlV.X + hoff, tlV.Y + voff),   // → contenido
                             new Size(lvi.ActualWidth, lvi.ActualHeight));
            }
            catch { continue; }
            bool want = r.IntersectsWith(band) || _marqueeBase.Contains(lst.Items[i]);
            if (lvi.IsSelected != want) lvi.IsSelected = want;
        }
    }

    // ---------- quitar de la lista / menú contextual ----------
    private void Lst_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            DeshacerPapelera();
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Delete) return;
        RemoveSelectedRows();
        e.Handled = true;
    }

    /// <summary>
    /// Ctrl+Z en la lista de Comprimir: restaura el último vídeo enviado a la Papelera propia
    /// (a su sitio en disco) y devuelve su fila a la lista. Si su sitio ya se reocupó, no pisa nada.
    /// </summary>
    private void DeshacerPapelera()
    {
        var nombre = Reindex.PapeleraApp.DeshacerUltimo();
        if (nombre == null)
        {
            lblProg.Text = _pilaPapelera.Count == 0
                ? Textos.Instancia.MainNadaQueRecuperar
                : Textos.Instancia.MainNoSePudoRecuperar;
            return;
        }
        if (_pilaPapelera.Count > 0 && Path.GetFileName(_pilaPapelera.Peek().Path) == nombre)
        {
            var r = _pilaPapelera.Pop();
            if (!_rows.Contains(r)) _rows.Add(r);
        }
        lblProg.Text = string.Format(Textos.Instancia.MainRecuperado, nombre);
    }

    /// <summary>Con el botón derecho, si la fila no estaba seleccionada pasa a serlo (como el explorador).</summary>
    private void Lst_RightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is not { } item) return;
        if (!item.IsSelected) { lst.UnselectAll(); item.IsSelected = true; }
    }

    /// <summary>
    /// Quita las filas de la lista. NO toca los archivos: solo deja de tenerlos en cuenta.
    /// Para borrar de verdad está «Enviar el archivo a la Papelera», que además pregunta.
    /// </summary>
    private void RemoveSelectedRows()
    {
        var sel = SelectedRows();
        if (sel.Count == 0) return;
        foreach (var r in sel) _rows.Remove(r);
        lblProg.Text = sel.Count == 1
            ? Textos.Instancia.MainQuitadoUno
            : string.Format(Textos.Instancia.MainQuitadosVarios, sel.Count);
    }

    /// <summary>
    /// Quitar pistas del fichero seleccionado SIN recomprimir. Es de uno en uno a propósito: las
    /// pistas de cada fichero son distintas (idiomas, orden), y aplicar «quita la segunda de
    /// audio» en bloque se llevaría por delante la equivocada en cuanto uno no coincida.
    /// </summary>
    private async Task QuitarPistasDeSeleccionado()
    {
        if (SelectedRows().FirstOrDefault() is not { } r || !File.Exists(r.Path)) return;

        lblProg.Text = Textos.Instancia.MainLeyendoPistas;
        var (pistas, dur) = await _engine.PistasDeAsync(r.Path);
        if (pistas.Count == 0)
        {
            lblProg.Text = "";
            DialogWindow.Aviso(this, Textos.Instancia.MainPistasTitulo, Textos.Instancia.MainPistasNoLeidas);
            return;
        }
        if (pistas.Count(p => p.Tipo != TipoPista.Video) == 0)
        {
            lblProg.Text = "";
            DialogWindow.Aviso(this, Textos.Instancia.MainPistasTitulo, Textos.Instancia.MainPistasSoloVideo);
            return;
        }

        lblProg.Text = "";
        var win = new PistasWindow(_engine, r.Path, pistas, dur) { Owner = this };
        win.ShowDialog();
        if (!win.SeCambioAlgo) return;

        // El fichero es OTRO: pesa menos y tiene otras pistas. La fila se rehace en vez de
        // parchearla —su tamaño es de solo lectura— y así vuelve a sondearse con lo que hay ahora.
        var ruta = r.Path;
        _rows.Remove(r);
        await AddFilesAsync(new[] { ruta });
        lblProg.Text = string.Format(Textos.Instancia.MainPistasQuitadas, Path.GetFileName(ruta));
    }

    private void UpdateContextMenu()
    {
        int n = SelectedRows().Count;
        miCtxRemove.IsEnabled = miCtxRecycle.IsEnabled = miCtxCopyPath.IsEnabled = n > 0;
        miCtxOpenFolder.IsEnabled = n > 0;
        miCtxInvert.IsEnabled = _rows.Count > 0;
        miCtxSelectAll.IsEnabled = _rows.Count > 0;
        miCtxRemove.Header = n > 1
            ? string.Format(Textos.Instancia.MainCtxQuitarVarias, n)
            : Textos.Instancia.MainCtxQuitar;
        miCtxRecycle.Header = n > 1
            ? string.Format(Textos.Instancia.MainCtxPapeleraVarios, n)
            : Textos.Instancia.MainCtxPapelera;
    }

    private void OpenContainingFolder()
    {
        if (SelectedRows().FirstOrDefault() is not { } r) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{r.Path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { lblProg.Text = string.Format(Textos.Instancia.MainNoSePudoAbrirCarpeta, ex.Message); }
    }

    private void CopySelectedPaths()
    {
        var sel = SelectedRows();
        if (sel.Count == 0) return;
        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, sel.Select(r => r.Path)));
            lblProg.Text = sel.Count == 1
                ? Textos.Instancia.MainRutaCopiada
                : string.Format(Textos.Instancia.MainRutasCopiadas, sel.Count);
        }
        catch (Exception ex) { lblProg.Text = string.Format(Textos.Instancia.MainNoSePudoCopiar, ex.Message); }
    }

    private void InvertSelection()
    {
        for (int i = 0; i < lst.Items.Count; i++)
            if (lst.ItemContainerGenerator.ContainerFromIndex(i) is ListViewItem lvi)
                lvi.IsSelected = !lvi.IsSelected;
    }

    /// <summary>Las filas seleccionadas, en el orden de la lista.</summary>
    private List<VideoRow> SelectedRows() => _rows.Where(r => lst.SelectedItems.Contains(r)).ToList();

    private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d != null)
        {
            if (d is T t) return t;
            d = VisualTreeHelper.GetParent(d);
        }
        return null;
    }

    /// <summary>Primer descendiente del tipo pedido en el árbol visual (búsqueda en anchura).</summary>
    private static T? FindDescendant<T>(DependencyObject? raiz) where T : DependencyObject
    {
        if (raiz == null) return null;
        int n = VisualTreeHelper.GetChildrenCount(raiz);
        for (int i = 0; i < n; i++)
        {
            var hijo = VisualTreeHelper.GetChild(raiz, i);
            if (hijo is T t) return t;
            if (FindDescendant<T>(hijo) is { } nieto) return nieto;
        }
        return null;
    }

    /// <summary>Genera y muestra el fotograma del vídeo seleccionado en el punto del timeline.</summary>
    private async Task ShowScrubFrameAsync()
    {
        if (lst.SelectedItem is not VideoRow r || !r.Probed) return;
        _scrubCts?.Cancel();
        var cts = _scrubCts = new CancellationTokenSource();
        var token = cts.Token;

        Directory.CreateDirectory(_thumbDir);
        var scrub = Path.Combine(_thumbDir, $"frame_{Guid.NewGuid():N}.jpg");   // archivo único: sin carreras
        bool ok = await Engine.MakeThumbnailAsync(r.Path, scrub, (int)sldPreview.Value);
        if (token.IsCancellationRequested || !ok || !File.Exists(scrub)) { TryDelete(scrub); return; }
        try
        {
            var bytes = await File.ReadAllBytesAsync(scrub, token);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.EndInit();
            bmp.Freeze();
            if (!token.IsCancellationRequested) imgPrev.Source = bmp;
        }
        catch { }
        TryDelete(scrub);
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

    // ---------- estimación de ahorro ----------
    /// <summary>
    /// Enseña una de las pestañas laterales. Va por nombre y no por booleano —como estaba—
    /// porque un <c>bool</c> que significa «la segunda» solo se entiende si hay exactamente
    /// dos, y añadir una tercera obligaba a reescribirlo entero.
    /// </summary>
    private void ShowSideTab(string cual)
    {
        var paneles = new (string Clave, Border Pestaña, UIElement Panel)[]
        {
            ("detalle", tabDetalle, panelDetalle),
            ("estim",   tabEstim,   panelEstim),
        };

        foreach (var (clave, pestaña, panel) in paneles)
        {
            bool activa = clave == cual;
            panel.Visibility = activa ? Visibility.Visible : Visibility.Collapsed;
            pestaña.BorderBrush = activa ? (Brush)FindResource("Accent") : Brushes.Transparent;
            ((TextBlock)pestaña.Child).Foreground = (Brush)FindResource(activa ? "Accent300" : "Neutral400");
        }
    }

    private void UpdateEstimate()
    {
        if (lst.SelectedItem is not VideoRow r || !r.Probed) { ClearEstimate(); return; }
        var est = Estimator.Compute(r, BuildOptions());
        if (!est.Valid) { ClearEstimate(); return; }
        lblEstSize.Text = "≈ " + Human(est.EstBytes);
        lblEstSaving.Text = string.Format(Textos.Instancia.MainEstAhorroResumen, est.SavedPct, Human(est.SavedBytes));
        SetBar(barVQ, est.VideoQuality); SetBar(barVS, est.VideoSaving);
        SetBar(barAQ, est.AudioQuality); SetBar(barAS, est.AudioSaving);
        lblEstDetail.Text = string.Format(Textos.Instancia.MainEstDetalle, est.EstVideoKbps, est.EstAudioKbps);
    }

    private void ClearEstimate()
    {
        lblEstSize.Text = "—";
        lblEstSaving.Text = Textos.Instancia.MainEstSinSeleccion;
        foreach (var b in new[] { barVQ, barVS, barAQ, barAS }) SetBar(b, 0);
        lblEstDetail.Text = "";
    }

    private void SetBar(StackPanel panel, int value)
    {
        panel.Children.Clear();
        var on = (Brush)FindResource("Accent");
        var off = (Brush)FindResource("Neutral800");
        for (int i = 0; i < 5; i++)
            panel.Children.Add(new Border
            {
                Width = 20, Height = 6, CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 3, 0),
                Background = i < value ? on : off,
            });
    }

    private static string Human(long bytes) => bytes switch
    {
        >= (1L << 30) => $"{bytes / (double)(1L << 30):n2} GB",
        >= (1L << 20) => $"{bytes / (double)(1L << 20):n0} MB",
        _ => $"{bytes / 1024.0:n0} KB",   // por debajo de 1 MB, «0 MB» no dice nada
    };

    private static string FmtTime(int sec) => $"{sec / 60}:{sec % 60:D2}";

    // ---------- medición real del tamaño (muestreo) ----------
    private async Task OnMeasureAsync()
    {
        if (lst.SelectedItem is not VideoRow r || !r.Probed)
        {
            DialogWindow.Aviso(this, Textos.Instancia.MainMedirTitulo, Textos.Instancia.MainSeleccionaVideoAnalizado);
            return;
        }
        if (_running) { DialogWindow.Aviso(this, Textos.Instancia.MainMedirTitulo, Textos.Instancia.MainMedirEspera); return; }

        var opt = BuildOptions();
        if (opt.AudioOnly) { DialogWindow.Aviso(this, Textos.Instancia.MainMedirTitulo, Textos.Instancia.MainMedirSoloAudio); return; }

        btnMeasure.IsEnabled = false;
        progRow.Visibility = Visibility.Visible; bar.Value = 0;
        lblProg.Text = Textos.Instancia.MainMidiendo;
        lblMeasure.Text = Textos.Instancia.MainMidiendoFragmentos;
        var cts = new CancellationTokenSource();
        try
        {
            int kbps = await _engine.MeasureVideoBitrateAsync(r.Path, opt, new PreviewReporter(this), cts.Token);
            if (kbps <= 0)
            {
                lblMeasure.Text = Textos.Instancia.MainMedirFallo;
                lblProg.Text = Textos.Instancia.MainMedirFalloCorto;
                return;
            }
            // el contenido manda: deducimos su factor de complejidad y recalibramos
            double factor = Estimator.FactorFromMeasurement(r, opt, kbps);
            Estimator.ComplexityFactor = factor;
            _settings.ComplexityFactor = factor;
            SettingsStore.Save(_settings);
            UpdateEstimate();
            var veredicto = factor < 0.85 ? Textos.Instancia.MainMedidoFacil
                          : factor > 1.15 ? Textos.Instancia.MainMedidoExigente
                          : Textos.Instancia.MainMedidoNormal;
            lblMeasure.Text = string.Format(Textos.Instancia.MainMedidoResultado, kbps, veredicto, factor);
            lblProg.Text = Textos.Instancia.MainMedicionAplicada;
        }
        catch (OperationCanceledException) { lblMeasure.Text = Textos.Instancia.MainMedicionCancelada; }
        catch (Exception ex) { lblMeasure.Text = string.Format(Textos.Instancia.MainMedirError, ex.Message); }
        finally
        {
            cts.Dispose();
            btnMeasure.IsEnabled = true;
            progRow.Visibility = Visibility.Collapsed;
        }
    }

    // ---------- previsualización de 10 s ----------
    private async Task OnPreviewAsync()
    {
        if (_previewCts != null) { _previewCts.Cancel(); return; }   // ya generando → el botón cancela
        if (lst.SelectedItem is not VideoRow r || !r.Probed)
        {
            DialogWindow.Aviso(this, Textos.Instancia.MainPrevisualizarTitulo, Textos.Instancia.MainSeleccionaVideoAnalizado);
            return;
        }
        _previewCts = new CancellationTokenSource();
        btnPreview.Content = Textos.Instancia.MainCancelarPrevisualizacion;
        progRow.Visibility = Visibility.Visible; bar.Value = 0;
        lblProg.Text = Textos.Instancia.MainGenerandoPrevisualizacion;
        try
        {
            Directory.CreateDirectory(_previewDir);
            foreach (var old in Directory.GetFiles(_previewDir)) TryDelete(old);   // borra la anterior
            var dest = Path.Combine(_previewDir, $"preview_{Guid.NewGuid():N}.mkv");
            var path = await _engine.PreviewAsync(r.Path, BuildOptions(), (int)sldPreview.Value, dest,
                                                  new PreviewReporter(this), _previewCts.Token);
            if (path != null)
            {
                lblProg.Text = Textos.Instancia.MainPrevisualizacionLista;
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            else lblProg.Text = Textos.Instancia.MainPrevisualizacionFallo;
        }
        catch (OperationCanceledException) { lblProg.Text = Textos.Instancia.MainPrevisualizacionCancelada; }
        catch (Exception ex) { lblProg.Text = string.Format(Textos.Instancia.MainPrevisualizacionError, ex.Message); }
        finally
        {
            progRow.Visibility = Visibility.Collapsed;
            _previewCts?.Dispose(); _previewCts = null;
            btnPreview.Content = _previewBtnContent;
        }
    }

    // reportero de progreso para la preview (actualiza la barra)
    private sealed class PreviewReporter : IEngineReporter
    {
        private readonly MainWindow _w;
        public PreviewReporter(MainWindow w) => _w = w;
        public void Log(string l) { }
        public void FileStart(int i, int t, string n, double d) { }
        public void FileProgress(double f, string raw) => _w.Dispatcher.BeginInvoke(() => _w.bar.Value = f);
        public void FileDone(FileResult r) { }
    }

    // ---------- eliminar (papelera) ----------
    private void DeleteSelected()
    {
        var sel = SelectedRows();
        if (sel.Count == 0) { DialogWindow.Aviso(this, Textos.Instancia.MainEliminarTitulo, Textos.Instancia.MainSinVideosSeleccionados); return; }
        double mb = sel.Sum(r => r.Bytes) / 1048576.0;
        if (!DialogWindow.Confirmar(this, Textos.Instancia.MainEliminarMarcadosTitulo,
                                    string.Format(Textos.Instancia.MainEliminarMarcadosPregunta, sel.Count, mb))) return;
        _pilaPapelera.Clear();   // una tanda nueva; el Ctrl+Z restaura de esta tanda hacia atrás
        foreach (var r in sel)
        {
            // Papelera PROPIA de la app (no la de Windows): permite deshacer con Ctrl+Z de forma
            // fiable y, al acumularse o cerrar, se finaliza en la Papelera del sistema.
            if (Reindex.PapeleraApp.Enviar(r.Path) != null) { _rows.Remove(r); _pilaPapelera.Push(r); }
            else r.Estado = Textos.Instancia.MainEstadoErrorAlBorrar;
        }
        lblProg.Text = _pilaPapelera.Count > 0
            ? string.Format(Textos.Instancia.MainALaPapeleraDeshacer, Reindex.PapeleraApp.UltimoNombre)
            : Textos.Instancia.MainEnviadosALaPapelera;
    }
    private void DeleteFolders()
    {
        var dirs = SelectedRows().Select(r => Path.GetDirectoryName(r.Path)!).Distinct().ToList();
        if (dirs.Count == 0)
        {
            var s = txtSrc.Text.Trim();
            if (!string.IsNullOrEmpty(s) && Directory.Exists(s)) dirs = new() { s };
        }
        if (dirs.Count == 0) { DialogWindow.Aviso(this, Textos.Instancia.MainEliminarCarpeta, Textos.Instancia.MainEliminarCarpetaSinNada); return; }
        if (!DialogWindow.Confirmar(this, Textos.Instancia.MainEliminarCarpeta,
                                    string.Format(Textos.Instancia.MainEliminarCarpetaPregunta,
                                                  dirs.Count, string.Join("\n", dirs)))) return;
        foreach (var d in dirs) RecycleBin.Send(d);
        foreach (var r in _rows.Where(r => Path.GetDirectoryName(r.Path) is { } d && dirs.Contains(d)).ToList()) _rows.Remove(r);
        lblProg.Text = Textos.Instancia.MainCarpetasALaPapelera;
    }

    // ---------- comprimir ----------
    private EncodeOptions BuildOptions()
    {
        var opt = new EncodeOptions
        {
            Output = EffectiveOutput() is { Length: > 0 } o ? o : null,
            Lang = string.IsNullOrWhiteSpace(cboLang.Text) ? "spa" : cboLang.Text.Trim(),
            KeepLangs = CheckedLangs(pnlALang),
            Force = chkForce.IsChecked == true,
            DryRun = chkDry.IsChecked == true,
            NameRule = _settings.Rename,
            VideoCodec = cboCodec.SelectedIndex switch { 1 => "h264", 2 => "av1", _ => "hevc" },
        };
        switch (cboFmt.SelectedIndex)
        {
            case 1: opt.Container = "mp4"; break;
            case 2: opt.Container = "webm"; break;
            case 3: opt.AudioOnly = true; opt.AudioFormat = "mp3"; break;
            case 4: opt.AudioOnly = true; opt.AudioFormat = "m4a"; break;
            case 5: opt.AudioOnly = true; opt.AudioFormat = "flac"; break;
            case 6: opt.AudioOnly = true; opt.AudioFormat = "opus"; break;
            default: opt.Container = "mkv"; break;
        }
        opt.Quality = cboQ.SelectedIndex switch { 1 => 22, 2 => 24, 3 => 27, 4 => 30, _ => 0 };
        opt.MaxHeight = cboRes.SelectedIndex switch { 1 => 1080, 2 => 720, 3 => 480, _ => 0 };
        opt.AudioBitrate = cboAud.SelectedIndex switch { 1 => 192, 2 => 160, 3 => 128, 4 => 96, _ => 0 };

        var sChips = pnlSLang.Children.OfType<CheckBox>().ToList();
        var sChecked = CheckedLangs(pnlSLang);
        if (sChips.Count > 0 && sChecked.Count == 0) opt.NoSubs = true;
        else if (sChecked.Count > 0 && sChecked.Count < sChips.Count) opt.SubLangs = sChecked;
        return opt;
    }

    private async Task RunAsync()
    {
        var selRows = SelectedRows();
        if (selRows.Count == 0) { DialogWindow.Aviso(this, Textos.Instancia.MainPaginaComprimir, Textos.Instancia.MainComprimirSinSeleccion); return; }
        var sel = selRows.Select(r => r.Path).ToList();

        var opt = BuildOptions();

        // ¿enviar cada original a la Papelera tras comprimirlo? (según preferencias / modal)
        bool deleteOriginals = false;
        if (!opt.DryRun)
        {
            if (_settings.AfterCompress == AfterCompress.Ask)
            {
                var (proceed, del, remember) = AskAfterCompress(selRows.Count);
                if (!proceed) return;   // canceló el modal
                deleteOriginals = del;
                if (remember)
                {
                    _settings.AfterCompress = del ? AfterCompress.RecycleOriginal : AfterCompress.Keep;
                    SettingsStore.Save(_settings);
                }
            }
            else deleteOriginals = _settings.AfterCompress == AfterCompress.RecycleOriginal;
        }

        _cts = new CancellationTokenSource();
        _running = true;
        btnRun.IsEnabled = false; btnCancel.IsEnabled = true; btnPause.IsEnabled = true;
        _paused = false; btnPause.Content = Textos.Instancia.MainPausar;
        progRow.Visibility = Visibility.Visible; bar.Value = 0;
        tglLog.IsChecked = true;
        txtLog.Clear();
        lblProg.Text = string.Format(Textos.Instancia.MainProcesando, sel.Count);
        ActualizarTextoPildora(0, sel.Count, 0);   // ya recalcula el indicador global

        foreach (var r in selRows) r.Estado = Textos.Instancia.MainEstadoEnCola;
        var reporter = new Reporter(this, deleteOriginals, selRows);
        // «ok» vive fuera del try porque el resumen de subtítulos lo consulta al terminar
        var ok = new List<FileResult>();
        try
        {
            // Task.Run: los trozos síncronos del motor (candados, sondeos, espacio) no
            // deben correr en el hilo de interfaz — sobre OneDrive son viajes de red y la
            // ventana se movía a tirones con la CPU libre.
            var tok = _cts.Token;
            var results = await Task.Run(() => _engine.CompressAsync(sel, opt, reporter, tok), tok);
            ok = results.Where(r => r.OutBytes != null).ToList();
            if (ok.Count > 0)
            {
                double inGb = ok.Sum(r => r.InBytes) / 1073741824.0;
                double outGb = ok.Sum(r => r.OutBytes!.Value) / 1073741824.0;
                int pct = (int)Math.Round(100 - (outGb / Math.Max(inGb, 1e-9) * 100));
                lblProg.Text = string.Format(Textos.Instancia.MainTerminadoResumen, inGb, outGb, pct, ok.Count);
            }
            else lblProg.Text = Textos.Instancia.MainTerminado;
        }
        catch (OperationCanceledException) { lblProg.Text = Textos.Instancia.MainCancelado; }
        // «Error» sale de Comun; los dos puntos son puntuación, no texto que traducir.
        catch (Exception ex) { lblProg.Text = Textos.Instancia.Error + ": " + ex.Message; AppendLog("ERROR: " + ex); }
        finally
        {
            _running = false; _paused = false;
            btnRun.IsEnabled = true; btnCancel.IsEnabled = false;
            btnPause.IsEnabled = false; btnPause.Content = Textos.Instancia.MainPausar;
            progRow.Visibility = Visibility.Collapsed;
            _cts?.Dispose(); _cts = null;
            // La píldora pasa a «✓ N hechos» un momento y se retira sola: si desapareciera
            // de golpe, quien estuviera en «Organizar» no llegaría a saber que terminó.
            AnunciarFinEnPildora(sel.Count);
        }

        // Con la ventana ya en reposo: si se han quedado subtítulos fuera, decirlo.
        WarnAboutLostSubtitles(ok);
    }

    // ---------- presets ----------
    private void ReloadPresets(string? select)
    {
        var all = PresetStore.Factory().Concat(PresetStore.LoadUser()).ToList();
        _applyingPreset = true;
        cboPreset.ItemsSource = all;
        _applyingPreset = false;
        // El nombre entra por NombreVigente y no crudo: los presets de fábrica se
        // guardan por su nombre TRADUCIDO, así que el que se eligió con la app en
        // castellano no existe con la app en inglés. Se quedaba sin casar y el
        // preset por defecto parecía haberse borrado solo.
        if (select != null)
        {
            var vigente = PresetStore.NombreVigente(select);
            cboPreset.SelectedItem = all.FirstOrDefault(p => p.Name == vigente);
        }
    }

    private void ApplyPreset()
    {
        if (_applyingPreset || cboPreset.SelectedItem is not Preset p) return;
        _applyingPreset = true;
        cboFmt.SelectedIndex = p.Fmt; cboCodec.SelectedIndex = p.Codec;
        cboQ.SelectedIndex = p.Quality; cboRes.SelectedIndex = p.Res; cboAud.SelectedIndex = p.Audio;
        cboLang.Text = p.Lang;
        // «Subcarpetas» NO se toca aquí: es un ajuste de exploración del disco que manda
        // el usuario desde Preferencias, no parte de la receta de codificación. Antes el
        // preset lo reactivaba y pisaba la preferencia.
        _applyingPreset = false;
        UpdateEstimate();
    }

    private void SavePreset()
    {
        var name = InputName(Textos.Instancia.MainNombrePresetPrompt, Textos.Instancia.MainGuardarPresetTitulo);
        if (string.IsNullOrWhiteSpace(name)) return;
        var user = PresetStore.LoadUser();
        user.RemoveAll(x => x.Name == name);
        user.Add(new Preset
        {
            Name = name, Fmt = cboFmt.SelectedIndex, Codec = cboCodec.SelectedIndex,
            Quality = cboQ.SelectedIndex, Res = cboRes.SelectedIndex, Audio = cboAud.SelectedIndex,
            Lang = cboLang.Text,
        });
        PresetStore.SaveUser(user);
        ReloadPresets(name);
        lblProg.Text = string.Format(Textos.Instancia.MainPresetGuardado, name);
    }

    private string? InputName(string prompt, string title)
    {
        var win = new Window
        {
            Title = title, Width = 380, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow,
            Background = (Brush)FindResource("Surface"),
        };
        var tb = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
        var ok = new Button { Content = Textos.Instancia.Guardar, Width = 90, Style = (Style)FindResource("BtnPrimary"), IsDefault = true };
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = prompt, Foreground = (Brush)FindResource("Text"), Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(tb);
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(ok);
        panel.Children.Add(row);
        win.Content = panel;
        string? result = null;
        ok.Click += (_, _) => { result = tb.Text.Trim(); win.DialogResult = true; };
        win.ContentRendered += (_, _) => tb.Focus();
        win.ShowDialog();
        return result;
    }

    /// <summary>
    /// Si algún vídeo ha salido sin los subtítulos que el usuario tenía marcados, decírselo
    /// a la cara al terminar. Antes solo constaba en el registro y se daba por hecho que iban.
    /// </summary>
    private void WarnAboutLostSubtitles(List<FileResult> done)
    {
        var afectados = done.Where(r => !string.IsNullOrEmpty(r.SubtitleWarning)).ToList();
        if (afectados.Count == 0) return;

        lblProg.Text += Textos.Instancia.MainAvisoSubsPie;

        // Un motivo por línea; casi siempre será uno solo repetido en varios archivos.
        var motivos = afectados.Select(r => r.SubtitleWarning!).Distinct().ToList();
        string cuerpo = afectados.Count == 1
            ? string.Format(Textos.Instancia.MainSubsUnoAfectado, afectados[0].Name, motivos[0])
            : string.Format(Textos.Instancia.MainSubsVariosAfectados, afectados.Count, done.Count)
              + string.Join("\n\n", motivos)
              + Textos.Instancia.MainSubsAfectadosLista + string.Join("\n· ", afectados.Take(8).Select(r => r.Name))
              + (afectados.Count > 8 ? string.Format(Textos.Instancia.MainSubsYMas, afectados.Count - 8) : "");

        DialogWindow.Aviso(this, Textos.Instancia.MainSubsTitulo, cuerpo);
    }

    private void TogglePause()
    {
        if (!_running) return;
        _paused = !_paused;
        if (_paused) { _engine.Pause(); btnPause.Content = Textos.Instancia.MainReanudar; lblProg.Text = Textos.Instancia.MainEnPausa; }
        else { _engine.Resume(); btnPause.Content = Textos.Instancia.MainPausar; lblProg.Text = Textos.Instancia.MainComprimiendoBarra; }
    }

    private void AppendLog(string line)
    {
        txtLog.AppendText(line + "\n");
        txtLog.ScrollToEnd();
    }

    /// <summary>
    /// Ancho por debajo del cual el panel lateral estorba más de lo que aporta. Sale de
    /// sumar lo que la tabla necesita para que sus columnas se lean (≈620) más los 262 del
    /// panel y los márgenes: por debajo, el panel se estaría quedando con espacio que la
    /// tabla necesita más.
    /// </summary>
    private const double AnchoMinimoConLateral = 940;

    /// <summary>
    /// WPF no tiene consultas de medios, así que la adaptación al ancho se hace aquí. Es un
    /// solo umbral a propósito: cuantos más puntos de corte, más difícil es que el resultado
    /// siga siendo coherente en todos ellos.
    /// </summary>
    private void AjustarAAncho()
    {
        bool cabeElLateral = ActualWidth >= AnchoMinimoConLateral;

        colLateral.Width = cabeElLateral ? new GridLength(262) : new GridLength(0);
        if (sideCol != null)
            sideCol.Visibility = cabeElLateral ? Visibility.Visible : Visibility.Collapsed;

        // El texto del botón de renombrar sobra antes que el botón: se queda el icono, que
        // con su descripción emergente sigue diciendo lo que hace.
        if (lblRename != null)
            lblRename.Visibility = ActualWidth >= 1080 ? Visibility.Visible : Visibility.Collapsed;

        // La versión junto al nombre es lo primero que sobra en la barra de título.
        if (lblVersion != null)
            lblVersion.Visibility = ActualWidth >= 900 ? Visibility.Visible : Visibility.Collapsed;
        // (El conmutador de páginas ya es un desplegable compacto: no hay texto de pestañas
        //  que esconder cuando la ventana se estrecha.)
    }

    // ─────────────────────── páginas de oficio ───────────────────────

    /// <summary>
    /// Cambia entre «Comprimir» y «Organizar». Las dos páginas comparten ventana, barra de
    /// título y registro, pero cada una tiene su lista y su ciclo de vida: cambiar de página
    /// NO detiene lo que estuviera corriendo en la otra.
    /// </summary>
    private enum Pagina { Comprimir, Organizar, Recortes }

    /// <summary>El nombre del modo tal y como lo declaran los complementos.</summary>
    private static string ModoDe(Pagina p) => p switch
    {
        Pagina.Organizar => "organizar",
        Pagina.Recortes => "recortes",
        _ => "comprimir",
    };

    /// <summary>
    /// Pone el botón de complementos al día para la página en la que estés.
    ///
    /// <para>
    /// Se recuenta en cada cambio de página y no una vez al arrancar: uno
    /// instalado con la aplicación abierta tiene que aparecer sin reiniciar, y
    /// además cada página enseña un conjunto distinto.
    /// </para>
    /// <para>
    /// Si no hay ninguno para esta página, el botón se esconde. Uno que solo
    /// sirve para decir «aquí no hay nada» ocupa sitio y no informa de nada.
    /// </para>
    /// </summary>
    private void RefrescarComplementos()
    {
        var modo = ModoDe(_paginaActual);
        var suyos = Complementos.Descubridor.Buscar().Bueno.Where(c => c.SaleEn(modo)).ToList();

        btnComplementos.Visibility = suyos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        lblCuantosComplementos.Text = suyos.Count.ToString();
        btnComplementos.Tag = suyos;
    }

    /// <summary>
    /// El menú del botón. Cada complemento abre SU panel; abajo, la gestión.
    ///
    /// <para>
    /// Se arma al pulsarlo y no al arrancar, por lo mismo que el recuento: lo
    /// instalado puede cambiar mientras la aplicación está abierta.
    /// </para>
    /// </summary>
    private void OnBotonComplementos(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = btnComplementos, Placement = PlacementMode.Bottom };

        foreach (var c in (btnComplementos.Tag as List<Complementos.Complemento>) ?? new())
        {
            var it = new MenuItem { Header = c.Nombre, ToolTip = c.Descripcion };
            var suyo = c;
            it.Click += (_, _) => AbrirComplementos(suyo);
            menu.Items.Add(it);
        }

        if (menu.Items.Count > 0) menu.Items.Add(new Separator());
        var gestionar = new MenuItem { Header = Textos.Instancia.MainComplementosGestionar };
        gestionar.Click += (_, _) => AbrirComplementos(null);
        menu.Items.Add(gestionar);

        menu.IsOpen = true;
    }

    private void AbrirComplementos(Complementos.Complemento? cual)
    {
        var v = new ComplementosWindow(pageOrganizar.CatalogoAbierto, pageOrganizar.LoQueHay, cual)
            { Owner = this };
        var trajo = v.ShowDialog();

        // Al volver puede haber uno nuevo instalado.
        RefrescarComplementos();

        // Y si se trajo algo y se pidió llevarlo, se va a Organizar apuntando ahí.
        // Cerrar la ventana y dejar a quien acaba de bajar cuarenta capítulos
        // buscando la carpeta a mano es abandonar el trabajo en el último paso.
        if (trajo == true && v.CarpetaTraida is { } carpeta)
        {
            CambiarPagina(Pagina.Organizar);
            pageOrganizar.ApuntarA(carpeta);
        }
    }

    private void CambiarPagina(Pagina pagina)
    {
        _paginaActual = pagina;
        // El botón del conmutador refleja la página actual.
        lblPaginaActual.Text = pagina switch
        {
            Pagina.Comprimir => Textos.Instancia.MainPaginaComprimir,
            Pagina.Organizar => Textos.Instancia.MainPaginaOrganizar,
            _ => Textos.Instancia.MainPaginaRecortes,
        };

        RefrescarComplementos();

        var comprimir = pagina == Pagina.Comprimir ? Visibility.Visible : Visibility.Collapsed;
        rowOrigen.Visibility = comprimir;
        rowOpciones.Visibility = comprimir;
        rowTabla.Visibility = comprimir;
        rowAcciones.Visibility = comprimir;
        // La barra de progreso solo reaparece si de verdad hay algo comprimiendo
        progRow.Visibility = pagina == Pagina.Comprimir && _running
            ? Visibility.Visible : Visibility.Collapsed;

        pageOrganizar.Visibility = pagina == Pagina.Organizar ? Visibility.Visible : Visibility.Collapsed;
        pageRecortes.Visibility = pagina == Pagina.Recortes ? Visibility.Visible : Visibility.Collapsed;
        // Recortes deja de trabajar (reloj, previsualizadores, vídeo, plasma) cuando no está a
        // la vista, y lo retoma al volver. Un tab oculto ya no se DIBUJA —WPF lo hace solo—,
        // pero sin esto seguiría decodificando vídeo y goteando fotogramas en segundo plano.
        pageRecortes.EnPantalla(pagina == Pagina.Recortes);
        ActualizarIndicadorGlobal();
    }

    // --- Indicador global de proceso -------------------------------------------------
    // Una sola píldora en la cabecera para TODAS las pestañas. Cada tarea de proceso deja
    // aquí su etiqueta (null = sin tarea); la píldora enseña la de mayor prioridad cuya
    // pestaña NO es la que miras —esa ya te muestra su progreso en línea—, así nunca hay dos
    // indicadores a la vez y desde cualquier tab sabes que algo sigue vivo.
    private string? _compEtiqueta;      // «Comprimir»  (prioridad alta)
    private string? _recortesEtiqueta;  // «Recortes»   (prioridad media)
    private Pagina _pillDestino = Pagina.Comprimir;   // adónde te lleva al pulsar la píldora

    /// <summary>Recalcula qué tarea (si alguna) muestra la píldora de la cabecera.</summary>
    private void ActualizarIndicadorGlobal()
    {
        // (página, prioridad, texto) de cada tarea activa. Mayor prioridad manda.
        var tareas = new List<(Pagina pag, int prio, string txt)>();
        if (_compEtiqueta != null) tareas.Add((Pagina.Comprimir, 3, _compEtiqueta));
        if (_recortesEtiqueta != null) tareas.Add((Pagina.Recortes, 2, _recortesEtiqueta));

        var visible = tareas
            .Where(t => t.pag != _paginaActual)      // la de tu propia pestaña ya se ve en línea
            .OrderByDescending(t => t.prio)
            .ToList();

        if (visible.Count == 0) { pillFondo.Visibility = Visibility.Collapsed; return; }

        var t0 = visible[0];
        lblPill.Text = t0.txt;
        _pillDestino = t0.pag;
        // El punto pulsa solo mientras la tarea vive; en el «✓ … hecho» final no.
        pillDot.Visibility = t0.txt.StartsWith("✓") ? Visibility.Collapsed : Visibility.Visible;
        pillFondo.Visibility = Visibility.Visible;
    }

    /// <summary>Marca la pestaña destino de la píldora al pulsarla.</summary>
    private void IrAPestaña(Pagina pag)
    {
        if (pag == Pagina.Comprimir) tabComprimir.IsChecked = true;
        else if (pag == Pagina.Organizar) tabOrganizar.IsChecked = true;
        else tabRecortes.IsChecked = true;
    }

    // --- Conmutador de páginas: desplegable compacto -----------------------------------
    // El header muestra UN botón con la página actual; al pulsarlo se despliega la lista con
    // TODAS las páginas. Así ocupa lo mínimo y escala a cualquier número. Los RadioButton
    // (ocultos) siguen siendo el modelo de estado; el menú se arma solo desde ellos.

    /// <summary>Despliega la lista de páginas bajo el botón. Se arma sola desde las pestañas.</summary>
    private void OnMasTabs(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = btnPagina,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };
        foreach (var rb in panelTabs.Children.OfType<RadioButton>())
        {
            var destino = rb;
            // El rótulo sale del propio RadioButton: su Content ya viene del catálogo de
            // textos, así que el menú habla el idioma en curso sin una segunda tabla.
            var item = new MenuItem { Header = rb.Content ?? "", IsChecked = rb.IsChecked == true };
            item.Click += (_, _) => destino.IsChecked = true;   // dispara Checked → CambiarPagina
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    /// <summary>
    /// Ordena la tabla de «Comprimir» al pulsar una cabecera. El primer clic ordena ascendente;
    /// repetir la misma columna alterna a descendente. La columna activa marca la flecha ▲/▼ y
    /// las numéricas (TAMAÑO, DURACIÓN) ordenan por su valor real, no por el texto formateado.
    /// </summary>
    private void OnCabeceraTablaClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader cab) return;
        if (cab.Role == GridViewColumnHeaderRole.Padding || cab.Column == null) return;

        string? campo = HeaderSort.GetField(cab.Column);
        if (string.IsNullOrEmpty(campo)) return;

        if (_lstSortHeader == cab)
            _lstSortDir = _lstSortDir == ListSortDirection.Ascending
                ? ListSortDirection.Descending : ListSortDirection.Ascending;
        else
        {
            if (_lstSortHeader != null) HeaderSort.SetGlyph(_lstSortHeader, "");
            _lstSortHeader = cab;
            _lstSortDir = ListSortDirection.Ascending;
        }

        var vista = CollectionViewSource.GetDefaultView(lst.ItemsSource);
        if (vista == null) return;
        vista.SortDescriptions.Clear();
        vista.SortDescriptions.Add(new SortDescription(campo, _lstSortDir));
        vista.Refresh();

        HeaderSort.SetGlyph(cab, _lstSortDir == ListSortDirection.Ascending ? "▲" : "▼");
    }

    /// <summary>Etiqueta de la tarea de «Comprimir»: «Comprimiendo 3/8 · 31 %».</summary>
    private void ActualizarTextoPildora(int hecho, int total, double fraccion)
    {
        _compEtiqueta = total > 0
            ? string.Format(Textos.Instancia.MainPildoraProgreso, hecho, total, fraccion * 100)
            : Textos.Instancia.MainPildoraComprimiendo;
        ActualizarIndicadorGlobal();
    }

    /// <summary>
    /// Al acabar la compresión, la píldora dice «✓ N hechos» unos segundos y se retira. Quitarla
    /// de golpe dejaría a quien esté en otra pestaña sin enterarse de que terminó.
    /// </summary>
    private void AnunciarFinEnPildora(int total)
    {
        _compEtiqueta = string.Format(
            total == 1 ? Textos.Instancia.MainPildoraFinUno : Textos.Instancia.MainPildoraFinVarios, total);
        ActualizarIndicadorGlobal();

        var reloj = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        reloj.Tick += (s, _) =>
        {
            reloj.Stop();
            _compEtiqueta = null;
            ActualizarIndicadorGlobal();
        };
        reloj.Start();
    }

    // reporter que marshaliza el avance del motor al hilo de la UI
    private sealed class Reporter : IEngineReporter
    {
        private readonly MainWindow _w;
        private readonly bool _del;
        private readonly IReadOnlyList<VideoRow> _queue;   // mismo orden que las rutas del motor
        private VideoRow? _current;
        private int _lastPct = -1;

        public Reporter(MainWindow w, bool deleteOriginals, IReadOnlyList<VideoRow> queue)
        { _w = w; _del = deleteOriginals; _queue = queue; }

        private VideoRow? RowOf(string path) =>
            _w._rows.FirstOrDefault(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase));

        public void Log(string line) => _w.Dispatcher.BeginInvoke(() => _w.AppendLog(line));
        // El índice y el total se guardan para la píldora de la barra de título: cuando el
        // usuario se va a «Organizar» deja de ver la barra de progreso, y esa píldora es lo
        // único que le dice que la compresión sigue viva.
        private int _idx, _total;

        public void FileStart(int i, int t, string name, double dur) =>
            _w.Dispatcher.BeginInvoke(() =>
            {
                _idx = i; _total = t;
                _w.lblProg.Text = $"[{i}/{t}] {name}";
                _w.bar.Value = 0;
                _lastPct = -1;
                // el motor numera del 1 al total en el mismo orden en que se le pasó la cola
                _current = i >= 1 && i <= _queue.Count ? _queue[i - 1] : null;
                if (_current != null) _current.Estado = Textos.Instancia.MainEstadoComprimiendo;
                _w.ActualizarTextoPildora(i, t, 0);
            });

        public void FileProgress(double frac, string raw)
        {
            int pct = (int)Math.Round(Math.Clamp(frac, 0, 1) * 100);
            _w.Dispatcher.BeginInvoke(() =>
            {
                _w.bar.Value = frac;
                // solo se reescribe la fila al cambiar de entero: si no, repinta sin parar
                if (_current != null && pct != _lastPct)
                {
                    _lastPct = pct;
                    _current.Estado = string.Format(Textos.Instancia.MainEstadoComprimiendoPct, pct);
                }
                _w.ActualizarTextoPildora(_idx, _total, frac);
            });
        }

        /// <summary>Un archivo que el motor se salta: la fila cuenta el motivo.</summary>
        public void FileSkipped(string sourcePath, string reason) =>
            _w.Dispatcher.BeginInvoke(() =>
            {
                if (RowOf(sourcePath) is { } row) row.Estado = reason;
            });

        // Borrado iteración a iteración: en cuanto un archivo se comprime OK, su original
        // se envía a la Papelera (en segundo plano, sin bloquear la codificación siguiente).
        public void FileDone(FileResult r)
        {
            // el resultado se queda escrito en la fila: cuánto se ahorró, o que falló
            _w.Dispatcher.BeginInvoke(() =>
            {
                if (RowOf(r.SourcePath) is { } row)
                    row.Estado = r.Ok ? $"{r.Status} · {Human(r.OutBytes!.Value)}" : Textos.Instancia.Error;
                if (_current != null && string.Equals(_current.Path, r.SourcePath, StringComparison.OrdinalIgnoreCase))
                    _current = null;
            });

            if (!Engine.ShouldRecycleSource(_del, r)) return;
            Task.Run(() =>
            {
                bool ok = RecycleBin.Send(r.SourcePath);
                _w.Dispatcher.BeginInvoke(() =>
                {
                    if (ok)
                    {
                        var row = _w._rows.FirstOrDefault(x => string.Equals(x.Path, r.SourcePath, StringComparison.OrdinalIgnoreCase));
                        if (row != null) _w._rows.Remove(row);
                        _w.AppendLog(string.Format(Textos.Instancia.MainLogOriginalAPapelera, r.Name));
                    }
                    else _w.AppendLog(string.Format(Textos.Instancia.MainLogPapeleraFallo, r.Name));
                });
            });
        }

        public void DiskFull(bool paused) => _w.Dispatcher.BeginInvoke(() =>
        {
            _w.lblProg.Text = paused
                ? Textos.Instancia.MainDiscoLleno
                : Textos.Instancia.MainComprimiendoBarra;
        });
    }

    // ---------- actualizaciones ----------
    private async Task CheckUpdateAsync(bool manual)
    {
        if (manual)
        {
            btnCheckUpdate.IsEnabled = false;         // evita repetir la búsqueda a lo tonto
            miCheckUpd.IsEnabled = false;
            lblProg.Text = Textos.Instancia.MainBuscandoActualizaciones;
        }
        try
        {
            var res = await Updater.CheckAsync();

            if (res.Failed)
            {
                // antes esto se confundía con «estás al día»: se decía que no había nada
                // nuevo aunque en realidad no hubiera habido conexión
                if (manual) lblProg.Text = string.Format(Textos.Instancia.MainNoSePudoComprobar, res.Error);
                return;
            }
            if (!res.Available)
            {
                if (manual) lblProg.Text = string.Format(Textos.Instancia.MainYaAlDia, Updater.Current);
                return;
            }

            var info = res.Info!;
            lblUpdate.Text = string.Format(Textos.Instancia.MainVersionNuevaDetalle, info.Tag, Updater.Current);
            updateBar.Visibility = Visibility.Visible;
            btnUpdateNow.Click -= OnUpdateNow;   // evitar suscripción doble
            btnUpdateNow.Click += OnUpdateNow;
            _pendingUpdate = info;
            // faltaba: sin esto se quedaba «Buscando actualizaciones…» para siempre
            lblProg.Text = string.Format(Textos.Instancia.MainVersionNuevaProgreso, info.Tag);
        }
        finally
        {
            btnCheckUpdate.IsEnabled = true;
            miCheckUpd.IsEnabled = true;
        }
    }

    private UpdateInfo? _pendingUpdate;
    private async void OnUpdateNow(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate == null) return;
        btnUpdateNow.IsEnabled = false;
        lblUpdate.Text = string.Format(Textos.Instancia.MainDescargando, _pendingUpdate.AssetName);
        progRow.Visibility = Visibility.Visible; bar.Value = 0;
        lblProg.Text = Textos.Instancia.MainDescargandoActualizacion;
        try
        {
            var progress = new Progress<double>(p =>
            {
                lblUpdate.Text = string.Format(Textos.Instancia.MainDescargandoPct, _pendingUpdate.AssetName, p * 100);
                bar.Value = p;
            });
            var installer = await Updater.DownloadAsync(_pendingUpdate, progress);
            lblUpdate.Text = Textos.Instancia.MainDescargaLista;
            lblProg.Text = Textos.Instancia.MainAbriendoInstalador;
            Updater.LaunchInstallerAndExit(installer);
        }
        catch (Exception ex)
        {
            btnUpdateNow.IsEnabled = true;
            progRow.Visibility = Visibility.Collapsed;
            lblUpdate.Text = Textos.Instancia.MainDescargaFallo;
            lblProg.Text = Textos.Instancia.MainDescargaFalloProgreso;
            DialogWindow.Aviso(this, Textos.Instancia.MainActualizarTitulo,
                               string.Format(Textos.Instancia.MainDescargaFalloDetalle, ex.Message));
        }
    }

    // ---------- cierre ----------
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_running)
        {
            if (DialogWindow.Confirmar(this, Textos.Instancia.MainMenuSalir, Textos.Instancia.MainSalirEnCurso))
                _cts?.Cancel();
            else { e.Cancel = true; return; }
        }
        // liberar miniaturas y previsualizaciones cacheadas en %TEMP% (foco: ahorro de almacenamiento)
        try { if (Directory.Exists(_thumbDir)) Directory.Delete(_thumbDir, true); } catch { }
        try { if (Directory.Exists(_previewDir)) Directory.Delete(_previewDir, true); } catch { }
        base.OnClosing(e);
    }
}
