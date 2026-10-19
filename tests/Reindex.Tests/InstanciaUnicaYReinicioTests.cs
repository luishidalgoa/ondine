using System.Diagnostics;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Una sola Ondine abierta, y volver a abrirla tras actualizar en Linux.
///
/// <para>
/// Lo que no puede fallar: que la segunda se entere de que hay otra, que el candado se suelte
/// al acabar la primera (si no, tras cerrarla no se podría abrir ninguna), y que el ayudante
/// del reinicio reciba la ruta como dato y no como parte de una orden de shell.
/// </para>
/// </summary>
public static class InstanciaUnicaYReinicioTests
{
    public static void Todas()
    {
        Program.Seccion("Instancia única y reinicio tras actualizar");

        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-inst-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Candado(carpeta);
            Aviso(carpeta);
        }
        finally { try { Directory.Delete(carpeta, true); } catch { } }

        Ayudante();
        VersionInstalada();
        ReemplazoDelAppImage();
    }

    private static void Candado(string carpeta)
    {
        var ruta = Path.Combine(carpeta, "instancia.lock");
        var primera = InstanciaUnica.Tomar(ruta, "Ondine.Test.a");
        Program.Assert(primera is not null, "la primera se queda con el candado");

        var segunda = InstanciaUnica.Tomar(ruta, "Ondine.Test.a");
        Program.Assert(segunda is null, "la segunda se entera de que hay otra");

        primera!.Dispose();
        var tercera = InstanciaUnica.Tomar(ruta, "Ondine.Test.a");
        Program.Assert(tercera is not null, "al cerrarse la primera el candado se suelta: se puede volver a abrir");
        tercera!.Dispose();
    }

    private static void Aviso(string carpeta)
    {
        var tuberia = "Ondine.Test.b." + Guid.NewGuid().ToString("N")[..6];
        using var primera = InstanciaUnica.Tomar(Path.Combine(carpeta, "b.lock"), tuberia)!;

        var recibidos = new List<IReadOnlyList<string>>();
        var llegado = new ManualResetEventSlim();
        primera.Escuchar(f => { lock (recibidos) { recibidos.Add(f); } llegado.Set(); });

        Program.Assert(InstanciaUnica.Avisar(["/mi disco/serie uno.mkv", "/otro/dos.mp4"], TimeSpan.FromSeconds(5), tuberia),
            "la segunda consigue avisar a la primera");
        Program.Assert(llegado.Wait(TimeSpan.FromSeconds(5)), "y la primera lo recibe");
        lock (recibidos)
            Program.Assert(recibidos[0].SequenceEqual(["/mi disco/serie uno.mkv", "/otro/dos.mp4"]),
                "con los ficheros tal cual, también las rutas con espacios");

        llegado.Reset();
        InstanciaUnica.Avisar([], TimeSpan.FromSeconds(5), tuberia);
        Program.Assert(llegado.Wait(TimeSpan.FromSeconds(5)), "un aviso sin ficheros también llega: sirve para sacarla al frente");

        Program.Assert(!InstanciaUnica.Avisar([], TimeSpan.FromMilliseconds(300), "Ondine.Test.nadie." + Guid.NewGuid().ToString("N")[..6]),
            "avisar a una que no existe no se queda colgado: devuelve que no llegó");
    }

    private static void Ayudante()
    {
        var a = Reinicio.ArgumentosDelAyudante(1234, "/opt/ondine/Ondine.Avalonia");
        Program.Eq("-c", a[0], "el ayudante se lanza con «sh -c»");
        Program.Assert(a[1].Contains("kill -0 \"$1\"") && a[1].Contains("exec \"$2\""),
            "espera al proceso por su pid y luego ejecuta la aplicación");
        Program.Eq("1234", a[3], "el pid viaja como parámetro");

        // Una ruta con comillas, espacios y «$»: tiene que seguir siendo UN dato.
        var rara = "/tmp/mi app; rm -rf ~ \"$HOME\"/Ondine";
        var b = Reinicio.ArgumentosDelAyudante(1, rara);
        Program.Eq(rara, b[^1], "una ruta con comillas, espacios o «;» va entera como dato");
        Program.Assert(!b[1].Contains("rm -rf"), "y nunca se escribe dentro del texto del guion");

        Program.Eq("/opt/ondine/Ondine.Avalonia",
            Reinicio.SinMarcaDeBorrado("/opt/ondine/Ondine.Avalonia (deleted)"),
            "el ejecutable que el instalador acaba de reemplazar pierde la marca « (deleted)»");
        Program.Eq("/opt/ondine/Ondine.Avalonia",
            Reinicio.SinMarcaDeBorrado("/opt/ondine/Ondine.Avalonia"), "y uno normal se queda como está");
    }

    private static void VersionInstalada()
    {
        var aqui = Updater.VersionInstalada(AppContext.BaseDirectory);
        Program.Eq(Updater.Current, aqui, "la versión instalada en la carpeta de la app es la que corre");
        Program.Assert(Updater.VersionInstalada(Path.Combine(Path.GetTempPath(), "no-existe-ondine")) is null,
            "en una carpeta sin Ondine no hay versión");
    }

    private static void ReemplazoDelAppImage()
    {
        var d = Path.Combine(Path.GetTempPath(), "ondine-appimage-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(d);
        try
        {
            var viejo = Path.Combine(d, "Ondine.AppImage");
            File.WriteAllBytes(viejo, [1, 2, 3]);

            byte[] Elf(int bytes) { var b = new byte[bytes]; b[0] = 0x7F; b[1] = (byte)'E'; b[2] = (byte)'L'; b[3] = (byte)'F'; return b; }

            var bueno = Path.Combine(d, "bajado.AppImage");
            File.WriteAllBytes(bueno, Elf(2_000_000));
            Program.Assert(Updater.ReemplazarAppImage(bueno, viejo), "un AppImage válido sustituye al que está en uso");
            Program.Eq(2_000_000L, new FileInfo(viejo).Length, "y el contenido es el nuevo");
            if (!OperatingSystem.IsWindows())
                Program.Assert((File.GetUnixFileMode(viejo) & UnixFileMode.UserExecute) != 0, "con permiso de ejecución");
            Program.Assert(!File.Exists(viejo + ".nuevo"), "sin dejar ficheros sueltos");

            var antes = new FileInfo(viejo).Length;
            var pequeno = Path.Combine(d, "pequeno");
            File.WriteAllBytes(pequeno, Elf(100));
            Program.Assert(!Updater.ReemplazarAppImage(pequeno, viejo), "uno diminuto (descarga cortada) no se acepta");

            var html = Path.Combine(d, "error.html");
            File.WriteAllBytes(html, new byte[2_000_000]);
            Program.Assert(!Updater.ReemplazarAppImage(html, viejo), "algo que no es un ejecutable (una página de error) no se acepta");
            Program.Eq(antes, new FileInfo(viejo).Length, "y lo que funcionaba se queda como estaba");

            Program.Assert(!Updater.ReemplazarAppImage(bueno, ""), "sin saber cuál es el AppImage en uso, no se toca nada");
        }
        finally { try { Directory.Delete(d, true); } catch { } }
    }
}
