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

        // Las barras de desplazamiento de Avalonia ya son finas y oscuras con el tema
        // Fluent. Portar las de WPF antes de ver si hacen falta es adornar a ciegas.
        ["ScrollThumbV"] = "Fluent ya trae barras finas; se mira cuando haya una pantalla larga",
        ["ScrollThumbH"] = "idem",
        ["ScrollNoop"] = "idem",

        ["ChipStyle"] = "los chips van con la pantalla de Organizar",
        ["InlineTextBox"] = "la caja normal ya está; esta es la variante de edición en línea de la tabla",
        ["TextoRecortable"] = "un TextBlock con recorte; se resuelve con un Setter suelto",
        ["HazDeBorde"] = "ya está dentro del ControlTheme de los botones, no hace falta suelto",

        // ── Los doce de ThemeOrganizar.xaml ───────────────────────────────────────
        // Aparecieron todos de golpe al empezar a mirar el segundo diccionario. Casi
        // todos son de la pantalla de Organizar, que es la última y la más grande del
        // puerto; los dos que hacen falta antes están dichos.
        ["EvidenciaMas"] = "va con el detalle de fila de Organizar",
        ["EvidenciaMenos"] = "idem",


    };

    /// <summary>
    /// Los que NO se traducen porque su control no existe en Avalonia, y lo que hace su
    /// trabajo ahora.
    ///
    /// <para>
    /// Esta tercera lista se añadió al portar la lista de comprimir, y hacía falta: había
    /// solo dos casillas —«portado» o «pendiente»— y ninguna decía la verdad de un estilo que
    /// se <b>rehizo con otro nombre</b>. Marcarlo como portado obligaba a llamar al tema
    /// nuevo igual que al viejo, mintiendo sobre lo que es; dejarlo como pendiente hacía que
    /// la cuenta dijera que queda más de lo que queda.
    /// </para>
    /// <para>
    /// El valor no es un comentario: es <b>el nombre del que lo sustituye</b>, y se comprueba
    /// que exista. Así «rehecho» no puede quedarse en una promesa.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> Rehechos = new()
    {
        // El ListView + GridView de la lista de comprimir. Este es el caso en que aquel
        // motivo era verdad: en Avalonia ese par no existe y la tabla pasa a DataGrid, que
        // es otro control con otras partes. (En Organizar NO lo era, y estuvo mal apuntado
        // un tiempo: allí ya había un DataGrid en WPF.)
        ["TableView"] = "TablaLista",
        ["RowStyle"] = "FilaLista",
        ["ColHeader"] = "CabeceraLista",
    };

    public static void Todas()
    {
        Program.Seccion("Cuánto del tema queda por portar");

        var raiz = LocalizarRaiz();
        var temas = Path.Combine(raiz, "src", "Ondine.Avalonia", "Temas");

        // LOS DOS diccionarios, no solo Theme.xaml. Esto empezó mirando uno, y así estuvo
        // hasta que al portar la pantalla del encargo aparecieron dos estilos —OrgLabel y
        // FiltroChip— que el termómetro decía tener todos decididos y no había visto nunca:
        // viven en ThemeOrganizar.xaml. Doce estilos, la tercera parte del tema, fuera de
        // la cuenta. Un termómetro que mide media habitación no avisa de nada, y encima
        // tranquiliza. Se localizan por patrón para que un tercer diccionario entre solo.
        var wpf = Directory.GetFiles(Path.Combine(raiz, "src", "Ondine"), "Theme*.xaml")
                           .OrderBy(f => f).ToList();

        if (wpf.Count == 0 || !Directory.Exists(temas))
        {
            Program.Assert(false, "no encuentro los temas de WPF o la carpeta de temas de Avalonia");
            return;
        }

        Program.Assert(wpf.Count >= 2,
            $"se miran los {wpf.Count} diccionarios de tema de WPF: {string.Join(", ", wpf.Select(Path.GetFileName))}");

        var enWpf = wpf.SelectMany(f =>
                Regex.Matches(File.ReadAllText(f), @"<Style\s+x:Key=""([A-Za-z][A-Za-z0-9]*)""")
                     .Select(m => m.Groups[1].Value))
            .Distinct().ToList();

        var portado = string.Concat(Directory.GetFiles(temas, "*.axaml").Select(File.ReadAllText));
        var enAvalonia = Regex.Matches(portado, @"<ControlTheme\s+x:Key=""([A-Za-z][A-Za-z0-9]*)""")
                              .Select(m => m.Groups[1].Value).ToHashSet();

        Program.Assert(enWpf.Count >= 30,
            $"el tema de WPF tiene {enWpf.Count} estilos con nombre: si fueran tres, esto no mediría nada");

        // ══ Cada uno, en una de las dos listas ═══════════════════════════════════
        var sinDecidir = enWpf
            .Where(k => !enAvalonia.Contains(k)
                     && !Pendientes.ContainsKey(k)
                     && !Rehechos.ContainsKey(k))
            .ToList();

        Program.Assert(sinDecidir.Count == 0,
            sinDecidir.Count == 0
                ? $"los {enWpf.Count} estilos de los {wpf.Count} diccionarios están decididos: " +
                  $"{enAvalonia.Count} portados, {Rehechos.Count} rehechos con otro nombre y " +
                  $"{Pendientes.Count} pendientes con su motivo"
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

        // ── Un «rehecho» tiene que existir de verdad ──────────────────────────────
        // Sin esto, «rehecho como TablaLista» sería una promesa: bastaría escribir el nombre
        // para que la cuenta lo diera por hecho.
        var prometidos = Rehechos
            .Where(r => !enAvalonia.Contains(r.Value))
            .ToList();
        Program.Assert(prometidos.Count == 0,
            prometidos.Count == 0
                ? $"y los {Rehechos.Count} rehechos apuntan a un tema que existe de verdad"
                : $"{string.Join(", ", prometidos.Select(r => $"{r.Key} dice ser {r.Value}"))}, " +
                  "y ese tema no está: un «rehecho» que no existe es un pendiente disfrazado");

        // Y no puede estar a la vez portado y rehecho.
        var dobles = Rehechos.Keys.Where(enAvalonia.Contains).ToList();
        Program.Assert(dobles.Count == 0,
            dobles.Count == 0
                ? "ni hay ninguno portado y rehecho a la vez"
                : $"{string.Join(", ", dobles)} están portados con su nombre: quítalos de los rehechos");

        // ── Ni se inventa pendientes que no existen ───────────────────────────────
        var fantasmas = Pendientes.Keys.Concat(Rehechos.Keys)
            .Where(k => !enWpf.Contains(k)).ToList();
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
