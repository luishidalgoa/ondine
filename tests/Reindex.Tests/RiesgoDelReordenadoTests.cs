using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Lo que hay que avisar ANTES de mover: un reordenado puede ser gratis o puede
/// costar una tarde de sincronización, y por fuera se ven iguales.
/// </summary>
public static class RiesgoDelReordenadoTests
{
    private static string R(params string[] p) => Path.Combine(p);

    private static PlanDeReordenado.Paso Va(string origen, string destino)
        => new(origen, destino, PlanDeReordenado.Porque.Va);

    public static void Todas()
    {
        Program.Seccion("Lo que cuesta mover, antes de moverlo");

        var raiz = R("C:", "Plex", "Doraemon (1979)");
        var sinNubes = Array.Empty<Nube.Sincronizacion>();

        // ── Lo normal: no hay nada que avisar ──────────────────────────────────
        var tranquilo = RiesgoDelReordenado.Mirar(
            new[] { Va(R(raiz, "a.mkv"), R(raiz, "Season 03", "a.mkv")) },
            sinNubes);
        Program.Assert(tranquilo.Count == 0,
            "una mudanza dentro del mismo disco y sin nubes no avisa de nada");

        // ── Cruzar volumen ────────────────────────────────────────────────────
        // Solo en Windows: «volumen» aquí es la letra de unidad, y fuera de
        // Windows «C:lgo» no tiene raíz que comparar —hay una sola, «/»—. Se
        // salta en voz alta en vez de inventar un caso neutro: un salto
        // silencioso se lee como cobertura.
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("  · saltado: cruzar volumen son letras de unidad y esto no es Windows");
        }
        else
        {
        // No es un detalle: dentro del mismo volumen mover es reetiquetar, y entre
        // volúmenes es copiar entero y borrar. Con 200 capítulos, la diferencia
        // entre un segundo y media hora.
        var cruza = RiesgoDelReordenado.Mirar(
            new[]
            {
                Va(R("C:", "descargas", "a.mkv"), R("E:", "Plex", "Season 03", "a.mkv")),
                Va(R("C:", "descargas", "b.mkv"), R("E:", "Plex", "Season 03", "b.mkv")),
                Va(R("C:", "descargas", "c.mkv"), R("C:", "Plex", "Season 03", "c.mkv")),
            },
            sinNubes);
        Program.Assert(cruza.Count == 1 && cruza[0].Que == RiesgoDelReordenado.Riesgo.CruzaVolumen,
            "mover a otro volumen se avisa");
        Program.Assert(cruza[0].Cuantos == 2,
            "y se dice cuántos: los dos que cruzan, no el que se queda en su disco");
        }

        // ── Lo que no se mueve, no cuenta ─────────────────────────────────────
        // Un aviso que suma los que se quedan quietos asusta por trabajo que
        // nadie va a hacer, y quien lo vea dos veces deja de leerlo.
        var soloLosQueVan = RiesgoDelReordenado.Mirar(
            new[]
            {
                new PlanDeReordenado.Paso(R("C:", "x.mkv"), R("E:", "x.mkv"), PlanDeReordenado.Porque.YaEsta),
                new PlanDeReordenado.Paso(R("C:", "y.mkv"), null, PlanDeReordenado.Porque.SinCurar),
                new PlanDeReordenado.Paso(R("C:", "z.mkv"), R("E:", "z.mkv"), PlanDeReordenado.Porque.Ocupado),
            },
            sinNubes);
        Program.Assert(soloLosQueVan.Count == 0,
            "los que no se mueven no generan aviso, por mucho que su destino cruzara");

        // ── Entrar en una nube ────────────────────────────────────────────────
        var nubes = new[] { new Nube.Sincronizacion("OneDrive", R("C:", "Users", "luis", "OneDrive")) };

        var entra = RiesgoDelReordenado.Mirar(
            new[]
            {
                Va(R("D:", "descargas", "a.mkv"),
                   R("C:", "Users", "luis", "OneDrive", "Plex", "Season 03", "a.mkv")),
            },
            nubes);
        Program.Assert(entra.Any(a => a.Que == RiesgoDelReordenado.Riesgo.Nube && a.Detalle == "OneDrive"),
            "meter ficheros en una carpeta sincronizada se avisa, y se dice de qué nube es");

        // ── Moverse DENTRO de la misma nube no es lo mismo ─────────────────────
        // Aquí el cliente de sincronización mueve la referencia, no vuelve a subir
        // nada. Avisar también de esto sería el aviso que se ignora siempre.
        var dentro = RiesgoDelReordenado.Mirar(
            new[]
            {
                Va(R("C:", "Users", "luis", "OneDrive", "Plex", "a.mkv"),
                   R("C:", "Users", "luis", "OneDrive", "Plex", "Season 03", "a.mkv")),
            },
            nubes);
        Program.Assert(dentro.All(a => a.Que != RiesgoDelReordenado.Riesgo.Nube),
            "moverse dentro de la MISMA nube no es subir nada, así que no se avisa");

        // ── Marcadores: los que solo están en el disco de nombre ──────────────
        // Medido en este mismo repositorio: abrir un marcador de 277 MB se lo bajó
        // entero en 18 segundos. Mover cien sin decirlo llena el disco.
        var marcadores = RiesgoDelReordenado.Mirar(
            new[]
            {
                Va(R(raiz, "a.mkv"), R(raiz, "Season 03", "a.mkv")),
                Va(R(raiz, "b.mkv"), R(raiz, "Season 03", "b.mkv")),
            },
            sinNubes,
            esMarcador: ruta => ruta.EndsWith("a.mkv", StringComparison.Ordinal));
        Program.Assert(marcadores.Any(a => a.Que == RiesgoDelReordenado.Riesgo.Marcador && a.Cuantos == 1),
            "se avisa de los que solo están en el disco de nombre, y de cuántos son");

        // ── Un plan vacío no inventa avisos ───────────────────────────────────
        Program.Assert(
            RiesgoDelReordenado.Mirar(Array.Empty<PlanDeReordenado.Paso>(), sinNubes).Count == 0,
            "sin nada que mover no hay nada que avisar");
    }
}
