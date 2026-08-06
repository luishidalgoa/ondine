using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Por qué una fila pide una decisión, agrupado.
///
/// <para>
/// Existe para poder decidir UNA vez por causa en vez de una vez por fichero.
/// En una biblioteca de 1411 quedaban 27 filas que pedían mano, y 16 de ellas
/// eran la misma cosa dicha dieciséis veces: especiales que este catálogo no
/// contempla. Contestar dieciséis veces lo mismo no es revisar, es teclear.
/// </para>
/// <para>
/// La causa se saca de las MARCAS de la resolución, nunca del texto del motivo:
/// el motivo está redactado para leerlo una persona y se reescribe cuando se
/// mejora la redacción. Un agrupador que dependa de cómo está escrito se rompe
/// en silencio el día que alguien arregla una coma.
/// </para>
/// </summary>
public static class CausaDeConflictoTests
{
    private static ReindexResolution R(string nombre) => new()
    {
        Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", nombre), "T1"),
        Estado = ReindexEstado.Conflicto,
        Confianza = ReindexConfianza.Ninguna,
    };

    public static void Todas()
    {
        Program.Seccion("Por qué pide decisión esta fila");

        // Un especial de Plex (S00Exx) contra un catálogo sin especiales: no hay a
        // dónde mandarlo, y por eso no trae ni un candidato.
        var especial = R("Doraemon (1979) S00E10 [S10] - Guerra espacial en el desvan.avi");
        Program.Eq(CausaDeConflicto.Causa.EspecialSinSitio, CausaDeConflicto.DeQueVa(especial),
            "un especial que el catálogo no contempla");

        var duplicado = R("cap.avi");
        duplicado.EsDuplicado = true;
        Program.Eq(CausaDeConflicto.Causa.DosFicherosElMismoEpisodio, CausaDeConflicto.DeQueVa(duplicado),
            "dos ficheros peleando por el mismo episodio");

        var dobles = R("cap.avi");
        dobles.TraeDosEpisodios = true;
        Program.Eq(CausaDeConflicto.Causa.TraeDosEpisodios, CausaDeConflicto.DeQueVa(dobles),
            "un fichero con dos episodios dentro");

        var nada = R("una cosa rara.avi");
        Program.Eq(CausaDeConflicto.Causa.SinCandidatos, CausaDeConflicto.DeQueVa(nada),
            "nada en el catálogo se le parece");

        // Con candidatos y sin decidir: es una duda de identificación, y cada una
        // se resuelve con SU episodio.
        var duda = R("cap.avi");
        duda.Alternativas = new[]
        {
            new ReindexCandidato { Episodio = new CatalogEpisode { Num = 1 }, Score = 0.9 },
        };
        Program.Eq(CausaDeConflicto.Causa.DudaDeCual, CausaDeConflicto.DeQueVa(duda),
            "hay candidatos y hay que elegir");

        var limpia = R("cap.avi");
        limpia.Estado = ReindexEstado.Limpio;
        limpia.Confianza = ReindexConfianza.Alta;
        Program.Eq(CausaDeConflicto.Causa.Ninguna, CausaDeConflicto.DeQueVa(limpia),
            "una fila resuelta no pide nada");

        // ── Lo que decide si se puede contestar en bloque ──
        // Solo las causas cuya respuesta es la MISMA para todo el grupo. Que dos
        // filas compartan causa no basta: dos ficheros peleando por el episodio 5
        // y otros dos por el 9 tienen la misma causa y respuestas distintas, y
        // resolverlas juntas sería tirar una moneda cuatro veces.
        Program.Assert(CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.EspecialSinSitio),
            "los especiales sin sitio, sí: la respuesta es la misma para todos");
        Program.Assert(CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.SinCandidatos),
            "y los que no casan con nada, también");

        Program.Assert(!CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.DosFicherosElMismoEpisodio),
            "los duplicados NO: cada pareja tiene su propio ganador");
        Program.Assert(!CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.DudaDeCual),
            "ni las dudas: cada una acaba en un episodio distinto");
        Program.Assert(!CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.TraeDosEpisodios),
            "ni los de dos episodios: eso se parte, no se contesta");
        Program.Assert(!CausaDeConflicto.SeDecideEnGrupo(CausaDeConflicto.Causa.Ninguna),
            "y de lo que no pide nada no hay grupo que valga");

        // ── Contar el grupo ──
        var lote = new[]
        {
            R("Doraemon (1979) S00E10 [S10] - Uno.avi"),
            R("Doraemon (1979) S00E13 [S13] - Dos.avi"),
            R("Doraemon (1979) S00E16 [S16] - Tres.avi"),
            duplicado,
            limpia,
        };
        Program.Eq(3, CausaDeConflicto.Companeras(lote, especial).Count,
            "las tres iguales, y ni el duplicado ni la resuelta");

        Program.Eq(0, CausaDeConflicto.Companeras(lote, duplicado).Count,
            "de una causa que no se decide en grupo no hay compañeras que ofrecer");
    }
}
