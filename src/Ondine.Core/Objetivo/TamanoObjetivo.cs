namespace Ondine.Objetivo;

/// <summary>Por qué un tamaño objetivo no se puede cumplir.</summary>
public enum PorQueNoCabe
{
    Cabe,
    /// <summary>Sin duración no hay cuenta que hacer.</summary>
    SinDuracion,
    /// <summary>Ni con el vídeo a cero: el audio solo ya se pasa.</summary>
    NoCabeNiElAudio,
    /// <summary>Cabría, pero dejando el vídeo por debajo de lo legible.</summary>
    NoLlegaAlMinimo,
}

/// <summary>Qué bitrate hace falta para caber, o por qué no se puede.</summary>
/// <param name="VideoKbps">Lo que le queda al vídeo. Cero si no cabe.</param>
/// <param name="AudioKbps">Lo que se lleva el audio. No se toca: bajarlo por detrás sería
/// cambiar algo que el usuario eligió.</param>
/// <param name="MinimoNecesarioBytes">
/// El tamaño más pequeño que sí valdría. Es lo único accionable cuando no cabe — sin esta
/// cifra solo queda ir probando números a ciegas.
/// </param>
public readonly record struct Objetivo(
    int VideoKbps,
    int AudioKbps,
    PorQueNoCabe Porque,
    long MinimoNecesarioBytes)
{
    public bool Cabe => Porque == PorQueNoCabe.Cabe;
}

/// <summary>
/// Llegar a un tamaño concreto: «que quepa en un pendrive», «que entre en el límite de
/// subida». Es la petición más común de un compresor.
///
/// <para>
/// <b>Lo que hay que acertar no es la cuenta —esa es una división— sino decir que NO cabe.</b>
/// Pedir 50 MB para dos horas se puede «cumplir» dando 40 kbps de vídeo, y sale un puré
/// ilegible que técnicamente pesa lo pedido. Eso es peor que negarse: has esperado la
/// codificación entera para tirar el resultado. Por eso hay un suelo, y por debajo se dice
/// que no, por qué, y cuánto haría falta.
/// </para>
/// </summary>
public static class TamanoObjetivo
{
    /// <summary>
    /// Por debajo de esto el vídeo deja de ser mirable. Es el mismo suelo que ya usaba el
    /// estimador para no prometer bitrates de fantasía, y se comparte a propósito: dos
    /// suelos distintos harían que la estimación y el objetivo se contradijeran.
    /// </summary>
    public const int MinimoVideoKbps = 120;

    /// <summary>
    /// Lo que se lleva el contenedor en cabeceras e índices. Se DESCUENTA del objetivo, no
    /// se suma: sumarlo dejaría el fichero pasándose justo del límite, que es el único
    /// sitio donde un límite importa.
    /// </summary>
    public const double MargenContenedor = 1.02;

    public static Objetivo Calcular(
        long bytesObjetivo,
        double duracionSeg,
        int audioKbps,
        int bitrateOriginalKbps = 0)
    {
        if (duracionSeg <= 0)
            return new(0, audioKbps, PorQueNoCabe.SinDuracion, 0);

        // Todo el presupuesto, en kbps, ya sin el margen del contenedor.
        var totalKbps = bytesObjetivo * 8.0 / duracionSeg / 1000.0 / MargenContenedor;
        var paraVideo = totalKbps - audioKbps;

        // El tamaño más pequeño que valdría: audio + el suelo del vídeo, con su margen.
        var minimo = (long)((audioKbps + MinimoVideoKbps) * 1000.0 / 8.0
                            * duracionSeg * MargenContenedor);

        // Ni con el vídeo a cero. Decir «baja la calidad» aquí sería un consejo inútil:
        // bajarla no arreglaría nada, porque el problema no es el vídeo.
        if (paraVideo <= 0)
            return new(0, audioKbps, PorQueNoCabe.NoCabeNiElAudio, minimo);

        if (paraVideo < MinimoVideoKbps)
            return new(0, audioKbps, PorQueNoCabe.NoLlegaAlMinimo, minimo);

        var video = (int)paraVideo;

        // No se recomprime «hacia arriba»: pasado el bitrate del original no hay calidad
        // que ganar, solo tiempo que perder y espacio que gastar. Mismo criterio que el
        // estimador. Sin saber el original no se inventa un tope.
        if (bitrateOriginalKbps > 0) video = Math.Min(video, bitrateOriginalKbps);

        return new(video, audioKbps, PorQueNoCabe.Cabe, minimo);
    }
}
