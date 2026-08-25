namespace Ondine.Rutas;

/// <summary>
/// Convierte lo que llega desde fuera —por argumentos, «Enviar a» o soltando con el ratón—
/// en una lista de vídeos: acepta archivos y también carpetas, que se recorren buscando.
///
/// <para>
/// Estaba dentro de <c>ShellIntegration</c>, la clase que escribe el menú contextual en el
/// registro de Windows. No tiene nada de Windows —son carpetas y extensiones—, y ahí ni se
/// podía probar ni la podía usar la interfaz de Avalonia, que también deja soltar ficheros.
/// </para>
/// </summary>
public static class VideosQueLlegan
{
    public static List<string> Expandir(IEnumerable<string> rutas, bool recursivo)
    {
        var res = new List<string>();
        foreach (var p in rutas)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            try
            {
                if (File.Exists(p))
                {
                    if (EsVideo(p)) res.Add(Path.GetFullPath(p));
                }
                else if (Directory.Exists(p))
                {
                    var opt = recursivo ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    res.AddRange(Directory.EnumerateFiles(p, "*.*", opt)
                        .Where(EsVideo).Select(Path.GetFullPath));
                }
            }
            catch { /* ruta inaccesible: se ignora */ }
        }

        // Sin repetidos y en orden: se puede soltar una carpeta Y un fichero de dentro, y el
        // orden en que el sistema los encuentra no es el que espera quien los soltó.
        return res.Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Los vídeos que vienen en la línea de órdenes al abrir la aplicación.
    ///
    /// <para>
    /// Es la tercera vía por la que entran ficheros —las otras dos son el selector y soltar
    /// con el ratón— y la que enseña el escritorio: el <c>.desktop</c> de Linux declara los
    /// tipos que Ondine abre, así que «Abrir con → Ondine» sobre un vídeo en Nemo lo pasa
    /// <b>por aquí</b>. Se recorre siempre en profundidad, como al soltar una carpeta.
    /// </para>
    /// <para>
    /// Se descartan los modificadores. Un guion es un modificador en los tres sistemas; una
    /// barra al principio, en cambio, <b>solo lo es en Windows</b> —<c>/select</c>— y en Linux
    /// y macOS es el comienzo de cualquier ruta absoluta. Descartarla en todas partes, que es
    /// lo que hacía el código de WPF, dejaría fuera <b>todos</b> los ficheros de Linux.
    /// </para>
    /// </summary>
    public static List<string> DeLosArgumentos(IEnumerable<string> argumentos) =>
        Expandir(
            argumentos.Where(a => !a.StartsWith('-'))
                      .Where(a => !(OperatingSystem.IsWindows() && a.StartsWith('/'))),
            recursivo: true);

    private static bool EsVideo(string ruta) =>
        Engine.VideoExtensions.Contains(Path.GetExtension(ruta).ToLowerInvariant());
}
