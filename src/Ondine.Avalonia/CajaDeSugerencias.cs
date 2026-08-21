using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Visual = Avalonia.Visual;
using Avalonia.Interactivity;

namespace Ondine.Ava;

/// <summary>
/// El autocompletado que cuelga de un campo de texto, portado de <c>SuggestionBox</c>.
///
/// <para>
/// <b>Aquí ya no se calcula nada.</b> Qué trozo se está escribiendo, qué se ofrece y dónde
/// cae lo elegido lo dice <see cref="Sugerencias"/>, que vive en el motor y tiene sus
/// pruebas. Lo que queda es el cableado: abrir, cerrar, moverse con las flechas y aceptar.
/// </para>
/// <para>
/// Lo que cambia respecto a WPF, y es lo que cuesta:
/// </para>
/// <list type="bullet">
/// <item><c>GotKeyboardFocus</c>/<c>LostKeyboardFocus</c> pasan a <c>GotFocus</c>/<c>LostFocus</c>.</item>
/// <item>
/// Los eventos «Preview» no existen. En Avalonia se pide la fase de túnel con
/// <c>AddHandler(..., RoutingStrategies.Tunnel)</c>, que es lo que hace falta para robarle
/// el Enter y las flechas al campo antes de que las use él.
/// </item>
/// <item>
/// <c>IsMouseOver</c> es <c>IsPointerOver</c>, y el <c>Popup</c> de Avalonia se ancla con
/// <c>PlacementTarget</c> igual, pero no se cierra solo al perder el foco: se cierra a mano.
/// </item>
/// </list>
/// </summary>
internal sealed class CajaDeSugerencias
{
    private readonly TextBox _campo;
    private readonly Popup _pop;
    private readonly ListBox _lista;
    private readonly IReadOnlyList<SuggestionItem> _catalogo;
    private readonly Func<IEnumerable<string>> _historial;
    private readonly Action<SuggestionItem>? _alAceptar;
    private bool _callado;

    public CajaDeSugerencias(TextBox campo, Popup pop, ListBox lista,
                             IReadOnlyList<SuggestionItem> catalogo,
                             Func<IEnumerable<string>> historial,
                             Action<SuggestionItem>? alAceptar = null)
    {
        _campo = campo; _pop = pop; _lista = lista;
        _catalogo = catalogo; _historial = historial; _alAceptar = alAceptar;

        _campo.GotFocus += (_, _) => Ensenar();
        _campo.PointerReleased += (_, _) => Ensenar();
        _campo.TextChanged += (_, _) => { if (!_callado) Ensenar(); };

        // Al salir del campo se cierra, salvo que el ratón esté encima del propio
        // desplegable: si no, elegir con el ratón lo cerraría antes de llegar a pulsar.
        _campo.LostFocus += (_, _) => { if (!_pop.IsPointerOver) Esconder(); };

        // En túnel, no en burbuja: hay que quedarse con el Enter y las flechas ANTES de
        // que el campo de texto los use. Es el sustituto de los «Preview…» de WPF.
        _campo.AddHandler(InputElement.KeyDownEvent, AlPulsarTecla, RoutingStrategies.Tunnel);

        // Clic en un elemento: se acepta antes de que el foco se mueva.
        _lista.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (DebajoDelRaton(e.Source as Visual) is { } it) { Aceptar(it); e.Handled = true; }
        }, RoutingStrategies.Tunnel);
    }

    private static SuggestionItem? DebajoDelRaton(Visual? v)
    {
        while (v is not null)
        {
            if (v is ListBoxItem { DataContext: SuggestionItem it }) return it;
            v = v.GetVisualParent();
        }
        return null;
    }

    private void AlPulsarTecla(object? _, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _pop.IsOpen) { Esconder(); e.Handled = true; return; }

        if (e.Key == Key.Down)
        {
            if (!_pop.IsOpen) { Ensenar(); e.Handled = true; return; }
            Mover(1); e.Handled = true; return;
        }
        if (e.Key == Key.Up && _pop.IsOpen) { Mover(-1); e.Handled = true; return; }

        if ((e.Key == Key.Enter || e.Key == Key.Tab) && _pop.IsOpen
            && _lista.SelectedItem is SuggestionItem it)
        {
            Aceptar(it); e.Handled = true;
        }
    }

    private void Mover(int delta)
    {
        if (_lista.ItemCount == 0) return;
        _lista.SelectedIndex = Math.Clamp(_lista.SelectedIndex + delta, 0, _lista.ItemCount - 1);
        _lista.ScrollIntoView(_lista.SelectedIndex);
    }

    private void Ensenar()
    {
        var (_, trozo) = Sugerencias.Trozo(_campo.Text, _campo.CaretIndex);
        var items = Sugerencias.Filtrar(_catalogo, _historial(), trozo);
        _lista.ItemsSource = items;
        if (items.Count == 0) { Esconder(); return; }
        _lista.SelectedIndex = 0;
        _pop.IsOpen = true;
    }

    private void Esconder() => _pop.IsOpen = false;

    private void Aceptar(SuggestionItem it)
    {
        // El corte y el pegado los hace el motor: es aritmética de índices y se prueba
        // sin ventana. Aquí solo queda mover el texto y el cursor a lo que diga.
        var (texto, cursor) = Sugerencias.Insertar(_campo.Text, _campo.CaretIndex, it.Text);
        _callado = true;
        _campo.Text = texto;
        _campo.CaretIndex = cursor;
        _callado = false;
        Esconder();
        _campo.Focus();
        _alAceptar?.Invoke(it);
    }
}
