namespace Ondine.Reindex;

/// <summary>
/// Dónde van las bandas que separan temporadas en la tabla de Organizar, y qué dice cada una.
///
/// <para>
/// Con doscientos ficheros de cinco temporadas, la tabla sin separar es un muro. Las bandas
/// dicen dónde empieza cada temporada y cuántos ficheros trae — es lo que permite mirar «la
/// 3» sin contar filas.
/// </para>
/// <para>
/// <b>Está en el motor porque el mecanismo de arriba cambia y la decisión no.</b> Esto vivía
/// pegado a <c>CollectionViewSource</c>, que en Avalonia no existe: al portar la pantalla se
/// reescribe cómo se filtra y se agrupa, y si la cuenta se reescribiera al mismo tiempo, un
/// fallo no se sabría de cuál de las dos cosas es.
/// </para>
/// </summary>
public static class BandasDeGrupo
{
    /// <summary>Una banda: en qué fila abre, de qué grupo es y cuántas filas cubre.</summary>
    public readonly record struct Banda(int Indice, string Grupo, int Cuantos);

    /// <summary>
    /// Las bandas de una lista de grupos <b>en el orden en que se ven</b>.
    ///
    /// <param name="grupos">
    /// El grupo de cada fila visible, ya filtrada y en el orden de la tabla. Se pasan los
    /// grupos y no las filas para que esto no sepa nada de la pantalla.
    /// </param>
    /// <param name="ordenManual">
    /// Si hay un orden por cabecera activo. Entonces <b>no se pone ninguna</b>: ordenando por
    /// nombre las temporadas se entremezclan y «aquí empieza la 2» deja de ser verdad. Una
    /// banda que miente es peor que ninguna.
    /// </param>
    /// </summary>
    public static IReadOnlyList<Banda> Calcular(IReadOnlyList<string> grupos, bool ordenManual)
    {
        if (ordenManual) return [];

        // Con un solo grupo la banda no separa nada: es una línea de ruido y un renglón
        // menos de tabla.
        if (grupos.Count <= 1 || grupos.Distinct().Count() <= 1) return [];

        var bandas = new List<Banda>();
        for (int i = 0; i < grupos.Count;)
        {
            // Hasta donde llegue ESTE tramo. Se cuenta el tramo y no el grupo: un grupo que
            // reaparece más abajo abre su propia banda, porque lo que se está diciendo es
            // «desde aquí y hasta ahí», no «cuántos hay en total».
            int j = i;
            while (j < grupos.Count && grupos[j] == grupos[i]) j++;

            bandas.Add(new Banda(i, grupos[i], j - i));
            i = j;
        }
        return bandas;
    }
}
