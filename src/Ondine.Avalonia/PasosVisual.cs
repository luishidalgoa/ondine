using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;

namespace Ondine.Ava;

/// <summary>
/// El progreso de la identificación como un recorrido de pasos, en vertical: cada etapa con
/// su círculo y su rótulo, unidas por una línea de puntos.
///
/// <para>
/// El gesto que lo distingue es el <b>paso de testigo</b>: al completarse una etapa, su
/// círculo se abulta, la línea que baja a la siguiente se tiñe de arriba abajo, y el círculo
/// de destino se abulta al recibirlo. Al terminar todas, la lista se apaga y aparece un único
/// check grande con un halo verde que se enciende y se va.
/// </para>
/// <para>
/// <b>Portado de <c>PasosVisual</c>, y la mecánica cambia entera.</b> En WPF cada figura
/// necesitaba SU instancia de transformación y de trazo —un <c>Freezable</c> compartido se
/// congela, y animarlo revienta al primer fotograma— y las animaciones se lanzaban con
/// <c>BeginAnimation</c>, que hay que acordarse de parar. Aquí se usan <b>transiciones</b>:
/// se declaran una vez por figura y el propio control interpola cada cambio de valor. No hay
/// nada que arrancar, nada que parar, y nada que se pueda quedar vivo — que es el fallo que
/// costó un 11 % de núcleo en el reproductor.
/// </para>
/// <para>
/// Lo único que sigue igual: solo se animan opacidad, escala y desplazamiento. Ni un efecto.
/// Una animación de espera no puede robarle sitio al trabajo por el que se está esperando.
/// </para>
/// </summary>
public sealed class PasosVisual
{
    // ── medidas ──
    private const double Diametro = 24;      // círculo de cada paso
    private const double Trazo = 2;
    private const double LargoConector = 22; // el tramo de puntos entre dos círculos
    private const double ViajeMs = 340;      // lo que tarda el testigo en bajar

    /// <summary>Perímetro en unidades de trazo: es lo que entiende StrokeDashArray.</summary>
    private static readonly double Vuelta = Math.PI * (Diametro - Trazo) / Trazo;

    public Control Raiz { get; }

    private readonly Paso[] _pasos;
    private readonly Conector[] _conectores;
    private readonly Grid _lista;
    private readonly StackPanel _final;
    private readonly TextBlock _finTitulo;
    private readonly TextBlock _finDetalle;
    private readonly Ellipse _resplandor;
    private readonly Grid _icono;

    private static IBrush Rec(string clave) =>
        Avalonia.Application.Current is { } app && app.TryFindResource(clave, out var v) && v is IBrush b
            ? b : Brushes.Gray;

