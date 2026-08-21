using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Ondine.Localizacion;

namespace Ondine.Ava;

/// <summary>
/// El diálogo de la app, portado de <c>DialogWindow</c>.
///
/// <para>
/// Se queda con lo mismo que aquel: modal, centrado en su ventana, Esc cancela, Intro
/// acepta, y el mensaje se puede seleccionar y copiar —que es lo primero que quieres hacer
/// cuando el aviso trae una ruta o el texto de un error—.
/// </para>
/// <para>
/// <b>La diferencia que hay que saber al portar pantallas: aquí es asíncrono.</b> En WPF,
/// <c>ShowDialog()</c> devuelve el resultado ahí mismo y quien pregunta sigue en la línea
/// siguiente. En Avalonia devuelve una tarea, así que <c>Confirmar</c> se espera con
/// <c>await</c>. No es un capricho de esta clase: es cómo funciona el framework, y obliga a
/// que todo método que pregunte algo pase a ser asíncrono. Es el coste de portar que menos
/// se ve venir.
/// </para>
/// <para>
/// Y necesita dueño. Avalonia no sabe mostrar un modal sin una ventana que lo posea, al
/// contrario que WPF, que se apañaba centrándolo en la pantalla.
/// </para>
/// </summary>
public partial class Dialogo : Window
{
    public Dialogo() => AvaloniaXamlLoader.Load(this);

    private Dialogo(string titulo, string mensaje, string aceptar, string? cancelar) : this()
    {
        this.FindControl<TextBlock>("lblTitulo")!.Text = titulo;
        this.FindControl<SelectableTextBlock>("lblMensaje")!.Text = mensaje;

        var si = this.FindControl<Button>("btnSi")!;
        var no = this.FindControl<Button>("btnNo")!;

        si.Content = aceptar;
        si.Click += (_, _) => Close(true);

        if (cancelar is not null)
        {
            no.Content = cancelar;
            no.IsVisible = true;
        }
        no.Click += (_, _) => Close(false);

        // Cerrar con Esc cuenta como «no», que es lo prudente cuando la acción toca
        // ficheros. Intro acepta.
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Close(false); e.Handled = true; }
            else if (e.Key == Key.Enter) { Close(true); e.Handled = true; }
        };

        Opened += (_, _) => si.Focus();
    }

    private static async Task<bool> Mostrar(Window dueno, string titulo, string mensaje,
                                            string aceptar, string? cancelar) =>
        await new Dialogo(titulo, mensaje, aceptar, cancelar).ShowDialog<bool>(dueno);

    // Los rótulos por defecto llegan como null y se resuelven aquí dentro: un valor por
    // defecto de parámetro tiene que ser constante en compilación, y un texto traducido no
    // lo es — depende del idioma en curso, que cambia en caliente.

    /// <summary>Informa de algo. Un solo botón.</summary>
    public static Task Aviso(Window dueno, string titulo, string mensaje, string? aceptar = null) =>
        Mostrar(dueno, titulo, mensaje, aceptar ?? Textos.Instancia.DialogoEntendido, null);

    /// <summary>
    /// Pide una confirmación. Devuelve true solo si se acepta: cerrar con Esc o con la X
    /// cuenta como «no».
    /// </summary>
    public static Task<bool> Confirmar(Window dueno, string titulo, string mensaje,
                                       string? aceptar = null, string? cancelar = null) =>
        Mostrar(dueno, titulo, mensaje,
                aceptar ?? Textos.Instancia.Si, cancelar ?? Textos.Instancia.No);
}
