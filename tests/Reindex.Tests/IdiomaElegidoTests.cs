using Ondine.Complementos;
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

/// <summary>
/// La ficha de propiedades que Windows guarda de un vídeo aparte de su contenido.
///
/// <para>
/// Aquí solo se prueba la conversión, que es lo que se puede probar en Linux: la
/// llamada a Windows no existe en el CI. Pero la conversión es donde está el
/// juicio -qué se considera «no lo sé» y qué es un dato corrupto-, y eso sí es
/// lógica que puede romperse sin que nadie lo note.
/// </para>
/// </summary>
public static class FichaDeWindowsTests
{
    public static void Todas()
    {
        Program.Seccion("La ficha de propiedades de un vídeo");

        Program.Assert(
            FichaDeWindows.DeUnidadesDe100ns(12_170_000_000) == TimeSpan.FromSeconds(1217),
            "20:17 en unidades de 100 ns se traduce bien");

        // Cero NO es «dura nada»: es lo que devuelve un fichero cuyo tipo el
        // sistema no sabe interpretar. Tratarlo como duración dejaría la barra del
        // reproductor con máximo cero y sin poder moverse.
        Program.Assert(
            FichaDeWindows.DeUnidadesDe100ns(0) is null,
            "Cero significa «no lo sé», no «dura nada»");

        // Un valor absurdo es un dato corrupto. Si se cuela, la barra queda con un
        // máximo imposible y el arrastre deja de corresponderse con el vídeo.
        Program.Assert(
            FichaDeWindows.DeUnidadesDe100ns((ulong)TimeSpan.FromHours(25).Ticks) is null,
            "Una duración de más de un día se descarta por corrupta");

        Program.Assert(
            FichaDeWindows.DeUnidadesDe100ns((ulong)TimeSpan.FromHours(3).Ticks) == TimeSpan.FromHours(3),
            "Una película larga de verdad sí pasa");

        // Fuera de Windows no hay ficha, y la respuesta tiene que ser «no sé»,
        // nunca abrir el fichero para averiguarlo.
        if (!OperatingSystem.IsWindows())
            Program.Assert(
                FichaDeWindows.Duracion("/tmp/loquesea.mkv") is null,
                "Fuera de Windows devuelve «no lo sé» sin tocar el fichero");
    }
}

/// <summary>
/// El reloj como segunda opinión sobre cuántas historias trae un fichero.
///
/// <para>
/// Salió de un caso real: «Doraemon (1979) S1981E618 [618] - Doraemon, te odio.avi»
/// anuncia UNA historia en su nombre, y la app estuvo a punto de darlo por bueno
/// como un episodio que el catálogo cuenta con DOS. Por el título no se distingue
/// -el parecido sale igual mida once minutos o media hora-; por la duración sí.
/// </para>
/// </summary>
public static class MedidaDelCapituloTests
{
    private static TimeSpan Min(double m) => TimeSpan.FromMinutes(m);

