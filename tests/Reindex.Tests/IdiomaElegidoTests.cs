using Ondine.Localizacion;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué idioma arranca la app.
///
/// <para>
/// Existe porque la 1.6.0 salió con los 1152 textos traducidos y sin ninguna
/// forma de cambiar de idioma: nadie leía <see cref="Idioma.Disponibles"/> ni
/// escribía <see cref="Idioma.Actual"/>, así que la app se abría siempre en
/// inglés. Las pruebas de traducción no lo vieron porque comprueban que los
/// textos ESTÉN, no que se puedan elegir.
/// </para>
/// </summary>
public static class IdiomaElegidoTests
{
    public static void Todas()
    {
        Program.Seccion("Qué idioma arranca la app");

        var delSistema = Idioma.DelSistema();

        // Lo importante: la primera vez no hay nada guardado, y ahí manda el
        // sistema. Un castellanohablante no tiene por qué abrir la app en
        // inglés para ir a buscar dónde se cambia.
        Program.Assert(
            Idioma.Resolver("") == delSistema,
            $"Sin nada guardado se usa el idioma del sistema ({delSistema})");

        Program.Assert(
            Idioma.Resolver(null) == delSistema,
            "Un ajuste ausente se comporta igual que uno vacío");

        // Lo elegido a mano manda por encima del sistema, en los dos sentidos.
        Program.Assert(Idioma.Resolver("es") == "es", "Lo guardado manda: es");
        Program.Assert(Idioma.Resolver("en") == "en", "Lo guardado manda: en");

        // El sistema devuelve códigos con región.
        Program.Assert(Idioma.Resolver("es-ES") == "es", "«es-ES» cuenta como «es»");

        // Si mañana se añade un idioma y alguien vuelve a una versión vieja, su
        // ajuste apunta a algo que aquí no existe. Mejor caer en el del sistema
        // que en inglés a secas: es la misma situación que no haber elegido.
        Program.Assert(
            Idioma.Resolver("fr") == delSistema,
            "Un idioma que ya no existe cae en el del sistema, no en inglés");

        // Y el ajuste tiene que viajar en el fichero de preferencias, o se
        // vuelve a elegir en cada arranque.
        var ajustes = new Settings();
        Program.Assert(
            ajustes.Idioma == "",
            "De fábrica el ajuste va vacío, que es lo que significa «lo que diga el sistema»");

        // El preset por defecto se guarda por su NOMBRE, y el de los de fábrica
        // está traducido. Al poder cambiar de idioma eso deja de ser teórico:
        // se elige «Ligero para móvil» en castellano y al abrir en inglés ese
        // nombre ya no existe. La tabla de nombres históricos lo resuelve, pero
        // solo si se usa donde se aplica el preset, no solo al pintar la lista.
        var antes = Idioma.Actual;
        try
        {
            Idioma.Actual = "es";
            Program.Assert(
                PresetStore.NombreVigente("Light for mobile (720p)") == "Ligero para móvil (720p)",
                "Un preset guardado en inglés se reconoce con la app en castellano");

            Idioma.Actual = "en";
            Program.Assert(
                PresetStore.NombreVigente("Ligero para móvil (720p)") == "Light for mobile (720p)",
                "Y al revés: guardado en castellano, reconocido en inglés");

            // Un preset del usuario no está en la tabla y tiene que salir intacto.
            Program.Assert(
                PresetStore.NombreVigente("Lo mío para el salón") == "Lo mío para el salón",
                "Un preset con nombre propio pasa sin tocar");
        }
        finally { Idioma.Actual = antes; }

        // Guardar Preferencias no puede tirar lo que el diálogo no toca. Antes
        // construía un Settings nuevo y se llevaba por delante el historial de
        // renombrado y el factor de complejidad aprendido.
        var previos = new Settings { ComplexityFactor = 1.37, Idioma = "es" };
        previos.RenameSearchHistory.Add("temporada");
        var copia = previos.Clone();
        copia.Idioma = "en";
        Program.Assert(
            copia.ComplexityFactor == 1.37 && copia.RenameSearchHistory.Contains("temporada"),
            "Clonar conserva lo que el diálogo de preferencias no edita");
    }
}
