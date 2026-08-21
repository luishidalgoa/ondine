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
    }
}
