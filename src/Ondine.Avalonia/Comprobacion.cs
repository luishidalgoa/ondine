using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

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
        var botones = v.GetVisualDescendants().OfType<Button>().ToList();

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
}
