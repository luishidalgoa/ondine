using Ondine.Objetivo;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Elegir cuánto se esmera el codificador.
///
/// <para>
/// Es el mismo compromiso de siempre —más lento, más pequeño con la misma calidad— pero
/// hasta ahora estaba fijado a mano por familia y no se podía tocar. Quien tiene la máquina
/// libre toda la noche no puede pedirle que apriete, y quien tiene prisa no puede aflojar.
/// </para>
/// <para>
/// <b>La trampa es que cada familia usa una escala distinta, y dos van AL REVÉS.</b> En
/// x265 «slow» es lento; en NVENC lo lento es <c>p7</c> y lo rápido <c>p1</c>; pero en
/// libaom y VP9 se cuenta con <c>-cpu-used</c>, donde <b>8 es lo más rápido y 0 lo más
/// lento</b>. Invertir esa dirección no da error de ningún tipo: la app diría «muy lento,
/// mejor calidad» y estaría pidiendo justo lo contrario. Por eso lo que se prueba aquí no
/// son los valores concretos, sino que <b>ir más lento sea ir más lento en las once</b>.
/// </para>
/// </summary>
public static class VelocidadDelCodificadorTests
{
    private static readonly string[] Codificadores =
    [
        "libx264", "libx265", "hevc_qsv", "h264_qsv", "av1_qsv", "vp9_qsv",
        "hevc_nvenc", "av1_nvenc", "hevc_amf", "libsvtav1", "libvpx-vp9", "libaom-av1",
    ];

    private static readonly Velocidad[] DeRapidoALento =
    [
        Velocidad.MuyRapido, Velocidad.Rapido, Velocidad.Equilibrado,
        Velocidad.Lento, Velocidad.MuyLento,
    ];