    public PasosVisual(params string[] rotulos)
    {
        _pasos = new Paso[rotulos.Length];
        _conectores = new Conector[Math.Max(0, rotulos.Length - 1)];

        // Una fila por paso y, entre ellas, una fila corta para el tramo de puntos.
        _lista = new Grid();
        _lista.ColumnDefinitions.Add(new ColumnDefinition(Diametro, GridUnitType.Pixel));
        _lista.ColumnDefinitions.Add(new ColumnDefinition());

        int fila = 0;
        for (int i = 0; i < rotulos.Length; i++)
        {
            if (i > 0)
            {
                _lista.RowDefinitions.Add(new RowDefinition(LargoConector, GridUnitType.Pixel));
                _conectores[i - 1] = new Conector();
                Grid.SetRow(_conectores[i - 1].Raiz, fila);
                _lista.Children.Add(_conectores[i - 1].Raiz);
                fila++;
            }

            _lista.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _pasos[i] = new Paso(i + 1, rotulos[i]);
            Grid.SetRow(_pasos[i].Raiz, fila);
            Grid.SetRow(_pasos[i].Textos, fila);
            Grid.SetColumn(_pasos[i].Textos, 1);
            _lista.Children.Add(_pasos[i].Raiz);
            _lista.Children.Add(_pasos[i].Textos);
            fila++;
        }

        // El desenlace: un solo check grande con su halo, en el sitio de la lista. Aparece
        // cuando ella se apaga, así el cambio se lee como una fusión y no como un corte.
        _resplandor = new Ellipse { Fill = Rec("EstadoOk"), Opacity = 0 };
        _icono = new Grid
        {
            Width = 44,
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
            RenderTransform = new ScaleTransform(0.6, 0.6),
            RenderTransformOrigin = RelativePoint.Center,
            Transitions = [Transicion(Visual.RenderTransformProperty, 420, new ElasticEaseOut())],
        };
        _icono.Children.Add(_resplandor);
        _icono.Children.Add(new Ellipse
        {
            Width = 30, Height = 30, Fill = Rec("EstadoOk"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        _icono.Children.Add(new Path
        {
            Data = Geometry.Parse("M16,22.5 L20.5,27 L28.5,17.5"),
            Stroke = Brushes.White, StrokeThickness = 2.4,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round,
        });
        _resplandor.Transitions = [Transicion(Visual.OpacityProperty, 900)];

        _finTitulo = new TextBlock
        {
            FontSize = 13, FontWeight = FontWeight.Medium, Foreground = Rec("Text"),
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0),
        };
        _finDetalle = new TextBlock
        {
            FontSize = 11.5, Foreground = Rec("Neutral500"),
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0),
        };

        _final = new StackPanel
        {
            Opacity = 0,
            IsHitTestVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Transitions = [Transicion(Visual.OpacityProperty, 320)],
        };
        _final.Children.Add(_icono);
        _final.Children.Add(_finTitulo);
        _final.Children.Add(_finDetalle);

        _lista.Transitions = [Transicion(Visual.OpacityProperty, 260)];

        Raiz = new Panel { Children = { _lista, _final } };
    }

    /// <summary>
    /// Una transición: se declara una vez y el control interpola cada cambio de esa
    /// propiedad. Es lo que sustituye a los <c>BeginAnimation</c> de WPF, y la razón por la
    /// que aquí no hay nada que parar.
    /// </summary>
    private static ITransition Transicion(AvaloniaProperty prop, double ms, Easing? suavizado = null)
        => prop == Visual.RenderTransformProperty
            ? new TransformOperationsTransition
            {
                Property = prop,
                Duration = TimeSpan.FromMilliseconds(ms),
                Easing = suavizado ?? new CubicEaseInOut(),
            }
            : new DoubleTransition
            {
                Property = prop,
                Duration = TimeSpan.FromMilliseconds(ms),
                Easing = suavizado ?? new CubicEaseInOut(),
            };

    public void Reiniciar()
    {
        _lista.Opacity = 1;
        _lista.IsHitTestVisible = true;
        _final.Opacity = 0;
        _resplandor.Opacity = 0;
        _icono.RenderTransform = new ScaleTransform(0.6, 0.6);

        foreach (var p in _pasos) p.Pendiente();
        foreach (var c in _conectores) c.Apagar();
    }

    public void EnCurso(int i) => _pasos[i].EnCurso();

    public void Detalle(int i, string texto) => _pasos[i].Marcha(texto);

    /// <summary>
    /// Cierra una etapa y pasa el testigo a la siguiente: su círculo se abulta, la línea se
    /// tiñe de arriba abajo y la de destino se abulta al recibirlo.
    /// </summary>
    public void Hecha(int i, string? detalle = null)
    {
        _pasos[i].Hecha(detalle);

        if (i >= _conectores.Length) return;

        _conectores[i].Encender();
        // El abultamiento de la siguiente llega CUANDO llega el testigo, no a la vez: es lo
        // que hace que se lea como una consecuencia y no como dos cosas animándose juntas.
        Retrasar(ViajeMs, () => _pasos[i + 1].Recibe());
    }

    public void Terminado(string titulo, string? detalle = null)
    {
        _finTitulo.Text = titulo;
        _finDetalle.Text = detalle ?? "";
        _finDetalle.IsVisible = !string.IsNullOrWhiteSpace(detalle);

        _lista.IsHitTestVisible = false;
        _lista.Opacity = 0;

        // El check entra cuando la lista ya se ha ido: si se cruzaran, se leería como dos
        // cosas distintas en pantalla en vez de como una fusión.
        Retrasar(180, () =>
        {
            _final.Opacity = 1;
            _icono.RenderTransform = new ScaleTransform(1, 1);

            // El destello: sube y se va. Con transiciones se pide el valor alto y, cuando
            // llega, el bajo — no hay que declarar ningún ciclo.
            _resplandor.Opacity = 0.5;
            Retrasar(260, () => _resplandor.Opacity = 0);
        });
    }

    private static void Retrasar(double ms, Action accion)
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        t.Tick += (_, _) => { t.Stop(); accion(); };
        t.Start();
    }

    /// <summary>Una etapa: su círculo con el número, el arco que gira, y su rótulo.</summary>
    private sealed class Paso
    {
        public Grid Raiz { get; }
        public StackPanel Textos { get; }

        private readonly Ellipse _arco;
        private readonly Ellipse _anillo;
        private readonly TextBlock _num;
        private readonly Path _check;
        private readonly TextBlock _detalle;

