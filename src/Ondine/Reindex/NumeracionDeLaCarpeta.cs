namespace Ondine.Reindex;

/// <summary>
/// ¿La numeración de los ficheros de esta carpeta es la del catálogo?
///
/// <para>
/// El motor usa el número del nombre como pista cuando no hay fecha con la que
/// confirmarlo. Eso vale en carpetas cuya numeración sale del catálogo, y falla
/// entero en las que no: si los ficheros los numeró otro —un canal, una lista de
/// reproducción, una emisión— el número <b>existe</b> en el catálogo pero apunta
/// a otro episodio. Y como existe, la propuesta sale con toda la cara de válida.
/// </para>
/// <para>
/// Comprobarlo mirando el vídeo sería caro. No hace falta: <b>la propia carpeta
/// lo dice</b>. Los ficheros que se identificaron por su título traen, cada uno,
/// una medida de cuánto se desvía su número del episodio de verdad; si esas
/// medidas se apartan de cero, la numeración no es la del catálogo y punto.
/// </para>
/// <para>
/// Medido en una carpeta real de Crayon Shin-Chan: de 42 identificados por
/// título, 36 caían entre −30 y −40 y 6 a cero —los ya renombrados—. Y los 17
/// que salieron del número asumían, los 17, desfase cero. La carpeta lo estaba
/// diciendo y solo esos 17 no la escuchaban.
/// </para>
/// </summary>
public static class NumeracionDeLaCarpeta
{
    /// <summary>
    /// Cuántos ficheros identificados por título hacen falta para declarar nada.
    /// Con cuatro no se declara: el precio de equivocarse aquí es dejar sin
    /// propuesta a una carpeta que estaba bien.
    /// </summary>
    public const int MinimoParaDeclarar = 8;

    /// <summary>
    /// Qué parte de los títulos tiene que estar desviada. Dos tercios y no la
    /// mitad porque una carpeta a medio arreglar tiene las dos poblaciones
    /// —lo renombrado a cero y lo pendiente corrido— y con la mitad justa no se
    /// sabe cuál manda.
    /// </summary>
    public const double ParteQueTieneQueEstarDesviada = 2.0 / 3.0;

    /// <summary>
    /// ¿Ha demostrado esta carpeta que sus números no son los del catálogo?
    ///
    /// <para>
    /// Solo votan los identificados <b>por título</b>. Los que salieron del número
    /// no pueden opinar sobre si el número vale: su desfase es cero por
    /// construcción, así que dejarlos votar sería preguntarle al acusado — el
    /// mismo error circular que ya se pagó una vez corroborando con el lote lo que
    /// venía del lote.
    /// </para>
    /// </summary>
    public static bool NoCuadra(IReadOnlyList<ReindexResolution> lote)
    {
        var desfases = lote
            .Where(r => r.Hint == ReindexHint.Titulo
                     && r.Episodio is not null
                     && r.Archivo.Indice is not null
                     && r.Score >= TitleMatch.UmbralTitulo
                     && !YaLoRenombramos(r))
            .Select(r => r.Episodio!.Num - r.Archivo.Indice!.Value)
            .ToList();

        if (desfases.Count < MinimoParaDeclarar) return false;

        int desviados = desfases.Count(d => d != 0);
        return desviados >= desfases.Count * ParteQueTieneQueEstarDesviada;
    }

    /// <summary>
    /// ¿Es este un fichero que ya lleva el nombre puesto por el catálogo?
    ///
    /// <para>
    /// Se reconoce en que su <b>propio nombre</b> ya trae el título del episodio.
    /// Quien escribió ese nombre sabía de qué episodio era, así que el número que
    /// puso salió del catálogo y no del origen: vota «desfase cero» por
    /// construcción, diga lo que diga el origen.
    /// </para>
    /// <para>
    /// Dejarlos votar tenía un efecto al revés del que hace falta: <b>cuantos más
    /// ficheros arreglabas, más votos a cero</b>, hasta que la regla se apagaba y
    /// volvían las propuestas inventadas. Medido en una carpeta a mitad de
    /// arreglar: 44 votos, 28 desviados, 16 a cero — y los dieciséis a cero eran
    /// ficheros ya renombrados. Hacían falta 29,3 y la protección se cayó por 1,3.
    /// </para>
    /// <para>
    /// Es la misma trampa de siempre con otra cara: contar como prueba algo que se
    /// deduce de la conclusión.
    /// </para>
    /// </summary>
    private static bool YaLoRenombramos(ReindexResolution r)
    {
        var delNombre = TitleMatch.Norm(r.Archivo.TituloNombre ?? "");
        if (delNombre.Length == 0) return false;

        return r.Episodio!.TitulosNorm.Any(t => TitleMatch.Sim(delNombre, t) >= TitleMatch.UmbralTitulo);
    }
}
