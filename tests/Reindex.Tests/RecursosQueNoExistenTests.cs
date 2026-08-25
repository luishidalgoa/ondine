using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Que no se pida ni un solo recurso que no exista.
///
/// <para>
/// Esto llega tarde y por eso está escrito con detalle: <b>la misma clase de fallo ha
/// aparecido tres veces</b> durante el puerto.
/// </para>
/// <list type="number">
/// <item>
/// Once colores de estado (<c>OrgOk</c>, <c>OrgWarn</c>…) no estaban portados, y la pantalla
/// de ordenar por temporadas se fusionó pintando <b>todas sus insignias en gris</b>.
/// </item>
/// <item>
/// El tema de la fila de sugerencias faltaba, y la lista salía con el azul de Fluent.
/// </item>
/// <item>
/// Las dos tipografías —<c>FontUI</c> y <c>FontMono</c>— se pedían en veinte sitios y
/// <b>no estaban definidas en ninguno</b>.
/// </item>
/// </list>
/// <para>
/// Ninguna de las tres dio un error. Un recurso que falta se resuelve a nada o a un valor
/// por defecto, la ventana abre igual, y lo que se ve es «así es la app». Los guardianes que
/// había miraban si el TEMA estaba portado; ninguno miraba si lo que las pantallas piden
/// existe. Esto sí.
/// </para>
/// <para>
/// Es texto contra texto, así que corre en CI sobre Linux como todo lo demás.
/// </para>
/// </summary>
public static class RecursosQueNoExistenTests
{
    /// <summary>
    /// Los que los pone Avalonia y no este proyecto. Se declaran para que la comprobación no
    /// tenga que conocer el catálogo entero de Fluent, y para que añadir uno sea deliberado.
    /// </summary>
    private static readonly HashSet<string> DeAvalonia = new(StringComparer.Ordinal)
    {
        // De momento ninguno: todo lo que se usa es de la app. Si algún día se usa uno de
        // Fluent, va aquí con su nombre y no se apaga la comprobación entera.
    };

    public static void Todas()
    {
        Program.Seccion("Ningún recurso pedido se queda sin definir");

        var raiz = LocalizarRaiz();
        var carpeta = Path.Combine(raiz, "src", "Ondine.Avalonia");
        if (!Directory.Exists(carpeta))
        {
            Program.Assert(false, "no encuentro el proyecto de Avalonia");
            return;
        }

        var axaml = Directory.GetFiles(carpeta, "*.axaml", SearchOption.AllDirectories);
        Program.Assert(axaml.Length >= 10,
            $"se miran los {axaml.Length} ficheros de interfaz: si fueran dos, esto no mediría nada");

        // ── Lo que se define ──────────────────────────────────────────────────
        var definidos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in axaml)
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"x:Key=""([A-Za-z][A-Za-z0-9]*)"""))
                definidos.Add(m.Groups[1].Value);

        Program.Assert(definidos.Count >= 30,
            $"y los {definidos.Count} recursos que declaran");

        // ── Lo que se pide ────────────────────────────────────────────────────
        var pedidos = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var f in axaml)
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"\{StaticResource\s+([A-Za-z][A-Za-z0-9]*)\s*\}"))
                pedidos.TryAdd(m.Groups[1].Value, Path.GetFileName(f));

        Program.Assert(pedidos.Count >= 20,
            $"contra los {pedidos.Count} que se piden desde las pantallas");

        // ══ LO QUE IMPORTA ═══════════════════════════════════════════════════
        var huerfanos = pedidos
            .Where(p => !definidos.Contains(p.Key) && !DeAvalonia.Contains(p.Key))
            .OrderBy(p => p.Key)
            .ToList();

        Program.Assert(huerfanos.Count == 0,
            huerfanos.Count == 0
                ? $"y los {pedidos.Count} existen: ninguna pantalla pide algo que no está"
                : $"{huerfanos.Count} recursos se piden y no existen: " +
                  string.Join(", ", huerfanos.Take(8).Select(p => $"{p.Key} (en {p.Value})")) +
                  ". No dan error: la ventana abre igual y se ve mal, y eso pasa por ser el diseño.");

        // ── Y las tipografías, que son el caso que lo destapó ─────────────────
        // Se comprueban aparte porque su fallo es el más silencioso de todos: una fuente que
        // no existe se sustituye por la de serie sin decir nada, así que en Windows se
        // parecía por casualidad y en Linux habría salido todo con la misma.
        foreach (var fuente in new[] { "FontUI", "FontMono" })
            Program.Assert(definidos.Contains(fuente),
                $"{fuente} está definida: una fuente que falta se sustituye en silencio");
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
