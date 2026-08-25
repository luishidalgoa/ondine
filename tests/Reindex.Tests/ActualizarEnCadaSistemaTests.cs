namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué se baja el actualizador en cada sistema, y quién puede instalarse solo.
///
/// <para>
/// <b>Esto se vio en pantalla y no era bonito.</b> En Linux Mint, «buscar actualizaciones» se
/// bajó <c>Ondine-Setup-1.14.1.exe</c> —el instalador de Windows— y lo abrió con
/// <c>UseShellExecute</c>. El escritorio no tiene con qué abrir un <c>.exe</c>, así que se lo
/// pasó al <b>gestor de archivadores</b>, que respondió «se produjo un error cargando el
/// archivador». Y detrás de eso, la aplicación se cerraba: el actualizador da por hecho que
/// tras lanzar el instalador hay que salir.
/// </para>
/// <para>
/// El actualizador se escribió cuando Ondine solo era de Windows y buscaba «el <c>.exe</c> que
/// lleva <i>setup</i> en el nombre». Eso sigue siendo correcto <b>en Windows</b>. En los otros
/// dos hay que bajar otra cosa y, sobre todo, <b>no lanzarla</b>: un <c>.deb</c> se instala con
/// permisos de administrador y un <c>.dmg</c> se monta y se arrastra. Ninguno de los dos es
/// «ejecutar y salir».
/// </para>
/// </summary>
public static class ActualizarEnCadaSistemaTests
{
    /// <summary>Los diez ficheros que trae una Release, tal cual se llaman.</summary>
    private static readonly string[] LosDiez =
    [
        "Ondine-Setup-1.14.1.exe",
        "ondine-windows-x64.exe",
        "ondine_1.14.1_amd64.deb",
        "Ondine-1.14.1-linux-x86_64.AppImage",
        "Ondine-1.14.1-macos-arm64.dmg",
        "Ondine-1.14.1-macos-x64.dmg",
        "ondine-linux-x64.tar.gz",
        "ondine-linux-arm64.tar.gz",
        "ondine-macos-arm64.tar.gz",
        "ondine-macos-x64.tar.gz",
    ];

    public static void Todas()
    {
        Program.Seccion("Qué se actualiza en cada sistema");

        CadaSistemaSeBajaLoSuyo();
        NoSeConfundeConElDeLaTerminal();
        SoloWindowsSeInstalaSolo();
        SinPaqueteParaTiNoSeInventaUno();
    }

    private static string? Elegido(Updater.Paquete paquete, string arquitectura = "x64") =>
        LosDiez.FirstOrDefault(n => Updater.EsElPaquete(n, paquete, arquitectura));

    private static void CadaSistemaSeBajaLoSuyo()
    {
        Program.Assert(Elegido(Updater.Paquete.InstaladorDeWindows) == "Ondine-Setup-1.14.1.exe",
            "en Windows, el instalador");

        Program.Assert(Elegido(Updater.Paquete.DebDeLinux) == "ondine_1.14.1_amd64.deb",
            "instalado desde el .deb, el .deb nuevo");

        Program.Assert(Elegido(Updater.Paquete.AppImageDeLinux) == "Ondine-1.14.1-linux-x86_64.AppImage",
            "corriendo como AppImage, el AppImage nuevo");

        Program.Assert(Elegido(Updater.Paquete.DmgDeMac, "arm64") == "Ondine-1.14.1-macos-arm64.dmg",
            "en un Mac con chip de Apple, su .dmg");
        Program.Assert(Elegido(Updater.Paquete.DmgDeMac, "x64") == "Ondine-1.14.1-macos-x64.dmg",
            "y en un Mac Intel, el otro — bajarse el que no es deja una app que no arranca");
    }

    /// <summary>
    /// La trampa que el actualizador ya tenía resuelta para Windows y que hay que mantener en
    /// los demás: una Release trae <b>dos</b> ficheros por sistema, el de la aplicación y el
    /// de la herramienta de terminal. Bajarse el segundo «actualiza» con algo que no instala
    /// nada, y encima no falla: descarga bien y no pasa nada.
    /// </summary>
    private static void NoSeConfundeConElDeLaTerminal()
    {
        Program.Assert(!Updater.EsElPaquete("ondine-windows-x64.exe", Updater.Paquete.InstaladorDeWindows, "x64"),
            "el .exe de la terminal NO es el instalador, aunque los dos sean .exe");

        Program.Assert(!Updater.EsElPaquete("ondine-linux-x64.tar.gz", Updater.Paquete.DebDeLinux, "x64"),
            "ni el .tar.gz de Linux es el paquete de la aplicación");

        Program.Assert(!Updater.EsElPaquete("ondine-macos-arm64.tar.gz", Updater.Paquete.DmgDeMac, "arm64"),
            "ni el de macOS");
    }

    /// <summary>
    /// El fallo de verdad. Windows sí: el instalador se ejecuta, hace su trabajo y por eso la
    /// aplicación se cierra antes. Los otros dos <b>no</b>, y lanzarlos es lo que acabó en el
    /// gestor de archivadores enseñando un error.
    /// </summary>
    private static void SoloWindowsSeInstalaSolo()
    {
        Program.Assert(Updater.SeInstalaSolo(Updater.Paquete.InstaladorDeWindows),
            "el instalador de Windows se ejecuta y se encarga: por eso la app se cierra");

        foreach (var p in new[] { Updater.Paquete.DebDeLinux, Updater.Paquete.AppImageDeLinux,
                                 Updater.Paquete.DmgDeMac })
            Program.Assert(!Updater.SeInstalaSolo(p),
                $"«{p}» NO se lanza: se deja descargado y se dice qué hacer con él");
    }

    /// <summary>
    /// Si en la Release no hay nada para tu sistema, se devuelve nada. Coger «lo primero que
    /// se parezca» es cómo se llegó a bajar un ejecutable de Windows en un Linux.
    /// </summary>
    private static void SinPaqueteParaTiNoSeInventaUno()
    {
        string[] soloWindows = ["Ondine-Setup-1.14.1.exe", "ondine-windows-x64.exe"];

        Program.Assert(!soloWindows.Any(n => Updater.EsElPaquete(n, Updater.Paquete.DebDeLinux, "x64")),
            "en una Release sin paquete de Linux no se acepta el de Windows");
        Program.Assert(!soloWindows.Any(n => Updater.EsElPaquete(n, Updater.Paquete.DmgDeMac, "arm64")),
            "ni en un Mac");
    }
}
