using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Aplicar el reordenado: mover de verdad, y poder volver atrás.
///
/// <para>
/// Con ficheros de verdad en una carpeta temporal. Un mover que se prueba con
/// dobles demuestra que se llamó a la función, no que el fichero acabase donde
/// tenía que acabar —y lo segundo es lo único que le importa a quien lo usa—.
/// </para>
/// </summary>
public static class MudanzaDeTemporadaTests
{
    private static string Nueva()
    {
        var t = Path.Combine(Path.GetTempPath(), "ondine-mudanza-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(t);
        return t;
    }

    private static void Crear(string ruta, string contenido = "x")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllText(ruta, contenido);
    }

    public static void Todas()
    {
        Program.Seccion("Aplicar el reordenado (mover y deshacer)");

        MueveYDeshace();
        SoloLoQueVa();
        NoPisaLoQueApareceEnMedio();
        ElDeshacerTampocoPisa();
    }

    // ── Lo normal: se mueve, se lleva a sus compañeros, y vuelve entero ──
    private static void MueveYDeshace()
    {
        var raiz = Nueva();
        try
        {
            var origen = Path.Combine(raiz, "cap.mkv");
            var subs = Path.Combine(raiz, "cap.es.srt");
            var ficha = Path.Combine(raiz, "cap.nfo");
            var ajeno = Path.Combine(raiz, "otro.mkv");
            Crear(origen); Crear(subs); Crear(ficha); Crear(ajeno);

            var destino = Path.Combine(raiz, "Season 03", "cap.mkv");
            var parte = MudanzaDeTemporada.Aplicar(new[]
            {
                new PlanDeReordenado.Paso(origen, destino, PlanDeReordenado.Porque.Va),
            });

            Program.Assert(parte.Fallidos.Count == 0, "no falla nada");
            Program.Assert(File.Exists(destino) && !File.Exists(origen),
                "el vídeo acaba en su carpeta de temporada");
            Program.Assert(Directory.Exists(Path.Combine(raiz, "Season 03")),
                "la carpeta se crea si no existía");

            // Un vídeo sin su .srt es un vídeo sin subtítulos: los compañeros viajan.
            Program.Assert(File.Exists(Path.Combine(raiz, "Season 03", "cap.es.srt")) &&
                           !File.Exists(subs), "el subtítulo viaja con él");
            Program.Assert(File.Exists(Path.Combine(raiz, "Season 03", "cap.nfo")),
                "y la ficha también");
            Program.Assert(File.Exists(ajeno), "el vídeo de al lado no se toca");

            // Deshacer devuelve TODO a su sitio exacto, compañeros incluidos, y
            // se lleva la carpeta que hizo falta crear: si no, deshacer dejaría
            // media biblioteca sembrada de carpetas vacías.
            var vueltos = MudanzaDeTemporada.Deshacer(parte);
            Program.Assert(vueltos == 1, "vuelve el que se movió");
            Program.Assert(File.Exists(origen) && !File.Exists(destino),
                "el vídeo está otra vez donde estaba");
            Program.Assert(File.Exists(subs) && File.Exists(ficha),
                "y sus compañeros con él");
            Program.Assert(!Directory.Exists(Path.Combine(raiz, "Season 03")),
                "la carpeta que se creó para nada se retira");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // ── Del plan solo se ejecuta lo marcado «Va» ──
    private static void SoloLoQueVa()
    {
        var raiz = Nueva();
        try
        {
            var quieto = Path.Combine(raiz, "conflicto.mkv");
            Crear(quieto);

            var parte = MudanzaDeTemporada.Aplicar(new[]
            {
                new PlanDeReordenado.Paso(quieto, null, PlanDeReordenado.Porque.SinCurar),
                new PlanDeReordenado.Paso(quieto, Path.Combine(raiz, "Season 01", "conflicto.mkv"),
                                          PlanDeReordenado.Porque.Ocupado),
            });

            Program.Assert(parte.Movidos.Count == 0, "no se mueve nada");
            Program.Assert(File.Exists(quieto), "el fichero sigue donde estaba");
            Program.Assert(!Directory.Exists(Path.Combine(raiz, "Season 01")),
                "ni siquiera se crea la carpeta de un paso que no va");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // ── La carrera: el plan se calculó antes, el disco pudo cambiar ──
    private static void NoPisaLoQueApareceEnMedio()
    {
        var raiz = Nueva();
        try
        {
            var origen = Path.Combine(raiz, "cap.mkv");
            var destino = Path.Combine(raiz, "Season 02", "cap.mkv");
            Crear(origen, "el bueno");
            Crear(destino, "el que ya estaba");   // apareció DESPUÉS de simular

            var parte = MudanzaDeTemporada.Aplicar(new[]
            {
                new PlanDeReordenado.Paso(origen, destino, PlanDeReordenado.Porque.Va),
            });

            Program.Assert(parte.Movidos.Count == 0 && parte.Fallidos.Count == 1,
                "se cuenta como fallo, no como hecho");
            Program.Assert(File.ReadAllText(destino) == "el que ya estaba",
                "no se pisa lo que hay en el destino");
            Program.Assert(File.Exists(origen), "y el origen se queda intacto");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    // ── Deshacer tiene la misma regla: nunca sobrescribe ──
    private static void ElDeshacerTampocoPisa()
    {
        var raiz = Nueva();
        try
        {
            var origen = Path.Combine(raiz, "cap.mkv");
            var destino = Path.Combine(raiz, "Season 04", "cap.mkv");
            Crear(origen, "el bueno");

            var parte = MudanzaDeTemporada.Aplicar(new[]
            {
                new PlanDeReordenado.Paso(origen, destino, PlanDeReordenado.Porque.Va),
            });
            Program.Assert(parte.Movidos.Count == 1, "se movió");

            Crear(origen, "uno nuevo que ocupó el hueco");

            Program.Assert(MudanzaDeTemporada.Deshacer(parte) == 0, "no vuelve ninguno");
            Program.Assert(File.ReadAllText(origen) == "uno nuevo que ocupó el hueco",
                "el que ocupó el hueco sigue ahí");
            Program.Assert(File.Exists(destino), "y el movido no se pierde: sigue en su destino");
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }
}
