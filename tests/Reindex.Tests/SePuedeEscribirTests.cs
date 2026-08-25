namespace Ondine.Reindex.Tests;

/// <summary>
/// Comprobar que se puede escribir ANTES de empezar, y no doce veces después.
///
/// <para>
/// <b>El caso real:</b> doce capítulos, cada uno con su análisis de pistas y su reintento sin
/// subtítulos, y los doce fallando por lo mismo — <c>Permission denied</c> en la carpeta de
/// destino. Doce veces el mismo error, uno detrás de otro, después de haber esperado a cada
/// uno.
/// </para>
/// <para>
/// El fallo estaba en el sistema del usuario y no en Ondine, pero <b>la forma de contarlo sí
/// era de Ondine</b>: nada impedía saberlo antes de tocar el primero. Escribir un fichero
/// vacío en la carpeta de destino cuesta milisegundos y responde la pregunta entera.
/// </para>
/// <para>
/// Y no vale con mirar los permisos: en un montaje de red, en un disco montado de solo
/// lectura o con un dueño distinto, los bits dicen una cosa y el sistema hace otra.
/// <b>La única forma honesta de saber si se puede escribir es escribir.</b>
/// </para>
/// </summary>
public static class SePuedeEscribirTests
{
    public static void Todas()
    {
        Program.Seccion("Se puede escribir donde se va a escribir");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-escritura-prueba");
        try
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, true);

            EnUnaCarpetaNormalSiSePuede(raiz);
            SeCreaLaCarpetaSiNoEstaba(raiz);
            NoDejaBasuraDetras(raiz);
            LoQueNoSePuedeCrearSeDice();
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    private static void EnUnaCarpetaNormalSiSePuede(string raiz)
    {
        var carpeta = Path.Combine(raiz, "normal");
        Directory.CreateDirectory(carpeta);

        Program.Assert(Engine.SePuedeEscribirEn(carpeta) is null,
            "en una carpeta normal se puede escribir, y no se devuelve queja");
    }

    /// <summary>
    /// Si la carpeta de destino no existe todavía, se crea — que es lo que hace la compresión
    /// de todas formas. Comprobar sobre una carpeta que no existe y decir «no se puede» sería
    /// un falso negativo en el caso más normal: la primera vez.
    /// </summary>
    private static void SeCreaLaCarpetaSiNoEstaba(string raiz)
    {
        var carpeta = Path.Combine(raiz, "nueva", "y", "honda");

        Program.Assert(Engine.SePuedeEscribirEn(carpeta) is null,
            "una carpeta que no existía se crea y se puede escribir");
        Program.Assert(Directory.Exists(carpeta), "y queda creada, como haría la compresión");
    }

    /// <summary>
    /// La comprobación escribe de verdad, así que tiene que recoger. Un fichero de prueba
    /// olvidado en la carpeta de la biblioteca es basura que parece contenido.
    /// </summary>
    private static void NoDejaBasuraDetras(string raiz)
    {
        var carpeta = Path.Combine(raiz, "limpia");
        Directory.CreateDirectory(carpeta);

        Engine.SePuedeEscribirEn(carpeta);
        Engine.SePuedeEscribirEn(carpeta);

        var quedan = Directory.GetFileSystemEntries(carpeta);
        Program.Assert(quedan.Length == 0,
            $"la comprobación no deja nada detrás ({quedan.Length} ficheros)");
    }

    /// <summary>
    /// Y cuando no se puede, se devuelve el motivo del sistema. No se traduce ni se adorna: es
    /// lo que hay que poder buscar en internet.
    /// </summary>
    private static void LoQueNoSePuedeCrearSeDice()
    {
        // Una ruta imposible en los dos sistemas: un carácter que no vale en Windows y una
        // carpeta bajo un fichero en Unix. Basta con que falle por algo.
        var imposible = OperatingSystem.IsWindows()
            ? "Z:" + Path.DirectorySeparatorChar + "no-existe-esta-unidad"
            : "/proc/no-se-puede-crear-aqui/destino";

        var queja = Engine.SePuedeEscribirEn(imposible);
        Program.Assert(queja is { Length: > 0 },
            $"donde no se puede escribir, se dice por qué ({queja})");
    }
}