        public Paso(int numero, string rotulo)
        {
            Raiz = new Grid
            {
                Width = Diametro, Height = Diametro,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = new ScaleTransform(1, 1),
                RenderTransformOrigin = RelativePoint.Center,
                Transitions = [Transicion(Visual.RenderTransformProperty, 260, new ElasticEaseOut())],
            };

            var pista = new Ellipse { Stroke = Rec("Neutral800"), StrokeThickness = Trazo };

            // Un arco de un cuarto de vuelta sobre la pista: es el que gira mientras trabaja.
            //
            // Gira con una animación y no con una transición, porque esto SÍ es un ciclo: da
            // vueltas mientras la etapa está en curso. Se lanza con un testigo de
            // cancelación que se corta al cerrarla, así que no puede quedarse viva.
            _arco = new Ellipse
            {
                Stroke = Rec("Accent"), StrokeThickness = Trazo,
                StrokeDashArray = [Vuelta * 0.28, Vuelta],
                StrokeLineCap = PenLineCap.Round,
                RenderTransform = new RotateTransform(0),
                RenderTransformOrigin = RelativePoint.Center,
                Opacity = 0,
                Transitions = [Transicion(Visual.OpacityProperty, 180)],
            };
            _anillo = new Ellipse
            {
                Stroke = Rec("EstadoOk"), StrokeThickness = Trazo, Opacity = 0,
                Transitions = [Transicion(Visual.OpacityProperty, 220)],
            };

            _num = new TextBlock
            {
                Text = numero.ToString(), FontSize = 11, Foreground = Rec("Neutral600"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Transitions = [Transicion(Visual.OpacityProperty, 200)],
            };
            _check = new Path
            {
                Data = Geometry.Parse("M7,12.5 L10.5,16 L17,8.5"),
                Stroke = Rec("EstadoOk"), StrokeThickness = 2,
                StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round,
                Opacity = 0,
                Transitions = [Transicion(Visual.OpacityProperty, 220)],
            };

            Raiz.Children.Add(pista);
            Raiz.Children.Add(_anillo);
            Raiz.Children.Add(_arco);
            Raiz.Children.Add(_num);
            Raiz.Children.Add(_check);

            var etiqueta = new TextBlock
            {
                Text = rotulo, FontSize = 12.5, Foreground = Rec("Neutral500"),
                TextWrapping = TextWrapping.Wrap,
            };
            _detalle = new TextBlock
            {
                FontSize = 11, Foreground = Rec("Neutral500"),
                IsVisible = false, Margin = new Thickness(0, 1, 0, 0),
            };
            Textos = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Textos.Children.Add(etiqueta);
            Textos.Children.Add(_detalle);
        }

        private CancellationTokenSource? _giro;

        public void Pendiente()
        {
            Pararlo();
            _arco.Opacity = 0;
            _anillo.Opacity = 0;
            _check.Opacity = 0;
            _num.Opacity = 1;
            _detalle.IsVisible = false;
            _detalle.Text = "";
            Raiz.RenderTransform = new ScaleTransform(1, 1);
        }

        public void Marcha(string texto)
        {
            _detalle.Text = texto;
            _detalle.IsVisible = !string.IsNullOrWhiteSpace(texto);
        }

        public void EnCurso()
        {
            Pararlo();
            _arco.Opacity = 1;
            _num.Opacity = 0.35;

            _giro = new CancellationTokenSource();
            _ = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(950),
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    Cuadro(0, 0),
                    Cuadro(1, 360),
                },
            }.RunAsync(_arco, _giro.Token);
        }

        public void Hecha(string? detalle)
        {
            Pararlo();
            _arco.Opacity = 0;
            _num.Opacity = 0;
            _anillo.Opacity = 1;
            _check.Opacity = 1;
            if (detalle is not null) Marcha(detalle);

            // El abultamiento del testigo: sube y vuelve. Dos cambios de valor sobre una
            // transición; en WPF eran tres fotogramas clave y un reloj.
            Raiz.RenderTransform = new ScaleTransform(1.22, 1.22);
            Retrasar(200, () => Raiz.RenderTransform = new ScaleTransform(1, 1));
        }

        /// <summary>Al recibir el testigo de la etapa anterior.</summary>
        public void Recibe()
        {
            Raiz.RenderTransform = new ScaleTransform(1.16, 1.16);
            Retrasar(190, () => Raiz.RenderTransform = new ScaleTransform(1, 1));
        }

        private void Pararlo()
        {
            _giro?.Cancel();
            _giro = null;
            _arco.RenderTransform = new RotateTransform(0);
        }

        private static KeyFrame Cuadro(double cue, double angulo) => new()
        {
            Cue = new Cue(cue),
            Setters = { new Avalonia.Styling.Setter(RotateTransform.AngleProperty, angulo) },
        };
    }

    /// <summary>El tramo de puntos entre dos etapas, que se tiñe al pasar el testigo.</summary>
    private sealed class Conector
    {
        public Control Raiz { get; }

        private readonly Rectangle _tenido;

        public Conector()
        {
            var puntos = new Rectangle
            {
                Width = Trazo, Height = LargoConector,
                Fill = Rec("Neutral800"), Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            // Lo teñido crece de arriba abajo. En WPF era un punto viajando por la línea y
            // dejándola pintada detrás; aquí es el propio tramo el que crece desde arriba,
            // que se lee igual y es una transición en vez de dos animaciones sincronizadas.
            _tenido = new Rectangle
            {
                Width = Trazo, Height = LargoConector,
                Fill = Rec("Accent"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = new ScaleTransform(1, 0),
                RenderTransformOrigin = RelativePoint.TopLeft,
                Transitions = [Transicion(Visual.RenderTransformProperty, ViajeMs)],
            };

            Raiz = new Panel
            {
                Width = Diametro,
                Children = { puntos, _tenido },
            };
        }

        public void Encender() => _tenido.RenderTransform = new ScaleTransform(1, 1);

        public void Apagar() => _tenido.RenderTransform = new ScaleTransform(1, 0);
    }
}
