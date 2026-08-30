using System.Runtime.InteropServices;
using Ondine.Complementos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué programa se ejecuta de un complemento, según el sistema.
///
/// <para>
/// <b>El caso que lo motiva.</b> Un complemento declara <c>"ejecutable": "algo.cmd"</c> porque se
/// escribió en Windows. En Linux y en macOS un <c>.cmd</c> no es ejecutable: <c>Process.Start</c>
/// intenta lanzarlo y el sistema contesta «permission denied» —o «exec format error», que aún se
/// entiende menos—. El complemento aparece instalado, en su sitio, y no arranca.
/// </para>
/// <para>
/// <b>Y por qué esto se prueba con el sistema como PARÁMETRO</b> en vez de con
/// <c>OperatingSystem.IsLinux()</c> por dentro: la resolución para Linux se comprueba corriendo en
/// Windows y al revés. Lo contrario —cada rama solo se prueba en su máquina— significa que la
/// mitad de esta lógica viaja sin red en cada máquina, y justo la que falla es la que el que
/// escribe el complemento no ve.
/// </para>
/// </summary>
public static class ComplementoMultiplataformaTests
{
    public static void Todas()
    {
        Program.Seccion("Complementos en Linux y macOS");

        ElCmdNoSeEjecutaEnUnix();
        LosCamposPorSistema();
        LoQueNoSePuedeArrancar();
        NoSeSaleDeSuCarpeta();
        ElBitDeEjecucion();
        ElManifiestoTraeLosCamposNuevos();
        InstalarDejaLosScriptsEjecutables();
        UnComplementoDeVerdadArranca();
        UnComplementoDePythonArranca();
    }

    // ── El caso del .cmd ─────────────────────────────────────────────────────

    /// <summary>
    /// En Windows se ejecuta el <c>.cmd</c>. En Unix, el hermano que sí se puede ejecutar.
    /// </summary>
    private static void ElCmdNoSeEjecutaEnUnix()
    {
        // Lo que trae el ejemplo de YouTube: el .cmd es un envoltorio de tres líneas y el trabajo
        // está en el .py de al lado.
        var hay = Hay("/plug/youtube.cmd", "/plug/youtube.py");

        var win = Arranque.Resolver("youtube.cmd", null, null, "/plug", So.Windows, hay, SinPython);
        Program.Assert(win.Reparo is null && win.Programa.EndsWith("youtube.cmd"),
            $"en Windows se ejecuta el .cmd, como siempre ({win.Programa})");
        Program.Assert(win.PorLotes, "y se sabe que es un fichero por lotes, que se le pasan los "
            + "argumentos de otra forma");

        var linux = Arranque.Resolver("youtube.cmd", null, null, "/plug", So.Linux, hay, ConPython);
        Program.Assert(linux.Reparo is null, $"en Linux también arranca ({linux.Reparo})");
        Program.Assert(linux.Programa == "/usr/bin/python3",
            $"y lo hace con el intérprete, porque el .py no trae almohadilla-bang ({linux.Programa})");
        Program.Assert(linux.Antes.Count == 1 && linux.Antes[0].EndsWith("youtube.py"),
            "con el script delante de todo lo demás");
        Program.Assert(!linux.PorLotes,
            "y ya no es un fichero por lotes: los argumentos van uno a uno, sin comillas de cmd");

        // Un .sh al lado gana al .py: si su autor escribió los dos, el .sh es el que hizo para
        // esto. Y no hace falta intérprete que buscar.
        var conSh = Hay("/plug/algo.cmd", "/plug/algo.sh", "/plug/algo.py");
        var conShLinux = Arranque.Resolver("algo.cmd", null, null, "/plug", So.Linux, conSh, SinPython);
        Program.Assert(conShLinux.Reparo is null && conShLinux.Programa.EndsWith("algo.sh"),
            $"un .sh al lado se prefiere al .py ({conShLinux.Programa})");
        Program.Assert(conShLinux.Antes.Count == 0, "y se ejecuta él, sin intérprete delante");

        // macOS va por el mismo camino que Linux. Se comprueba aparte y no «es Unix, ya está»:
        // son dos ramas del switch y una se puede escribir mal sin que la otra se entere.
        var mac = Arranque.Resolver("youtube.cmd", null, null, "/plug", So.Mac, hay, ConPython);
        Program.Assert(mac.Reparo is null && mac.Programa == "/usr/bin/python3",
            $"y en macOS lo mismo ({Dice(mac)})");
    }

