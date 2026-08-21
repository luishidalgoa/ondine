using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Threading.Tasks;

namespace Ondine.Ava;

/// <summary>
/// Que el tema portado se aplique de verdad.
///
/// <para>
/// Hace falta porque <b>un selector que no casa no da error</b>. Si
/// «^:pointerover /template/ Border#b» apunta a una parte que no existe, Avalonia no se
/// queja: el boton sale con el aspecto de fabrica y solo se nota mirandolo. Es el mismo
/// silencio que ya mordio en el spike con los bindings.
/// </para>
/// <para>
/// Compilar no prueba nada aqui. Por eso esto abre la ventana de verdad, busca las partes
/// que el ControlTemplate deberia haber creado, y mira si los valores son los nuestros o
/// los del tema Fluent que viene de serie.
/// </para>
/// </summary>
public static class Comprobacion
{
    public static readonly List<string> Resultados = [];

    private static void Dice(bool bien, string que) =>
        Resultados.Add($"{(bien ? "\u2713" : "\u2717")} {que}");

    public static void Correr(Window v)
    {
        // Solo los que llevan un Theme PUESTO por nosotros. La primera version contaba
        // todos los Button del arbol, y al anadir un ComboBox empezo a contar tambien el
        // que ese control trae dentro de su plantilla — que no usa el tema de Ondine ni
        // tiene por que. La prueba fallaba por un boton que no era suyo.
        var botones = v.GetVisualDescendants().OfType<Button>()
                       .Where(b => b.Theme is not null)
                       .ToList();

        Dice(botones.Count >= 5, $"los botones estan en el arbol visual: {botones.Count}");
        if (botones.Count == 0) return;

        // ── La plantilla es LA NUESTRA, no la de Fluent ───────────────────────────
        // Si el ControlTheme no se hubiera aplicado, estas dos partes no existirian:
        // el boton de Fluent no tiene ni «b» ni «haz».
        foreach (var (nombre, cuantos) in new[] { ("b", botones.Count), ("haz", botones.Count) })
        {
            var partes = botones
                .Select(b => b.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == nombre))
                .Count(x => x is not null);

            Dice(partes == cuantos,
                $"los {cuantos} botones tienen su parte «{nombre}» ({partes} encontradas) " +
                "— si faltara, el ControlTheme no se habria aplicado y no lo diria nadie");
        }

        // ── Y los valores son los nuestros ───────────────────────────────────────
        var primario = botones[0];
        var esAcento = primario.Foreground is ISolidColorBrush s &&
                       s.Color.ToString().EndsWith("968AE0", StringComparison.OrdinalIgnoreCase);
        Dice(esAcento, $"el primario pinta con el acento de Ondine ({primario.Foreground})");

