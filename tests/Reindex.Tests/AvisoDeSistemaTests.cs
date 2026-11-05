using System.Text;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El aviso del sistema al terminar una tanda, y el borrado definitivo de un original.
///
/// <para>
/// <b>Lo que se prueba es el comando que se lanza, no que el aviso salga.</b> Que aparezca en
/// pantalla solo se ve en cada sistema, y eso queda dicho donde toca. Lo que sí se puede
/// comprobar desde cualquier sitio es que el texto de una película nunca acaba dentro del
/// comando: un nombre con comillas o con un punto y coma no puede romper la línea ni colar nada.
/// </para>
/// <para>
/// El borrado definitivo es lo contrario de la Papelera, así que se prueba su límite: borra
/// ficheros y nada más. Una carpeta, o algo que ya no existe, no se toca.
/// </para>
/// </summary>
public static class AvisoDeSistemaTests
{
    public static void Todas()
    {
        Program.Seccion("El aviso del sistema y el borrado definitivo");

        ElTextoNuncaVaEnElComando();
        LinuxYMacLlevanElTextoComoArgumento();
        WindowsLlevaElTextoEnElEntorno();
        BorradoDefinitivoSoloFicheros();
    }

    // Un título y un cuerpo con todo lo que rompe un comando: comillas, punto y coma, barras,
    // un acento invertido y un signo de dólar.
    private const string Titulo = "Compresión terminada";
    private const string Cuerpo = "Ocean's \"Eleven\"; rm -rf $HOME `id` \\ fin";

    private static void ElTextoNuncaVaEnElComando()
    {
        var linux = AvisoDeSistema.Armar("linux", Titulo, Cuerpo);
        var mac = AvisoDeSistema.Armar("macos", Titulo, Cuerpo);
        var win = AvisoDeSistema.Armar("windows", Titulo, Cuerpo);
        Program.Assert(linux is not null && mac is not null && win is not null,
            "hay comando para Windows, Linux y macOS");

        // Ningún nombre de archivo ni el propio texto entra en el nombre del programa.
        Program.Assert(!linux!.FileName.Contains("Eleven") && !mac!.FileName.Contains("Eleven"),
            "el nombre del programa no lleva el texto");

        // En macOS, el guion (las líneas -e) no contiene el texto; solo los dos últimos argumentos.
        var guionMac = mac!.ArgumentList.Take(mac.ArgumentList.Count - 2);
        Program.Assert(!guionMac.Any(a => a.Contains("Eleven") || a.Contains("rm -rf")),
            "el guion de macOS no contiene el texto");
    }

    private static void LinuxYMacLlevanElTextoComoArgumento()
    {
        var linux = AvisoDeSistema.Armar("linux", Titulo, Cuerpo)!;
        Program.Assert(linux.FileName == "notify-send", "en Linux se usa notify-send");
        Program.Assert(linux.ArgumentList.Contains(Titulo) && linux.ArgumentList.Contains(Cuerpo),
            "en Linux el título y el cuerpo llegan tal cual, cada uno como su argumento");
        Program.Assert(linux.ArgumentList.Contains("--"),
            "en Linux un «--» separa las opciones del texto, por si el texto empieza por guion");

        var mac = AvisoDeSistema.Armar("macos", Titulo, Cuerpo)!;
        Program.Assert(mac.FileName == "/usr/bin/osascript", "en macOS se usa osascript");
        Program.Assert(mac.ArgumentList[^2] == Titulo && mac.ArgumentList[^1] == Cuerpo,
            "en macOS el título y el cuerpo son los dos últimos argumentos, tal cual");
    }

    private static void WindowsLlevaElTextoEnElEntorno()
    {
        var win = AvisoDeSistema.Armar("windows", Titulo, Cuerpo)!;
        Program.Assert(win.FileName.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase),
            "en Windows se usa PowerShell, que trae el propio sistema");
        Program.Assert(win.Environment["ONDINE_AVISO_TITULO"] == Titulo
                    && win.Environment["ONDINE_AVISO_CUERPO"] == Cuerpo,
            "en Windows el texto viaja en variables de entorno, no en la línea de comandos");

        int i = win.ArgumentList.IndexOf("-EncodedCommand");
        Program.Assert(i >= 0 && i + 1 < win.ArgumentList.Count,
            "el guion va codificado en base64 tras -EncodedCommand");
        var guion = Encoding.Unicode.GetString(Convert.FromBase64String(win.ArgumentList[i + 1]));
        Program.Assert(!guion.Contains("Eleven") && !guion.Contains("rm -rf"),
            "y el guion decodificado no contiene el texto");
        Program.Assert(guion.Contains("ONDINE_AVISO_CUERPO"),
            "pero sí lee el cuerpo del entorno, que es de donde sale");
    }

    private static void BorradoDefinitivoSoloFicheros()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ondine-borrado-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var fichero = Path.Combine(dir, "original.mkv");
            File.WriteAllText(fichero, "x");

            Program.Assert(BorradoDefinitivo.Mandar(fichero), "un fichero se borra y se dice que sí");
            Program.Assert(!File.Exists(fichero), "y ya no existe");
            Program.Assert(!BorradoDefinitivo.Mandar(fichero), "uno que ya no existe no cuenta como borrado");
            Program.Assert(!BorradoDefinitivo.Mandar(dir), "una carpeta nunca se borra con esto");
            Program.Assert(Directory.Exists(dir), "y la carpeta sigue en su sitio");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
