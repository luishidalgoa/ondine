using System.Diagnostics;

namespace Ondine;

/// <summary>
/// Volver a abrir Ondine después de actualizarla, en Linux.
///
/// <para>
/// En Windows lo hace el instalador. Aquí no hay instalador que lo haga: el <c>.deb</c> lo
/// instala el sistema y el AppImage se sustituye. Quien lo lanza es la propia app, justo
/// antes de cerrarse.
/// </para>
/// <para>
/// <b>La nueva no puede arrancar mientras la vieja siga viva</b>: con la instancia única, la
/// segunda se encontraría el candado puesto, le pasaría el aviso a la que se está cerrando y
/// se iría. Por eso se lanza un ayudante que ESPERA a que el proceso termine y solo entonces
/// abre la nueva.
/// </para>
/// </summary>
public static class Reinicio
{
    /// <summary>
    /// Los argumentos para <c>/bin/sh</c>: espera a que muera <paramref name="pid"/> —con un
    /// tope, por si cancelan el cierre— y ejecuta la aplicación.
    ///
    /// <para>
    /// La ruta y el pid viajan como parámetros posicionales (<c>$1</c>, <c>$2</c>) y NO dentro
    /// del texto del guion: una ruta con espacios, comillas o <c>$</c> no puede romper la
    /// línea ni ejecutar nada que no sea ella.
    /// </para>
    /// </summary>
    public static string[] ArgumentosDelAyudante(int pid, string ejecutable) =>
    [
        "-c",
        "i=0; while kill -0 \"$1\" 2>/dev/null && [ \"$i\" -lt 240 ]; do sleep 0.5; i=$((i+1)); done; exec \"$2\"",
        "ondine-reinicio",
        pid.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ejecutable,
    ];

    /// <summary>
    /// Qué hay que ejecutar para abrir ESTA aplicación otra vez. Un AppImage se abre por su
    /// propio fichero (<c>APPIMAGE</c>), no por el ejecutable de dentro de su montaje, que
    /// desaparece al cerrarse. Y un <c>.deb</c> recién actualizado deja el ejecutable en uso
    /// con la etiqueta « (deleted)» al final, porque el instalador lo cambió de sitio.
    /// </summary>
    public static string? Ejecutable()
    {
        var appImage = Environment.GetEnvironmentVariable("APPIMAGE");
        if (!string.IsNullOrEmpty(appImage)) return appImage;
        return SinMarcaDeBorrado(Environment.ProcessPath);
    }

    public static string? SinMarcaDeBorrado(string? ruta)
    {
        const string marca = " (deleted)";
        return ruta is not null && ruta.EndsWith(marca, StringComparison.Ordinal)
            ? ruta[..^marca.Length]
            : ruta;
    }

    /// <summary>
    /// Deja listo el relevo: cuando este proceso acabe, se abre la aplicación. Devuelve si
    /// pudo preparar algo. Solo Linux y macOS: Windows tiene su instalador.
    /// </summary>
    public static bool AbrirCuandoSalga()
    {
        if (OperatingSystem.IsWindows()) return false;
        var exe = Ejecutable();
        if (string.IsNullOrEmpty(exe)) return false;

        try
        {
            var info = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
            foreach (var a in ArgumentosDelAyudante(Environment.ProcessId, exe)) info.ArgumentList.Add(a);
            Process.Start(info);
            return true;
        }
        catch { return false; }
    }
}
