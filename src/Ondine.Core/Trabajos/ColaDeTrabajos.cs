namespace Ondine.Trabajos;

/// <summary>En qué punto está un trabajo de la cola.</summary>
public enum EstadoDelTrabajo
{
    Pendiente,
    EnCurso,
    /// <summary>Salieron todos sus ficheros.</summary>
    Hecho,
    /// <summary>
    /// Salieron unos y otros no. Es un estado propio a propósito: decir «hecho» con la mitad
    /// fuera es mentir, y decir «fallido» con la mitad dentro manda a repetir lo que ya está.
    /// </summary>
    AMedias,
    /// <summary>No salió ninguno.</summary>
    Fallido,
    Cancelado,
}

/// <summary>
/// Un trabajo: unos ficheros, unas opciones y un destino.
///
/// <para>
/// Las opciones son <b>suyas</b>, no las de la pantalla. Ver <see cref="ColaDeTrabajos"/>.
/// </para>
/// </summary>
public sealed class Trabajo
{
    public required int Id { get; init; }
    public required IReadOnlyList<string> Ficheros { get; init; }
    public required EncodeOptions Opciones { get; init; }
    public required string Destino { get; init; }

    public EstadoDelTrabajo Estado { get; internal set; } = EstadoDelTrabajo.Pendiente;

    /// <summary>Cuántos salieron y cuántos no. Solo tiene sentido una vez despachado.</summary>
    public int Salieron { get; internal set; }
    public int Fallaron { get; internal set; }

    /// <summary>Si ya no queda nada que hacerle.</summary>
    public bool Despachado => Estado is EstadoDelTrabajo.Hecho or EstadoDelTrabajo.AMedias
                                      or EstadoDelTrabajo.Fallido or EstadoDelTrabajo.Cancelado;
}

/// <summary>
/// Trabajos en espera, cada uno con sus propios ajustes.
///
/// <para>
/// Hoy todos los ficheros de una tanda comparten las mismas opciones, así que para comprimir
/// dos cosas con ajustes distintos hay que esperar a que acabe la primera. Con la cola, cada
/// trabajo se lleva los suyos puestos y se despachan en orden.
/// </para>
/// <para>
/// <b>La regla de la que depende todo lo demás: las opciones se COPIAN.</b> Si un trabajo
/// guardara una referencia a las de la pantalla, cambiar los ajustes para el trabajo
/// siguiente cambiaría también el que ya está esperando — que es exactamente la sorpresa que
/// esta función viene a evitar. Sin esa copia, la cola es peor que no tenerla: promete algo
/// que no cumple.
/// </para>
/// <para>
/// Esto solo <b>ordena y recuerda</b>. No lanza ffmpeg ni sabe de progreso: quien despacha es
/// la pantalla, preguntando por <see cref="Siguiente"/>. Así la cola se prueba entera sin
/// tocar un fichero.
/// </para>
/// </summary>
public sealed class ColaDeTrabajos
{
    private readonly List<Trabajo> _trabajos = [];
    private int _siguienteId = 1;

    public IReadOnlyList<Trabajo> Trabajos => _trabajos;

    /// <summary>Trabajos que ni siquiera han empezado.</summary>
    public int Pendientes => _trabajos.Count(t => t.Estado == EstadoDelTrabajo.Pendiente);

    /// <summary>
    /// Ficheros que quedan, contando los del trabajo en curso: es lo que hay que enseñar
    /// como «queda esto», y un trabajo a medias sigue teniendo ficheros por delante.
    /// </summary>
    public int FicherosPorHacer =>
        _trabajos.Where(t => !t.Despachado).Sum(t => t.Ficheros.Count);

    /// <summary>
    /// Mete un trabajo al final con una copia de las opciones. Devuelve <c>null</c> si no
    /// hay ficheros: una fila que no hace nada solo estorba.
    /// </summary>
    public Trabajo? Encolar(IReadOnlyList<string> ficheros, EncodeOptions opciones, string destino)
    {
        if (ficheros.Count == 0) return null;

        var t = new Trabajo
        {
            Id = _siguienteId++,
            Ficheros = [.. ficheros],
            Opciones = Copiar(opciones),
            Destino = destino,
        };
        _trabajos.Add(t);
        return t;
    }

    /// <summary>El que toca: el que está en curso, o el primero que espera.</summary>
    public Trabajo? Siguiente() =>
        _trabajos.FirstOrDefault(t => t.Estado == EstadoDelTrabajo.EnCurso)
        ?? _trabajos.FirstOrDefault(t => t.Estado == EstadoDelTrabajo.Pendiente);

