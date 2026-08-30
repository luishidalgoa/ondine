using System.IO;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>Los sistemas que Ondine distingue al arrancar un complemento.</summary>
public enum So
{
    Windows,
    Linux,
    Mac,
}

/// <summary>
/// Qué programa se ejecuta de un complemento, y con qué delante.
///
/// <para>
/// <b>El problema.</b> Un complemento se escribe en Windows y declara
/// <c>"ejecutable": "algo.cmd"</c>. En Linux y en macOS eso no es un programa: el sistema
/// contesta «permission denied» —o «exec format error»— y el complemento aparece instalado, en su
/// sitio, sin arrancar. El caso no es raro: el envoltorio <c>.cmd</c> de tres líneas que llama a
/// un script es el patrón normal, y el ejemplo de YouTube que trae Ondine es exactamente eso.
/// </para>
/// <para>
/// <b>Por qué esto es una función pura con el sistema como parámetro</b> en vez de mirar
/// <c>OperatingSystem.IsLinux()</c> por dentro: así la resolución para Linux se comprueba
/// corriendo en Windows y al revés. Metido dentro, cada rama solo se probaría en su máquina — y
/// la que falla es siempre la que quien escribe el complemento no tiene delante.
/// </para>
/// </summary>
/// <param name="Programa">Lo que se ejecuta: el script, el binario, o el intérprete.</param>
/// <param name="Antes">Lo que va delante de los argumentos del complemento (el script, si va por intérprete).</param>
/// <param name="PorLotes">Si es un <c>.cmd</c>/<c>.bat</c>, que recibe los argumentos de otra forma.</param>
/// <param name="Reparo">Por qué no se puede arrancar, si no se puede. <c>null</c> cuando todo está en orden.</param>
public sealed record Arranque(
    string Programa,
    IReadOnlyList<string> Antes,
    bool PorLotes,
    string? Reparo)
{
    /// <summary>
    /// Los intérpretes que se prueban, en orden, para un <c>.py</c>.
    ///
    /// <para>
    /// En Windows se prueba <c>python</c> antes que <c>python3</c> porque una instalación normal
    /// —la de python.org— deja <c>python.exe</c> en el PATH y no <c>python3.exe</c>: el
    /// <c>python3</c> que aparece suele ser el alias de la Tienda. Fuera de Windows es al revés,
    /// donde <c>python</c> a secas puede seguir siendo el 2.
    /// </para>
    /// </summary>
    private static string[] Pythons => OperatingSystem.IsWindows()
        ? ["python", "python3"]
        : ["python3", "python"];

    /// <summary>
    /// La ruta relativa de un manifiesto, con las barras puestas de forma que signifiquen lo
    /// mismo en todas partes.
    ///
    /// <para>
    /// <b>Un manifiesto se escribe en Windows</b>, y ahí las dos barras separan carpetas. En Unix
    /// la invertida <b>no separa nada</b>: es un carácter más del nombre. Así que
    /// <c>"sub\\app.cmd"</c> visto desde Linux no es un fichero dentro de <c>sub</c>, es un
    /// fichero llamado literalmente <c>«sub\app.cmd»</c> — y el hermano que se buscaba al lado
    /// era otro invento igual, en la raíz, que no existía. El complemento quedaba descartado por
    /// «solo funciona en Windows» cuando lo que pasaba era que nadie había traducido la barra.
    /// </para>
    /// <para>
    /// El precio: un fichero de Unix que llevara una barra invertida <b>en el nombre</b> —legal,
    /// aunque nadie lo hace— dejaría de encontrarse. Frente a que no funcione ningún manifiesto
    /// escrito en Windows con subcarpetas, el cambio está claro.
    /// </para>
    /// </summary>
    public static string Normalizar(string? relativa) => (relativa ?? "").Replace('\\', '/');

    /// <summary>El sistema donde corre ahora mismo la aplicación.</summary>
    public static So Actual =>
        OperatingSystem.IsWindows() ? So.Windows
        : OperatingSystem.IsMacOS() ? So.Mac
        : So.Linux;   // lo que no es ninguno de los dos se trata como Unix, que es lo que es

    // Buscar por el PATH cuesta una vuelta al disco por carpeta, y esto se llama al pintar la
    // lista de complementos —una vez por complemento—. Se recuerda lo encontrado Y lo no
    // encontrado: el precio de recordar un «no está» es que instalar Python pide reiniciar la
    // aplicación, y a cambio una máquina sin Python no repasa el PATH entero en cada repintado.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string?> Sabidos = new();

    /// <summary>
    /// <b>Todas</b> las apariciones de un programa en el PATH, en el orden del PATH. En Windows
    /// prueba también las extensiones de <c>PATHEXT</c>, que es como lo resuelve el sistema.
    ///
    /// <para>
    /// Todas y no la primera, y esto importa: en Windows el PATH de un usuario suele traer
    /// <c>WindowsApps</c> por delante de la instalación de verdad, y ahí vive un alias de la
    /// Tienda que puede no ejecutar nada. Quedándose con la primera, se elige ese y no se llega
    /// nunca al Python que sí está instalado unas carpetas más allá.
    /// </para>
    /// </summary>
    public static IEnumerable<string> TodosEnLaRuta(string programa)
    {
        var extensiones = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [""];

        foreach (var carpeta in (Environment.GetEnvironmentVariable("PATH") ?? "")
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensiones)
            {
                string? candidato = null;
                try
                {
                    var c = Path.Combine(carpeta.Trim('"'), programa + ext);
                    if (File.Exists(c)) candidato = c;
                }
                catch { /* una carpeta del PATH con caracteres imposibles no para la búsqueda */ }

                if (candidato is not null) yield return candidato;
            }
        }
    }

    /// <summary>
    /// Dónde está un intérprete de Python que <b>de verdad conteste</b>, o <c>null</c>.
    ///
    /// <para>
    /// <b>No vale con encontrarlo.</b> En Windows, <c>WindowsApps</c> trae alias de la Tienda para
    /// <c>python.exe</c> y <c>python3.exe</c> que pesan cero bytes: si hay Python detrás, arrancan
    /// Python; si no lo hay, abren la Tienda y el complemento no se ejecuta nunca. Y no se pueden
    /// distinguir mirándolos — se midió: en la máquina donde se escribió esto, el alias pesa 0 y
    /// es un punto de reanálisis, <b>y contesta «Python 3.14.3»</b>. Así que descartarlos por el
    /// tamaño habría roto una instalación que funciona.
    /// </para>
    /// <para>
    /// Lo único que separa a uno bueno de uno malo es <b>preguntárselo</b>: se le pide la versión
    /// y se mira si contesta. Cuesta un proceso, una vez, y se recuerda.
    /// </para>
    /// </summary>
    public static string? Interprete(string programa) => Sabidos.GetOrAdd(programa, static quien =>
        ElQueContesta(TodosEnLaRuta(quien), Contesta));

    /// <summary>
    /// El primero de la lista que conteste. La decisión, aparte de ejecutar nada, para poder
    /// comprobarla sin depender de qué haya instalado en la máquina que corre las pruebas.
    /// </summary>
    public static string? ElQueContesta(IEnumerable<string> candidatos, Func<string, bool> contesta)
    {
        foreach (var c in candidatos)
            if (contesta(c))
                return c;
        return null;
    }

    /// <summary>¿Este ejecutable contesta a <c>--version</c> diciendo que es Python?</summary>
    private static bool Contesta(string ruta)
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta)
            {
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (p is null) return false;

            // Con tope: el alias de la Tienda sin nada detrás abre una ventana y se queda ahí, y
            // esto se llama mientras se pinta una lista.
            var dijo = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            if (!p.WaitForExit(5000)) { try { p.Kill(entireProcessTree: true); } catch { } return false; }

            return p.ExitCode == 0 && dijo.Contains("Python", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static bool EsPorLotes(string ruta) =>
        ruta.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
        ruta.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resuelve qué ejecutar.
    /// </summary>
    /// <param name="ejecutable">El campo <c>ejecutable</c> del manifiesto.</param>
    /// <param name="paraLinux">El campo <c>ejecutable_linux</c>, si lo declara.</param>
    /// <param name="paraMac">El campo <c>ejecutable_macos</c>, si lo declara.</param>
    /// <param name="carpeta">La carpeta del complemento. Nada puede salir de aquí.</param>
    /// <param name="so">El sistema para el que se resuelve.</param>
    /// <param name="existe">Si un fichero está. Se inyecta para poder probar los tres sistemas.</param>
    /// <param name="buscarEnRuta">Dónde está un programa del PATH, o null si no está.</param>
    public static Arranque Resolver(
        string ejecutable,
        string? paraLinux,
        string? paraMac,
        string carpeta,
        So so,
        Func<string, bool> existe,
        Func<string, string?> buscarEnRuta)
    {
        // ── 1) Nada puede salir de su carpeta, y se comprueba en los TRES campos ──
        //
        // Los campos por sistema son ejecutables igual que el general, así que la comprobación que
        // ya tenía «ejecutable» vale para ellos o no vale para nada. Y se comprueban los tres
        // ejecutando en cualquier sistema, no solo el que toca: un «ejecutable_linux» que apunta a
        // «../../../bin/sh» colaría en Windows —donde ese campo ni se mira— y el mismo paquete
        // quedaría aceptado en una máquina y rechazado en otra.
        // Las barras, lo primero de todo: lo que venga después trabaja sobre rutas que significan
        // lo mismo aquí y allí. Hacerlo más tarde deja a la comprobación de contención mirando una
        // ruta y a la búsqueda del hermano mirando otra.
        ejecutable = Normalizar(ejecutable);
        paraLinux = Normalizar(paraLinux);
        paraMac = Normalizar(paraMac);

        var suya = Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar;
        foreach (var declarado in new[] { ejecutable, paraLinux, paraMac })
        {
            if (string.IsNullOrWhiteSpace(declarado)) continue;
            var donde = Path.GetFullPath(Path.Combine(carpeta, declarado));
            if (!donde.StartsWith(suya, StringComparison.OrdinalIgnoreCase))
                return Imposible(Textos.Instancia.ComplementoEjecutableFuera);
        }

        // ── 2) Cuál de los tres campos manda ────────────────────────────────────
        //
        // El del sistema si lo declara; si no, el general. Que macOS caiga en el general —y desde
        // ahí, si hace falta, en el hermano ejecutable— es a propósito: obligar a repetir el mismo
        // campo dos veces para el caso normal, que es que Unix sea Unix, sobra.
        var elegido = so switch
        {
            So.Linux => Primero(paraLinux, ejecutable),
            So.Mac => Primero(paraMac, ejecutable),
            _ => ejecutable,
        };

        if (string.IsNullOrWhiteSpace(elegido))
            return Imposible(Textos.Instancia.ComplementoSinEjecutable);

        // ── 3) Un .cmd en Unix no es un programa: se busca su hermano ───────────
        //
        // El envoltorio .cmd de tres líneas que llama a un script es el patrón normal —el ejemplo
        // de YouTube que trae Ondine es exactamente eso—, así que casi siempre hay al lado un
        // hermano que sí se puede ejecutar. El .sh gana al .py: si su autor escribió los dos, el
        // .sh es el que hizo para esto.
        if (so != So.Windows && EsPorLotes(elegido))
        {
            var hermano = new[] { ".sh", ".py" }
                .Select(ext => Cambiar(elegido, ext))
                .FirstOrDefault(c => existe(Path.GetFullPath(Path.Combine(carpeta, c))));

            if (hermano is null)
                return Imposible(string.Format(Textos.Instancia.ComplementoSoloParaWindows, elegido));

            elegido = hermano;
        }

        var ruta = Path.GetFullPath(Path.Combine(carpeta, elegido));
        if (!existe(ruta))
            return Imposible(string.Format(Textos.Instancia.ComplementoEjecutableNoEsta, elegido));

        // ── 4) Un .py va por el intérprete, en los tres sistemas ────────────────
        //
        // Ni en Windows ni en Unix se ejecuta un .py a secas sin la shell: en Unix haría falta que
        // trajera almohadilla-bang —el del ejemplo no lo trae— y en Windows depende de una
        // asociación de ficheros que aquí no se usa. Así que la regla es la misma en los tres
        // sitios, en vez de una excepción que solo se descubre al portar el complemento.
        if (elegido.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
        {
            var interprete = Pythons.Select(buscarEnRuta).FirstOrDefault(x => !string.IsNullOrEmpty(x));
            if (string.IsNullOrEmpty(interprete))
                return Imposible(string.Format(Textos.Instancia.ComplementoSinInterprete, elegido));

            return new Arranque(interprete, [ruta], PorLotes: false, Reparo: null);
        }

        return new Arranque(ruta, [], EsPorLotes(ruta), Reparo: null);
    }

    private static Arranque Imposible(string motivo) => new("", [], false, motivo);

    private static string Primero(string? suyo, string general) =>
        string.IsNullOrWhiteSpace(suyo) ? general : suyo;

    /// <summary>
    /// El mismo nombre con otra extensión, respetando la carpeta relativa que traiga.
    ///
    /// <para>
    /// Se parte a mano por la barra en vez de usar <c>Path.GetDirectoryName</c> a propósito: esos
    /// métodos aplican las reglas del sistema <b>donde corre el proceso</b>, no las del sistema
    /// para el que se está resolviendo. Con ellos, resolver «para Linux» desde Windows daba un
    /// resultado y desde Linux otro — y entonces la función deja de ser pura y las pruebas dejan
    /// de valer para las dos, que era justo lo que se quería.
    /// </para>
    /// </summary>
    public static string Cambiar(string relativa, string extension)
    {
        var limpia = Normalizar(relativa);
        var barra = limpia.LastIndexOf('/');
        var carpeta = barra < 0 ? "" : limpia[..(barra + 1)];
        var hoja = barra < 0 ? limpia : limpia[(barra + 1)..];

        var punto = hoja.LastIndexOf('.');
        var sinExtension = punto <= 0 ? hoja : hoja[..punto];

        return carpeta + sinExtension + extension;
    }
}
