using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ondine.Ava;

/// <summary>
/// Enseñar un fichero en el gestor de archivos del sistema, señalado.
///
/// <para>
/// En WPF era una línea —<c>explorer.exe /select,"ruta"</c>— y aquí son tres sistemas, así
/// que vive en su sitio en vez de repetida en cada pantalla.
/// </para>
/// <para>
/// <b>En Linux no hay una sola forma, pero sí la hay por gestor.</b> La primera versión de
/// esto abría la carpeta y ya —<c>xdg-open</c> no sabe señalar un fichero— y quedó escrito
/// como una limitación. Es media verdad: <c>xdg-open</c> no sabe, pero <b>los gestores sí</b>.
/// Nemo, el de Cinnamon —o sea el de Linux Mint—, lo señala; Nautilus, Dolphin, Thunar y
/// Caja también, cada uno con su forma de pedirlo.
/// </para>
/// <para>
/// Así que se prueban por orden y solo se cae a abrir la carpeta cuando no hay ninguno. El
/// distintivo que lleva a esto existe para ahorrar la búsqueda; abrir una carpeta con
/// cuatrocientos capítulos dentro no la ahorra.
/// </para>
/// </summary>
internal static class EnElGestorDeArchivos
{
    /// <summary>
    /// Los gestores que saben señalar un fichero, con cómo se les pide.
    ///
    /// <para>
    /// El orden importa: primero el del escritorio más probable en cada familia. Nemo va el
    /// primero porque es el de Cinnamon, y Mint es donde más se va a usar esto.
    /// </para>
    /// <para>
    /// <c>nemo</c> y <c>caja</c> señalan pasándoles el fichero a secas; los otros tres
    /// quieren una opción. Pasarle el fichero a secas a <c>nautilus</c> ABRE el vídeo en el
    /// reproductor en vez de enseñarlo, que es justo lo contrario de lo que se pide.
    /// </para>
    /// </summary>
    private static readonly (string Programa, string[] Antes)[] Gestores =
    [
        ("nemo",     []),                  // Cinnamon · Linux Mint
        ("nautilus", ["--select"]),        // GNOME
        ("dolphin",  ["--select"]),        // KDE
        ("caja",     []),                  // MATE · Linux Mint edición MATE
        ("thunar",   []),                  // Xfce · Linux Mint edición Xfce
        ("pcmanfm",  []),                  // LXDE
    ];

    /// <summary>
    /// Abre el gestor de archivos en ese fichero. Devuelve si se pudo lanzar algo — si no,
    /// quien llama decide qué decir: un botón que no hace nada visible convierte «no tengo
    /// gestor de archivos» en «esto está roto».
    /// </summary>
    public static bool Ensenar(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return false;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Lanzar("explorer.exe", $"/select,\"{ruta}\"");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Lanzar("open", $"-R \"{ruta}\"");

            // Linux: el que esté instalado, por orden.
            foreach (var (programa, antes) in Gestores)
            {
                if (!EstaInstalado(programa)) continue;
                var args = string.Join(' ', antes.Append($"\"{ruta}\""));
                if (Lanzar(programa, args)) return true;
            }

            // Ninguno: se abre la carpeta, que es lo único que xdg-open sabe hacer. Peor,
            // pero mejor que no hacer nada.
            var carpeta = System.IO.Path.GetDirectoryName(ruta);
            return !string.IsNullOrEmpty(carpeta) && Lanzar("xdg-open", $"\"{carpeta}\"");
        }
        catch { return false; }
    }

    /// <summary>
    /// Si el programa existe en el PATH. Se pregunta antes de lanzarlo porque lanzar uno que
    /// no está <b>no falla enseguida</b> en todos los casos, y probar los seis a base de
    /// excepciones deja una ristra de procesos muertos por el camino.
    /// </summary>
    private static bool EstaInstalado(string programa)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo("/usr/bin/which", programa)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p is null) return false;
            p.WaitForExit(1500);
            return p.HasExited && p.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool Lanzar(string programa, string argumentos)
    {
        try
        {
            Process.Start(new ProcessStartInfo(programa, argumentos) { UseShellExecute = false });
            return true;
        }
        catch { return false; }
    }
}
