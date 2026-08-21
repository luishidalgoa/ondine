using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Ondine.Spike;

/// <summary>
/// La prueba, corrida sola.
///
/// <para>
/// Hacía falta porque en Avalonia <b>un binding que no resuelve no da error</b>: deja
/// null y sigue. Que el XAML compile no dice nada; el compilador acepta encantado
/// <c>$parent[DataGridRow]</c> apunte o no a algo. Así que se abre la ventana de verdad,
/// se selecciona una fila, se buscan los botones que el <c>RowDetails</c> haya realizado
/// y se mira qué llegó a su <c>Tag</c>.
/// </para>
/// <para>
/// Y se pulsan. Un binding puede traer la fila correcta y aun así no servir si al
/// cambiarla la tabla no se entera: el <c>DataGrid</c> es un control virtualizado y sus
/// celdas no siempre escuchan lo que uno cree.
/// </para>
/// </summary>
public static class Comprobacion
{
    public static readonly List<string> Resultados = [];

    private static void Dice(bool bien, string que) =>
        Resultados.Add($"{(bien ? "✓" : "✗")} {que}");

    public static void Correr(MainWindow ventana, DataGrid tabla, IList<Fila> filas)
    {
        // ── Una fila CON candidatos despliega su panel ────────────────────────────
        tabla.SelectedIndex = 0;
        tabla.UpdateLayout();

        var botones = tabla.GetVisualDescendants().OfType<Button>()
            .Where(b => b.Content?.ToString()?.StartsWith("Es esta") == true)
            .ToList();

        Dice(botones.Count > 0,
            $"el RowDetails se realiza al seleccionar: {botones.Count} botones «Es esta» en el árbol visual");

        if (botones.Count == 0)
        {
            Dice(false, "sin panel no hay nada más que medir: el DataGrid NO sirve para Organizar");
            return;
        }

        // ── LA PREGUNTA: ¿el botón alcanza el DataContext de SU fila? ─────────────
        foreach (var sintaxis in new[] { "$parent", "RelativeSource" })
        {
            var boton = botones.FirstOrDefault(b => b.Content!.ToString()!.Contains(sintaxis));
            if (boton is null) { Dice(false, $"no encontré el botón de la sintaxis {sintaxis}"); continue; }

            var llego = boton.Tag;
            Dice(llego is Fila,
                $"binding «{sintaxis}» sube hasta la fila: Tag = {(llego is null ? "null" : llego.GetType().Name)}");
        }

        // ── Pulsar de verdad y ver si la fila cambia y la tabla lo refleja ────────
        var antes = filas[0].Propuesta;
        var elPrimero = botones.First(b => b.Content!.ToString()!.Contains("$parent"));
        elPrimero.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        tabla.UpdateLayout();

        Dice(filas[0].Propuesta != antes && filas[0].HayEleccion,
            $"pulsar cambia la fila: «{antes}» → «{filas[0].Propuesta}»");

        // Que el modelo cambie no basta: la celda tiene que repintarse. Se busca el
        // texto nuevo dentro de la tabla, que es lo que vería el usuario.
        var enPantalla = tabla.GetVisualDescendants().OfType<TextBlock>()
            .Any(t => t.Text == filas[0].Propuesta);
        Dice(enPantalla, "y la celda de la tabla se repinta con el valor nuevo");

        // El semáforo de confianza, que en WPF es un Trigger y aquí es Classes.*
        var chip = tabla.GetVisualDescendants().OfType<Border>()
            .FirstOrDefault(b => b.Classes.Contains("segura"));
        Dice(chip is not null, "el semáforo por Classes.* reacciona (la clase «segura» está puesta)");

        // ── Una fila SIN candidatos no debe enseñar panel ─────────────────────────
        tabla.SelectedIndex = 1;
        tabla.UpdateLayout();

        // Se mira SU fila, no la tabla entera: al deseleccionar, el panel de la fila
        // anterior puede seguir en el arbol visual un rato -esta virtualizada- y buscar
        // por toda la tabla encontraria aquel, no este. La primera version de esta
        // comprobacion hacia justo eso y acusaba a Avalonia de un fallo que era mio.
        var suFila = tabla.GetVisualDescendants().OfType<DataGridRow>()
            .FirstOrDefault(r => ReferenceEquals(r.DataContext, filas[1]));

        if (suFila is null)
        {
            Dice(false, "no encontre la fila sin candidatos en el arbol visual");
        }
        else
        {
            // Por NOMBRE, no "el primer Border que contenga tal boton": el DataGridRow
            // trae sus propios Border de chrome y envuelven al del template, asi que la
            // busqueda por contenido devolvia el de la fila -con el mismo DataContext
            // heredado, que es lo que despistaba- en vez del panel.
            var suPanel = suFila.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Name == "panelDetalle");

            Dice(suPanel is null || !suPanel.IsVisible,
                $"una fila sin candidatos no despliega panel (panel {(suPanel is null ? "ni existe" : "existe pero IsVisible=" + suPanel.IsVisible)})");

            // Diagnostico: si falla hay que saber si es el binding o el modelo.
            Resultados.Add($"    · DataContext del panel = {suPanel?.DataContext?.GetType().Name ?? "null"}");
            Resultados.Add($"    · TieneDetalle de esa fila = {filas[1].TieneDetalle}");
            Resultados.Add($"    · Candidatos = {filas[1].Candidatos.Count}");
            Resultados.Add($"    · IsVisible de su DataGridRow = {suFila.IsVisible}");
            Resultados.Add($"    · AreRowDetailsFrozen/DetailsVisibility = {tabla.RowDetailsVisibilityMode}");
        }

        // ── Y el «Olvidar», que ata directo a la fila sin subir por nadie ─────────
        tabla.SelectedIndex = 0;
        tabla.UpdateLayout();

        var olvidar = tabla.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(b => b.Content?.ToString() == "Olvidar elección");

        Dice(olvidar is not null && olvidar.IsVisible,
            "el botón «Olvidar» aparece solo cuando hay elección (IsVisible atado a la fila)");

        if (olvidar is not null)
        {
            olvidar.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dice(!filas[0].HayEleccion, "y deshace la elección");
        }
    }
}
