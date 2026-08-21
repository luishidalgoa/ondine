namespace Ondine.Reindex.Tests;

/// <summary>
/// El coloreado del JSON del explorador de catálogos, sin ninguna ventana delante.
///
/// <para>
/// Es un autómata mínimo escrito a mano —no un parser— que parte un JSON en trozos y le
/// pone a cada uno su color. Vivía dentro de la ventana de WPF y devolvía tipos de WPF, así
/// que no se podía probar ni reutilizar: al portar la pantalla habría que <b>volver a
/// escribirlo</b>, y un autómata reescrito a ojo es un autómata con otro fallo.
/// </para>
/// <para>
/// La decisión con miga, y la única que se puede equivocar de verdad, es esta: una cadena
/// es CLAVE si lo siguiente —saltando espacios— son los dos puntos. Si eso se rompe, el
/// JSON sale coloreado del revés y sigue siendo un JSON perfectamente legible. Nadie
/// presenta un parte por eso; simplemente el panel deja de ayudar.
/// </para>
/// </summary>
public static class ColoreadoDeJsonTests
{
    public static void Todas()
    {
        Program.Seccion("Coloreado del JSON del catálogo");

        NoSePierdeNiUnCaracter();
        LaClaveSeDistingueDelValor();
        CadaCosaConSuColor();
    }

    // ══ Lo primero de todo: que no se coma nada ══════════════════════════════
    private static void NoSePierdeNiUnCaracter()
    {
        // Si un trozo se pierde por el camino, el panel enseña un JSON DISTINTO del que se
        // copia con el botón de al lado. Eso es peor que no colorear: es mentir.
        string[] casos =
        [
            """{"num":1}""",
            """{ "num" : 175 , "titulos" : { "es" : [ "El planeta espejo" ] } }""",
            """{"especial":true,"nota":null,"fecha":"2009-07-03"}""",
            """{"raro":"con \"comillas\" dentro","menos":-3.5e10}""",
            "",
            "   ",
        ];

        foreach (var json in casos)
        {
            var junto = string.Concat(ColoreadoDeJson.Partir(json).Select(t => t.Texto));
            Program.Assert(junto == json,
                junto == json
                    ? $"no se pierde nada al colorear ({json.Length} caracteres)"
                    : $"el coloreado cambió el texto: «{junto}» en vez de «{json}»");
        }
    }

    // ══ La decisión con miga ═════════════════════════════════════════════════
    private static void LaClaveSeDistingueDelValor()
    {
        var trozos = ColoreadoDeJson.Partir("""{"serie":"serie"}""");

        // La misma palabra a los dos lados de los dos puntos: una es clave y la otra valor.
        // Es el caso que separa «mira si vienen los dos puntos» de «mira si es la primera».
        var cadenas = trozos.Where(t => t.Texto.StartsWith('"')).ToList();
        Program.Assert(cadenas.Count == 2, $"dos cadenas en el ejemplo ({cadenas.Count})");
        Program.Assert(cadenas.Count == 2 && cadenas[0].Color == "Accent300",
            "la de la izquierda de los dos puntos es CLAVE");
        Program.Assert(cadenas.Count == 2 && cadenas[1].Color == "OrgOk",
            "y la de la derecha, con el mismo texto, es VALOR");

        // Con espacios por medio sigue siendo clave: el JSON con sangría los trae.
        var conEspacios = ColoreadoDeJson.Partir("""{"num"   :   1}""");
        Program.Assert(conEspacios.First(t => t.Texto.StartsWith('"')).Color == "Accent300",
            "y con espacios antes de los dos puntos, también");

        // Una cadena dentro de una lista NO es clave, aunque le siga otra cosa.
        var enLista = ColoreadoDeJson.Partir("""["uno","dos"]""");
        Program.Assert(enLista.Where(t => t.Texto.StartsWith('"')).All(t => t.Color == "OrgOk"),
            "y las de una lista son valores, ninguna es clave");
    }

    // ══ Cada cosa con su color ═══════════════════════════════════════════════
    private static void CadaCosaConSuColor()
    {
        var t = ColoreadoDeJson.Partir("""{"n":-12.5,"si":true}""");

        Program.Assert(t.Any(x => x.Texto == "-12.5" && x.Color == "OrgWarn"),
            "un número negativo con decimales sale entero y en su color");
        Program.Assert(t.Any(x => x.Texto == "true" && x.Color == "OrgDanger"),
            "y «true» va como palabra, no como texto suelto");
        Program.Assert(t.Any(x => x.Texto.Contains(':') && x.Color == "Neutral500"),
            "y la puntuación va apagada");

        // Una cadena con comillas escapadas dentro no puede partir el trozo por la mitad:
        // ahí es donde un autómata mal escrito se rompe.
        // Se monta con Concat en vez de con un literal: escribir a mano una cadena que
        // lleva comillas escapadas DENTRO de otra que también las escapa es donde uno se
        // equivoca —y el primer intento de esta línea se equivocó, comiéndose las comillas
        // de fuera—. Aquí la esperada se construye igual que la de entrada.
        const string dentro = """dijo \"hola\" y se fue""";
        var escapada = ColoreadoDeJson.Partir($$"""{"t":"{{dentro}}"}""");
        var deUnaPieza = escapada.FirstOrDefault(x => x.Texto.Contains("hola")).Texto;
        Program.Assert(deUnaPieza == $"\"{dentro}\"",
            deUnaPieza == $"\"{dentro}\""
                ? "una cadena con comillas escapadas dentro sale de una pieza"
                : $"la cadena escapada se partió: «{deUnaPieza}»");
    }
}
