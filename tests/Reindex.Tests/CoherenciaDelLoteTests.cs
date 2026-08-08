using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La coherencia del lote como SEGUNDA señal.
///
/// <para>
/// Ondine da «confianza alta» cuando dos señales independientes coinciden —
/// normalmente el título y la fecha—. Un catálogo sin fechas deja solo el
/// título, así que <b>nada llega a alta por bien que case</b>, y cada fichero
/// pide una decisión. Medido en una carpeta real de Crayon Shin-Chan: catálogo
/// de 1342 episodios con <b>cero fechas</b>, y 53 de 59 ficheros pidiendo mano
/// con aciertos de 0,82 a 0,95.
/// </para>
/// <para>
/// Pero había otra señal tirada ahí: si N ficheros ordenados por su número
/// apuntan a N episodios <b>distintos y crecientes</b>, esa consistencia
/// corrobora a cada uno. En esa carpeta, 26 de 29 encajaban — y las 3 que no
/// eran justo las que merecían mirarse.
/// </para>
/// <para>
/// <b>Por BANDAS de desfase y no en una sola serie.</b> Una carpeta a medio
/// arreglar tiene dos poblaciones intercaladas: lo ya renombrado (desfase 0) y
/// lo pendiente (−30 a −40). Buscar un único orden global no encuentra ninguno,
/// porque no lo hay: hay dos, y los dos son válidos.
/// </para>
/// </summary>
public static class CoherenciaDelLoteTests
{
    private static readonly ReindexCatalog Cat = Construir();

    private static ReindexCatalog Construir()
    {
        var eps = string.Join(",\n", Enumerable.Range(1, 600)
            .Select(i => $$"""{ "num": {{i}}, "titulos": { "es": ["Titulo {{i}}"] } }"""));
        return ReindexCatalog.Parse($$"""
        { "esquema": "reindex/1.0", "serie": "Serie", "episodios": [ {{eps}} ] }
        """);
    }

    /// <summary>Un fichero que dice ser el <paramref name="dice"/> y al que se le propuso el <paramref name="propone"/>.</summary>
    private static ReindexResolution R(int dice, int propone, double score = 0.90)
    {
        var a = SignalExtractor.Extract(
            Path.Combine("C:", "tv", $"Serie S01E{dice} - Titulo {propone}.mkv"), "Season 01");
        return new ReindexResolution
        {
            Archivo = a,
            Estado = ReindexEstado.Corregido,
            Confianza = ReindexConfianza.Revisar,
            Episodio = Cat.PorNum(propone),
            Score = score,
        };
    }

    private static bool[] Marcar(params ReindexResolution[] lote)
    {
        CoherenciaDelLote.Marcar(lote);
        return lote.Select(r => r.CorroboradoPorElLote).ToArray();
    }

    public static void Todas()
    {
        Program.Seccion("La coherencia del lote");

        DesfaseConstante();
        DesfaseQueSeDesliza();
        ElQueRompeLaSerie();
        DosPoblacionesIntercaladas();
        CuandoNoSePuedeAfirmar();
    }

    // ── Lo más simple: todos con el mismo desfase ──
    private static void DesfaseConstante()
    {
        var lote = Enumerable.Range(0, 8).Select(i => R(100 + i, 70 + i)).ToArray();
        var ok = Marcar(lote);
        Program.Assert(ok.All(x => x), "ocho ficheros con desfase −30 se corroboran entre sí");
    }

    // ── Como en la vida real: el desfase resbala poco a poco ──
    private static void DesfaseQueSeDesliza()
    {
        // −30, −30, −31, −31, −32, −32, −33: sale de contar algo que el catálogo
        // no cuenta, y por eso crece despacio en vez de saltar.
        var pares = new[] { (487, 457), (490, 460), (493, 462), (494, 463),
                            (500, 468), (501, 469), (513, 480) };
        var lote = pares.Select(p => R(p.Item1, p.Item2)).ToArray();
        var ok = Marcar(lote);
        Program.Assert(ok.All(x => x), "un desfase que se desliza sigue siendo una serie");
    }