    public void Empezar(int id)
    {
        if (Buscar(id) is { Estado: EstadoDelTrabajo.Pendiente } t)
            t.Estado = EstadoDelTrabajo.EnCurso;
    }

    /// <summary>
    /// Cierra un trabajo con el recuento real. El estado sale de los números y no de quien
    /// llama: así no hay dos formas de decir lo mismo.
    /// </summary>
    public void Terminar(int id, int salieron, int fallaron)
    {
        if (Buscar(id) is not { } t) return;

        t.Salieron = salieron;
        t.Fallaron = fallaron;
        t.Estado = (salieron, fallaron) switch
        {
            (> 0, 0) => EstadoDelTrabajo.Hecho,
            (> 0, > 0) => EstadoDelTrabajo.AMedias,
            _ => EstadoDelTrabajo.Fallido,
        };
    }

    public void Cancelar(int id)
    {
        if (Buscar(id) is { Despachado: false } t) t.Estado = EstadoDelTrabajo.Cancelado;
    }

    /// <summary>
    /// Saca un trabajo de la cola. El que está en curso NO se saca: para eso se cancela, que
    /// es otra cosa —ya hay bytes escritos— y tiene que verse distinto.
    /// </summary>
    public bool Quitar(int id)
    {
        if (Buscar(id) is not { Estado: EstadoDelTrabajo.Pendiente } t) return false;
        return _trabajos.Remove(t);
    }

    public bool Subir(int id) => Mover(id, -1);
    public bool Bajar(int id) => Mover(id, +1);

    /// <summary>
    /// Reordenar solo vale entre pendientes. Ni se mueve el que corre —ya se está
    /// escribiendo en el disco— ni se cuela nada por encima de él.
    /// </summary>
    private bool Mover(int id, int paso)
    {
        var i = _trabajos.FindIndex(t => t.Id == id);
        if (i < 0 || _trabajos[i].Estado != EstadoDelTrabajo.Pendiente) return false;

        var j = i + paso;
        if (j < 0 || j >= _trabajos.Count) return false;
        if (_trabajos[j].Estado != EstadoDelTrabajo.Pendiente) return false;

        (_trabajos[i], _trabajos[j]) = (_trabajos[j], _trabajos[i]);
        return true;
    }

    /// <summary>
    /// Cuáles de esos ficheros ya están esperando en la cola.
    ///
    /// <para>
    /// No se prohíbe encolar el mismo dos veces —puede ser a propósito: dos formatos del
    /// mismo original— pero hay que poder avisar, porque el segundo trabajo leería un
    /// fichero que el primero puede haber mandado a la papelera. Descubrirlo a mitad de cola
    /// es tardísimo.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> YaEnCola(IReadOnlyList<string> ficheros)
    {
        var esperando = _trabajos
            .Where(t => !t.Despachado)
            .SelectMany(t => t.Ficheros)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. ficheros.Where(esperando.Contains)];
    }

    private Trabajo? Buscar(int id) => _trabajos.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// La copia. Llega hasta las listas de dentro: copiando solo la superficie, las de
    /// idiomas y subtítulos seguirían siendo las MISMAS que las de la pantalla, y añadir un
    /// idioma después se colaría en un trabajo ya encolado. Es media copia y no se nota
    /// hasta que muerde.
    /// </summary>
    private static EncodeOptions Copiar(EncodeOptions o) => new()
    {
        Output = o.Output,
        Container = o.Container,
        VideoCodec = o.VideoCodec,
        Codificador = o.Codificador,
        AudioOnly = o.AudioOnly,
        AudioFormat = o.AudioFormat,
        Lang = o.Lang,
        KeepLangs = [.. o.KeepLangs],
        SubLangs = o.SubLangs is null ? null : [.. o.SubLangs],
        NoSubs = o.NoSubs,
        Quality = o.Quality,
        BitrateVideoKbps = o.BitrateVideoKbps,
        TamanoObjetivoBytes = o.TamanoObjetivoBytes,
        Velocidad = o.Velocidad,
        AudioCodec = o.AudioCodec,
        AudioMezcla = o.AudioMezcla,
        MaxHeight = o.MaxHeight,
        AudioBitrate = o.AudioBitrate,
        Force = o.Force,
        DryRun = o.DryRun,
        NameRule = o.NameRule,
        Desde = o.Desde,
        Duracion = o.Duracion,
        NombreSalida = o.NombreSalida,
    };
}
