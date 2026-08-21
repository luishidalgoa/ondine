namespace Ondine.Audio;

/// <summary>Qué hacer con los canales.</summary>
public enum Mezcla
{
    /// <summary>Como venga. Lo que la app hacía antes de que esto se pudiera elegir.</summary>
    SinTocar,
    /// <summary>Bajar a dos canales lo que traiga más. Nunca sube nada.</summary>
    Estereo,
}

/// <summary>Qué va a pasar con los canales de una pista.</summary>
/// <param name="HayQueMezclar">Si de verdad hay algo que rehacer.</param>
/// <param name="CanalesFinales">Con cuántos canales queda. De aquí sale el bitrate.</param>
public readonly record struct DecisionDeMezcla(bool HayQueMezclar, int CanalesFinales);

/// <summary>
/// Bajar el audio a estéreo, o dejarlo como está.
///
/// <para>
/// Un 5.1 pesa el doble que un estéreo y no sirve de nada en unos auriculares o en la tele
/// del cuarto. Bajarlo es lo que convierte una película de 8 GB en una de 5 sin tocar el
/// vídeo.
/// </para>
/// <para>
/// <b>La regla que se equivoca sola es el bitrate.</b> Tiene que seguir a los canales de
/// DESPUÉS de la mezcla. Mantener el del 5.1 al bajar a estéreo desperdicia media pista;
/// dejar un 5.1 con el del estéreo suena mal. Ninguna de las dos falla ni avisa.
/// </para>
/// </summary>
public static class MezclaDeAudio
{
    /// <summary>Lo que necesita un 5.1 para sonar bien. Por debajo se nota.</summary>
    public const int KbpsMultiCanal = 448;

    /// <summary>Para dos canales o uno. La diferencia que importa es 5.1 o no.</summary>
    public const int KbpsEstereo = 192;

    public static DecisionDeMezcla Decidir(Mezcla mezcla, int canalesOrigen)
    {
        // Nunca se SUBE de canales: pedir estéreo sobre un mono no es bajar, es inventar
        // un canal que no existe. Y pedir estéreo sobre un estéreo no puede forzar una
        // recodificación — sería perder calidad y tiempo para dejarlo exactamente igual.
        if (mezcla != Mezcla.Estereo || canalesOrigen <= 2)
            return new(false, Math.Max(1, canalesOrigen));

        return new(true, 2);
    }

    /// <summary>El bitrate que le corresponde a ese número de canales.</summary>
    public static int BitratePorDefecto(int canales) =>
        canales >= 6 ? KbpsMultiCanal : KbpsEstereo;

    /// <summary>
    /// Si con esta decisión se puede seguir copiando los bytes tal cual.
    ///
    /// <para>
    /// Copiar es pasar los bytes; mezclar es rehacerlos. Pedir las dos cosas <b>no da
    /// error</b>: ffmpeg copia y se salta la mezcla en silencio, y el fichero sale con su
    /// 5.1 intacto pese a que la app decía «estéreo».
    /// </para>
    /// </summary>
    public static bool SePuedeCopiar(DecisionDeMezcla d) => !d.HayQueMezclar;

    /// <summary>
    /// Los argumentos de UNA pista. Sin nada que mezclar no se manda nada: un «-ac» de más
    /// es ruido que alguien acabará depurando.
    /// </summary>
    public static IReadOnlyList<string> Argumentos(int indice, DecisionDeMezcla d) =>
        d.HayQueMezclar ? [$"-ac:a:{indice}", $"{d.CanalesFinales}"] : [];
}
