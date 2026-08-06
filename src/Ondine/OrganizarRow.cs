using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// Una opción del resolvedor de conflictos, ya con su consecuencia a la vista.
///
/// Incluye SIEMPRE al candidato que la app ha elegido, no solo a los descartados: antes se
/// enseñaban únicamente las alternativas, así que la propuesta buena no aparecía por ningún
/// lado y las dos opciones ofrecidas parecían —con razón— equivocadas.
/// </summary>
public sealed class CandidatoVista
{
    public required CatalogEpisode Episodio { get; init; }
    public required string Etiqueta { get; init; }
    public required string Titulo { get; init; }
    public required string NombreResultante { get; init; }
    public IReadOnlyList<string> Evidencia { get; init; } = Array.Empty<string>();
    public bool EsElegido { get; init; }

    /// <summary>«E318 · 2014»</summary>
    public string Cabecera => Episodio.Temporada.HasValue
        ? $"E{Episodio.Num} · {Episodio.Temporada}"
        : $"E{Episodio.Num}";

    public string TextoBoton => string.Format(
        EsElegido ? Textos.Instancia.OrganizarBotonDejar : Textos.Instancia.OrganizarBotonCambiar,
        Episodio.Num);
}

/// <summary>
/// Una fila de la tabla de «Organizar»: traduce la resolución del motor a lo que se pinta.
/// El motor no sabe nada de colores ni de glifos, y así sigue siendo.
/// </summary>
public sealed class OrganizarRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    /// <summary>Formato del diseño: coma decimal y dos cifras («0,99»).</summary>
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    public ReindexResolution Res { get; }
    public LibraryTemplate Plantilla { get; }
    public ReindexCatalog Catalogo { get; }

    /// <summary>Carpeta de temporada de la que salió, ya con nombre presentable.</summary>
    public string Grupo { get; }

    private bool _marcado = true;
    /// <summary>
    /// Si esta fila entra en el próximo «Aplicar». Solo pesa cuando la fila está lista: los
    /// conflictos y las dudas no se aplican jamás, marcados o no.
    /// </summary>
    public bool Marcado
    {
        get => _marcado;
        set { _marcado = value; N(); }
    }

    private bool _primeraDeGrupo;
    /// <summary>Esta fila abre su temporada: lleva encima la banda separadora.</summary>
    public bool PrimeraDeGrupo
    {
        get => _primeraDeGrupo;
        set { _primeraDeGrupo = value; N(); }
    }

    private string _grupoConteo = "";
    /// <summary>«32 ficheros» de esa temporada, contando solo los que el filtro deja ver.</summary>
    public string GrupoConteo
    {
        get => _grupoConteo;
        set { _grupoConteo = value; N(); }
    }

    private int _iguales;
    /// <summary>
    /// Cuántas OTRAS filas están atascadas exactamente por lo mismo que esta.
    /// Lo pone la vista, que es la única que ve el lote entero.
    ///
    /// <para>
    /// A cero no se ofrece nada: un botón que dice «y las otras 0» es ruido, y
    /// uno que aparece siempre acaba pulsándose sin leerlo.
    /// </para>
    /// </summary>
    public int Iguales
    {
        get => _iguales;
        set
        {
            if (_iguales == value) return;
            _iguales = value;
            N(); N(nameof(DejarIgualesTexto)); N(nameof(VerDejarIguales));
        }
    }

    public string DejarIgualesTexto =>
        string.Format(Textos.Instancia.OrganizarDejarIguales, Iguales);

    public System.Windows.Visibility VerDejarIguales => Iguales > 0
        ? System.Windows.Visibility.Visible
        : System.Windows.Visibility.Collapsed;

    private int _confirmables;
    /// <summary>Cuántas OTRAS filas se pueden confirmar a la vez que esta.</summary>
    public int Confirmables
    {
        get => _confirmables;
        set
        {
            if (_confirmables == value) return;
            _confirmables = value;
            N(); N(nameof(ConfirmarIgualesTexto)); N(nameof(VerConfirmarIguales));
        }
    }

    public string ConfirmarIgualesTexto =>
        string.Format(Textos.Instancia.OrganizarConfirmarIguales, Confirmables + 1);

    public System.Windows.Visibility VerConfirmarIguales => Confirmables > 0
        ? System.Windows.Visibility.Visible
        : System.Windows.Visibility.Collapsed;

    private bool _apartada;
    /// <summary>
    /// Está en la cola. No cambia lo que la app propone: solo la señala, para poder volver
    /// a ella desde la cola sin buscarla entre cientos. Sobrevive al cierre de la app.
    /// </summary>
    public bool Apartada
    {
        get => _apartada;
        set { _apartada = value; N(); N(nameof(VerApartada)); }
    }

    public System.Windows.Visibility VerApartada =>
        Apartada ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>
    /// «E1bc» cuando el fichero es SOLO algunas historias del episodio. Se enseña como píldora
    /// porque es una decisión que cambia el nombre final y, sin verla, no hay forma de saber que
    /// esa fila no es el episodio entero: en la tabla se leía igual que cualquier otra.
    /// </summary>
    public string SegmentoPildora
    {
        get
        {
            if (Res.Episodio is not { } ep) return "";
            var propio = SegElegido ?? "";
            if (propio.Length == 0 && _tambien.Count == 0) return "";
            var extra = string.Concat(_tambien.Select(h => $"+{h.Episodio.Num}{h.Segmento}"));
            return $"E{ep.Num}{propio}{extra}";
        }
    }

    public Visibility VerSegmento =>
        SegmentoPildora.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Qué historias trae, con sus nombres, para el tooltip de la píldora.</summary>
    public string SegmentoTooltip
    {
        get
        {
            if (Res.Episodio is not { } ep || SegmentoPildora.Length == 0) return "";

            static IEnumerable<string> Cuales(CatalogEpisode e, string? seg) =>
                seg is { Length: > 0 }
                    ? seg.Select(c => char.ToLowerInvariant(c) - 'a')
                         .Where(i => i >= 0 && i < e.TitulosSalida.Count)
                         .Select(i => string.Format(Textos.Instancia.OrganizarHistoriaDe,
                                                    e.TitulosSalida[i], e.Num))
                    : new[] { string.Format(Textos.Instancia.OrganizarEpisodioEntero, e.Num) };

            var todas = Cuales(ep, SegElegido).Concat(_tambien.SelectMany(h => Cuales(h.Episodio, h.Segmento)));
            var aviso = _tambien.Count > 0 ? Textos.Instancia.OrganizarSegmentoAviso : "";
            return string.Format(Textos.Instancia.OrganizarSegmentoTrae, string.Join(", ", todas)) + aviso;
        }
    }

    /// <summary>Nombre final que se escribirá, o null si esta fila no se toca.</summary>
    public string? NombreNuevo { get; private set; }

    /// <summary>
    /// Dónde está el fichero AHORA: tras aplicar, el de disco ya es el nombre nuevo y abrir
    /// la ruta vieja fallaría.
    /// </summary>
    public string RutaActual => Aplicado && NombreNuevo != null
        ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Res.Archivo.Path) ?? "", NombreNuevo)
        : Res.Archivo.Path;

    private bool _aplicado;
    /// <summary>Ya renombrado en disco: la fila pasa a «Hecho ✓».</summary>
    public bool Aplicado
    {
        get => _aplicado;
        set { _aplicado = value; RefrescarTodo(); }
    }

    public OrganizarRow(ReindexResolution res, ReindexCatalog catalogo, LibraryTemplate plantilla,
                        string grupo = "")
    {
        Res = res;
        Catalogo = catalogo;
        Plantilla = plantilla;
        Grupo = grupo;
        Recalcular();
    }

    /// <summary>
    /// Recalcula el nombre propuesto. Se llama al crear la fila y cada vez que el usuario
    /// resuelve un conflicto o cambia la plantilla.
    /// </summary>
    public void Recalcular()
    {
        var archivo = SegElegido != null ? Res.Archivo.ConSegmento(SegElegido) : Res.Archivo;
        NombreNuevo = Res.Episodio != null && Res.Estado != ReindexEstado.Error
            ? Plantilla.Render(Catalogo, Res.Episodio, archivo, _tambien.Count > 0 ? _tambien : null)
            : null;
        RefrescarTodo();
    }

    // ── Historias de OTROS episodios metidas en este mismo fichero ──

    private readonly List<LibraryTemplate.Historia> _tambien = new();

    /// <summary>Las historias añadidas de otros episodios. Vacío en el caso normal.</summary>
    public IReadOnlyList<LibraryTemplate.Historia> Tambien => _tambien;

    /// <summary>
    /// Apunta que este fichero trae TAMBIÉN una historia de otro episodio. El nombre pasa a
    /// llevar el código compuesto («E1b+2b»): la app no sabrá releerlo y al reanalizar saldrá
    /// como duda, y así debe ser — es un fichero que no encaja en ningún episodio.
    /// </summary>
    public void AnadirHistoria(CatalogEpisode ep, string? seg)
    {
        _tambien.Add(new LibraryTemplate.Historia(ep, seg));
        Recalcular();
    }

    /// <summary>Deshace las historias añadidas y deja la fila como un episodio normal.</summary>
    public void QuitarHistorias()
    {
        if (_tambien.Count == 0) return;
        _tambien.Clear();
        Recalcular();
    }

    private void RefrescarTodo()
    {
        foreach (var p in new[]
        {
            nameof(EstadoTexto), nameof(EstadoGlifo), nameof(EstadoFg), nameof(EstadoBg), nameof(EstadoBorde),
            nameof(Original), nameof(Propuesta), nameof(PropuestaDestacada), nameof(PropuestaFg),
            nameof(PorQue), nameof(Explicacion), nameof(TieneDetalle), nameof(Aplicado), nameof(Candidatos),
            // La casilla de aplicar se enseña según esto: al resolver un conflicto la fila
            // pasa a estar lista y su casilla tiene que aparecer en ese momento.
            nameof(ListoParaAplicar), nameof(EstadoTooltip),
            nameof(RecomiendaRecortar), nameof(VerRecortar), nameof(TituloDetalle), nameof(VerSelector),
            nameof(EsRepetido), nameof(VerBorrarCopia),
            nameof(SegmentoPildora), nameof(VerSegmento), nameof(SegmentoTooltip),
            nameof(DuracionVisible), nameof(DuracionOrden), nameof(DuracionTooltip),
            nameof(NombrePropio), nameof(CarpetaPropia), nameof(NombrePareja), nameof(CarpetaPareja),
        }) N(p);
    }

    // ───────────────────────── columna ESTADO ─────────────────────────

    /// <summary>
    /// Palabra + glifo + color. El glifo y la palabra van SIEMPRE: con daltonismo, o en una
    /// captura en blanco y negro, el color por sí solo no dice nada.
    /// </summary>
    public string EstadoTexto
    {
        get
        {
            if (Aplicado) return Textos.Instancia.OrganizarEstadoHecho;
            // «Partir» manda sobre el estado: un fichero con dos episodios no es un renombrado
            // pendiente, es un corte. Decirle «Con cambios» invitaría justo al error de
            // ponerle un nombre y perder la otra historia.
            if (RecomiendaRecortar) return Textos.Instancia.OrganizarEstadoPartir;
            return Res.Estado switch
            {
                // «Correcto», no «Limpio»: el nombre ya está bien, no es que se haya limpiado.
                ReindexEstado.Limpio => Textos.Instancia.OrganizarEstadoCorrecto,
                // «Con cambios», no «Corregido»: aún no se ha corregido nada — hay un cambio
                // propuesto que espera a que lo apliques.
                ReindexEstado.Corregido => Textos.Instancia.OrganizarEstadoConCambios,
                ReindexEstado.Especial => Textos.Instancia.OrganizarEstadoEspecial,
                ReindexEstado.Conflicto => Textos.Instancia.OrganizarEstadoConflicto,
                _ => Textos.Instancia.Error,
            };
        }
    }

    public string EstadoGlifo
    {
        get
        {
            if (Aplicado) return "✓";
            if (RecomiendaRecortar) return "✂";
            return Res.Estado switch
            {
                ReindexEstado.Limpio => "●",
                ReindexEstado.Corregido => "↻",
                ReindexEstado.Especial => "▲",
                ReindexEstado.Conflicto => "◆",
                _ => "✕",
            };
        }
    }

    /// <summary>
    /// El color sale de la CONFIANZA, no del estado. Así el reparto queda limpio: la palabra
    /// dice qué es («Limpio», «Especial»…) y el color dice si te puedes fiar. Atándolo al
    /// estado salían filas en verde que luego no se aplicaban —un «Limpio» sin fecha que lo
    /// confirme necesita revisión igual, y pintarlo de verde era mentir.
    /// </summary>
    private string Tono => Aplicado || SinCambios ? "Ok" : Res.Confianza switch
    {
        ReindexConfianza.Alta => "Ok",
        ReindexConfianza.Revisar => "Warn",
        _ => "Danger",
    };

    /// <summary>
    /// Explica por qué el color no siempre concuerda con la palabra. Un «Limpio» en ámbar
    /// desconcierta con razón: dice que el fichero YA se llama como toca, pero que no está
    /// confirmado que sea ese episodio. Sin decirlo, parece un fallo de la app.
    /// </summary>
    public string EstadoTooltip
    {
        get
        {
            if (Aplicado) return Textos.Instancia.OrganizarTooltipHecho;

            if (SinCambios) return Textos.Instancia.OrganizarTooltipSinCambios;

            var porque = string.IsNullOrWhiteSpace(Res.Motivo) ? "" : $" ({Res.Motivo})";
            var plantilla = Res.Confianza switch
            {
                ReindexConfianza.Alta => Textos.Instancia.OrganizarTooltipSegura,
                ReindexConfianza.Revisar => Textos.Instancia.OrganizarTooltipRevisar,
                _ => Textos.Instancia.OrganizarTooltipDecides,
            };
            return string.Format(plantilla, EstadoTexto, porque);
        }
    }

    public Brush EstadoFg => Rec($"Org{Tono}");
    public Brush EstadoBg => Rec($"Org{Tono}Bg");
    public Brush EstadoBorde => Rec($"Org{Tono}Border");

    private static Brush Rec(string clave) =>
        Application.Current?.TryFindResource(clave) as Brush ?? Brushes.Transparent;

    // ───────────────────── columna FICHERO ORIGINAL ─────────────────────

    public string Original => Res.Archivo.NombreArchivo;

    // ─────────────────────── columna PROPUESTA ───────────────────────

    public string Propuesta
    {
        get
        {
            if (Aplicado) return NombreNuevo ?? Original;

            switch (Res.Estado)
            {
                case ReindexEstado.Error:
                    return Textos.Instancia.OrganizarPropuestaNoParseable;

                // Un duplicado no se explica con los candidatos de título: lo que hay que
                // contar es quién le ganó el sitio. Mezclar las dos cosas hacía una frase
                // larguísima que no decía ninguna de las dos bien.
                case ReindexEstado.Conflicto when Res.EsDuplicado:
                    return "- " + Res.Motivo;

                case ReindexEstado.Conflicto when Res.Alternativas.Count >= 2:
                    var a = Res.Alternativas[0].Episodio;
                    var b = Res.Alternativas[1].Episodio;
                    return string.Format(Textos.Instancia.OrganizarPropuestaDosTitulos,
                                         a.Num, a.Temporada, b.Num, b.Temporada);

                case ReindexEstado.Conflicto:
                    return "- " + Res.Motivo;

                case ReindexEstado.Especial when Res.Episodio != null:
                    return string.Format(Textos.Instancia.OrganizarPropuestaEspecial,
                                         Res.Episodio.Num, Res.Episodio.TituloPrincipal);

                case ReindexEstado.Especial:
                    return Textos.Instancia.OrganizarPropuestaQueEspecial;
            }

            // Ya trae el nombre correcto: no hay nada que hacer, y decirlo es la información
            if (NombreNuevo == null) return Textos.Instancia.OrganizarPropuestaSinPropuesta;
            if (string.Equals(NombreNuevo, Original, StringComparison.OrdinalIgnoreCase))
                return Textos.Instancia.OrganizarPropuestaSinCambios;
            return NombreNuevo;
        }
    }

    /// <summary>Solo se destaca lo que de verdad va a cambiar; lo demás queda atenuado.</summary>
    public bool PropuestaDestacada =>
        !Aplicado && NombreNuevo != null && Res.Estado is ReindexEstado.Corregido or ReindexEstado.Especial
        && !string.Equals(NombreNuevo, Original, StringComparison.OrdinalIgnoreCase);

    public Brush PropuestaFg => PropuestaDestacada || Aplicado
        ? Rec("Text")
        : Res.Estado is ReindexEstado.Conflicto or ReindexEstado.Error
            ? Rec("Neutral400")
            : Rec("Neutral500");

    // ───────────────────────── columna POR QUÉ ─────────────────────────

    /// <summary>
    /// Versión corta: la evidencia y su puntuación. La explicación larga del motor vive en
    /// el tooltip, que es donde cabe sin romper la tabla.
    /// </summary>
    public string PorQue
    {
        get
        {
            var partes = new List<string>();

            string evidencia = Res.Hint switch
            {
                ReindexHint.Override => Textos.Instancia.OrganizarPorQueDecision,
                ReindexHint.IndiceFecha => Textos.Instancia.OrganizarPorQueNumFecha,
                ReindexHint.OrdinalTemporada => Textos.Instancia.OrganizarPorQueOrdinal,
                ReindexHint.Titulo => Res.Archivo.Segmentos.Count > 1
                    ? string.Format(Textos.Instancia.OrganizarPorQueSegmentos, Res.Archivo.Segmentos.Count)
                    : Textos.Instancia.OrganizarPorQueTitulo,
                ReindexHint.IndiceFechaAprox => Textos.Instancia.OrganizarPorQueNumFechaAprox,
                ReindexHint.TituloDebil => Textos.Instancia.OrganizarPorQueTituloDebil,
                _ => Textos.Instancia.OrganizarPorQueSinSenales,
            };
            partes.Add($"{evidencia} {Res.Score.ToString("0.00", Es)}");

            if (Res.Alternativas.Count > 0)
                partes.Add(string.Format(Res.Estado == ReindexEstado.Conflicto
                    ? Textos.Instancia.OrganizarPorQueCandidatos
                    : Textos.Instancia.OrganizarPorQueAlternativas, Res.Alternativas.Count));

            return string.Join(" · ", partes);
        }
    }

    /// <summary>La prosa completa del motor, para el tooltip de la fila.</summary>
    public string Explicacion => Res.Motivo;

    /// <summary>¿Merece la pena desplegar el resolutor bajo esta fila?</summary>
    /// <summary>
    /// Si la fila se puede desplegar.
    ///
    /// <para>
    /// También las verdes. El motor les vacía las alternativas a propósito -ofrecer
    /// candidatos peores en una fila ya resuelta es ruido que invita a un clic
    /// equivocado-, y de rebote se quedaban sin poder abrirse: si la app acertaba pero
    /// no era lo que tú querías, no había por dónde cambiarlo. Se abren igual; lo que
    /// no traen es la lista de candidatos ni el botón de confirmar uno, que ahí no
    /// significa nada. Quedan las dos salidas que sí valen: elegir otro episodio a mano
    /// y dejarlo como está.
    /// </para>
    /// </summary>
    // ───────────────────────── columna DURACIÓN ─────────────────────────

    /// <summary>
    /// Lo que dura, tal cual se enseña. Un guion cuando no se sabe: dejarlo en blanco se
    /// confunde con «cero» y con «todavía cargando», y aquí no saber es un estado normal
    /// -Windows no tiene apuntada la ficha de todos los ficheros-.
    /// </summary>
    public string DuracionVisible => Res.Archivo.Duracion is { } d
        ? (d.TotalHours >= 1 ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}" : $"{d.Minutes}:{d.Seconds:00}")
        : "-";

    /// <summary>Para ordenar. Las desconocidas al final, no confundidas con las cortas.</summary>
    public double DuracionOrden => Res.Archivo.Duracion?.TotalSeconds ?? double.MaxValue;

    public string DuracionTooltip => Res.Archivo.Duracion is null
        ? Textos.Instancia.OrganizarDuracionDesconocida
        : string.Format(Textos.Instancia.OrganizarDuracionTip, Res.UnidadDeHistoria is { } u
            ? (u.TotalHours >= 1 ? $"{(int)u.TotalHours}:{u.Minutes:00}:{u.Seconds:00}" : $"{u.Minutes}:{u.Seconds:00}")
            : "?");

    public bool TieneDetalle => Res.Alternativas.Count > 0 || Res.EsDuda || Res.Episodio != null;

    /// <summary>
    /// Las opciones del resolvedor: primero la que la app propone, después las descartadas.
    /// Cada una enseña el nombre que quedaría, porque «Elegir E318» sin ver la consecuencia
    /// obliga a decidir a ciegas.
    /// </summary>
    public IReadOnlyList<CandidatoVista> Candidatos
    {
        get
        {
            var lista = new List<CandidatoVista>();

            if (Res.Episodio != null)
                lista.Add(Ver(Res.Episodio, Res.Score, esElegido: true, Res.Alternativas.Count > 0
                    ? new[] { Textos.Instancia.OrganizarCandidatoActual }
                    : Array.Empty<string>()));

            foreach (var alt in Res.Alternativas)
            {
                if (alt.Episodio.Num == Res.Episodio?.Num) continue;   // no repetir al elegido
                lista.Add(Ver(alt.Episodio, alt.Score, esElegido: false, alt.Evidencia));
            }
            return lista;
        }
    }

    private CandidatoVista Ver(CatalogEpisode ep, double score, bool esElegido, IReadOnlyList<string> evidencia)
    {
        // Para el ELEGIDO se reutiliza el nombre que ya calculó Recalcular(), no se
        // vuelve a componer. Así la tarjeta no puede discrepar de la fila: antes
        // renderizaba sin el segmento elegido ni las historias fusionadas, y en un
        // fichero con dos episodios dentro la tarjeta decía «quedaría como
        // S1993E1260 - El invento para hacer bonsáis» mientras el renombrado de
        // verdad ponía «S1993E1260+1261 - ... + La rueda auxiliar invisible».
        // Dos formas de calcular lo mismo son dos respuestas en cuanto una cambia.
        //
        // Las alternativas sí se componen aquí, porque son un «qué pasaría si»,
        // pero con las MISMAS piezas: elegir otro episodio no descarta las
        // historias que ya se habían fusionado.
        var archivo = SegElegido != null ? Res.Archivo.ConSegmento(SegElegido) : Res.Archivo;
        var nombre = esElegido && NombreNuevo != null
            ? NombreNuevo
            : Plantilla.Render(Catalogo, ep, archivo, _tambien.Count > 0 ? _tambien : null);

        return new CandidatoVista
        {
            Episodio = ep,
            EsElegido = esElegido,
            Etiqueta = (esElegido
                           ? Textos.Instancia.OrganizarCandidatoProbable
                           : Textos.Instancia.OrganizarCandidatoAlternativa)
                       + $" · {score.ToString("0.00", Es)}",
            Titulo = ep.TituloCompleto,
            NombreResultante = nombre ?? Textos.Instancia.OrganizarCandidatoSinNombre,
            Evidencia = evidencia,
        };
    }

    // ───────────────────────── clasificación ─────────────────────────

    /// <summary>
    /// Aplicar esta fila no cambiaría nada: ya se llama exactamente como la plantilla manda.
    ///
    /// Y eso, además de no dar trabajo, es la identificación MÁS FUERTE que hay. El nombre
    /// propuesto lleva serie, temporada, número y título; que coincida carácter a carácter
    /// con el que el fichero ya tiene no es un parecido de 0,81, es una confirmación —
    /// normalmente porque ya lo procesaste en su día—. Por eso una fila así va en verde
    /// aunque la cascada la resolviera con poca confianza: no hay nada que decidir.
    /// </summary>
    public bool SinCambios => !Aplicado && NombreNuevo != null
        && string.Equals(NombreNuevo, Original, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// El estado a efectos de contadores y de filtros. Una fila ya aplicada está BIEN en el
    /// disco, así que cuenta como limpia: seguir sumándola a «corregidos» decía que quedaba
    /// trabajo pendiente que ya no existe, y al filtrar por «corregidos» salían mezcladas
    /// las hechas con las que faltan.
    ///
    /// <see cref="ReindexResolution.Estado"/> no se toca: ese es el veredicto de la
    /// identificación y es lo que explica POR QUÉ se renombró.
    /// </summary>
    public ReindexEstado EstadoVisible => Aplicado ? ReindexEstado.Limpio : Res.Estado;

    /// <summary>
    /// Un fichero con dos episodios dentro no se arregla eligiendo número: hay que partirlo.
    /// Es lo único que resuelve el caso, así que se ofrece donde se está decidiendo.
    /// </summary>
    public bool RecomiendaRecortar => !Aplicado && Res.TraeDosEpisodios;
    public Visibility VerRecortar => RecomiendaRecortar ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Es una copia repetida (mismo episodio que otro fichero): se puede borrar el sobrante.</summary>
    public bool EsRepetido => !Aplicado && Res.EsDuplicado;
    public Visibility VerBorrarCopia => EsRepetido ? Visibility.Visible : Visibility.Collapsed;

    // ── Las DOS rutas de un «fichero repetido» (#128): esta y su pareja, para elegir cuál borrar ──

    /// <summary>Nombre de ESTE fichero (la copia).</summary>
    public string NombrePropio => Res.Archivo.NombreArchivo;
    /// <summary>Carpeta de ESTE fichero, para distinguirlo de su pareja cuando comparten nombre.</summary>
    public string CarpetaPropia => System.IO.Path.GetDirectoryName(Res.Archivo.Path) ?? "";
    /// <summary>Nombre del OTRO fichero (el que la app conserva), o «(desconocido)».</summary>
    public string NombrePareja => Res.RutaPareja != null
        ? System.IO.Path.GetFileName(Res.RutaPareja)
        : Textos.Instancia.OrganizarDesconocido;
    /// <summary>Carpeta del OTRO fichero.</summary>
    public string CarpetaPareja => Res.RutaPareja != null ? System.IO.Path.GetDirectoryName(Res.RutaPareja) ?? "" : "";

    /// <summary>
    /// Su fichero rival se fue a la Papelera: esta fila deja de ser un duplicado y vuelve a ser la
    /// copia válida del episodio que ya tenía identificado, lista para aplicar.
    /// </summary>
    public void MarcarRecuperadaDeDuplicado()
    {
        Res.EsDuplicado = false;
        Res.RutaPareja = null;
        Res.Estado = Res.Episodio?.Especial == true ? ReindexEstado.Especial : ReindexEstado.Corregido;
        Res.Confianza = ReindexConfianza.Alta;
        Res.Motivo = Textos.Instancia.OrganizarMotivoOtroALaPapelera;
        Res.Alternativas = Array.Empty<ReindexCandidato>();
        Recalcular();
    }

    /// <summary>Cabecera del panel: partir manda sobre resolver, y un repetido tiene su propio texto.</summary>
    public string TituloDetalle => RecomiendaRecortar
        ? Textos.Instancia.OrganizarDetalleDosEpisodios
        : EsRepetido ? Textos.Instancia.OrganizarDetalleRepetido
        // En una fila que la app resolvió sola no hay conflicto ninguno, y llamarlo asi
        // seria mentirle a quien solo queria cambiar la propuesta.
        : !Res.EsDuda && Res.Alternativas.Count == 0
            ? Textos.Instancia.OrganizarDetalleCambiar
        : Textos.Instancia.OrganizarDetalleConflicto;

    /// <summary>
    /// El selector de «elige un episodio» se esconde cuando la respuesta es partir (ofrecer E588 o
    /// E589 es empujar al error de perder el otro) o cuando es una copia repetida (no hay episodio
    /// que elegir: lo que toca es borrar el sobrante).
    /// </summary>
    public Visibility VerSelector => RecomiendaRecortar || EsRepetido ? Visibility.Collapsed : Visibility.Visible;

    // Ni pendiente ni por despachar: no hay nada que hacerle.
    public bool EsDuda => !Aplicado && !SinCambios && Res.EsDuda;
    public bool ListoParaAplicar =>
        !Aplicado && !SinCambios && Res.AplicableEnBloque && NombreNuevo != null;

    /// <summary>
    /// El «es solo la historia X» decidido a mano. Vive en la fila y no en las señales
    /// porque las señales son inmutables: la decisión crea una variante al renderizar.
    /// </summary>
    public string? SegElegido { get; private set; }

    /// <summary>
    /// Fija a mano el episodio de esta fila (el usuario eligió en el resolutor). Deja de ser
    /// una duda: hay una persona detrás. Con <paramref name="seg"/>, el fichero es SOLO esa
    /// historia del episodio: E413b, con el título de esa historia.
    /// </summary>
    public void ElegirEpisodio(CatalogEpisode ep, string? seg = null)
    {
        SegElegido = seg;
        Res.Episodio = ep;
        Res.Hint = ReindexHint.Override;
        Res.Score = 1.0;
        Res.Confianza = ReindexConfianza.Alta;
        // Elegir un episodio a mano MANDA sobre la detección de «dos episodios»: el usuario ha
        // decidido que es este, así que deja de recomendarse partir (si no, la fila seguiría en
        // «Partir en 2» ignorando la elección). Es lo que permite corregir un falso positivo.
        Res.TraeDosEpisodios = false;
        Res.Estado = ep.Especial ? ReindexEstado.Especial : ReindexEstado.Corregido;
        Res.Motivo = seg == null
            ? string.Format(Textos.Instancia.OrganizarMotivoElegido, ep.Num)
            : string.Format(Textos.Instancia.OrganizarMotivoElegidoHistoria, seg, ep.Num);
        Res.Alternativas = Array.Empty<ReindexCandidato>();
        Recalcular();
    }
}
