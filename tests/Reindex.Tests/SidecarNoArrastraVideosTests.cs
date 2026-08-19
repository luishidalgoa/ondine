using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Un compañero es un subtítulo o una ficha. <b>Nunca otro vídeo.</b>
///
/// <para>
/// El planificador decidía «es compañero» solo por el nombre: cualquier fichero
/// que empezara por «&lt;base&gt;.» viajaba con el vídeo. Y hay nombres muy
/// normales en los que el nombre base de un vídeo es prefijo del de otro —«Up.mkv»
/// y «Up.2009.mkv» en la misma carpeta—, así que el segundo se movía como si
/// fuera un subtítulo.
/// </para>
/// <para>
/// Lo grave no es que se mueva: es que se movía <b>a espaldas del plan</b>. Una
/// fila pintada en rojo como «ese nombre ya está ocupado» —es decir, «no lo
/// toco»— acababa movida igualmente por ser compañera de otra. El pie de la
/// ventana dice «todavía no se ha tocado nada», y eso tiene que ser verdad.
/// </para>
/// </summary>
public static class SidecarNoArrastraVideosTests
{
    private static string R(params string[] p) => Path.Combine(p);

    public static void Todas()
    {
        Program.Seccion("Un compañero nunca es otro vídeo");

        var raiz = R("C:", "Plex", "Movies");
        var video = R(raiz, "Up.mkv");
        var destino = R(raiz, "Up (2009)", "Up (2009).mkv");

        var enCarpeta = new[]
        {
            video,
            R(raiz, "Up.srt"),          // sí: subtítulo
            R(raiz, "Up.es.srt"),       // sí: subtítulo con idioma
            R(raiz, "Up.nfo"),          // sí: ficha
            R(raiz, "Up.2009.mkv"),     // NO: es otra película
            R(raiz, "Up.1080p.mp4"),    // NO: otro vídeo
        };

        var plan = SidecarPlanner.Planear(video, destino, enCarpeta);
        var llevados = plan.Select(p => Path.GetFileName(p.De)).ToList();

        Program.Assert(llevados.Contains("Up.srt") && llevados.Contains("Up.es.srt")
                       && llevados.Contains("Up.nfo"),
            "los subtítulos y la ficha sí viajan con su vídeo");

        Program.Assert(!llevados.Contains("Up.2009.mkv"),
            "otro VÍDEO no es un compañero: moverlo lo mete en la carpeta de una película que no es");

        Program.Assert(!llevados.Contains("Up.1080p.mp4"),
            "y da igual la extensión de vídeo que sea");

        Program.Assert(plan.Count == 3, "tres compañeros, ni uno más");

        // Y el nombre nuevo se lo llevan, que es para lo que existe esto.
        Program.Assert(plan.Any(p => Path.GetFileName(p.A) == "Up (2009).es.srt"),
            "el subtítulo llega con el nombre nuevo y su sufijo de idioma intacto");
    }
}
