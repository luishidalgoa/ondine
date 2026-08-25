using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Thickness = Avalonia.Thickness;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine.Ava;

/// <summary>Una mini-historia del episodio, con el código que le toca («E1a»).</summary>
public sealed record SegmentoVista(string Codigo, string Titulo);

/// <summary>Una fila del explorador, ya lista para pintar.</summary>
public sealed class EpisodioVista
{
    public required CatalogEpisode Ep { get; init; }

    public string Codigo => $"E{Ep.Num}";

    /// <summary>
    /// Las historias del episodio, cada una con SU código. Un capítulo puede traer 2-3
    /// mini-historias y se numeran «1a», «1b», «1c» —igual que en los anexos de referencia—,
    /// así que se pintan una por línea. Juntarlas en un renglón separadas por rayas hacía
    /// pensar que el episodio tenía un título larguísimo en vez de tres historias distintas.
    /// </summary>
    public IReadOnlyList<SegmentoVista> Segmentos
    {
        get
        {
            var t = Ep.TitulosSalida;
            if (t.Count == 0) return [new SegmentoVista(Codigo, Textos.Instancia.CatalogoSinTitulo)];
            if (t.Count == 1) return [new SegmentoVista(Codigo, t[0])];
            return t.Select((x, i) => new SegmentoVista($"{Codigo}{SegmentSplitter.Letra(i)}", x)).ToList();
        }
    }

    /// <summary>«3 historias», o vacío si solo trae una: sin nada que contar, no se dice.</summary>
    public string Cuantas => Ep.TitulosSalida.Count > 1
        ? string.Format(Textos.Instancia.CatalogoCuantasHistorias, Ep.TitulosSalida.Count)
        : "";

    /// <summary>«2009 · 03/07/2009» — lo que confirma o desmiente una sospecha.</summary>
    public string Detalle
    {
        get
        {
            var partes = new List<string>();
            if (Ep.Temporada.HasValue) partes.Add(Ep.Temporada.Value.ToString());
            if (Ep.FechaParsed.HasValue) partes.Add(Ep.FechaParsed.Value.ToString("dd/MM/yyyy"));
            return string.Join(" · ", partes);
        }
    }

    /// <summary>
    /// Las dos cosas de la línea de abajo, ya juntas. En WPF eran tres <c>Run</c> pegados
    /// dentro del mismo <c>TextBlock</c>; aquí se monta el texto y se pinta de una vez.
    /// Menos piezas para lo mismo, y una menos que se puede quedar sin pintar.
    /// </summary>
    public string DetalleYCuantas =>
        Cuantas.Length == 0 ? Detalle
        : Detalle.Length == 0 ? Cuantas
        : $"{Detalle}   {Cuantas}";

    public bool EsEspecial => Ep.Especial;

    // ── ¿lo tengo? ──
    // Null = no hay carpeta analizada detrás, así que no se dice nada. Callar es la
    // respuesta correcta ahí: un distintivo que dijera «te falta» sin haber mirado ningún
    // disco sería una afirmación inventada.

    /// <summary>Lo que hay de este episodio, o <c>null</c> si no se ha analizado nada.</summary>
    public CoberturaCatalogo.Tenencia? Que { get; init; }

    public bool TenenciaVisible => Que is not null;

    public string TenenciaEtiqueta => Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => Textos.Instancia.CatalogoTengo,
        CoberturaCatalogo.Tengo.AMedias => Textos.Instancia.CatalogoTengoAMedias,
        _ => Textos.Instancia.CatalogoNoTengo,
    };

    // Los mismos tres colores que el semáforo de Organizar: verde lo que está, ámbar lo que
    // está a medias y apagado lo que no. Reaprender un código de colores por pantalla es
    // trabajo que se le carga a quien mira.
    public IBrush TenenciaColor => Pincel(Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => "OrgOk",
        CoberturaCatalogo.Tengo.AMedias => "OrgWarn",
        _ => "Neutral500",
    });

    public IBrush TenenciaFondo => Pincel(Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => "OrgOkBg",
        CoberturaCatalogo.Tengo.AMedias => "OrgWarnBg",
        _ => "Field",
    });

    /// <summary>El fichero al que apunta el distintivo, o vacío si no hay ninguno.</summary>
    public string Fichero => Que is { Ficheros.Count: > 0 } q ? q.Ficheros[0] : "";

    /// <summary>Mano solo si hay a dónde ir: lo que parece pulsable tiene que serlo.</summary>
    public Cursor TenenciaCursor => new(Fichero.Length > 0 ? StandardCursorType.Hand
                                                           : StandardCursorType.Arrow);

    public string TenenciaTip => Que is not { Ficheros.Count: > 0 } q
        ? Textos.Instancia.CatalogoNoTengoTip
        : q.Ficheros.Count == 1
            ? string.Format(Textos.Instancia.CatalogoTengoTip, q.Ficheros[0])
            : string.Format(Textos.Instancia.CatalogoTengoTipVarios, q.Ficheros[0], q.Ficheros.Count);

    private static IBrush Pincel(string clave) =>
        Avalonia.Application.Current is { } app && app.TryFindResource(clave, out var v) && v is IBrush b
            ? b : Brushes.Gray;
}

