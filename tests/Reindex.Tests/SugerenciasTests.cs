namespace Ondine.Reindex.Tests;

/// <summary>
/// El autocompletado de renombrar, cuando todavía no hay ninguna ventana delante.
///
/// <para>
/// Estas tres cosas —qué trozo se está escribiendo, qué se ofrece y dónde cae lo elegido—
/// vivían dentro del widget de WPF, y son <b>aritmética de cadenas</b>: no necesitan una
/// ventana para nada. Bajarlas al motor no es orden por el orden: es que al portar la
/// pantalla a Avalonia habría que volver a escribirlas, y volver a escribir un cálculo de
/// posiciones es volver a equivocarse en el mismo sitio. Aquí se prueban una vez y las dos
/// interfaces usan lo mismo.
/// </para>
/// <para>
/// El caso que lo justifica entero es el del cursor: el texto se corta y se pega por índice,
/// y un índice mal puesto no revienta — deja el nombre del fichero con una letra de más.
/// </para>
/// </summary>
public static class SugerenciasTests
{
    public static void Todas()
    {
        Program.Seccion("Autocompletado de renombrar");

        ElTrozoQueSeEstaEscribiendo();
        LoQueSeOfrece();
        DondeCaeLoElegido();
    }

    // ══ Qué trozo se está escribiendo ════════════════════════════════════════
    private static void ElTrozoQueSeEstaEscribiendo()
    {
        // Sin ningún «$», el trozo es todo lo que hay hasta el cursor: estás escribiendo
        // texto normal y se busca por lo que llevas puesto.
        var (a, ta) = Sugerencias.Trozo("temporada", 4);
        Program.Assert(a == 0 && ta == "temp",
            $"sin «$», el trozo es todo lo escrito hasta el cursor («{ta}» desde {a})");

        // Con un «$», el trozo empieza AHÍ: estás escribiendo una variable, y lo que se
        // ofrece son variables. Si el corte no empezara en el «$», al aceptar se
        // machacaría el texto de delante.
        var (b, tb) = Sugerencias.Trozo("Cap ${cou", 9);
        Program.Assert(b == 4 && tb == "${cou",
            $"con «$», el trozo empieza en el «$» y no antes («{tb}» desde {b})");

        // Un «$» con un espacio por medio ya no es una variable a medias: es un dólar
        // suelto en el nombre. Volver a ofrecer variables ahí sería ofrecer una
        // sustitución que se comería la palabra entera.
        var (c, tc) = Sugerencias.Trozo("$ de la temp", 12);
        Program.Assert(c == 0 && tc == "$ de la temp",
            $"un «$» con espacios por medio ya no es una variable a medias («{tc}»)");

        // El cursor al principio: no hay nada escrito todavía, así que se ofrece todo.
        var (d, td) = Sugerencias.Trozo("loquesea", 0);
        Program.Assert(d == 0 && td == "", "con el cursor al principio no hay trozo");

        // Un cursor imposible no puede tumbar la ventana. Pasa de verdad: el texto se
        // cambia por código y el cursor se queda donde estaba, más allá del final.
        var (e, te) = Sugerencias.Trozo("ab", 99);
        Program.Assert(e == 0 && te == "ab", "un cursor pasado del final se recorta, no revienta");
    }

    // ══ Qué se ofrece ════════════════════════════════════════════════════════
    private static void LoQueSeOfrece()
    {
        var catalogo = new List<SuggestionItem>
        {
            new() { Text = "${counter}", Desc = "un numero que sube" },
            new() { Text = "^",          Desc = "el principio del nombre" },
            new() { Text = ".*",         Desc = "todo el texto" },
        };
        string[] historial = ["1080p", "  ", "BluRay"];

        var todo = Sugerencias.Filtrar(catalogo, historial, "");
        Program.Assert(todo.Count == 5,
            $"sin nada escrito se ofrece el historial y el catálogo entero ({todo.Count})");
        Program.Assert(todo[0].Text == "1080p",
            "y lo usado hace poco va primero: es lo que más veces se repite");
        Program.Assert(todo.All(i => !string.IsNullOrWhiteSpace(i.Text)),
            "sin colar entradas en blanco del historial");

        // Empezando por «$» se filtra por PREFIJO: estás escribiendo una variable y lo
        // que quieres es la que empieza así, no cualquiera que la mencione.
        var vars = Sugerencias.Filtrar(catalogo, historial, "${cou");
        Program.Assert(vars.Count == 1 && vars[0].Text == "${counter}",
            $"escribiendo una variable se filtra por el principio ({vars.Count})");

        // Sin «$» se busca por contenido, y también en la descripción: el catálogo está
        // lleno de símbolos que nadie escribe de memoria («^», «.*»), así que se
        // encuentran por lo que hacen.
        var porTexto = Sugerencias.Filtrar(catalogo, historial, "principio");
        Program.Assert(porTexto.Count == 1 && porTexto[0].Text == "^",
            "y sin «$» se busca también por la descripción, no solo por el símbolo");

        // Un historial largo no puede llenar la pantalla de una lista infinita.
        var largo = Enumerable.Range(0, 200).Select(i => $"h{i}").ToArray();
        Program.Assert(Sugerencias.Filtrar(catalogo, largo, "").Count <= 60,
            "y la lista tiene tope: un historial largo no llena la pantalla");
    }

    // ══ Dónde cae lo elegido ═════════════════════════════════════════════════
    private static void DondeCaeLoElegido()
    {
        // El caso normal: se sustituye SOLO el trozo, y el cursor queda detrás de lo
        // insertado para poder seguir escribiendo.
        var (t1, c1) = Sugerencias.Insertar("Cap ${cou", 9, "${counter}");
        Program.Assert(t1 == "Cap ${counter}" && c1 == 14,
            $"lo elegido sustituye el trozo y el cursor queda detrás («{t1}», cursor {c1})");

        // Lo que hay DESPUÉS del cursor se conserva. Es lo que se pierde si el cálculo
        // se rehace a ojo: el nombre sale cortado y no se ve hasta aplicar.
        var (t2, c2) = Sugerencias.Insertar("Cap ${cou - final", 9, "${counter}");
        Program.Assert(t2 == "Cap ${counter} - final" && c2 == 14,
            $"y lo que había detrás del cursor sigue ahí («{t2}»)");

        // Sobre un campo vacío: ni corta nada ni se sale de rango.
        var (t3, c3) = Sugerencias.Insertar("", 0, "^");
        Program.Assert(t3 == "^" && c3 == 1, $"sobre un campo vacío entra tal cual («{t3}»)");

        // Y un cursor imposible tampoco revienta aquí.
        var (t4, _) = Sugerencias.Insertar("ab", 99, "X");
        Program.Assert(t4 == "X", $"con el cursor pasado del final tampoco revienta («{t4}»)");
    }
}
