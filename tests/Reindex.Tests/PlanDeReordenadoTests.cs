using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La simulación del reordenado: qué se movería, y sobre todo qué no.
/// </summary>
public static class PlanDeReordenadoTests
{
    private static string R(params string[] p) => Path.Combine(p);

    private static ReindexResolution Res(string ruta, ReindexEstado estado, int? temporada)
        => new()
        {
            Archivo = SignalExtractor.Extract(ruta, Path.GetFileName(Path.GetDirectoryName(ruta)) ?? ""),
            Estado = estado,
            Episodio = temporada is null ? null : new CatalogEpisode { Num = 1, Temporada = temporada },
        };

    public static void Todas()
    {
        Program.Seccion("La simulación del reordenado");

        var raiz = R("C:", "Plex", "Doraemon (1979)");

        var plan = PlanDeReordenado.Montar(new[]
        {
            Res(R(raiz, "suelto.mkv"), ReindexEstado.Limpio, 3),
            Res(R(raiz, "Season 03", "ya.mkv"), ReindexEstado.Limpio, 3),
            Res(R(raiz, "dudoso.mkv"), ReindexEstado.Conflicto, 3),
            Res(R(raiz, "especial.mkv"), ReindexEstado.Especial, 0),
            Res(R(raiz, "sinTemp.mkv"), ReindexEstado.Corregido, null),
        }, raiz, enCastellano: false, existe: _ => false);

        Program.Assert(plan.Count == 5, "sale un paso por fichero, también por los que no se mueven");
        Program.Assert(PlanDeReordenado.Cuantos(plan) == 1, "solo uno se mueve de verdad");

        Program.Assert(plan[0].Motivo == PlanDeReordenado.Porque.Va &&
                       plan[0].Destino == R(raiz, "Season 03", "suelto.mkv"),
            "el suelto y ya curado va a su temporada");

        Program.Assert(plan[1].Motivo == PlanDeReordenado.Porque.YaEsta,
            "el que ya está en su sitio no se toca");

        // LO QUE PIDIÓ EL USUARIO: mover solo lo curado. Un fichero en conflicto
        // no se sabe de qué temporada es -por eso está en conflicto- y moverlo a
        // una carpeta decidida a medias deja una pista falsa para la próxima pasada.
        Program.Assert(plan[2].Motivo == PlanDeReordenado.Porque.SinCurar,
            "un conflicto no se mueve: aún no se sabe qué es");
        Program.Assert(plan[3].Motivo == PlanDeReordenado.Porque.SinCurar,
            "un especial sin confirmar tampoco, aunque tenga temporada");
        Program.Assert(plan[4].Motivo == PlanDeReordenado.Porque.SinTemporada,
            "y sin temporada en el catálogo no hay a dónde llevarlo");

        // Nada de sobrescribir.
        var choque = PlanDeReordenado.Montar(
            new[] { Res(R(raiz, "x.mkv"), ReindexEstado.Limpio, 3) },
            raiz, false, existe: _ => true);
        Program.Assert(choque[0].Motivo == PlanDeReordenado.Porque.Ocupado,
            "si el destino ya tiene ese nombre, no se pisa");

        // Y el choque DENTRO del propio plan: dos ficheros iguales en carpetas
        // distintas. Sin esto el segundo se comería al primero y el deshacer no
        // podría devolver lo que ya no está.
        var dos = PlanDeReordenado.Montar(new[]
        {
            Res(R(raiz, "a", "cap.mkv"), ReindexEstado.Limpio, 3),
            Res(R(raiz, "b", "cap.mkv"), ReindexEstado.Limpio, 3),
        }, raiz, false, existe: _ => false);
        Program.Assert(PlanDeReordenado.Cuantos(dos) == 1 &&
                       dos[1].Motivo == PlanDeReordenado.Porque.Ocupado,
            "dos ficheros al mismo destino: solo va el primero");
    }
}
