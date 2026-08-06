using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El guardián del reloj y el «omitir», por separado y JUNTOS.
///
/// <para>
/// Los tres fallos que hay que impedir, y cada uno tiene su bloque:
/// <list type="number">
///   <item>que el reloj se calle cuando <b>sí</b> hace falta que hable;</item>
///   <item>que el reloj hable cuando <b>no</b> hace falta —fue el fallo real: 67
///         ficheros de 1986 medidos con la vara de 1979—;</item>
///   <item>que los dos se estorben: que una fila marcada por el reloj se pueda
///         despachar en grupo con otras que no tienen nada que ver con ella, o
///         que después de omitirla el reloj se la vuelva a encontrar.</item>
/// </list>
/// </para>
/// <para>
/// Se prueba a través de <see cref="ReindexEngine.Resolve"/> entero y no contra
/// las piezas sueltas: la interacción es justo lo que se quiere fijar, y dos
/// piezas correctas por separado se pueden estorbar igual.
/// </para>
/// </summary>
public static class RelojYOmitirTests
{
    // Una serie que cambia de formato a mitad, como las de verdad: en 1979 una
    // historia dura ~6 minutos y en 1986 ~12.
    private const string Json = """
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 1,  "temporada": 1979, "fecha": "1979-01-01", "titulos": { "es": ["Corto uno"] } },
        { "num": 2,  "temporada": 1979, "fecha": "1979-01-08", "titulos": { "es": ["Corto dos"] } },
        { "num": 3,  "temporada": 1979, "fecha": "1979-01-15", "titulos": { "es": ["Corto tres"] } },
        { "num": 4,  "temporada": 1979, "fecha": "1979-01-22", "titulos": { "es": ["Corto cuatro"] } },
        { "num": 5,  "temporada": 1979, "fecha": "1979-01-29", "titulos": { "es": ["Corto cinco"] } },
        { "num": 6,  "temporada": 1979, "fecha": "1979-02-05", "titulos": { "es": ["Corto seis"] } },
        { "num": 20, "temporada": 1986, "fecha": "1986-01-01", "titulos": { "es": ["Largo uno"] } },
        { "num": 21, "temporada": 1986, "fecha": "1986-01-08", "titulos": { "es": ["Largo dos"] } },
        { "num": 22, "temporada": 1986, "fecha": "1986-01-15", "titulos": { "es": ["Largo tres"] } },
        { "num": 23, "temporada": 1986, "fecha": "1986-01-22", "titulos": { "es": ["Largo cuatro"] } },
        { "num": 24, "temporada": 1986, "fecha": "1986-01-29", "titulos": { "es": ["Largo cinco"] } },
        { "num": 25, "temporada": 1986, "fecha": "1986-02-05", "titulos": { "es": ["Largo seis"] } },
        { "num": 26, "temporada": 1986, "fecha": "1986-02-12", "titulos": { "es": ["Largo siete"] } }
      ]
    }
    """;

    private static FileSignals F(string nombre, int temporada, int min, int seg) =>
        SignalExtractor.Extract(
            Path.Combine("C:", "tv", $"Season {temporada}", nombre),
            $"Season {temporada}",
            duracion: new TimeSpan(0, min, seg));

    /// <summary>La carpeta base: seis de 1979 a 6 min y seis de 1986 a 12 min, todos correctos.</summary>
    private static List<FileSignals> Base() => new()
    {
        F("Serie - S1979E1 - Corto uno.mkv", 1979, 6, 10),
        F("Serie - S1979E2 - Corto dos.mkv", 1979, 6, 12),
        F("Serie - S1979E3 - Corto tres.mkv", 1979, 6, 8),
        F("Serie - S1979E4 - Corto cuatro.mkv", 1979, 6, 15),
        F("Serie - S1979E5 - Corto cinco.mkv", 1979, 6, 11),
        F("Serie - S1979E6 - Corto seis.mkv", 1979, 6, 9),
        F("Serie - S1986E20 - Largo uno.mkv", 1986, 12, 10),
        F("Serie - S1986E21 - Largo dos.mkv", 1986, 12, 12),
        F("Serie - S1986E22 - Largo tres.mkv", 1986, 12, 14),
        F("Serie - S1986E23 - Largo cuatro.mkv", 1986, 12, 11),
        F("Serie - S1986E24 - Largo cinco.mkv", 1986, 12, 13),
        F("Serie - S1986E25 - Largo seis.mkv", 1986, 12, 9),
    };