    // ── Lo que de verdad importa: aislar al que no encaja ──
    private static void ElQueRompeLaSerie()
    {
        var lote = new[]
        {
            R(487, 457), R(490, 460), R(491, 461), R(493, 462), R(494, 463),
            R(500, 468), R(501, 469),
            R(502, 504),               // ← el intruso: desfase +2 entre vecinos de −32
            R(503, 471), R(504, 472), R(506, 474), R(507, 475),
        };
        var ok = Marcar(lote);

        Program.Assert(!ok[7], "el que rompe la serie NO se corrobora");
        Program.Assert(ok.Where((_, i) => i != 7).All(x => x),
            "y los once que sí encajan, sí");

        // Un fichero que va hacia atrás tampoco: dos ficheros consecutivos no
        // pueden apuntar a episodios en orden inverso.
        var atras = new[] { R(10, 100), R(11, 101), R(12, 99), R(13, 103), R(14, 104), R(15, 105) };
        var ok2 = Marcar(atras);
        Program.Assert(!ok2[2], "uno que retrocede rompe el orden y no se corrobora");
    }

    // ── Una carpeta a medio arreglar: dos series válidas a la vez ──
    private static void DosPoblacionesIntercaladas()
    {
        // Lo ya renombrado (desfase 0) y lo pendiente (−30), mezclados por número
        // de fichero, que es como queda una carpeta que se arregló a medias.
        var lote = new[]
        {
            R(488, 488), R(487, 457), R(489, 489), R(490, 460), R(492, 492),
            R(491, 461), R(495, 495), R(493, 462), R(496, 496), R(494, 463),
            R(497, 497), R(500, 468),
        };
        var ok = Marcar(lote);
        Program.Assert(ok.All(x => x),
            "las dos poblaciones se corroboran cada una por su lado");

        // Y con las dos bandas presentes, un tercero suelto sigue sin corroborarse.
        var conIntruso = lote.Append(R(505, 999)).ToArray();
        var ok2 = Marcar(conIntruso);
        Program.Assert(!ok2[^1], "el que no pertenece a ninguna banda se queda fuera");
    }

    // ── Cuándo hay que callarse ──
    private static void CuandoNoSePuedeAfirmar()
    {
        // Por tres puntos pasa cualquier recta. Con pocos ficheros, «coinciden»
        // no significa nada, y corroborar ahí sería fabricar confianza.
        var pocos = new[] { R(10, 5), R(11, 6), R(12, 7) };
        Program.Assert(Marcar(pocos).All(x => !x), "con tres ficheros no se afirma nada");

        // Sin estructura no hay serie que valga.
        var caos = new[] { R(10, 300), R(11, 7), R(12, 512), R(13, 44), R(14, 190),
                           R(15, 88), R(16, 401), R(17, 12) };
        Program.Assert(Marcar(caos).All(x => !x), "un lote sin estructura no corrobora a nadie");

        // Dos ficheros que apuntan al MISMO episodio no se corroboran: la serie
        // exige episodios distintos, y si no lo fueran esto convertiría un
        // duplicado en dos certezas.
        var repes = new[] { R(10, 100), R(11, 101), R(12, 102), R(13, 102),
                            R(14, 104), R(15, 105), R(16, 106) };
        var ok = Marcar(repes);
        Program.Assert(!(ok[2] && ok[3]), "dos que reclaman el mismo episodio no se corroboran los dos");

        // Un título flojo NO se corrobora aunque encaje en la serie. Esto
        // corrobora, no identifica: si el título no llegaba, encajar en una
        // cuenta no lo convierte en cierto.
        var flojo = new[] { R(10, 100), R(11, 101), R(12, 102, 0.40), R(13, 103),
                            R(14, 104), R(15, 105), R(16, 106) };
        Program.Assert(!Marcar(flojo)[2], "un parecido flojo no se salva por encajar");

        // Y una fila sin episodio propuesto jamás sale corroborada: esto NO
        // inventa identificaciones, solo confirma las que ya había.
        var sinEp = R(20, 110);
        sinEp.Episodio = null;
        var conHueco = new[] { R(10, 100), R(11, 101), R(12, 102), R(13, 103),
                               R(14, 104), R(15, 105), sinEp };
        Program.Assert(!Marcar(conHueco)[^1], "sin episodio propuesto no hay nada que corroborar");
    }
}

