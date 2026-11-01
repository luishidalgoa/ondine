namespace Ondine.Reindex.Tests;

/// <summary>
/// Un catálogo importado se lee del fichero del usuario, así que editarlo tiene que contar sin
/// reimportar. Aquí se comprueba lo que la app mira para saberlo.
/// </summary>
public static class CatalogoQueSeReleeTests
{
    private const string Un = """{ "esquema": "reindex/1.0", "serie": "S", "episodios": [ { "num": 1, "temporada": 1979, "titulos": { "es": ["Uno"] } } ] }""";
    private const string Dos = """{ "esquema": "reindex/1.0", "serie": "S", "episodios": [ { "num": 1, "temporada": 1979, "titulos": { "es": ["Uno"] } }, { "num": 2, "temporada": 1979, "titulos": { "es": ["Dos"] } } ] }""";

    public static void Todas()
    {
        Program.Seccion("Un catálogo editado se relee sin reimportar");

        var temporal = Path.Combine(Path.GetTempPath(), "ondine-relee-" + Guid.NewGuid().ToString("N")[..8]);
        var antes = ReindexStore.RaizOverride;
        ReindexStore.RaizOverride = Path.Combine(temporal, "datos");
        try
        {
            Directory.CreateDirectory(temporal);
            var json = Path.Combine(temporal, "serie.json");
            File.WriteAllText(json, Un);
            ReindexStore.ImportarCatalogo(json);

            var f1 = ReindexStore.FirmaDelFichero(json);
            Program.Assert(f1.Length > 0, "el fichero tiene huella mientras existe");

            // La tarjeta se lee del disco en cada listado: tras editar, ya dice 2 sin reimportar.
            Program.Eq(1, ReindexStore.ListarCatalogos()[0].Episodios, "al principio, un episodio");
            File.WriteAllText(json, Dos);
            Program.Eq(2, ReindexStore.ListarCatalogos()[0].Episodios,
                "editado y sin reimportar, la tarjeta ya dice dos episodios");

            // Mismo tamaño, otro contenido: solo la fecha lo delata, y debe contar.
            File.WriteAllText(json, Un.Replace("\"Uno\"", "\"Uno \""));
            File.SetLastWriteTimeUtc(json, DateTime.UtcNow.AddMinutes(5));
            Program.Assert(ReindexStore.FirmaDelFichero(json) != f1,
                "cambiar el contenido (y su fecha) cambia la huella, aunque el tamaño sea el mismo");

            // Un JSON a medias: no vale, no sale en la lista, y su huella sigue viva para volver a mirarlo.
            File.WriteAllText(json, "{ \"esquema\": \"reindex/1.0\", \"serie\": ");
            Program.Eq(0, ReindexStore.ListarCatalogos().Count, "un JSON a medias no aparece en la lista");
            Program.Assert(ReindexStore.FirmaDelFichero(json).Length > 0, "y su huella existe para detectar el arreglo");

            // Arreglado: vuelve a aparecer con lo nuevo.
            File.WriteAllText(json, Dos);
            Program.Eq(2, ReindexStore.ListarCatalogos()[0].Episodios, "arreglado, vuelve con sus dos episodios");

            // Sin fichero: huella vacía y desaparece de la lista.
            File.Delete(json);
            Program.Eq("", ReindexStore.FirmaDelFichero(json), "sin fichero, huella vacía");
            Program.Eq(0, ReindexStore.ListarCatalogos().Count, "y sin fichero, fuera de la lista");
        }
        finally
        {
            ReindexStore.RaizOverride = antes;
            try { Directory.Delete(temporal, recursive: true); } catch { }
        }
    }
}
