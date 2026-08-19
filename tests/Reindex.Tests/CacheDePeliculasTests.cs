using Ondine.Peliculas;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La caché de lo ya preguntado a TMDb.
///
/// <para>
/// Dos motivos, los dos del issue. Uno: <b>no preguntar dos veces lo mismo</b> —
/// se analiza la misma carpeta muchas veces y la ficha de una película de 1972 no
/// va a cambiar—. Y dos: <b>que funcione sin red</b> con lo ya consultado, que es
/// el caso de una biblioteca en un equipo sin conexión.
/// </para>
/// </summary>
public static class CacheDePeliculasTests
{
    public static void Todas()
    {
        Program.Seccion("La caché de lo ya preguntado");

        var dir = Path.Combine(Path.GetTempPath(), "ondine-cache-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        var fichero = Path.Combine(dir, "tmdb.json");

        try
        {
            var cache = CacheDePeliculas.Abrir(fichero);

            Program.Assert(cache.Buscar("El pasajero", 2018, "es-ES") is null,
                "lo que no se ha preguntado nunca da «no sé», que no es lo mismo que «no hay nada»");

            cache.Guardar("El pasajero", 2018, "es-ES",
                new[] { new Tmdb.Candidato(399035, "El pasajero", "The Commuter", 2018) });

            Program.Assert(cache.Buscar("El pasajero", 2018, "es-ES")?.Count == 1,
                "lo guardado se encuentra");
            Program.Assert(cache.Buscar("  el PASAJERO ", 2018, "es-ES")?.Count == 1,
                "y sin depender de mayúsculas ni espacios: es el mismo título escrito de otra forma");

            // Un «no hay nada» también se recuerda. Si no, cada análisis vuelve a
            // preguntar por las que nunca se van a encontrar, que son justo las
            // que más se repiten en una carpeta.
            cache.Guardar("peli final 2", null, "es-ES", Array.Empty<Tmdb.Candidato>());
            Program.Assert(cache.Buscar("peli final 2", null, "es-ES") is { Count: 0 },
                "un «no encontré nada» se recuerda como respuesta, no como hueco");

            // El año y el idioma forman parte de la pregunta.
            Program.Assert(cache.Buscar("El pasajero", 2019, "es-ES") is null,
                "otro año es otra pregunta");
            Program.Assert(cache.Buscar("El pasajero", 2018, "en-US") is null,
                "y otro idioma también: la respuesta trae el título traducido");

            // ── Sin red, con lo ya consultado ─────────────────────────────────
            cache.Volcar();
            var deNuevo = CacheDePeliculas.Abrir(fichero);
            Program.Assert(deNuevo.Buscar("El pasajero", 2018, "es-ES")?.Count == 1,
                "al volver a abrir la app sigue estando: esto es lo que hace que funcione sin red");
            Program.Assert(deNuevo.Buscar("El pasajero", 2018, "es-ES")![0].Original == "The Commuter",
                "y entero, no solo el título");

            // ── Un fichero roto no tumba la app ───────────────────────────────
            File.WriteAllText(fichero, "{ esto no es json");
            var roto = CacheDePeliculas.Abrir(fichero);
            Program.Assert(roto.Buscar("El pasajero", 2018, "es-ES") is null,
                "una caché ilegible se empieza de cero: es una caché, no datos del usuario");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
