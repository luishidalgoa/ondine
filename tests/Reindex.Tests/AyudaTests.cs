using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El trinquete de la Ayuda.
///
/// <para>
/// La norma del repositorio dice que tocar un flujo obliga a actualizar su tutorial en el
/// MISMO cambio. Es una norma buena y <b>nada la comprobaba</b>: se cumplió mientras alguien
/// se acordó, y dejó de cumplirse en cuanto no. Pasó con «cortar sin recodificar» — el flujo
/// de Recortes cambió y la Ayuda siguió describiendo la pantalla anterior durante un PR
/// entero. Un tutorial que cuenta la versión pasada es peor que no tenerlo: se lee, se cree,
/// y manda a buscar cosas que ya no están donde dice.
/// </para>
/// <para>
/// <b>Esto NO comprueba que la Ayuda sea buena.</b> Eso no lo puede comprobar una máquina, y
/// una prueba que lo intentara gritaría en falso y se acabaría desactivando. Lo que hace es
/// <b>congelar cuántas opciones tiene cada pantalla</b>. Si aparece una nueva, la cifra deja
/// de cuadrar y alguien tiene que mirar la Ayuda antes de poder seguir. Ese «alguien tiene
/// que mirar» es todo el mecanismo, y es exactamente el mismo trinquete que ya usa
/// <see cref="TraduccionTests"/> con los literales sin traducir.
/// </para>
/// <para>
/// Se cuentan las casillas y no los botones a propósito: una casilla es una <i>opción</i>
/// —cambia lo que la app va a hacer— y por eso pertenece al tutorial. Un botón suele ser el
/// gesto de ejecutar algo que el tutorial ya explica.
/// </para>
/// </summary>
public static class AyudaTests
{
    /// <summary>
    /// Cuántas casillas tiene hoy cada pantalla, y dónde se cuenta lo que hacen.
    ///
    /// <para>
    /// Si añades o quitas una: abre <c>AyudaWindow.xaml</c>, comprueba que su apartado sigue
    /// contando la verdad, y ajusta la cifra aquí. Bajarla también cuenta — el trinquete
    /// aprieta en los dos sentidos, porque una opción retirada deja un tutorial que manda a
    /// buscar algo que ya no existe.
    /// </para>
    /// </summary>
    private static readonly (string Pantalla, int Casillas, string Apartado)[] Congelado =
    [
        ("MainWindow.xaml",     4, "Ayuda → Comprimir"),
        ("OrganizarView.xaml",  6, "Ayuda → Organizar"),
        ("RecortesView.xaml",   1, "Ayuda → Recortes"),
    ];

    // Con `Singleline` a propósito: una casilla ocupa varias líneas en cuanto lleva
    // etiqueta y globo de ayuda, y una expresión por líneas se las salta justo cuando más
    // importa —las recién añadidas son siempre las largas—. Contarlas mal en menos y decir
    // que todo cuadra es la única forma en que este trinquete podría mentir.
    private static readonly Regex Casilla = new(@"<CheckBox\b[^>]*?>", RegexOptions.Singleline);

    public static void Todas()
    {
        Program.Seccion("La Ayuda va al día con lo que hacen las pantallas");

        var raiz = LocalizarRaiz();
        if (raiz.Length == 0)
        {
            Program.Assert(false, "no encuentro la raíz del repositorio");
            return;
        }

        foreach (var (pantalla, esperadas, apartado) in Congelado)
        {
            var ruta = Path.Combine(raiz, "src", "Ondine", pantalla);
            if (!File.Exists(ruta))
            {
                Program.Assert(false, $"no encuentro «{pantalla}»: ¿se ha movido o renombrado?");
                continue;
            }

            var hay = Casilla.Matches(File.ReadAllText(ruta)).Count;

            Program.Assert(hay == esperadas,
                hay == esperadas
                    ? $"{pantalla}: {hay} opciones, las mismas que cuenta {apartado}"
                    : $"{pantalla} tiene {hay} opciones y estaban congeladas {esperadas}. " +
                      $"Repasa «{apartado}» en AyudaWindow.xaml y, cuando cuente la verdad, " +
                      $"ajusta la cifra en AyudaTests.");
        }

        // Y que el tutorial exista, no solo que cuadre la cuenta. Una pantalla sin apartado
        // deja el trinquete comparando contra nada.
        var ayuda = Path.Combine(raiz, "src", "Ondine", "AyudaWindow.xaml");
        var texto = File.Exists(ayuda) ? File.ReadAllText(ayuda) : "";
        // Organizar tiene DOS apartados -«cómo decide» y «los pasos»- y por eso no hay un
        // «pagOrganizar». Lo supuse al escribir esto y esta misma comprobación me corrigió,
        // que es justo para lo que está.
        foreach (var pagina in new[] { "pagComprimir", "pagOrgComo", "pagOrgPasos", "pagRecortes" })
            Program.Assert(texto.Contains(pagina, StringComparison.Ordinal),
                $"la Ayuda sigue teniendo su apartado «{pagina}»");
    }

    /// <summary>Sube hasta la raíz del repositorio, para no depender del cwd.</summary>
    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "src", "Ondine"))
                               && Directory.Exists(Path.Combine(d.FullName, "src", "Ondine.Core"))))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