    // ── Los campos por sistema ───────────────────────────────────────────────

    /// <summary>
    /// <c>ejecutable_linux</c> y <c>ejecutable_macos</c> mandan sobre <c>ejecutable</c>, cada uno
    /// en el suyo. Es para el complemento que trae un programa distinto de verdad, no un
    /// envoltorio: un binario compilado por plataforma, por ejemplo.
    /// </summary>
    private static void LosCamposPorSistema()
    {
        var hay = Hay("/plug/w.cmd", "/plug/l.sh", "/plug/m.sh");

        var win = Arranque.Resolver("w.cmd", "l.sh", "m.sh", "/plug", So.Windows, hay, SinPython);
        Program.Assert(win.Programa.EndsWith("w.cmd"),
            $"en Windows manda «ejecutable» aunque estén los otros dos ({win.Programa})");

        var lin = Arranque.Resolver("w.cmd", "l.sh", "m.sh", "/plug", So.Linux, hay, SinPython);
        Program.Assert(lin.Programa.EndsWith("l.sh"), $"en Linux manda el suyo ({lin.Programa})");

        var mac = Arranque.Resolver("w.cmd", "l.sh", "m.sh", "/plug", So.Mac, hay, SinPython);
        Program.Assert(mac.Programa.EndsWith("m.sh"), $"y en macOS el suyo ({mac.Programa})");

        // Declarar solo el de Linux no deja a macOS sin nada que hacer: cae al general, y de ahí
        // al hermano ejecutable si hace falta. Lo contrario obligaría a repetir el campo dos veces
        // para el caso normal, que es que Unix sea Unix.
        var soloLinux = Hay("/plug/w.cmd", "/plug/l.sh", "/plug/w.py");
        var macSinSuyo = Arranque.Resolver("w.cmd", "l.sh", null, "/plug", So.Mac, soloLinux, ConPython);
        Program.Assert(macSinSuyo.Reparo is null && macSinSuyo.Antes.Count == 1
                       && macSinSuyo.Antes[0].EndsWith("w.py"),
            $"sin campo propio, macOS sigue el camino general ({macSinSuyo.Programa} {string.Join(" ", macSinSuyo.Antes)})");

        // Un .py declarado a pelo va por el intérprete en TODOS los sistemas. En Windows tampoco
        // se puede ejecutar un .py directamente sin la shell, así que la regla es la misma en los
        // tres sitios en vez de una excepción que solo se descubre al portarlo.
        var py = Hay("/plug/x.py");
        foreach (var so in new[] { So.Windows, So.Linux, So.Mac })
        {
            var r = Arranque.Resolver("x.py", null, null, "/plug", so, py, ConPython);
            Program.Assert(r.Reparo is null && r.Programa == "/usr/bin/python3"
                           && r.Antes.Count == 1,
                $"un .py va por el intérprete también en {so} ({Dice(r)})");
        }
    }

    // ── Lo que no arranca, dicho ─────────────────────────────────────────────

