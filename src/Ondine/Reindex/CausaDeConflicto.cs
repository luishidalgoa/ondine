namespace Ondine.Reindex;

/// <summary>
/// Por qué una fila pide una decisión, agrupado — para poder contestar UNA vez
/// por causa en vez de una vez por fichero.
///
/// <para>
/// Medido en una biblioteca real de 1411 ficheros: quedaban 27 filas pidiendo
/// mano, y <b>16 de ellas eran la misma cosa dicha dieciséis veces</b> —
/// especiales que ese catálogo no contempla—. Contestar dieciséis veces lo
/// mismo no es revisar, es teclear; y lo que se teclea sin mirar deja de ser
/// una decisión.
/// </para>
/// <para>
/// La causa sale de las <b>marcas</b> de la resolución, nunca del texto del
/// motivo. El motivo está redactado para que lo lea una persona y se reescribe
/// en cuanto alguien mejora la redacción: un agrupador que dependa de cómo está
/// escrito se rompe en silencio el día que se arregla una coma.
/// </para>
/// </summary>
public static class CausaDeConflicto
{
    public enum Causa
    {
        /// <summary>Resuelta: no pide nada.</summary>
        Ninguna,
        /// <summary>Viene marcado como especial y el catálogo no tiene dónde ponerlo.</summary>
        EspecialSinSitio,
        /// <summary>Nada del catálogo se le parece lo bastante.</summary>
        SinCandidatos,
        /// <summary>Otro fichero reclama el mismo episodio.</summary>
        DosFicherosElMismoEpisodio,
        /// <summary>El fichero trae dos episodios del catálogo dentro.</summary>
        TraeDosEpisodios,
        /// <summary>Hay candidatos y hay que elegir cuál.</summary>
        DudaDeCual,
        /// <summary>
        /// Un especial que casa con UNO del catálogo sin margen de duda. Nace en
        /// «revisar» como todos los especiales, pero lo único que le falta es que
        /// alguien diga que sí.
        /// </summary>
        EspecialSeguro,
        /// <summary>
        /// El nombre solo dice una de las historias del episodio, pero el fichero
        /// dura lo que TODAS. El aviso es correcto —renombrarlo entero afirma algo
        /// que el nombre no decía— y la respuesta es la misma para todos: sí, es el
        /// episodio entero con un nombre corto.
        /// </summary>
        NombreCortoEpisodioEntero,
    }

    /// <summary>De qué va esta fila.</summary>
    public static Causa DeQueVa(ReindexResolution r)
    {
        if (r.Confianza == ReindexConfianza.Alta && r.Estado is not ReindexEstado.Conflicto)
            return Causa.Ninguna;

        // El orden importa: un fichero puede cumplir varias, y la que manda es la
        // que decide QUÉ se hace con él. «Trae dos episodios» va antes que
        // cualquier duda de identificación porque eso no se contesta: se parte.
        if (r.TraeDosEpisodios) return Causa.TraeDosEpisodios;
        if (r.EsDuplicado) return Causa.DosFicherosElMismoEpisodio;

        // Un especial que casó sin margen de duda: lo único que le falta es un sí.
        if (r.Estado == ReindexEstado.Especial && r.Episodio is not null && r.Score >= SinMargenDeDuda)
            return Causa.EspecialSeguro;

        // El nombre nombra una historia y el fichero dura lo que todas. Medido en
        // una carpeta real de Crayon Shin-Chan: 34 de 48 decisiones eran esta, la
        // misma pregunta repetida con la misma respuesta.
        if (r.NombreCortoParaEpisodioEntero) return Causa.NombreCortoEpisodioEntero;

        if (r.Episodio is null && r.Alternativas.Count == 0)
            return r.Archivo.IndiceEspecial is not null
                ? Causa.EspecialSinSitio
                : Causa.SinCandidatos;

        return Causa.DudaDeCual;
    }

    /// <summary>
    /// ¿Se puede contestar de una para todo el grupo?
    ///
    /// <para>
    /// Compartir causa NO basta. Dos ficheros peleando por el episodio 5 y otros
    /// dos por el 9 tienen la misma causa y respuestas distintas: resolverlos
    /// juntos sería tirar una moneda cuatro veces. Solo entran las causas cuya
    /// respuesta es <b>literalmente la misma</b> para todos —«esto no está en
    /// este catálogo, déjalo en paz»—.
    /// </para>
    /// </summary>
    public static bool SeDecideEnGrupo(Causa c) =>
        c is Causa.EspecialSinSitio or Causa.SinCandidatos;

    /// <summary>
    /// A partir de aquí el parecido no deja margen: es ese episodio o el catálogo
    /// está mal escrito. Por debajo hay que mirar, y por eso no se agrupa.
    /// </summary>
    public const double SinMargenDeDuda = 0.95;

    /// <summary>
    /// ¿Se pueden confirmar de una?
    ///
    /// <para>
    /// Es la acción CONTRARIA a <see cref="SeDecideEnGrupo"/>: allí la respuesta
    /// común es «no toques esto» y aquí es «acepta lo que propones». Comparten
    /// forma y no significado, así que no comparten botón — mezclarlas haría que
    /// un clic hiciera lo opuesto de lo que se leyó.
    /// </para>
    /// <para>
    /// Solo los especiales seguros. Una duda normal acaba en un episodio distinto
    /// por fila, y aceptarlas todas de un clic sería firmar sin leer.
    /// </para>
    /// </summary>
    public static bool SeConfirmaEnGrupo(Causa c) =>
        c is Causa.EspecialSeguro or Causa.NombreCortoEpisodioEntero;

    /// <summary>Las otras filas que se pueden confirmar junto a esta.</summary>
    public static List<ReindexResolution> CompanerasParaConfirmar(
        IEnumerable<ReindexResolution> lote, ReindexResolution cual)
    {
        var causa = DeQueVa(cual);
        if (!SeConfirmaEnGrupo(causa)) return new();
        return lote.Where(r => !ReferenceEquals(r, cual) && DeQueVa(r) == causa).ToList();
    }

    /// <summary>
    /// Las otras filas del lote que tienen la MISMA causa que <paramref name="cual"/>,
    /// sin contarla a ella. Vacío si esa causa no se decide en grupo.
    /// </summary>
    public static List<ReindexResolution> Companeras(
        IEnumerable<ReindexResolution> lote, ReindexResolution cual)
    {
        var causa = DeQueVa(cual);
        if (!SeDecideEnGrupo(causa)) return new();

        return lote.Where(r => !ReferenceEquals(r, cual) && DeQueVa(r) == causa).ToList();
    }
}
