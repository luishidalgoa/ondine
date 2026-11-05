namespace Ondine.Trabajos;

/// <summary>A qué grupo de la tabla de Comprimir pertenece un vídeo.</summary>
public enum GrupoDeVideo
{
    /// <summary>Está en un trabajo de la cola, esperando o comprimiéndose.</summary>
    EnCola,
    /// <summary>Analizado, pero no está en ningún trabajo de la cola.</summary>
    Analizado,
}

/// <summary>Una fila ya colocada: su grupo, si abre grupo, y su puesto en la cola si lo tiene.</summary>
public readonly record struct EntradaDeVista(VideoRow Fila, GrupoDeVideo Grupo, bool PrimeraDeGrupo, int Cuantos, int PuestoEnCola);

/// <summary>
/// La tabla de Comprimir como vista de la cola: qué vídeos están en la cola, en qué orden, y
/// qué queda fuera.
///
/// <para>
/// <b>Es la única fuente de la pertenencia.</b> Antes, quitar una fila de la tabla no tocaba
/// el trabajo ya encolado, y la tabla y la cola decían cosas distintas. Ahora la fila no guarda
/// «estoy en la cola»: se pregunta a la cola. Las dos interfaces solo dibujan lo que esto dice.
/// </para>
/// <para>
/// Pura: no toca la interfaz ni el disco. Por eso se prueba entera desde Linux.
/// </para>
/// </summary>
public static class VistaDeCola
{
    /// <summary>
    /// Coloca las filas: primero los vídeos de la cola, en el orden de la cola (trabajo a
    /// trabajo, fichero a fichero); después los analizados, en el orden en que llegaron.
    /// </summary>
    /// <param name="filas">La lista de vídeos, en el orden que tenga ahora.</param>
    /// <param name="trabajos">La cola, en su orden.</param>
    public static IReadOnlyList<EntradaDeVista> Colocar(IReadOnlyList<VideoRow> filas, IReadOnlyList<Trabajo> trabajos)
    {
        // Posición en la cola de cada ruta: la primera vez que aparece cuenta. Un fichero en dos
        // trabajos aparece en el primero, que es el que se comprimirá antes.
        var puesto = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int n = 0;
        foreach (var t in trabajos)
        {
            if (t.Despachado) continue;
            foreach (var ruta in t.Ficheros)
            {
                n++;
                puesto.TryAdd(ruta, n);
            }
        }

        var enCola = new List<(VideoRow Fila, int Puesto)>();
        var analizados = new List<VideoRow>();
        foreach (var f in filas)
        {
            if (puesto.TryGetValue(f.Path, out var p)) enCola.Add((f, p));
            else analizados.Add(f);
        }
        enCola.Sort((a, b) => a.Puesto.CompareTo(b.Puesto));

        var salida = new List<EntradaDeVista>(filas.Count);
        for (int i = 0; i < enCola.Count; i++)
            salida.Add(new EntradaDeVista(enCola[i].Fila, GrupoDeVideo.EnCola, i == 0, enCola.Count, enCola[i].Puesto));
        for (int i = 0; i < analizados.Count; i++)
            salida.Add(new EntradaDeVista(analizados[i], GrupoDeVideo.Analizado, i == 0, analizados.Count, 0));
        return salida;
    }
}