    public static void Todas()
    {
        Program.Seccion("El reloj como segunda opinión");

        // Una serie de historias de ~11 min: unas de una historia, otras de dos.
        var carpeta = new (TimeSpan, int)[]
        {
            (Min(11.0), 1), (Min(22.4), 2), (Min(10.6), 1),
            (Min(21.8), 2), (Min(11.3), 1), (Min(22.0), 2),
        };
        var unidad = MedidaDelCapitulo.Unidad(carpeta);
        Program.Assert(unidad is not null, "con bastantes ficheros se aprende cuánto dura una historia");
        Program.Assert(
            unidad!.Value.TotalMinutes is > 10.4 and < 11.4,
            $"y sale del orden de once minutos ({unidad.Value.TotalMinutes:F1})");

        // El caso del usuario: once minutos que alguien quiere dar por episodio de dos.
        Program.Assert(
            !MedidaDelCapitulo.Cuadra(Min(11), historias: 2, unidad),
            "un fichero de 11 min NO es un episodio de dos historias");
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(11), historias: 1, unidad),
            "pero sí es uno de una");

        // Y al revés, que es el otro que pediste: juntar de más.
        Program.Assert(
            !MedidaDelCapitulo.Cuadra(Min(33), historias: 1, unidad),
            "media hora larga NO es una sola historia");
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(33), historias: 3, unidad),
            "tres historias sí explican esa duración");
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(22), historias: 2, unidad),
            "y el caso normal de dos historias no molesta");

        // El margen existe para que la cabecera y los créditos no disparen el aviso.
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(24.5), historias: 2, unidad),
            "dos minutos de más sobre 22 no son sospecha");

        // No saber NO es sospechar. Sin duración -un fichero que Windows no indexó- o
        // sin unidad -una carpeta con cuatro ficheros- la comprobación se calla.
        Program.Assert(
            MedidaDelCapitulo.Cuadra(null, historias: 2, unidad),
            "sin duración no se opina");
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(11), historias: 2, null),
            "sin unidad aprendida tampoco");
        Program.Assert(
            MedidaDelCapitulo.Unidad(new[] { (Min(11), 1), (Min(22), 2) }) is null,
            "con cuatro ficheros no hay mediana de la que fiarse");

        // La mediana y no la media: un tráiler suelto no puede mover la vara.
        var conBasura = new (TimeSpan, int)[]
        {
            (Min(11.0), 1), (Min(11.1), 1), (Min(10.9), 1),
            (Min(11.2), 1), (Min(10.8), 1), (Min(0.5), 1),
        };
        Program.Assert(
            MedidaDelCapitulo.Unidad(conBasura)!.Value.TotalMinutes > 10.5,
            "un fichero de medio minuto no arrastra la medida");

        // Y lo que hace falta para redactar el aviso.
        Program.Assert(
            MedidaDelCapitulo.HistoriasQueSugiere(Min(11), unidad) == 1,
            "el reloj dice cuántas historias ve: una");
        Program.Assert(
            MedidaDelCapitulo.HistoriasQueSugiere(Min(22), unidad) == 2,
            "y dos cuando son dos");

        // Una serie de 45 min se mide con su propia vara: nada está escrito a mano.
        var larga = new (TimeSpan, int)[]
        {
            (Min(44), 1), (Min(46), 1), (Min(45), 1),
            (Min(43), 1), (Min(47), 1), (Min(45), 1),
        };
        var unidadLarga = MedidaDelCapitulo.Unidad(larga);
        Program.Assert(
            MedidaDelCapitulo.Cuadra(Min(45), historias: 1, unidadLarga),
            "en una serie de 45 min, 45 min es UNA historia");
        Program.Assert(
            !MedidaDelCapitulo.Cuadra(Min(45), historias: 2, unidadLarga),
            "y no dos");
    }
}

/// <summary>
/// Los complementos: qué se acepta como manifiesto y qué se descarta, y por qué.
///
/// <para>
/// Lo que de verdad se prueba aquí es lo que se RECHAZA. Un sistema que ejecuta
/// programas de fuera se juzga por lo que no deja pasar, no por lo que arranca.
/// </para>
/// </summary>
public static class ComplementoTests
{
    public static void Todas()
    {
        Program.Seccion("Complementos");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-complementos-" + Guid.NewGuid().ToString("N")[..8]);
        var suya = Path.Combine(raiz, "youtube");
        Directory.CreateDirectory(suya);
        var programa = Path.Combine(suya, "traer.cmd");
        File.WriteAllText(programa, "@echo off");

        try
        {
            string Manifiesto(string json)
            {
                var p = Path.Combine(suya, "plugin.json");
                File.WriteAllText(p, json);
                return p;
            }

            var bueno = Complemento.Leer(Manifiesto("""
            {
              "nombre": "YouTube",
              "descripcion": "Trae vídeos de una lista",
              "version": "1.0.0",
              "ejecutable": "traer.cmd",
              "capacidades": ["importar"],
              "contrato": 1
            }
            """));
            Program.Assert(bueno is not null, "un manifiesto correcto se lee");
            Program.Assert(bueno!.Reparo() is null, "y no tiene ningún reparo");
            Program.Assert(bueno.Id == "youtube", "el identificador sale de la carpeta, no se escribe");
            Program.Assert(bueno.Puede("importar"), "declara que sabe importar");
            Program.Assert(!bueno.Puede("comprimir"), "y solo lo que declara");

            // Lo que se rechaza, que es lo que importa.
            var fuera = Complemento.Leer(Manifiesto("""
            {"nombre":"Malo","ejecutable":"../../Windows/System32/cmd.exe","capacidades":["importar"],"contrato":1}
            """));
            Program.Assert(fuera!.Reparo() is not null,
                "un ejecutable que apunta FUERA de su carpeta se rechaza");

            var noEsta = Complemento.Leer(Manifiesto("""
            {"nombre":"Fantasma","ejecutable":"no-existe.exe","capacidades":["importar"],"contrato":1}
            """));
            Program.Assert(noEsta!.Reparo() is not null, "un programa que no está se rechaza");

            var otroContrato = Complemento.Leer(Manifiesto("""
            {"nombre":"Futuro","ejecutable":"traer.cmd","capacidades":["importar"],"contrato":99}
            """));
            Program.Assert(otroContrato!.Reparo() is not null,
                "un contrato que esta versión no habla se rechaza");

            var sinNada = Complemento.Leer(Manifiesto("""
            {"nombre":"Vago","ejecutable":"traer.cmd","capacidades":[],"contrato":1}
            """));
            Program.Assert(sinNada!.Reparo() is not null, "uno que no declara capacidades se rechaza");

            // Y todos los reparos DICEN algo: un descarte mudo deja a quien lo
            // instaló mirando una lista vacía sin nada que corregir.
            foreach (var malo in new[] { fuera, noEsta, otroContrato, sinNada })
                Program.Assert((malo!.Reparo() ?? "").Length > 10,
                    "cada rechazo explica su motivo");

            // Un fichero roto no puede impedir que arranque la aplicación.
            Program.Assert(Complemento.Leer(Manifiesto("{ esto no es json")) is null,
                "un manifiesto roto se ignora sin reventar");
        }
        finally
        {
            try { Directory.Delete(raiz, recursive: true); } catch { }
        }
    }
}