    private static ReindexResolution? Por(List<ReindexResolution> res, string trozo) =>
        res.FirstOrDefault(r => r.Archivo.Path.Contains(trozo, StringComparison.OrdinalIgnoreCase));

    public static void Todas()
    {
        Program.Seccion("El reloj y el omitir, juntos");

        var cat = ReindexCatalog.Parse(Json);

        ElRelojSeCallaCuandoDebe(cat);
        ElRelojHablaCuandoDebe(cat);
        NoSeEstorban(cat);
    }

    // ── 1. Que NO marque lo que es normal para su año ──
    private static void ElRelojSeCallaCuandoDebe(ReindexCatalog cat)
    {
        var señales = Base();
        // El caso real que falló: 13:23 en 1986. Con la vara de 1986 (12:12) es un
        // capítulo normal; con la vara global de la serie entera parecían 2 historias.
        señales.Add(F("Serie - S1986E26 - Largo siete.mkv", 1986, 13, 23));

        var res = ReindexEngine.Resolve(señales, cat);
        var siete = Por(res, "Largo siete")!;

        Program.Eq(ReindexConfianza.Alta, siete.Confianza,
            "13:23 en 1986 no se marca: es lo normal de su año");
        Program.Assert(res.All(r => r.Confianza == ReindexConfianza.Alta),
            "y no se marca ninguno de los demás tampoco");

        // La vara que se le reparte a la fila es la SUYA, no la de la serie: es lo
        // que se enseña en la interfaz para poder juzgarla.
        Program.Assert(siete.UnidadDeHistoria is { } u && u > new TimeSpan(0, 11, 0),
            "a una fila de 1986 se le reparte la vara de 1986");
        Program.Assert(Por(res, "Corto uno")!.UnidadDeHistoria is { } v && v < new TimeSpan(0, 7, 0),
            "y a una de 1979 la de 1979");
    }

    // ── 2. Que SÍ marque lo que de verdad no cuadra ──
    private static void ElRelojHablaCuandoDebe(ReindexCatalog cat)
    {
        var señales = Base();
        // El doble de la vara de SU año: eso son dos historias, y el catálogo dice
        // que ese episodio solo tiene una. Es el caso para el que existe el aviso.
        señales.Add(F("Serie - S1986E26 - Largo siete.mkv", 1986, 24, 30));

        var res = ReindexEngine.Resolve(señales, cat);
        var doble = Por(res, "Largo siete")!;

        Program.Eq(ReindexConfianza.Revisar, doble.Confianza,
            "el doble de largo SÍ se marca: la vara por temporada no apaga el guardián");
        Program.Assert(!string.IsNullOrWhiteSpace(doble.Motivo), "y dice por qué");

        // Y en el otro sentido: la mitad de lo que debería. Un episodio de 1979
        // metido en un fichero de minuto y medio no es ese episodio.
        var corto = Base();
        corto.Add(F("Serie - S1979E1 - Corto uno.mkv", 1979, 1, 30));
        var res2 = ReindexEngine.Resolve(corto, cat);
        Program.Assert(res2.Any(r => r.Confianza != ReindexConfianza.Alta),
            "un fichero muchísimo más corto de lo que toca tampoco pasa por bueno");

        // Sin duración no se opina: no saber no es sospechar.
        var sinDuracion = Base();
        sinDuracion.Add(SignalExtractor.Extract(
            Path.Combine("C:", "tv", "Season 1986", "Serie - S1986E26 - Largo siete.mkv"),
            "Season 1986"));
        var res3 = ReindexEngine.Resolve(sinDuracion, cat);
        Program.Eq(ReindexConfianza.Alta, Por(res3, "Largo siete")!.Confianza,
            "sin duración el reloj se calla");
    }

