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
        {
            if (escritorio.Args?.Contains("--auto") == true)
            {
                escritorio.MainWindow = new MainWindow();
            }
            else
            {
                var ventana = new VentanaPrincipal();
                escritorio.MainWindow = ventana;

                // LOS FICHEROS CON LOS QUE SE ABRE LA APLICACIÓN, que esto no leía.
                //
                // Y el paquete de Linux los PROMETE: su .desktop declara los tipos de vídeo
                // que Ondine abre, así que Ondine sale en «Abrir con» al pulsar el botón
                // derecho en Nemo. Se abría… y no pasaba nada: la ventana salía vacía y el
                // vídeo que habías elegido no estaba en ninguna parte. La versión de WPF sí
                // los leía; al portar se quedó fuera.
                //
                // En un Mac esto NO basta y conviene saberlo: macOS no pasa los ficheros por
                // la línea de órdenes, los manda como un evento del sistema una vez abierta
                // la aplicación. Así que ahí «Abrir con» seguirá sin traer el vídeo hasta que
                // se atienda ese evento. Queda dicho en vez de dado por hecho.
                var vienen = Ondine.Rutas.VideosQueLlegan.DeLosArgumentos(escritorio.Args ?? []);
                if (vienen.Count > 0)
                    ventana.Opened += (_, _) => ventana.AddFilesFromShell(vienen);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
