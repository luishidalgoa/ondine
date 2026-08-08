using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// De qué nube es cada fichero, sin saber nada de ninguna nube en concreto.
///
/// <para>
/// Medido: abrir un fichero de 65 MB que solo está en OneDrive y leer <b>un
/// mega</b> bloquea más de cinco minutos y no termina. Windows recupera el
/// fichero entero al abrirlo — no hay forma de asomarse a él—, así que
/// reproducir para verificar sale carísimo y encima llena el disco.
/// </para>
/// <para>
/// La salida es no abrirlo: decir de qué nube es y dejar que se vea en su web,
/// que es lo que ya ofrecen todos —«Ver en línea» en OneDrive, «Abrir en
/// navegador» en Nextcloud—. Y para eso solo hace falta saber <b>qué raíz de
/// sincronización posee el fichero</b>, que Windows publica para cualquier
/// proveedor que use su API de nube.
/// </para>
/// </summary>
public static class NubeSincronizadaTests
{
    private static Nube.Sincronizacion S(string proveedor, string raiz) => new(proveedor, raiz);

    public static void Todas()
    {
        Program.Seccion("De qué nube es este fichero");

        // Rutas de Windows comparadas con las reglas de Windows: «C:\…» y la barra
        // invertida como separador. En Linux —que es donde corre el arnés en la
        // integración continua— «C:\Nube\Otra\x.mkv» no es una ruta absoluta sino
        // UN NOMBRE de fichero con barras dentro, así que la comparación no falla
        // por estar mal, falla por no significar nada allí.
        //
        // Se salta en vez de traducirse a rutas neutras porque lo que se prueba es
        // precisamente el trato de las rutas de Windows: la API de nube que hay
        // detrás no existe en ningún otro sitio. Y se dice en voz alta, que un
        // salto silencioso se lee como cobertura.
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("  · saltado: son rutas de Windows y esto no es Windows");
            return;
        }

        var raices = new[]
        {
            S("OneDrive - Personal", @"C:\Users\luis\OneDrive"),
            S("Nextcloud", @"C:\Users\luis\Nextcloud"),
        };

        Program.Eq("OneDrive - Personal",
            Nube.Duena(@"C:\Users\luis\OneDrive\Plex\Anime\cap.mkv", raices)?.Proveedor,
            "un fichero dentro de OneDrive es de OneDrive");
        Program.Eq("Nextcloud",
            Nube.Duena(@"C:\Users\luis\Nextcloud\fotos\a.jpg", raices)?.Proveedor,
            "y uno dentro de Nextcloud, de Nextcloud");

        Program.Eq(null, Nube.Duena(@"D:\Videos\cap.mkv", raices),
            "uno de fuera no es de ninguna");

        // EL FALLO CLÁSICO: comparar el principio de la ruta sin mirar dónde acaba
        // la carpeta. «OneDriveViejo» empieza por «OneDrive» y no está dentro.
        Program.Eq(null, Nube.Duena(@"C:\Users\luis\OneDriveViejo\cap.mkv", raices),
            "una carpeta que solo EMPIEZA igual no cuenta");

        // Windows no distingue mayúsculas en las rutas, y el registro las escribe
        // como le parece.
        Program.Eq("OneDrive - Personal",
            Nube.Duena(@"c:\users\luis\onedrive\plex\cap.mkv", raices)?.Proveedor,
            "las mayúsculas dan igual");

        // Nubes anidadas: gana la MÁS específica. Si no, un fichero de la de dentro
        // se atribuiría a la de fuera y se abriría la web equivocada.
        var anidadas = new[]
        {
            S("Fuera", @"C:\Nube"),
            S("Dentro", @"C:\Nube\Otra"),
        };
        Program.Eq("Dentro", Nube.Duena(@"C:\Nube\Otra\x.mkv", anidadas)?.Proveedor,
            "manda la raíz más específica");
        Program.Eq("Fuera", Nube.Duena(@"C:\Nube\x.mkv", anidadas)?.Proveedor,
            "y fuera de ella, la de arriba");

        // Sin raíces registradas no se afirma nada, y no revienta.
        Program.Eq(null, Nube.Duena(@"C:\lo que sea\x.mkv", Array.Empty<Nube.Sincronizacion>()),
            "sin nubes registradas, ninguna");
        Program.Eq(null, Nube.Duena("", raices), "ni con una ruta vacía");
    }
}
