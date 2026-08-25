using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Ondine.Localizacion;

namespace Ondine;

public sealed class UpdateInfo
{
    public Version Version { get; init; } = new(0, 0, 0);
    public string Tag { get; init; } = "";
    public string Notes { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string AssetName { get; init; } = "";

    /// <summary>
    /// Qué clase de paquete es. Lo lleva puesto para que la interfaz no tenga que volver a
    /// deducirlo: es lo que decide si se puede lanzar o solo dejar descargado.
    /// </summary>
    public Updater.Paquete Paquete { get; init; } = Updater.Paquete.InstaladorDeWindows;
}

/// <summary>Comprueba GitHub Releases, descarga el instalador nuevo y relanza para actualizar.</summary>
public static class Updater
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("Ondine-Updater");
        h.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return h;
    }

    public static Version Current
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
        }
    }

    public static string Repo
    {
        get
        {
            var meta = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "UpdateRepo");
            return string.IsNullOrWhiteSpace(meta?.Value) ? "luishidalgoa/ondine" : meta!.Value!;
        }
    }

    /// <summary>
    /// Resultado de comprobar si hay versión nueva. Distingue «estás al día» de «no se
    /// pudo comprobar»: antes ambos casos devolvían null y la app decía que estabas al
    /// día aunque en realidad no hubiera habido conexión.
    /// </summary>
    public sealed record CheckResult(UpdateInfo? Info, string? Error)
    {
        public bool Failed => Error != null;
        public bool Available => Info != null;
        public static CheckResult UpToDate() => new(null, null);
        public static CheckResult Fail(string e) => new(null, e);
        public static CheckResult Found(UpdateInfo i) => new(i, null);
    }

    /// <summary>Con qué se actualiza esta instalación de Ondine.</summary>
    public enum Paquete
    {
        /// <summary>El instalador de Windows. El único que se ejecuta y se encarga él.</summary>
        InstaladorDeWindows,
        /// <summary>El paquete de Debian, para quien instaló desde el <c>.deb</c>.</summary>
        DebDeLinux,
        /// <summary>El fichero único, para quien está corriendo un AppImage.</summary>
        AppImageDeLinux,
        /// <summary>La imagen de disco de macOS, la de su arquitectura.</summary>
        DmgDeMac,
    }

    /// <summary>
    /// ¿Es este fichero de la Release el paquete que le toca a esta instalación?
    ///
    /// <para>
    /// <b>Antes esto solo sabía de Windows</b> —«el <c>.exe</c> que lleva <i>setup</i> en el
    /// nombre»— y en Linux se bajaba tal cual ese instalador. El escritorio no tiene con qué
    /// abrir un <c>.exe</c>, así que se lo pasaba al gestor de archivadores, que respondía
    /// «se produjo un error cargando el archivador». Y detrás, la aplicación se cerraba.
    /// </para>
    /// <para>
    /// La trampa que ya estaba resuelta y hay que mantener: una Release trae <b>dos</b>
    /// ficheros por sistema, el de la aplicación y el de la herramienta de terminal. Coger el
    /// segundo «actualiza» con algo que no instala nada, y no falla — descarga bien y no pasa
    /// nada.
    /// </para>
    /// </summary>
    public static bool EsElPaquete(string nombre, Paquete paquete, string arquitectura) => paquete switch
    {
        // «setup» distingue el instalador del .exe de la terminal, que también es .exe.
        Paquete.InstaladorDeWindows =>
            nombre.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            nombre.Contains("setup", StringComparison.OrdinalIgnoreCase),

        Paquete.DebDeLinux => nombre.EndsWith(".deb", StringComparison.OrdinalIgnoreCase),

        Paquete.AppImageDeLinux => nombre.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase),

        // La arquitectura importa de verdad: bajarse el .dmg de Intel en un Mac con chip de
        // Apple deja una aplicación que no arranca, y al revés lo mismo.
        Paquete.DmgDeMac =>
            nombre.EndsWith(".dmg", StringComparison.OrdinalIgnoreCase) &&
            nombre.Contains("-" + arquitectura + ".", StringComparison.OrdinalIgnoreCase),

        _ => false,
    };

    /// <summary>
    /// ¿Puede este paquete instalarse él solo?
    ///
    /// <para>
    /// Solo el de Windows. Es la razón por la que <see cref="LaunchInstallerAndExit"/> cierra
    /// la aplicación: el instalador se queda trabajando y necesita que los ficheros no estén
    /// en uso. Un <c>.deb</c> pide permisos de administrador y un <c>.dmg</c> se monta y se
    /// arrastra: ninguno es «ejecutar y salir», y tratarlos así fue el fallo.
    /// </para>
    /// </summary>
    public static bool SeInstalaSolo(Paquete paquete) => paquete == Paquete.InstaladorDeWindows;

    /// <summary>
    /// Con qué se actualiza ESTA instalación, mirando dónde está.
    ///
    /// <para>
    /// En Linux hay dos formas de tener Ondine y se actualizan distinto. Se distinguen por
    /// una señal que no hay que adivinar: un AppImage <b>exporta la variable
    /// <c>APPIMAGE</c></b> con su propia ruta, y eso lo hace el propio formato. Sin ella, es
    /// la instalación del paquete.
    /// </para>
    /// </summary>
    public static Paquete PaqueteDeEstaInstalacion()
    {
        if (OperatingSystem.IsWindows()) return Paquete.InstaladorDeWindows;
        if (OperatingSystem.IsMacOS()) return Paquete.DmgDeMac;

        return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"))
            ? Paquete.DebDeLinux
            : Paquete.AppImageDeLinux;
    }

    /// <summary>La arquitectura, como la escriben los nombres de los paquetes.</summary>
    public static string ArquitecturaDeEstaMaquina() =>
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture
            == System.Runtime.InteropServices.Architecture.Arm64 ? "arm64" : "x64";

    /// <summary>Comprueba si hay versión nueva publicada.</summary>
    public static async Task<CheckResult> CheckAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{Repo}/releases/latest";
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            string clean = tag.TrimStart('v', 'V');
            if (!Version.TryParse(clean, out var latest))
                return CheckResult.Fail(string.Format(Textos.Instancia.MotorActualizacionVersionIlegible, tag));
            latest = new Version(latest.Major, latest.Minor, Math.Max(latest.Build, 0));
            if (latest <= Current) return CheckResult.UpToDate();

            // El paquete de ESTE sistema entre los adjuntos, que son diez y solo uno vale.
            var paquete = PaqueteDeEstaInstalacion();
            var arquitectura = ArquitecturaDeEstaMaquina();

            string dl = "", asset = "";
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var nm = a.GetProperty("name").GetString() ?? "";
                    if (EsElPaquete(nm, paquete, arquitectura))
                    {
                        asset = nm;
                        dl = a.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }
            }
            if (dl == "")
                return CheckResult.Fail(string.Format(Textos.Instancia.MotorActualizacionSinInstalador, tag));

            return CheckResult.Found(new UpdateInfo
            {
                Version = latest,
                Tag = tag,
                Notes = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "",
                DownloadUrl = dl,
                AssetName = asset,
                Paquete = paquete,
            });
        }
        catch (TaskCanceledException) { return CheckResult.Fail(Textos.Instancia.MotorActualizacionTiempoAgotado); }
        // El mensaje de la excepción lo escribe .NET y no se traduce: es diagnóstico.
        catch (HttpRequestException ex) { return CheckResult.Fail(string.Format(Textos.Instancia.MotorActualizacionSinConexion, ex.Message)); }
        catch (Exception ex) { return CheckResult.Fail(ex.Message); }
    }

    /// <summary>Descarga el instalador a una carpeta temporal y devuelve su ruta.</summary>
    public static async Task<string> DownloadAsync(UpdateInfo info, IProgress<double>? progress = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), "OndineUpdate");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, info.AssetName);

        using var resp = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        long? totalLen = resp.Content.Headers.ContentLength;
        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var dst = File.Create(dest);
        var buffer = new byte[81920];
        long read = 0; int r;
        while ((r = await src.ReadAsync(buffer)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, r));
            read += r;
            if (totalLen is > 0) progress?.Report((double)read / totalLen.Value);
        }
        return dest;
    }

    /// <summary>
    /// Cómo se cierra la app. Lo pone cada interfaz al arrancar, porque cerrarse es lo único
    /// de aquí que no es igual en las dos: WPF y Avalonia lo dicen distinto.
    ///
    /// <para>
    /// Era la ÚNICA línea de este fichero que hablaba de la interfaz, y por ella entero vivía
    /// en el proyecto de WPF — ciento cincuenta líneas de HTTP y comparación de versiones que
    /// no podía usar nadie más. Sacada de aquí, el resto se comparte.
    /// </para>
    /// </summary>
    public static Action? Cerrar { get; set; }

    /// <summary>
    /// Lanza el instalador descargado y cierra la app para que pueda actualizar.
    ///
    /// <para>
    /// <b>Solo para el instalador de Windows</b>, y ahora se comprueba. Antes lo llamaba
    /// cualquiera con lo que hubiera bajado: en Linux eso era el <c>.exe</c> de Windows, y
    /// <c>UseShellExecute</c> se lo daba al escritorio, que lo mandó al gestor de archivadores
    /// —«se produjo un error cargando el archivador»— y encima la aplicación se cerraba
    /// detrás. Lanzar algo que no se puede lanzar y cerrarse es lo peor de los dos mundos.
    /// </para>
    /// </summary>
    public static void LaunchInstallerAndExit(string installerPath, Paquete paquete)
    {
        if (!SeInstalaSolo(paquete))
            // El mensaje va sin acentos ni comillas latinas a proposito: es diagnostico para
            // quien programa, no texto de interfaz, y el trinquete de traduccion cuenta los
            // literales en castellano para cazar justo lo contrario. Que no cuente este.
            throw new InvalidOperationException(
                $"{paquete} does not install itself: leave it downloaded and tell the user what to do.");

        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        Cerrar?.Invoke();
    }
}
