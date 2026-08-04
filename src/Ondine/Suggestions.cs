using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ondine.Localizacion;

namespace Ondine;

/// <summary>Una entrada del desplegable de autocompletado.</summary>
public sealed class SuggestionItem
{
    public string Text { get; init; } = "";     // lo que se inserta
    public string Desc { get; init; } = "";     // explicación breve
    /// <summary>Opción que hay que activar para que esto funcione: "regex", "enum" o "rand".</summary>
    public string? Enables { get; init; }
}

/// <summary>
/// Catálogos de sugerencias para los campos Buscar y Reemplazar por.
///
/// <para>
/// Son propiedades y no campos <c>static readonly</c>: una lista construida una
/// sola vez se quedaría con las descripciones del idioma que hubiera al arrancar,
/// y al cambiar de idioma el desplegable seguiría en el anterior. Se rehacen en
/// cada acceso, que ocurre al abrir la ventana de renombrado y no en bucle.
/// </para>
/// </summary>
public static class Suggestions
{
    /// <summary>Patrones frecuentes para el campo «Buscar» (todos son regex).</summary>
    public static IReadOnlyList<SuggestionItem> Search
    {
        get
        {
            var t = Textos.Instancia;
            return new List<SuggestionItem>
            {
                new() { Text = "^",                          Desc = t.SugerenciaInicioNombre, Enables = "regex" },
                new() { Text = "$",                          Desc = t.SugerenciaFinNombre, Enables = "regex" },
                new() { Text = ".*",                         Desc = t.SugerenciaTodoElTexto, Enables = "regex" },
                new() { Text = "(.*)",                       Desc = t.SugerenciaCapturaNombre, Enables = "regex" },
                new() { Text = "^.*$",                       Desc = t.SugerenciaNombreCompleto, Enables = "regex" },
                new() { Text = @"\d+",                       Desc = t.SugerenciaDigitos, Enables = "regex" },
                new() { Text = @"\s+",                       Desc = t.SugerenciaEspacios, Enables = "regex" },
                new() { Text = "[._-]+",                     Desc = t.SugerenciaSeparadores, Enables = "regex" },
                new() { Text = @"\[.*?\]",                   Desc = t.SugerenciaCorchetes, Enables = "regex" },
                new() { Text = @"\(.*?\)",                   Desc = t.SugerenciaParentesis, Enables = "regex" },
                new() { Text = "^.{3}",                      Desc = t.SugerenciaTresPrimeros, Enables = "regex" },
                new() { Text = ".{3}$",                      Desc = t.SugerenciaTresUltimos, Enables = "regex" },
                new() { Text = "^foo",                       Desc = t.SugerenciaEmpiezaPor, Enables = "regex" },
                new() { Text = "bar$",                       Desc = t.SugerenciaTerminaEn, Enables = "regex" },
                new() { Text = "^foo.*bar$",                 Desc = t.SugerenciaEmpiezaYAcaba, Enables = "regex" },
                new() { Text = ".+?(?=bar)",                 Desc = t.SugerenciaAnteriorA, Enables = "regex" },
                new() { Text = @"foo[\s\S]*bar",             Desc = t.SugerenciaEntreDos, Enables = "regex" },
                new() { Text = @"(\d{2})-(\d{2})-(\d{4})",   Desc = t.SugerenciaFecha, Enables = "regex" },
                new() { Text = @"[Ss](\d{1,2})[Ee](\d{1,2})", Desc = t.SugerenciaTemporadaEpisodio, Enables = "regex" },
                new() { Text = "1080p|720p|480p|2160p|4K",   Desc = t.SugerenciaResolucion, Enables = "regex" },
                new() { Text = "x264|x265|HEVC|WEB-DL|BluRay", Desc = t.SugerenciaCodecFuente, Enables = "regex" },
            };
        }
    }

