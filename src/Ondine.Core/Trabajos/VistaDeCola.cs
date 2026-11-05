namespace Ondine.Trabajos;

/// <summary>A qué grupo de la tabla de Comprimir pertenece un vídeo.</summary>
public enum GrupoDeVideo
{
    /// <summary>Analizado, pero no se está comprimiendo ni está en la cola: no hace nada aún. Va primero en el
    /// enumerado para que el valor por defecto de una fila nueva no parezca de cola.</summary>
    Analizado,
    /// <summary>Se está comprimiendo, o está esperando en la cola.</summary>
    EnCola,
}

/// <summary>
/// Una fila ya colocada.
/// <para>
/// <c>Procesando</c>: se está comprimiendo ahora mismo. <c>TrabajoId</c>: el trabajo de la cola al que
/// pertenece (0 si no está en la cola). <c>PuestoEnCola</c>: su número en la cola (0 si no lo tiene).
/// </para>
/// </summary>
public readonly record struct EntradaDeVista(
    VideoRow Fila, GrupoDeVideo Grupo, bool PrimeraDeGrupo, int Cuantos,
    int PuestoEnCola, bool Procesando, int TrabajoId);

/// <summary>
/// La tabla de Comprimir como vista de la cola: qué se está comprimiendo, qué espera, en qué orden, y qué
/// queda fuera.
///
/// <para>
/// <b>Es la única fuente de la pertenencia.</b> La fila no guarda «estoy en la cola»: se pregunta a la cola
/// (y a la compresión en curso). Las dos interfaces solo dibujan lo que esto dice.
/// </para>
/// <para>
/// Pura: no toca la interfaz ni el disco. Por eso se prueba entera desde Linux.
/// </para>
/// </summary>
public static class VistaDeCola
{
    /// <summary>
    /// Coloca las filas. Primero lo que se está comprimiendo, después lo que espera en la cola (en el orden de
    /// la cola), y al final lo analizado, en el orden en que llegó.
    /// </summary>
    /// <param name="filas">La lista de vídeos, en el orden que tenga ahora.</param>
    /// <param name="trabajos">La cola, en su orden.</param>
    /// <param name="enProceso">Rutas que se están comprimiendo ahora, lancen o no desde la cola.</param>
    public static IReadOnlyList<EntradaDeVista> Colocar(
        IReadOnlyList<VideoRow> filas, IReadOnlyList<Trabajo> trabajos, IReadOnlySet<string>? enProceso = null)
    {
        enProceso ??= new HashSet<string>();

        // Posición en la cola de cada ruta: la primera vez que aparece cuenta. Un fichero en dos trabajos
        // aparece en el primero, que es el que se comprimirá antes.
        var puesto = new Dictionary<string, (int Puesto, int TrabajoId, bool Curso)>(StringComparer.OrdinalIgnoreCase);
        int n = 0;
        foreach (var t in trabajos)
        {
            if (t.Despachado) continue;
            foreach (var ruta in t.Ficheros)
            {
                n++;
                puesto.TryAdd(ruta, (n, t.Id, t.Estado == EstadoDelTrabajo.EnCurso));
            }
        }

        var enCola = new List<(VideoRow Fila, int Orden, int Puesto, int TrabajoId, bool Procesando)>();
        var analizados = new List<VideoRow>();
        foreach (var f in filas)
        {
            bool procesando = enProceso.Contains(f.Path)
                              || (puesto.TryGetValue(f.Path, out var p) && p.Curso);
            if (procesando)
            {
                enCola.Add((f, 0, 0, puesto.TryGetValue(f.Path, out var pp) ? pp.TrabajoId : 0, true));
            }
            else if (puesto.TryGetValue(f.Path, out var q))
            {
                enCola.Add((f, q.Puesto, q.Puesto, q.TrabajoId, false));
            }
            else analizados.Add(f);
        }

        // Lo que se comprime va primero; lo demás, en el orden de la cola. Orden estable para empates.
        var ordenados = enCola.OrderBy(x => x.Orden).ToList();

        var salida = new List<EntradaDeVista>(filas.Count);
        for (int i = 0; i < ordenados.Count; i++)
        {
            var e = ordenados[i];
            salida.Add(new EntradaDeVista(e.Fila, GrupoDeVideo.EnCola, i == 0, ordenados.Count,
                                          e.Puesto, e.Procesando, e.TrabajoId));
        }
        for (int i = 0; i < analizados.Count; i++)
            salida.Add(new EntradaDeVista(analizados[i], GrupoDeVideo.Analizado, i == 0, analizados.Count, 0, false, 0));
        return salida;
    }
}