    /// <summary>
    /// Cuando no hay nada que ejecutar se dice POR QUÉ. Un complemento que no arranca y no explica
    /// el motivo deja a quien lo instaló mirando una lista sin nada que corregir — que es la razón
    /// por la que <c>Reparo</c> devuelve un texto y no un booleano.
    /// </summary>
    private static void LoQueNoSePuedeArrancar()
    {
        var soloCmd = Hay("/plug/solo.cmd");

        var sinHermano = Arranque.Resolver("solo.cmd", null, null, "/plug", So.Linux, soloCmd, ConPython);
        Program.Assert(sinHermano.Reparo is not null && sinHermano.Reparo.Contains("solo.cmd"),
            $"un .cmd sin hermano ejecutable no arranca en Linux, y se dice ({sinHermano.Reparo})");

        var sinPython = Arranque.Resolver("x.py", null, null, "/plug", So.Linux, Hay("/plug/x.py"), SinPython);
        Program.Assert(sinPython.Reparo is not null && sinPython.Reparo.Contains("Python"),
            $"y si hace falta Python y no está, se dice cuál falta ({sinPython.Reparo})");

        var noEsta = Arranque.Resolver("fantasma.sh", null, null, "/plug", So.Linux, Hay(), SinPython);
        Program.Assert(noEsta.Reparo is not null && noEsta.Reparo.Contains("fantasma.sh"),
            $"un programa que no está se dice por su nombre ({noEsta.Reparo})");

        var vacio = Arranque.Resolver("", null, null, "/plug", So.Linux, Hay(), SinPython);
        Program.Assert(vacio.Reparo is not null, "y sin declarar ninguno, tampoco hay nada que hacer");
    }

    // ── Seguridad ────────────────────────────────────────────────────────────

    /// <summary>
    /// La comprobación que ya tenía <c>ejecutable</c> vale igual para los campos nuevos.
    ///
    /// <para>
    /// <b>Esto es lo que había que no olvidar al añadirlos.</b> Un manifiesto que declara
    /// <c>"ejecutable_linux": "../../../bin/sh"</c> no es un complemento mal escrito: es uno
    /// pidiendo ejecutar cualquier cosa del disco. Y colaría sin que nadie lo mirase en Windows,
    /// donde ese campo ni se usa, hasta que alguien instalase el mismo paquete en Linux.
    /// </para>
    /// </summary>
    private static void NoSeSaleDeSuCarpeta()
    {
        var todo = new Func<string, bool>(_ => true);

        foreach (var (campo, ej, lin, mac) in new (string, string, string?, string?)[]
        {
            ("ejecutable",       "../../fuera.sh", null,             null),
            ("ejecutable_linux", "dentro.sh",      "../../fuera.sh", null),
            ("ejecutable_macos", "dentro.sh",      null,             "../../fuera.sh"),
        })
        {
            // Se comprueban los TRES en los TRES sistemas: uno que se sale solo se rechazaría
            // donde se usa, y entonces el paquete pasaría la revisión en una máquina y no en otra.
            foreach (var so in new[] { So.Windows, So.Linux, So.Mac })
            {
                var r = Arranque.Resolver(ej, lin, mac, "/plug", so, todo, ConPython);
                Program.Assert(r.Reparo is not null,
                    $"«{campo}» apuntando fuera de su carpeta se rechaza, también en {so}");
            }
        }
    }

    // ── El bit de ejecución ──────────────────────────────────────────────────

