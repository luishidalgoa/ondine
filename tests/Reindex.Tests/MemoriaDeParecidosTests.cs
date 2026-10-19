using System.Text;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La memoria de parecidos: ahorra trabajo SIN cambiar ni una respuesta, y nunca se queda vieja.
///
/// <para>
/// Una caché que acelera pero a veces contesta distinto es peor que no tenerla, y la
/// tentación de las cachés es esa. Aquí se prueban las dos cosas que la hacen segura:
/// </para>
/// <list type="number">
///   <item>Con memoria —vacía, a medio llenar o llena— el motor da EXACTAMENTE lo mismo que sin ella.</item>
///   <item>Tocar el catálogo la invalida sola, y un fichero roto o ajeno se ignora entero.</item>
/// </list>
/// </summary>
public static class MemoriaDeParecidosTests
{
    private static ReindexCatalog Catalogo(string tituloExtra = "") => ReindexCatalog.Parse($$"""
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 1, "temporada": 1979, "titulos": { "es": ["La caja de las fichas predictoras"] } },
        { "num": 2, "temporada": 1979, "titulos": { "es": ["Doraemon vuelve a casa"] } },
        { "num": 3, "temporada": 1980, "titulos": { "es": ["El controlador del mar", "Alquiler estilo futurista{{tituloExtra}}"] } },
        { "num": 4, "temporada": 1980, "titulos": { "es": ["La fiesta de cumpleanos de Nobita"] } },
        { "num": 5, "temporada": 1981, "titulos": { "es": ["Parte una", "Parte dos"] } }
      ]
    }
    """);

    private static readonly string[] Nombres =
    [
        "Doraemon - S1979E01 - La caja de las fichas predictoras.mp4",
        "Doraemon | Doraemon vuelve a casa | Episodio 2.mp4",
        "Doraemon - S1980E03 - El controlador del mar.mp4",
        "Doraemon - S1980E99 - Un episodio que no existe en el catalogo.mp4",
        "Doraemon - S1981E05 - Parte una.mp4",
        "Doraemon - La fiesta de cumpleanos de Nobita.mp4",
    ];

    private static List<FileSignals> Senales() =>
        Nombres.Select(n => SignalExtractor.Extract(Path.Combine("/tv", "Season 1980", n), "Season 1980")).ToList();

    public static void Todas()
    {
        Program.Seccion("Memoria de parecidos");

        DaLoMismoQueSinMemoria();
        UnCatalogoEditadoLaInvalida();
        UnFicheroRotoSeIgnoraEntero();
        VaYVuelveDeDisco();
    }

    private static string Foto(IEnumerable<ReindexResolution> rs) =>
        string.Join("\n", rs.Select(r =>
            $"{r.Archivo.NombreArchivo}|{r.Estado}|{r.Confianza}|{r.Hint}|{r.Episodio?.Num}|{r.Score:R}|{r.Motivo}|" +
            string.Join(",", r.Alternativas.Select(a => $"{a.Episodio.Num}:{a.Score:R}"))));

    private static void DaLoMismoQueSinMemoria()
    {
        var cat = Catalogo();
        var sinMemoria = Foto(ReindexEngine.Resolve(Senales(), cat));

        var memoria = MemoriaDeParecidos.Para(cat);
        var fria = Foto(ReindexEngine.Resolve(Senales(), cat, memoria: memoria));
        Program.Assert(fria == sinMemoria, "con la memoria vacía, el resultado es idéntico al de sin memoria");
        Program.Assert(memoria.Filas > 0 && memoria.HayNovedades, "y la memoria ha aprendido algo");

        var caliente = Foto(ReindexEngine.Resolve(Senales(), cat, memoria: memoria));
        Program.Assert(caliente == sinMemoria, "a la segunda, tirando de la memoria, también es idéntico");

        // Y a medio llenar: la mitad conocida, la otra mitad nueva.
        var media = MemoriaDeParecidos.Para(cat);
        ReindexEngine.Resolve(Senales().Take(3).ToList(), cat, memoria: media);
        var mixta = Foto(ReindexEngine.Resolve(Senales(), cat, memoria: media));
        Program.Assert(mixta == sinMemoria, "con la memoria a medias (unos ficheros conocidos y otros no), idéntico");

        // La temporada del fichero NO se recuerda: es de cada vuelta. El mismo nombre en dos
        // carpetas distintas tiene que poder dar dos desempates.
        var a = SignalExtractor.Extract("/tv/Season 1979/Doraemon - La caja de las fichas predictoras.mp4", "Season 1979");
        var b = SignalExtractor.Extract("/tv/Season 1980/Doraemon - La caja de las fichas predictoras.mp4", "Season 1980");
        var juntos = Foto(ReindexEngine.Resolve([a, b], cat, memoria: memoria));
        Program.Assert(juntos == Foto(ReindexEngine.Resolve([a, b], cat)),
            "dos carpetas con el mismo nombre comparten el cálculo pero no el veredicto");
    }

