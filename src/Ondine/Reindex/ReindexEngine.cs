using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>Estado final de un fichero. Cada uno cae en exactamente uno.</summary>
public enum ReindexEstado
{
    /// <summary>El número que ya trae es correcto: no hay que tocarlo.</summary>
    Limpio,
    /// <summary>Identificado con confianza, pero el número era otro → renombrar.</summary>
    Corregido,
    /// <summary>Es un especial: siempre pide confirmación de a cuál corresponde.</summary>
    Especial,
    /// <summary>Ambigüedad (duplicado, señales contradictorias, sin match) → decisión humana.</summary>
    Conflicto,
    /// <summary>Nombre no parseable o fichero ilegible.</summary>
    Error,
}

/// <summary>Semáforo de confianza, ortogonal al estado: qué se puede aplicar sin mirar.</summary>
public enum ReindexConfianza
{
    /// <summary>🟢 evidencia sólida: se puede aplicar en bloque.</summary>
    Alta,
    /// <summary>🟡 hay que echarle un ojo antes de aplicar.</summary>
    Revisar,
    /// <summary>🔴 no se aplica sin que una persona decida.</summary>
    Ninguna,
}

/// <summary>Qué evidencia resolvió el fichero (se enseña en la columna «POR QUÉ»).</summary>
public enum ReindexHint
{
    Ninguno,
    /// <summary>Decisión que el usuario ya tomó antes.</summary>
    Override,
    /// <summary>Número + fecha exacta: la más fuerte.</summary>
    IndiceFecha,
    /// <summary>Coincidencia de título por encima del umbral.</summary>
    Titulo,
    /// <summary>Número + fecha cercana (≤ 4 días).</summary>
    IndiceFechaAprox,
    /// <summary>Título por debajo del umbral: sugerencia, nunca automático.</summary>
    TituloDebil,
    /// <summary>El número releído como «el N.º de la temporada de la carpeta».</summary>
    OrdinalTemporada,
}

/// <summary>
/// En qué evidencia confía el catálogo al identificar. La fiabilidad es una propiedad de la
/// biblioteca, no global, así que esto se elige POR catálogo (opt-in). El modo por defecto no
/// cambia nada de lo de siempre.
/// </summary>
public enum ModoPrioridad
{
    /// <summary>La cascada de siempre (número+fecha, luego título). El comportamiento por defecto.</summary>
    Automatico,
    /// <summary>
    /// «El número manda»: el episodio N de la temporada T que dice el fichero se aplica directo,
    /// sin bajar a «revisar» por un título flojo. Para bibliotecas ya bien numeradas aunque los
    /// nombres estén sucios.
    /// </summary>
    NumeroPorTemporada,
}

/// <summary>Un episodio propuesto, con por qué se propone.</summary>
public sealed class ReindexCandidato
{
    public required CatalogEpisode Episodio { get; init; }
    public double Score { get; init; }
    public ReindexHint Hint { get; init; }
    /// <summary>Frases «＋ …» / «－ …» que sostienen o restan a este candidato.</summary>
    public IReadOnlyList<string> Evidencia { get; init; } = Array.Empty<string>();
}

/// <summary>Veredicto para un fichero: lo que la tabla de «Organizar» pinta en una fila.</summary>
public sealed class ReindexResolution
{
    public required FileSignals Archivo { get; init; }
    public ReindexEstado Estado { get; set; }
    public ReindexConfianza Confianza { get; set; }
    public ReindexHint Hint { get; set; }
    public CatalogEpisode? Episodio { get; set; }
    public double Score { get; set; }
    /// <summary>Explicación corta en español para la columna «POR QUÉ».</summary>
    public string Motivo { get; set; } = "";
    /// <summary>Otros episodios plausibles, para corregir con un clic.</summary>
    public IReadOnlyList<ReindexCandidato> Alternativas { get; set; } = Array.Empty<ReindexCandidato>();

    /// <summary>
    /// El resto del lote sostiene esta fila: hay más ficheros que, ordenados por su
    /// número, apuntan a episodios distintos y en el mismo orden.
    ///
    /// <para>
    /// Es la segunda señal cuando el catálogo no trae fechas. Ver
    /// <see cref="CoherenciaDelLote"/>.
    /// </para>
    /// </summary>
    public bool CorroboradoPorElLote { get; set; }

    /// <summary>
    /// El nombre solo nombra una de las historias del episodio, pero el fichero dura
    /// lo que TODAS: el nombre es corto, no el contenido.
    ///
    /// <para>
    /// Es una marca y no una comparación de textos, por lo mismo que las de abajo.
    /// Sirve para poder contestar una vez por todas las que están igual, en vez de
    /// repetir la misma respuesta una vez por fichero.
    /// </para>
    /// </summary>
    public bool NombreCortoParaEpisodioEntero { get; set; }

    /// <summary>
    /// El conflicto viene de que otro fichero reclama el mismo episodio, no de una duda de
    /// identificación. Es una marca y no una comparación de textos: depender de cómo está
    /// redactado el motivo se rompe en cuanto se reescribe el mensaje.
    /// </summary>
    public bool EsDuplicado { get; set; }

    /// <summary>
    /// Cuando esta fila es un duplicado/conflicto, la ruta del OTRO fichero que reclama el mismo
    /// episodio (el «ganador» que la app conserva). Permite enseñar las DOS rutas implicadas y que
    /// el usuario elija cuál mandar a la Papelera. null si no aplica.
    /// </summary>
    public string? RutaPareja { get; set; }

    /// <summary>
    /// El fichero contiene DOS episodios del catálogo. Es una marca y no una comparación de
    /// textos, por lo mismo que <see cref="EsDuplicado"/>: depender de cómo está redactado
    /// el motivo se rompe en cuanto se reescribe el mensaje.
    /// </summary>
    public bool TraeDosEpisodios { get; set; }

    /// <summary>
    /// Lo que dura una historia en la carpeta de la que salió esta fila, aprendido de ella
    /// misma. Se guarda para poder EXPLICARLO en la interfaz: un aviso que dice «no cuadra»
    /// sin decir contra qué obliga a ir a comprobarlo a mano.
    /// </summary>
    public TimeSpan? UnidadDeHistoria { get; set; }

    /// <summary>
    /// ¿Se puede aplicar sin más intervención? «Verdes + confirmados»: los especiales entran
    /// solo cuando alguien los ha confirmado, porque nacen en Revisar y únicamente una
    /// decisión humana (o un override guardado) los sube a Alta.
    /// </summary>
    public bool AplicableEnBloque => Confianza == ReindexConfianza.Alta
        && Estado is ReindexEstado.Limpio or ReindexEstado.Corregido or ReindexEstado.Especial;
    /// <summary>¿Necesita a una persona? Es el filtro «Solo dudas» del diseño.</summary>
    public bool EsDuda => Confianza != ReindexConfianza.Alta;
}

/// <summary>
/// Una decisión que el usuario ya tomó y que no se le vuelve a preguntar. Se guarda con el
/// nombre original al lado por trazabilidad: dentro de un año, «episodio 72» sin más no le
/// dirá nada a nadie.
/// </summary>
public sealed class ReindexOverride
{
    [System.Text.Json.Serialization.JsonPropertyName("num")]
    public required int Num { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("temporada")]
    public int? Temporada { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("serie")]
    public string Serie { get; init; } = "";
    /// <summary>«usuario» o «auto-confirmado».</summary>
    [System.Text.Json.Serialization.JsonPropertyName("origen")]
    public string Origen { get; init; } = "usuario";
    /// <summary>Si el fichero es SOLO una historia del episodio: su letra («a», «b»…).</summary>
    [System.Text.Json.Serialization.JsonPropertyName("seg")]
    public string? Seg { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("fecha_decision")]
    public string FechaDecision { get; init; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("nombre_original")]
    public string NombreOriginal { get; init; } = "";
}

