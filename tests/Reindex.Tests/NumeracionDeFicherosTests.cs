namespace Ondine.Reindex.Tests;

/// <summary>
/// La numeración que el usuario declara para un catálogo manda sobre la del JSON: «7x1» es el
/// primer episodio de la temporada 7, y «100» puede ser el centésimo de la serie, aunque el JSON
/// diga otra cosa. Y con la numeración automática no cambia nada.
/// </summary>
public static class NumeracionDeFicherosTests
{
    // Los `num` del JSON son globales y con huecos a propósito: el episodio que ocupa la posición 1
    // de la serie no es el num 1.
    private static readonly ReindexCatalog Cat = ReindexCatalog.Parse("""
    {
      "esquema": "reindex/1.0", "serie": "S",
      "episodios": [
        { "num": 98,  "temporada": 5, "titulos": { "es": ["Alfa"] } },
        { "num": 99,  "temporada": 5, "titulos": { "es": ["Beta"] } },
        { "num": 100, "temporada": 7, "titulos": { "es": ["Gamma"] } },
        { "num": 101, "temporada": 7, "titulos": { "es": ["Delta"] } },
        { "num": 102, "temporada": 7, "titulos": { "es": ["Epsilon"] } }
      ]
    }
    """);

    private static ReindexResolution Uno(FileSignals f, NumeracionFicheros n) =>
        ReindexEngine.Resolve([f], Cat, null, ModoPrioridad.Automatico, null, n)[0];

    public static void Todas()
    {
        Program.Seccion("La numeración que declara el usuario manda sobre la del JSON");

        var siete1 = SignalExtractor.Extract("/tv/S/Season 7/S 7x01.mkv", "Season 7");
        var cinco2 = SignalExtractor.Extract("/tv/S/Season 5/S 5x02.mkv", "Season 5");
        var nueve3 = SignalExtractor.Extract("/tv/S/Season 9/S 9x03.mkv", "Season 9");
        var e04 = SignalExtractor.Extract("/tv/S/Todo/S - E04.mkv", "Todo");
        var e01 = SignalExtractor.Extract("/tv/S/Todo/S - E01.mkv", "Todo");

        // Reinicia por temporada: el 7x1 es el primero de la temporada 7, aunque el JSON dé 100 a otro.
        var a = Uno(siete1, NumeracionFicheros.PorTemporada);
        Program.Eq("Gamma", a.Episodio?.TitulosSalida.FirstOrDefault(), "7x01 con «reinicia» es el primero de la temporada 7");
        Program.Eq(ReindexConfianza.Alta, a.Confianza, "y con la numeración declarada, la confianza es alta");

        var b = Uno(cinco2, NumeracionFicheros.PorTemporada);
        Program.Eq("Beta", b.Episodio?.TitulosSalida.FirstOrDefault(), "5x02 es el segundo de la temporada 5");

        // Sigue de corrido: el 4 es el cuarto episodio de toda la serie (Delta), no el num 4 que no existe.
        var c = Uno(e04, NumeracionFicheros.Continua);
        Program.Eq("Delta", c.Episodio?.TitulosSalida.FirstOrDefault(),
            "E04 con «seguida» es el cuarto de la serie, aunque el JSON no tenga num 4");
        Program.Eq(ReindexHint.OrdinalSerie, c.Hint, "y lo dice con su propia pista, no la de temporada");

        var d = Uno(e01, NumeracionFicheros.Continua);
        Program.Eq("Alfa", d.Episodio?.TitulosSalida.FirstOrDefault(), "E01 con «seguida» es el primero de la serie");

        // Si no hay tal posición, se sigue con la cascada de siempre: ni se inventa ni se cuela.
        Igual(nueve3, "temporada 9 no existe: sin posición, la misma decisión que en automática", NumeracionFicheros.PorTemporada);
        Igual(e04, "por temporada, un fichero sin temporada no se puede ubicar: cascada de siempre", NumeracionFicheros.PorTemporada);

        // Automática: exactamente lo de siempre, y el parámetro por defecto es la automática.
        var porDefecto = ReindexEngine.Resolve([siete1], Cat)[0];
        var automatica = Uno(siete1, NumeracionFicheros.Automatica);
        Program.Assert(porDefecto.Episodio?.Num == automatica.Episodio?.Num && porDefecto.Estado == automatica.Estado,
            "sin declarar nada, el resultado es el de siempre");
    }

    private static void Igual(FileSignals f, string descripcion, NumeracionFicheros declarada = NumeracionFicheros.Continua)
    {
        var con = Uno(f, declarada);
        var auto = Uno(f, NumeracionFicheros.Automatica);
        Program.Assert(con.Episodio?.Num == auto.Episodio?.Num && con.Estado == auto.Estado,
            $"{descripcion} ({con.Episodio?.Num} vs {auto.Episodio?.Num})");
    }
}
