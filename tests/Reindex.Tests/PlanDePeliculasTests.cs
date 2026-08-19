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
    }
}
