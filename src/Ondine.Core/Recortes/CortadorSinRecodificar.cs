using System.Diagnostics;
using Ondine.Localizacion;

namespace Ondine.Recortes;

/// <summary>Cómo fue un corte sin recodificar.</summary>
/// <param name="Ok">Si salió un fichero con contenido.</param>
/// <param name="Salida">Dónde quedó.</param>
/// <param name="Error">Lo que dijo ffmpeg, si algo fue mal.</param>
public sealed record ResultadoDelCorte(bool Ok, string Salida, string? Error = null);

/// <summary>
/// Saca un tramo copiando los paquetes. Es el mismo gesto que ya hacía Organizar al partir
/// un capítulo en sus historias; aquí se ofrece en Recortes, que es donde faltaba.
/// </summary>
public static class CortadorSinRecodificar
{
    public static async Task<ResultadoDelCorte> CortarAsync(
        string origen, string destino, double desde, double duracion,
        CancellationToken ct = default)
    {
        var args = CorteSinRecodificar.Argumentos(origen, destino, desde, duracion);

        string error;
        try
        {
            error = await CorrerAsync(Engine.FfmpegPath, args, ct);
        }
        catch (OperationCanceledException)
        {
            // Cancelar deja un fichero a medias, y un trozo a medias es peor que ninguno:
            // parece que salió. Se borra antes de propagar.
            Borrar(destino);
            throw;
        }

        var salioAlgo = File.Exists(destino) && new FileInfo(destino).Length > 0;
        if (salioAlgo) return new(true, destino);

        Borrar(destino);
        return new(false, destino, error.Length > 0 ? error.Trim() : null);
    }

    private static void Borrar(string ruta)
    {
        try { if (File.Exists(ruta)) File.Delete(ruta); } catch { /* no empeora nada */ }
    }

    /// <summary>Lanza ffmpeg y devuelve lo que escriba. ffmpeg informa por stderr.</summary>
    private static async Task<string> CorrerAsync(
        string exe, IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException(
                string.Format(Textos.Instancia.MotorNoSePudoLanzar, exe));

        var err = p.StandardError.ReadToEndAsync(ct);
        var _ = p.StandardOutput.ReadToEndAsync(ct);

        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            throw;
        }

        return await err;
    }
}
