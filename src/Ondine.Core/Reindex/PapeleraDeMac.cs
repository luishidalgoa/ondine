using System.Diagnostics;

namespace Ondine;

/// <summary>
/// Mandar a la papelera en macOS.
///
/// <para>
/// Aquí no hay un acuerdo sobre carpetas como en Linux ni una llamada como en Windows. La
/// papelera del Mac es <c>~/.Trash</c>, pero <b>mover el fichero ahí a mano no es mandarlo a
/// la papelera</b>: aparece en el Dock, sí, y «Volver a poner» sale en gris. El registro de
/// dónde venía cada cosa lo lleva el Finder en su propia base de datos, y no se escribe
/// desde fuera.
/// </para>
/// <para>
/// Así que se le pide al Finder. Cuesta dos cosas y conviene saberlas: <b>arranca un proceso</b>
/// (<c>osascript</c>) y la primera vez <b>macOS pregunta</b> si Ondine puede controlar el
/// Finder — un permiso de automatización que el usuario concede una vez. A cambio, lo borrado
/// se comporta como lo borrado por cualquier otra app del sistema, incluido «Volver a poner»,
/// que es justo lo que la app promete.
/// </para>
/// <para>
/// La ruta va <b>como argumento</b> y no metida en el texto del guion. Es lo que evita el
/// problema clásico de esta vía: un nombre de película con una comilla —«Ocean's Eleven»—
/// cerraría la cadena de AppleScript y el guion no compilaría, o peor, haría otra cosa.
/// </para>
/// </summary>
internal static class PapeleraDeMac
{
    public static bool Mandar(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return false;
        if (!File.Exists(ruta) && !Directory.Exists(ruta)) return false;

        return PorElFinder(Path.GetFullPath(ruta)) || ALaCarpetaTrash(Path.GetFullPath(ruta));
    }

    /// <summary>
    /// El camino bueno. El guion recibe la ruta en <c>argv</c>, así que ningún carácter del
    /// nombre puede romperlo.
    /// </summary>
    private static bool PorElFinder(string ruta)
    {
        try
        {
            var p = new ProcessStartInfo("/usr/bin/osascript")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            p.ArgumentList.Add("-e");
            p.ArgumentList.Add("on run argv");
            p.ArgumentList.Add("-e");
            p.ArgumentList.Add("tell application \"Finder\" to delete (POSIX file (item 1 of argv))");
            p.ArgumentList.Add("-e");
            p.ArgumentList.Add("end run");
            p.ArgumentList.Add(ruta);

            using var proc = Process.Start(p);
            if (proc is null) return false;

            // Con tope: si el permiso de automatización está sin conceder, el sistema saca su
            // propio diálogo y esto se queda esperando la respuesta. Diez segundos son de
            // sobra para el caso normal —el Finder responde en milisegundos— y evitan que
            // borrar treinta ficheros deje la app colgada media hora.
            if (!proc.WaitForExit(10_000)) { try { proc.Kill(true); } catch { } return false; }

            // Y se comprueba el resultado, no solo el código de salida: osascript puede
            // devolver 0 con el Finder quejándose por la salida de error.
            return proc.ExitCode == 0 && !File.Exists(ruta) && !Directory.Exists(ruta);
        }
        catch { return false; }
    }

    /// <summary>
    /// El respaldo, para cuando no hay Finder al que pedírselo: una sesión sin escritorio, el
    /// permiso denegado a propósito.
    ///
    /// <para>
    /// Deja el fichero en la papelera y visible, pero <b>sin «Volver a poner»</b>, porque eso
    /// solo lo sabe el Finder. Se hace igualmente porque la alternativa es no mover nada — y
    /// entre «recuperable a mano» y «sigue ocupando sitio donde estaba», lo primero se parece
    /// más a lo que se pidió.
    /// </para>
    /// </summary>
    private static bool ALaCarpetaTrash(string ruta)
    {
        try
        {
            var papelera = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Trash");
            Directory.CreateDirectory(papelera);

            var destino = Libre(papelera, Path.GetFileName(ruta.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

            if (Directory.Exists(ruta)) Directory.Move(ruta, destino);
            else File.Move(ruta, destino);
            return true;
        }
        catch
        {
            // Si no se pudo mover, NO se borra: el fichero se queda donde estaba, que es lo
            // que quien lo mandó a la papelera preferiría.
            return false;
        }
    }

    /// <summary>
    /// Un nombre libre. Dos capítulos que se llaman igual en carpetas distintas es lo más
    /// normal del mundo, y el segundo no puede comerse al primero en silencio.
    /// </summary>
    private static string Libre(string papelera, string nombre)
    {
        var destino = Path.Combine(papelera, nombre);
        if (!File.Exists(destino) && !Directory.Exists(destino)) return destino;

        var sinExt = Path.GetFileNameWithoutExtension(nombre);
        var ext = Path.GetExtension(nombre);
        for (int i = 2; i < 10_000; i++)
        {
            destino = Path.Combine(papelera, $"{sinExt} ({i}){ext}");
            if (!File.Exists(destino) && !Directory.Exists(destino)) return destino;
        }
        return Path.Combine(papelera, $"{sinExt} ({Guid.NewGuid():N}){ext}");
    }
}
