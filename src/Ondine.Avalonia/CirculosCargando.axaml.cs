using System.Threading;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Ondine.Ava;

/// <summary>
/// La espera, en cuatro círculos que laten en cascada. Portado de <c>CirculosCargando</c>.
///
/// <para>
/// Cada círculo hace dos cosas: el aro crece y se atenúa, y el punto de dentro se encoge
/// hasta desaparecer y vuelve. Cada uno va 0,3 s por detrás del anterior, así que el
/// conjunto se lee como algo que <b>recorre</b> la fila en vez de cuatro cosas parpadeando a
/// la vez. Solo se anima escala y opacidad: ni un efecto ni un repintado — una animación de
/// espera es justo la que no puede robarle sitio al trabajo por el que se está esperando.
/// </para>
/// <para>
/// <b>Lo que cambia al portar es de dónde viene el fallo que hubo.</b> En WPF esto montaba
/// <c>Storyboard</c>s a mano y había que guardar la lista de pistas para poder pararlas; una
/// que se escapó costó <b>un 11% de un núcleo para siempre</b> tras cerrar el reproductor,
/// porque un reloj de animación vivo mantiene su objeto en pie y obliga a pintar en cada
/// fotograma aunque no se vea nada. Aquí las animaciones se lanzan con un testigo de
/// cancelación que se corta al salir del árbol — <b>una línea, y no hay lista que
/// mantener</b>. El fallo no es que se arregle: es que deja de tener dónde esconderse.
/// </para>
/// </summary>
public partial class CirculosCargando : UserControl
{
    private const int Cuantos = 4;
    private static readonly TimeSpan Ciclo = TimeSpan.FromSeconds(2);
    /// <summary>Lo que va cada círculo por detrás del anterior. Sin esto, los cuatro laten al unísono.</summary>
    private static readonly TimeSpan Escalon = TimeSpan.FromSeconds(0.3);

    private readonly CancellationTokenSource _corte = new();

    public CirculosCargando()
    {
        AvaloniaXamlLoader.Load(this);

        var fila = this.FindControl<StackPanel>("fila")!;
        for (int i = 0; i < Cuantos; i++) fila.Children.Add(Unidad(i));

        DetachedFromVisualTree += (_, _) => _corte.Cancel();
    }

    /// <summary>Lo que se está esperando, dicho debajo de los círculos.</summary>
    public string Texto
    {
        get => this.FindControl<TextBlock>("lblTexto")!.Text ?? "";
        set => this.FindControl<TextBlock>("lblTexto")!.Text = value;
    }

    private Control Unidad(int i)
    {
        var retraso = Escalon * i;

        var aro = new Ellipse
        {
            Width = 20, Height = 20,
            Stroke = Pincel("Accent700"), StrokeThickness = 1.4,
            RenderTransform = new ScaleTransform(1, 1),
        };
        var punto = new Ellipse
        {
            Width = 9, Height = 9,
            Fill = Pincel("Accent"),
            RenderTransform = new ScaleTransform(1, 1),
        };

        // El latido se define UNA vez y el retraso es un parámetro. La alternativa era
        // cuatro bloques casi idénticos en el XAML, y entonces cambiar el ritmo obligaría a
        // tocarlo en ocho sitios.
        _ = Latido(0.85, 0.15, 1, 1.35, retraso).RunAsync(aro, _corte.Token);
        _ = Latido(1, 1, 1, 0.1, retraso).RunAsync(punto, _corte.Token);

        return new Panel { Width = 22, Height = 22, Children = { aro, punto } };
    }

    /// <summary>
    /// Un ciclo: del reposo al punto alto y vuelta. El punto alto cae al 35 % y no a la
    /// mitad, para que la subida sea más rápida que la bajada — es lo que lo hace parecer un
    /// pulso y no un balanceo.
    /// </summary>
    private static Animation Latido(double op0, double op1, double esc0, double esc1, TimeSpan retraso) => new()
    {
        Duration = Ciclo,
        Delay = retraso,
        IterationCount = IterationCount.Infinite,
        Easing = new SineEaseInOut(),
        Children =
        {
            Fotograma(0d, op0, esc0),
            Fotograma(0.35d, op1, esc1),
            Fotograma(1d, op0, esc0),
        },
    };

    private static KeyFrame Fotograma(double cue, double opacidad, double escala) => new()
    {
        Cue = new Cue(cue),
        Setters =
        {
            new Setter(OpacityProperty, opacidad),
            new Setter(ScaleTransform.ScaleXProperty, escala),
            new Setter(ScaleTransform.ScaleYProperty, escala),
        },
    };

    private static IBrush Pincel(string clave) =>
        Avalonia.Application.Current is { } app && app.TryFindResource(clave, out var v) && v is IBrush b
            ? b : Brushes.Gray;
}
