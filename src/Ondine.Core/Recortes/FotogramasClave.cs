using System.Diagnostics;
using System.Globalization;

namespace Ondine.Recortes;

/// <summary>
/// El índice de fotogramas clave de un vídeo, que es lo que decide dónde puede caer un
/// corte sin recodificar.
///
/// <para>
/// <b>Se pregunta por una ventana, no por el fichero entero</b>, y esa es la decisión que
/// importa. Listar los paquetes de una película de dos horas son cientos de miles de
/// líneas y varios segundos de espera; para saber dónde cae un corte solo hacen falta los
/// de justo antes. Con la ventana, la respuesta es inmediata y se puede enseñar mientras
/// arrastras la junta.
/// </para>
/// <para>
/// Se leen los PAQUETES y no los fotogramas: los paquetes salen de la cabecera y del
/// índice, sin decodificar nada. Preguntar por fotogramas obliga a ffprobe a decodificar,
/// y ahí se va el tiempo que esto viene a ahorrar.
/// </para>
/// </summary>
public static class FotogramasClave
{
    /// <summary>
    /// Cuánto se mira hacia atrás. Un vídeo normal trae fotograma clave cada 2-10 s; con
    /// 60 s de margen se cubren de sobra los que espacian mucho, y sigue siendo una lectura
    /// corta.
    /// </summary>
    public const double VentanaSeg = 60;

    /// <summary>
    /// Los fotogramas clave que hay entre <paramref name="segundo"/> menos la ventana y ese
    /// segundo, en orden. Lista vacía si no se pudo saber — y entonces no se promete nada.
    /// </summary>
    public static async Task<IReadOnlyList<double>> AntesDeAsync(
        string ruta, double segundo, CancellationToken ct = default)
    {
        var desde = Math.Max(0, segundo - VentanaSeg);
        var claves = await LeerAsync(ruta, desde, segundo - desde + 0.5, ct);

        // Si en la ventana no cayó ninguno, el fichero espacia más de lo normal. Se mira
        // desde el principio antes de rendirse: es lento, pero es raro y vale más que
        // decir «no se sabe» pudiendo saberlo.
        if (claves.Count == 0 && desde > 0)
            claves = await LeerAsync(ruta, 0, segundo + 0.5, ct);

        return claves;
    }

    private static async Task<IReadOnlyList<double>> LeerAsync(
        string ruta, double desde, double cuanto, CancellationToken ct)
    {
        static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        var args = new[]
        {
            "-v", "error",
            "-select_streams", "v:0",
            // La ventana. «%+N» es «desde este punto, N segundos».
            "-read_intervals", $"{N(desde)}%+{N(cuanto)}",
            "-show_entries", "packet=pts_time,flags",
            "-of", "csv=p=0",
            "--", ruta,
        };

        var salida = await CorrerAsync(Engine.FfprobePath, args, ct);
        if (salida is null) return [];

        var claves = new List<double>();
        foreach (var linea in salida.Split('\n'))
        {
            var trozos = linea.Split(',');
            if (trozos.Length < 2) continue;

            // La «K» de la columna de banderas marca el fotograma clave.
            if (!trozos[1].Contains('K')) continue;

            if (double.TryParse(trozos[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var t))
                claves.Add(t);
        }

        claves.Sort();
        return claves;
    }

    private static async Task<string?> CorrerAsync(string exe, string[] args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo(exe)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p is null) return null;

            var salida = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            return p.ExitCode == 0 ? salida : null;
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Sin ffprobe, con un fichero ilegible o con un formato sin índice, no se sabe.
            // No saberlo es un resultado válido: quien llame lo dirá en vez de inventarse
            // una promesa sobre dónde cae el corte.
            return null;
        }
    }
}
