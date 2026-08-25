using Ondine.Audio;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El plan de audio de UNA pista: qué se hace con ella y por qué.
///
/// <para>
/// Esto existe porque la decisión vivía dentro de <c>BuildArgs</c>, una función local en el
/// bucle de ficheros de <c>Engine.CompressAsync</c>: inalcanzable desde aquí, así que la regla
/// más delicada del motor —copiar o recodificar— no tenía ni una prueba. Y se notó.
/// </para>
/// <para>
/// <b>El fallo que lo trajo.</b> Un usuario puso «Códec de audio: Sin tocar» sobre un E-AC-3 de
/// 224 kbps y le salió <c>-c:a:0 aac -b:a:0 128k</c>: pérdida irreversible del ajuste que existe
/// justamente para impedirla. La causa era un atajo en el motor —«si hay caudal puesto y el
/// origen no es aac/opus/mp3/vorbis, recodifica a AAC»— con dos problemas: pisaba lo pedido, y
/// su lista se dejaba fuera E-AC-3, AC-3 y DTS, que también son con pérdida. Encima lo hacía en
/// silencio: el aviso del registro mira lo que decidió <see cref="CodecDeAudio"/>, y a esa
/// función ya se le pasaba el códec mutado.
/// </para>
/// <para>
/// Lo que se comprueba aquí es la regla nueva: <b>«Sin tocar» manda</b>. El contenedor es lo
/// único que puede impedir una copia, la mezcla a estéreo es lo único que la impide por
/// necesidad, y un caudal elegido sobre una copia no cambia nada — pero se dice.
/// </para>
/// </summary>
public static class PlanDeAudioTests
{
    public static void Todas()
    {
        Program.Seccion("El plan de audio de cada pista");

        SinTocarManda();
        LoQueNoCabeSeRecodifica();
        LoPedidoASecas();
        BajarAEstereoObligaARehacer();
        ElCaudalSoloCuentaSiSeRecodifica();
        NadaCambiaEnSilencio();
    }

    /// <summary>
    /// El caso del usuario, exacto: E-AC-3 224 kbps a un MP4, «Sin tocar», y un caudal de 128
    /// puesto por el preset. Tiene que copiar.
    /// </summary>
    private static void SinTocarManda()
    {
        var p = PlanDeAudio.Para("mp4", AudioElegido.Copiar, Mezcla.SinTocar,
                                 kbpsPedido: 128, codecOrigen: "eac3", canalesOrigen: 2, indice: 0);

        Program.Assert(p.Codec.Copiar, "«Sin tocar» sobre un E-AC-3 en MP4 copia: cabe, y se pidió");
        Program.Assert(p.Argumentos.SequenceEqual(["-c:a:0", "copy"]),
            $"y los argumentos son solo eso ({string.Join(" ", p.Argumentos)})");
        Program.Assert(!p.Argumentos.Any(x => x.StartsWith("-b:a")),
            "copiando no hay caudal que poner: son los bytes tal cual");

        // Mayúsculas: ffprobe devuelve minúsculas, pero la lista de MP4 compara sin distinguir
        // y esto lo deja fijado. Una comparación ordinal aquí volvería a recodificar.
        var mayus = PlanDeAudio.Para("mp4", AudioElegido.Copiar, Mezcla.SinTocar, 128, "EAC3", 2, 0);
        Program.Assert(mayus.Codec.Copiar, "y da igual cómo venga escrito el códec de origen");

        // Y en MKV, donde cabe todo, tampoco se toca nada aunque el origen sea enorme.
        var mkv = PlanDeAudio.Para("mkv", AudioElegido.Copiar, Mezcla.SinTocar, 128, "truehd", 8, 0);
        Program.Assert(mkv.Codec.Copiar, "un TrueHD en MKV se copia: el contenedor lo admite");
    }

    /// <summary>
    /// Lo que NO cabe sigue recodificándose, y al códec que pide el contenedor. Sin esto la
    /// prueba de arriba se pasaría con un «copia siempre», que produce ficheros que no abren.
    /// </summary>
    private static void LoQueNoCabeSeRecodifica()
    {
        var dts = PlanDeAudio.Para("mp4", AudioElegido.Copiar, Mezcla.SinTocar, 128, "dts", 6, 0);

        Program.Assert(!dts.Codec.Copiar && dts.Codec.Codec == "aac",
            $"un DTS no cabe en MP4: se recodifica a AAC ({dts.Codec.Codec})");
        Program.Assert(dts.Codec.Porque == PorQueSeCambio.ElOrigenNoSePuedeCopiar,
            "y queda dicho por qué, que es lo que enciende el aviso del registro");
        Program.Assert(dts.Argumentos.Contains("-b:a:0") && dts.Argumentos.Contains("128k"),
            $"aquí sí manda el caudal pedido ({string.Join(" ", dts.Argumentos)})");

        var webm = PlanDeAudio.Para("webm", AudioElegido.Copiar, Mezcla.SinTocar, 0, "aac", 2, 1);
        Program.Assert(!webm.Codec.Copiar && webm.Codec.Codec == "libopus",
            $"y en WebM el respaldo es Opus, no AAC ({webm.Codec.Codec})");
        Program.Assert(webm.Kbps == 160, $"con el caudal por defecto de WebM ({webm.Kbps})");
    }

