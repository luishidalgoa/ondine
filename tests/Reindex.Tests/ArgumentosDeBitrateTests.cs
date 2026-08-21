using Ondine.Objetivo;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Los argumentos para codificar a un bitrate concreto.
///
/// <para>
/// <b>La trampa está en que CRF y bitrate objetivo se excluyen.</b> Si van los dos, cada
/// codificador hace una cosa distinta —unos ignoran el bitrate, otros el CRF, x265 avisa y
/// sigue— y el resultado es que no obedece a ninguno de los dos. El fichero sale con un
/// tamaño que no es el pedido y con una calidad que tampoco elegiste, y encima no falla:
/// hay que darse cuenta mirando el resultado.
/// </para>
/// <para>
/// Por eso esto se prueba codificador a codificador. Son ocho familias con banderas
/// distintas para lo mismo, y basta que una se cuele con su CRF puesto.
/// </para>
/// </summary>
public static class ArgumentosDeBitrateTests
{
    /// <summary>Las banderas de calidad constante. Ninguna puede aparecer con un bitrate.</summary>
    private static readonly string[] DeCalidadConstante =
        ["-crf", "-global_quality", "-cq", "-qp", "-qp_i", "-qp_p"];

    private static readonly string[] Codificadores =
    [
        "libx264", "libx265", "hevc_qsv", "h264_qsv", "av1_qsv",
        "hevc_nvenc", "av1_nvenc", "hevc_amf", "libsvtav1", "libvpx-vp9", "libaom-av1",
    ];

    public static void Todas()
    {
        Program.Seccion("Codificar a un bitrate concreto");

        foreach (var enc in Codificadores)
        {
            var args = ArgumentosDeBitrate.Para(enc, 1500);

            Program.Assert(args.Contains("-b:v"),
                $"{enc}: se le pide el bitrate con «-b:v»");

            Program.Assert(args.Contains("1500k"),
                $"{enc}: el bitrate va en kbits con la «k», que es como lo lee ffmpeg");

            var colada = DeCalidadConstante.FirstOrDefault(args.Contains);
            Program.Assert(colada is null,
                colada is null
                    ? $"{enc}: sin banderas de calidad constante, que anularían el objetivo"
                    : $"{enc}: se cuela «{colada}» junto al bitrate — con las dos puestas no obedece a ninguna");

            Program.Assert(args[0] == "-c:v" && args[1] == enc,
                $"{enc}: el codificador va primero, como en el resto del motor");
        }

        // ── El techo y el colchón ─────────────────────────────────────────────────
        // Sin techo, un VBR se dispara en las escenas movidas y el fichero se pasa del
        // objetivo justo en el sitio donde el objetivo importaba.
        var x265 = ArgumentosDeBitrate.Para("libx265", 1000);
        Program.Assert(x265.Contains("-maxrate") && x265.Contains("-bufsize"),
            "lleva techo y colchón: sin ellos el VBR se dispara y el fichero se pasa");

        var iTecho = x265.ToList().IndexOf("-maxrate");
        Program.Assert(x265[iTecho + 1] == "1500k",
            "el techo es una vez y media el objetivo: sitio para las escenas movidas sin desbocarse");

        var iColchon = x265.ToList().IndexOf("-bufsize");
        Program.Assert(x265[iColchon + 1] == "2000k",
            "y el colchón el doble, que es lo que deja al codificador repartir entre escenas");

        // ── Un bitrate imposible no se acepta en silencio ─────────────────────────
        foreach (var malo in new[] { 0, -100 })
        {
            var vacio = ArgumentosDeBitrate.Para("libx265", malo);
            Program.Assert(vacio.Count == 0,
                $"con {malo} kbps no se devuelven argumentos: pedir un bitrate de cero no es pedir nada");
        }
    }
}
