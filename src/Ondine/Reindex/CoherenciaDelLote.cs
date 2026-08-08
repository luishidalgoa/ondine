namespace Ondine.Reindex;

/// <summary>
/// Que el lote se sostenga a sí mismo, como SEGUNDA señal.
///
/// <para>
/// La confianza alta se da cuando dos señales independientes coinciden —lo
/// normal es el título y la fecha—. Un catálogo <b>sin fechas</b> deja solo el
/// título, así que nada llega a alta por bien que case y cada fichero acaba
/// pidiendo una decisión. Medido en una carpeta real de Crayon Shin-Chan:
/// catálogo de 1342 episodios con cero fechas, y 53 de 59 ficheros pidiendo
/// mano con aciertos de 0,82 a 0,95. Ninguno era dudoso; faltaba con qué
/// confirmarlos.
/// </para>
/// <para>
/// Y había otra señal sin usar. Si varios ficheros ordenados por su número
/// apuntan a episodios <b>distintos y en el mismo orden</b>, esa consistencia
/// vale lo que una fecha: que veinte títulos coincidan por casualidad
/// <i>y además</i> en orden no pasa. En aquella carpeta encajaban 26 de 29, y
/// las 3 que no eran justo las que merecían mirarse.
/// </para>
/// <para>
/// <b>Esto corrobora, no identifica.</b> Nunca propone un episodio ni salva un
/// parecido flojo: solo confirma lo que el título ya decía. Al revés sería una
/// máquina de cuadrar números, que es exactamente lo que no se quiere cuando
/// lo que hay en juego es renombrar la biblioteca de alguien.
/// </para>
/// </summary>
public static class CoherenciaDelLote
{
    /// <summary>
    /// Cuántos ficheros hacen falta en una banda para fiarse de ella. Por tres
    /// puntos pasa cualquier recta: ahí «coinciden» no significa nada, y
    /// corroborar sería fabricar confianza en vez de encontrarla.
    /// </summary>
    public const int MinimoParaFiarse = 5;

    /// <summary>
    /// Cuánto puede resbalar el desfase dentro de una misma banda, de un fichero
    /// al siguiente.
    ///
    /// <para>
    /// No es cero porque el desfase de verdad <b>se desliza</b>: sale de que la
    /// numeración de origen cuenta algo que el catálogo no —un recopilatorio, un
    /// especial— y cada vez que eso ocurre crece en uno. En la carpeta medida iba
    /// de −30 a −40 a lo largo de cincuenta ficheros.
    /// </para>
    /// </summary>
    public const int DerivaPorFichero = 3;

    /// <summary>
    /// Marca en cada fila si el resto del lote la sostiene.
    ///
    /// <para>
    /// Va por BANDAS de desfase y no buscando una sola serie global, porque una
    /// carpeta a medio arreglar tiene dos poblaciones intercaladas —lo ya
    /// renombrado, con desfase cero, y lo pendiente— y las dos son válidas.
    /// Buscar un único orden no encontraría ninguno: no es que no haya, es que
    /// hay dos.
    /// </para>
    /// </summary>
    public static void Marcar(IReadOnlyList<ReindexResolution> lote)
    {
        foreach (var r in lote) r.CorroboradoPorElLote = false;

        // Solo entran las que YA tienen un episodio decente por título. Esto
        // confirma; no rescata.
        var utiles = lote
            .Where(r => r.Episodio is not null
                     && r.Archivo.Indice is not null
                     && r.Archivo.SubSegmento is null
                     && r.Score >= TitleMatch.UmbralTitulo)
            .OrderBy(r => r.Archivo.Indice!.Value)
            .ToList();

        if (utiles.Count < MinimoParaFiarse) return;

        foreach (var banda in EnBandas(utiles))
        {
            if (banda.Count < MinimoParaFiarse) continue;
            foreach (var r in EnOrdenCreciente(banda)) r.CorroboradoPorElLote = true;
        }
    }

    /// <summary>
    /// Reparte las filas en bandas de desfase parecido, permitiendo que resbale.
    ///
    /// <para>
    /// Se ordena por desfase y se corta donde el salto es mayor que la deriva
    /// admitida: dos ficheros a −31 y −33 son la misma banda; uno a −32 y otro a
    /// +2 no lo son ni de lejos.
    /// </para>
    /// </summary>
    private static List<List<ReindexResolution>> EnBandas(List<ReindexResolution> utiles)
    {
        var porDesfase = utiles
            .OrderBy(r => r.Episodio!.Num - r.Archivo.Indice!.Value)
            .ToList();

        var bandas = new List<List<ReindexResolution>>();
        var actual = new List<ReindexResolution>();
        int? anterior = null;

        foreach (var r in porDesfase)
        {
            int d = r.Episodio!.Num - r.Archivo.Indice!.Value;
            if (anterior is { } a && d - a > DerivaPorFichero)
            {
                bandas.Add(actual);
                actual = new List<ReindexResolution>();
            }
            actual.Add(r);
            anterior = d;
        }
        if (actual.Count > 0) bandas.Add(actual);
        return bandas;
    }

    /// <summary>
    /// De una banda, las filas que forman la cadena creciente más larga —ordenadas
    /// por número de fichero y con episodios ESTRICTAMENTE crecientes—.
    ///
    /// <para>
    /// Es la subsecuencia creciente más larga de toda la vida. Se elige eso y no
    /// «comparar con el anterior» porque un solo intruso a mitad no puede
    /// invalidar a los que vienen detrás: descartándolo, la cadena sigue. Y
    /// estrictamente creciente, no «no decreciente», para que dos ficheros que
    /// reclaman el MISMO episodio no salgan los dos corroborados — eso convertiría
    /// un duplicado en dos certezas.
    /// </para>
    /// </summary>
    private static List<ReindexResolution> EnOrdenCreciente(List<ReindexResolution> banda)
    {
        var xs = banda.OrderBy(r => r.Archivo.Indice!.Value).ToList();
        int n = xs.Count;

        var colas = new List<int>();      // colas[k] = índice del final de la mejor cadena de largo k+1
        var previo = new int[n];
        for (int i = 0; i < n; i++) previo[i] = -1;

        for (int i = 0; i < n; i++)
        {
            int num = xs[i].Episodio!.Num;
            int lo = 0, hi = colas.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (xs[colas[mid]].Episodio!.Num < num) lo = mid + 1; else hi = mid;
            }
            if (lo > 0) previo[i] = colas[lo - 1];
            if (lo == colas.Count) colas.Add(i); else colas[lo] = i;
        }

        var cadena = new List<ReindexResolution>();
        for (int i = colas.Count > 0 ? colas[^1] : -1; i >= 0; i = previo[i]) cadena.Add(xs[i]);
        cadena.Reverse();

        // Una cadena que apenas cubre la banda no dice nada: si de veinte ficheros
        // solo seis van en orden, lo que hay no es una serie con tres intrusos.
        return cadena.Count >= MinimoParaFiarse && cadena.Count * 2 >= banda.Count
            ? cadena
            : new List<ReindexResolution>();
    }
}