    private static void UnCatalogoEditadoLaInvalida()
    {
        var antes = Catalogo();
        var despues = Catalogo(tituloExtra: " (version nueva)");   // un título distinto en el episodio 3

        var memoria = MemoriaDeParecidos.Para(antes);
        ReindexEngine.Resolve(Senales(), antes, memoria: memoria);

        Program.Assert(MemoriaDeParecidos.Para(despues).Huella != memoria.Huella,
            "editar un solo título del catálogo cambia la huella");

        // Usar la memoria vieja con el catálogo nuevo NO debe contaminar: se ignora entera.
        var conVieja = Foto(ReindexEngine.Resolve(Senales(), despues, memoria: memoria));
        var limpio = Foto(ReindexEngine.Resolve(Senales(), despues));
        Program.Assert(conVieja == limpio,
            "una memoria de otro catálogo no se cuela: el resultado es el del catálogo nuevo");
    }

    private static void UnFicheroRotoSeIgnoraEntero()
    {
        var cat = Catalogo();
        var llena = MemoriaDeParecidos.Para(cat);
        ReindexEngine.Resolve(Senales(), cat, memoria: llena);
        var bytes = llena.Serializar();

        var cortado = MemoriaDeParecidos.Para(cat);
        Program.Assert(!cortado.Absorber(bytes[..(bytes.Length - 7)]), "un fichero cortado a medias no vale");
        Program.Assert(cortado.Filas == 0, "y no deja NADA a medio meter");

        var basura = MemoriaDeParecidos.Para(cat);
        Program.Assert(!basura.Absorber(Encoding.UTF8.GetBytes("esto no es una memoria")), "basura: no vale");
        Program.Assert(!MemoriaDeParecidos.Para(cat).Absorber([]), "vacío: no vale");
    }

    private static void VaYVuelveDeDisco()
    {
        var temporal = Path.Combine(Path.GetTempPath(), "ondine-parecidos-" + Guid.NewGuid().ToString("N")[..8]);
        var antes = ReindexStore.RaizOverride;
        ReindexStore.RaizOverride = temporal;
        try
        {
            var cat = Catalogo();
            var sin = Foto(ReindexEngine.Resolve(Senales(), cat));

            var primera = ReindexStore.CargarParecidos(cat);
            Program.Assert(primera.Filas == 0, "sin nada guardado, arranca vacía");
            ReindexEngine.Resolve(Senales(), cat, memoria: primera);
            ReindexStore.GuardarParecidos(primera);
            Program.Assert(Directory.GetFiles(ReindexStore.DirParecidos, "*.bin").Length == 1, "se guarda un fichero");

            var segunda = ReindexStore.CargarParecidos(cat);
            Program.Assert(segunda.Filas == primera.Filas, "y vuelve entera de disco");
            Program.Assert(!segunda.HayNovedades, "lo que vuelve de disco no cuenta como novedad: no se reescribe");
            Program.Assert(Foto(ReindexEngine.Resolve(Senales(), cat, memoria: segunda)) == sin,
                "y con lo cargado de disco el resultado es idéntico");

            // Otro catálogo no usa ese fichero.
            var otro = ReindexStore.CargarParecidos(Catalogo(" (version nueva)"));
            Program.Assert(otro.Filas == 0, "otro catálogo no hereda lo del primero");
        }
        finally
        {
            ReindexStore.RaizOverride = antes;
            try { Directory.Delete(temporal, recursive: true); } catch { }
        }
    }
}
