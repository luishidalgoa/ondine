using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Qué ficheros habría que mover a su carpeta de temporada, y cuáles NO se tocan.
///
/// <para>
/// Es una <b>simulación</b>: no escribe nada. Se calcula entero, se enseña, y solo
/// después se aplica —el mismo trato que ya tiene el renombrado, que propone y
/// espera—. Un reordenado que empieza a mover mientras decides es un reordenado
/// que no se puede cancelar a tiempo.
/// </para>
/// </summary>
public static class PlanDeReordenado
{
    public enum Porque
    {
        /// <summary>Se mueve.</summary>
        Va,
        /// <summary>Ya está en su carpeta.</summary>
        YaEsta,
        /// <summary>Sin resolver: conflicto, especial sin confirmar o error.</summary>
        SinCurar,
        /// <summary>El catálogo no dice de qué temporada es.</summary>
        SinTemporada,
        /// <summary>En el destino ya hay otro fichero con ese nombre.</summary>
        Ocupado,

        /// <summary>
        /// El fichero ya no está donde decía el análisis: lo han renombrado o movido
        /// por el camino, casi siempre desde la propia app.
        /// </summary>
        YaNoEsta,
    }

    public sealed record Paso(string Origen, string? Destino, Porque Motivo);

    /// <summary>
    /// Monta el plan. <paramref name="existe"/> —¿está ocupado el destino?— y
    /// <paramref name="hayOrigen"/> —¿sigue el fichero donde decía el análisis?— se
    /// inyectan para poder probar la decisión sin disco; en la aplicación los dos son
    /// <c>File.Exists</c>. Van separados porque preguntan cosas opuestas y las pruebas
    /// necesitan fijar cada una por su lado.
    /// </summary>
    public static List<Paso> Montar(
        IEnumerable<ReindexResolution> resoluciones,
        string raiz,
        bool enCastellano,
        Func<string, bool>? existe = null,
        Func<string, bool>? hayOrigen = null)
    {
        existe ??= File.Exists;
        hayOrigen ??= File.Exists;
        var plan = new List<Paso>();
        var pedidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in resoluciones)
        {
            var origen = r.Archivo.Path;

            // Lo primero: ¿sigue estando ahí? La lista viene de un análisis, y entre
            // aquel momento y este puede haberse aplicado un renombrado o un movimiento
            // —desde la propia app, casi siempre—. Ofrecer mover un fichero que ya no
            // está termina en «0 movidos · 6 no se pudieron», sin decir por qué; se vio
            // tal cual. Seis fallos sin motivo son peores que seis filas que digan
            // «vuelve a analizar».
            if (!hayOrigen(origen))
            {
                plan.Add(new(origen, null, Porque.YaNoEsta));
                continue;
            }

            // SOLO lo ya curado. Un fichero en conflicto no se sabe de qué
            // temporada es -por eso está en conflicto-, y moverlo a una carpeta
            // decidida a medias es peor que dejarlo donde está: la próxima
            // pasada lo encontrará con una pista falsa encima.
            if (r.Estado is not (ReindexEstado.Limpio or ReindexEstado.Corregido))
            {
                plan.Add(new(origen, null, Porque.SinCurar));
                continue;
            }

            if (r.Episodio?.Temporada is not { } temporada)
            {
                plan.Add(new(origen, null, Porque.SinTemporada));
                continue;
            }

            if (DestinoDeTemporada.HayQueMover(origen, raiz, temporada, enCastellano) is not { } carpeta)
            {
                plan.Add(new(origen, null, Porque.YaEsta));
                continue;
            }

            // Nada de sobrescribir, y tampoco dos ficheros al mismo destino dentro
            // del propio plan: el segundo se comería al primero, y el deshacer no
            // podría devolver lo que ya no está.
            var destino = Path.Combine(carpeta, Path.GetFileName(origen));
            if (existe(destino) || !pedidos.Add(destino))
            {
                plan.Add(new(origen, destino, Porque.Ocupado));
                continue;
            }

            plan.Add(new(origen, destino, Porque.Va));
        }

        return plan;
    }

    /// <summary>Cuántos se moverían de verdad.</summary>
    public static int Cuantos(IEnumerable<Paso> plan) => plan.Count(p => p.Motivo == Porque.Va);
}
