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
        // hayOrigen: estos ficheros son inventados y no estan en disco, pero la
        // premisa de esta prueba es que SI estan donde dice el analisis. Antes no
        // hacia falta decirlo porque el plan no lo miraba; ahora si lo mira.
        }, raiz, enCastellano: false, existe: _ => false, hayOrigen: _ => true);

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
            raiz, false, existe: _ => true, hayOrigen: _ => true);
        Program.Assert(choque[0].Motivo == PlanDeReordenado.Porque.Ocupado,
            "si el destino ya tiene ese nombre, no se pisa");

        // Y el choque DENTRO del propio plan: dos ficheros iguales en carpetas
        // distintas. Sin esto el segundo se comería al primero y el deshacer no
        // podría devolver lo que ya no está.
        var dos = PlanDeReordenado.Montar(new[]
        {
            Res(R(raiz, "a", "cap.mkv"), ReindexEstado.Limpio, 3),
            Res(R(raiz, "b", "cap.mkv"), ReindexEstado.Limpio, 3),
        }, raiz, false, existe: _ => false, hayOrigen: _ => true);
        Program.Assert(PlanDeReordenado.Cuantos(dos) == 1 &&
                       dos[1].Motivo == PlanDeReordenado.Porque.Ocupado,
            "dos ficheros al mismo destino: solo va el primero");
    }

    /// <summary>
    /// Un fichero que ya no está donde decía el análisis no se ofrece para mover.
    ///
    /// <para>
    /// Sale de un caso real. Se analiza, se aplica el renombrado —o se mueve desde
    /// otra pantalla— y después se abre «Ordenar por temporadas»: la lista es de
    /// ANTES, así que sus rutas ya no existen. El plan ofrecía moverlas igual y
    /// terminaba en «0 movidos · 6 no se pudieron», sin decir por qué. Seis fallos
    /// sin motivo es peor que seis filas que digan «vuelve a analizar».
    /// </para>
    /// </summary>
    public static void LoQueYaNoEsta()
    {
        Program.Seccion("Reordenar: lo que ya no está donde decía el análisis");

        var raiz = Path.Combine("C:", "tv", "Serie");
        var res = new ReindexResolution
        {
            Archivo = SignalExtractor.Extract(Path.Combine(raiz, "Serie S01E01.mkv"), "Serie"),
            Estado = ReindexEstado.Limpio,
            Episodio = new CatalogEpisode { Num = 1, Temporada = 2004 },
        };

        // Con el fichero en su sitio, se mueve: es el caso de siempre.
        var normal = PlanDeReordenado.Montar(
            new[] { res }, raiz, enCastellano: true,
            existe: _ => false, hayOrigen: _ => true);
        Program.Assert(normal[0].Motivo == PlanDeReordenado.Porque.Va,
            "con el fichero donde dice el análisis, se mueve");

        // Y si ya no está, NO se ofrece mover: se dice que ya no está.
        var fantasma = PlanDeReordenado.Montar(
            new[] { res }, raiz, enCastellano: true,
            existe: _ => false, hayOrigen: _ => false);
        Program.Assert(fantasma[0].Motivo == PlanDeReordenado.Porque.YaNoEsta,
            "si el fichero ya no está ahí, se dice — no se intenta y se falla después");
        Program.Assert(fantasma[0].Destino is null,
            "y sin destino: no hay a dónde llevar lo que no existe");
        Program.Assert(PlanDeReordenado.Cuantos(fantasma) == 0,
            "y no cuenta para el botón: «Mover 6» sobre seis fantasmas es una promesa falsa");
    }
}
