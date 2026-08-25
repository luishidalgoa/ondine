using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Ondine.Ava;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Qué ventana se abre.
    ///
    /// <para>
    /// Con el puerto terminado, <b>la aplicación abre la ventana de verdad</b>. Hasta ahora
    /// abría el esqueleto de comprobación, que era lo único que había.
    /// </para>
    /// <para>
    /// Ese esqueleto se queda, y no por nostalgia: es el que corre los autochequeos con
    /// <c>--auto</c>. Abre las pantallas una por una, mira lo que quedó pintado y se cierra.
    /// Hace falta porque en Avalonia <b>un selector que no casa no da error</b> y un enlace
    /// roto tampoco: compilar no dice nada sobre si el tema se aplicó. La comprobación no va
    /// dentro de la ventana real para no hacerle cargar con andamios que solo usa ella.
    /// </para>
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime escritorio)
            escritorio.MainWindow = Environment.GetCommandLineArgs().Contains("--auto")
                ? new MainWindow()
                : new VentanaPrincipal();

        base.OnFrameworkInitializationCompleted();
    }
}
