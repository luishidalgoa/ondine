using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Que el servidor MCP no se quede atrás cuando la app siga cambiando.
///
/// <para>
/// Es lo que pidió el usuario tal cual: «mantener el MCP con las actualizaciones que se hagan».
/// Y no se mantiene solo — se mantiene si algo <b>falla</b> cuando se olvida. Un servidor que
/// vive en un proyecto aparte es justo el que se queda con la versión de hace tres releases, con
/// una herramienta que ya no existe documentada, o fuera del paquete que se instala: nada de eso
/// da error al compilar.
/// </para>
/// <para>
/// Aquí se vigilan las cuatro formas en que se queda atrás, y las cuatro se han visto ya en este
/// proyecto en otras piezas: la <b>versión</b> que se desengancha, la <b>documentación</b> que
/// miente, el <b>paquete</b> que deja de llevarlo, y la herramienta que <b>escribe sin pedir
/// permiso</b>.
/// </para>
/// </summary>
public static class ElMcpNoSeQuedaAtrasTests
{
    public static void Todas()
    {
        Program.Seccion("El MCP no se queda atrás");

        LaVersionVaConLaDeLaApp();
        LaDocumentacionDiceLasQueHay();
        LosPaquetesDeEscritorioLoLlevan();
        NingunaEscribeSinPermiso();
        CadaEspejoTieneQuienLoVigile();
    }

    /// <summary>
    /// Cada superficie de la app que el MCP refleja tiene quien la vigile, <b>y las que no se
    /// reflejan todavía están declaradas</b>.
    ///
    /// <para>
    /// Esto salió de una pregunta del usuario: «en el mantenimiento, también hay que hacerlo de
    /// las cosas de Preferencias o las pestañas de Organizar». Tiene razón, y el problema no es
    /// escribir la comprobación de hoy: es que dentro de tres meses nadie recuerde que existía.
    /// Por eso el inventario vive AQUÍ, en el fichero del mantenimiento, y no repartido entre las
    /// pruebas de cada cosa.
    /// </para>
    /// <para>
    /// Dos formas de vigilar, según lo que haya:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Lo que YA se refleja se compara por reflexión contra el tipo del motor —los mandos de
    /// Comprimir contra <c>EncodeOptions</c>, las Preferencias contra <c>Settings</c>—, y aquí se
    /// comprueba que esa prueba sigue existiendo y sigue registrada. Una comprobación que alguien
    /// borra sin que nada chiste no vigila nada.
    /// </item>
    /// <item>
    /// Lo que NO se refleja todavía —la cola de trabajos, la calidad según el contenido— tiene
    /// que estar escrito en <c>docs/mcp.md</c>, en su apartado. Así el hueco es público en vez de
    /// vivir en la cabeza de quien lo dejó.
    /// </item>
    /// </list>
    /// </summary>
    private static void CadaEspejoTieneQuienLoVigile()
    {
        var raiz = LocalizarRaiz();
        var registro = Leer(Path.Combine(raiz, "tests", "Reindex.Tests", "Program.cs"));
        var doc = Leer(Path.Combine(raiz, "docs", "mcp.md"));

        // ── Lo que se refleja, con la prueba que lo compara contra el motor ──
        var vigilados = new (string Superficie, string Tipo, string Suite)[]
        {
            ("los mandos de Comprimir", nameof(EncodeOptions), "ComprimirPorMcpTests"),
            ("las Preferencias", nameof(Settings), "PreferenciasPorMcpTests"),
            ("las decisiones de Organizar", nameof(ReindexOverride), "OrganizarPorMcpTests"),
        };

        foreach (var (superficie, tipo, suite) in vigilados)
        {
            var fichero = Path.Combine(raiz, "tests", "Reindex.Tests", suite + ".cs");
            var texto = Leer(fichero);

            Program.Assert(texto.Length > 0, $"{superficie}: existe {suite}");
            Program.Assert(texto.Contains($"typeof({tipo}).GetProperties()"),
                $"y compara por reflexión contra «{tipo}», que es lo que caza un mando nuevo");
            Program.Assert(registro.Contains(suite + ".Todas()"),
                $"y está registrada en Program.cs, o no correría");
        }

        // ── Lo que todavía no se refleja, declarado en la documentación ──────
        // Recortes salió de esta lista al exponerse con «ondine_partir», y Organizar fila a fila
        // al exponerse con «ondine_fijar_episodio» y compañía: ese pasó a la lista de arriba, que
        // es a donde se pasa un hueco cerrado. Que la lista ENCOJA también hay que hacerlo a
        // mano, y por eso esta prueba se pone roja al cerrar uno — que es justo lo que se le pide.
        var pendientes = new (string Que, string Sena)[]
        {
            ("la cola de trabajos", "La cola de trabajos"),
            ("la calidad según el contenido", "La calidad según el contenido"),
        };

        Program.Assert(doc.Contains("## Lo que todavía no hace"),
            "docs/mcp.md tiene su apartado de lo que falta");

        foreach (var (que, sena) in pendientes)
            Program.Assert(doc.Contains(sena),
                $"y dice que falta {que}: un hueco escrito se arregla, uno recordado no");
    }

