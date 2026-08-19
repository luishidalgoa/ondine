using Ondine.Peliculas;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La cascada de confianza para películas: cuándo se cree lo que dice TMDb y,
/// sobre todo, <b>cuándo se planta</b>.
///
/// <para>
/// La regla que manda, y está en el propio issue: <b>una película mal
/// identificada es peor que una sin identificar</b>. Si renombras «El Padrino
/// II» como «El Padrino», meses después no sabes qué pasó. Así que casi todas
/// las pruebas de aquí abajo comprueban una negativa.
/// </para>
/// <para>
/// Lo que Ondine aporta encima del proveedor no es el dato —ese lo da TMDb—: es
/// esta decisión, con la señal a la vista, y el renombrado.
/// </para>
/// </summary>
public static class IdentificacionDePeliculaTests
{
    private static Tmdb.Candidato C(int id, string titulo, string? original, int? anio)
        => new(id, titulo, original, anio);

    public static void Todas()
    {
        Program.Seccion("Identificar una película: la cascada y sus negativas");

        // ── Lo que arregla de verdad: el título traducido ─────────────────────
        // Caso real de la biblioteca medida: el fichero se llama «The commuter»
        // y la carpeta «El pasajero», o al revés. Sin proveedor no hay forma de
        // saber que son la misma película; con él, se cotejan los DOS títulos.
        var pasajero = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("The commuter", 2018),
            new[] { C(399035, "El pasajero", "The Commuter", 2018) });

        Program.Assert(pasajero.Grado == IdentificacionDePelicula.Grado.Segura,
            "título original + año que cuadra: eso es seguro");
        Program.Assert(pasajero.Senal == IdentificacionDePelicula.Porque.TituloOriginal,
            "y se dice por qué señal: coincidió por el título ORIGINAL, no por el traducido");
        Program.Assert(IdentificacionDePelicula.Propuesta(pasajero) is { } p
                       && TituloDePelicula.Canonico(p) == "El pasajero (2018)",
            "y lo que se propone escribir es el título del idioma de la app, que es el que unifica la biblioteca");

        // ── El desempate por año, que es para lo que sirve el año ─────────────
        var alien = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("Alien", 1979),
            new[]
            {
                C(348, "Alien", "Alien", 1979),
                C(126889, "Alien: Covenant", "Alien: Covenant", 2017),
            });
        Program.Assert(alien.Elegido?.Id == 348 && alien.Grado == IdentificacionDePelicula.Grado.Segura,
            "con el año delante no hay duda entre una película y su saga");

        // Una diferencia de un año es normal: la fecha de estreno de TMDb es de
        // otro país. Plantarse por eso sería plantarse en media biblioteca.
        var porUnAnio = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("Cadena perpetua", 1995),
            new[] { C(278, "Cadena perpetua", "The Shawshank Redemption", 1994) });
        Program.Assert(porUnAnio.Grado == IdentificacionDePelicula.Grado.Segura,
            "un año de diferencia es el estreno en otro país, no otra película");

        // ── Y AQUÍ es donde se tiene que plantar ──────────────────────────────
        // Dos películas con el MISMO título y sin año en el fichero. Un remake es
        // exactamente esto, y no hay ninguna señal que las separe. Elegir una es
        // acertar la mitad de las veces sobre la biblioteca de alguien.
        var psicosis = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("Psicosis", null),
            new[]
            {
                C(539, "Psicosis", "Psycho", 1960),
                C(10634, "Psicosis", "Psycho", 1998),
            });
        Program.Assert(psicosis.Grado == IdentificacionDePelicula.Grado.Ninguna,
            "mismo título, sin año, dos candidatas: NO se elige. Acertar la mitad de las veces no es identificar");
        Program.Assert(psicosis.Senal == IdentificacionDePelicula.Porque.Empate,
            "y se dice que fue un empate, para que el usuario sepa que hay que mirarlo");
        Program.Assert(!psicosis.SePuedeAplicar, "y desde luego no se aplica");

        // El caso del issue: del nombre no sale nada aprovechable.
        var basura = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("peli final 2", null),
            new[] { C(1, "Perfect Blue", "Perfect Blue", 1997) });
        Program.Assert(basura.Grado == IdentificacionDePelicula.Grado.Ninguna
                       && basura.Senal == IdentificacionDePelicula.Porque.TituloFlojo,
            "si el título no se parece, lo que devuelva el buscador es ruido");

        // Título que cuadra pero año que no: es el otro remake, o es un error.
        // Se marca DUDOSA, que en esta app significa «se enseña y no se toca».
        var remake = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("Psicosis", 1960),
            new[] { C(10634, "Psicosis", "Psycho", 1998) });
        Program.Assert(remake.Grado == IdentificacionDePelicula.Grado.Dudosa,
            "el título cuadra y el año no: eso es una duda, no un hallazgo");
        Program.Assert(!remake.SePuedeAplicar,
            "y una duda NUNCA se aplica sola: es la regla que ya sigue Organizar");

        // Sin candidatos no hay nada que decidir, y eso no es un error.
        var vacio = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("Una que no existe", 1999),
            Array.Empty<Tmdb.Candidato>());
        Program.Assert(vacio.Grado == IdentificacionDePelicula.Grado.Ninguna
                       && vacio.Senal == IdentificacionDePelicula.Porque.SinCandidatos,
            "no encontrar nada es un resultado, no un fallo");

        // ── Sin año, pero sin competencia ─────────────────────────────────────
        // «El pasajero.mkv» a secas: una sola candidata y el título calcado. Es
        // lo mejor que se puede tener sin año, y plantarse aquí dejaría sin
        // arreglar las 52 películas de la biblioteca real que no traen año.
        var unaSola = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("El pasajero", null),
            new[] { C(399035, "El pasajero", "The Commuter", 2018) });
        Program.Assert(unaSola.Grado == IdentificacionDePelicula.Grado.Segura,
            "sin año, título calcado y ninguna competidora: eso se puede aplicar");
        Program.Assert(IdentificacionDePelicula.Propuesta(unaSola)?.Anio == 2018,
            "y el año lo APORTA el proveedor, que es medio motivo de conectarlo");

        // Pero si se parece a medias y no hay año, se enseña y no se toca.
        var aMedias = IdentificacionDePelicula.Decidir(
            new TituloDePelicula.Ficha("El padrino parte", null),
            new[] { C(240, "El padrino parte II", "The Godfather Part II", 1974) });
        Program.Assert(aMedias.Grado == IdentificacionDePelicula.Grado.Dudosa,
            "un parecido a medias sin año se queda en duda: aquí es donde se renombra «El Padrino II» como «El Padrino»");

        // ── La confianza se ve ────────────────────────────────────────────────
        Program.Assert(pasajero.Confianza > remake.Confianza
                       && remake.Confianza > psicosis.Confianza,
            "la confianza ordena los casos: no es un adorno, es lo que se enseña para decidir");
    }
}
