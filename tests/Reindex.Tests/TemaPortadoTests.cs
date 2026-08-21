using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Cuánto del tema de WPF queda por portar, dicho en voz alta.
///
/// <para>
/// El puerto a Avalonia va por trozos y va a durar. El riesgo de eso no es equivocarse: es
/// <b>perder la cuenta</b>. Un estilo que no se porte no rompe nada —la pantalla que lo use
/// todavía no existe en Avalonia— así que no hay ningún momento en el que alguien se dé
/// cuenta de que falta.
/// </para>
/// <para>
/// Esto no exige portarlo todo. Exige que <b>cada estilo esté en una de dos listas</b>:
/// portado, o pendiente con su motivo. Si aparece uno nuevo en el tema de WPF y nadie dice
/// qué se hace con él, esta comprobación se planta.
/// </para>
/// <para>
/// Como <see cref="TemaCompartidoTests"/>, no necesita Avalonia: son ficheros de texto.
/// </para>
/// </summary>
public static class TemaPortadoTests
{
    /// <summary>
    /// Los que NO se portan, y por qué. Un hueco declarado se rellena; uno que nadie ha
    /// escrito, no.
    /// </summary>
    private static readonly Dictionary<string, string> Pendientes = new()
    {
        // Estos cuatro visten un ListView + GridView de WPF. En Avalonia esa pantalla NO
        // va a ser un ListView: el spike ya estableció que la sustituye un DataGrid, que
        // es otro control con otras partes. Portar su tema ahora sería trabajo tirado.
        ["TableView"] = "la tabla pasa a ser un DataGrid: otro control, otro tema",
        ["RowStyle"] = "idem, va con el DataGrid",
        ["ColHeader"] = "idem, va con el DataGrid",
        ["SuggestItem"] = "va con la caja de sugerencias, en su pantalla",

        // Las barras de desplazamiento de Avalonia ya son finas y oscuras con el tema
        // Fluent. Portar las de WPF antes de ver si hacen falta es adornar a ciegas.
        ["ScrollThumbV"] = "Fluent ya trae barras finas; se mira cuando haya una pantalla larga",
        ["ScrollThumbH"] = "idem",
        ["ScrollNoop"] = "idem",

        // Estos tres son de Recortes y del reproductor, que van con LibVLC en la Fase 4.
        ["TimelineSlider"] = "va con Recortes, en la Fase 4",
        ["BarraVideo"] = "va con el reproductor, en la Fase 4",
        ["BarraVolumen"] = "va con el reproductor, en la Fase 4",
        ["BtnIcono"] = "va con el reproductor, en la Fase 4",
        ["BtnPlay"] = "va con el reproductor, en la Fase 4",

        // La barra de título propia. Se decide al portar la ventana principal, porque en
        // Avalonia no es WindowChrome sino ExtendClientAreaToDecorationsHint.
        ["BtnTitle"] = "la barra de título propia se resuelve distinto en Avalonia",
        ["BtnClose"] = "idem",

        ["ChipStyle"] = "los chips van con la pantalla de Organizar",
        ["InlineTextBox"] = "va con la edición en línea de la tabla",
        ["TextoRecortable"] = "un TextBlock con recorte; se resuelve con un Setter suelto",
        ["HazDeBorde"] = "ya está dentro del ControlTheme de los botones, no hace falta suelto",
    };

    public static void Todas()
    {
        Program.Seccion("Cuánto del tema queda por portar");

        var raiz = LocalizarRaiz();
        var wpf = Path.Combine(raiz, "src", "Ondine", "Theme.xaml");
        var temas = Path.Combine(raiz, "src", "Ondine.Avalonia", "Temas");

        if (!File.Exists(wpf) || !Directory.Exists(temas))
        {
            Program.Assert(false, "no encuentro el tema de WPF o la carpeta de temas de Avalonia");
            return;
        }

        var enWpf = Regex.Matches(File.ReadAllText(wpf), @"<Style\s+x:Key=""([A-Za-z][A-Za-z0-9]*)""")
                         .Select(m => m.Groups[1].Value).Distinct().ToList();

        var portado = string.Concat(Directory.GetFiles(temas, "*.axaml").Select(File.ReadAllText));
        var enAvalonia = Regex.Matches(portado, @"<ControlTheme\s+x:Key=""([A-Za-z][A-Za-z0-9]*)""")
                              .Select(m => m.Groups[1].Value).ToHashSet();

        Program.Assert(enWpf.Count >= 20,
            $"el tema de WPF tiene {enWpf.Count} estilos con nombre: si fueran tres, esto no mediría nada");

        // ══ Cada uno, en una de las dos listas ═══════════════════════════════════
        var sinDecidir = enWpf
            .Where(k => !enAvalonia.Contains(k) && !Pendientes.ContainsKey(k))
            .ToList();

        Program.Assert(sinDecidir.Count == 0,
            sinDecidir.Count == 0
                ? $"los {enWpf.Count} estilos están decididos: {enAvalonia.Count} portados y {Pendientes.Count} pendientes con su motivo"
                : $"{sinDecidir.Count} estilos sin decidir: {string.Join(", ", sinDecidir.Take(6))}. " +
                  "Pórtalos, o apúntalos como pendientes con el motivo — un hueco que nadie ha escrito no se rellena.");

        // ── Y la lista de pendientes no se queda rancia ───────────────────────────
        // Uno que ya se portó pero sigue apuntado como pendiente hace que la cuenta
        // mienta hacia el lado malo: parece que queda más de lo que queda.
        var yaHechos = Pendientes.Keys.Where(enAvalonia.Contains).ToList();
        Program.Assert(yaHechos.Count == 0,
            yaHechos.Count == 0
                ? "y ninguno está en las dos listas a la vez"
                : $"{string.Join(", ", yaHechos)} ya están portados: quítalos de la lista de pendientes");

        // ── Ni se inventa pendientes que no existen ───────────────────────────────
        var fantasmas = Pendientes.Keys.Where(k => !enWpf.Contains(k)).ToList();
        Program.Assert(fantasmas.Count == 0,
            fantasmas.Count == 0
                ? "ni hay pendientes de estilos que ya no existen en WPF"
                : $"{string.Join(", ", fantasmas)} no están en Theme.xaml: sobran de la lista");
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "src", "Ondine"))
                               && Directory.Exists(Path.Combine(d.FullName, "src", "Ondine.Core"))))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
