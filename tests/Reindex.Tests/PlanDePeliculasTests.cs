using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El plan de una carpeta de películas: qué se renombra, qué se mueve y —sobre
/// todo— <b>qué no se toca</b>.
///
/// <para>
/// Los casos de aquí salen de una biblioteca real de 75 películas, medida con
/// <see cref="InformeBiblioteca"/>. Los nombres son los suyos.
/// </para>
/// </summary>
public static class PlanDePeliculasTests
{
    private static string R(params string[] p) => Path.Combine(p);

    public static void Todas()
    {
        Program.Seccion("El plan de una carpeta de películas");

        var raiz = R("C:", "Plex", "Movies");

        // La forma de una biblioteca de verdad: unas cuantas carpetas de
        // colección con muchas películas dentro, y otras de una sola.
        var biblioteca = new[]
        {
            R(raiz, "Disney", "Up.mp4"),
            R(raiz, "Disney", "El Rey Leon 2 1080P.mp4"),
            R(raiz, "Disney", "101 Dalmatas (1961).avi"),
            R(raiz, "Bob Esponja", "Bob Esponja Plankton La Película 2025.mkv"),
            R(raiz, "Bob Esponja", "Bob Esponja Historia Marina.avi"),
            R(raiz, "Grease (1978)", "Grease.mp4"),
            R(raiz, "Cadena perpetua (1994)", "Cadena Perpetua (Frank Darabont, 1994).mkv"),
            R(raiz, "Titanic", "Titanic.mp4"),
        };

        var plan = PlanDePeliculas.Montar(biblioteca, raiz, existe: _ => false);
        PlanDePeliculas.Paso Del(string fichero) =>
            plan.First(p => p.Origen.EndsWith(fichero, StringComparison.Ordinal));

        // ── Una carpeta con VARIAS películas es una colección: no se desmonta ──
        // Es tu forma de mirar la biblioteca. Sacar las 26 de «Disney» a 26
        // carpetas sueltas sería «correcto» para el escáner y destruiría eso.
        Program.Assert(Del("El Rey Leon 2 1080P.mp4").Destino == R(raiz, "Disney", "El Rey Leon 2.mp4"),
            "dentro de una colección se limpia el nombre, pero el fichero NO sale de su carpeta");

        Program.Assert(Del("El Rey Leon 2 1080P.mp4").Motivo == PlanDePeliculas.Porque.EnColeccion,
            "y se dice que es por eso, no por casualidad");

        Program.Assert(Del("Bob Esponja Plankton La Película 2025.mkv").Destino
                       == R(raiz, "Bob Esponja", "Bob Esponja Plankton La Película (2025).mkv"),
            "el año suelto se pone entre paréntesis, sin mover el fichero de sitio");

        // Los que ya están bien dentro de su colección no dan trabajo.
        Program.Assert(Del("Up.mp4").Motivo == PlanDePeliculas.Porque.YaEsta,
            "«Up.mp4» ya es su nombre canónico: no hay nada que hacer");
        Program.Assert(Del("101 Dalmatas (1961).avi").Motivo == PlanDePeliculas.Porque.YaEsta,
            "y este también, con su año ya puesto");

        // ── Una carpeta con UNA sola película sí se normaliza entera ───────────
        Program.Assert(Del("Cadena Perpetua (Frank Darabont, 1994).mkv").Destino
                       == R(raiz, "Cadena Perpetua (1994)", "Cadena Perpetua (1994).mkv"),
            "una película sola sí va a su carpeta canónica, con el director fuera del nombre");

        // El año lo tenía la carpeta, y sobrevive.
        Program.Assert(Del("Grease.mp4").Destino == R(raiz, "Grease (1978)", "Grease (1978).mp4"),
            "el año que estaba en la carpeta acaba también en el fichero");

        Program.Assert(Del("Titanic.mp4").Motivo == PlanDePeliculas.Porque.YaEsta,
            "«Titanic/Titanic.mp4» ya cumple la convención aunque no tenga año");

        // ── Nada se pisa ──────────────────────────────────────────────────────
        var ocupado = PlanDePeliculas.Montar(
            new[] { R(raiz, "Grease (1978)", "Grease.mp4") }, raiz, existe: _ => true);
        Program.Assert(ocupado[0].Motivo == PlanDePeliculas.Porque.Ocupado,
            "si el destino ya existe no se pisa, igual que en el reordenado de temporadas");

        // ── Y lo ilegible se queda quieto ─────────────────────────────────────
        var sinNombre = PlanDePeliculas.Montar(new[] { R(raiz, "Disney", ".mkv") }, raiz, existe: _ => false);
        Program.Assert(sinNombre[0].Motivo == PlanDePeliculas.Porque.SinTitulo,
            "de un nombre del que no sale título no sale destino");

        // ── Cuántos se mueven de verdad ───────────────────────────────────────
        Program.Assert(PlanDePeliculas.Cuantos(plan) == 4,
            "de los ocho, cuatro tienen trabajo: dos dentro de su colección y dos que se colocan");

        // ── El paso lleva la ficha que se leyó ────────────────────────────────
        // La ventana necesita preguntarle a TMDb por ESTA ficha, la que el plan
        // ha leído de verdad. Si la volviera a calcular por su cuenta habría dos
        // versiones de la misma regla —la del año que aporta la carpeta, la de
        // las colecciones— y una de las dos se quedaría atrás.
        Program.Assert(Del("Grease.mp4").Ficha == new TituloDePelicula.Ficha("Grease", 1978),
            "el paso lleva la ficha que se leyó, con el año que puso la carpeta");
        Program.Assert(Del("Up.mp4").Ficha?.Titulo == "Up",
            "también dentro de una colección");
        ConIdentificacion();
    }