        var caja = primario.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "b");
        Dice(caja is not null && caja.CornerRadius.TopLeft == 8,
            $"y con el radio de esquina de Ondine, no el de Fluent ({caja?.CornerRadius})");

        // ── El haz nace apagado ──────────────────────────────────────────────────
        // Encendido siempre serian tantas animaciones como botones haya en pantalla, y
        // eso se paga en CPU aunque no se vea. Ya costo medirlo en la version de WPF.
        var haz = primario.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "haz");
        Dice(haz is not null && haz.Opacity == 0,
            "el haz nace apagado: encendido siempre son tantas animaciones como botones");

        // ── El deshabilitado se atenua ───────────────────────────────────────────
        var apagado = botones.FirstOrDefault(b => !b.IsEnabled);
        var suCaja = apagado?.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "b");
        Dice(suCaja is not null && suCaja.Opacity < 0.6,
            $"un boton apagado se atenua ({suCaja?.Opacity}) — y se atenua la caja, no el haz");
    }

    /// <summary>
    /// Los campos: que lleven el tema de Ondine y no el de Fluent, y que sus estados
    /// reaccionen.
    ///
    /// <para>
    /// Van como estilos implicitos, asi que el riesgo aqui es el contrario que en los
    /// botones: no que el selector no case, sino que el ControlTheme <b>no se aplique a
    /// todos</b>. Un TextBox dentro de un popup o de una plantilla podria quedarse con el
    /// de serie, y eso solo se ve mirando.
    /// </para>
    /// </summary>
    public static void CorrerCampos(Window v)
    {
        var caja = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        var marco = caja?.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "caja");
        Dice(marco is not null, "la caja de texto usa la plantilla de Ondine, no la de Fluent");
        Dice(marco is not null && marco.CornerRadius.TopLeft == 6,
            $"con su radio de esquina ({marco?.CornerRadius})");

        // El texto de ayuda se ve con la caja vacia y se va al escribir. En WPF era un
        // DataTrigger sobre Text.IsEmpty; aqui es la pseudoclase :empty.
        var pista = caja?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "pista");
        Dice(pista is not null && pista.IsVisible,
            "el texto de ayuda se ve con la caja vacia");

        if (caja is not null)
        {
            caja.Text = "algo";
            v.UpdateLayout();
            Dice(pista is not null && !pista.IsVisible,
                "y desaparece al escribir — la pseudoclase :empty hace lo del DataTrigger de WPF");
            caja.Text = "";
        }

        // La casilla: el tic aparece al marcar.
        var casilla = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        var tic = casilla?.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                          .FirstOrDefault(p => p.Name == "tic");
        Dice(tic is not null, "la casilla usa la plantilla de Ondine");
        Dice(tic is not null && tic.IsVisible,
            "y marcada enseña el tic");

        if (casilla is not null)
        {
            casilla.IsChecked = false;
            v.UpdateLayout();
            Dice(tic is not null && !tic.IsVisible, "y desmarcada lo esconde");
            casilla.IsChecked = true;
        }

        // El desplegable se viste con Setters, no con plantilla: aqui basta comprobar que
        // los valores son los nuestros.
        var combo = v.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault();
        Dice(combo is not null && combo.CornerRadius.TopLeft == 6,
            $"el desplegable lleva los valores de Ondine ({combo?.CornerRadius})");
    }

    /// <summary>
    /// El dialogo, abierto de verdad y contestado desde el codigo.
    ///
    /// <para>
    /// Lo que se mide no es que se vea: es que <b>devuelva lo que se pulso</b> y que
    /// cerrarlo con Esc cuente como «no». Esa segunda parte es la que importa, porque
    /// Confirmar se usa antes de tocar ficheros y un «cancelar» que se leyera como «si»
    /// borraria cosas.
    /// </para>
    /// </summary>
    public static async Task CorrerDialogo(Window dueno)
    {
        // Aceptar
        var tarea = Dialogo.Confirmar(dueno, "prueba", "un mensaje con una ruta: C:/algo");
        await Task.Delay(300);

        var d = dueno.OwnedWindows.OfType<Dialogo>().FirstOrDefault();
        if (d is null) { Dice(false, "el dialogo no llego a abrirse"); return; }

        Dice(true, "el dialogo se abre como modal de su ventana");

        var titulo = d.GetVisualDescendants().OfType<TextBlock>()
                      .FirstOrDefault(t => t.Name == "lblTitulo");
        Dice(titulo?.Text == "prueba", $"y lleva el titulo que se le paso ({titulo?.Text})");

        var si = d.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "btnSi");
        Dice(si is not null && si.IsVisible, "el boton de aceptar esta puesto");

        // Los rotulos salen del catalogo compartido, no de una cadena suelta.
        Dice(si?.Content?.ToString() == Ondine.Localizacion.Textos.Instancia.Si,
            $"y su rotulo sale del catalogo, igual que en WPF ({si?.Content})");

        si!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dice(await tarea, "pulsar aceptar devuelve true");

        // ══ Y LO QUE IMPORTA: cerrar sin aceptar es «no» ═════════════════════════
        var otra = Dialogo.Confirmar(dueno, "prueba", "esta se cierra sin contestar");
        await Task.Delay(300);

        var d2 = dueno.OwnedWindows.OfType<Dialogo>().FirstOrDefault();

        // Se pulsa ESC de verdad, no se llama a Close(). Llamar a Close() sin argumento
        // devuelve false por el framework, no por este codigo: la comprobacion habria
        // pasado igual con el manejador de Esc borrado o puesto en true. Es el mismo
        // error que ya se colo una vez en la escala del codificador — una prueba que no
        // puede fallar por culpa del codigo que dice verificar.
        d2?.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Escape,
        });

        Dice(!await otra,
            "cerrar con Esc cuenta como NO — se pregunta antes de tocar ficheros, " +
            "y un «cancelar» leido como «si» borraria cosas");
    }
}