/// <summary>
/// Cotejar una lista de fuera contra el catálogo abierto: qué de eso te falta.
///
/// <para>
/// Es lo que convierte una lista de cuatrocientos vídeos en la respuesta a la
/// única pregunta que importa. Y lo delicado no es acertar: es callarse cuando no
/// se sabe. Decir «te falta» sobre algo que ya tienes te lo hace bajar dos veces;
/// decir «ya lo tienes» sobre algo que no, te lo hace perder.
/// </para>
/// </summary>
public static class CotejoDeListaTests
{
    public static void Todas()
    {
        Program.Seccion("Cotejar una lista contra el catálogo");

        var cat = ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0",
          "serie": "Doraemon (2005)",
          "episodios": [
            { "num": 10, "temporada": 2020, "titulos": { "es": ["El gorro de la suerte", "El cazamariposas"] } },
            { "num": 11, "temporada": 2020, "titulos": { "es": ["Cuidado con los estornudos"] } },
            { "num": 12, "temporada": 2020, "titulos": { "es": ["La lanza de la consideración"] } }
          ]
        }
        """);

        // Lo que ya hay en la carpeta: el 10 solo por su primera historia, y el 11 entero.
        var loQueHay = ReindexEngine.Resolve(new[]
        {
            SignalExtractor.Extract(Path.Combine("S", "Doraemon (2005) - S2020E10a - El gorro de la suerte.mkv"), "Season 2020"),
            SignalExtractor.Extract(Path.Combine("S", "Doraemon (2005) - S2020E11 - Cuidado con los estornudos.mkv"), "Season 2020"),
        }, cat);

        var v = CotejoDeLista.Cotejar(new[]
        {
            "El gorro de la suerte + El cazamariposas",
            "Cuidado con los estornudos",
            "La lanza de la consideración",
            "Un vídeo de la lista que no es de esta serie para nada",
        }, cat, loQueHay);

        Program.Assert(v.Count == 4, "sale un veredicto por elemento de la lista");

        Program.Assert(v[0].Estado == CotejoDeLista.Estado.AMedias,
            "del episodio de dos historias solo se tiene una: va a medias");
        Program.Assert(v[0].HistoriasQueFaltan.SequenceEqual(new[] { "b" }),
            "y dice CUÁL falta, que es lo que hay que traer");

        Program.Assert(v[1].Estado == CotejoDeLista.Estado.YaEsta,
            "el que está entero no hay que traerlo");
        Program.Assert(v[1].HistoriasQueFaltan.Count == 0, "y no nombra historias que no faltan");

        Program.Assert(v[2].Estado == CotejoDeLista.Estado.Falta,
            "el que no está en la carpeta es el que interesa");

        // El importante: callarse. Un vídeo que no casa con nada NO se declara
        // «te falta», porque eso invita a bajarse cosas que no son de la serie.
        Program.Assert(v[3].Estado == CotejoDeLista.Estado.Desconocido,
            "lo que no casa con el catálogo no se declara ni presente ni ausente");

        // Un episodio de UNA sola historia no nombra letras: o está o no está, y
        // enseñar una «a» donde no hay partes confunde más que informa.
        Program.Assert(v[2].HistoriasQueFaltan.Count == 0,
            "un episodio de una sola historia no enseña letras");
    }
}
