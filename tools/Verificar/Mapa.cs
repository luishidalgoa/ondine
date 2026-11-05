namespace Ondine.Verificar;

/// <summary>
/// Paso 3: el mapa de funcionalidades (docs/funcionalidades) coincide con el código.
///
/// <para>
/// Cada ficha es un bloque de funcionalidad con su cabecera:
/// <code>
/// ---
/// funcion: Cola de trabajos y panel Comprimir
/// modulos:
///   - src/Ondine.Core/Trabajos/
///   - src/Ondine.Avalonia/VentanaPrincipal.axaml.cs
/// pruebas:
///   - tests/Reindex.Tests/ColaDeTrabajosTests.cs
/// tareas:
///   - ROADMAP.md › Flujo de trabajo
/// ---
/// </code>
/// </para>
/// <para>
/// Qué falla y qué avisa. Falla si el mapa dice algo que no existe (un módulo o una prueba
/// renombrados sin actualizar la ficha): el mapa miente y eso es un fallo. Avisa, sin fallar, si
/// un módulo cambia en esta rama y su ficha no, y si hay código del motor, la CLI o el MCP que
/// todavía no tiene ficha.
/// </para>
/// </summary>
public static class Mapa
{
    public const string Carpeta = "docs/funcionalidades";

    private sealed record Ficha(string Ruta, string Funcion, IReadOnlyList<string> Modulos, IReadOnlyList<string> Pruebas);

    public static IReadOnlyList<Hallazgo> Correr(Contexto ctx)
    {
        var h = new List<Hallazgo>();

        if (!Directory.Exists(ctx.Ruta(Carpeta)))
            return [Hallazgo.Falla($"no existe {Carpeta}/: el mapa de funcionalidades no está")];
        if (!File.Exists(ctx.Ruta($"{Carpeta}/README.md")))
            h.Add(Hallazgo.Falla($"falta {Carpeta}/README.md, el índice del mapa"));

        var fichas = Directory.GetFiles(ctx.Ruta(Carpeta), "*.md")
            .Where(f => Path.GetFileName(f) != "README.md")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => Analizar(ctx, f, h))
            .OfType<Ficha>()
            .ToList();

        if (fichas.Count == 0)
            h.Add(Hallazgo.Falla("no hay ninguna ficha en el mapa"));

        // 1) Lo que dice el mapa tiene que existir. Si no, el mapa miente.
        foreach (var f in fichas)
        {
            foreach (var m in f.Modulos)
            {
                var esCarpeta = m.EndsWith('/');
                var ruta = ctx.Ruta(m.TrimEnd('/'));
                var existe = esCarpeta ? Directory.Exists(ruta) : File.Exists(ruta);
                if (!existe)
                    h.Add(Hallazgo.Falla($"{f.Ruta}: el módulo «{m}» no existe; la ficha está desfasada"));
            }
            foreach (var p in f.Pruebas)
                if (!File.Exists(ctx.Ruta(p)))
                    h.Add(Hallazgo.Falla($"{f.Ruta}: la prueba «{p}» no existe; la ficha está desfasada"));
        }

        // 2) Lo que cambia tiene que dejar rastro en su ficha. Solo avisa.
        var cambiados = CambiadosRespectoA(ctx);
        foreach (var ficha in fichas)
        {
            if (cambiados.Contains(ficha.Ruta)) continue;   // la ficha ya va en el cambio: bien
            var tocados = cambiados.Where(c => ficha.Modulos.Any(m => Cubre(m, c))).OrderBy(c => c, StringComparer.Ordinal).ToList();
            if (tocados.Count > 0)
                h.Add(Hallazgo.Aviso($"{ficha.Ruta} no se ha actualizado, pero su código ha cambiado: {string.Join(", ", tocados)}"));
        }