    /// <summary>
    /// El plan cuando TMDb ya ha identificado la película: manda la ficha del
    /// proveedor y no la que se pudo leer del nombre. Es medio motivo de
    /// conectarlo — el nombre del fichero no puede arreglar un título mal escrito
    /// ni traer un año que no está escrito en ninguna parte.
    ///
    /// <para>
    /// Aquí se le pasan las fichas ya decididas. Filtrar las dudosas es cosa de
    /// quien pregunta: el plan se cree lo que le den, y por eso lo que le llega
    /// solo son las seguras.
    /// </para>
    /// </summary>
    private static void ConIdentificacion()
    {
        Program.Seccion("El plan cuando TMDb ya identificó la película");

        var raiz = R("C:", "Plex", "Movies");
        var suelta = R(raiz, "The commuter.mkv");
        var enColeccion = R(raiz, "Disney", "up 1080p x264.mp4");
        var otraDeLaColeccion = R(raiz, "Disney", "El Rey Leon 2 1080P.mp4");
        var elExtra = R(raiz, "The commuter-trailer.mkv");
        var aSecas = R(raiz, "Grease.mp4");

        var fichas = new Dictionary<string, TituloDePelicula.Ficha>(StringComparer.OrdinalIgnoreCase)
        {
            [suelta] = new("El pasajero", 2018),
            [enColeccion] = new("Up", 2009),
            [elExtra] = new("El pasajero", 2018),
        };

        var plan = PlanDePeliculas.Montar(
            new[] { suelta, enColeccion, otraDeLaColeccion, elExtra, aSecas }, raiz,
            existe: _ => false,
            identificada: f => fichas.TryGetValue(f, out var v) ? v : null);

        PlanDePeliculas.Paso Del(string ruta) => plan.First(p => p.Origen == ruta);

        // Lo que no puede hacer ningún nombre de fichero: saber que «The
        // commuter» y «El pasajero» son la misma película.
        Program.Assert(Del(suelta).Destino == R(raiz, "El pasajero (2018)", "El pasajero (2018).mkv"),
            "manda el título del proveedor, que es lo que unifica una biblioteca medio en inglés y medio en castellano");
        Program.Assert(Del(suelta).Motivo == PlanDePeliculas.Porque.Va, "y se coloca");

        // Dentro de una colección sigue sin salir de su carpeta: identificar no
        // cambia esa regla, solo mejora el nombre que se escribe.
        Program.Assert(Del(enColeccion).Destino == R(raiz, "Disney", "Up (2009).mp4"),
            "en una colección se renombra con el nombre bueno y NO se desmonta la carpeta");
        Program.Assert(Del(enColeccion).Motivo == PlanDePeliculas.Porque.EnColeccion,
            "y sigue contando como «se renombra donde está»");

        // Lo que NO se identificó se sigue leyendo del nombre, como antes.
        Program.Assert(Del(otraDeLaColeccion).Destino == R(raiz, "Disney", "El Rey Leon 2.mp4"),
            "lo que el proveedor no supo decir se sigue leyendo del nombre: no se pierde lo que ya funcionaba");
        Program.Assert(Del(aSecas).Destino == R(raiz, "Grease", "Grease.mp4"),
            "y una sin identificar tampoco se inventa un año");

        // Y la regla de los extras NO se puede saltar por identificar. Es el
        // fallo que convertía un tráiler en una segunda versión de la película,
        // y ahora el proveedor sabría decir de qué película es el tráiler: razón
        // de más para que esto siga cerrado.
        Program.Assert(Del(elExtra).Ficha is null,
            "un extra no lleva ficha: no se va a preguntar por él");
        Program.Assert(Del(elExtra).Motivo == PlanDePeliculas.Porque.EsExtra
                       && Del(elExtra).Destino is null,
            "un extra identificado sigue siendo un extra: renombrarlo lo convertiría en otra versión de la película");
    }
}
