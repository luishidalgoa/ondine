using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Ondine.Localizacion;

namespace Ondine.Ava;

/// <summary>Una fila de la vista previa: nombre original → nombre resultante.</summary>
public sealed class RenamePreviewRow
{
    public string Original { get; init; } = "";
    public string Nuevo { get; init; } = "";
    public IBrush Color { get; init; } = Brushes.Gray;
}

/// <summary>
/// «Renombrar», portado de <c>RenameWindow</c>. Al estilo PowerRename, con vista previa en
/// vivo: la regla se aplica al nombre del fichero de salida de cada vídeo al procesarlo.
///
/// <para>
/// Es la primera pantalla del puerto que trae piezas nuevas de verdad, y son dos.
/// </para>
/// <list type="number">
/// <item>
/// <b>El desplegable de sugerencias.</b> El <c>Popup</c> existe igual, pero lo que decide
/// qué se ofrece y dónde cae lo elegido ya no está aquí: bajó al motor en su propio cambio,
/// con pruebas. Lo que queda —<see cref="CajaDeSugerencias"/>— es cableado.
/// </item>
/// <item>
/// <b>La vista previa.</b> En WPF era un <c>ListView</c> con <c>GridView</c>; aquí es un
/// <c>DataGrid</c>, que es <b>otro control</b>. Por eso los temas <c>TableView</c> y
/// <c>ColHeader</c> del tema de WPF no se portan: no visten nada que exista en Avalonia.
/// </item>
/// </list>
/// <para>
/// Y una diferencia de fondo: WPF tenía <c>DialogResult</c>, una propiedad que cerraba la
/// ventana al asignarla. En Avalonia el modal devuelve un valor al cerrarse
/// —<c>Close(algo)</c>—, así que quien abre esta ventana recoge la regla ahí y no leyendo
/// una propiedad después. <see cref="Result"/> se conserva por comodidad, pero el valor
/// bueno es el que devuelve <c>ShowDialog</c>.
/// </para>
/// </summary>
public partial class Renombrar : Window
{
    private readonly IReadOnlyList<(string name, DateTime created)> _items = [];
    private readonly ObservableCollection<RenamePreviewRow> _preview = [];
    private readonly List<string> _histSearch = [], _histReplace = [];
    private bool _cargada;

    /// <summary>La regla resultante tras pulsar Aplicar/Quitar (null si se cancela).</summary>
    public RenameRule? Result { get; private set; }

    public Renombrar() => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

    public Renombrar(RenameRule current, IReadOnlyList<(string name, DateTime created)> items,
                     List<string> historySearch, List<string> historyReplace) : this()
    {
        _items = items;
        _histSearch = historySearch;
        _histReplace = historyReplace;
        Grid("lstPrev").ItemsSource = _preview;

        // La barra de título es nuestra, así que arrastrar también.
        this.FindControl<Grid>("header")!.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };

        Btn("btnX").Click += (_, _) => Close(null);
        Btn("btnCancel").Click += (_, _) => Close(null);
        Btn("btnApply").Click += (_, _) =>
        {
            Recordar(_histSearch, Txt("txtSearch").Text);
            Recordar(_histReplace, Txt("txtReplace").Text);
            Result = Montar(enabled: true);
            Close(Result);
        };
        Btn("btnClear").Click += (_, _) => { Result = new RenameRule(); Close(Result); };

        // Los desplegables se llenan por código: en WPF eran <ComboBoxItem> con {i:T}
        // dentro del XAML, y aquí una lista de textos es más corta y dice lo mismo.
        var t = Textos.Instancia;
        Cbo("cboTarget").ItemsSource = new List<string>
        { t.RenameSoloNombre, t.RenameSoloExtension, t.RenameNombreYExtension };
        Cbo("cboCase").ItemsSource = new List<string>
        {
            t.RenameFormatoNinguno, t.RenameFormatoMinusculas, t.RenameFormatoMayusculas,
            t.RenameFormatoPrimeraLetra, t.RenameFormatoCadaPalabra,
        };

        // autocompletado reactivo bajo cada campo
        _ = new CajaDeSugerencias(Txt("txtSearch"), Pop("popSearch"), Lst("lstSearch"),
                                  Suggestions.Search, () => _histSearch, AutoActivar);
        _ = new CajaDeSugerencias(Txt("txtReplace"), Pop("popReplace"), Lst("lstReplace"),
                                  Suggestions.Replace, () => _histReplace, AutoActivar);

