using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

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
            if (t.Count == 0)
                return new[] { new SegmentoVista(Codigo, Textos.Instancia.CatalogoSinTitulo) };
            if (t.Count == 1)
                return new[] { new SegmentoVista(Codigo, t[0]) };
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

    public Visibility EsEspecial => Ep.Especial ? Visibility.Visible : Visibility.Collapsed;

    // ── ¿lo tengo? ──
    // Null = no hay carpeta analizada detrás, así que no se dice nada. Callar es la
    // respuesta correcta ahí: un distintivo que dijera «te falta» sin haber mirado
    // ningún disco sería una afirmación inventada.

    /// <summary>Lo que hay de este episodio, o <c>null</c> si no se ha analizado nada.</summary>
    public CoberturaCatalogo.Tenencia? Que { get; init; }

    public Visibility TenenciaVisible => Que is null ? Visibility.Collapsed : Visibility.Visible;

    public string TenenciaEtiqueta => Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => Textos.Instancia.CatalogoTengo,
        CoberturaCatalogo.Tengo.AMedias => Textos.Instancia.CatalogoTengoAMedias,
        _ => Textos.Instancia.CatalogoNoTengo,
    };

    // Los mismos tres colores que el semáforo de Organizar: verde lo que está,
    // ámbar lo que está a medias y apagado lo que no. Reaprender un código de
    // colores por pantalla es trabajo que se le carga a quien mira.
    public Brush TenenciaColor => Pincel(Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => "OrgOk",
        CoberturaCatalogo.Tengo.AMedias => "OrgWarn",
        _ => "Neutral500",
    });

    public Brush TenenciaFondo => Pincel(Que?.Que switch
    {
        CoberturaCatalogo.Tengo.Entero => "OrgOkBg",
        CoberturaCatalogo.Tengo.AMedias => "OrgWarnBg",
        _ => "Field",
    });

    /// <summary>El fichero al que apunta el distintivo, o vacío si no hay ninguno.</summary>
    public string Fichero => Que is { Ficheros.Count: > 0 } q ? q.Ficheros[0] : "";

    /// <summary>Mano solo si hay a dónde ir: lo que parece pulsable tiene que serlo.</summary>
    public Cursor TenenciaCursor => Fichero.Length > 0 ? Cursors.Hand : Cursors.Arrow;

    public string TenenciaTip => Que is not { Ficheros.Count: > 0 } q
        ? Textos.Instancia.CatalogoNoTengoTip
        : q.Ficheros.Count == 1
            ? string.Format(Textos.Instancia.CatalogoTengoTip, q.Ficheros[0])
            : string.Format(Textos.Instancia.CatalogoTengoTipVarios, q.Ficheros[0], q.Ficheros.Count);

    private static Brush Pincel(string clave) =>
        Application.Current?.TryFindResource(clave) as Brush ?? Brushes.Gray;
}

/// <summary>
/// Explorador de solo lectura del catálogo elegido, con buscador por número o título.
///
/// Existe para verificar una propuesta sin abrir el JSON a mano: dudar de una sugerencia
/// («¿de verdad el planeta espejo es el 175?») obligaba a buscar entre cientos de episodios
/// fuera de la app, y esa fricción deja dudas razonables sin comprobar.
/// </summary>
public partial class CatalogoWindow : Window
{
    private readonly ReindexCatalog _cat;
    private readonly bool _modoElegir;

    /// <summary>En modo elegir: el episodio escogido y, si es solo una historia, su letra.</summary>
    public CatalogEpisode? Elegido { get; private set; }
    public string? SegElegido { get; private set; }

    /// <summary>
    /// Qué hay de cada episodio en la carpeta analizada. Vacío si se abrió sin
    /// carpeta detrás: entonces el explorador no dice nada sobre tenencia.
    /// </summary>
    private readonly Dictionary<int, CoberturaCatalogo.Tenencia> _tenencia = new();

