namespace Ondine.Reindex;

/// <summary>
/// Lo que una fila de Organizar aporta al resumen del lote. Solo lo que se cuenta:
/// ni ruta, ni nombre, ni nada de pintar.
///
/// <para>
/// Es una proyección a propósito. La fila de verdad vive en la interfaz y arrastra
/// tipos de WPF; el resumen no puede depender de eso o dejaría de ser una regla y
/// volvería a ser pantalla.
/// </para>
/// </summary>
/// <param name="EstadoVisible">
/// El estado que se ve, que no siempre es el que calculó el motor: lo ya aplicado cuenta
/// como limpio, porque está bien en el disco y no queda nada que hacerle.
/// </param>
/// <param name="Partible">Ya identificado, con más de una historia dentro y sin partir.</param>
public readonly record struct FilaDelLote(
    ReindexEstado EstadoVisible,
    bool ListoParaAplicar,
    bool Marcado,
    bool EsDuda,
    bool SinCambios,
    bool Partible);

/// <summary>Qué promete el botón de aplicar. El texto lo pone la pantalla.</summary>
public enum ModoDeAplicar
{
    /// <summary>Nada marcado: el botón no promete un número.</summary>
    Nada,
    /// <summary>Todo lo que está listo, marcado. Basta con decir cuántos.</summary>
    Todos,
    /// <summary>Parte de lo listo. Hay que decir «N de M» o cabe la sorpresa.</summary>
    Algunos,
}

/// <summary>
/// Todo lo que la pantalla de Organizar DECIDE a partir del lote: cuántos hay de cada
/// cosa, qué se puede pulsar y qué promete el botón de aplicar.
///
/// <para>
/// Vivía dentro de <c>ActualizarContadores</c>, entre asignaciones a etiquetas. Los
/// motivos estaban escritos en comentarios y no los comprobaba nada, que es la forma
/// habitual de perderlos. Aquí son reglas con pruebas.
/// </para>
/// <para>
/// <b>No redacta.</b> Devuelve números y modos; el texto —y su traducción— es cosa de la
/// pantalla. Por eso esta regla vale igual en WPF que en cualquier otra interfaz.
/// </para>
/// </summary>
public sealed record ResumenDelLote
{
    public required int Total { get; init; }

    public required int Limpios { get; init; }
    public required int Corregidos { get; init; }
    public required int Especiales { get; init; }
    public required int Conflictos { get; init; }
    public required int Errores { get; init; }

    /// <summary>Los que se pueden aplicar, estén marcados o no.</summary>
    public required int Listos { get; init; }

    /// <summary>Los que se van a aplicar de verdad: listos Y marcados.</summary>
    public required int Marcados { get; init; }

    public required int Dudas { get; init; }

    /// <summary>Los que ya estaban bien. Se cuentan aparte para que la suma cuadre a la vista.</summary>
    public required int YaBien { get; init; }

    public required int Partibles { get; init; }

    public required ModoDeAplicar ModoAplicar { get; init; }

    public bool PuedeAplicar => Marcados > 0;
    public bool PuedeAceptarVerdes => Listos > 0;
    public bool PuedeConfirmarEspeciales => Especiales > 0;
    public bool PuedePartir => Partibles > 0;

    public required bool PuedeCompararCatalogo { get; init; }
    public required bool PuedeReordenar { get; init; }

    /// <summary>
    /// Si la mayoría del lote está en duda, se dice de frente en vez de dejar que se
    /// descubra fila a fila. La mitad justa no cuenta: eso es un lote repartido, no un
    /// lote en duda.
    /// </summary>
    public required bool AvisarDeDudas { get; init; }

    public static ResumenDelLote De(IEnumerable<FilaDelLote> filas, bool hayCatalogo)
    {
        var lista = filas as IReadOnlyList<FilaDelLote> ?? filas.ToList();

        int Cuantos(ReindexEstado e) => lista.Count(f => f.EstadoVisible == e);

        var listos = lista.Count(f => f.ListoParaAplicar);
        var marcados = lista.Count(f => f.ListoParaAplicar && f.Marcado);
        var dudas = lista.Count(f => f.EsDuda);

        // Sin catálogo no se sabe de qué temporada es cada fichero, y sin análisis no hay
        // nada que comparar ni que ordenar. Las dos condiciones, no una.
        var hayAlgoAnalizado = lista.Count > 0;

        return new ResumenDelLote
        {
            Total = lista.Count,
            Limpios = Cuantos(ReindexEstado.Limpio),
            Corregidos = Cuantos(ReindexEstado.Corregido),
            Especiales = Cuantos(ReindexEstado.Especial),
            Conflictos = Cuantos(ReindexEstado.Conflicto),
            Errores = Cuantos(ReindexEstado.Error),

            Listos = listos,
            Marcados = marcados,
            Dudas = dudas,
            YaBien = lista.Count(f => f.SinCambios),
            Partibles = lista.Count(f => f.Partible),

            // El botón dice EXACTAMENTE cuántos va a tocar: aplicar nunca lleva sorpresa
            // dentro. Si hay listos sin marcar, tiene que notarse en el propio texto.
            ModoAplicar = marcados == 0 ? ModoDeAplicar.Nada
                        : marcados == listos ? ModoDeAplicar.Todos
                        : ModoDeAplicar.Algunos,

            PuedeCompararCatalogo = hayCatalogo && hayAlgoAnalizado,
            PuedeReordenar = hayCatalogo && hayAlgoAnalizado,

            AvisarDeDudas = lista.Count > 0 && dudas > lista.Count / 2,
        };
    }
}
