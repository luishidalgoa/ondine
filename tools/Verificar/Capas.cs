using System.Text.RegularExpressions;

namespace Ondine.Verificar;

/// <summary>
/// Paso 2: qué proyecto puede depender de cuál.
///
/// <para>
/// Las capas, de abajo arriba: el motor (Ondine.Core) no depende de nada del producto. Encima
/// están los frentes que usan el motor: la app de Windows (WPF), la de todas las plataformas
/// (Avalonia), la CLI y el servidor MCP. Ninguno de ellos depende de otro. Las pruebas pueden
/// tocar lo que quieran, porque comprueban precisamente eso.
/// </para>
/// <para>
/// Dos reglas: (1) una referencia entre proyectos solo vale si la tabla la permite; (2) el
/// código del motor, de la CLI y del MCP no puede traer la interfaz (Avalonia, WPF), aunque no
/// haya referencia de por medio, por ejemplo con un «using» o un nombre completo en el código.
/// </para>
/// <para>
/// Un proyecto que no está en la tabla falla. Así un proyecto nuevo no entra sin que alguien
/// decida en qué capa va.
/// </para>
/// </summary>
public static class Capas
{
    /// <summary>
    /// Qué proyectos puede referenciar cada uno. <c>null</c> = sin restricción (las pruebas).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]?> Tabla = new Dictionary<string, string[]?>
    {
        ["Ondine.Core"] = [],
        ["Ondine"] = ["Ondine.Core"],
        ["Ondine.Avalonia"] = ["Ondine.Core"],
        ["Ondine.Cli"] = ["Ondine.Core"],
        ["Ondine.Mcp"] = ["Ondine.Core"],
        ["Verificar"] = [],
        ["Reindex.Tests"] = null,
        ["Ui.Smoke"] = null,
    };

    // Carpetas de código que no pueden depender de la interfaz.
    private static readonly string[] CarpetasSinInterfaz = ["src/Ondine.Core", "src/Ondine.Mcp", "src/Ondine.Cli"];

    // Lo que es interfaz y no puede aparecer en el motor, la CLI ni el MCP.
    private static readonly string[] Interfaz = ["Avalonia", "System.Windows", "Microsoft.UI"];

    public static IReadOnlyList<Hallazgo> Correr(Contexto ctx)
    {
        var hallazgos = new List<Hallazgo>();
        int proyectosConCapa = 0;

        var proyectos = Proceso.Git(ctx.Raiz, "ls-files", "--", "*.csproj")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().Replace('\\', '/'))
            .ToList();

        foreach (var ruta in proyectos)
        {
            if (ruta.StartsWith("spike/", StringComparison.Ordinal))
            {
                hallazgos.Add(Hallazgo.Info($"{ruta}: fuera de las capas (es un prototipo, no parte del producto)"));
                continue;
            }

            var nombre = Path.GetFileNameWithoutExtension(ruta);
            if (!Tabla.TryGetValue(nombre, out var permitidos))
            {
                hallazgos.Add(Hallazgo.Falla($"{ruta}: el proyecto «{nombre}» no tiene capa definida; añádelo a la tabla en tools/Verificar/Capas.cs"));
                continue;
            }
            proyectosConCapa++;
            if (permitidos is null) continue;

            var xml = File.ReadAllText(ctx.Ruta(ruta));
            foreach (Match m in Regex.Matches(xml, "ProjectReference\\s+Include=\"([^\"]+)\""))
            {
                var referido = Path.GetFileNameWithoutExtension(m.Groups[1].Value.Replace('\\', '/'));
                if (!permitidos.Contains(referido))
                    hallazgos.Add(Hallazgo.Falla($"{nombre} referencia a {referido}, y su capa no puede (ver la tabla de capas)"));
            }
        }

        // El código de las capas bajas, sin la interfaz. Se mira línea a línea, sin comentarios.
        var ficheros = Proceso.Git(ctx.Raiz, ["ls-files", "--", .. CarpetasSinInterfaz])
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().Replace('\\', '/'))
            .Where(p => p.EndsWith(".cs", StringComparison.Ordinal))
            .ToList();

        foreach (var f in ficheros)
        {
            var lineas = File.ReadAllLines(ctx.Ruta(f));
            for (int i = 0; i < lineas.Length; i++)
            {
                var l = lineas[i].Trim();
                if (l.StartsWith("//", StringComparison.Ordinal) || l.StartsWith("*", StringComparison.Ordinal)
                    || l.StartsWith("/*", StringComparison.Ordinal)) continue;

                foreach (var prohibido in Interfaz)
                {
                    bool importa = l.StartsWith("using ", StringComparison.Ordinal) && l.Contains(prohibido);
                    bool nombra = l.Contains(prohibido + ".", StringComparison.Ordinal);
                    if (importa || nombra)
                        hallazgos.Add(Hallazgo.Falla($"{f}:{i + 1} usa «{prohibido}», y esta capa no puede depender de la interfaz"));
                }
            }
        }

        if (hallazgos.All(h => h.Nivel != Nivel.Falla))
            hallazgos.Insert(0, Hallazgo.Pasa($"{proyectosConCapa} proyectos con capa; {ficheros.Count} ficheros del motor, la CLI y el MCP revisados; ninguna dependencia prohibida"));

        return hallazgos;
    }
}
