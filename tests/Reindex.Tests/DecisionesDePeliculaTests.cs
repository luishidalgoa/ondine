using Ondine.Peliculas;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Lo que tú decides sobre una película se recuerda.
///
/// <para>
/// Es el último criterio que le faltaba a la identificación contra TMDb: la
/// cascada se planta cuando dos encajan igual de bien —«Psicosis» de 1960 y la de
/// 1998— y eso está bien, pero si no puedes resolverlo tú, se planta <b>para
/// siempre</b>. Y volver a resolverlo cada vez que analizas no es resolverlo.
/// </para>
/// <para>
/// Es lo mismo que ya hacen las series con sus decisiones, y por el mismo motivo.
/// </para>
/// </summary>
public static class DecisionesDePeliculaTests
{
    private static Tmdb.Candidato Psicosis1960 => new(539, "Psicosis", "Psycho", 1960);
    private static Tmdb.Candidato Psicosis1998 => new(10634, "Psicosis", "Psycho", 1998);

    public static void Todas()
    {
        Program.Seccion("Las decisiones sobre películas se recuerdan");

        var dir = Path.Combine(Path.GetTempPath(), "ondine-decis-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        var fichero = Path.Combine(dir, "decididas.json");
        var peli = Path.Combine(dir, "Psicosis.mkv");

        try
        {
            var d = DecisionesDePelicula.Abrir(fichero);

            Program.Assert(d.Para(peli) is null,
                "de entrada no hay ninguna decisión: no se inventa lo que no has dicho");

            d.Recordar(peli, Psicosis1960);
            Program.Assert(d.Para(peli)?.Id == 539, "lo decidido se encuentra");

            // ── Sobrevive a cerrar la app ─────────────────────────────────────
            d.Volcar();
            var otra = DecisionesDePelicula.Abrir(fichero);
            Program.Assert(otra.Para(peli)?.Id == 539,
                "y sigue ahí al volver a abrir: si no, no es recordar, es aguantar hasta que cierres");
            Program.Assert(otra.Para(peli)?.Anio == 1960 && otra.Para(peli)?.Titulo == "Psicosis",
                "y entera, no solo el número");

            // ── Cambiar de opinión ────────────────────────────────────────────
            otra.Recordar(peli, Psicosis1998);
            Program.Assert(otra.Para(peli)?.Id == 10634,
                "decidir otra cosa sustituye a lo anterior; no se acumulan dos verdades");

            // ── La decisión sigue al fichero ──────────────────────────────────
            // Aplicar renombra y mueve. Si la decisión se quedara atada a la ruta
            // vieja, se perdería justo al aplicar lo que decidiste — que es cuando
            // más falta hace que se conserve.
            var nueva = Path.Combine(dir, "Psicosis (1998)", "Psicosis (1998).mkv");
            otra.Renombrado(peli, nueva);
            Program.Assert(otra.Para(nueva)?.Id == 10634, "la decisión viaja con el fichero");
            Program.Assert(otra.Para(peli) is null, "y no se queda además en la ruta vieja");

            // ── Olvidar ───────────────────────────────────────────────────────
            otra.Olvidar(nueva);
            Program.Assert(otra.Para(nueva) is null, "y se puede deshacer lo decidido");

            // ── Un fichero roto no tumba nada ─────────────────────────────────
            File.WriteAllText(fichero, "{ esto no es json");
            var rota = DecisionesDePelicula.Abrir(fichero);
            Program.Assert(rota.Para(peli) is null,
                "un fichero ilegible se empieza de cero en vez de reventar");

            // ══ Y lo que de verdad importa: que mande sobre la cascada ════════
            var candidatos = new[] { Psicosis1960, Psicosis1998 };
            var ficha = new TituloDePelicula.Ficha("Psicosis", null);

            var sinDecidir = IdentificacionDePelicula.Decidir(ficha, candidatos);
            Program.Assert(sinDecidir.Grado == IdentificacionDePelicula.Grado.Ninguna,
                "sin decisión sigue plantándose: dos iguales y nada que las separe");

            var conDecision = IdentificacionDePelicula.Decidir(ficha, candidatos, Psicosis1998);
            Program.Assert(conDecision.Grado == IdentificacionDePelicula.Grado.Segura,
                "con tu decisión delante ya no hay duda: la duda era de la app, no tuya");
            Program.Assert(conDecision.Senal == IdentificacionDePelicula.Porque.LoDijisteTu,
                "y se dice que fue cosa tuya, no que la app lo dedujo");
            Program.Assert(conDecision.Elegido?.Id == 10634 &&
                           IdentificacionDePelicula.Propuesta(conDecision)?.Anio == 1998,
                "y se propone la que elegiste");

            // Una decisión manda aunque la cascada tuviera una respuesta propia:
            // es tu biblioteca y tú la has mirado.
            var conAnio = new TituloDePelicula.Ficha("Psicosis", 1960);
            var mandaLaTuya = IdentificacionDePelicula.Decidir(conAnio, candidatos, Psicosis1998);
            Program.Assert(mandaLaTuya.Elegido?.Id == 10634,
                "tu decisión manda sobre lo que habría deducido la cascada");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
