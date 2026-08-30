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
    /// <summary>Los intérpretes que se prueban, en orden, para un <c>.py</c>.</summary>
    private static readonly string[] Pythons = ["python3", "python"];

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
    /// Dónde está un programa del PATH, o <c>null</c> si no está. En Windows prueba también las
    /// extensiones de <c>PATHEXT</c>, que es como lo resuelve el sistema.
    /// </summary>
    public static string? EnLaRuta(string programa) => Sabidos.GetOrAdd(programa, static quien =>
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
                try
                {
                    var candidato = Path.Combine(carpeta.Trim('"'), quien + ext);
                    if (File.Exists(candidato)) return candidato;
                }
                catch { /* una carpeta del PATH con caracteres imposibles no para la búsqueda */ }
            }
        }
        return null;
    });

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

    /// <summary>El mismo nombre con otra extensión, respetando la carpeta relativa que traiga.</summary>
    private static string Cambiar(string relativa, string extension)
    {
        var carpeta = Path.GetDirectoryName(relativa) ?? "";
        var nombre = Path.GetFileNameWithoutExtension(relativa) + extension;
        return carpeta.Length == 0 ? nombre : Path.Combine(carpeta, nombre);
    }
}