/// <summary>
/// Explorador de solo lectura del catálogo elegido, portado de <c>CatalogoWindow</c>.
///
/// <para>
/// Existe para verificar una propuesta sin abrir el JSON a mano: dudar de una sugerencia
/// («¿de verdad el planeta espejo es el 175?») obligaba a buscar entre cientos de episodios
/// fuera de la app, y esa fricción deja dudas razonables sin comprobar.
/// </para>
/// <para>
/// <b>El panel del JSON pierde maquinaria al portarse.</b> En WPF era un <c>RichTextBox</c>
/// con un <c>FlowDocument</c>, que Avalonia no tiene; aquí es un <c>SelectableTextBlock</c>
/// con sus <c>Inlines</c>, se sigue pudiendo seleccionar y copiar, y de paso desaparece una
/// trampa del original: allí había que vaciar y rellenar el documento existente en vez de
/// asignar uno nuevo, porque reemplazarlo dejaba al lector de accesibilidad leyendo el viejo
/// para siempre. Aquí no hay documento que reemplazar.
/// </para>
/// <para>
/// Y quién es cada trozo del JSON tampoco se decide aquí: lo dice <see cref="ColoreadoDeJson"/>,
/// que vive en el motor con sus pruebas. Esta ventana solo traduce el nombre del color a un
/// pincel del tema.
/// </para>
/// </summary>
public partial class Catalogo : Window
{
    private readonly ReindexCatalog _cat = null!;
    private readonly bool _modoElegir;

    /// <summary>En modo elegir: el episodio escogido y, si es solo una historia, su letra.</summary>
    public CatalogEpisode? Elegido { get; private set; }
    public string? SegElegido { get; private set; }

    /// <summary>
    /// Qué hay de cada episodio en la carpeta analizada. Vacío si se abrió sin carpeta
    /// detrás: entonces el explorador no dice nada sobre tenencia.
    /// </summary>
    private readonly Dictionary<int, CoberturaCatalogo.Tenencia> _tenencia = [];

    public Catalogo() => AvaloniaXamlLoader.Load(this);