        // cargar la regla actual
        Txt("txtSearch").Text = current.Search;
        Txt("txtReplace").Text = current.Replace;
        Chk("chkRegex").IsChecked = current.UseRegex;
        Chk("chkCase").IsChecked = current.CaseSensitive;
        Chk("chkAll").IsChecked = current.MatchAll;
        Chk("chkEnum").IsChecked = current.Enumerate;
        Chk("chkRand").IsChecked = current.RandomStrings;
        Cbo("cboTarget").SelectedIndex = (int)current.Target;
        Cbo("cboCase").SelectedIndex = (int)current.Case;

        // vista previa en vivo
        Txt("txtSearch").TextChanged += (_, _) => Refrescar();
        Txt("txtReplace").TextChanged += (_, _) => Refrescar();
        foreach (var n in new[] { "chkRegex", "chkCase", "chkAll", "chkEnum", "chkRand" })
            Chk(n).IsCheckedChanged += (_, _) => Refrescar();
        Cbo("cboTarget").SelectionChanged += (_, _) => Refrescar();
        Cbo("cboCase").SelectionChanged += (_, _) => Refrescar();

        _cargada = true;
        Refrescar();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private TextBox Txt(string n) => this.FindControl<TextBox>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private ComboBox Cbo(string n) => this.FindControl<ComboBox>(n)!;
    private ListBox Lst(string n) => this.FindControl<ListBox>(n)!;
    private Popup Pop(string n) => this.FindControl<Popup>(n)!;
    private DataGrid Grid(string n) => this.FindControl<DataGrid>(n)!;

    /// <summary>Al elegir una sugerencia, activa sola la opción que esa sugerencia necesita.</summary>
    private void AutoActivar(SuggestionItem it)
    {
        switch (it.Enables)
        {
            case "regex": Chk("chkRegex").IsChecked = true; break;
            case "enum": Chk("chkEnum").IsChecked = true; break;
            case "rand": Chk("chkRand").IsChecked = true; break;
        }
    }

    /// <summary>Guarda un valor en el historial (el más reciente primero, sin duplicados, máx. 10).</summary>
    private static void Recordar(List<string> hist, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        hist.RemoveAll(h => string.Equals(h, value, StringComparison.Ordinal));
        hist.Insert(0, value);
        if (hist.Count > 10) hist.RemoveRange(10, hist.Count - 10);
    }

    private RenameRule Montar(bool enabled) => new()
    {
        Enabled = enabled,
        Search = Txt("txtSearch").Text ?? "",
        Replace = Txt("txtReplace").Text ?? "",
        UseRegex = Chk("chkRegex").IsChecked == true,
        CaseSensitive = Chk("chkCase").IsChecked == true,
        MatchAll = Chk("chkAll").IsChecked == true,
        Enumerate = Chk("chkEnum").IsChecked == true,
        RandomStrings = Chk("chkRand").IsChecked == true,
        Target = (RenameTarget)Math.Max(0, Cbo("cboTarget").SelectedIndex),
        Case = (TextCase)Math.Max(0, Cbo("cboCase").SelectedIndex),
    };

    private void Refrescar()
    {
        if (!_cargada) return;
        var regla = Montar(enabled: true);
        Lbl("lblExtWarn").IsVisible = regla.Target != RenameTarget.NameOnly;

        var cambia = Pincel("Accent300", Brushes.White);
        var igual = Pincel("Neutral700", Brushes.Gray);

        _preview.Clear();
        int contador = 0, n = 0;
        foreach (var (name, created) in _items)
        {
            // ${rstring…}/${ruuidv4} cambian en cada evaluación: la vista previa es orientativa
            var nuevo = regla.Apply(name, contador, created);
            if (nuevo != null) { contador++; n++; }
            _preview.Add(new RenamePreviewRow
            {
                Original = name,
                Nuevo = nuevo ?? Textos.Instancia.RenameSinCambio,
                Color = nuevo != null ? cambia : igual,
            });
        }

        // el castellano concuerda el verbo con el primer número, así que el
        // singular tiene su propia frase
        var plantilla = n == 1 ? Textos.Instancia.RenameCuentaUno : Textos.Instancia.RenameCuenta;
        Lbl("lblCount").Text = _items.Count == 0
            ? Textos.Instancia.RenameSinVideos
            : string.Format(CultureInfo.CurrentCulture, plantilla, n, _items.Count);
    }

    private IBrush Pincel(string clave, IBrush siNoEsta) =>
        this.TryFindResource(clave, out var v) && v is IBrush b ? b : siNoEsta;
}
