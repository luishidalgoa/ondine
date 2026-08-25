namespace Ondine.Reindex.Tests;

/// <summary>
/// Dónde buscar libvlc en cada sistema, y qué decir si no está.
///
/// <para>
/// En Windows va dentro de la app. En Linux la trae el sistema y el cargador la encuentra
/// sola. <b>En macOS no pasa ni una cosa ni la otra:</b> un Mac no trae libvlc en ninguna
/// ruta estándar, y la que hay —la de VLC.app— vive dentro de un paquete de aplicación,
/// donde nadie mira por defecto.
/// </para>
/// <para>
/// El paquete de VideoLAN para Mac no sirve: es de 2018, solo Intel y sin los
/// decodificadores. Está contado en el <c>.csproj</c>. Así que hay que ir a buscarla, y esto
/// prueba las dos mitades de eso: por dónde se busca, y qué se le dice a quien no la tenga.
/// </para>
/// </summary>
public static class RutaDeLibVlcTests
{
    public static void Todas()
    {
        Program.Seccion("Dónde vive libvlc en un Mac");

        LaDeVlcAppPrimero();
        LaDeHomebrewTambien();
        SinNadaInstaladoNoSeInventaUnaRuta();
        ElAvisoHablaDelSistemaEnQueEstas();
    }

    /// <summary>
    /// VLC.app antes que Homebrew, y por un motivo: es la que VideoLAN publica universal y
    /// al día, y la que va a tener quien ya vea vídeo en su Mac.
    /// </summary>
    private static void LaDeVlcAppPrimero()
    {
        var r = RutaDeLibVlc.EnMac("/Users/x", ruta => true);
        Program.Assert(r == "/Applications/VLC.app/Contents/MacOS/lib",
            $"con todo instalado, la de VLC.app gana ({r})");
    }

    private static void LaDeHomebrewTambien()
    {
        // Solo Homebrew en un Mac con chip de Apple.
        var r = RutaDeLibVlc.EnMac("/Users/x", ruta => ruta == "/opt/homebrew/lib");
        Program.Assert(r == "/opt/homebrew/lib",
            $"sin VLC.app, se acepta la de Homebrew ({r})");

        // Y la carpeta de Homebrew en un Mac Intel, que no es la misma.
        var intel = RutaDeLibVlc.EnMac("/Users/x", ruta => ruta == "/usr/local/lib");
        Program.Assert(intel == "/usr/local/lib",
            $"y la de los Mac Intel, que Homebrew pone en otro sitio ({intel})");

        // La de la carpeta personal: quien no puede escribir en /Applications.
        var suya = RutaDeLibVlc.EnMac("/Users/x",
            ruta => ruta == "/Users/x/Applications/VLC.app/Contents/MacOS/lib");
        Program.Assert(suya is not null,
            "y VLC instalado en la carpeta del usuario, que es lo que pasa sin permisos de administrador");
    }

    /// <summary>
    /// Lo importante: si no hay ninguna, se devuelve nada. Devolver una ruta que no existe
    /// haría que el reproductor fallara <b>más adelante</b> con un mensaje sobre una
    /// biblioteca que no carga, en vez de decir aquí mismo que hay que instalar VLC.
    /// </summary>
    private static void SinNadaInstaladoNoSeInventaUnaRuta()
    {
        var r = RutaDeLibVlc.EnMac("/Users/x", ruta => false);
        Program.Assert(r is null,
            "sin VLC por ningún lado no se devuelve una ruta a lo que no está");
    }

    /// <summary>
    /// El aviso de que falta. Decía «sudo apt install vlc» en todos los sistemas: en un Mac
    /// eso es una orden que no existe, y quien la lea se queda sin saber qué hacer — que es
    /// justo lo que este mensaje venía a evitar.
    /// </summary>
    private static void ElAvisoHablaDelSistemaEnQueEstas()
    {
        var enLinux = Ondine.Localizacion.Textos.Instancia.FaltaLibVlcEn(mac: false);
        Program.Assert(enLinux.Contains("apt install vlc"),
            "en Linux se dice la orden de apt, que se teclea y ya está");

        var enMac = Ondine.Localizacion.Textos.Instancia.FaltaLibVlcEn(mac: true);
        Program.Assert(!enMac.Contains("apt install"),
            "en macOS NO se manda a apt, que ahí no existe");
        Program.Assert(enMac.Contains("brew install") || enMac.Contains("videolan.org"),
            "sino a instalar VLC como se instala en un Mac");
    }
}
