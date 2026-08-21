using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ondine.Ava;

/// <summary>
/// Una caja de texto con un icono a la izquierda, portada de <c>CampoTexto</c>.
///
/// <para>
/// Sigue siendo un <c>TextBox</c> de verdad y no un compuesto: así hereda el tema de los
/// campos, el foco, la selección y el teclado sin que haya que reproducir nada.
/// </para>
/// <para>
/// Lo que cambia: en WPF una propiedad nueva se declara con
/// <c>DependencyProperty.Register</c> y en Avalonia con <c>StyledProperty</c>. Es la misma
/// idea con otro nombre — y hay que registrarla en el tipo, no basta con la propiedad de C#,
/// porque si no el enlace del XAML no la encuentra y <b>no da error: simplemente no pinta el
/// icono</b>.
/// </para>
/// </summary>
public class CampoTexto : TextBox
{
    /// <summary>
    /// Icono de la izquierda. Sin él, el hueco no se reserva: un campo sin icono queda
    /// alineado como cualquier otro campo normal.
    /// </summary>
    public static readonly StyledProperty<Geometry?> IconoProperty =
        AvaloniaProperty.Register<CampoTexto, Geometry?>(nameof(Icono));

    public Geometry? Icono
    {
        get => GetValue(IconoProperty);
        set => SetValue(IconoProperty, value);
    }
}
