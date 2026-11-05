using System.Diagnostics;
using System.Text;
using Ondine.Localizacion;

namespace Ondine;

/// <summary>
/// Aviso del sistema («notificación») cuando termina una tanda de compresión.
///
/// <para>
/// Sin paquetes externos en ninguna plataforma. Windows usa la notificación que trae el propio
/// Windows, invocada desde PowerShell; Linux usa <c>notify-send</c> (libnotify); macOS usa
/// <c>osascript</c>, que la app ya usa para la Papelera. Ninguna es obligatoria: si falta la
/// herramienta o no hay servicio de notificaciones, el aviso se pierde y la compresión sigue
/// igual. Nunca lanza excepciones ni espera a que el aviso salga.
/// </para>
/// <para>
/// El texto nunca va metido en el comando. En Windows viaja en variables de entorno, y en Linux
/// y macOS como argumentos sueltos. Así un nombre de película con comillas no puede romper el
/// comando ni colar nada en él.
/// </para>
/// </summary>
public static class AvisoDeSistema
{
    /// <summary>Avisa de que una tanda ha terminado, con cuántos salieron bien y cuántos no.</summary>
    public static void TandaTerminada(int correctos, int fallidos)
    {
        var cuerpo = string.Format(Textos.Instancia.AvisoTandaCuerpo, correctos, fallidos);
        Enviar(Textos.Instancia.AvisoTandaTitulo, cuerpo);
    }

    /// <summary>Lanza el aviso en segundo plano. No devuelve nada: un aviso que falla no es un error.</summary>
    public static void Enviar(string titulo, string cuerpo)
    {
        try
        {
            var plataforma = OperatingSystem.IsWindows() ? "windows"
                           : OperatingSystem.IsMacOS() ? "macos"
                           : "linux";
            var psi = Armar(plataforma, titulo, cuerpo);
            if (psi is null) return;
            _ = Task.Run(() => Lanzar(psi, plataforma));
        }
        catch { /* sin aviso: la tanda ya ha terminado y eso es lo que importa */ }
    }

    private static void Lanzar(ProcessStartInfo psi, string plataforma)
    {
        try
        {
            psi.RedirectStandardError = true;
            using var proc = Process.Start(psi);
            if (proc is null) { Anotar(plataforma, "no se pudo lanzar el proceso"); return; }

            // El guion de Windows deja aquí el motivo si algo falla; se recoge para poder verlo en avisos.log.
            var error = proc.StandardError.ReadToEndAsync();
            if (!proc.WaitForExit(15_000))
            {
                try { proc.Kill(); } catch { }
                Anotar(plataforma, "tiempo agotado: el proceso no terminó en 15 s");
                return;
            }
            error.Wait(2_000);
            var codigo = proc.ExitCode;
            Anotar(plataforma, codigo == 0 ? "enviado" : $"codigo {codigo}: {(error.IsCompletedSuccessfully ? error.Result.Trim() : "")}");
        }
        catch (Exception ex)
        {
            // Falta la herramienta o no hay servicio de notificaciones: se anota, y la compresión sigue igual.
            Anotar(plataforma, "excepción: " + ex.Message);
        }
    }

    /// <summary>
    /// Anota lo que pasó con cada aviso en avisos.log, en la carpeta de datos de Ondine. Sirve para diagnosticar
    /// un aviso que no sale: el registro dice si el sistema lo recibió, y con qué código y motivo falló.
    /// </summary>
    internal static void Anotar(string plataforma, string linea)
    {
        try
        {
            Directory.CreateDirectory(DatosDeUsuario.Raiz);
            File.AppendAllText(Path.Combine(DatosDeUsuario.Raiz, "avisos.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{plataforma}] {linea}{Environment.NewLine}");
        }
        catch { /* anotar no puede romper nada */ }
    }

    /// <summary>
    /// El comando para cada plataforma. Se separa de <see cref="Enviar"/> para poder comprobar
    /// qué se lanza sin lanzarlo, desde cualquier sistema.
    /// </summary>
    internal static ProcessStartInfo? Armar(string plataforma, string titulo, string cuerpo)
    {
        switch (plataforma)
        {
            case "windows":
                {
                    // El guion va codificado en base64 para que ninguna comilla ni barra del
                    // propio guion llegue al analizador de la línea de comandos.
                    var guion = string.Join("\n", GuionWindows);
                    var psi = new ProcessStartInfo(RutaPowerShellWindows)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                    };
                    psi.ArgumentList.Add("-NoProfile");
                    psi.ArgumentList.Add("-NonInteractive");
                    psi.ArgumentList.Add("-WindowStyle");
                    psi.ArgumentList.Add("Hidden");
                    psi.ArgumentList.Add("-EncodedCommand");
                    psi.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(guion)));
                    // El texto viaja aparte, en el entorno, y el guion lo lee de ahí.
                    psi.Environment["ONDINE_AVISO_TITULO"] = titulo;
                    psi.Environment["ONDINE_AVISO_CUERPO"] = cuerpo;
                    return psi;
                }

            case "macos":
                {
                    // Mismo patrón que la Papelera de macOS: el texto va en argv, no en el guion.
                    var psi = new ProcessStartInfo("/usr/bin/osascript")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                    };
                    psi.ArgumentList.Add("-e");
                    psi.ArgumentList.Add("on run argv");
                    psi.ArgumentList.Add("-e");
                    psi.ArgumentList.Add("display notification (item 2 of argv) with title (item 1 of argv)");
                    psi.ArgumentList.Add("-e");
                    psi.ArgumentList.Add("end run");
                    psi.ArgumentList.Add(titulo);
                    psi.ArgumentList.Add(cuerpo);
                    return psi;
                }

            case "linux":
                {
                    var psi = new ProcessStartInfo("notify-send")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                    };
                    psi.ArgumentList.Add("--app-name=Ondine");
                    psi.ArgumentList.Add("--");
                    psi.ArgumentList.Add(titulo);
                    psi.ArgumentList.Add(cuerpo);
                    return psi;
                }

            default:
                return null;
        }
    }

    private static string RutaPowerShellWindows =>
        Path.Combine(Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows",
                     "System32", "WindowsPowerShell", "v1.0", "powershell.exe");

    // Notificación nativa de Windows 10 y 11 sin paquetes. Se muestra con el nombre del host
    // de PowerShell, que es lo que Windows deja usar a un proceso sin identidad propia.
    private static readonly string[] GuionWindows =
    {
        "try {",
        "[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null",
        "[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null",
        "$xml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)",
        "$textos = $xml.GetElementsByTagName('text')",
        "$textos.Item(0).AppendChild($xml.CreateTextNode($env:ONDINE_AVISO_TITULO)) | Out-Null",
        "$textos.Item(1).AppendChild($xml.CreateTextNode($env:ONDINE_AVISO_CUERPO)) | Out-Null",
        "$aviso = [Windows.UI.Notifications.ToastNotification]::new($xml)",
        "$app = '{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\\WindowsPowerShell\\v1.0\\powershell.exe'",
        "$notificador = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($app)",
        // Si el usuario ha silenciado «Windows PowerShell» o está el modo concentración, el sistema no lo muestra.
        // Se dice por aquí, para que quede en avisos.log.
        "if ($notificador.Setting -ne 'Enabled') { [Console]::Error.WriteLine('silenciado: ' + $notificador.Setting); exit 3 }",
        "$notificador.Show($aviso)",
        "exit 0",
        "} catch { [Console]::Error.WriteLine('error: ' + $_.Exception.Message); exit 2 }",
    };
}
