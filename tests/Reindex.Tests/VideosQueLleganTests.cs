using Ondine.Rutas;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Lo que llega de fuera —soltado con el ratón, por «Enviar a» o como argumento— convertido
/// en una lista de vídeos.
///
/// <para>
/// Vivía dentro de <c>ShellIntegration</c>, la clase que escribe en el registro de Windows.
/// Ahí no se podía probar ni usar desde Linux, y no tiene nada de Windows: son carpetas y
/// extensiones. Sale al motor para que las dos interfaces suelten igual.
/// </para>
/// </summary>
public static class VideosQueLleganTests
{
    public static void Todas()
    {
        Program.Seccion("Lo que se suelta en la ventana");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-soltar-prueba");
        try
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, true);
            Directory.CreateDirectory(Path.Combine(raiz, "serie", "temporada 1"));

            Toca(raiz, "suelto.mkv");
            Toca(raiz, "apuntes.txt");
            Toca(raiz, "serie", "cap1.mp4");
            Toca(raiz, "serie", "portada.jpg");
            Toca(raiz, "serie", "temporada 1", "cap2.mkv");

            SoloVideos(raiz);
            LasCarpetasSeRecorrenSoloSiSePide(raiz);
            NiRepetidosNiDesordenados(raiz);
            LoQueNoExisteNoRompe(raiz);
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    private static void Toca(params string[] partes)
    {
        var p = Path.Combine(partes);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, "x");
    }

    private static void SoloVideos(string raiz)
    {
        var r = VideosQueLlegan.Expandir(
            [Path.Combine(raiz, "suelto.mkv"), Path.Combine(raiz, "apuntes.txt")], recursivo: false);

        Program.Assert(r.Count == 1, "de un vídeo y un texto sueltos, entra solo el vídeo");
        Program.Assert(r[0].EndsWith("suelto.mkv"), "y es el que era");
    }

    /// <summary>
    /// La casilla «Subcarpetas» de la ventana manda también aquí. Sin esto, soltar la carpeta
    /// de una serie entera metería solo lo del primer nivel y el usuario no vería por qué
    /// faltan capítulos.
    /// </summary>
    private static void LasCarpetasSeRecorrenSoloSiSePide(string raiz)
    {
        var carpeta = Path.Combine(raiz, "serie");

        var plano = VideosQueLlegan.Expandir([carpeta], recursivo: false);
        Program.Assert(plano.Count == 1, "una carpeta suelta trae sus vídeos, no sus imágenes");

        var hondo = VideosQueLlegan.Expandir([carpeta], recursivo: true);
        Program.Assert(hondo.Count == 2, "y con subcarpetas puestas, también los de dentro");
    }

    private static void NiRepetidosNiDesordenados(string raiz)
    {
        var uno = Path.Combine(raiz, "serie", "cap1.mp4");
        var r = VideosQueLlegan.Expandir([Path.Combine(raiz, "serie"), uno, uno], recursivo: true);

        Program.Assert(r.Count == 2,
            "soltar una carpeta Y un fichero de dentro no lo mete dos veces");
        Program.Assert(r.SequenceEqual(r.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
            "y lo que sale viene ordenado, no en el orden en que el sistema los fue encontrando");
    }

    /// <summary>
    /// Soltar algo inaccesible —una unidad de red caída, una carpeta sin permiso— no puede
    /// tirar la ventana: se ignora lo que no se puede leer y entra lo demás.
    /// </summary>
    private static void LoQueNoExisteNoRompe(string raiz)
    {
        var r = VideosQueLlegan.Expandir(
            [Path.Combine(raiz, "no-existe", "x.mkv"), "", Path.Combine(raiz, "suelto.mkv")],
            recursivo: false);

        Program.Assert(r.Count == 1, "lo que no se puede leer se ignora y lo demás entra igual");
    }
}
