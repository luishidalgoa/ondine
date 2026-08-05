using System.IO.Compression;
using Ondine.Complementos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El índice de complementos y su instalación.
///
/// <para>
/// Esto baja ejecutables de internet y los deja donde la aplicación los va a
/// correr, así que lo único que importa aquí es lo que NO deja pasar. Un gestor
/// de complementos se juzga por sus rechazos.
/// </para>
/// </summary>
public static class IndiceTests
{
    private static byte[] Zip(params (string Ruta, string Contenido)[] entradas)
    {
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
            foreach (var (ruta, contenido) in entradas)
            {
                var e = z.CreateEntry(ruta);
                using var w = new StreamWriter(e.Open());
                w.Write(contenido);
            }
        return ms.ToArray();
    }

    private const string ManifiestoBueno =
        """{"nombre":"Demo","ejecutable":"demo.cmd","capacidades":["importar"],"contrato":1}""";

    public static void Todas()
    {
        Program.Seccion("El índice de complementos");

        var paquete = Zip(("plugin.json", ManifiestoBueno), ("demo.cmd", "@echo off"));
        var huella = Indice.Huella(paquete);

        Indice.Entrada E(Action<Indice.Entrada> ajustar)
        {
            var e = new Indice.Entrada
            {
                Id = "demo",
                Nombre = "Demo",
                Paquete = "https://ejemplo.invalid/demo.zip",
                Sha256 = huella,
            };
            ajustar(e);
            return e;
        }

        Program.Assert(E(_ => { }).Reparo() is null, "una entrada bien formada se acepta");

        Program.Assert(E(e => e.Paquete = "http://ejemplo.invalid/demo.zip").Reparo() is not null,
            "por HTTP no: quien esté en medio puede cambiar el índice, no solo el paquete");

        Program.Assert(E(e => e.Sha256 = "").Reparo() is not null,
            "sin sha256 no se instala nada, aunque hoy el índice sea de un solo autor");
        Program.Assert(E(e => e.Sha256 = "abc").Reparo() is not null, "ni con uno que no lo es");

        Program.Assert(E(e => e.Id = "../otro").Reparo() is not null,
            "un identificador que es una ruta instalaría fuera de donde debe");
        Program.Assert(E(e => e.Id = "carpeta/dentro").Reparo() is not null, "con separador tampoco");

        Program.Assert(Indice.Cuadra(paquete, huella), "el checksum del paquete cuadra consigo mismo");
        Program.Assert(Indice.Cuadra(paquete, huella.ToUpperInvariant()),
            "y da igual en mayúsculas: el hexadecimal se escribe de las dos formas");
        Program.Assert(!Indice.Cuadra(paquete, new string('0', 64)), "y no cuadra con otro");

        var baseDir = Path.Combine(Path.GetTempPath(), "ondine-inst-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(baseDir);

            var ok = Instalador.Instalar(E(_ => { }), paquete, baseDir);
            Program.Assert(ok.Ok, "un paquete correcto se instala");
            Program.Assert(File.Exists(Path.Combine(baseDir, "demo", "plugin.json")),
                "y queda en la carpeta que dice su identificador");

            var cambiado = Zip(("plugin.json", ManifiestoBueno), ("demo.cmd", "@echo OTRA COSA"));
            Program.Assert(!Instalador.Instalar(E(_ => { }), cambiado, baseDir).Ok,
                "un paquete que no es el que prometía el índice no se instala");

            // La entrada de un zip puede decir «../algo». Combinarla sin mirar
            // escribe fuera del destino: es la forma clásica de que un paquete
            // deje cosas donde no debe.
            var escapa = Zip(("../fuera.txt", "no deberia estar aqui"), ("plugin.json", ManifiestoBueno));
            Program.Assert(!Instalador.Instalar(E(e => e.Sha256 = Indice.Huella(escapa)), escapa, baseDir).Ok,
                "un paquete que escribe fuera de su carpeta se rechaza");
            Program.Assert(!File.Exists(Path.Combine(baseDir, "fuera.txt")),
                "y no deja nada suelto: ni el fichero que intentaba colar");

            // Venir del índice no da permisos extra: el manifiesto se valida
            // igual que uno puesto a mano.
            var malo = Zip(("plugin.json",
                """{"nombre":"X","ejecutable":"../../cmd.exe","capacidades":["importar"],"contrato":1}"""));
            Program.Assert(!Instalador.Instalar(
                E(e => { e.Id = "malo"; e.Sha256 = Indice.Huella(malo); }), malo, baseDir).Ok,
                "un manifiesto que apunta fuera se rechaza aunque venga del índice");
            Program.Assert(!Directory.Exists(Path.Combine(baseDir, "malo")),
                "y no queda media instalación tirada");

            var sinManifiesto = Zip(("algo.txt", "hola"));
            Program.Assert(!Instalador.Instalar(
                E(e => { e.Id = "sinman"; e.Sha256 = Indice.Huella(sinManifiesto); }), sinManifiesto, baseDir).Ok,
                "un paquete sin plugin.json no es un complemento");

            // Media instalación encima de algo que funcionaba es peor que no
            // haber intentado nada.
            Program.Assert(File.Exists(Path.Combine(baseDir, "demo", "plugin.json")),
                "los intentos fallidos no se han llevado por delante el que ya estaba");
        }
        finally { try { Directory.Delete(baseDir, recursive: true); } catch { } }

        Program.Assert(Indice.Leer("{ roto") is null, "un índice ilegible no revienta nada");
        Program.Assert(Indice.Leer("""{"contrato":99,"complementos":[]}""") is null,
            "y uno que habla otro contrato se descarta entero");
    }
}
