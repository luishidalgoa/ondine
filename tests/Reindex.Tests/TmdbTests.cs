using Ondine.Peliculas;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La consulta a TMDb: qué sale de esta máquina, y qué se entiende de lo que
/// contesta.
///
/// <para>
/// Se prueba la <b>forma</b> —construir la URL y leer el JSON— sin hablar con
/// ningún servidor. Una prueba que necesita red no corre en CI, y una que corre
/// solo a veces no es una prueba.
/// </para>
/// </summary>
public static class TmdbTests
{
    public static void Todas()
    {
        Program.Seccion("Hablar con TMDb: qué se manda y qué se entiende");

        // ── Qué sale de esta máquina ──────────────────────────────────────────
        // Esto es lo más delicado de toda la función: se consulta un servicio de
        // fuera con lo que hay en el disco de alguien. Lo que sale es el título
        // ya limpio y el año, NUNCA el nombre del fichero: la resolución, el
        // códec y el nombre del grupo de release no le importan a nadie de
        // fuera, y dicen de dónde salió el fichero.
        var ficha = TituloDePelicula.Leer("Pelicula.2019.1080p.BluRay.x264-GRUPO.mkv");
        var url = Tmdb.Url(ficha.Titulo, ficha.Anio, "es-ES");

        Program.Assert(url.Contains("query=Pelicula"), $"se busca el título limpio · {url}");
        Program.Assert(url.Contains("year=2019"), "y el año, que es lo que descarta los remakes");
        foreach (var morralla in new[] { "1080p", "BluRay", "x264", "GRUPO", "mkv" })
            Program.Assert(!url.Contains(morralla, StringComparison.OrdinalIgnoreCase),
                $"«{morralla}» no sale de esta máquina: no hace falta para identificar y dice de dónde salió el fichero");

        // Un título con espacios y acentos tiene que ir escapado, o la petición
        // se rompe justo en las películas españolas.
        var conAcentos = Tmdb.Url("El laberinto del fauno", 2006, "es-ES");
        Program.Assert(!conAcentos.Contains(' '), "la URL no lleva espacios en crudo");
        Program.Assert(conAcentos.Contains("language=es-ES"),
            "y se pide en el idioma de la app: el título que se va a escribir en el disco es el traducido");

        var sinAnio = Tmdb.Url("Avatar", null, "es-ES");
        Program.Assert(!sinAnio.Contains("year="),
            "sin año no se manda un year vacío: TMDb lo tomaría como filtro y no devolvería nada");

        // ── Qué se entiende de lo que contesta ────────────────────────────────
        // Payload con la forma real de /search/movie, recortado a lo que se usa.
        var json = """
        {
          "page": 1,
          "results": [
            {
              "id": 278,
              "title": "Cadena perpetua",
              "original_title": "The Shawshank Redemption",
              "release_date": "1994-09-23",
              "popularity": 92.1
            },
            {
              "id": 999,
              "title": "Sin fecha",
              "original_title": "No date",
              "release_date": "",
              "popularity": 1.0
            }
          ],
          "total_results": 2
        }
        """;

        var c = Tmdb.Leer(json);
        Program.Assert(c.Count == 2, $"salen los dos candidatos · salieron {c.Count}");
        Program.Assert(c[0].Id == 278 && c[0].Titulo == "Cadena perpetua",
            "el título traducido es el que se va a escribir en el disco");
        Program.Assert(c[0].Original == "The Shawshank Redemption",
            "y el original se guarda: media biblioteca está nombrada con él");
        Program.Assert(c[0].Anio == 1994, "el año sale de la fecha de estreno");
        Program.Assert(c[1].Anio is null,
            "una fecha vacía es «no se sabe», no el año 0: TMDb la manda vacía más de lo que parece");

        // Nada de excepciones hacia fuera. Una respuesta rara es «no sé», no un
        // fallo que se coma la ventana entera.
        Program.Assert(Tmdb.Leer("").Count == 0, "una respuesta vacía no revienta");
        Program.Assert(Tmdb.Leer("no soy json").Count == 0, "ni una que no sea JSON");
        Program.Assert(Tmdb.Leer("""{"results":null}""").Count == 0, "ni una sin resultados");
    }
}
