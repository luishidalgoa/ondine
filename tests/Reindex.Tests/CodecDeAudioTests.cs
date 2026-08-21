using Ondine.Audio;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Elegir el códec de audio.
///
/// <para>
/// Hasta ahora había dos opciones enterradas en el motor: copiar, o pasarlo a AAC. Quien
/// quiere AC3 para el receptor del salón, o quiere Opus porque pesa la mitad, no podía.
/// </para>
/// <para>
/// <b>Lo que hay que acertar es que no todo cabe en todo.</b> WebM solo admite Opus y
/// Vorbis; MP4 no sabe guardar FLAC de forma que nadie lo reproduzca. Si se le pasa a ffmpeg
/// una combinación imposible, unas veces falla y otras produce un fichero que no abre en
/// ningún sitio — y eso último es peor, porque parece que salió bien. Así que se decide
/// antes, se cambia lo que no cabe, y <b>se dice que se cambió y por qué</b>: cambiarlo en
/// silencio deja al usuario creyendo que tiene AC3 cuando tiene AAC.
/// </para>
/// </summary>
public static class CodecDeAudioTests
{
    public static void Todas()
    {
        Program.Seccion("Elegir el códec de audio");

        // ── MKV se lo traga todo ──────────────────────────────────────────────────
        foreach (var q in new[] { AudioElegido.Aac, AudioElegido.Ac3, AudioElegido.Eac3,
                                  AudioElegido.Opus, AudioElegido.Flac })
        {
            var d = CodecDeAudio.Decidir("mkv", q, "dts");
            Program.Assert(d.Porque == PorQueSeCambio.SeHizoLoPedido,
                $"mkv admite {q}: no hay nada que cambiar");
        }

        // ── WebM solo admite Opus ─────────────────────────────────────────────────
        var webmAac = CodecDeAudio.Decidir("webm", AudioElegido.Aac, "aac");
        Program.Assert(webmAac.Codec == "libopus" && webmAac.Porque == PorQueSeCambio.ContenedorNoLoAdmite,
            "en WebM el AAC pedido pasa a Opus, y se dice que fue el contenedor");

        var webmCopia = CodecDeAudio.Decidir("webm", AudioElegido.Copiar, "aac");
        Program.Assert(!webmCopia.Copiar && webmCopia.Codec == "libopus",
            "y copiar tampoco vale: un AAC dentro de un WebM no lo abre nadie");

        // Copiar SÍ vale si lo que hay ya es Opus: no se recodifica por gusto.
        var webmYaOpus = CodecDeAudio.Decidir("webm", AudioElegido.Copiar, "opus");
        Program.Assert(webmYaOpus.Copiar && webmYaOpus.Porque == PorQueSeCambio.SeHizoLoPedido,
            "si el origen ya es Opus se copia tal cual: recodificar Opus a Opus solo pierde");

        // ── MP4 y el FLAC ─────────────────────────────────────────────────────────
        var mp4Flac = CodecDeAudio.Decidir("mp4", AudioElegido.Flac, "flac");
        Program.Assert(mp4Flac.Codec == "aac" && mp4Flac.Porque == PorQueSeCambio.ContenedorNoLoAdmite,
            "MP4 con FLAC es legal sobre el papel y no lo reproduce casi nada: se pasa a AAC");

        // ── Copiar algo que el contenedor no aguanta ──────────────────────────────
        var mp4CopiaDts = CodecDeAudio.Decidir("mp4", AudioElegido.Copiar, "dts");
        Program.Assert(!mp4CopiaDts.Copiar && mp4CopiaDts.Porque == PorQueSeCambio.ElOrigenNoSePuedeCopiar,
            "no se puede copiar un DTS a un MP4, así que se recodifica y se dice por qué");

        var mp4CopiaAac = CodecDeAudio.Decidir("mp4", AudioElegido.Copiar, "aac");
        Program.Assert(mp4CopiaAac.Copiar,
            "pero un AAC sí se copia a MP4: ahí no hay nada que arreglar");

        // ── AC3 y E-AC3, que es lo que pide un receptor de salón ──────────────────
        var ac3 = CodecDeAudio.Decidir("mkv", AudioElegido.Ac3, "aac");
        Program.Assert(ac3.Codec == "ac3", "AC3 se pide por su nombre de codificador");

        var eac3EnMp4 = CodecDeAudio.Decidir("mp4", AudioElegido.Eac3, "aac");
        Program.Assert(eac3EnMp4.Codec == "eac3" && eac3EnMp4.Porque == PorQueSeCambio.SeHizoLoPedido,
            "y E-AC3 sí cabe en MP4: no se cambia lo que no hace falta");

        // ══ EL BITRATE: al FLAC no se le pone ═════════════════════════════════════
        // FLAC es sin pérdida. Un «-b:a 192k» sobre FLAC no lo comprime a 192: o lo
        // ignora o falla según la versión, y en el primer caso el usuario cree que ha
        // pedido algo que no ha pedido.
        var argsFlac = CodecDeAudio.Argumentos(0, CodecDeAudio.Decidir("mkv", AudioElegido.Flac, "pcm"), 192);
        Program.Assert(!argsFlac.Any(x => x.StartsWith("-b:a")),
            "al FLAC no se le pone bitrate: es sin pérdida, y ponérselo es pedir algo que no existe");

        var argsAac = CodecDeAudio.Argumentos(0, CodecDeAudio.Decidir("mkv", AudioElegido.Aac, "dts"), 192);
        Program.Assert(argsAac.Contains("-b:a:0") && argsAac.Contains("192k"),
            "y al AAC sí, con el índice de su pista");

        var argsCopia = CodecDeAudio.Argumentos(2, CodecDeAudio.Decidir("mkv", AudioElegido.Copiar, "dts"), 192);
        Program.Assert(argsCopia.Contains("-c:a:2") && argsCopia.Contains("copy")
                       && !argsCopia.Any(x => x.StartsWith("-b:a")),
            "copiando tampoco hay bitrate que poner: se copian los bytes tal cual");

        // El índice de pista se respeta: es lo que ata cada ajuste a SU pista, y
        // equivocarlo aplica el códec de la primera a todas.
        Program.Assert(argsAac[0] == "-c:a:0" && argsCopia[0] == "-c:a:2",
            "cada pista lleva su índice: sin él, el ajuste de una se aplicaría a todas");
    }
}
