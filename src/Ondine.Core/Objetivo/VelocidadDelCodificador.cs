namespace Ondine.Objetivo;

/// <summary>
/// Cuánto se esmera el codificador. Una sola escala de cinco pasos, en cristiano.
///
/// <para>
/// El compromiso es siempre el mismo: más lento, más pequeño con la misma calidad. Se
/// expone en cinco pasos y no en los diez de x265 porque nadie sabe qué hay entre
/// «superfast» y «veryfast», y ofrecerlo solo hace la elección más difícil.
/// </para>
/// </summary>
public enum Velocidad
{
    MuyRapido,
    Rapido,
    /// <summary>Lo que la app hacía antes de que esto se pudiera elegir.</summary>
    Equilibrado,
    Lento,
    MuyLento,
}

/// <summary>
/// Traduce esa escala a lo que entiende cada familia de codificadores.
///
/// <para>
/// <b>Cada familia usa una escala distinta, y dos van al revés.</b> En x265 «slow» es lento;
/// en NVENC lo lento es <c>p7</c> y lo rápido <c>p1</c>; pero libaom y VP9 se cuentan con
/// <c>-cpu-used</c>, donde <b>8 es lo más rápido y 0 lo más lento</b>. Invertir esa dirección
/// no da error de ningún tipo: la app diría «muy lento, mejor calidad» y estaría pidiendo
/// justo lo contrario, y solo se notaría comparando tiempos.
/// </para>
/// <para>
/// Y las escalas no son intercambiables ni de forma: x265 espera un nombre y SVT-AV1 un
/// número. Darle a uno lo del otro no es «un poco peor», es que lo rechaza.
/// </para>
/// </summary>
public static class VelocidadDelCodificador
{
    // Aqui habia un «Bandera(encoder)» que devolvia la bandera de cada familia, y no lo llamaba
    // nadie en todo src/: la bandera ya sale de «Para(...)[0]», que es la MISMA lista y no puede
    // desincronizarse de los valores que acompana. Un segundo mapa por familia, sin usar, solo
    // sirve para que alguien lo actualice creyendo que arregla algo — o para que no lo actualice
    // y quede mintiendo.
    //
    // (El «BanderaDe» de VelocidadDelCodificadorTests es otra cosa y se queda: alli es un oraculo
    // ESCRITO A MANO desde la documentacion de cada codificador. Sustituirlo por esta funcion
    // convertiria su comprobacion en «Para[0] == Para[0]», que no comprueba nada.)

    public static IReadOnlyList<string> Para(string encoder, Velocidad v)
    {
        var i = (int)v;   // 0 = lo más rápido … 4 = lo más lento

        return encoder switch
        {
            // ── Al revés: 8 es el más rápido ──────────────────────────────────────
            "libaom-av1" => ["-cpu-used", Elegir(i, "8", "6", "4", "2", "0")],
            "libvpx-vp9" => ["-cpu-used", Elegir(i, "5", "4", "2", "1", "0")],

            // ── AMF: tres nombres, no cinco. Los extremos se repiten ─────────────
            "hevc_amf" or "h264_amf" or "av1_amf" =>
                ["-quality", Elegir(i, "speed", "speed", "balanced", "quality", "quality")],

            // ── NVENC: p1 rápido, p7 lento ───────────────────────────────────────
            "hevc_nvenc" or "h264_nvenc" or "av1_nvenc" =>
                ["-preset", Elegir(i, "p1", "p3", "p6", "p7", "p7")],

            // ── QSV: nombres propios. El equilibrado es «slow», que es lo que había ──
            "hevc_qsv" or "h264_qsv" or "av1_qsv" or "vp9_qsv" =>
                ["-preset", Elegir(i, "veryfast", "fast", "slow", "slower", "veryslow")],

            // ── SVT-AV1: número, y aquí el ALTO es el rápido ─────────────────────
            "libsvtav1" => ["-preset", Elegir(i, "10", "8", "6", "4", "2")],

            // ── x264 / x265: nombres. El equilibrado es «medium», el de siempre ──
            _ => ["-preset", Elegir(i, "veryfast", "faster", "medium", "slow", "veryslow")],
        };
    }

    private static string Elegir(int i, params string[] escala) => escala[Math.Clamp(i, 0, escala.Length - 1)];
}