/// <summary>
/// Motor de identificación. Librería PURA: entra (señales + catálogo + overrides), sale una
/// lista de resoluciones. Sin tocar disco y sin UI, para poder testear las reglas con
/// fixtures y cambiar la interfaz sin rozarlas.
/// </summary>
public static class ReindexEngine
{
    /// <summary>Tolerancia de P3: fecha «casi» igual.</summary>
    public const int ToleranciaDiasAprox = 4;

    /// <summary>
    /// Por debajo de esta diferencia, dos candidatos se consideran indistinguibles y la
    /// elección deja de ser automática.
    /// </summary>
    public const double MargenDesempate = 0.02;

    /// <summary>
    /// Cuánto puede quedarse por detrás del ganador un candidato para seguir mereciendo que
    /// se te ofrezca. Con el ganador al 100 %, enseñar uno al 67 % no ayuda a decidir: mete
    /// ruido en una fila que ya estaba resuelta e invita a un clic equivocado.
    /// </summary>
    public const double MargenAlternativa = 0.12;

    // Nota sobre la «ventana de candidatos» (±12 nº / ±12 días) de la epic: era un truco de
    // rendimiento de los scripts para no comparar contra 1800 episodios. Aquí no se usa, y a
    // propósito: la regla anti-remake EXIGE mirar la serie entera (el remake está a cientos de
    // números de distancia), y la cota superior de TitleBag ya hace ese barrido barato. Acotar
    // la búsqueda reintroduciría justo el fallo que la regla 1 existe para evitar.

    public static List<ReindexResolution> Resolve(
        IReadOnlyList<FileSignals> archivos,
        ReindexCatalog catalogo,
        IReadOnlyDictionary<string, ReindexOverride>? overrides = null,
        ModoPrioridad modo = ModoPrioridad.Automatico)
    {
        overrides ??= new Dictionary<string, ReindexOverride>();
        var indice = new IndiceTitulos(catalogo);

        // Cada fichero se resuelve contra un índice de solo lectura, sin mirar a los demás:
        // se puede repartir entre núcleos tal cual. Las reglas de LOTE (deduplicación) van
        // después, cuando ya están todas. AsOrdered mantiene el orden de entrada, que es el
        // que verá el usuario en la tabla.
        var resoluciones = archivos.AsParallel().AsOrdered()
            .Select(a => ResolverUno(a, catalogo, overrides, indice, modo))
            .ToList();
        Deduplicar(resoluciones);

        // El resolvedor sirve para RESOLVER, no para decorar filas ya resueltas: en una fila
        // que la app identificó con confianza no hay nada que elegir, y ofrecer candidatos
        // peores solo mete ruido e invita a un clic equivocado. Se hace al final, después de
        // la deduplicación, porque esa puede degradar una fila que hasta entonces iba verde.
        MarcarLosQueTraenDosEpisodios(resoluciones, catalogo, overrides, indice, modo);

        foreach (var r in resoluciones)
            if (r.Confianza == ReindexConfianza.Alta)
                r.Alternativas = Array.Empty<ReindexCandidato>();

        return resoluciones;
    }

    // ────────────────────────── resolución de un fichero ──────────────────────────

    private static ReindexResolution ResolverUno(FileSignals f, ReindexCatalog cat,
        IReadOnlyDictionary<string, ReindexOverride> overrides, IndiceTitulos indice,
        ModoPrioridad modo = ModoPrioridad.Automatico)
    {
        var r = new ReindexResolution { Archivo = f };

        if (!string.IsNullOrEmpty(f.Error))
            return Fallo(r, ReindexEstado.Error, f.Error!);

        // ── El usuario ya dijo que este fichero está bien como está ──
        // Antes que nada, incluso antes que un override: es la palabra del usuario sobre un
        // fichero que NO está en el catálogo (los especiales que no salen en ningún anexo), así
        // que no hay nada que resolver. Sale en verde y sin propuesta, no desaparece de la lista:
        // hacerlo desaparecer daría a entender que el fichero ya no está en la carpeta.
        if (cat.SeDejaComoEsta(f.NombreArchivo))
        {
            r.Estado = ReindexEstado.Limpio;
            r.Confianza = ReindexConfianza.Alta;
            r.Motivo = Textos.Instancia.ReindexMotivoDejarComoEsta;
            r.Alternativas = Array.Empty<ReindexCandidato>();
            return r;
        }

        if (!f.TieneSeñales)
            return Fallo(r, ReindexEstado.Error, Textos.Instancia.ReindexMotivoSinPistas);

        // ── P0: el usuario ya decidió esto ──
        if (overrides.TryGetValue(f.Fingerprint, out var ov))
        {
            var epOv = cat.PorNum(ov.Num);
            if (epOv != null)
            {
                // La decisión puede incluir «es solo la historia b»: el segmento viaja con
                // el fichero para que la plantilla lo escriba (E12b, solo su título).
                if (!string.IsNullOrEmpty(ov.Seg))
                {
                    f = f.ConSegmento(ov.Seg);
                    r = new ReindexResolution { Archivo = f };
                }
                r.Episodio = epOv;
                r.Hint = ReindexHint.Override;
                r.Score = 1.0;
                r.Confianza = ReindexConfianza.Alta;
                r.Estado = epOv.Especial ? ReindexEstado.Especial
                         : (f.Indice == epOv.Num ? ReindexEstado.Limpio : ReindexEstado.Corregido);
                r.Motivo = Textos.Instancia.ReindexMotivoDecidisteTu;
                return r;
            }
        }

        // ── Modo «el número manda por temporada» (opt-in, por catálogo) ──
        // Solo se activa si el catálogo lo pide. Quien SABE que su numeración es buena (aunque los
        // nombres estén sucios) aplica directo el episodio N de la temporada T del fichero, sin
        // caer en «revisar» por un título flojo. Si el fichero no trae número+temporada, o ese
        // hueco no existe en el catálogo, se deja pasar a la cascada normal (no se inventa nada).
        if (modo == ModoPrioridad.NumeroPorTemporada && !f.Especial
            && f.Temporada is int temp && f.Indice is int idx && idx >= 1)
        {
            var porPos = cat.Regulares.Where(e => e.Temporada == temp)
                                      .OrderBy(e => e.Num)
                                      .ElementAtOrDefault(idx - 1);
            if (porPos != null)
            {
                r.Episodio = porPos;
                r.Hint = ReindexHint.OrdinalTemporada;
                r.Score = 1.0;
                r.Confianza = ReindexConfianza.Alta;
                r.Estado = f.Indice == porPos.Num ? ReindexEstado.Limpio : ReindexEstado.Corregido;
                r.Motivo = string.Format(Textos.Instancia.ReindexMotivoNumeroManda, idx, temp);
                return r;
            }
        }

        // ── Regla 4: los especiales van por su rama, nunca a la numeración regular ──
        if (f.Especial) return ResolverEspecial(r, f, cat, indice);

        // ── títulos del fichero, ya normalizados ──
        var titulosArchivo = TitulosDe(f, cat);
        // La temporada que declara el fichero (por su nombre o por su carpeta) entra en el
        // desempate: sin ella, un episodio de 2014 competia de tu a tu con el de 2005 que el
        // propio fichero estaba diciendo.
        // El barrido del catálogo se hace UNA vez y se reutiliza: elegir al mejor y sacar
        // las alternativas son el mismo cálculo, y hacerlo dos veces costaba la mitad del
        // tiempo de una simulación entera.
        var puntuados = indice.Puntuados(titulosArchivo, cat.Episodios, f.Temporada);
        var (mejorTituloEp, mejorTituloP) = IndiceTitulos.MejorDe(puntuados);
        double mejorTituloScore = mejorTituloP.Sim;

        // ── Regla 3: metadato y nombre apuntan a episodios distintos → nunca elegir en silencio ──
        var choque = ChoqueMetaVsNombre(f, cat, indice);
        if (choque != null)
        {
            r.Estado = ReindexEstado.Conflicto;
            r.Confianza = ReindexConfianza.Ninguna;
            r.Episodio = null;
            r.Motivo = Textos.Instancia.ReindexMotivoChoqueMetaNombre;
            r.Alternativas = choque;
            return r;
        }

        var epPorIndice = f.Indice.HasValue ? cat.PorNum(f.Indice.Value) : null;

        // ── P1: número existe en la serie Y la fecha coincide exacta ──
        if (epPorIndice != null && f.Fecha.HasValue && epPorIndice.FechaParsed == f.Fecha)
            return PorNumero(r, f, epPorIndice, ReindexHint.IndiceFecha, indice, titulosArchivo,
                mejorTituloEp, mejorTituloScore,
                Textos.Instancia.ReindexMotivoNumeroYFecha);

        // ── P2: título por encima del umbral ──
        if (mejorTituloEp != null && mejorTituloScore >= TitleMatch.UmbralTitulo)
        {
            r.Episodio = mejorTituloEp;
            r.Score = mejorTituloScore;
            r.Hint = ReindexHint.Titulo;
            r.Confianza = ReindexConfianza.Alta;
            r.Estado = f.Indice == mejorTituloEp.Num ? ReindexEstado.Limpio : ReindexEstado.Corregido;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoTituloCoincide, Pct(mejorTituloScore));
            r.Alternativas = OtrosCandidatos(puntuados, mejorTituloEp, mejorTituloScore);

            // Otra cara del remake: DOS episodios distintos encajan igual de bien y ganó el
            // primero por orden de lista. Por título no hay manera de separarlos, así que no
            // se aplica solo — es exactamente el caso que el resolvedor de conflictos existe
            // para resolver (en el diseño, «E210 vs E655»).
            // Solo es empate si, mirándolo TODO (parecido, cuántos trozos casan y la
            // temporada), los dos siguen siendo indistinguibles. Antes bastaba con que el
            // parecido fuera parecido, y se preguntaba por casos que la temporada resolvía sola.
            var rival = r.Alternativas.FirstOrDefault();
            if (rival != null &&
                indice.Puntuar(titulosArchivo, rival.Episodio, f.Temporada).IndistinguibleDe(in mejorTituloP))
            {
                r.Confianza = ReindexConfianza.Revisar;
                r.Motivo = string.Format(Textos.Instancia.ReindexMotivoEmpateTitulo,
                    mejorTituloEp.Num, rival.Episodio.Num);
            }
            return r;
        }

