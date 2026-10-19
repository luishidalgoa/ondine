namespace Ondine.Reindex.Tests;

/// <summary>
/// Una serie repartida en varias carpetas (a veces en discos distintos) se carga como una
/// sola, y se puede elegir qué temporadas analizar.
///
/// <para>
/// Lo que importa aquí es lo que NO debe pasar: que un fichero salga dos veces, que una
/// temporada del disco B se cuente aparte de la misma del disco A, o que «reordenar» mande a
/// un fichero a la carpeta de otro disco —eso sería copiar gigas donde se esperaba renombrar—.
/// </para>
/// </summary>
public static class MulticargaTests
{
    private static readonly string[] Exts = [".mp4", ".mkv"];

    public static void Todas()
    {
        Program.Seccion("Multicarga y temporadas");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-multi-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var a = Path.Combine(raiz, "discoA", "Doraemon");
            var b = Path.Combine(raiz, "discoB", "Doraemon");
            Hacer(a, "Season 1979", "e01.mp4", "e02.mp4");
            Hacer(a, "Season 1980", "e10.mp4");
            Hacer(b, "Season 1979", "e03.mp4");           // la MISMA temporada, en el otro disco
            Hacer(b, "Season 1981", "e20.mkv", "e21.mkv");
            Hacer(b, "", "suelto.mp4");                    // en la raíz del disco B
            File.WriteAllText(Path.Combine(b, "notas.txt"), "no es un vídeo");

            Normalizar(a, b);
            Escaneo(a, b);
            Temporadas(a, b);
            Reordenado(a, b);
        }
        finally { try { Directory.Delete(raiz, recursive: true); } catch { } }
    }

    private static void Hacer(string raiz, string temporada, params string[] ficheros)
    {
        var dir = temporada.Length == 0 ? raiz : Path.Combine(raiz, temporada);
        Directory.CreateDirectory(dir);
        foreach (var f in ficheros) File.WriteAllText(Path.Combine(dir, f), "x");
    }

    private static void Normalizar(string a, string b)
    {
        var n = LibraryScan.NormalizarRaices([a + Path.DirectorySeparatorChar, "  ", null, a, b]);
        Program.Eq(2, n.Count, "las repetidas, las vacías y las nulas se quitan");

        var anidada = LibraryScan.NormalizarRaices([a, Path.Combine(a, "Season 1979")]);
        Program.Eq(1, anidada.Count, "una carpeta que cuelga de otra de la lista sobra: la de arriba ya la incluye");

        // «Doraemon2» NO cuelga de «Doraemon»: solo comparte el principio del nombre.
        var hermana = LibraryScan.NormalizarRaices([a, a + "2"]);
        Program.Eq(2, hermana.Count, "y una que solo empieza igual no es una subcarpeta");
    }

    private static void Escaneo(string a, string b)
    {
        var todos = LibraryScan.Escanear([a, b], Exts);
        Program.Eq(7, todos.Length, "se cargan los vídeos de las dos carpetas y solo los vídeos");
        Program.Eq(todos.Length, todos.Distinct(StringComparer.OrdinalIgnoreCase).Count(), "ninguno sale dos veces");

        var con = LibraryScan.Escanear([a, b, Path.Combine(a, "Season 1979")], Exts);
        Program.Eq(7, con.Length, "pasar además una subcarpeta ya incluida no duplica nada");

        // Las dos «Season 1979» salen juntas, no una por disco.
        var g79 = todos.Select((f, i) => (f, i))
            .Where(x => LibraryScan.Grupo(LibraryScan.RaizDe([a, b], x.f), x.f) == "Season 1979")
            .Select(x => x.i).ToList();
        Program.Eq(3, g79.Count, "la temporada 1979 trae los tres ficheros, de los dos discos");
        Program.Assert(g79.Max() - g79.Min() == 2, "y salen contiguos en la lista");

        Program.Assert(LibraryScan.Escanear([a], Exts).SequenceEqual(LibraryScan.Escanear(a, Exts)),
            "con una sola carpeta da exactamente lo mismo que antes");
        Program.Eq(0, LibraryScan.Escanear([Path.Combine(a, "no-existe")], Exts).Length,
            "una carpeta que no existe no rompe: no hay nada");
        Program.Eq(3, LibraryScan.Escanear([a, Path.Combine(a, "no-existe")], Exts).Length,
            "y entre varias, la que falta se salta sin perder las demás");

        var cualquiera = Path.Combine(b, "Season 1981", "e20.mkv");
        Program.Eq(b, LibraryScan.RaizDe([a, b], cualquiera), "cada fichero sabe de qué carpeta cuelga");
    }

    private static void Temporadas(string a, string b)
    {
        var raices = new[] { a, b };
        var todos = LibraryScan.Escanear(raices, Exts);
        var t = LibraryScan.Temporadas(raices, todos);

        Program.Eq(1979 + 0, SignalExtractor.TemporadaDeCarpeta(t[0].Grupo) ?? -1, "las temporadas salen en orden");
        Program.Eq(3, t.First(x => x.Grupo == "Season 1979").Cuenta, "la 1979 suma lo de los dos discos");
        Program.Assert(t.Any(x => x.Grupo.Length == 0 && x.Cuenta == 1), "y los sueltos de la raíz van aparte");

        var sin = LibraryScan.SinTemporadas(raices, todos, new HashSet<string> { "Season 1979", "Season 1981" });
        Program.Eq(2, sin.Length, "apartando dos temporadas quedan solo los de las demás (la 1980 y el suelto)");
        Program.Assert(sin.All(f => !f.Contains("Season 1979") && !f.Contains("Season 1981")), "y no queda ninguno de las apartadas");
        Program.Eq(todos.Length, LibraryScan.SinTemporadas(raices, todos, new HashSet<string>()).Length,
            "sin apartar nada se analiza todo");
    }

    private static void Reordenado(string a, string b)
    {
        var cat = ReindexCatalog.Parse("""
        { "esquema": "reindex/1.0", "serie": "S",
          "episodios": [ { "num": 1, "temporada": 1981, "titulos": { "es": ["Uno"] } } ] }
        """);

        // Un fichero suelto en la raíz del disco B, que el catálogo dice que es de 1981.
        var suelto = Path.Combine(b, "suelto.mp4");
        var res = new ReindexResolution
        {
            Archivo = SignalExtractor.Extract(suelto, "Doraemon"),
            Episodio = cat.PorNum(1),
            Estado = ReindexEstado.Corregido,
            Confianza = ReindexConfianza.Alta,
        };

        var plan = PlanDeReordenado.Montar([res], [a, b], enCastellano: false,
            existe: _ => false, hayOrigen: _ => true);

        Program.Eq(1, plan.Count, "un paso por fichero");
        Program.Eq(PlanDeReordenado.Porque.Va, plan[0].Motivo, "se mueve a su temporada");
        Program.Assert(plan[0].Destino!.StartsWith(b, StringComparison.OrdinalIgnoreCase),
            "y a la carpeta de SU disco, no a la de otro");
        Program.Assert(!plan[0].Destino!.StartsWith(a, StringComparison.OrdinalIgnoreCase),
            "nunca cruza de disco");
    }
}
