using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// El reordenado por temporadas, apoyado en <see cref="Mudanza"/>.
///
/// <para>
/// Aquí solo queda lo que es PROPIO de las temporadas: qué pasos del plan se
/// mueven de verdad. Mover, no pisar, llevarse los compañeros y deshacer es lo
/// mismo para las series y para las películas, así que vive una sola vez.
/// </para>
/// </summary>
public static class MudanzaDeTemporada
{
    public static Mudanza.Parte Aplicar(
        IEnumerable<PlanDeReordenado.Paso> plan,
        Action<string>? avanza = null,
        CancellationToken corte = default)
        => Mudanza.Aplicar(
            plan.Where(p => p.Motivo == PlanDeReordenado.Porque.Va && p.Destino is not null)
                .Select(p => (p.Origen, p.Destino!)),
            avanza, corte);

    public static int Deshacer(Mudanza.Parte parte) => Mudanza.Deshacer(parte);
}