    public static void Todas()
    {
        Program.Seccion("Cuánto se esmera el codificador");

        foreach (var enc in Codificadores)
        {
            // ── Ninguna familia se queda sin traducir ─────────────────────────────
            // Una que cayera al caso por defecto recibiría la bandera de otra escala:
            // «-preset slow» a SVT-AV1, que espera un número, o al revés.
            var eq = VelocidadDelCodificador.Para(enc, Velocidad.Equilibrado);
            Program.Assert(eq.Count == 2,
                $"{enc}: se traduce a UNA bandera con su valor, no a media ni a tres");

            // ── Y ES la bandera de SU familia ─────────────────────────────────────
            Program.Assert(eq[0] == BanderaDe(enc),
                $"{enc}: usa la bandera que le toca — sale «{eq[0]}» y debería ser «{BanderaDe(enc)}»");

            // ══ LO QUE IMPORTA: ir más lento es ir más lento ══════════════════════
            // La escala de cada familia está escrita AQUÍ, en la prueba, y no se le
            // pregunta al código. La primera versión de esto le preguntaba —había un
            // `Escalon()` que devolvía el propio enum— y por tanto no comprobaba nada:
            // habría pasado igual de verde con las cinco escalas invertidas. Una prueba
            // que deriva del código lo que dice verificar es peor que no tenerla, porque
            // ocupa su sitio.
            var escala = EscalaDe(enc);
            var puestos = DeRapidoALento
                .Select(v => Array.IndexOf(escala, VelocidadDelCodificador.Para(enc, v)[1]))
                .ToList();

            Program.Assert(puestos.All(p => p >= 0),
                $"{enc}: todos los valores que produce existen en su escala real");

            for (var i = 1; i < puestos.Count; i++)
                Program.Assert(puestos[i] >= puestos[i - 1],
                    $"{enc}: «{DeRapidoALento[i]}» sale más RÁPIDO que «{DeRapidoALento[i - 1]}» " +
                    "— la escala está invertida, y eso no da error, solo hace lo contrario");

            Program.Assert(puestos[^1] > puestos[0],
                $"{enc}: «muy lento» y «muy rápido» tienen que ser distintos, o el ajuste no hace nada");
        }

        // ── Las dos escalas invertidas, comprobadas por su valor real ─────────────
        // Es el caso que motiva todo esto, así que se mira el número que sale de verdad
        // y no solo el orden: si el orden estuviera bien pero el valor mal, la prueba de
        // arriba pasaría igual.
        var aomRapido = VelocidadDelCodificador.Para("libaom-av1", Velocidad.MuyRapido);
        var aomLento = VelocidadDelCodificador.Para("libaom-av1", Velocidad.MuyLento);
        Program.Assert(int.Parse(aomRapido[1]) > int.Parse(aomLento[1]),
            "en libaom «-cpu-used» va al revés: el número ALTO es el rápido");

        var vpxRapido = VelocidadDelCodificador.Para("libvpx-vp9", Velocidad.MuyRapido);
        var vpxLento = VelocidadDelCodificador.Para("libvpx-vp9", Velocidad.MuyLento);
        Program.Assert(int.Parse(vpxRapido[1]) > int.Parse(vpxLento[1]),
            "y en VP9 igual, que es la otra que se cuenta al revés");

        // Y una que va «de frente», para que la comprobación de arriba no pase por
        // casualidad si alguien invirtiera TODAS.
        var nvRapido = VelocidadDelCodificador.Para("hevc_nvenc", Velocidad.MuyRapido);
        var nvLento = VelocidadDelCodificador.Para("hevc_nvenc", Velocidad.MuyLento);
        Program.Assert(string.CompareOrdinal(nvRapido[1], nvLento[1]) < 0,
            "en NVENC en cambio p1 es el rápido y p7 el lento: no todas van al revés");

        // ── x265 usa nombres, no números ──────────────────────────────────────────
        var x265 = VelocidadDelCodificador.Para("libx265", Velocidad.MuyLento);
        Program.Assert(!int.TryParse(x265[1], out _),
            "x265 espera un nombre («veryslow»), y darle un número lo rechaza");

        var svt = VelocidadDelCodificador.Para("libsvtav1", Velocidad.MuyLento);
        Program.Assert(int.TryParse(svt[1], out _),
            "y SVT-AV1 al contrario: espera un número, y un nombre lo rechaza");

        // ── El equilibrado es el de siempre ───────────────────────────────────────
        // Quien no toque nada tiene que seguir obteniendo lo que obtenía: un cambio
        // silencioso del valor por defecto haría que todo el mundo notara que «algo va
        // más lento» sin haber tocado nada.
        Program.Assert(VelocidadDelCodificador.Para("libx265", Velocidad.Equilibrado)[1] == "medium",
            "en x265 el equilibrado sigue siendo «medium», que es lo que había");
        Program.Assert(VelocidadDelCodificador.Para("hevc_qsv", Velocidad.Equilibrado)[1] == "slow",
            "y en QSV sigue siendo «slow», que es lo que había");
    }

    /// <summary>
    /// La escala real de cada familia, del MÁS RÁPIDO al más lento. Escrita a mano y a
    /// propósito: es lo que convierte esto en una comprobación de verdad y no en un espejo
    /// del código. Sale de la documentación de cada codificador, no de la implementación.
    /// </summary>
    private static string[] EscalaDe(string enc) => enc switch
    {
        // «-cpu-used»: el número ALTO es el rápido. Las dos que van al revés.
        "libaom-av1" => ["8", "7", "6", "5", "4", "3", "2", "1", "0"],
        "libvpx-vp9" => ["5", "4", "3", "2", "1", "0"],

        "hevc_amf" or "h264_amf" or "av1_amf" => ["speed", "balanced", "quality"],
        "hevc_nvenc" or "h264_nvenc" or "av1_nvenc" => ["p1", "p2", "p3", "p4", "p5", "p6", "p7"],
        "hevc_qsv" or "h264_qsv" or "av1_qsv" or "vp9_qsv" =>
            ["veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow"],

        // SVT-AV1: también número, y aquí el alto es el rápido.
        "libsvtav1" => ["13", "12", "11", "10", "9", "8", "7", "6", "5", "4", "3", "2", "1", "0"],

        _ => ["ultrafast", "superfast", "veryfast", "faster", "fast",
              "medium", "slow", "slower", "veryslow", "placebo"],
    };

    private static string BanderaDe(string enc) => enc switch
    {
        "libaom-av1" or "libvpx-vp9" => "-cpu-used",
        "hevc_amf" or "h264_amf" or "av1_amf" => "-quality",
        _ => "-preset",
    };
}
