using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ondine.Localizacion;

namespace Ondine;

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

    private void Show()
    {
        var (_, token) = Sugerencias.Trozo(_box.Text, _box.CaretIndex);
        var items = Sugerencias.Filtrar(_catalog, _history(), token);
        _list.ItemsSource = items;
        if (items.Count == 0) { Hide(); return; }
        _list.SelectedIndex = 0;
        _pop.IsOpen = true;
    }

    private void Hide() => _pop.IsOpen = false;

    private void Accept(SuggestionItem it)
    {
        // El corte y el pegado los hace el motor: es aritmética de índices y se prueba
        // sin ventana. Aquí solo queda mover el texto y el cursor a lo que diga.
        var (texto, cursor) = Sugerencias.Insertar(_box.Text, _box.CaretIndex, it.Text);
        _suppress = true;
        _box.Text = texto;
        _box.CaretIndex = cursor;
        _suppress = false;
        Hide();
        _box.Focus();
        _onAccept?.Invoke(it);
    }
}