    private static string Leer(string ruta) => File.Exists(ruta) ? File.ReadAllText(ruta) : "";

    /// <summary>
    /// La misma versión que el resto, y comprobado <b>aquí</b> y no solo en el tag.
    ///
    /// <para>
    /// CI ya lo comprueba al publicar (<c>verificar-version</c>), pero eso avisa tarde: el
    /// desenganche se produce al cortar la versión y se descubre al empujar el tag, con el
    /// release a medias. Esta prueba lo dice en cuanto se corren las pruebas.
    /// </para>
    /// </summary>
    private static void LaVersionVaConLaDeLaApp()
    {
        var raiz = LocalizarRaiz();
        var versiones = Directory
            .GetFiles(Path.Combine(raiz, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(f => (Proyecto: Path.GetFileNameWithoutExtension(f), Version: VersionDe(f)))
            .Where(x => x.Version is not null)
            .ToList();

        Program.Assert(versiones.Count >= 5,
            $"encuentro los proyectos con versión declarada ({versiones.Count})");

        var distintas = versiones.Select(v => v.Version).Distinct().ToList();
        Program.Assert(distintas.Count == 1,
            "todos declaran la misma versión: "
            + string.Join(", ", versiones.Select(v => $"{v.Proyecto}={v.Version}")));

        Program.Assert(versiones.Any(v => v.Proyecto == "Ondine.Mcp"),
            "y el servidor MCP es uno de ellos: sin <Version> no habría nada que desengancharse");
    }

    /// <summary>
    /// La documentación dice <b>las que hay</b>: ni una de más ni una de menos.
    ///
    /// <para>
    /// Es el mismo truco que el <c>API_MAP</c> del backend. Una herramienta nueva sin documentar
    /// existe para el código y no para quien la va a usar; una documentada que ya no existe es
    /// peor, porque manda a alguien a llamar algo que no está. Las dos direcciones fallan.
    /// </para>
    /// </summary>
    private static void LaDocumentacionDiceLasQueHay()
    {
        var doc = Path.Combine(LocalizarRaiz(), "docs", "mcp.md");
        if (!File.Exists(doc)) { Program.Assert(false, "existe docs/mcp.md"); return; }

        var texto = File.ReadAllText(doc);

        // Los titulares «### ondine_algo», que es como se documenta cada una.
        var documentadas = texto.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("### ondine_"))
            .Select(l => l[4..].Trim())
            .ToHashSet();

        var reales = Catalogo.Todas.Select(h => h.Nombre).ToHashSet();

        var sinDocumentar = reales.Except(documentadas).ToList();
        var inventadas = documentadas.Except(reales).ToList();

        Program.Assert(sinDocumentar.Count == 0,
            $"todas las herramientas están en docs/mcp.md (faltan: {string.Join(", ", sinDocumentar)})");
        Program.Assert(inventadas.Count == 0,
            $"y el documento no cuenta ninguna que no exista (sobran: {string.Join(", ", inventadas)})");
    }

    /// <summary>
    /// Los paquetes de escritorio lo llevan dentro.
    ///
    /// <para>
    /// Esta es la que motivó todo: el usuario instaló Ondine y su agente no encontró ningún
    /// servidor MCP, porque el paquete no lo traía. Se mira contra <c>ondine-cli</c>, que ya
    /// viajaba dentro de los tres: donde va uno tiene que ir el otro, y así un empaquetador
    /// nuevo hereda la comprobación sin que haya que acordarse de tocarla.
    /// </para>
    /// </summary>
    private static void LosPaquetesDeEscritorioLoLlevan()
    {
        var raiz = LocalizarRaiz();
        var guiones = Directory
            .GetFiles(Path.Combine(raiz, "empaquetado"), "*.sh", SearchOption.AllDirectories)
            .Select(f => (Nombre: Path.GetFileName(f), Texto: File.ReadAllText(f)))
            .Where(g => g.Texto.Contains("ondine-cli"))
            .ToList();

        Program.Assert(guiones.Count == 3,
            $"encuentro los tres empaquetadores de escritorio ({guiones.Count})");

        foreach (var g in guiones)
            Program.Assert(g.Texto.Contains("Ondine.Mcp/Ondine.Mcp.csproj") && g.Texto.Contains("ondine-mcp"),
                $"{g.Nombre} publica el servidor MCP dentro del paquete");

        // Y que el .deb lo compruebe al montarse: un paquete al que se le cae un binario se
        // instala igual de bien y el fallo aparece meses después, en la máquina de alguien.
        var flujo = Path.Combine(raiz, ".github", "workflows", "build.yml");
        Program.Assert(File.Exists(flujo) && File.ReadAllText(flujo).Contains("./opt/ondine/ondine-mcp"),
            "y CI comprueba que el .deb lo trae dentro");

        // Windows va por otro camino -build.ps1 arma la carpeta que se traga Inno Setup-, así
        // que se mira aparte. Es el único de los cuatro donde el servidor cuesta dinero en
        // megas, y por eso mismo es el primero del que alguien lo quitaría «para adelgazar».
        var guion = Path.Combine(raiz, "build.ps1");
        var texto = File.Exists(guion) ? File.ReadAllText(guion) : "";
        Program.Assert(texto.Contains("Ondine.Mcp") && texto.Contains("ondine-mcp.exe"),
            "el instalador de Windows también lo lleva (build.ps1 lo publica en la carpeta del instalador)");
    }

    /// <summary>
    /// Ninguna herramienta escribe sin permiso, <b>y esto se comprueba en todas</b>.
    ///
    /// <para>
    /// Las pruebas de al lado ejercitan las dos que escriben hoy, una a una. Esta es la que
    /// cubre la de mañana: se llama a cada herramienta que declara escribir <b>sin argumentos</b>
    /// y la respuesta solo puede ser una de dos — un error por faltarle datos, o el ensayo
    /// diciendo lo que haría. Lo que no puede es contestar como si hubiera hecho algo.
    /// </para>
    /// </summary>
    private static void NingunaEscribeSinPermiso()
    {
        foreach (var h in Catalogo.Todas.Where(h => h.Escribe))
        {
            var r = h.Ejecutar(new System.Text.Json.Nodes.JsonObject());
            var prudente = r.EsError || r.Texto.Contains("SIN CONFIRMAR");

            Program.Assert(prudente,
                $"«{h.Nombre}» sin argumentos ni permiso no hace nada ({Recorte(r.Texto)})");
        }
    }

    private static string? VersionDe(string csproj)
    {
        foreach (var l in File.ReadAllLines(csproj))
        {
            var i = l.IndexOf("<Version>", StringComparison.Ordinal);
            if (i < 0) continue;
            var j = l.IndexOf("</Version>", StringComparison.Ordinal);
            if (j > i) return l[(i + 9)..j].Trim();
        }
        return null;
    }

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 70 ? l[..70] + "…" : l;

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }
}
