namespace Ondine.Reindex.Tests;

/// <summary>
/// El arnés de la documentación.
///
/// <para>
/// Existe por un accidente concreto: en el cambio de nombre a Ondine, 85 ficheros a
/// la vez, unas cuantas docs se desprendieron de su método y quedaron pegadas al
/// siguiente miembro. El resultado compila, no molesta a nadie y miente: el bloque
/// describe algo que ese método no hace, y quien lo lee se lo cree. Aparecieron 23
/// sitios así, y ninguna revisión humana los habría encontrado leyendo diffs.
/// </para>
/// <para>
/// No comprueba que la documentación sea BUENA -eso no lo juzga una máquina- sino
/// que cada bloque tenga UN dueño. Dos descripciones pegadas significan que una de
/// las dos habla de otra cosa.
/// </para>
/// </summary>
public static class DocumentacionTests
{
    private static readonly string Raiz = LocalizarRaiz();

    public static void Todas()
    {
        Program.Seccion("Documentación (que cada bloque tenga un dueño)");
        NingunBloqueLlevaDosDescripciones();
    }

    /// <summary>
    /// Un bloque de documentación con dos aperturas de descripción es un bloque con
    /// una doc huérfana dentro: C# solo reconoce la primera, así que la segunda -o la
    /// primera, según cuál sea la desplazada- no describe nada de lo que hay debajo.
    /// </summary>
    private static void NingunBloqueLlevaDosDescripciones()
    {
        // Solo se miran las líneas de documentación, así que este literal no cuenta.
        const string apertura = "<summary>";
        var culpables = new List<string>();

        foreach (var fichero in FuentesCs())
        {
            var lineas = File.ReadAllLines(fichero);
            for (int i = 0; i < lineas.Length;)
            {
                if (!EsDoc(lineas[i])) { i++; continue; }

                int inicio = i, cuantas = 0;
                while (i < lineas.Length && EsDoc(lineas[i]))
                    cuantas += Apariciones(lineas[i++], apertura);

                if (cuantas > 1)
                    culpables.Add($"{Relativa(fichero)}:{inicio + 1} ({cuantas})");
            }
        }

        Program.Assert(culpables.Count == 0,
            culpables.Count == 0
                ? "ningún bloque de documentación lleva dos descripciones"
                : $"{(culpables.Count == 1 ? "1 bloque" : $"{culpables.Count} bloques")} con documentación huérfana dentro: " +
                  string.Join(" · ", culpables.Take(6)) +
                  (culpables.Count > 6 ? $" (y {culpables.Count - 6} más)" : ""));
    }

    private static bool EsDoc(string linea) => linea.TrimStart().StartsWith("///");

    private static int Apariciones(string linea, string aguja)
    {
        int n = 0;
        for (int i = linea.IndexOf(aguja, StringComparison.Ordinal); i >= 0;
                 i = linea.IndexOf(aguja, i + aguja.Length, StringComparison.Ordinal))
            n++;
        return n;
    }

    private static string Relativa(string ruta) =>
        Raiz.Length > 0 && ruta.StartsWith(Raiz, StringComparison.Ordinal)
            ? ruta[(Raiz.Length + 1)..].Replace(Path.DirectorySeparatorChar, '/')
            : Path.GetFileName(ruta);

    /// <summary>Sube hasta encontrar la raíz del repositorio, para no depender del cwd.</summary>
    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src", "Ondine")))
            d = d.Parent;
        return d?.FullName ?? "";
    }

    /// <summary>
    /// Todo el C# del repositorio, motor y pruebas. Las pruebas también: los dos
    /// ficheros de pruebas traían su propia doc huérfana.
    /// </summary>
    private static IEnumerable<string> FuentesCs()
    {
        foreach (var carpeta in new[] { "src", "tests" })
        {
            var raiz = Path.Combine(Raiz, carpeta);
            if (!Directory.Exists(raiz)) continue;

            foreach (var f in Directory.EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories))
                if (!f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                    yield return f;
        }
    }
}