    // ── 3. Que los dos mecanismos no se estorben ──
    private static void NoSeEstorban(ReindexCatalog cat)
    {
        var señales = Base();
        señales.Add(F("Serie - S1986E26 - Largo siete.mkv", 1986, 24, 30));   // marcado por el reloj
        señales.Add(F("Serie S00E4 [S4] - Un especial.mkv", 0, 9, 0));        // especial sin sitio
        señales.Add(F("Serie S00E9 [S9] - Otro especial.mkv", 0, 9, 0));      // otro igual

        var res = ReindexEngine.Resolve(señales, cat);
        var reloj = Por(res, "Largo siete")!;
        var esp1 = Por(res, "S00E4")!;
        var esp2 = Por(res, "S00E9")!;

        // LO IMPORTANTE. Una fila marcada por el reloj NO se puede despachar en
        // grupo: cada una es un fichero distinto con su propio contenido, y un
        // botón que las dejara todas como están de un clic sería exactamente el
        // error que el reloj existe para evitar, con la comodidad de no mirar.
        Program.Assert(!CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.DeQueVa(reloj)),
            "lo que marca el reloj NUNCA se decide en grupo");
        Program.Eq(0, CausaDeConflicto.Companeras(res, reloj).Count,
            "y por tanto no se le ofrecen compañeras");

        // Y al revés: los especiales sin sitio sí se deciden en grupo, y el reloj
        // no tiene nada que ver con ellos —no los ha tocado—.
        Program.Assert(CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.DeQueVa(esp1)),
            "los especiales sin sitio sí se deciden en grupo");
        Program.Eq(1, CausaDeConflicto.Companeras(res, esp1).Count,
            "y se ofrecen entre ellos, sin arrastrar al del reloj");
        Program.Assert(!CausaDeConflicto.Companeras(res, esp1).Contains(reloj),
            "el marcado por el reloj NO entra en el grupo de los especiales");

        // Omitir una fila marcada por el reloj la deja resuelta, y entonces deja de
        // pedir nada. Es lo que hace «Dejarlo como está» en la interfaz.
        reloj.Estado = ReindexEstado.Limpio;
        reloj.Confianza = ReindexConfianza.Alta;
        reloj.Episodio = null;
        reloj.Alternativas = Array.Empty<ReindexCandidato>();
        Program.Eq(CausaDeConflicto.Causa.Ninguna, CausaDeConflicto.DeQueVa(reloj),
            "una vez omitida, la fila ya no pide decisión");

        // Y el reloj no se la vuelve a encontrar en el siguiente análisis: el
        // catálogo la lleva apuntada en «dejar_como_esta», así que ni se identifica.
        var conOmitido = ReindexCatalog.Parse(
            Json.TrimEnd().TrimEnd('}').TrimEnd() +
            """, "dejar_como_esta": ["Serie - S1986E26 - Largo siete.mkv"] }""");
        var res2 = ReindexEngine.Resolve(señales, conOmitido);
        var otraVez = Por(res2, "Largo siete")!;
        Program.Eq(ReindexEstado.Limpio, otraVez.Estado,
            "lo omitido no se vuelve a marcar en el siguiente análisis");
        Program.Eq(CausaDeConflicto.Causa.Ninguna, CausaDeConflicto.DeQueVa(otraVez),
            "ni vuelve a pedir decisión");

        // Y omitir uno NO apaga el guardián para los demás: sigue vigilando.
        // Ojo: el segundo tiene que ser un episodio DISTINTO. Repetir uno de la
        // base lo convertiría en un duplicado —otro conflicto, por otro motivo— y
        // la prueba pasaría por la razón equivocada.
        var conOtroLargo = Base();
        conOtroLargo.RemoveAll(s => s.Path.Contains("Largo seis"));
        conOtroLargo.Add(F("Serie - S1986E26 - Largo siete.mkv", 1986, 24, 30));   // omitido
        conOtroLargo.Add(F("Serie - S1986E25 - Largo seis.mkv", 1986, 25, 0));     // sospechoso

        var res3 = ReindexEngine.Resolve(conOtroLargo, conOmitido);
        var vigilado = Por(res3, "Largo seis")!;
        Program.Eq(ReindexConfianza.Revisar, vigilado.Confianza,
            "omitir uno no desactiva el reloj para el resto");
        Program.Eq(ReindexEstado.Limpio, Por(res3, "Largo siete")!.Estado,
            "y el omitido sigue omitido en la misma pasada");
    }
}