        // ── P3: número existe y la fecha queda a ≤ 4 días ──
        if (epPorIndice != null && f.Fecha.HasValue && epPorIndice.FechaParsed.HasValue)
        {
            int dias = Math.Abs(epPorIndice.FechaParsed.Value.DayNumber - f.Fecha.Value.DayNumber);
            if (dias <= ToleranciaDiasAprox)
            {
                var res = PorNumero(r, f, epPorIndice, ReindexHint.IndiceFechaAprox, indice, titulosArchivo,
                    mejorTituloEp, mejorTituloScore,
                    string.Format(dias == 1
                        ? Textos.Instancia.ReindexMotivoFechaBailaUnDia
                        : Textos.Instancia.ReindexMotivoFechaBailaDias, dias));
                // P3 nunca es verde por sí sola: la fecha no encaja del todo
                if (res.Confianza == ReindexConfianza.Alta) res.Confianza = ReindexConfianza.Revisar;
                return res;
            }
        }

        // ── P3b: el número contradice a su propia carpeta → releerlo como ordinal de temporada ──
        //
        // El caso real: «S2018E01.mkv» sin título ni fecha, numerado del 1 en adelante DENTRO
        // de cada temporada. Leído como número global, el 1 es el estreno de 2005: contradice
        // a la carpeta y encima se pelea con el fichero del episodio 1 de verdad. Si la
        // temporada del fichero y la del episodio global no cuadran, el N se relee como «el
        // N.º de la temporada de la carpeta» — y la lectura global queda de alternativa.
        // Dos puertas de entrada: el global existe pero es de OTRA temporada (contradice a
        // la carpeta), o el global directamente NO existe (la numeración de la serie salta).
        // En ambas, el N.º de la carpeta es la única lectura coherente que queda.
        bool globalContradice = epPorIndice is { Temporada: not null } &&
                                f.Temporada.HasValue && epPorIndice.Temporada != f.Temporada;
        bool globalNoExiste = epPorIndice == null && f.Indice.HasValue;
        if (!f.Fecha.HasValue && f.Temporada.HasValue && f.Indice.HasValue &&
            (globalContradice || globalNoExiste))
        {
            var deTemporada = cat.Episodios
                .Where(e => e.Temporada == f.Temporada && !e.Especial)
                .OrderBy(e => e.Num)
                .ToList();

            if (f.Indice!.Value >= 1 && f.Indice.Value <= deTemporada.Count)
            {
                var ordinal = deTemporada[f.Indice.Value - 1];
                r.Episodio = ordinal;
                r.Score = 0.9;
                r.Hint = ReindexHint.OrdinalTemporada;
                r.Confianza = ReindexConfianza.Revisar;   // sin título ni fecha que lo confirme
                r.Estado = ReindexEstado.Corregido;
                r.Motivo = epPorIndice != null
                    ? string.Format(Textos.Instancia.ReindexMotivoOrdinalContradice,
                        f.Indice, f.Temporada, epPorIndice.Temporada)
                    : string.Format(Textos.Instancia.ReindexMotivoOrdinalNoExiste,
                        f.Indice, f.Temporada);
                r.Alternativas = epPorIndice == null
                    ? Array.Empty<ReindexCandidato>()
                    : new[]
                    {
                        new ReindexCandidato
                        {
                            Episodio = epPorIndice, Score = 0.5, Hint = ReindexHint.IndiceFechaAprox,
                            Evidencia = new[]
                            {
                                string.Format(Textos.Instancia.ReindexEvidenciaFicheroDice, f.Indice),
                                string.Format(Textos.Instancia.ReindexEvidenciaOtraTemporada,
                                    epPorIndice.Temporada, f.Temporada),
                            },
                        },
                    };
                return r;
            }
        }

        // Número que existe pero sin fecha con la que confirmarlo: vale como pista, no como prueba
        if (epPorIndice != null && !f.Fecha.HasValue)
        {
            var res = PorNumero(r, f, epPorIndice, ReindexHint.IndiceFechaAprox, indice, titulosArchivo,
                    mejorTituloEp, mejorTituloScore,
                Textos.Instancia.ReindexMotivoNumeroSinFecha);
            if (res.Confianza == ReindexConfianza.Alta) res.Confianza = ReindexConfianza.Revisar;
            return res;
        }

