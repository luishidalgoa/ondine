using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Ficheros que están en una carpeta de películas y <b>no son la película</b>, y
/// películas que vienen partidas en dos.
///
/// <para>
/// Todo esto salió de una revisión adversarial del motor: casos en los que el
/// renombrado no era feo, sino <b>dañino</b>.
/// </para>
/// </summary>
public static class PeliculasQueNoLoSonTests
{
    private static string R(params string[] p) => Path.Combine(p);

    public static void Todas()
    {
        Program.Seccion("Lo que hay en la carpeta y no es la película");

        var raiz = R("C:", "Plex", "Movies");

        // ── Extras ────────────────────────────────────────────────────────────
        // El caso peor no necesita nombres sucios: en una carpeta YA canónica, con
        // la película en .mkv y el extra en .mp4, se proponía renombrar el extra a
        // «Título (Año).mp4». Plex lo leería como una SEGUNDA VERSIÓN de la
        // película, y el extra desaparece de donde debía estar.
        var conExtras = new[]
        {
            R(raiz, "Up (2009)", "Up (2009).mkv"),
            R(raiz, "Up (2009)", "Up (2009)-behindthescenes.mp4"),
            R(raiz, "Up (2009)", "Up (2009)-trailer.mp4"),
            R(raiz, "Up (2009)", "Up (2009)-deleted.mkv"),
        };

        var plan = PlanDePeliculas.Montar(conExtras, raiz, existe: _ => false);
        PlanDePeliculas.Paso Del(string f) => plan.First(p => p.Origen.EndsWith(f, StringComparison.Ordinal));

        Program.Assert(Del("-behindthescenes.mp4").Motivo == PlanDePeliculas.Porque.EsExtra,
            "un extra se reconoce y no se toca: renombrarlo lo convertiría en otra versión de la película");
        Program.Assert(Del("-trailer.mp4").Motivo == PlanDePeliculas.Porque.EsExtra,
            "el tráiler tampoco");
        Program.Assert(Del("-deleted.mkv").Motivo == PlanDePeliculas.Porque.EsExtra,
            "ni las escenas eliminadas");
        Program.Assert(Del("Up (2009).mkv").Motivo == PlanDePeliculas.Porque.YaEsta,
            "y la película de verdad sigue estando bien");

        // Los extras tampoco cuentan para decidir si la carpeta es una colección:
        // si contaran, una carpeta de UNA película con tres extras pasaría por
        // colección y dejaría de colocarse.
        var unaConExtras = new[]
        {
            R(raiz, "peli suelta", "Gladiator 2000.mkv"),
            R(raiz, "peli suelta", "Gladiator 2000-trailer.mp4"),
            R(raiz, "peli suelta", "Gladiator 2000-featurette.mp4"),
        };
        var plan2 = PlanDePeliculas.Montar(unaConExtras, raiz, existe: _ => false);
        Program.Assert(plan2.First(p => p.Origen.EndsWith("Gladiator 2000.mkv", StringComparison.Ordinal)).Motivo
                       == PlanDePeliculas.Porque.Va,
            "una película con extras al lado sigue siendo UNA película, no una colección");

        // Y los nombres que no son de nadie.
        Program.Assert(TituloDePelicula.EsExtra("sample.mkv"), "«sample» no es una película");
        Program.Assert(TituloDePelicula.EsExtra("trailer.mp4"), "«trailer» a secas tampoco");
        Program.Assert(!TituloDePelicula.EsExtra("Blade Runner (1982).mkv"),
            "y una película normal no se confunde con un extra");
        Program.Assert(!TituloDePelicula.EsExtra("The Sample Man (1999).mkv"),
            "ni una que lleve la palabra dentro del título");

        // ── Películas partidas en dos ─────────────────────────────────────────
        // Las dos mitades daban el MISMO nombre canónico: la primera perdía el
        // «cd1» y la segunda se quedaba sin sitio para siempre.
        Program.Seccion("Una película partida en dos");

        var partida = new[]
        {
            R(raiz, "Lo que el viento se llevó (1939)", "Lo que el viento se llevó (1939) cd1.avi"),
            R(raiz, "Lo que el viento se llevó (1939)", "Lo que el viento se llevó (1939) cd2.avi"),
        };
        var plan3 = PlanDePeliculas.Montar(partida, raiz, existe: _ => false);

        Program.Assert(plan3.All(p => p.Motivo != PlanDePeliculas.Porque.Ocupado),
            "las dos mitades no pelean por el mismo nombre");

        var nombres = plan3.Select(p => p.Destino is null ? Path.GetFileName(p.Origen) : Path.GetFileName(p.Destino))
                           .ToList();
        Program.Assert(nombres.Distinct().Count() == 2,
            "cada mitad conserva un nombre propio");
        Program.Assert(nombres.Any(n => n.Contains("part1")) && nombres.Any(n => n.Contains("part2")),
            "y la parte se escribe como la documenta Plex: «- part1», «- part2»");

        // Lo mismo con las otras formas de escribirlo.
        Program.Assert(TituloDePelicula.Parte("Peli (2000) CD2.avi") == 2, "«CD2» es la parte 2");
        Program.Assert(TituloDePelicula.Parte("Peli (2000) part1.mkv") == 1, "«part1» también");
        Program.Assert(TituloDePelicula.Parte("Peli (2000).mkv") is null, "y una entera no tiene parte");
        Program.Assert(TituloDePelicula.Parte("Cars 2.mp4") is null,
            "un número al final del título NO es una parte: «Cars 2» es una película");

        // ── Dos años en el nombre ─────────────────────────────────────────────
        Program.Seccion("Dos años en el mismo nombre");

        // El de los paréntesis es el del remaster, no el del estreno. Sin esto,
        // «Alien» se archivaba como de 2003.
        var alien = TituloDePelicula.Leer("Alien 1979 REMASTERED (2003).mkv");
        Program.Assert(alien.Anio == 1979 && alien.Titulo == "Alien",
            $"con una marca de reedición por medio manda el año de ESTRENO · salió «{alien.Titulo}» ({alien.Anio})");

        // Pero sin esa marca, el de los paréntesis sigue mandando: es lo que
        // alguien escribió a propósito, y un número suelto anterior suele ser
        // parte del título.
        var casa = TituloDePelicula.Leer("The 1900 House (1999).mkv");
        Program.Assert(casa.Anio == 1999 && casa.Titulo == "The 1900 House",
            $"sin marca de reedición no se toca nada · salió «{casa.Titulo}» ({casa.Anio})");
    }
}
