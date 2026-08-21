using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ondine.Ava;

/// <summary>
/// Enseñar un fichero en el gestor de archivos del sistema.
///
/// <para>
/// En WPF era una línea —<c>explorer.exe /select,"ruta"</c>— y aquí son tres sistemas con
/// tres respuestas, así que vive en su sitio en vez de repetida en cada pantalla.
/// </para>
/// <para>
/// <b>Y no las tres hacen lo mismo, que es lo que hay que decir en voz alta.</b> Windows y
/// macOS saben abrir la carpeta <i>con el fichero ya señalado</i>. En Linux no hay una forma
/// que funcione en todos los escritorios: <c>xdg-open</c> abre carpetas, no selecciona
/// ficheros. Así que ahí se abre la carpeta y el fichero hay que buscarlo con la vista.
/// </para>
/// <para>
/// Es una diferencia pequeña y real. Se deja escrita porque el distintivo que lleva a esto
/// existe justamente para ahorrar la búsqueda, y en Linux la ahorra solo a medias.
/// </para>
/// </summary>
internal static class EnElGestorDeArchivos
{
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

            // Linux: se abre la CARPETA, no el fichero señalado. Ningún gestor comparte una
            // forma de pedir «y márcame este de dentro» que funcione en todos.
            var carpeta = Path.GetDirectoryName(ruta);
            return !string.IsNullOrEmpty(carpeta) && Lanzar("xdg-open", $"\"{carpeta}\"");
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
