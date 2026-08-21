using Ondine.Recortes;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Cortar sin recodificar.
///
/// <para>
/// La diferencia con recodificar no es de velocidad, es de <b>naturaleza</b>: copiando los
/// paquetes tal cual no se pierde ni un ápice de calidad y tarda un suspiro, pero el corte
/// <b>solo puede caer en un fotograma clave</b>. En un fichero con fotograma clave cada dos
/// segundos, pedir el corte en el 10,4 lo pone en el 10,0 y no hay forma de afinarlo más.
/// </para>
/// <para>
/// Por eso lo que se prueba aquí no es «que corte», sino que la app <b>sepa decir de
/// antemano dónde va a caer de verdad</b>. Un corte que se mueve medio segundo sin avisar
/// es lo que hace que se desconfíe de la herramienta.
/// </para>
/// </summary>
public static class CorteSinRecodificarTests
{
    // Un fichero normalito: fotograma clave cada 2 s.
    private static readonly double[] CadaDosSegundos =
        [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

    public static void Todas()
    {
        Program.Seccion("Cortar sin recodificar: dónde cae el corte de verdad");

        // ── El corte SIEMPRE retrocede al fotograma clave anterior ────────────────
        // Hacia atrás y no hacia delante: adelantándolo se perdería contenido que
        // pediste, y eso no se ve venir. Retrocediendo sobra un poco al principio,
        // que se nota y no se pierde nada.
        var a = CorteSinRecodificar.DondeCae(CadaDosSegundos, 10.4);
        Program.Assert(a.Real == 10 && a.Pedido == 10.4,
            "pedir el 10,4 corta en el 10: se retrocede al fotograma clave anterior");
        Program.Assert(Math.Abs(a.Desfase - 0.4) < 0.0001,
            "y el desfase se dice en claro, para poder enseñarlo antes de cortar");

        var justo = CorteSinRecodificar.DondeCae(CadaDosSegundos, 8);
        Program.Assert(justo.Real == 8 && justo.Desfase == 0 && !justo.SeMueve,
            "pedir justo un fotograma clave no mueve nada, y se sabe");

        var casi = CorteSinRecodificar.DondeCae(CadaDosSegundos, 11.999);
        Program.Assert(casi.Real == 10,
            "casi llegando al siguiente sigue retrocediendo: no se adelanta nunca");

        // ── Los extremos ──────────────────────────────────────────────────────────
        var principio = CorteSinRecodificar.DondeCae(CadaDosSegundos, 0.5);
        Program.Assert(principio.Real == 0,
            "antes del primer fotograma clave se corta desde el principio");

        var final = CorteSinRecodificar.DondeCae(CadaDosSegundos, 100);
        Program.Assert(final.Real == 20,
            "más allá del último se queda en el último que hay");

        // Sin índice no se puede prometer nada. Devolver «cae donde pediste» sería
        // mentir, y es justo la mentira que hace desconfiar de la herramienta.
        var sinIndice = CorteSinRecodificar.DondeCae([], 10.4);
        Program.Assert(!sinIndice.SeSabe,
            "sin lista de fotogramas clave se admite que no se sabe, en vez de inventar");

        // ── El contenedor tiene que ser el mismo ──────────────────────────────────
        // Copiar los paquetes significa meterlos tal cual en otra caja: si la caja no
        // admite ese códec, no hay copia que valga. La regla honesta es no cambiar de
        // caja, y eso hay que decirlo antes de que alguien elija MP4 y no entienda por
        // qué se le recodifica.
        Program.Assert(CorteSinRecodificar.SePuedeCopiar(".mkv", ".mkv"),
            "misma caja: se puede copiar");
        Program.Assert(CorteSinRecodificar.SePuedeCopiar(".MP4", ".mp4"),
            "y las mayúsculas de la extensión no cuentan");
        Program.Assert(!CorteSinRecodificar.SePuedeCopiar(".mkv", ".mp4"),
            "cambiar de caja obliga a recodificar: no es un capricho, es que puede no caber");
        Program.Assert(!CorteSinRecodificar.SePuedeCopiar(".mkv", ".webm"),
            "y a WebM menos todavía, que solo admite dos códecs");

        // ── Los argumentos de ffmpeg ──────────────────────────────────────────────
        var args = CorteSinRecodificar.Argumentos("entrada.mkv", "salida.mkv", 10, 5);

        Program.Assert(Indice(args, "-ss") < Indice(args, "-i"),
            "el salto va ANTES del «-i»: si no, ffmpeg decodifica todo lo anterior para tirarlo");
        Program.Assert(Indice(args, "-t") > Indice(args, "-i"),
            "y la duración después, que se mide sobre la salida");
        Program.Assert(Seguido(args, "-c", "copy"),
            "se copian los paquetes: ni un recodificado");
        Program.Assert(Seguido(args, "-map", "0"),
            "y TODAS las pistas: los packs traen varios idiomas de audio y subtítulos");

        // La que costó una tarde en Organizar y no se vuelve a pagar aquí: con «-c copy»
        // el corte arranca en el fotograma clave anterior, y esos fotogramas de más llevan
        // marca de tiempo negativa, así que el reproductor los descarta y el trozo empieza
        // donde toca. «-avoid_negative_ts make_zero» los desplaza a cero y pasan a verse:
        // el trozo arrancaba 5 segundos antes y enseñaba el final de la historia anterior.
        Program.Assert(!args.Contains("-avoid_negative_ts"),
            "NADA de «-avoid_negative_ts»: haría visibles los fotogramas de más del arranque");

        var desdeCero = CorteSinRecodificar.Argumentos("e.mkv", "s.mkv", 0, 5);
        Program.Assert(!desdeCero.Contains("-ss"),
            "desde el principio no hace falta saltar");

        var hastaElFinal = CorteSinRecodificar.Argumentos("e.mkv", "s.mkv", 10, 0);
        Program.Assert(!hastaElFinal.Contains("-t"),
            "sin duración se llega hasta el final");
    }

    private static int Indice(IReadOnlyList<string> args, string cual)
    {
        for (var i = 0; i < args.Count; i++) if (args[i] == cual) return i;
        return -1;
    }

    private static bool Seguido(IReadOnlyList<string> args, string uno, string otro)
    {
        var i = Indice(args, uno);
        return i >= 0 && i + 1 < args.Count && args[i + 1] == otro;
    }
}
