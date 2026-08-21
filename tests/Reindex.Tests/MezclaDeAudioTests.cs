using Ondine.Audio;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Bajar el audio a estéreo, o dejarlo como está.
///
/// <para>
/// Un 5.1 pesa el doble que un estéreo y no sirve de nada en unos auriculares o en la tele
/// del cuarto. Poder bajarlo es lo que convierte una película de 8 GB en una de 5 sin tocar
/// el vídeo.
/// </para>
/// <para>
/// <b>La regla que se equivoca sola es el bitrate.</b> Tiene que seguir a los canales de
/// DESPUÉS de la mezcla, no a los de antes. Si al bajar un 5.1 a estéreo se mantiene el
/// bitrate del 5.1, se desperdicia la mitad del fichero en un audio que ya no lo necesita; y
/// al revés, dejar un 5.1 con el bitrate del estéreo suena mal. Ninguna de las dos cosas
/// falla ni avisa: hay que darse cuenta escuchando o mirando el tamaño.
/// </para>
/// </summary>
public static class MezclaDeAudioTests
{
    public static void Todas()
    {
        Program.Seccion("Bajar el audio a estéreo");

        // ── Bajar un 5.1 ──────────────────────────────────────────────────────────
        var baja = MezclaDeAudio.Decidir(Mezcla.Estereo, canalesOrigen: 6);
        Program.Assert(baja.HayQueMezclar && baja.CanalesFinales == 2,
            "un 5.1 al que se le pide estéreo se baja a 2 canales");

        // ── Y no tocar lo que ya está ─────────────────────────────────────────────
        // Pedir estéreo sobre algo que ya es estéreo NO puede forzar una recodificación:
        // seria perder calidad y tiempo para dejarlo exactamente igual.
        var yaEsta = MezclaDeAudio.Decidir(Mezcla.Estereo, canalesOrigen: 2);
        Program.Assert(!yaEsta.HayQueMezclar && yaEsta.CanalesFinales == 2,
            "pedir estéreo sobre algo que ya lo es no hace nada: recodificar para dejarlo igual solo pierde");

        var mono = MezclaDeAudio.Decidir(Mezcla.Estereo, canalesOrigen: 1);
        Program.Assert(!mono.HayQueMezclar && mono.CanalesFinales == 1,
            "y un mono no se «sube» a estéreo: eso no es bajar, es inventar un canal");

        // ── Sin tocar ─────────────────────────────────────────────────────────────
        var intacto = MezclaDeAudio.Decidir(Mezcla.SinTocar, canalesOrigen: 6);
        Program.Assert(!intacto.HayQueMezclar && intacto.CanalesFinales == 6,
            "«sin tocar» deja el 5.1 como está, que es lo que viene puesto");

        // ══ EL BITRATE SIGUE A LOS CANALES DE DESPUÉS ═════════════════════════════
        var deUn51QueBaja = MezclaDeAudio.BitratePorDefecto(
            MezclaDeAudio.Decidir(Mezcla.Estereo, 6).CanalesFinales);
        var deUn51QueSeQueda = MezclaDeAudio.BitratePorDefecto(
            MezclaDeAudio.Decidir(Mezcla.SinTocar, 6).CanalesFinales);

        Program.Assert(deUn51QueBaja < deUn51QueSeQueda,
            $"al bajar a estéreo baja el bitrate ({deUn51QueBaja} contra {deUn51QueSeQueda}): " +
            "mantener el del 5.1 desperdiciaría media pista");

        Program.Assert(deUn51QueSeQueda >= 384,
            "y un 5.1 que se queda necesita su bitrate: con el del estéreo suena mal");

        Program.Assert(MezclaDeAudio.BitratePorDefecto(2) == MezclaDeAudio.BitratePorDefecto(1),
            "mono y estéreo comparten cifra: la diferencia que importa es 5.1 o no");

        // ── Los argumentos ────────────────────────────────────────────────────────
        var args = MezclaDeAudio.Argumentos(1, MezclaDeAudio.Decidir(Mezcla.Estereo, 6));
        Program.Assert(args.Count == 2 && args[0] == "-ac:a:1" && args[1] == "2",
            "la mezcla va con el índice de SU pista: sin él se aplicaría a todas");

        var sinArgs = MezclaDeAudio.Argumentos(0, MezclaDeAudio.Decidir(Mezcla.SinTocar, 6));
        Program.Assert(sinArgs.Count == 0,
            "sin nada que mezclar no se manda nada: un «-ac» de más es ruido que alguien acabará depurando");

        // ══ Y LO QUE NO SE PUEDE HACER A LA VEZ ══════════════════════════════════
        // Copiar es pasar los bytes tal cual. Mezclar es rehacerlos. Pedir las dos cosas
        // no da error: ffmpeg copia y se salta la mezcla en silencio, y el fichero sale
        // con su 5.1 intacto pese a que la app decía «estéreo».
        Program.Assert(!MezclaDeAudio.SePuedeCopiar(MezclaDeAudio.Decidir(Mezcla.Estereo, 6)),
            "no se puede copiar Y mezclar: ffmpeg copiaría y se saltaría la mezcla sin avisar");

        Program.Assert(MezclaDeAudio.SePuedeCopiar(MezclaDeAudio.Decidir(Mezcla.SinTocar, 6)),
            "pero sin mezcla, copiar sigue siendo lo mejor: no se pierde nada");

        Program.Assert(MezclaDeAudio.SePuedeCopiar(MezclaDeAudio.Decidir(Mezcla.Estereo, 2)),
            "y pedir estéreo sobre un estéreo no impide copiarlo, porque no hay mezcla que hacer");
    }
}
