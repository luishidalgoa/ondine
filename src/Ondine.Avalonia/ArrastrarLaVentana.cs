using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Ondine.Ava;

/// <summary>
/// Mover y estirar una ventana sin marco del sistema.
///
/// <para>
/// Ondine dibuja sus propias barras de título, así que todas sus ventanas van con
/// <c>SystemDecorations="None"</c>. Eso se lleva por delante dos cosas que nadie declara y todo
/// el mundo espera: <b>arrastrar</b> y <b>estirar por los bordes</b>. En WPF las daba
/// <c>WindowChrome</c> gratis; aquí hay que pedirlas.
/// </para>
/// <para>
/// <b>Y no se pidieron.</b> De las diez ventanas, solo dos las tenían. Las otras ocho —Ayuda,
/// Catálogo, Encargo, Faltantes, Pistas, Reordenar, Reproductor y la principal— se quedaban
/// clavadas donde el sistema las abriera. La principal se arregló cuando alguien lo dijo; las
/// demás seguían igual.
/// </para>
/// <para>
/// Está aquí y no copiado ocho veces por lo de siempre: ocho copias son ocho sitios donde
/// arreglar el siguiente detalle, y siete que se olvidan.
/// </para>
/// </summary>
internal static class ArrastrarLaVentana
{
    /// <summary>Cuántos píxeles del borde agarran para estirar.</summary>
    private const double Grosor = 6;

    /// <summary>
    /// Deja la ventana movible y estirable.
    ///
    /// <para>
    /// Se arrastra desde <b>cualquier hueco</b> y no solo desde la franja del título. Para un
    /// modal pequeño es mejor —hay poco hueco y da igual dónde lo agarres— y evita tener que
    /// ponerle nombre a una cabecera en cada XAML, que es la clase de trabajo que se hace en
    /// siete de ocho ventanas.
    /// </para>
    /// <para>
    /// Lo que <b>no</b> arrastra es un control: si la pulsación viene de un botón, un menú o
    /// una caja de texto, es suya. Sin eso, un menú de Avalonia —que se abre al PULSAR— pierde
    /// el clic y hay que insistir dos veces; pasó en la ventana principal y lo dijo el usuario.
    /// </para>
    /// </summary>
    public static void Enganchar(Window v)
    {
        v.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(v).Properties.IsLeftButtonPressed) return;
            if (e.Source is Visual origen && EsDeUnControl(origen, v)) return;

            // Estirar gana a mover: si el ratón está en el borde, se estira.
            if (BordeEn(e.GetPosition(v), v.Bounds.Size, Grosor) is { } borde)
            {
                v.BeginResizeDrag(borde, e);
                return;
            }

            // Doble clic = maximizar, como en cualquier barra de título del sistema. Solo si
            // la ventana se puede redimensionar: maximizar una de tamaño fijo la deja rara.
            if (e.ClickCount == 2 && v.CanResize)
            {
                v.WindowState = v.WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            v.BeginMoveDrag(e);
        };

        // El puntero avisa de dónde se agarra. Sin la flecha, un borde que se puede estirar no
        // lo parece y no lo estira nadie.
        v.PointerMoved += (_, e) =>
        {
            if (!v.CanResize) return;
            var borde = BordeEn(e.GetPosition(v), v.Bounds.Size, Grosor);
            v.Cursor = new Cursor(CursorDe(borde));
        };
    }

    /// <summary>
    /// Si la pulsación cayó sobre algo con lo que se puede hablar.
    ///
    /// <para>
    /// Se recorre hacia arriba hasta la ventana: el origen de un evento es el elemento más de
    /// dentro —el texto de un menú, no el menú— y ese no dice de quién es.
    /// </para>
    /// </summary>
    private static bool EsDeUnControl(Visual origen, Window tope)
    {
        for (var x = origen; x is not null && x != tope; x = x.GetVisualParent())
            if (x is Button or MenuItem or Menu or ToggleButton or ComboBox or TextBox
                     or Slider or ListBox or ListBoxItem or DataGrid or ScrollBar or Thumb)
                return true;
        return false;
    }

    /// <summary>
    /// Qué borde de la ventana hay bajo un punto, o <c>null</c> si no hay ninguno.
    ///
    /// <para>
    /// Aparte y sin tocar nada a propósito: es la única parte de «estirar la ventana» que se
    /// puede comprobar sin un ratón de verdad, y es donde está lo que se puede equivocar —las
    /// esquinas tienen que ganar a los lados, o estirar en diagonal es imposible.
    /// </para>
    /// </summary>
    internal static WindowEdge? BordeEn(Point p, Size tamano, double grosor)
    {
        bool izq = p.X <= grosor, der = p.X >= tamano.Width - grosor;
        bool arr = p.Y <= grosor, aba = p.Y >= tamano.Height - grosor;

        // Las esquinas primero: en una esquina se cumplen dos condiciones a la vez, y si
        // ganara el lado no se podría estirar en diagonal, que es como se estira de verdad.
        if (arr && izq) return WindowEdge.NorthWest;
        if (arr && der) return WindowEdge.NorthEast;
        if (aba && izq) return WindowEdge.SouthWest;
        if (aba && der) return WindowEdge.SouthEast;

        if (arr) return WindowEdge.North;
        if (aba) return WindowEdge.South;
        if (izq) return WindowEdge.West;
        if (der) return WindowEdge.East;
        return null;
    }

    /// <summary>El puntero de cada borde. Sin flecha, nadie sabe que agarra.</summary>
    private static StandardCursorType CursorDe(WindowEdge? borde) => borde switch
    {
        WindowEdge.North or WindowEdge.South => StandardCursorType.SizeNorthSouth,
        WindowEdge.West or WindowEdge.East => StandardCursorType.SizeWestEast,
        WindowEdge.NorthWest or WindowEdge.SouthEast => StandardCursorType.TopLeftCorner,
        WindowEdge.NorthEast or WindowEdge.SouthWest => StandardCursorType.TopRightCorner,
        _ => StandardCursorType.Arrow,
    };
}