    /// <param name="loQueHay">
    /// Lo identificado en la carpeta, para poder decir de cada episodio si lo tienes y dónde.
    /// Opcional: en modo elegir, o al abrirlo sin haber analizado, no hay nada que comparar.
    /// </param>
    public Catalogo(ReindexCatalog cat, string? consultaInicial = null, bool modoElegir = false,
                    IReadOnlyList<ReindexResolution>? loQueHay = null) : this()
    {
        _cat = cat;
        _modoElegir = modoElegir;

        var soloFaltan = Chk("chkSoloFaltan");
        if (loQueHay is { Count: > 0 })
        {
            _tenencia = CoberturaCatalogo.PorEpisodio(cat, loQueHay);
            soloFaltan.IsVisible = true;
            soloFaltan.IsCheckedChanged += (_, _) => Refrescar();
        }

        var lista = Lista();
        if (modoElegir)
        {
            Lbl("lblPie").Text = Textos.Instancia.CatalogoPieElegir;
            lista.DoubleTapped += (_, _) => ElegirSeleccionado();
        }

        Lbl("lblTitulo").Text = string.Format(Textos.Instancia.CatalogoTituloSerie, cat.Serie);
        Txt("txtBuscar").TextChanged += (_, _) => Refrescar();
        Btn("btnCerrar").Click += (_, _) => Close();

        // Se puede llegar con una consulta puesta (p. ej. desde una fila): el buscador
        // arranca apuntando a lo que se estaba mirando en vez de a la lista entera.
        Txt("txtBuscar").Text = consultaInicial ?? "";
        Refrescar();
        Opened += (_, _) => Txt("txtBuscar").Focus();

        lista.SelectionChanged += (_, _) => MostrarJson();
        Btn("btnCopiarJson").Click += async (_, _) => await CopiarJson();
        // Cerrar también deselecciona: si la fila siguiera elegida, volver a pincharla no
        // dispararía el cambio de selección y el panel no reaparecería.
        Btn("btnCerrarJson").Click += (_, _) => lista.SelectedItem = null;
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private TextBox Txt(string n) => this.FindControl<TextBox>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private ListBox Lista() => this.FindControl<ListBox>("lista")!;

    /// <summary>El JSON tal cual, sin colores, para el botón de copiar.</summary>
    private string _jsonActual = "";

    private static readonly System.Text.Json.JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Un episodio sin temporada no debe enseñar «"temporada": null»: en el fichero del
        // usuario esa clave sencillamente no está, y esto pretende ser LO QUE HAY.
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// El JSON del episodio elegido, serializado desde el MISMO modelo que usa el motor: lo
    /// que se ve aquí es lo que el identificador está leyendo, no una reconstrucción.
    /// </summary>
    private void MostrarJson()
    {
        var col = this.FindControl<Grid>("zonaLista")!.ColumnDefinitions[1];
        var borde = this.FindControl<Border>("bordeJson")!;

        if (Lista().SelectedItem is not EpisodioVista v)
        {
            col.Width = new GridLength(0);
            borde.IsVisible = false;   // sin esto quedaba una tira del borde
            return;
        }

        col.Width = new GridLength(300);
        borde.IsVisible = true;
        Lbl("lblJsonTitulo").Text = string.Format(Textos.Instancia.CatalogoJsonTitulo, v.Ep.Num);
        _jsonActual = System.Text.Json.JsonSerializer.Serialize(v.Ep, OpcionesJson);

        var caja = this.FindControl<SelectableTextBlock>("txtJson")!;
        caja.Inlines ??= [];
        caja.Inlines.Clear();
        foreach (var t in ColoreadoDeJson.Partir(_jsonActual))
            caja.Inlines.Add(new Run(t.Texto) { Foreground = Pincel(t.Color) });
    }

    private static IBrush Pincel(string clave) =>
        Avalonia.Application.Current is { } app && app.TryFindResource(clave, out var v) && v is IBrush b
            ? b : Brushes.Gray;

    private async Task CopiarJson()
    {
        try
        {
            if (Clipboard is null) return;
            var datos = new DataTransfer();
            datos.Add(DataTransferItem.CreateText(_jsonActual));
            await Clipboard.SetDataAsync(datos);
            Lbl("lblJsonTitulo").Text += Textos.Instancia.CatalogoJsonCopiado;
        }
        catch { /* portapapeles ocupado por otro proceso: se reintenta a mano */ }
    }

    // ── modo elegir ──

    private void ElegirSeleccionado()
    {
        if (Lista().SelectedItem is not EpisodioVista v) return;

        // Con una sola historia no hay nada que preguntar; con varias, sí: el fichero puede
        // ser el episodio entero o solo uno de sus trozos.
        if (v.Ep.TitulosSalida.Count <= 1) { Terminar(v.Ep, null); return; }

        int n = v.Ep.TitulosSalida.Count;
        Lbl("lblHistoriaTitulo").Text = string.Format(Textos.Instancia.CatalogoHistoriaPregunta, v.Ep.Num, n);
        var panel = this.FindControl<StackPanel>("panelHistorias")!;
        panel.Children.Clear();

        Button Boton(string texto, string tema, Action accion)
        {
            var b = new Button
            {
                Content = texto,
                Theme = this.TryFindResource(tema, out var t) ? t as ControlTheme : null,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 12.5,
            };
            b.Click += (_, _) => accion();
            return b;
        }

        // El caso más común, destacado: el episodio entero.
        panel.Children.Add(Boton(Textos.Instancia.CatalogoHistoriaCompleto, "BtnSecondary",
                                 () => Terminar(v.Ep, null)));

        // O SOLO ALGUNAS: una casilla por historia. Se pueden marcar varias (p. ej. la a y
        // la c de tres), no solo una. Las letras de las marcadas van pegadas al número.
        panel.Children.Add(new TextBlock
        {
            Text = Textos.Instancia.CatalogoHistoriaMarcar,
            FontSize = 11.5,
            Foreground = Pincel("Neutral500"),
            Margin = new Thickness(2, 6, 0, 6),
        });

        var casillas = new List<CheckBox>();
        for (int i = 0; i < n && i < 6; i++)
        {
            char letra = (char)('a' + i);
            var chk = new CheckBox
            {
                Content = string.Format(Textos.Instancia.CatalogoHistoriaOpcion,
                                        v.Ep.TitulosSalida[i], $"E{v.Ep.Num}{letra}"),
                Foreground = Pincel("Text"),
                FontSize = 12.5,
                Margin = new Thickness(2, 0, 0, 7),
            };
            casillas.Add(chk);
            panel.Children.Add(chk);
        }

        panel.Children.Add(Boton(Textos.Instancia.CatalogoHistoriaUsar, "BtnPrimary", () =>
        {
            var letras = new string(Enumerable.Range(0, casillas.Count)
                .Where(i => casillas[i].IsChecked == true)
                .Select(i => (char)('a' + i))
                .ToArray());
            if (letras.Length == 0) return;                          // nada marcado: no hace nada
            Terminar(v.Ep, letras.Length == n ? null : letras);      // todas marcadas = el completo
        }));

        panel.Children.Add(Boton(Textos.Instancia.Cancelar, "BtnGhostMuted",
            () => this.FindControl<Border>("overlayHistoria")!.IsVisible = false));

        this.FindControl<Border>("overlayHistoria")!.IsVisible = true;
    }

    private void Terminar(CatalogEpisode ep, string? seg)
    {
        Elegido = ep;
        SegElegido = seg;
        // Close(true), no Close(ep): quien abre pide ShowDialog<bool> y lee Elegido.
        // Devolver el episodio hacía que la conversión reventara dentro de Close, así que
        // elegir a mano no elegía nada.
        Close(true);
    }

    /// <summary>
    /// Enseña el fichero en el gestor de archivos. Es lo que convierte el distintivo en algo
    /// que sirve: saber que lo tienes sin saber dónde deja el trabajo a medias.
    /// </summary>
    public void OnIrAlFichero(object? remitente, RoutedEventArgs e)
    {
        // No se propaga a la fila: pulsar el distintivo es una acción suya, y en modo elegir
        // la fila responde al doble clic escogiendo el episodio.
        e.Handled = true;

        if ((remitente as Control)?.DataContext is EpisodioVista v && v.Fichero.Length > 0)
            EnElGestorDeArchivos.Ensenar(v.Fichero);
    }

    private void Refrescar()
    {
        var encontrados = CatalogSearch.Filtrar(_cat, Txt("txtBuscar").Text ?? "");
        bool soloFaltan = Chk("chkSoloFaltan").IsChecked == true;

        // El filtro se aplica DESPUÉS del buscador: los dos acotan, y así se puede buscar
        // «playa» y quedarse solo con las que faltan.
        if (soloFaltan)
            encontrados = encontrados
                .Where(e => !_tenencia.TryGetValue(e.Num, out var t)
                            || t.Que != CoberturaCatalogo.Tengo.Entero)
                .ToList();

        Lista().ItemsSource = encontrados.Select(e => new EpisodioVista
        {
            Ep = e,
            Que = _tenencia.Count == 0 ? null
                : _tenencia.TryGetValue(e.Num, out var t) ? t
                : new CoberturaCatalogo.Tenencia(CoberturaCatalogo.Tengo.Nada, []),
        }).ToList();

        Lbl("lblCuenta").Text = soloFaltan
            ? string.Format(Textos.Instancia.CatalogoCuentaFaltan, encontrados.Count, _cat.Episodios.Count)
            : encontrados.Count == _cat.Episodios.Count
                ? string.Format(encontrados.Count == 1
                                    ? Textos.Instancia.CatalogoCuentaEpisodioUno
                                    : Textos.Instancia.CatalogoCuentaEpisodios,
                                encontrados.Count)
                : string.Format(Textos.Instancia.CatalogoCuentaFiltrada,
                                encontrados.Count, _cat.Episodios.Count);
    }
}
