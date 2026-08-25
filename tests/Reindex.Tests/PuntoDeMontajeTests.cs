namespace Ondine.Reindex.Tests;

/// <summary>
/// De qué volumen es una ruta, que en Linux y macOS no es lo que parece.
///
/// <para>
/// <b>El problema:</b> <c>Path.GetPathRoot()</c> devuelve la letra de unidad en Windows
/// —«E:\»— pero en Linux y macOS devuelve <b>«/» para cualquier ruta absoluta</b>. Así que dos
/// rutas de discos completamente distintos —<c>/home/luis/Descargas</c> y
/// <c>/mnt/nas/Plex</c>— salen como el mismo volumen, y la raíz sale como el volumen de todo.
/// </para>
/// <para>
/// Eso rompía dos cosas del camino de trabajo, las dos silenciosas:
/// </para>
/// <list type="number">
/// <item>
/// <b>El guardián de «disco lleno» medía la partición raíz</b> en vez del disco de destino. Con
/// la raíz llena y el destino vacío, la compresión se queda esperando para siempre a que se
/// libere un sitio que no necesita; al revés, escribe hasta llenar el disco de destino sin
/// avisar.
/// </item>
/// <item>
/// <b>El aviso de «esto cruza de disco» antes de reordenar no salía nunca.</b> Justo el aviso
/// que importa cuando la biblioteca está en un NAS o en un USB, que es lo normal — y ahí mover
/// no es renombrar: es copiar el fichero entero.
/// </item>
/// </list>
/// <para>
/// La función toma la lista de montajes como argumento para poder comprobar el reparto desde
/// cualquier sistema. Lo que en la app se le pasa es lo que dice el sistema.
/// </para>
/// </summary>
public static class PuntoDeMontajeTests
{
    /// <summary>Un Linux normal: raíz, la casa en su disco, un NAS y un USB.</summary>
    private static readonly string[] ComoUnLinux =
        ["/", "/home", "/mnt/nas", "/media/luis/USB", "/boot/efi"];

    public static void Todas()
    {
        Program.Seccion("De qué volumen es una ruta");

        GanaElMontajeMasLargo();
        DosDiscosDistintosNoSonElMismo();
        LaRaizNoSeComeATodos();
        SinMontajesQueEncajenNoSeMiente();
    }

    /// <summary>
    /// El montaje más específico gana. <c>/media/luis/USB/pelis</c> es del USB, no de
    /// <c>/media</c> ni de <c>/</c>, aunque las tres sean prefijos suyos.
    /// </summary>
    private static void GanaElMontajeMasLargo()
    {
        Program.Assert(
            PuntoDeMontaje.De("/media/luis/USB/pelis/x.mkv", ComoUnLinux) == "/media/luis/USB",
            "una ruta del USB es del USB, no de la raíz");
        Program.Assert(
            PuntoDeMontaje.De("/home/luis/Descargas/x.mkv", ComoUnLinux) == "/home",
            "y una de la carpeta personal, de su disco");
        Program.Assert(
            PuntoDeMontaje.De("/mnt/nas/Plex/Series/x.mkv", ComoUnLinux) == "/mnt/nas",
            "y una del NAS, del NAS");
    }

    /// <summary>
    /// Lo que arregla el aviso de reordenar: dos rutas de discos distintos tienen que salir
    /// distintas. Con <c>GetPathRoot</c> las dos daban «/».
    /// </summary>
    private static void DosDiscosDistintosNoSonElMismo()
    {
        var casa = PuntoDeMontaje.De("/home/luis/Descargas/cap01.mkv", ComoUnLinux);
        var nas = PuntoDeMontaje.De("/mnt/nas/Plex/cap01.mkv", ComoUnLinux);

        Program.Assert(casa != nas,
            $"la carpeta personal y el NAS son volúmenes distintos ({casa} ≠ {nas})");

        // Y dos del mismo, iguales: el aviso tiene que saltar solo cuando toca.
        Program.Assert(
            PuntoDeMontaje.De("/mnt/nas/a/x.mkv", ComoUnLinux)
            == PuntoDeMontaje.De("/mnt/nas/b/y.mkv", ComoUnLinux),
            "y dos carpetas del mismo NAS, el mismo volumen");
    }

    /// <summary>
    /// La raíz solo se lleva lo que no es de nadie más. Es el error que se está corrigiendo:
    /// antes se llevaba todo.
    /// </summary>
    private static void LaRaizNoSeComeATodos()
    {
        Program.Assert(PuntoDeMontaje.De("/opt/ondine/x", ComoUnLinux) == "/",
            "lo que no cae en otro montaje es de la raíz");

        // El caso que engaña: «/homeless» empieza por «/home» como texto, y no está en él.
        Program.Assert(PuntoDeMontaje.De("/homeless/x.mkv", ComoUnLinux) == "/",
            "«/homeless» no es de «/home»: el montaje se compara por carpetas, no por letras");
    }

    /// <summary>
    /// Sin montajes que encajen —una lista vacía, una ruta relativa— se devuelve nada, y quien
    /// pregunta decide. Es lo que sostiene el «ante la duda, el mismo volumen» del aviso: más
    /// vale no avisar que avisar en falso.
    /// </summary>
    private static void SinMontajesQueEncajenNoSeMiente()
    {
        Program.Assert(PuntoDeMontaje.De("/home/luis/x.mkv", []) is null,
            "sin montajes no se inventa uno");
        Program.Assert(PuntoDeMontaje.De("", ComoUnLinux) is null,
            "y una ruta vacía no es de ningún volumen");
    }
}