/// <summary>
/// Un nombre pobre no es una promesa de traer menos.
///
/// <para>
/// Ondine avisa cuando el nombre declara UNA historia y el episodio tiene
/// varias, porque renombrarlo como el episodio entero afirmaría que trae lo que
/// no trae. Bien. Pero <b>si el reloj dice que ahí caben todas</b>, el fichero
/// las trae y lo único pobre es el nombre — y entonces el aviso sobra.
/// </para>
/// <para>
/// Medido en una carpeta real de Crayon Shin-Chan: <b>34 de 48</b> decisiones
/// pendientes eran esto. Ficheros de 24 minutos, episodios de 3 historias de
/// unos 8, y un <c>.nfo</c> que solo nombraba la primera. La regla ya sabía la
/// respuesta —lo dice su propio comentario— pero la usaba solo para elegir la
/// letra, no para callarse.
/// </para>
/// </summary>
public static class NombrePobreNoEsPromesaTests
{
    private const string Json = """
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 1, "temporada": 1, "titulos": { "es": ["Uno a", "Uno b", "Uno c"] } },
        { "num": 2, "temporada": 1, "titulos": { "es": ["Dos a", "Dos b", "Dos c"] } },
        { "num": 3, "temporada": 1, "titulos": { "es": ["Tres a", "Tres b", "Tres c"] } },
        { "num": 4, "temporada": 1, "titulos": { "es": ["Cuatro a", "Cuatro b", "Cuatro c"] } },
        { "num": 5, "temporada": 1, "titulos": { "es": ["Cinco a", "Cinco b", "Cinco c"] } },
        { "num": 6, "temporada": 1, "titulos": { "es": ["Seis a", "Seis b", "Seis c"] } }
      ]
    }
    """;

    private static FileSignals F(int n, string titulo, int min, int seg) =>
        SignalExtractor.Extract(
            Path.Combine("C:", "tv", "Season 01", $"Serie - S01E{n} - {titulo}.mkv"),
            "Season 01", duracion: new TimeSpan(0, min, seg));

    public static void Todas()
    {
        Program.Seccion("Un nombre pobre no es una promesa");

        var cat = ReindexCatalog.Parse(Json);

        // La carpeta: episodios de tres historias de ~8 min, ficheros de 24.
        // Los nombres solo dicen la PRIMERA historia — que es como quedan los
        // ficheros bajados de una lista.
        List<FileSignals> Carpeta() => new()
        {
            F(1, "Uno a + Uno b + Uno c", 24, 0),      // uno bien nombrado, para la vara
            F(2, "Dos a + Dos b + Dos c", 24, 2),
            F(3, "Tres a", 24, 1),
            F(4, "Cuatro a", 23, 58),
            F(5, "Cinco a", 24, 3),
        };

        var res = ReindexEngine.Resolve(Carpeta(), cat);
        var pobres = res.Where(r => r.Archivo.NombreArchivo.Contains("E3 ")
                                 || r.Archivo.NombreArchivo.Contains("E4 ")
                                 || r.Archivo.NombreArchivo.Contains("E5 ")).ToList();

        Program.Eq(3, pobres.Count, "las tres de nombre pobre están ahí");

        // El aviso SE QUEDA: renombrarlo como el episodio entero afirma algo que el
        // nombre no decía, y eso lo tiene que decir una persona. Lo que cambia es
        // que ya no hay que decirlo treinta y cuatro veces.
        Program.Assert(pobres.All(r => r.Confianza != ReindexConfianza.Alta),
            "el aviso se mantiene: sigue siendo una decisión");
        Program.Assert(pobres.All(r => r.Archivo.SubSegmento is null),
            "y no se le propone una letra: el reloj dice que caben todas");

        Program.Assert(pobres.All(r => r.NombreCortoParaEpisodioEntero),
            "pero se marcan como «el nombre es corto, no el contenido»");
        Program.Assert(pobres.All(r => CausaDeConflicto.SeConfirmaEnGrupo(CausaDeConflicto.DeQueVa(r))),
            "y por tanto se pueden confirmar de una vez, que era el problema");
        Program.Eq(2, CausaDeConflicto.CompanerasParaConfirmar(res, pobres[0]).Count,
            "cada una ofrece a las otras dos");

        // ── Y lo que NO puede cambiar ──
        // Un fichero que dura lo que UNA historia sí trae una sola, y ahí el aviso
        // es lo único que impide renombrarlo como el episodio entero y dar por
        // buenas dos historias que no están.
        var corta = Carpeta();
        corta.Add(F(6, "Seis a", 8, 0));
        var res2 = ReindexEngine.Resolve(corta, cat);
        var uno = res2.First(r => r.Archivo.NombreArchivo.Contains("E6 "));

        Program.Assert(uno.Confianza != ReindexConfianza.Alta,
            "el que dura una sola historia SIGUE pidiendo decisión");
        Program.Eq("a", uno.Archivo.SubSegmento,
            "y se le propone la historia que su nombre dice");
        Program.Assert(!uno.NombreCortoParaEpisodioEntero,
            "y NO se agrupa con los otros: ese sí trae menos, y su respuesta es otra");
    }
}
