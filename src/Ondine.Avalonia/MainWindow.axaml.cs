using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ondine.Ava;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        // Se le pregunta al motor de verdad, no se pinta un texto fijo: lo que esta
        // ventana viene a demostrar es que el motor esta enlazado y responde desde aqui.
        var donde = Ondine.DatosDeUsuario.Raiz;
        this.FindControl<TextBlock>("rotuloMotor")!.Text = donde;

        // Con --auto la ventana se abre, se comprueba sola y se cierra. Es la unica forma
        // honesta: en Avalonia un selector que no casa NO da error, asi que compilar no
        // dice nada sobre si el tema se aplico.
        if (Environment.GetCommandLineArgs().Contains("--auto"))
            Opened += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    Comprobacion.Correr(this);
                    Comprobacion.CorrerCampos(this);
                    await Comprobacion.CorrerDialogo(this);
                    await Comprobacion.CorrerFaltantes(this);
                    await Comprobacion.CorrerPistas(this);
                    await Comprobacion.CorrerReordenar(this);
                    await Comprobacion.CorrerRenombrar(this);
                    await Comprobacion.CorrerEncargo(this);
                    await Comprobacion.CorrerAyuda(this);
                    await Comprobacion.CorrerPreferencias(this);
                    await Comprobacion.CorrerCatalogo(this);
                    await Comprobacion.CorrerReproductor(this);
                }
                catch (Exception ex) { Comprobacion.Resultados.Add($"REVENTO: {ex.Message}"); }
                Close();
            }, Avalonia.Threading.DispatcherPriority.Background);
    }
}
