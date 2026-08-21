using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Que la Ayuda de Avalonia explique LO MISMO que la de WPF.
///
/// <para>
/// La Ayuda es la pantalla con menos código de la app —no tiene ninguna lógica, solo
/// explica— y por eso es la más fácil de portar mal sin enterarse. Si al traducir doscientas
/// líneas de XAML se cae un párrafo, <b>nada protesta</b>: la ventana abre, se ve bien, y
/// simplemente explica menos. No hay ningún momento en el que alguien lo descubra, porque
/// nadie se lee la ayuda dos veces para compararla.
/// </para>
/// <para>
/// Aquí se comparan las claves de texto de las dos versiones. No el aspecto —eso puede y
/// debe cambiar— sino <b>qué se dice</b>. Y va en las dos direcciones: una clave que solo
/// esté en Avalonia también canta, porque significa que la de WPF se quedó atrás cuando se
/// añadió algo nuevo. Las dos interfaces conviven mientras dure el puerto, así que las dos
/// tienen que contar la misma historia.
/// </para>
/// <para>
/// Como <see cref="TemaCompartidoTests"/>, no necesita Avalonia: son ficheros de texto.
/// </para>
/// </summary>
public static class AyudaPortadaTests
{
    /// <summary>
    /// Lo que la versión de Avalonia NO trae a propósito, con su motivo. Vacío hoy; existe
    /// para que una diferencia deliberada se pueda declarar en vez de tener que apagar la
    /// comprobación entera.
    /// </summary>
    private static readonly Dictionary<string, string> SoloEnWpf = new();

    public static void Todas()
    {
        Program.Seccion("La Ayuda portada dice lo mismo");

        var raiz = LocalizarRaiz();
        var wpf = Path.Combine(raiz, "src", "Ondine", "AyudaWindow.xaml");
        var ava = Path.Combine(raiz, "src", "Ondine.Avalonia", "Ayuda.axaml");

        if (!File.Exists(wpf) || !File.Exists(ava))
        {
            Program.Assert(false, "no encuentro una de las dos Ayudas");
            return;
        }

        var enWpf = Claves(wpf);
        var enAva = Claves(ava);

        // Suelo: si un día el patrón deja de casar, esto mediría cero y pasaría.
        Program.Assert(enWpf.Count >= 60,
            $"la Ayuda de WPF usa {enWpf.Count} textos del catálogo: si fueran cuatro, esto no mediría nada");

        // ══ Lo que falta en Avalonia ═════════════════════════════════════════
        var faltan = enWpf.Where(k => !enAva.Contains(k) && !SoloEnWpf.ContainsKey(k)).ToList();
        Program.Assert(faltan.Count == 0,
            faltan.Count == 0
                ? $"los {enWpf.Count} textos de la Ayuda están en las dos versiones"
                : $"{faltan.Count} textos se quedaron sin portar: {string.Join(", ", faltan.Take(8))}. " +
                  "La ventana abre igual y explica menos, que es lo que nadie nota.");

        // ══ Y lo que sobra ═══════════════════════════════════════════════════
        // Al revés también importa: si se añade algo a la Ayuda de Avalonia y no a la de
        // WPF, quien siga en Windows —que hoy son todos— se queda sin esa explicación.
        var sobran = enAva.Where(k => !enWpf.Contains(k)).ToList();
        Program.Assert(sobran.Count == 0,
            sobran.Count == 0
                ? "y ninguna versión explica algo que la otra no"
                : $"{sobran.Count} textos solo están en Avalonia: {string.Join(", ", sobran.Take(8))}. " +
                  "Mientras las dos interfaces convivan, las dos cuentan la misma historia.");

        // ══ La lista de excepciones no se queda rancia ════════════════════════
        var fantasmas = SoloEnWpf.Keys.Where(k => enAva.Contains(k) || !enWpf.Contains(k)).ToList();
        Program.Assert(fantasmas.Count == 0,
            fantasmas.Count == 0
                ? "ni hay excepciones apuntadas que ya no vengan a cuento"
                : $"{string.Join(", ", fantasmas)} sobran de la lista de excepciones");
    }

    private static HashSet<string> Claves(string fichero) =>
        Regex.Matches(File.ReadAllText(fichero), @"\{i:T\s+([A-Za-z][A-Za-z0-9]*)\s*\}")
             .Select(m => m.Groups[1].Value).ToHashSet();

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "src", "Ondine"))
                               && Directory.Exists(Path.Combine(d.FullName, "src", "Ondine.Core"))))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
