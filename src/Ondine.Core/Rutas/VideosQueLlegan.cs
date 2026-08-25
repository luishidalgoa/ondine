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

    private static bool EsVideo(string ruta) =>
        Engine.VideoExtensions.Contains(Path.GetExtension(ruta).ToLowerInvariant());
}
