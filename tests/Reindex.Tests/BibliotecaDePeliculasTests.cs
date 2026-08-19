using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Una carpeta de películas: dónde va cada fichero, y que la app recuerde que
/// esa carpeta es de películas y no de episodios.
/// </summary>
public static class BibliotecaDePeliculasTests
{
    private static string R(params string[] p) => Path.Combine(p);

    public static void Todas()
    {
        Program.Seccion("Una carpeta de películas");

        var raiz = R("C:", "Plex", "Películas");

        // ── Dónde va cada una ─────────────────────────────────────────────────
        // «Título (Año)/Título (Año).ext» es lo que esperan Plex y Jellyfin: la
        // carpeta propia es lo que les deja meter dentro carátula, subtítulos y
        // versiones sin que se confundan con las de otra película.
        var bladeRunner = DestinoDePelicula.HayQueMover(
            R(raiz, "Blade.Runner.1982.1080p.BluRay.x264-GRUPO.mkv"), raiz);
        Program.Assert(bladeRunner == R(raiz, "Blade Runner (1982)", "Blade Runner (1982).mkv"),
            "la película va a su propia carpeta, con el nombre canónico y su extensión");

        // ── Sin año ───────────────────────────────────────────────────────────
        var sinAnio = DestinoDePelicula.HayQueMover(R(raiz, "Una película rara.mkv"), raiz);
        Program.Assert(sinAnio == R(raiz, "Una película rara", "Una película rara.mkv"),
            "sin año no se inventa un paréntesis: la carpeta es el título y ya");

        // ── La que ya está en su sitio no se toca ─────────────────────────────
        var yaEsta = DestinoDePelicula.HayQueMover(
            R(raiz, "Blade Runner (1982)", "Blade Runner (1982).mkv"), raiz);
        Program.Assert(yaEsta is null,
            "la que ya está bien colocada y bien nombrada no da destino: no hay nada que hacer");

        // Pero si está en su carpeta con el nombre sucio, SÍ hay que renombrarla.
        var carpetaBienNombreMal = DestinoDePelicula.HayQueMover(
            R(raiz, "Blade Runner (1982)", "Blade Runner 1982 1080p BluRay.mkv"), raiz);
        Program.Assert(carpetaBienNombreMal == R(raiz, "Blade Runner (1982)", "Blade Runner (1982).mkv"),
            "estar en la carpeta correcta no basta: el nombre del fichero también lo lee el escáner");

        // ── Lo que NO se arregla, y conviene que esté escrito ─────────────────
        // Un nombre de descarga en minúsculas da un título en minúsculas, y aquí
        // se deja como viene. Poner mayúsculas a cada palabra es lo que hacen
        // otros renombradores, pero acierta en inglés y falla en castellano —«El
        // Espíritu De La Colmena»—, así que sería inventarse un dato que no
        // tenemos. Quien lo arregla de verdad es el proveedor de #155, que sabe
        // cómo se escribe el título. Hasta entonces, esto es lo honesto.
        var minusculas = DestinoDePelicula.HayQueMover(
            R(raiz, "blade.runner.1982.1080p.mkv"), raiz);
        Program.Assert(minusculas == R(raiz, "blade runner (1982)", "blade runner (1982).mkv"),
            "el título se deja con las mayúsculas que traía: sin base de datos no hay de dónde sacarlas");

        // ── Lo que no se puede leer no se mueve ───────────────────────────────
        Program.Assert(DestinoDePelicula.HayQueMover(R(raiz, ".mkv"), raiz) is null,
            "de un nombre del que no sale título no sale destino: mover eso sería perderlo");

        // ── Caracteres que no valen en un nombre de fichero ───────────────────
        // No se inventa una limpieza nueva: es la misma que usa la plantilla de
        // los episodios, y por eso «:» acaba igual en los dos sitios.
        Program.Assert(
            DestinoDePelicula.Nombre(new TituloDePelicula.Ficha("2001: A Space Odyssey", 1968), ".mkv")
                == LibraryTemplate.LimpiarNombre("2001: A Space Odyssey (1968)") + ".mkv",
            "los caracteres prohibidos se quitan con lo que ya lo hacía, no con una lista nueva");

        // ── Que la app recuerde de qué es cada carpeta ────────────────────────
        // Sin esto hay que decirle en cada análisis que la carpeta de pelis es de
        // pelis, que es justo la clase de pregunta repetida que la app ya evita
        // con el catálogo y con el modo de prioridad.
        var temporal = Path.Combine(Path.GetTempPath(), "ondine-tipos-" + Guid.NewGuid().ToString("N")[..8]);
        var antes = ReindexStore.RaizOverride;
        try
        {
            Directory.CreateDirectory(temporal);
            ReindexStore.RaizOverride = temporal;

            Program.Assert(ReindexStore.TipoDeCarpeta(raiz) == TipoDeBiblioteca.Serie,
                "por defecto una carpeta es de serie: es lo que la app ha hecho siempre");

            ReindexStore.GuardarTipoDeCarpeta(raiz, TipoDeBiblioteca.Pelicula);
            Program.Assert(ReindexStore.TipoDeCarpeta(raiz) == TipoDeBiblioteca.Pelicula,
                "lo elegido se recuerda para esa carpeta");

            Program.Assert(ReindexStore.TipoDeCarpeta(R("C:", "Plex", "Series")) == TipoDeBiblioteca.Serie,
                "y no se contagia a las demás carpetas");

            // Windows no distingue mayúsculas en las rutas, y quien vuelva con la
            // ruta escrita de otra forma es la misma carpeta.
            Program.Assert(ReindexStore.TipoDeCarpeta(raiz.ToUpperInvariant()) == TipoDeBiblioteca.Pelicula,
                "la misma carpeta escrita en otras mayúsculas sigue siendo la misma");

            ReindexStore.GuardarTipoDeCarpeta(raiz, TipoDeBiblioteca.Serie);
            Program.Assert(ReindexStore.TipoDeCarpeta(raiz) == TipoDeBiblioteca.Serie,
                "y se puede volver atrás");
        }
        finally
        {
            ReindexStore.RaizOverride = antes;
            try { Directory.Delete(temporal, true); } catch { }
        }
    }
}