    /// <summary>Variables disponibles para el campo «Reemplazar por».</summary>
    public static IReadOnlyList<SuggestionItem> Replace
    {
        get
        {
            var t = Textos.Instancia;
            return new List<SuggestionItem>
            {
                // grupos de captura
                new() { Text = "$1", Desc = t.SugerenciaGrupo1, Enables = "regex" },
                new() { Text = "$2", Desc = t.SugerenciaGrupo2, Enables = "regex" },
                new() { Text = "$3", Desc = t.SugerenciaGrupo3, Enables = "regex" },
                new() { Text = "$$", Desc = t.SugerenciaDolarLiteral },
                // contadores
                new() { Text = "${}",                                  Desc = t.SugerenciaContadorSimple, Enables = "enum" },
                new() { Text = "${start=1}",                           Desc = t.SugerenciaContadorDesdeUno, Enables = "enum" },
                new() { Text = "${padding=2;start=1}",                 Desc = t.SugerenciaContadorDosCifras, Enables = "enum" },
                new() { Text = "${padding=3;start=1}",                 Desc = t.SugerenciaContadorTresCifras, Enables = "enum" },
                new() { Text = "${increment=2}",                       Desc = t.SugerenciaContadorDeDosEnDos, Enables = "enum" },
                new() { Text = "${padding=4;increment=2;start=10}",    Desc = t.SugerenciaContadorCombinado, Enables = "enum" },
                // fecha del archivo original
                new() { Text = "$YYYY", Desc = t.SugerenciaAnio4 },
                new() { Text = "$YY",   Desc = t.SugerenciaAnio2 },
                new() { Text = "$Y",    Desc = t.SugerenciaAnio1 },
                new() { Text = "$MMMM", Desc = t.SugerenciaMesNombre },
                new() { Text = "$MMM",  Desc = t.SugerenciaMesAbreviado },
                new() { Text = "$MM",   Desc = t.SugerenciaMesCero },
                new() { Text = "$M",    Desc = t.SugerenciaMesSinCero },
                new() { Text = "$DDDD", Desc = t.SugerenciaDiaSemana },
                new() { Text = "$DDD",  Desc = t.SugerenciaDiaSemanaAbreviado },
                new() { Text = "$DD",   Desc = t.SugerenciaDiaCero },
                new() { Text = "$D",    Desc = t.SugerenciaDiaSinCero },
                new() { Text = "$hh",   Desc = t.SugerenciaHoraCero },
                new() { Text = "$h",    Desc = t.SugerenciaHoraSinCero },
                new() { Text = "$mm",   Desc = t.SugerenciaMinutosCero },
                new() { Text = "$m",    Desc = t.SugerenciaMinutosSinCero },
                new() { Text = "$ss",   Desc = t.SugerenciaSegundosCero },
                new() { Text = "$s",    Desc = t.SugerenciaSegundosSinCero },
                new() { Text = "$fff",  Desc = t.SugerenciaMilisegundos3 },
                new() { Text = "$ff",   Desc = t.SugerenciaMilisegundos2 },
                new() { Text = "$f",    Desc = t.SugerenciaMilisegundos1 },
                // aleatorios
                new() { Text = "${rstringalnum=8}", Desc = t.SugerenciaAleatorioAlfanumerico, Enables = "rand" },
                new() { Text = "${rstringalpha=8}", Desc = t.SugerenciaAleatorioLetras, Enables = "rand" },
                new() { Text = "${rstringdigit=6}", Desc = t.SugerenciaAleatorioDigitos, Enables = "rand" },
                new() { Text = "${ruuidv4}",        Desc = t.SugerenciaUuid, Enables = "rand" },
            };
        }
    }
}

/// <summary>
/// Desplegable de autocompletado reactivo para un TextBox: se abre al enfocar/pulsar,
/// filtra según se escribe y permite elegir con ratón o teclado (↑ ↓ Entrar Esc).
/// </summary>
internal sealed class SuggestionBox
{
    private readonly TextBox _box;
    private readonly Popup _pop;
    private readonly ListBox _list;
    private readonly IReadOnlyList<SuggestionItem> _catalog;
    private readonly Func<IEnumerable<string>> _history;
    private readonly Action<SuggestionItem>? _onAccept;
    private bool _suppress;