    private static void LoPedidoASecas()
    {
        var p = PlanDeAudio.Para("mp4", AudioElegido.Aac, Mezcla.SinTocar, 128, "eac3", 2, 0);
        Program.Assert(p.Argumentos.SequenceEqual(["-c:a:0", "aac", "-b:a:0", "128k"]),
            $"pedir AAC 128 recodifica a AAC 128, sin sorpresas ({string.Join(" ", p.Argumentos)})");
        Program.Assert(p.Codec.Porque == PorQueSeCambio.SeHizoLoPedido, "y sin nada que avisar");

        // Sin caudal elegido, el que corresponde a los canales que queden.
        var seisCanales = PlanDeAudio.Para("mkv", AudioElegido.Ac3, Mezcla.SinTocar, 0, "dts", 6, 0);
        Program.Assert(seisCanales.Kbps == MezclaDeAudio.KbpsMultiCanal,
            $"un 5.1 sin caudal elegido va a {MezclaDeAudio.KbpsMultiCanal} ({seisCanales.Kbps})");
    }

    /// <summary>
    /// Bajar a estéreo es la ÚNICA razón por la que «Sin tocar» deja de copiar sin que el
    /// contenedor tenga nada que ver: copiar es pasar los bytes y mezclar es rehacerlos, y
    /// pedir las dos cosas no da error — ffmpeg copia y se salta la mezcla en silencio.
    /// </summary>
    private static void BajarAEstereoObligaARehacer()
    {
        var p = PlanDeAudio.Para("mkv", AudioElegido.Copiar, Mezcla.Estereo, 0, "eac3", 6, 2);

        Program.Assert(!p.Codec.Copiar, "pedir estéreo sobre un 5.1 impide copiar");
        Program.Assert(p.Mezcla.HayQueMezclar && p.Argumentos.Contains("-ac:a:2"),
            $"y la mezcla va en los argumentos ({string.Join(" ", p.Argumentos)})");
        Program.Assert(p.Kbps == MezclaDeAudio.KbpsEstereo,
            $"con el caudal de los canales de DESPUÉS, no los de antes ({p.Kbps})");

        // Y sobre un estéreo no hay nada que bajar: se sigue copiando.
        var yaEstereo = PlanDeAudio.Para("mkv", AudioElegido.Copiar, Mezcla.Estereo, 0, "eac3", 2, 0);
        Program.Assert(yaEstereo.Codec.Copiar,
            "pedir estéreo sobre un estéreo no fuerza recodificar: sería perder calidad para nada");
    }

    /// <summary>
    /// El caudal elegido sobre una copia no cambia nada — y esa es la trampa que hay que
    /// declarar. Antes el motor lo resolvía recodificando en silencio; ahora copia, pero
    /// <see cref="PlanDeAudio.PlanDeUnaPista.CaudalIgnorado"/> deja que la interfaz lo diga.
    /// El silencio en cualquiera de las dos direcciones es el fallo.
    /// </summary>
    private static void ElCaudalSoloCuentaSiSeRecodifica()
    {
        var conCaudal = PlanDeAudio.Para("mp4", AudioElegido.Copiar, Mezcla.SinTocar, 128, "eac3", 2, 0);
        Program.Assert(conCaudal.CaudalIgnorado,
            "elegir 128 kbps con «Sin tocar» no hace nada, y el plan lo dice");
        Program.Assert(conCaudal.Kbps == 0, $"el caudal del plan es cero: no se aplica ({conCaudal.Kbps})");

        var sinCaudal = PlanDeAudio.Para("mp4", AudioElegido.Copiar, Mezcla.SinTocar, 0, "eac3", 2, 0);
        Program.Assert(!sinCaudal.CaudalIgnorado,
            "y sin caudal elegido no hay nada que avisar: es el camino normal");

        var recodifica = PlanDeAudio.Para("mp4", AudioElegido.Aac, Mezcla.SinTocar, 128, "eac3", 2, 0);
        Program.Assert(!recodifica.CaudalIgnorado, "recodificando el caudal sí cuenta, así que tampoco");
    }

    /// <summary>
    /// La invariante que sostiene todo lo demás: si se pidió copiar y no se copia, tiene que
    /// haber un motivo contable. Es lo que impide que vuelva a colarse un atajo que cambie lo
    /// pedido sin que el registro se entere.
    /// </summary>
    private static void NadaCambiaEnSilencio()
    {
        string[] contenedores = ["mkv", "mp4", "webm"];
        string[] origenes = ["aac", "eac3", "ac3", "dts", "truehd", "flac", "pcm_s16le", "opus", "mp3"];
        int[] caudales = [0, 128, 192];
        int[] canales = [2, 6];
        Mezcla[] mezclas = [Mezcla.SinTocar, Mezcla.Estereo];

        int mudos = 0, conCaudalYCopia = 0;
        foreach (var c in contenedores)
            foreach (var o in origenes)
                foreach (var k in caudales)
                    foreach (var ch in canales)
                        foreach (var mz in mezclas)
                        {
                            var p = PlanDeAudio.Para(c, AudioElegido.Copiar, mz, k, o, ch, 0);

                            if (!p.Codec.Copiar
                                && p.Codec.Porque == PorQueSeCambio.SeHizoLoPedido
                                && !p.Mezcla.HayQueMezclar) mudos++;

                            // Y nunca las dos cosas a la vez: copiar y poner caudal es una
                            // orden contradictoria que ffmpeg acepta ignorando media.
                            if (p.Codec.Copiar && p.Argumentos.Any(x => x.StartsWith("-b:a"))) conCaudalYCopia++;
                        }

        Program.Assert(mudos == 0, $"nada deja de copiarse sin un motivo que contar ({mudos} casos mudos)");
        Program.Assert(conCaudalYCopia == 0, $"y nunca «copy» con caudal al lado ({conCaudalYCopia})");
    }
}
