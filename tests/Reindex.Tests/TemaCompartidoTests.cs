using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Que las dos interfaces pinten del mismo color.
///
/// <para>
/// Mientras dure el puerto a Avalonia van a convivir dos temas: el de WPF
/// (<c>Theme.xaml</c>) y el nuevo (<c>Temas/Colores.axaml</c>). Un color que se cambie en
/// uno y no en el otro <b>no falla en ningún sitio</b>: la app simplemente tiene dos
/// aspectos según con qué interfaz se abra, y eso no se ve en una captura ni lo caza una
/// revisión. Se descubre cuando alguien pone las dos ventanas al lado.
/// </para>
/// <para>
/// Esto no necesita Avalonia para nada: son dos ficheros de texto y una comparación. Por
/// eso vive aquí, en el arnés del motor, y corre en CI sobre Linux como todo lo demás.
/// </para>
/// </summary>
public static class TemaCompartidoTests
{
    private static readonly Regex Pincel =
        new(@"<SolidColorBrush\s+x:Key=""([^""]+)""\s+Color=""([^""]+)""\s*/>");

    public static void Todas()
    {
        Program.Seccion("Las dos interfaces pintan del mismo color");

        var raiz = LocalizarRaiz();
        var wpf = Path.Combine(raiz, "src", "Ondine", "Theme.xaml");
        var avalonia = Path.Combine(raiz, "src", "Ondine.Avalonia", "Temas", "Colores.axaml");

        if (!File.Exists(wpf) || !File.Exists(avalonia))
        {
            Program.Assert(false, "no encuentro uno de los dos temas: ¿se ha movido alguno?");
            return;
        }

        var deWpf = Leer(wpf);
        var deAvalonia = Leer(avalonia);

        Program.Assert(deWpf.Count >= 25,
            $"el tema de WPF tiene {deWpf.Count} colores: si fueran cuatro, esto no estaría midiendo nada");

        // ── Ninguno se queda sin portar ───────────────────────────────────────────
        var faltan = deWpf.Keys.Where(k => !deAvalonia.ContainsKey(k)).ToList();
        Program.Assert(faltan.Count == 0,
            faltan.Count == 0
                ? $"los {deWpf.Count} colores de WPF están todos en el tema de Avalonia"
                : $"faltan {faltan.Count} colores en Avalonia: {string.Join(", ", faltan.Take(6))}");

        // ── Y ninguno se inventa ──────────────────────────────────────────────────
        // Un color de más en el tema nuevo es uno que nadie decidió: o sobra, o es que
        // alguien lo añadió allí y se olvidó del otro lado.
        var sobran = deAvalonia.Keys.Where(k => !deWpf.ContainsKey(k)).ToList();
        Program.Assert(sobran.Count == 0,
            sobran.Count == 0
                ? "y no hay ninguno de más, que sería uno que nadie decidió"
                : $"sobran {sobran.Count} en Avalonia: {string.Join(", ", sobran.Take(6))}");

        // ══ LO QUE IMPORTA: que sean EL MISMO color ══════════════════════════════
        var distintos = deWpf
            .Where(p => deAvalonia.TryGetValue(p.Key, out var c) &&
                        !c.Equals(p.Value, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"{p.Key} ({p.Value} contra {deAvalonia[p.Key]})")
            .ToList();

        Program.Assert(distintos.Count == 0,
            distintos.Count == 0
                ? "y todos valen exactamente lo mismo en las dos"
                : $"{distintos.Count} colores se han separado: {string.Join(" · ", distintos.Take(4))}. " +
                  "La app tendría dos aspectos según con qué interfaz se abra, y eso no falla en ningún sitio.");
    }

    private static Dictionary<string, string> Leer(string ruta) =>
        Pincel.Matches(File.ReadAllText(ruta))
              .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "src", "Ondine"))
                               && Directory.Exists(Path.Combine(d.FullName, "src", "Ondine.Core"))))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