        // ── P4: título flojo → sugerencia, jamás automático ──
        if (mejorTituloEp != null && mejorTituloScore > 0)
        {
            r.Episodio = mejorTituloEp;
            r.Score = mejorTituloScore;
            r.Hint = ReindexHint.TituloDebil;
            r.Confianza = ReindexConfianza.Revisar;
            r.Estado = ReindexEstado.Corregido;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoTituloDebil, Pct(mejorTituloScore));
            r.Alternativas = OtrosCandidatos(puntuados, mejorTituloEp, mejorTituloScore);
            return r;
        }

        // ── nada ──
        r.Estado = ReindexEstado.Conflicto;
        r.Confianza = ReindexConfianza.Ninguna;
        r.Motivo = Textos.Instancia.ReindexMotivoNada;
        return r;
    }

    /// <summary>
    /// Resolución por número, aplicando el cross-check anti-remake: si el título apunta con
    /// fuerza a OTRO episodio, el número deja de ser suficiente.
    /// </summary>
    private static ReindexResolution PorNumero(ReindexResolution r, FileSignals f, CatalogEpisode ep,
        ReindexHint hint, IndiceTitulos indice, IReadOnlyList<TitleBag> titulos,
        CatalogEpisode? otroEp, double otroScore, string motivo)
    {
        r.Episodio = ep;
        r.Hint = hint;
        r.Score = 1.0;
        r.Confianza = ReindexConfianza.Alta;
        r.Estado = f.Indice == ep.Num ? ReindexEstado.Limpio : ReindexEstado.Corregido;
        r.Motivo = motivo;

        // Regla 1 (anti-remake): el mejor título viene de barrer TODA la serie, no una ventana
        // alrededor del número — un remake vive a cientos de episodios de distancia y jamás
        // caería cerca. Si ese título apunta a otro episodio, el número deja de bastar.
        //
        // Exigimos que el otro encaje ESTRICTAMENTE mejor que el propio episodio asignado: en
        // un remake ambos comparten título, y con un empate el número sigue siendo la mejor
        // prueba que hay. Sin esta condición, todo remake bien numerado saldría como duda.
        double scoreAsignado = indice.ScoreDe(titulos, ep);
        if (otroEp != null && otroEp.Num != ep.Num
            && otroScore >= TitleMatch.UmbralTitulo && otroScore > scoreAsignado)
        {
            r.Confianza = ReindexConfianza.Revisar;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoPeroElTitulo,
                motivo, Pct(otroScore), otroEp.Num);
            r.Alternativas = new[]
            {
                new ReindexCandidato
                {
                    Episodio = otroEp, Score = otroScore, Hint = ReindexHint.Titulo,
                    Evidencia = new[]
                    {
                        string.Format(Textos.Instancia.ReindexEvidenciaTituloCoincide, Pct(otroScore)),
                        Textos.Instancia.ReindexEvidenciaNumeroApuntaAOtro,
                    },
                },
            };
        }
        return r;
    }

    private static ReindexResolution ResolverEspecial(ReindexResolution r, FileSignals f,
        ReindexCatalog cat, IndiceTitulos indice)
    {
        r.Estado = ReindexEstado.Especial;
        // Los especiales SIEMPRE se confirman a mano: su numeración es aparte y poco fiable.
        r.Confianza = ReindexConfianza.Revisar;

        if (cat.Especiales.Count == 0)
        {
            r.Estado = ReindexEstado.Conflicto;
            r.Confianza = ReindexConfianza.Ninguna;
            r.Motivo = Textos.Instancia.ReindexMotivoEspecialSinEspeciales;
            return r;
        }

        var titulos = TitulosDe(f, cat);
        var (ep, score) = indice.MejorPorTitulo(titulos, cat.Especiales);

        if (ep != null && score > 0)
        {
            r.Episodio = ep;
            r.Score = score;
            r.Hint = score >= TitleMatch.UmbralTitulo ? ReindexHint.Titulo : ReindexHint.TituloDebil;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoEspecialTitulo, Pct(score));
            r.Alternativas = OtrosCandidatos(indice.Puntuados(titulos, cat.Especiales, null), ep, score);
        }
        else if (f.IndiceEspecial.HasValue)
        {
            r.Episodio = cat.Especiales.FirstOrDefault(e => e.Num == f.IndiceEspecial.Value);
            r.Hint = ReindexHint.IndiceFechaAprox;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoEspecialNumero, f.IndiceEspecial);
        }
        else
        {
            r.Motivo = Textos.Instancia.ReindexMotivoEspecialSinTitulo;
        }
        return r;
    }

    /// <summary>Regla 3: ¿el metadato y el nombre llevan a episodios distintos?</summary>
    private static IReadOnlyList<ReindexCandidato>? ChoqueMetaVsNombre(FileSignals f, ReindexCatalog cat,
        IndiceTitulos indice)
    {
        if (string.IsNullOrWhiteSpace(f.TituloMeta) || string.IsNullOrWhiteSpace(f.TituloNombre)) return null;

        var porNombre = indice.MejorPorTitulo(new[] { new TitleBag(TitleMatch.Norm(f.TituloNombre)) }, cat.Episodios);
        var porMeta = indice.MejorPorTitulo(new[] { new TitleBag(TitleMatch.Norm(f.TituloMeta)) }, cat.Episodios);

        // Solo es choque si AMBOS son fuertes y discrepan: uno flojo no contradice a nadie.
        if (porNombre.ep == null || porMeta.ep == null) return null;
        if (porNombre.score < TitleMatch.UmbralTitulo || porMeta.score < TitleMatch.UmbralTitulo) return null;
        if (porNombre.ep.Num == porMeta.ep.Num) return null;

        return new[]
        {
            new ReindexCandidato
            {
                Episodio = porNombre.ep, Score = porNombre.score, Hint = ReindexHint.Titulo,
                Evidencia = new[]
                {
                    string.Format(Textos.Instancia.ReindexEvidenciaNombreCoincide, Pct(porNombre.score)),
                    Textos.Instancia.ReindexEvidenciaMetadatoDiscrepa,
                },
            },
            new ReindexCandidato
            {
                Episodio = porMeta.ep, Score = porMeta.score, Hint = ReindexHint.Titulo,
                Evidencia = new[]
                {
                    string.Format(Textos.Instancia.ReindexEvidenciaMetadatoCoincide, Pct(porMeta.score)),
                    Textos.Instancia.ReindexEvidenciaNombreDiscrepa,
                },
            },
        };
    }

    // ────────────────────────── reglas de lote ──────────────────────────

    /// <summary>
    /// Regla 2: dos ficheros que apuntan al MISMO destino no pueden estar los dos bien.
    /// Gana el de mayor score; los demás pasan a conflicto. Sin esto, aplicar el lote
    /// machacaría un fichero con otro.
    /// </summary>
    /// <summary>
    /// Hay ficheros que emparejan dos historias que el catálogo cuenta como episodios
    /// DISTINTOS. Ponerles el número de uno pierde al otro: el fichero sigue conteniéndolo,
    /// pero ya no hay nada que lo diga, y quien lo mire dentro de un año no lo sabrá. Eso lo
    /// decide una persona.
    ///
    /// El listón es «el episodio elegido no cubre esta historia, y otro sí». No basta con
    /// que la historia aparezca en otro episodio: en Doraemon la misma historia está en el
    /// remake viejo y en el moderno, y eso es lo normal, no una ambigüedad.
    /// </summary>
    private static void MarcarLosQueTraenDosEpisodios(List<ReindexResolution> resoluciones,
        ReindexCatalog cat, IReadOnlyDictionary<string, ReindexOverride> overrides,
        IndiceTitulos indice, ModoPrioridad modo)
    {
        foreach (var r in resoluciones)
        {
            if (r.Episodio == null) continue;
            // Antes solo miraba las filas de confianza ALTA, y así se le escapaba el caso más
            // claro de todos: el fichero cuyo trozo A casa al 100 % con un episodio y el trozo
            // B con OTRO. Eso no llega como fila verde — llega como EMPATE (Revisar), porque
            // dos episodios distintos puntúan igual de alto, y la app lo tomaba por una
            // ambigüedad de «elige uno». Pero justo ahí no se puede elegir uno: hay que
            // partir. Se incluye Revisar. Ninguna no, porque sin un episodio de referencia
            // fiable no hay contra qué medir los trozos.
            if (r.Confianza is not (ReindexConfianza.Alta or ReindexConfianza.Revisar)) continue;
            // Si ya se decidió que el fichero es UNA historia, no hay nada que perder.
            if (r.Archivo.SubSegmento != null) continue;
            if (r.Archivo.Segmentos.Count < 2) continue;

            foreach (var trozo in r.Archivo.Segmentos)
            {
                var bolsa = TitleBag.From(trozo);
                if (bolsa.Text.Length < 4) continue;

                // ¿La cubre el episodio elegido? Entonces no hay problema.
                if (r.Episodio.TitulosNorm.Any(n => TitleMatch.Sim(bolsa.Text, n) >= TitleMatch.UmbralSegmento))
                    continue;

                // No la cubre: ¿de quién es, entonces?
                var (otro, score) = indice.MejorPorTitulo(new[] { bolsa }, cat.Episodios);
                if (otro == null || otro.Num == r.Episodio.Num || score < TitleMatch.UmbralSegmento) continue;

                r.Confianza = ReindexConfianza.Revisar;
                r.TraeDosEpisodios = true;
                r.Motivo = string.Format(Textos.Instancia.ReindexMotivoDosEpisodios,
                    r.Episodio.Num, otro.Num, trozo);
                break;
            }
        }

        // Primero se SUBE lo que el lote sostiene, y después corren los avisos.
        // Así cada regla que baja la confianza -el nombre que promete menos de lo
        // que trae, el reloj- se aplica encima y su veto manda. Al revés, subir al
        // final borraría un aviso puesto a propósito: un fichero que declara una
        // historia y dura dos no se vuelve seguro porque las cuentas de la carpeta
        // cuadren.
        SubirLasQueSostieneElLote(resoluciones);
        MarcarLosQueDeclaranMenosDeLoQuePromete(resoluciones, cat, overrides, indice, modo);
        MarcarLosQueNoCuadranConElReloj(resoluciones);
    }

    /// <summary>
    /// La segunda opinión: cuánto mide el fichero frente a cuántas historias promete el
    /// episodio que le ha tocado.
    ///
    /// <para>
    /// La comprobación del nombre no llega a todo. Un fichero puede traer los dos títulos
    /// en el nombre y contener solo una historia -media descarga, un corte mal hecho, una
    /// copia parcial-, y ahí el nombre no delata nada. El reloj sí.
    /// </para>
    /// <para>
    /// <b>Esto es de SERIES.</b> Vive en el reindexado, que solo trata episodios contra un
    /// catálogo. Una película no tiene «historias dentro» ni un número típico con el que
    /// comparar, así que cuando llegue esa parte no debe reutilizar nada de aquí: la idea
    /// entera -«cuántas unidades caben en esta duración»- no significa nada para un largo.
    /// </para>
    /// <para>
    /// Y se calla ante lo raro. Un especial que dura hora y media sale a nueve historias
    /// de las de esta carpeta, y eso no es «te faltan siete»: es que no se sabe qué es.
    /// Decir algo falso con seguridad es peor que no decir nada, así que solo se avisa
    /// cuando el reloj apunta a un número CREÍBLE de historias.
    /// </para>
    /// </summary>
    private static void MarcarLosQueNoCuadranConElReloj(List<ReindexResolution> resoluciones)
    {
        // Se aprende SOLO de las filas de confianza alta: aprender de las dudas es
        // aprender del error, y bastaría un lote malo para mover la vara y callar los
        // avisos justo cuando más falta hacen.
        var observaciones = resoluciones
            .Where(r => r.Confianza == ReindexConfianza.Alta && r.Episodio != null)
            .Where(r => r.Archivo.Duracion is { } d && d > TimeSpan.Zero)
            .Select(r => (r.Archivo.Duracion!.Value,
                          Math.Max(1, r.Episodio!.TitulosSalida.Count),
                          Temporada(r)))
            .ToList();

        // Una vara POR TEMPORADA, con la global de respaldo. Una serie larga cambia
        // de formato: medido en Doraemon (1979), una historia dura ~6:12 en 1979,
        // ~12:12 en 1986 y ~23:35 en 1991. Con una sola mediana se medía 1986 con la
        // vara de 1979 y salían marcados 67 ficheros que eran normales para su año.
        var varas = MedidaDelCapitulo.UnidadPorTemporada(observaciones);
        if (varas.Global is null) return;   // carpeta pequeña: no hay vara de la que fiarse

        // Se reparte a TODAS las filas, no solo a las que se marcan: la columna de
        // duración la enseña para que se pueda juzgar cualquier fila, no solo las malas.
        foreach (var r in resoluciones) r.UnidadDeHistoria = varas.De(Temporada(r));

        foreach (var r in resoluciones)
        {
            if (r.Episodio == null || r.Confianza != ReindexConfianza.Alta) continue;
            if (r.Archivo.SubSegmento != null) continue;   // ya se dijo qué trozo es
            if (r.Archivo.Duracion is not { } dura) continue;

            var unidad = varas.De(Temporada(r));
            if (unidad is null) continue;

            int promete = Math.Max(1, r.Episodio.TitulosSalida.Count);
            if (MedidaDelCapitulo.Cuadra(dura, promete, unidad)) continue;

            // La guarda de los especiales. Si el reloj no apunta a un número creíble de
            // historias, no hay nada que decir: no se sabe qué es ese fichero.
            var sugiere = MedidaDelCapitulo.HistoriasQueSugiere(dura, unidad);
            if (sugiere is not (>= 1 and <= MaxHistoriasCreibles)) continue;
            if (sugiere == promete) continue;

            r.Confianza = ReindexConfianza.Revisar;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoRelojNoCuadra,
                Reloj(dura), r.Episodio.Num, promete, Reloj(unidad.Value * promete));
        }
    }

    /// <summary>
    /// Sube a confianza alta lo que el resto del lote sostiene.
    ///
    /// <para>
    /// La confianza alta pide DOS señales que coincidan. Con un catálogo sin
    /// fechas solo hay el título, así que nada llegaba a alta por bien que casara
    /// y cada fichero acababa pidiendo una decisión — medido en una carpeta real:
    /// 53 de 59, con aciertos de 0,82 a 0,95. Ninguno era dudoso; faltaba con qué
    /// confirmarlos.
    /// </para>
    /// <para>
    /// La segunda señal es el propio lote: que veinte títulos coincidan por
    /// casualidad <b>y además en orden</b> no pasa. Lo que encaja sube; lo que
    /// rompe la serie se queda pidiendo mano, que es justo lo que hay que mirar.
    /// </para>
    /// <para>
    /// Corre ANTES que todos los avisos, a propósito. Así el que promete menos de
    /// lo que trae y el del reloj se aplican encima y pueden volver a bajarla: sus
    /// vetos se conservan enteros. Ponerlo al final borraba uno de ellos, y lo
    /// cazó una prueba que ya existía.
    /// </para>
    /// </summary>
    private static void SubirLasQueSostieneElLote(List<ReindexResolution> resoluciones)
    {
        CoherenciaDelLote.Marcar(resoluciones);

        foreach (var r in resoluciones)
        {
            if (!r.CorroboradoPorElLote) continue;

            // Solo se sube lo que estaba a UN paso: una duda de identificación.
            // Un conflicto -dos ficheros peleando, un fichero con dos episodios-
            // no se resuelve porque las cuentas cuadren, y subirlo sería colar
            // una decisión humana por la puerta de atrás.
            if (r.Confianza != ReindexConfianza.Revisar) continue;
            if (r.Estado is not (ReindexEstado.Limpio or ReindexEstado.Corregido)) continue;
            if (r.EsDuplicado || r.TraeDosEpisodios) continue;

            r.Confianza = ReindexConfianza.Alta;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoLoteLoSostiene, Pct(r.Score));
        }
    }

    /// <summary>
    /// Con qué temporada se mide esta fila. Manda la del CATÁLOGO: es el año de
    /// emisión, que es lo que determina el formato. La de la carpeta es donde
    /// alguien la puso, que puede estar mal — y es justo lo que se está corrigiendo.
    /// </summary>
    private static int? Temporada(ReindexResolution r) =>
        r.Episodio?.Temporada ?? r.Archivo.Temporada;

    /// <summary>
    /// Por encima de esto el reloj deja de opinar. Un episodio con cinco historias ya es
    /// raro; lo que mide diez veces la unidad no es un episodio mal contado, es otra cosa
    /// -un especial, una película, un recopilatorio- y no se puede juzgar con esta vara.
    /// </summary>
    private const int MaxHistoriasCreibles = 5;

    private static string Reloj(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";

    /// <summary>
    /// El caso contrario: el nombre declara UNA historia y el episodio al que se le
    /// quiere renombrar tiene DOS.
    ///
    /// <para>
    /// «Doraemon (1979) S1981E618 [618] - Doraemon, te odio.avi» anuncia una sola, y la
    /// app lo daba por bueno -en verde, listo para aplicar- como el episodio 629, que el
    /// catálogo cuenta con dos historias. El nombre resultante afirmaría que el fichero
    /// trae las dos. Después de eso ya no hay forma de saber que falta una: el nombre
    /// miente y nadie lo sabe. Es el mismo daño que renombrar un fichero de dos con el
    /// número de uno, solo que al revés.
    /// </para>
    /// <para>
    /// No hace falta abrir el fichero ni medir nada. Lo decide una comparación: ¿el título
    /// del fichero se parece más a UNA historia suelta que a las dos juntas? Si sí, el
    /// nombre está hablando de una sola.
    /// </para>
    /// <para>
    /// Contar separadores NO vale, y costó dos pruebas rotas averiguarlo: hay ficheros que
    /// traen las dos historias pegadas sin separador ninguno («Miedo a una Burger
    /// Cangreburger La concha de un hombre»), y ahí «no hay separador» no significa «una
    /// sola historia». Con la comparación esos salen bien solos, porque casan con el
    /// conjunto y no con una suelta.
    /// </para>
    /// <para>
    /// Lo que se decide no puede decidirlo el programa -si el fichero trae el episodio
    /// entero mal nombrado o solo una historia, eso solo lo sabe quien lo vea-, así que se
    /// pregunta en vez de aplicarse.
    /// </para>
    /// </summary>
    private static void MarcarLosQueDeclaranMenosDeLoQuePromete(List<ReindexResolution> resoluciones,
        ReindexCatalog cat, IReadOnlyDictionary<string, ReindexOverride> overrides,
        IndiceTitulos indice, ModoPrioridad modo)
    {
        // La vara de esta carpeta, para confirmar con el reloj lo que sospecha el nombre.
        var unidad = MedidaDelCapitulo.Unidad(resoluciones
            .Where(x => x.Confianza == ReindexConfianza.Alta && x.Episodio != null)
            .Where(x => x.Archivo.Duracion is { } d && d > TimeSpan.Zero)
            .Select(x => (x.Archivo.Duracion!.Value, Math.Max(1, x.Episodio!.TitulosSalida.Count))));

        var aElegir = new List<(ReindexResolution Fila, string Letra)>();

        foreach (var r in resoluciones)
        {
            if (r.Episodio == null) continue;
            if (r.Confianza != ReindexConfianza.Alta) continue;
            // Ya se decidió qué trozo es: el nombre no promete de más.
            if (r.Archivo.SubSegmento != null) continue;

            // Los del idioma de SALIDA, no `TitulosNorm`: aquellos juntan todos los
            // idiomas para poder comparar, así que un episodio con título en castellano y
            // en inglés parecía tener dos historias. Son el mismo título dicho dos veces.
            var historias = r.Episodio.TitulosSalida.Select(TitleMatch.Norm).ToList();
            if (historias.Count <= 1) continue;

            // Si el nombre no aporta título, no hay nada que lo contradiga. El episodio
            // pudo identificarse por el número o por el metadato del contenedor, y de
            // ninguno de los dos se deduce cuántas historias trae el fichero.
            var suyo = TitleMatch.Norm(r.Archivo.TituloNombre);
            if (suyo.Length < 4) continue;

            double conElConjunto = TitleMatch.Sim(suyo, string.Join(" ", historias));
            double conLaMejorSuelta = historias.Max(h => TitleMatch.Sim(suyo, h));

            // El margen evita disparar en los empates: si casa parecido con las dos
            // lecturas, no hay nada claro que objetar y se deja pasar.
            if (conLaMejorSuelta <= conElConjunto + 0.08) continue;


            r.Confianza = ReindexConfianza.Revisar;
            r.Motivo = string.Format(Textos.Instancia.ReindexMotivoNombreDeMas,
                r.Episodio.Num, historias.Count);

            // ¿Y el reloj qué dice? Si en el fichero caben TODAS las historias, lo
            // corto es el nombre y no el contenido. El aviso se queda -renombrarlo
            // entero afirma algo que el nombre no decía- pero se marca, porque
            // entonces la respuesta es la misma para todos los que estén igual y se
            // puede contestar una vez. Sin duración no se marca: la sospecha del
            // nombre basta para preguntar, no para agrupar.
            if (unidad is { } vara && r.Archivo.Duracion is { } dur &&
                MedidaDelCapitulo.Cuadra(dur, historias.Count, vara))
                r.NombreCortoParaEpisodioEntero = true;

            // ¿Y CUÁL de las historias es? Cuando se sabe, no hay que preguntarlo: se
            // propone. Avisar y dejar que la persona la elija a mano es pedirle que repita
            // una cuenta que el programa ya ha hecho.
            //
            // Pero solo cuando el RELOJ lo confirma. Si el fichero mide lo que miden las
            // dos historias, lo que trae es el episodio entero con un nombre pobre, y
            // proponer «a» perdería la otra sin que nadie se entere. Sin duración tampoco
            // se elige: la sospecha del nombre basta para preguntar, no para decidir.
            int cual = historias
                .Select((h, i) => (i, sim: TitleMatch.Sim(suyo, h)))
                .OrderByDescending(x => x.sim)
                .First().i;

            if (unidad is not { } u) continue;
            if (r.Archivo.Duracion is not { } dura) continue;
            if (!MedidaDelCapitulo.Cuadra(dura, 1, u)) continue;

            aElegir.Add((r, ((char)('a' + cual)).ToString()));
        }

        // Se rehace FUERA del bucle y re-resolviendo, no tocando la fila: las señales son
        // inmutables a propósito -la decisión crea una variante, no reescribe la evidencia-
        // y aquí se está decidiendo algo, así que le toca una variante.
        foreach (var (fila, letra) in aElegir)
        {
            int i = resoluciones.IndexOf(fila);
            if (i < 0) continue;

            var rehecha = ResolverUno(fila.Archivo.ConSegmento(letra), cat, overrides, indice, modo);

            // Acertar no lo vuelve automático. Lo que hay debajo es un fichero que no
            // contiene lo que su nombre decía, y eso lo confirma quien lo tenga delante.
            rehecha.Confianza = ReindexConfianza.Revisar;
            rehecha.Motivo = string.Format(Textos.Instancia.ReindexMotivoHistoriaSuelta,
                fila.Episodio!.Num, letra, Reloj(fila.Archivo.Duracion!.Value));
            resoluciones[i] = rehecha;
        }
    }

    private static void Deduplicar(List<ReindexResolution> resoluciones)
    {
        var porDestino = resoluciones
            .Where(r => r.Episodio != null && r.Estado is ReindexEstado.Limpio or ReindexEstado.Corregido)
            // Los sub-segmentos («[438a]», «[438b]») comparten número a propósito: no chocan
            // entre sí, solo con otro fichero que reclame el mismo trozo.
            .GroupBy(r => (r.Episodio!.Num, r.Archivo.SubSegmento ?? ""));

        foreach (var grupo in porDestino)
        {
            if (grupo.Count() < 2) continue;

            var ordenados = grupo.OrderByDescending(r => r.Confianza == ReindexConfianza.Alta)
                                 // El TITULAR manda: un fichero que YA está correctamente
                                 // nombrado (Limpio) es el dueño legítimo de su número y gana a
                                 // cualquier aspirante que solo quiera renombrarse a él. Sin
                                 // esto, un fichero perfecto perdía su propio número por el
                                 // desempate alfabético y salía en «Conflicto» una y otra vez:
                                 // lo «corregías» y reaparecía, porque el número seguía disputado.
                                 .ThenByDescending(r => r.Estado == ReindexEstado.Limpio)
                                 .ThenByDescending(r => r.Score)
                                 // Entre copias por lo demás iguales (el caso típico: el MISMO
                                 // fichero ya en su sitio y una copia en una subcarpeta de
                                 // trabajo tipo «Renombrar»), gana la MÁS SUPERFICIAL: la de la
                                 // biblioteca manda sobre la de staging. Sin esto, cuál caía en
                                 // conflicto dependía del orden de escaneo, y a veces se marcaba
                                 // la copia buena y ya colocada. El desempate por nombre queda al
                                 // final, solo para que el resultado sea estable del todo.
                                 .ThenBy(r => r.Archivo.Path.Count(c => c is '/' or '\\'))
                                 .ThenBy(r => r.Archivo.NombreArchivo, StringComparer.OrdinalIgnoreCase)
                                 .ToList();
            var ganador = ordenados[0];

            // ¿Era pelea de verdad? Solo si el rival llegaba con la misma solvencia. Que un
            // fichero identificado por su título al 100 % coincida con otro que solo traía
            // un número dudoso no es una ambigüedad: es un número dudoso. Marcarlos a los
            // dos convertía casos obvios en trabajo manual — que es justo lo que sobra.
            // Y si el ganador YA está correctamente nombrado (Limpio), no es pelea ninguna: es
            // el titular, se queda verde y punto; el aspirante es el que tiene el problema.
            // Se mira ANTES de tocar a los perdedores, que en el bucle pierden su confianza.
            bool peleaDeVerdad = ordenados[1].Confianza == ReindexConfianza.Alta
                                 && ganador.Estado != ReindexEstado.Limpio;

            // Qué dice el catálogo que ES ese número. Sin esto el mensaje nombraba el
            // episodio en disputa pero no su título, y no había manera de juzgar quién de
            // los dos ficheros tenía razón — que es justo lo que hay que decidir aquí.
            var enDisputa = grupo.First().Episodio!;
            var comoSeLlama = enDisputa.TituloCompleto;

            foreach (var r in grupo)
            {
                if (ReferenceEquals(r, ganador)) continue;
                r.Estado = ReindexEstado.Conflicto;
                r.Confianza = ReindexConfianza.Ninguna;
                r.EsDuplicado = true;
                r.RutaPareja = ganador.Archivo.Path;   // el otro fichero, para enseñar las dos rutas

                // ¿Es la MISMA obra (una copia repetida) o dos ficheros DISTINTOS peleando por el
                // número? Si el título y el sub-segmento coinciden, es lo primero: el mensaje debe
                // decir «fichero repetido» y señalar al otro para que el usuario borre el sobrante.
                bool mismaObra =
                    TitleMatch.Norm(r.Archivo.TituloNombre) == TitleMatch.Norm(ganador.Archivo.TituloNombre)
                    && r.Archivo.SubSegmento == ganador.Archivo.SubSegmento;
                r.Motivo = mismaObra
                    ? string.Format(Textos.Instancia.ReindexMotivoRepetido,
                        grupo.Key.Num, comoSeLlama, ganador.Archivo.NombreArchivo)
                    : string.Format(Textos.Instancia.ReindexMotivoConflicto,
                        ganador.Archivo.NombreArchivo, grupo.Key.Num, comoSeLlama);
            }

            // El ganador tampoco se aplica a ciegas: hubo pelea, que se vea.
            if (peleaDeVerdad && ganador.Confianza == ReindexConfianza.Alta)
            {
                ganador.Confianza = ReindexConfianza.Revisar;
                ganador.Motivo += Textos.Instancia.ReindexMotivoTambienReclamado;
            }
        }
    }

    // ────────────────────────── utilidades ──────────────────────────

    /// <summary>
    /// Los títulos comparables del fichero: el completo, sus segmentos y el metadato.
    ///
    /// De cada uno se añade TAMBIÉN la variante sin el nombre de la serie delante. Muchas
    /// bibliotecas nombran «Serie SxxExx - Título», y ese prefijo contaminaba el parecido
    /// («doraemon 2005 el planeta espejo» vs «el planeta espejo» = 0,71, bajo el umbral):
    /// la vía por título moría y acababa ganando el NÚMERO equivocado del propio fichero.
    /// Se añade como variante en vez de sustituir, por si el título del episodio de verdad
    /// empieza como la serie.
    /// </summary>
    private static IReadOnlyList<TitleBag> TitulosDe(FileSignals f, ReindexCatalog cat)
    {
        var serie = TitleMatch.Norm(cat.Serie);
        var lista = new List<TitleBag>();
        void Add(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            var n = TitleMatch.Norm(s);
            if (n.Length == 0) return;
            if (!lista.Any(b => b.Text == n)) lista.Add(new TitleBag(n));

            if (serie.Length > 0 && n.Length > serie.Length + 1 &&
                n.StartsWith(serie + " ", StringComparison.Ordinal))
            {
                var sinSerie = n[(serie.Length + 1)..];
                if (!lista.Any(b => b.Text == sinSerie)) lista.Add(new TitleBag(sinSerie));
            }
        }
        Add(f.TituloNombre);
        Add(f.TituloMeta);
        // El metadato tambien puede venir multi-historia («A ┃ B»): se compara ademas cada
        // trozo por separado, igual que se hace con el nombre del fichero.
        if (f.TituloMeta != null)
            foreach (var parte in f.TituloMeta.Split(new[] { '┃', '|' },
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Add(parte);
        foreach (var seg in f.Segmentos) Add(seg);
        return lista;
    }

    /// <summary>
    /// Los otros episodios que DE VERDAD compiten con el elegido. Se filtran por cercanía a
    /// su puntuación: una «alternativa» que va treinta puntos por detrás no es una opción,
    /// es ruido.
    /// </summary>
    private static IReadOnlyList<ReindexCandidato> OtrosCandidatos(
        List<(CatalogEpisode ep, IndiceTitulos.Puntuacion p)> puntuados, CatalogEpisode elegido,
        double scoreElegido)
    {
        return IndiceTitulos.RankingDe(puntuados, excluir: elegido, cuantos: 2)
            .Where(x => x.score >= scoreElegido - MargenAlternativa)
            .Select(x => new ReindexCandidato
            {
                Episodio = x.ep,
                Score = x.score,
                Hint = x.score >= TitleMatch.UmbralTitulo ? ReindexHint.Titulo : ReindexHint.TituloDebil,
                Evidencia = new[]
                {
                    string.Format(Textos.Instancia.ReindexEvidenciaTituloCoincide, Pct(x.score)),
                },
            })
            .ToList();
    }

    private static ReindexResolution Fallo(ReindexResolution r, ReindexEstado estado, string motivo)
    {
        r.Estado = estado;
        r.Confianza = ReindexConfianza.Ninguna;
        r.Motivo = motivo;
        return r;
    }

    // El número va siempre en cultura invariante (nadie escribe «87,0 %» aquí);
    // lo que cambia con el idioma es el espacio antes del signo.
    internal static string Pct(double score) =>
        string.Format(Textos.Instancia.ReindexPorcentaje,
            (score * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture));
}

/// <summary>
/// Índice de títulos del catálogo con los recuentos precalculados. Existe solo por
/// velocidad: sin la cota superior, comparar 600 ficheros contra 1800 episodios es
/// inviable, y el cross-check anti-remake obliga a barrer la serie entera cada vez.
/// </summary>
internal sealed class IndiceTitulos
{
    private readonly Dictionary<CatalogEpisode, TitleBag[]> _bolsas = new();

    public IndiceTitulos(ReindexCatalog cat)
    {
        foreach (var e in cat.Episodios)
        {
            var bolsas = e.TitulosNorm.Select(t => new TitleBag(t)).ToList();
            // Un fichero puede nombrar las DOS historias JUNTAS, sin separador («Historia A
            // Historia B»), como las descargas AMZN. Contra un catálogo con las historias
            // PARTIDAS en títulos sueltos, ese título junto no casaba con ninguno y el parecido
            // caía a «Revisar» (no aplicable en bloque). Se indexa TAMBIÉN la concatenación de
            // las historias (idioma de salida, en su orden) para que el título junto case fuerte
            // sin perder el caso de una sola historia —esa sigue casando su título suelto—.
            if (e.TitulosSalida.Count > 1)
            {
                var unido = TitleMatch.Norm(string.Concat(e.TitulosSalida));
                if (unido.Length > 0 && !e.TitulosNorm.Contains(unido))
                    bolsas.Add(new TitleBag(unido));
            }
            _bolsas[e] = bolsas.ToArray();
        }
    }

    /// <summary>
    /// Lo que se sabe de un episodio frente a un fichero. El parecido manda, pero cuando
    /// dos episodios empatan en parecido hay más evidencia que mirar antes de rendirse y
    /// preguntarle al usuario.
    /// </summary>
    internal readonly record struct Puntuacion(double Sim, int SegmentosCasados, bool MismaTemporada)
    {
        public static readonly Puntuacion Cero = new(0, 0, false);

        /// <summary>
        /// ¿Es esta puntuación mejor que la otra? Primero el parecido; si empatan (dentro del
        /// margen), gana el que case MÁS trozos del nombre, y en último término el de la
        /// misma temporada que dice el fichero.
        /// </summary>
        public bool MejorQue(in Puntuacion otra)
        {
            if (Sim > otra.Sim + ReindexEngine.MargenDesempate) return true;
            if (otra.Sim > Sim + ReindexEngine.MargenDesempate) return false;

            if (SegmentosCasados != otra.SegmentosCasados) return SegmentosCasados > otra.SegmentosCasados;
            if (MismaTemporada != otra.MismaTemporada) return MismaTemporada;
            return Sim > otra.Sim;
        }

        /// <summary>¿Siguen siendo indistinguibles después de mirarlo todo?</summary>
        public bool IndistinguibleDe(in Puntuacion otra) =>
            Math.Abs(Sim - otra.Sim) <= ReindexEngine.MargenDesempate
            && SegmentosCasados == otra.SegmentosCasados
            && MismaTemporada == otra.MismaTemporada;
    }

    /// <summary>Mejor episodio y su puntuación sobre los títulos dados.</summary>
    public (CatalogEpisode? ep, double score) MejorPorTitulo(IReadOnlyList<TitleBag> titulos,
                                                            IReadOnlyList<CatalogEpisode> candidatos)
        => MejorPorTitulo(titulos, candidatos, null) is var (e, p) ? (e, p.Sim) : default;

    internal (CatalogEpisode? ep, Puntuacion p) MejorPorTitulo(IReadOnlyList<TitleBag> titulos,
        IReadOnlyList<CatalogEpisode> candidatos, int? temporadaArchivo)
        => MejorDe(Puntuados(titulos, candidatos, temporadaArchivo));

    /// <summary>
    /// UN solo barrido del catálogo, con la puntuación de cada episodio que da algo.
    ///
    /// Existe porque las dos preguntas que se le hacen al catálogo —«¿cuál es el mejor?» y
    /// «¿cuáles son los siguientes, para ofrecerlos?»— son el MISMO cálculo, y se estaban
    /// haciendo por separado: dos recorridos completos comparando textos, uno detrás de
    /// otro. Medido sobre una biblioteca de 546 ficheros, esa duplicación era la mitad de
    /// los 20 segundos que tardaba una simulación.
    /// </summary>
    internal List<(CatalogEpisode ep, Puntuacion p)> Puntuados(IReadOnlyList<TitleBag> titulos,
        IReadOnlyList<CatalogEpisode> candidatos, int? temporadaArchivo)
    {
        var lista = new List<(CatalogEpisode ep, Puntuacion p)>();
        foreach (var ep in candidatos)
        {
            var p = Puntuar(titulos, ep, temporadaArchivo);
            if (p.Sim > 0) lista.Add((ep, p));
        }
        return lista;
    }

    /// <summary>
    /// El ganador del barrido. Recorre en el orden del catálogo y solo cambia de líder si el
    /// nuevo es ESTRICTAMENTE mejor: ante un empate manda el primero, que es como se elegía
    /// antes de unificar los dos recorridos. Ordenar y coger el primero no serviría — el
    /// orden de .NET no es estable y los empates saldrían por otro sitio.
    /// </summary>
    internal static (CatalogEpisode? ep, Puntuacion p) MejorDe(List<(CatalogEpisode ep, Puntuacion p)> puntuados)
    {
        CatalogEpisode? mejor = null;
        var mejorP = Puntuacion.Cero;
        foreach (var (ep, p) in puntuados)
            if (mejor == null || p.MejorQue(in mejorP)) { mejorP = p; mejor = ep; }
        return (mejor, mejorP);
    }

    /// <summary>
    /// Puntúa un episodio: el mejor parecido, cuántos trozos del nombre casan de verdad, y si
    /// la temporada coincide con la que declara el fichero.
    /// </summary>
    internal Puntuacion Puntuar(IReadOnlyList<TitleBag> titulos, CatalogEpisode ep, int? temporadaArchivo)
    {
        if (!_bolsas.TryGetValue(ep, out var bolsas) || bolsas.Length == 0) return Puntuacion.Cero;

        double mejor = 0;
        int casados = 0;
        foreach (var t in titulos)
        {
            double mejorDeEste = 0;
            foreach (var b in bolsas)
            {
                if (TitleMatch.CotaSuperior(in t, in b) <= mejorDeEste) continue;
                double s = TitleMatch.Sim(t.Text, b.Text);
                if (s > mejorDeEste) mejorDeEste = s;
            }
            if (mejorDeEste > mejor) mejor = mejorDeEste;
            // Un episodio que explica DOS de los trozos del nombre encaja mejor que otro que
            // solo explica uno, aunque los dos den 1,00 en su mejor trozo.
            if (mejorDeEste >= TitleMatch.UmbralSegmento) casados++;
        }

        bool mismaTemp = temporadaArchivo.HasValue && ep.Temporada == temporadaArchivo;
        return new Puntuacion(mejor, casados, mismaTemp);
    }

    /// <summary>Score de UN episodio concreto contra los títulos dados.</summary>
    public double ScoreDe(IReadOnlyList<TitleBag> titulos, CatalogEpisode ep) => Score(titulos, ep, 0);

    /// <summary>Los siguientes mejores, para ofrecer alternativas de un clic.</summary>
    public List<(CatalogEpisode ep, double score)> Ranking(IReadOnlyList<TitleBag> titulos,
        IReadOnlyList<CatalogEpisode> candidatos, CatalogEpisode? excluir, int cuantos,
        int? temporadaArchivo = null)
        => RankingDe(Puntuados(titulos, candidatos, temporadaArchivo), excluir, cuantos);

    /// <summary>
    /// Los siguientes mejores de un barrido ya hecho. Se ordenan con el MISMO criterio con el
    /// que se eligió al ganador: si no, la primera alternativa podría no ser la segunda mejor
    /// y el usuario elegiría a ciegas.
    /// </summary>
    internal static List<(CatalogEpisode ep, double score)> RankingDe(
        List<(CatalogEpisode ep, Puntuacion p)> puntuados, CatalogEpisode? excluir, int cuantos)
    {
        var lista = puntuados.Where(x => !ReferenceEquals(x.ep, excluir)).ToList();
        lista.Sort((a, b) => a.p.MejorQue(in b.p) ? -1 : b.p.MejorQue(in a.p) ? 1 : 0);
        return lista.Take(cuantos).Select(x => (x.ep, x.p.Sim)).ToList();
    }

    /// <summary>
    /// Score de un episodio = MÁXIMO sim sobre todos sus títulos (segmentos es/lat + alias).
    /// <paramref name="minimoUtil"/> permite abandonar pronto: si ni la cota superior supera
    /// lo ya encontrado, no hace falta calcular la similitud real.
    /// </summary>
    private double Score(IReadOnlyList<TitleBag> titulos, CatalogEpisode ep, double minimoUtil)
    {
        if (!_bolsas.TryGetValue(ep, out var bolsas)) return 0;
        double mejor = 0;
        foreach (var t in titulos)
            foreach (var b in bolsas)
            {
                double cota = TitleMatch.CotaSuperior(in t, in b);
                if (cota <= mejor || cota <= minimoUtil) continue;   // imposible que mejore: se salta
                double s = TitleMatch.Sim(t.Text, b.Text);
                if (s > mejor) mejor = s;
            }
        return mejor;
    }
}