    public SuggestionBox(TextBox box, Popup pop, ListBox list,
                         IReadOnlyList<SuggestionItem> catalog,
                         Func<IEnumerable<string>> history,
                         Action<SuggestionItem>? onAccept = null)
    {
        _box = box; _pop = pop; _list = list; _catalog = catalog; _history = history; _onAccept = onAccept;

        _box.GotKeyboardFocus += (_, _) => Show();
        _box.PreviewMouseLeftButtonUp += (_, _) => Show();
        _box.TextChanged += (_, _) => { if (!_suppress) Show(); };
        _box.LostKeyboardFocus += (_, _) => { if (!_pop.IsMouseOver) Hide(); };
        _box.PreviewKeyDown += OnKeyDown;

        // clic en un elemento: lo aceptamos antes de que el foco se mueva
        _list.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (ItemUnder(e.OriginalSource as DependencyObject) is { } it) { Accept(it); e.Handled = true; }
        };
    }

    private static SuggestionItem? ItemUnder(DependencyObject? d)
    {
        while (d != null)
        {
            if (d is ListBoxItem { DataContext: SuggestionItem it }) return it;
            d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        }
        return null;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _pop.IsOpen) { Hide(); e.Handled = true; return; }

        if (e.Key == Key.Down)
        {
            if (!_pop.IsOpen) { Show(); e.Handled = true; return; }
            Move(1); e.Handled = true; return;
        }
        if (e.Key == Key.Up && _pop.IsOpen) { Move(-1); e.Handled = true; return; }

        if ((e.Key == Key.Enter || e.Key == Key.Tab) && _pop.IsOpen && _list.SelectedItem is SuggestionItem it)
        {
            Accept(it); e.Handled = true;
        }
    }

    private void Move(int delta)
    {
        if (_list.Items.Count == 0) return;
        int i = _list.SelectedIndex + delta;
        _list.SelectedIndex = Math.Clamp(i, 0, _list.Items.Count - 1);
        _list.ScrollIntoView(_list.SelectedItem);
    }

    /// <summary>El "trozo" que se está escribiendo: desde el último $ si lo hay, o todo hasta el cursor.</summary>
    private (int start, string token) CurrentToken()
    {
        string t = _box.Text ?? "";
        int caret = Math.Clamp(_box.CaretIndex, 0, t.Length);
        if (caret > 0)
        {
            int dollar = t.LastIndexOf('$', caret - 1);
            if (dollar >= 0)
            {
                string seg = t[dollar..caret];
                if (!seg.Any(char.IsWhiteSpace)) return (dollar, seg);
            }
        }
        return (0, t[..caret]);
    }

    private void Show()
    {
        var (_, token) = CurrentToken();
        var items = Filter(_catalog, _history(), token);
        _list.ItemsSource = items;
        if (items.Count == 0) { Hide(); return; }
        _list.SelectedIndex = 0;
        _pop.IsOpen = true;
    }

    /// <summary>
    /// Filtra catálogo + historial según lo escrito. Si el token empieza por '$' se
    /// filtra por prefijo (estás escribiendo una variable); si no, por contenido en
    /// el texto o en la descripción. Vacío = todo. Lógica pura, testeable.
    /// </summary>
    internal static List<SuggestionItem> Filter(
        IReadOnlyList<SuggestionItem> catalog, IEnumerable<string> history, string token)
    {
        bool isVar = token.StartsWith('$');
        var hist = history.Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => new SuggestionItem { Text = h, Desc = Textos.Instancia.SugerenciaUsadoRecientemente });

        var all = hist.Concat(catalog);
        var filtered = token.Length == 0
            ? all
            : all.Where(i => isVar
                ? i.Text.StartsWith(token, StringComparison.OrdinalIgnoreCase)
                : i.Text.Contains(token, StringComparison.OrdinalIgnoreCase)
                  || i.Desc.Contains(token, StringComparison.OrdinalIgnoreCase));

        return filtered.Take(60).ToList();
    }

    private void Hide() => _pop.IsOpen = false;

    private void Accept(SuggestionItem it)
    {
        var (start, token) = CurrentToken();
        string t = _box.Text ?? "";
        _suppress = true;
        _box.Text = t.Remove(start, token.Length).Insert(start, it.Text);
        _box.CaretIndex = start + it.Text.Length;
        _suppress = false;
        Hide();
        _box.Focus();
        _onAccept?.Invoke(it);
    }
}
