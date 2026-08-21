using Ondine.Localizacion;

namespace Ondine;

/// <summary>Una entrada del desplegable de autocompletado.</summary>
public sealed class SuggestionItem
{
    public string Text { get; init; } = "";     // lo que se inserta
    public string Desc { get; init; } = "";     // explicación breve
    /// <summary>Opción que hay que activar para que esto funcione: "regex", "enum" o "rand".</summary>
    public string? Enables { get; init; }
}

/// <summary>
/// Lo que sabe el autocompletado de renombrar, sin ninguna ventana delante.
///
/// <para>
/// Las tres cosas que hay aquí —qué trozo se está escribiendo, qué se ofrece y dónde cae lo
/// elegido— son <b>aritmética de cadenas</b>. Vivían dentro del widget de WPF, que es donde
/// se escribieron, pero no necesitan una ventana para nada.
/// </para>
/// <para>
/// Están en el motor porque al portar la pantalla a Avalonia habría que volver a
/// escribirlas, y volver a escribir un cálculo de posiciones es volver a equivocarse en el
/// mismo sitio: el texto se corta y se pega por índice, y un índice mal puesto no revienta
/// —deja el nombre del fichero con una letra de más—. Se prueban una vez y las dos
/// interfaces usan lo mismo.
/// </para>
/// </summary>
public static class Sugerencias
{
    /// <summary>Cuántas caben en el desplegable antes de que deje de ser una ayuda.</summary>
    private const int Tope = 60;

    /// <summary>
    /// El «trozo» que se está escribiendo: desde el último <c>$</c> si lo hay, o todo lo
    /// que haya hasta el cursor.
    ///
    /// <para>
    /// El <c>$</c> manda porque marca el principio de una variable. Si el corte no empezara
    /// ahí, aceptar una sugerencia machacaría el texto de delante. Y un <c>$</c> con un
    /// espacio por medio ya no es una variable a medias, es un dólar suelto en el nombre.
    /// </para>
    /// </summary>
    public static (int inicio, string trozo) Trozo(string? texto, int cursor)
    {
        string t = texto ?? "";
        // El cursor puede venir pasado del final: el texto se cambia por código y él se
        // queda donde estaba. Recortarlo aquí evita un reventón por un caso que pasa.
        int caret = Math.Clamp(cursor, 0, t.Length);

        if (caret > 0)
        {
            int dolar = t.LastIndexOf('$', caret - 1);
            if (dolar >= 0)
            {
                string seg = t[dolar..caret];
                if (!seg.Any(char.IsWhiteSpace)) return (dolar, seg);
            }
        }
        return (0, t[..caret]);
    }

    /// <summary>
    /// Lo elegido, metido en su sitio: devuelve el texto ya cambiado y dónde queda el cursor.
    ///
    /// <para>
    /// Sustituye SOLO el trozo que se estaba escribiendo y conserva lo que hubiera detrás
    /// del cursor. Eso último es lo que se pierde si el cálculo se rehace a ojo, y no se ve
    /// hasta aplicar el renombrado.
    /// </para>
    /// </summary>
    public static (string texto, int cursor) Insertar(string? texto, int cursor, string loElegido)
    {
        string t = texto ?? "";
        var (inicio, trozo) = Trozo(t, cursor);
        string nuevo = t.Remove(inicio, trozo.Length).Insert(inicio, loElegido);
        return (nuevo, inicio + loElegido.Length);
    }

    /// <summary>
    /// Filtra catálogo + historial según lo escrito. Si el token empieza por <c>$</c> se
    /// filtra por prefijo (estás escribiendo una variable); si no, por contenido en el texto
    /// o en la descripción — el catálogo está lleno de símbolos que nadie escribe de memoria
    /// («^», «.*»), así que se encuentran por lo que hacen.
    /// </summary>
    public static List<SuggestionItem> Filtrar(
        IReadOnlyList<SuggestionItem> catalogo, IEnumerable<string> historial, string token)
    {
        bool esVariable = token.StartsWith('$');
        var hist = historial.Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => new SuggestionItem { Text = h, Desc = Textos.Instancia.SugerenciaUsadoRecientemente });

        // El historial primero: lo usado hace poco es lo que más veces se repite.
        var todo = hist.Concat(catalogo);
        var filtrado = token.Length == 0
            ? todo
            : todo.Where(i => esVariable
                ? i.Text.StartsWith(token, StringComparison.OrdinalIgnoreCase)
                : i.Text.Contains(token, StringComparison.OrdinalIgnoreCase)
                  || i.Desc.Contains(token, StringComparison.OrdinalIgnoreCase));

        return filtrado.Take(Tope).ToList();
    }
}
