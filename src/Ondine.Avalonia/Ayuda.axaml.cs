using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ondine.Ava;

/// <summary>
/// La Ayuda, portada de <c>AyudaWindow</c>: índice de tutoriales a la izquierda y su
/// contenido a la derecha. Sin lógica ninguna — solo explica cómo funcionan Organizar,
/// Comprimir y Recortes.
///
/// <para>
/// Es la pantalla con menos código del puerto y, aun así, la que más cuidado pide: su
/// contenido es el contrato con el usuario. Si al traducir el XAML se cae un párrafo,
/// <b>nadie lo nota</b> — la ventana sigue abriendo igual, solo que explica menos. De eso
/// se encarga la comprobación, que cuenta los párrafos de las dos versiones y las compara.
/// </para>
/// <para>
/// El único cambio de código: en WPF cada entrada del índice se enganchaba con
/// <c>Checked</c>, que existe también aquí, pero Avalonia lo unifica en
/// <c>IsCheckedChanged</c> — y ese salta también al DESmarcarse, así que hay que preguntar
/// si el que avisa es el que ha quedado elegido.
/// </para>
/// </summary>
public partial class Ayuda : Window
{
    public Ayuda()
    {
        AvaloniaXamlLoader.Load(this);

        this.FindControl<Button>("btnCerrar")!.Click += (_, _) => Close();

        Atar("navOrgComo", "pagOrgComo");
        Atar("navOrgPasos", "pagOrgPasos");
        Atar("navComprimir", "pagComprimir");
        Atar("navRecortes", "pagRecortes");
    }

    private static readonly string[] Paginas =
        ["pagOrgComo", "pagOrgPasos", "pagComprimir", "pagRecortes"];

    private void Atar(string boton, string pagina)
    {
        var b = this.FindControl<RadioButton>(boton)!;
        b.IsCheckedChanged += (_, _) =>
        {
            // IsCheckedChanged salta también al desmarcarse, y el grupo desmarca la
            // anterior justo antes de marcar la nueva: sin esta pregunta, la pantalla
            // parpadearía a la página de quien se acaba de apagar.
            if (b.IsChecked == true) Mostrar(pagina);
        };
    }

    private void Mostrar(string pagina)
    {
        foreach (var p in Paginas)
            this.FindControl<StackPanel>(p)!.IsVisible = p == pagina;
    }
}
