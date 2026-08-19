using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Leer una película de su nombre de fichero.
///
/// <para>
/// Una serie tiene un anexo del que sacar la verdad; una película no —una película
/// es solo una película—, así que lo primero que hay es el nombre del fichero. Y
/// ahí la trampa no son los formatos raros: es que <b>hay títulos que son un
/// año</b>.
/// </para>
/// </summary>
public static class TituloDePeliculaTests
{
    private static void Igual(string nombre, string titulo, int? anio)
    {
        var f = TituloDePelicula.Leer(nombre);
        Program.Assert(f.Titulo == titulo && f.Anio == anio,
            $"«{nombre}» → «{f.Titulo}»{(f.Anio is null ? "" : $" ({f.Anio})")}  ·  se esperaba «{titulo}»{(anio is null ? "" : $" ({anio})")}");
    }

    public static void Todas()
    {
        Program.Seccion("Leer una película de su nombre");

        // ── Lo corriente ──────────────────────────────────────────────────────
        Igual("Blade Runner (1982).mkv", "Blade Runner", 1982);
        Igual("Blade Runner [1982].mkv", "Blade Runner", 1982);
        Igual("Blade Runner 1982.mkv", "Blade Runner", 1982);

        // Nombre de descarga, todo con puntos. Los puntos son separadores aquí y
        // no puntuación: no hay ni un espacio en todo el nombre.
        Igual("Blade.Runner.1982.1080p.BluRay.x264-GRUPO.mkv", "Blade Runner", 1982);

        // Y la morralla del final se corta con lo que ya sabía hacerlo, no con
        // una lista nueva que se quedaría atrás de la otra.
        Igual("Blade Runner (1982) 2160p WEB-DL HEVC.mkv", "Blade Runner", 1982);

        // ── La trampa: años que son parte del título ──────────────────────────

        // El año de verdad va entre paréntesis, y el del título no. Coger «el
        // último número de cuatro cifras» daría 2049 y dejaría la película
        // llamada «Blade Runner».
        Igual("Blade Runner 2049 (2017).mkv", "Blade Runner 2049", 2017);

        // Al principio del nombre, un año es título. Nadie llama a un fichero
        // empezando por el año de estreno.
        Igual("2001 A Space Odyssey (1968).mkv", "2001 A Space Odyssey", 1968);
        Igual("1917 (2019).mkv", "1917", 2019);

        // Y si SOLO hay eso, es el título entero y no hay año. Vale más quedarse
        // sin dato que inventar uno: el año va a acabar en el nombre del fichero
        // y en lo que lea Plex.
        Igual("1917.mkv", "1917", null);

        // Un año que aún no ha pasado no es un año de estreno: es título. Sin
        // esta regla, «Blade Runner 2049» a secas se parte por la mitad.
        Igual("Blade Runner 2049.mkv", "Blade Runner 2049", null);

        // ── Sacados de una biblioteca DE VERDAD ───────────────────────────────
        // Estos no me los he inventado: son nombres reales de una carpeta de 75
        // películas, y los tres primeros los fallaba la versión anterior.

        // El año entre paréntesis pero con el director delante. Antes salía
        // «Cadena Perpetua (Frank Darabont,» y una carpeta con ese nombre.
        Igual("Cadena Perpetua (Frank Darabont, 1994).mkv", "Cadena Perpetua", 1994);

        // Sin espacio antes del paréntesis.
        Igual("Una cuestion de tiempo(2013).mkv", "Una cuestion de tiempo", 2013);

        // La resolución pegada al final y en mayúsculas, sin separador de guión.
        Igual("El Rey Leon 2 1080P.mp4", "El Rey Leon 2", null);

        // Números de tres cifras al principio que NO son años: son el título.
        Igual("101 Dalmatas (1961).avi", "101 Dalmatas", 1961);
        Igual("102 Dalmatas.avi", "102 Dalmatas", null);

        // Un año reciente sin paréntesis sí es el año.
        Igual("Bob Esponja Plankton La Película 2025.mkv", "Bob Esponja Plankton La Película", 2025);

        // Dos espacios seguidos en medio del título.
        Igual("Piratas Del Caribe  La Venganza De Salazar.mp4",
              "Piratas Del Caribe La Venganza De Salazar", null);

        // Signos de apertura al principio, que no son basura sino el título.
        Igual("¡Qué bello es vivir! (1946).mkv", "¡Qué bello es vivir!", 1946);

        // ── El año lo tiene la CARPETA y no el fichero ────────────────────────
        // 52 de 75 ficheros de esa biblioteca no traen año, y en muchos casos la
        // carpeta que los contiene sí. Leer solo el fichero significaba proponer
        // «Grease/Grease.mp4» y TIRAR un año que ya estaba escrito.
        var conCarpeta = TituloDePelicula.Leer("Grease.mp4", "Grease (1978)");
        Program.Assert(conCarpeta.Titulo == "Grease" && conCarpeta.Anio == 1978,
            "si el fichero no trae año y la carpeta sí, se toma el de la carpeta");

        // El del fichero manda cuando lo hay: es el más específico de los dos.
        var manda = TituloDePelicula.Leer("It (2017).mp4", "IT (1990)");
        Program.Assert(manda.Anio == 2017,
            "el año del fichero gana al de la carpeta: es el que habla de ESTE fichero");

        // Y una carpeta de colección no aporta año ninguno, así que no estorba.
        var coleccion = TituloDePelicula.Leer("Up.mp4", "Disney");
        Program.Assert(coleccion.Titulo == "Up" && coleccion.Anio is null,
            "una carpeta sin año no inventa ninguno");

        // ── Sin año ───────────────────────────────────────────────────────────
        Igual("Una película sin año.mkv", "Una película sin año", null);
        Igual("Una.pelicula.sin.año.mkv", "Una pelicula sin año", null);

        // ── Ruido que no debe llegar al título ────────────────────────────────
        Igual("Amanece que no es poco (1989) [1080p].mkv", "Amanece que no es poco", 1989);
        Igual("  El espíritu de la colmena  (1973) .mkv", "El espíritu de la colmena", 1973);

        // ── Lo que no se puede leer no revienta ───────────────────────────────
        var vacio = TituloDePelicula.Leer("");
        Program.Assert(vacio.Titulo.Length == 0 && vacio.Anio is null,
            "un nombre vacío no da título ni año, y tampoco explota");

        // ── El nombre canónico, que es lo que acaba en el disco ───────────────
        // «Título (Año)» es lo que Plex y Jellyfin esperan de una película. Sin
        // año no se inventa un paréntesis vacío.
        Program.Assert(TituloDePelicula.Canonico(new("Blade Runner", 1982)) == "Blade Runner (1982)",
            "con año, el nombre lleva el año entre paréntesis");
        Program.Assert(TituloDePelicula.Canonico(new("Blade Runner 2049", null)) == "Blade Runner 2049",
            "sin año, el nombre es el título y ya: un paréntesis vacío no ayuda a nadie");
    }
}