    /// <param name="loQueHay">
    /// Lo identificado en la carpeta, para poder decir de cada episodio si lo
    /// tienes y dónde. Opcional: en modo elegir, o al abrirlo sin haber
    /// analizado, no hay nada que comparar.
    /// </param>
    public CatalogoWindow(ReindexCatalog cat, string? consultaInicial = null, bool modoElegir = false,
                          IReadOnlyList<ReindexResolution>? loQueHay = null)
    {
        InitializeComponent();
        _cat = cat;
        _modoElegir = modoElegir;

        if (loQueHay is { Count: > 0 })
        {
            _tenencia = CoberturaCatalogo.PorEpisodio(cat, loQueHay);
            chkSoloFaltan.Visibility = Visibility.Visible;
            chkSoloFaltan.Checked += (_, _) => Refrescar();
            chkSoloFaltan.Unchecked += (_, _) => Refrescar();
        }

        if (modoElegir)
        {
            lblPie.Text = Textos.Instancia.CatalogoPieElegir;
            lista.MouseDoubleClick += (_, _) => ElegirSeleccionado();
        }

        lblTitulo.Text = string.Format(Textos.Instancia.CatalogoTituloSerie, cat.Serie);
        txtBuscar.TextChanged += (_, _) => Refrescar();
        btnCerrar.Click += (_, _) => Close();

        // Se puede llegar con una consulta puesta (p. ej. desde una fila): el buscador
        // arranca apuntando a lo que se estaba mirando en vez de a la lista entera.
        txtBuscar.Text = consultaInicial ?? "";
        Refrescar();
        Loaded += (_, _) => txtBuscar.Focus();

        lista.SelectionChanged += (_, _) => MostrarJson();
        btnCopiarJson.Click += (_, _) =>
        {
            try { Clipboard.SetText(_jsonActual); lblJsonTitulo.Text += Textos.Instancia.CatalogoJsonCopiado; }
            catch { /* portapapeles ocupado por otro proceso: se reintenta a mano */ }
        };
        // Cerrar también deselecciona: si la fila siguiera elegida, volver a pincharla no
        // dispararía el cambio de selección y el panel no reaparecería.
        btnCerrarJson.Click += (_, _) => lista.SelectedItem = null;
    }

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
        if (lista.SelectedItem is not EpisodioVista v)
        {
            colJson.Width = new GridLength(0);
            bordeJson.Visibility = Visibility.Collapsed;   // sin esto quedaba una tira del borde
            return;
        }

        colJson.Width = new GridLength(300);
        bordeJson.Visibility = Visibility.Visible;
        lblJsonTitulo.Text = string.Format(Textos.Instancia.CatalogoJsonTitulo, v.Ep.Num);
        _jsonActual = System.Text.Json.JsonSerializer.Serialize(v.Ep, OpcionesJson);

