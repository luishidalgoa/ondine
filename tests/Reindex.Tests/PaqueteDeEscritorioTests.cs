namespace Ondine.Reindex.Tests;

/// <summary>
/// El lanzador de escritorio de Linux dice la verdad sobre lo que la app abre.
///
/// <para>
/// El <c>.desktop</c> es lo que hace que Ondine salga en el menú y, sobre todo, en «Abrir
/// con» al pulsar con el botón derecho sobre un vídeo en <b>Nemo</b>, el gestor de Cinnamon
/// —o sea el de Linux Mint—. Es el equivalente de la casilla «menú del Explorador» de
/// Windows, pero declarado en el paquete en vez de escrito en el registro.
/// </para>
/// <para>
/// <b>Lo que se vigila es que no se separe del motor.</b> La lista de extensiones que la app
/// sabe abrir vive en <c>Engine.VideoExtensions</c>; la de tipos que el escritorio le ofrece
/// vive en un fichero de texto. Añadir una al motor y olvidarse de la otra <b>no rompe
/// nada</b>: la app abre ese vídeo perfectamente si se lo pasas, pero el sistema no la ofrece
/// para él y no hay forma de notarlo salvo probando con un fichero de cada tipo.
/// </para>
/// </summary>
public static class PaqueteDeEscritorioTests
{
    /// <summary>
    /// De extensión a tipo MIME. Escrito a mano porque no hay dónde consultarlo: los tipos
    /// los fija IANA y los gestores los reconocen por su base de datos, no por la extensión.
    /// </summary>
    private static readonly Dictionary<string, string> Tipos = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mkv"] = "video/x-matroska",
        [".mp4"] = "video/mp4",
        [".m4v"] = "video/x-m4v",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".webm"] = "video/webm",
        [".mpg"] = "video/mpeg",
        [".mpeg"] = "video/mpeg",
        [".flv"] = "video/x-flv",
    };

    public static void Todas()
    {
        Program.Seccion("El lanzador de escritorio de Linux");

        var raiz = LocalizarRaiz();
        ElPaqueteDeLinuxNoLlevaWindowsDentro(raiz);

        var lanzador = Path.Combine(raiz, "empaquetado", "linux", "ondine.desktop");
        if (!File.Exists(lanzador))
        {
            Program.Assert(false, "no encuentro empaquetado/linux/ondine.desktop");
            return;
        }

        var lineas = File.ReadAllLines(lanzador)
            .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
            .ToDictionary(l => l[..l.IndexOf('=')], l => l[(l.IndexOf('=') + 1)..], StringComparer.Ordinal);

        // ── Lo que la especificación exige ────────────────────────────────────
        foreach (var clave in new[] { "Type", "Name", "Exec", "Icon", "Categories" })
            Program.Assert(lineas.ContainsKey(clave) && lineas[clave].Length > 0,
                $"el lanzador declara «{clave}»");

        Program.Assert(lineas.GetValueOrDefault("Type") == "Application",
            "y es de tipo Application, que es lo que hace que salga en el menú");

        // El nombre de la clase de ventana. Sin esto, Cinnamon no sabe emparejar la ventana
        // abierta con el lanzador y el icono de la barra sale genérico —y anclarla al panel
        // deja de funcionar.
        Program.Assert(lineas.ContainsKey("StartupWMClass"),
            "y el nombre de la clase de ventana, o el icono de la barra sale genérico");

        // ── Que el Exec apunte a donde el paquete lo pone ─────────────────────
        var script = Path.Combine(raiz, "empaquetado", "linux", "hacer-deb.sh");
        Program.Assert(File.Exists(script), "el script del paquete está");

        var exec = lineas.GetValueOrDefault("Exec", "");
        var donde = exec.Split(' ')[0];
        Program.Assert(File.Exists(script) && File.ReadAllText(script).Contains(donde),
            $"y el lanzador apunta a donde el paquete instala la app ({donde})");

        // %F y no %f: la app acepta VARIOS ficheros a la vez, que es lo normal al marcar
        // media carpeta en el gestor y darle a «Abrir con». Con %f el sistema abriría una
        // instancia por fichero.
        Program.Assert(exec.EndsWith("%F"),
            $"y recibe varios ficheros de una vez, no uno por instancia ({exec})");

        // ══ LO QUE IMPORTA: que no se separe del motor ═══════════════════════
        var declarados = lineas.GetValueOrDefault("MimeType", "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Program.Assert(declarados.Count >= 5,
            $"el lanzador declara {declarados.Count} tipos de fichero");

        var sinTipo = Engine.VideoExtensions
            .Where(e => !Tipos.ContainsKey(e))
            .ToList();
        Program.Assert(sinTipo.Count == 0,
            sinTipo.Count == 0
                ? $"y esta comprobación conoce el tipo de las {Engine.VideoExtensions.Length} extensiones del motor"
                : $"{string.Join(", ", sinTipo)} no tienen tipo MIME apuntado aquí: añádelo a la tabla");

        var faltan = Engine.VideoExtensions
            .Where(e => Tipos.TryGetValue(e, out var t) && !declarados.Contains(t))
            .Select(e => $"{e} ({Tipos[e]})")
            .Distinct()
            .ToList();

        Program.Assert(faltan.Count == 0,
            faltan.Count == 0
                ? "y ofrece la app para TODAS las que el motor sabe abrir"
                : $"el motor abre {string.Join(", ", faltan)} y el escritorio no ofrece la app para eso. " +
                  "No rompe nada: la app los abre si se los pasas, pero el sistema no la propone.");

        // Y al revés: prometer un tipo que el motor no sabe abrir es peor, porque el sistema
        // sí ofrece la app y al pulsar no pasa nada.
        var sobran = declarados
            .Where(t => t != "inode/directory" && !Tipos.ContainsValue(t))
            .ToList();
        Program.Assert(sobran.Count == 0,
            sobran.Count == 0
                ? "ni promete tipos que el motor no sepa abrir"
                : $"{string.Join(", ", sobran)} se ofrecen y el motor no los abre");
    }

    /// <summary>
    /// Que el paquete de Linux no se lleve los binarios de Windows dentro.
    ///
    /// <para>
    /// <b>Esto pasó.</b> La condición del proyecto decía «si compilo en Windows O el destino
    /// es Windows», y compilar el paquete de Linux <i>desde</i> Windows —que es lo normal
    /// aquí— cumplía la primera mitad: la publicación para <c>linux-x64</c> se llevaba
    /// <b>283 MB de libVLC de Windows</b> dentro, la versión de arm64 incluida. 383 MB en vez
    /// de 101.
    /// </para>
    /// <para>
    /// No lo ve nadie leyendo el proyecto y no lo rompe nada: el paquete instala y funciona,
    /// solo que pesa cuatro veces lo que debe. Se vio publicando y midiendo la carpeta, así
    /// que lo que se comprueba aquí es <b>la forma de la condición</b>, que es lo único que
    /// se puede leer sin publicar.
    /// </para>
    /// </summary>
    private static void ElPaqueteDeLinuxNoLlevaWindowsDentro(string raiz)
    {
        var proyecto = Path.Combine(raiz, "src", "Ondine.Avalonia", "Ondine.Avalonia.csproj");
        if (!File.Exists(proyecto)) { Program.Assert(false, "no encuentro el proyecto de Avalonia"); return; }

        var texto = File.ReadAllText(proyecto);
        var linea = texto.Split('\n')
            .FirstOrDefault(l => l.Contains("VideoLAN.LibVLC.Windows"))
            ?? "";

        Program.Assert(linea.Length > 0 || texto.Contains("VideoLAN.LibVLC.Windows"),
            "los binarios de libVLC de Windows están declarados");

        // El bloque que los mete tiene que estar condicionado, y la condición tiene que
        // mirar el RID ANTES que el sistema. Sin el RID delante, publicar para Linux desde
        // Windows se los lleva.
        var condicion = texto.Split('\n')
            .Where(l => l.Contains("ItemGroup") && l.Contains("Condition"))
            .FirstOrDefault(l => l.Contains("RuntimeIdentifier")) ?? "";

        Program.Assert(condicion.Length > 0,
            "y el bloque que los mete está condicionado al destino");
        Program.Assert(condicion.Contains("RuntimeIdentifier.StartsWith('win')"),
            "la condición pregunta por el RID de destino");
        Program.Assert(condicion.Contains("'$(RuntimeIdentifier)' == ''"),
            "y solo cae a mirar el sistema cuando NO hay RID, o publicar para Linux desde " +
            "Windows se lleva 283 MB de binarios de Windows dentro");
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