    /// <summary>
    /// Un fichero salido de un <c>.zip</c> hecho en Windows llega sin permiso de ejecución, así
    /// que el <c>.sh</c> está donde tiene que estar y no se puede lanzar. Se le pone, como haría
    /// <c>chmod +x</c>: ejecución donde ya hay lectura, y nada más.
    ///
    /// <para>
    /// En Windows la operación no existe y no tiene que reventar: se llama igual desde el mismo
    /// sitio, y una excepción aquí se llevaría por delante una instalación que iba bien.
    /// </para>
    /// </summary>
    private static void ElBitDeEjecucion()
    {
        // Primero la DECISIÓN, que es pura y se comprueba corriendo en cualquier sistema. Lo de
        // abajo toca el disco y en Windows no puede hacer nada: si esto viviera solo ahí, la
        // regla entera viajaría sin comprobar en la máquina donde se escribe.
        const UnixFileMode Normal = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                    UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        var listo = Permisos.ConEjecucion(Normal);
        Program.Assert(listo.HasFlag(UnixFileMode.UserExecute) &&
                       listo.HasFlag(UnixFileMode.GroupExecute) &&
                       listo.HasFlag(UnixFileMode.OtherExecute),
            $"un 644 pasa a poder ejecutarse por quien lo lee ({listo})");
        Program.Assert(listo.HasFlag(UnixFileMode.UserWrite) && !listo.HasFlag(UnixFileMode.GroupWrite),
            $"y no se toca nada más: la escritura queda como estaba ({listo})");

        var soloSuyo = Permisos.ConEjecucion(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        Program.Assert(soloSuyo.HasFlag(UnixFileMode.UserExecute) &&
                       !soloSuyo.HasFlag(UnixFileMode.GroupExecute) &&
                       !soloSuyo.HasFlag(UnixFileMode.OtherExecute),
            $"un fichero que solo lee su dueño solo lo ejecuta su dueño ({soloSuyo})");

        Program.Assert(Permisos.ConEjecucion(listo) == listo,
            "y aplicarlo dos veces deja lo mismo: se instala encima a menudo");

        Program.Assert(Permisos.ConEjecucion(UnixFileMode.None) == UnixFileMode.None,
            "un fichero que no puede leer nadie no se vuelve ejecutable por si acaso");

        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-chmod-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        try
        {
            var f = Path.Combine(carpeta, "algo.sh");
            File.WriteAllText(f, "#!/bin/sh\necho hola\n");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Permisos.AsegurarEjecutable(f);
                Program.Assert(File.Exists(f), "en Windows no hace nada y no revienta");
                return;
            }

            File.SetUnixFileMode(f, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                    UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            Permisos.AsegurarEjecutable(f);

            var modo = File.GetUnixFileMode(f);
            Program.Assert(modo.HasFlag(UnixFileMode.UserExecute), $"su dueño lo puede ejecutar ({modo})");
            Program.Assert(modo.HasFlag(UnixFileMode.GroupExecute) &&
                           modo.HasFlag(UnixFileMode.OtherExecute),
                $"y quien lo pueda leer, también: es lo que hace «chmod +x» ({modo})");
            Program.Assert(!modo.HasFlag(UnixFileMode.SetUser) && !modo.HasFlag(UnixFileMode.SetGroup),
                "sin repartir nada más que ejecución");

            // Un fichero que nadie puede leer no se vuelve ejecutable por si acaso.
            var privado = Path.Combine(carpeta, "privado.sh");
            File.WriteAllText(privado, "x");
            File.SetUnixFileMode(privado, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Permisos.AsegurarEjecutable(privado);
            var m2 = File.GetUnixFileMode(privado);
            Program.Assert(m2.HasFlag(UnixFileMode.UserExecute) && !m2.HasFlag(UnixFileMode.OtherExecute),
                $"la ejecución se añade donde hay lectura, no en todas partes ({m2})");

            // Y llamarlo dos veces no cambia nada la segunda: se instala encima a menudo.
            Permisos.AsegurarEjecutable(f);
            Program.Assert(File.GetUnixFileMode(f) == modo, "llamarlo dos veces deja lo mismo");
        }
        finally
        {
            try { Directory.Delete(carpeta, recursive: true); } catch { }
        }
    }

    // ── De extremo a extremo ─────────────────────────────────────────────────

    /// <summary>
    /// Los campos nuevos se leen del manifiesto. Uno que no se deserializa es un campo que no
    /// existe: la resolución lo pediría, encontraría vacío, y nadie sabría por qué.
    /// </summary>
    private static void ElManifiestoTraeLosCamposNuevos()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-plug-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        try
        {
            var manifiesto = Path.Combine(carpeta, "plugin.json");
            File.WriteAllText(manifiesto, """
            {
              "nombre": "De prueba", "version": "1.0.0", "contrato": 1,
              "ejecutable": "x.cmd",
              "ejecutable_linux": "x-linux.sh",
              "ejecutable_macos": "x-mac.sh",
              "capacidades": ["importar"]
            }
            """);

            var c = Complemento.Leer(manifiesto);
            Program.Assert(c is not null, "el manifiesto se lee");
            if (c is null) return;
            Program.Assert(c.EjecutableLinux == "x-linux.sh", $"«ejecutable_linux» llega ({c.EjecutableLinux})");
            Program.Assert(c.EjecutableMacos == "x-mac.sh", $"y «ejecutable_macos» ({c.EjecutableMacos})");

            // Y sin declararlos se quedan vacíos y no estorban: los manifiestos que ya existen
            // siguen valiendo tal cual, que es lo que no puede romperse al añadir un campo.
            File.WriteAllText(manifiesto, """
            { "nombre": "Vieja", "version": "1.0.0", "contrato": 1,
              "ejecutable": "x.cmd", "capacidades": ["importar"] }
            """);
            var vieja = Complemento.Leer(manifiesto);
            Program.Assert(vieja is not null && vieja.EjecutableLinux.Length == 0,
                "un manifiesto de los de antes se sigue leyendo igual");
        }
        finally { try { Directory.Delete(carpeta, recursive: true); } catch { } }
    }

    /// <summary>
    /// Instalar deja los <c>.sh</c> ejecutables. Es la otra mitad del problema: el <c>.zip</c> no
    /// guarda el bit y el script queda en su sitio sin poder lanzarse.
    /// </summary>
    private static void InstalarDejaLosScriptsEjecutables()
    {
        var basePlug = Path.Combine(Path.GetTempPath(), "ondine-inst-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(basePlug);
        try
        {
            // Un zip sin permisos de Unix dentro: es lo que sale de comprimir en Windows, que es
            // como se empaqueta la mayoría de los complementos.
            var paquete = Zip(
                ("plugin.json", Manifiesto("eco.cmd")),
                ("eco.cmd", "@echo off\r\n"),
                ("eco.sh", "#!/bin/sh\necho hola\n"),
                ("datos.json", "{}\n"));

            var entrada = new Indice.Entrada
            {
                Id = "eco", Nombre = "Eco", Version = "1.0.0",
                Paquete = "https://ejemplo/eco.zip", Sha256 = Indice.Huella(paquete),
            };

            var r = Instalador.Instalar(entrada, paquete, basePlug);
            Program.Assert(r.Ok, $"el paquete se instala ({r.Motivo})");
            if (!r.Ok) return;

            var sh = Path.Combine(basePlug, "eco", "eco.sh");
            Program.Assert(File.Exists(sh), "y el script está donde toca");

            // A QUIÉN se le da el bit es una decisión con criterio dentro, y se comprueba aquí
            // corriendo en cualquier sistema. Lo de abajo toca el disco y en Windows no puede
            // hacer nada: dejarlo solo ahí es lo que hacía que sabotear el reparto no pusiera
            // nada rojo en la máquina donde se escribe el código.
            var instalado = Complemento.Leer(Path.Combine(basePlug, "eco", "plugin.json"))!;
            var elegidos = Instalador.AQuienDarPermiso(Path.Combine(basePlug, "eco"), instalado)
                .Select(Path.GetFileName).ToList();

            Program.Assert(elegidos.Contains("eco.sh"),
                $"el .sh entra en el reparto ({string.Join(", ", elegidos)})");
            Program.Assert(!elegidos.Contains("datos.json"),
                $"y los datos no: un .json no se ejecuta ({string.Join(", ", elegidos)})");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            Program.Assert(File.GetUnixFileMode(sh).HasFlag(UnixFileMode.UserExecute),
                $"el .sh sale ejecutable de la instalación ({File.GetUnixFileMode(sh)})");

            // Y los datos NO. Repartir el bit a todo lo extraído «por si acaso» es justo lo que no
            // hay que hacer: un .json no se ejecuta.
            var datos = Path.Combine(basePlug, "eco", "datos.json");
            Program.Assert(!File.GetUnixFileMode(datos).HasFlag(UnixFileMode.UserExecute),
                $"y un fichero de datos no ({File.GetUnixFileMode(datos)})");
        }
        finally { try { Directory.Delete(basePlug, recursive: true); } catch { } }
    }

    /// <summary>
    /// Y lo que de verdad importa: <b>arrancarlo</b>, en la máquina donde corra esta prueba.
    ///
    /// <para>
    /// El complemento declara un <c>.cmd</c> —como los que ya existen— y trae al lado un
    /// <c>.sh</c>. En Windows corre el <c>.cmd</c>; en Linux y en macOS, el <c>.sh</c>, sin que el
    /// manifiesto diga una palabra del sistema. Antes de esto, en Unix, la respuesta era
    /// «permission denied» y no llegaba ni un mensaje.
    /// </para>
    /// </summary>
    private static void UnComplementoDeVerdadArranca()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-corre-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        try
        {
            File.WriteAllText(Path.Combine(carpeta, "plugin.json"), Manifiesto("eco.cmd"));

            // Los dos dicen lo mismo: una línea de JSON devolviendo lo que reciben, para comprobar
            // de paso que los argumentos llegan enteros.
            File.WriteAllText(Path.Combine(carpeta, "eco.cmd"),
                "@echo off\r\necho {\"tipo\":\"elemento\",\"id\":\"%~2\",\"titulo\":\"eco\"}\r\n");
            File.WriteAllText(Path.Combine(carpeta, "eco.sh"),
                "#!/bin/sh\nprintf '{\"tipo\":\"elemento\",\"id\":\"%s\",\"titulo\":\"eco\"}\\n' \"$2\"\n");

            var c = Complemento.Leer(Path.Combine(carpeta, "plugin.json"));
            Program.Assert(c is not null, "se lee el complemento");
            if (c is null) return;

            Program.Assert(c.Reparo() is null, $"no le pasa nada en {Arranque.Actual} ({c.Reparo()})");

            var mensajes = Recoger(c, Invocador.ComandoListar, ["una fuente"]);
            var elementos = mensajes.Where(m => m.Tipo == Mensaje.TipoElemento).ToList();

            Program.Assert(elementos.Count == 1,
                $"arranca y contesta ({string.Join(" | ", mensajes.Select(m => m.Tipo + " " + m.MensajeError))})");
            if (elementos.Count == 0) return;

            Program.Assert(elementos[0].Id == "una fuente",
                $"y los argumentos le llegan enteros, con su espacio dentro ({elementos[0].Id})");
        }
        finally { try { Directory.Delete(carpeta, recursive: true); } catch { } }
    }

    /// <summary>
    /// El otro camino: el que pasa por el intérprete.
    ///
    /// <para>
    /// Se prueba aparte porque es el único donde el programa que se lanza <b>no</b> es el fichero
    /// del complemento, y donde el script tiene que ir delante de sus argumentos. Equivocar ese
    /// orden no da un error claro: Python se queda esperando por la entrada estándar o abre el
    /// fichero equivocado.
    /// </para>
    /// <para>
    /// Si en la máquina no hay Python, se dice y no se prueba. Un aviso es honesto; dar por buena
    /// una comprobación que no se ha hecho, no.
    /// </para>
    /// </summary>
    private static void UnComplementoDePythonArranca()
    {
        if (Arranque.EnLaRuta("python3") is null && Arranque.EnLaRuta("python") is null)
        {
            Program.Assert(true, "sin Python en esta máquina: el camino del intérprete no se prueba aquí");
            return;
        }

        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-py-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        try
        {
            // Se declara el .py DIRECTAMENTE, y no un .cmd que lo llame. Con el .cmd, Windows lo
            // ejecutaba a él y el intérprete lo ponía cmd por dentro: el camino que se quiere
            // probar aquí —el de Ondine lanzando al intérprete— no llegaba a correrse en Windows,
            // y sabotearlo no ponía nada rojo. Declarando el .py pasan por él los tres sistemas.
            File.WriteAllText(Path.Combine(carpeta, "plugin.json"), Manifiesto("eco.py"));
            File.WriteAllText(Path.Combine(carpeta, "eco.py"),
                "import sys, json\n"
                + "print(json.dumps({\"tipo\": \"elemento\", \"id\": sys.argv[2], \"titulo\": \"eco\"}))\n");

            var c = Complemento.Leer(Path.Combine(carpeta, "plugin.json"));
            if (c is null) { Program.Assert(false, "se lee el complemento de Python"); return; }

            Program.Assert(c.Reparo() is null, $"no le pasa nada en {Arranque.Actual} ({c.Reparo()})");

            var mensajes = Recoger(c, Invocador.ComandoListar, ["una fuente"]);
            var elementos = mensajes.Where(m => m.Tipo == Mensaje.TipoElemento).ToList();

            Program.Assert(elementos.Count == 1,
                $"arranca por el intérprete y contesta ({string.Join(" | ", mensajes.Select(m => m.Tipo + " " + m.MensajeError))})");
            if (elementos.Count == 0) return;

            Program.Assert(elementos[0].Id == "una fuente",
                $"y el script va delante, así que los argumentos caen donde el complemento los espera ({elementos[0].Id})");
        }
        finally { try { Directory.Delete(carpeta, recursive: true); } catch { } }
    }

    private static List<Mensaje> Recoger(Complemento c, string comando, string[] args)
    {
        var fuera = new List<Mensaje>();
        Task.Run(async () =>
        {
            await foreach (var m in Invocador.CorrerAsync(c, comando, args)) fuera.Add(m);
        }).GetAwaiter().GetResult();
        return fuera;
    }

    private static string Manifiesto(string ejecutable) => $$"""
    { "nombre": "Eco", "version": "1.0.0", "contrato": 1,
      "ejecutable": "{{ejecutable}}", "capacidades": ["importar"] }
    """;

    /// <summary>Un .zip en memoria, sin permisos de Unix dentro: como el que sale de Windows.</summary>
    private static byte[] Zip(params (string Nombre, string Texto)[] ficheros)
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(
                   ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (nombre, texto) in ficheros)
            {
                using var w = new StreamWriter(zip.CreateEntry(nombre).Open());
                w.Write(texto);
            }
        }
        return ms.ToArray();
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Un disco de mentira: existen estos ficheros y ninguno más.
    ///
    /// <para>
    /// Se compara por NOMBRE y no por ruta entera a propósito. La resolución devuelve rutas del
    /// sistema donde corre la prueba —«C:\plug\x.cmd» en Windows—, así que compararlas enteras
    /// haría que estas comprobaciones solo valiesen en Unix, que es justo lo que se quería
    /// evitar. Todos los ficheros de estas pruebas viven en la misma carpeta.
    /// </para>
    /// </summary>
    private static Func<string, bool> Hay(params string[] rutas)
    {
        var juego = rutas.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return r => juego.Contains(Path.GetFileName(r.Replace('\\', '/')));
    }

    /// <summary>Lo que resolvió, o por qué no pudo: para que el mensaje del fallo lo diga.</summary>
    private static string Dice(Arranque a) =>
        a.Reparo ?? (a.Programa + " " + string.Join(" ", a.Antes)).Trim();

    private static string? ConPython(string quien) =>
        quien is "python3" or "python" ? "/usr/bin/python3" : null;

    private static string? SinPython(string _) => null;
}