        // Se VACÍA y RELLENA el documento existente en vez de asignar uno nuevo: reemplazar
        // Document desengancha al lector de accesibilidad, que se queda leyendo el documento
        // original (vacío) para siempre. Con el mismo documento, lo que se pinta y lo que se
        // lee son la misma cosa — y además se puede verificar.
        var doc = rtbJson.Document;
        doc.Blocks.Clear();
        doc.PageWidth = 2000;   // sin renglones artificiales: las líneas son las del JSON
        doc.Blocks.Add(Colorear(_jsonActual));
    }

    // ── coloreado de sintaxis ──
    // Los colores son los del tema, no los de VS: claves en el acento, cadenas en el verde
    // del semáforo, números en su ámbar y la puntuación apagada. Así el panel es de esta
    // app y no un pegote de otro editor.
    private static readonly System.Windows.Media.Brush ColClave = Pincel("Accent300");
    private static readonly System.Windows.Media.Brush ColCadena = Pincel("OrgOk");
    private static readonly System.Windows.Media.Brush ColNumero = Pincel("OrgWarn");
    private static readonly System.Windows.Media.Brush ColBool = Pincel("OrgDanger");
    private static readonly System.Windows.Media.Brush ColSigno = Pincel("Neutral500");

    private static System.Windows.Media.Brush Pincel(string clave) =>
        Application.Current?.TryFindResource(clave) as System.Windows.Media.Brush
        ?? System.Windows.Media.Brushes.Gray;

    /// <summary>
    /// Un JSON como documento coloreado. El recorrido es un autómata mínimo, no un parser:
    /// este JSON lo acaba de producir el serializador, así que siempre está bien formado y
    /// basta con distinguir cadena / número / palabra / signo. La única decisión con miga:
    /// una cadena es CLAVE si lo siguiente (saltando espacios) son los dos puntos.
    /// </summary>
    private static System.Windows.Documents.Paragraph Colorear(string json)
    {
        var parrafo = new System.Windows.Documents.Paragraph { Margin = new Thickness(0) };
        int i = 0;

        void Trozo(string t, System.Windows.Media.Brush b) =>
            parrafo.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b });

        while (i < json.Length)
        {
            char c = json[i];

            if (c == '"')
            {
                int j = i + 1;
                while (j < json.Length && (json[j] != '"' || json[j - 1] == '\\')) j++;
                var cadena = json[i..Math.Min(j + 1, json.Length)];

                int k = j + 1;
                while (k < json.Length && char.IsWhiteSpace(json[k])) k++;
                bool esClave = k < json.Length && json[k] == ':';

                Trozo(cadena, esClave ? ColClave : ColCadena);
                i = j + 1;
            }
            else if (char.IsDigit(c) || c == '-')
            {
                int j = i;
                while (j < json.Length && (char.IsDigit(json[j]) || json[j] is '-' or '+' or '.' or 'e' or 'E')) j++;
                Trozo(json[i..j], ColNumero);
                i = j;
            }
            else if (char.IsLetter(c))   // true / false (null no llega: se omite al serializar)
            {
                int j = i;
                while (j < json.Length && char.IsLetter(json[j])) j++;
                Trozo(json[i..j], ColBool);
                i = j;
            }
            else
            {
                int j = i;
                while (j < json.Length && json[j] is not ('"' or '-') &&
                       !char.IsLetterOrDigit(json[j])) j++;
                Trozo(json[i..j], ColSigno);
                i = j;
            }
        }

        return parrafo;
    }

    // ── modo elegir ──

    private void ElegirSeleccionado()
    {
        if (lista.SelectedItem is not EpisodioVista v) return;

        // Con una sola historia no hay nada que preguntar; con varias, sí: el fichero puede
        // ser el episodio entero o solo uno de sus trozos.
        if (v.Ep.TitulosSalida.Count <= 1) { Terminar(v.Ep, null); return; }

        int n = v.Ep.TitulosSalida.Count;
        lblHistoriaTitulo.Text = string.Format(Textos.Instancia.CatalogoHistoriaPregunta, v.Ep.Num, n);
        panelHistorias.Children.Clear();

        Button Boton(string texto, Action accion)
        {
            var b = new Button
            {
                Content = texto,
                Style = (Style)FindResource("BtnSecondary"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 12.5,
            };
            b.Click += (_, _) => accion();
            return b;
        }

        // El caso más común, destacado: el episodio entero.
        panelHistorias.Children.Add(Boton(Textos.Instancia.CatalogoHistoriaCompleto, () => Terminar(v.Ep, null)));

        // O SOLO ALGUNAS: un checkbox por historia. Se pueden marcar varias (p. ej. la a y la c
        // de tres), no solo una. Las letras de las marcadas van pegadas al número (E413ac).
        panelHistorias.Children.Add(new TextBlock
        {
            Text = Textos.Instancia.CatalogoHistoriaMarcar,
            FontSize = 11.5, Foreground = (Brush)FindResource("Neutral500"),
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
                Foreground = (Brush)FindResource("Text"),
                FontSize = 12.5,
                Margin = new Thickness(2, 0, 0, 7),
            };
            casillas.Add(chk);
            panelHistorias.Children.Add(chk);
        }

        var aceptar = Boton(Textos.Instancia.CatalogoHistoriaUsar, () =>
        {
            var letras = new string(Enumerable.Range(0, casillas.Count)
                .Where(i => casillas[i].IsChecked == true)
                .Select(i => (char)('a' + i))
                .ToArray());
            if (letras.Length == 0) return;                          // nada marcado: no hace nada
            Terminar(v.Ep, letras.Length == n ? null : letras);      // todas marcadas = el completo
        });
        aceptar.Style = (Style)FindResource("BtnPrimary");
        panelHistorias.Children.Add(aceptar);

        var cancelar = Boton(Textos.Instancia.Cancelar, () => overlayHistoria.Visibility = Visibility.Collapsed);
        cancelar.Style = (Style)FindResource("BtnGhostMuted");
        panelHistorias.Children.Add(cancelar);

        overlayHistoria.Visibility = Visibility.Visible;
    }

    private void Terminar(CatalogEpisode ep, string? seg)
    {
        Elegido = ep;
        SegElegido = seg;
        DialogResult = true;
    }

    /// <summary>
    /// Enseña el fichero en el explorador de Windows, seleccionado. Es lo que
    /// convierte el distintivo en algo que sirve: saber que lo tienes sin saber
    /// dónde deja el trabajo a medias.
    /// </summary>
    private void OnIrAlFichero(object sender, RoutedEventArgs e)
    {
        // No se propaga a la fila: pulsar el distintivo es una acción suya, y en
        // modo elegir la fila responde al doble clic escogiendo el episodio.
        e.Handled = true;

        if (sender is not Button { Tag: string ruta } || string.IsNullOrEmpty(ruta)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "explorer.exe", $"/select,\"{ruta}\"") { UseShellExecute = true });
        }
        catch { /* si el explorador no arranca, no hay nada que decir aquí */ }
    }

    private void Refrescar()
    {
        var encontrados = CatalogSearch.Filtrar(_cat, txtBuscar.Text);

        // El filtro se aplica DESPUÉS del buscador: los dos acotan, y así se puede
        // buscar «playa» y quedarse solo con las que faltan.
        if (chkSoloFaltan.IsChecked == true)
            encontrados = encontrados
                .Where(e => !_tenencia.TryGetValue(e.Num, out var t)
                            || t.Que != CoberturaCatalogo.Tengo.Entero)
                .ToList();

        lista.ItemsSource = encontrados.Select(e => new EpisodioVista
        {
            Ep = e,
            Que = _tenencia.Count == 0 ? null
                : _tenencia.TryGetValue(e.Num, out var t) ? t
                : new CoberturaCatalogo.Tenencia(CoberturaCatalogo.Tengo.Nada, Array.Empty<string>()),
        }).ToList();

        lblCuenta.Text = chkSoloFaltan.IsChecked == true
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
