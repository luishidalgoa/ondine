namespace Ondine.Audio;

/// <summary>
/// Qué se hace con UNA pista de audio: copiar o recodificar, con cuántos canales, a qué caudal,
/// y con qué argumentos de ffmpeg.
///
/// <para>
/// <b>Por qué existe.</b> Esto vivía dentro de <c>BuildArgs</c>, una función local en el bucle
/// de ficheros de <c>Engine.CompressAsync</c>, y desde allí no se podía probar: la regla más
/// delicada del motor —la que decide si el audio del usuario se recodifica— no tenía ni una
/// prueba, y con un fallo dentro las seis suites de argumentos seguían verdes.
/// </para>
/// <para>
/// <b>Quién manda.</b> El códec pedido. «Sin tocar» significa copiar, y solo dos cosas pueden
/// impedirlo: que el contenedor no admita ese códec de origen (lo decide
/// <see cref="CodecDeAudio"/>) o que haya que bajar a estéreo (lo decide
/// <see cref="MezclaDeAudio"/>, porque mezclar es rehacer los bytes). El caudal <b>no</b> manda:
/// es el destino de una recodificación, no una orden de recodificar.
/// </para>
/// <para>
/// <b>El atajo que había y por qué se fue.</b> El motor traía una regla de compatibilidad: si
/// había caudal puesto y el códec de origen no estaba en <c>{ aac, opus, mp3, vorbis }</c>,
/// recodificaba a AAC pasando por encima de lo pedido. Se cargaba dos cosas. Una, la lista se
/// dejaba fuera E-AC-3, AC-3 y DTS, que también son con pérdida, así que un E-AC-3 de 224 kbps
/// acababa en AAC de 128 mientras un AAC de 320 se salvaba: la misma petición hacía cosas
/// distintas según el origen. Y dos, lo hacía <b>en silencio</b>, porque el aviso del registro
/// mira lo que decidió <see cref="CodecDeAudio"/> y a esa función ya se le pasaba el códec
/// mutado — la decisión se tomaba antes de llegar a quien sabía contarla.
/// </para>
/// <para>
/// Quien de verdad quería aquel atajo eran los presets y las dos pantallas sin desplegable de
/// códec (Recortes y la línea de órdenes). Esos ahora piden AAC explícitamente, que es lo que
/// siempre quisieron decir.
/// </para>
/// </summary>
public static class PlanDeAudio
{
    /// <summary>El caudal por defecto en WebM, donde el audio es Opus y rinde más por kbps.</summary>
    public const int KbpsWebm = 160;

    /// <summary>
    /// Lo que va a pasar con la pista.
    /// </summary>
    /// <param name="Codec">Copiar o a qué códec, y por qué si no es lo pedido.</param>
    /// <param name="Mezcla">Qué pasa con los canales.</param>
    /// <param name="Kbps">El caudal que se va a aplicar. Cero al copiar: no hay ninguno.</param>
    /// <param name="CaudalIgnorado">
    /// Se eligió un caudal y no se va a aplicar porque se está copiando. No es un error —copiar
    /// es lo pedido— pero hay que poder DECIRLO: un ajuste que no hace nada y no avisa es la
    /// misma clase de silencio que el atajo que esto vino a quitar, solo en la otra dirección.
    /// </param>
    /// <param name="Argumentos">Los argumentos de ffmpeg de esta pista, con su índice.</param>
    public readonly record struct PlanDeUnaPista(
        DecisionDeAudio Codec,
        DecisionDeMezcla Mezcla,
        int Kbps,
        bool CaudalIgnorado,
        IReadOnlyList<string> Argumentos);

    /// <param name="contenedor">«mkv», «mp4» o «webm».</param>
    /// <param name="pedido">El códec elegido en la interfaz.</param>
    /// <param name="mezcla">Qué hacer con los canales.</param>
    /// <param name="kbpsPedido">El caudal elegido, o 0 si se dejó en automático.</param>
    /// <param name="codecOrigen">El <c>codec_name</c> que dio ffprobe.</param>
    /// <param name="canalesOrigen">Los canales que trae la pista.</param>
    /// <param name="indice">El índice de la pista en la salida.</param>
    public static PlanDeUnaPista Para(string contenedor, AudioElegido pedido, Mezcla mezcla,
                                     int kbpsPedido, string codecOrigen, int canalesOrigen, int indice)
    {
        // La mezcla se decide ANTES que el códec: bajar a estéreo impide copiar, y ffmpeg no
        // avisa de eso — copiaría y se saltaría la mezcla en silencio, dejando el 5.1 intacto
        // pese a que la app decía estéreo.
        var m = MezclaDeAudio.Decidir(mezcla, canalesOrigen);

        var elegido = pedido;
        if (!MezclaDeAudio.SePuedeCopiar(m) && elegido == AudioElegido.Copiar)
            elegido = AudioElegido.Aac;

        var d = CodecDeAudio.Decidir(contenedor, elegido, codecOrigen ?? "");

        // El caudal sigue a los canales de DESPUÉS de la mezcla: mantener el del 5.1 al bajar a
        // estéreo desperdicia media pista, y al revés suena mal. Y copiando no hay caudal
        // ninguno que aplicar, así que el elegido se queda sin usar y se dice.
        var kbps = d.Copiar
            ? 0
            : kbpsPedido > 0
                ? kbpsPedido
                : contenedor == "webm" ? KbpsWebm : MezclaDeAudio.BitratePorDefecto(m.CanalesFinales);

        return new(d, m, kbps,
                   CaudalIgnorado: d.Copiar && kbpsPedido > 0,
                   Argumentos: [.. CodecDeAudio.Argumentos(indice, d, kbps),
                                .. MezclaDeAudio.Argumentos(indice, m)]);
    }
}