        // 3) Código del motor, la CLI y el MCP que todavía no tiene ficha. Solo avisa: es el
        //    trabajo pendiente del mapa, no un error de este cambio.
        var modulos = fichas.SelectMany(f => f.Modulos).ToList();
        var huecos = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var raiz in new[] { "src/Ondine.Core/", "src/Ondine.Mcp/", "src/Ondine.Cli/" })
        {
            foreach (var f in Proceso.Git(ctx.Raiz, ["ls-files", "--", raiz]).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var p = f.Trim().Replace('\\', '/');
                var resto = p[raiz.Length..];
                var primero = resto.Split('/')[0];
                var unidad = resto.Contains('/') ? raiz + primero + "/" : p;
                if (unidad.Contains("/bin/") || unidad.Contains("/obj/")) continue;
                if (!modulos.Any(m => Cubre(m, unidad)))
                    huecos.Add(unidad);
            }
        }
        if (huecos.Count > 0)
            h.Add(Hallazgo.Aviso($"{huecos.Count} trozo(s) del código sin ficha: {string.Join(", ", huecos.Take(25))}{(huecos.Count > 25 ? " …" : "")}"));

        if (h.All(x => x.Nivel != Nivel.Falla))
            h.Insert(0, Hallazgo.Pasa($"{fichas.Count} fichas; sus módulos y pruebas existen en el código"));

        return h;
    }

    private static Ficha? Analizar(Contexto ctx, string ruta, List<Hallazgo> h)
    {
        var rel = Carpeta + "/" + Path.GetFileName(ruta);
        var lineas = File.ReadAllLines(ruta);
        if (lineas.Length < 3 || lineas[0].Trim() != "---")
        {
            h.Add(Hallazgo.Falla($"{rel}: no empieza con su cabecera (---)"));
            return null;
        }
        var fin = Array.FindIndex(lineas, 1, l => l.Trim() == "---");
        if (fin < 0)
        {
            h.Add(Hallazgo.Falla($"{rel}: la cabecera no se cierra (---)"));
            return null;
        }

        var funcion = "";
        var listas = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        string? clave = null;
        for (int i = 1; i < fin; i++)
        {
            var l = lineas[i];
            if (l.StartsWith("  - ", StringComparison.Ordinal))
            {
                if (clave is not null && listas.TryGetValue(clave, out var lista))
                    lista.Add(l[4..].Trim());
                continue;
            }
            var c = l.IndexOf(':');
            if (c <= 0) continue;
            var k = l[..c].Trim();
            var v = l[(c + 1)..].Trim();
            clave = k;
            if (k == "funcion") funcion = v;
            else listas[k] = [];
        }

        if (funcion.Length == 0) h.Add(Hallazgo.Falla($"{rel}: falta la clave «funcion»"));
        var modulos = listas.GetValueOrDefault("modulos") ?? [];
        if (modulos.Count == 0) h.Add(Hallazgo.Falla($"{rel}: la ficha no lista ningún módulo"));

        return new Ficha(rel, funcion, modulos, listas.GetValueOrDefault("pruebas") ?? []);
    }

    /// <summary>Un módulo cubre un fichero si es ese fichero, o si es una carpeta que lo contiene.</summary>
    private static bool Cubre(string modulo, string fichero) =>
        modulo.EndsWith('/') ? fichero.StartsWith(modulo, StringComparison.Ordinal)
                             : string.Equals(modulo, fichero, StringComparison.Ordinal);

    /// <summary>Lo que cambia en esta rama respecto a la base: lo modificado y lo nuevo sin añadir.</summary>
    private static HashSet<string> CambiadosRespectoA(Contexto ctx)
    {
        var rutas = Proceso.Git(ctx.Raiz, "diff", "--name-only", ctx.Rama)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Concat(Proceso.Git(ctx.Raiz, "ls-files", "--others", "--exclude-standard")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(p => p.Trim().Replace('\\', '/'));
        return new HashSet<string>(rutas, StringComparer.Ordinal);
    }
}
