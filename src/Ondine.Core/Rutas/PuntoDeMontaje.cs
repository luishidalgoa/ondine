namespace Ondine;

/// <summary>
/// En qué volumen está una ruta.
///
/// <para>
/// <b>Por qué no se usa <c>Path.GetPathRoot()</c>, que es lo que parece:</b> devuelve la letra
/// de unidad en Windows —«E:\»— pero en Linux y macOS devuelve <b>«/» para cualquier ruta
/// absoluta</b>. Así que dos rutas de discos completamente distintos —la carpeta personal y un
/// NAS montado en <c>/mnt</c>— salen como el mismo volumen.
/// </para>
/// <para>
/// Eso rompía dos cosas del camino de trabajo, y las dos en silencio: el guardián de «disco
/// lleno» medía la partición raíz en vez del disco de destino —con la raíz llena, la
/// compresión se quedaba esperando para siempre a que se liberara un sitio que no
/// necesitaba—, y el aviso de «esto cruza de disco» antes de reordenar <b>no salía nunca</b>,
/// que es justo el aviso que importa cuando la biblioteca está en un NAS o en un USB. Y ahí
/// mover no es renombrar: es copiar el fichero entero.
/// </para>
/// </summary>
public static class PuntoDeMontaje
{
    /// <summary>
    /// El montaje al que pertenece una ruta, o <c>null</c> si ninguno encaja.
    ///
    /// <para>
    /// Gana el <b>más específico</b>: <c>/media/luis/USB/pelis</c> es del USB y no de la raíz,
    /// aunque las dos sean prefijos suyos. Y se compara por carpetas, no por letras, porque si
    /// no <c>/homeless</c> saldría como parte de <c>/home</c>.
    /// </para>
    /// <param name="montajes">
    /// Los puntos de montaje del sistema. Se pasan como argumento para poder comprobar el
    /// reparto desde cualquier máquina; en la app se le da lo que dice el sistema.
    /// </param>
    /// </summary>
    public static string? De(string ruta, IReadOnlyList<string> montajes)
    {
        if (string.IsNullOrWhiteSpace(ruta) || montajes.Count == 0) return null;

        // AQUI NO SE NORMALIZA LA RUTA, y esa es la parte que hay que entender.
        //
        // La primera version llamaba a Path.GetFullPath, y eso rompe la funcion en dos
        // sentidos: es dependiente del sistema -en Windows, «/mnt/nas/x» se convierte en
        // «C:\mnt\nas\x» con separadores de Windows, asi que ninguna ruta de Linux casaria con su
        // montaje- y convierte una comparacion de texto en algo que depende de donde corra.
        //
        // Normalizar es del que llama, que ya lo hace y sabe en que sistema esta. Aqui se
        // comparan rutas absolutas contra montajes, y punto.
        var completa = ruta;

        string? mejor = null;
        foreach (var m in montajes)
        {
            if (string.IsNullOrEmpty(m) || !EstaDentro(completa, m)) continue;
            if (mejor is null || m.Length > mejor.Length) mejor = m;
        }
        return mejor;
    }

    /// <summary>
    /// Si una ruta cae dentro de un montaje.
    ///
    /// <para>
    /// La comparación es por <b>frontera de carpeta</b> y no por texto: «/homeless» empieza por
    /// «/home» como cadena y no está dentro de él. Es el error que se comete al escribir esto
    /// con un StartsWith a secas, y no se nota hasta que alguien tiene una carpeta con ese
    /// nombre.
    /// </para>
    /// </summary>
    private static bool EstaDentro(string ruta, string montaje)
    {
        var m = montaje.TrimEnd('/', '\\');
        if (m.Length == 0) return true;   // la raíz de Unix: dentro está todo

        // En Windows las rutas no distinguen mayúsculas; en Linux y macOS, según el sistema de
        // ficheros. Se compara sin distinguir: equivocarse hacia «es el mismo volumen» es el
        // lado bueno de equivocarse, porque el aviso solo salta cuando es seguro que cruza.
        var cmp = StringComparison.OrdinalIgnoreCase;
        if (!ruta.StartsWith(m, cmp)) return false;
        if (ruta.Length == m.Length) return true;

        var siguiente = ruta[m.Length];
        return siguiente == '/' || siguiente == '\\';
    }

    /// <summary>Los montajes de esta máquina, tal como los ve el sistema.</summary>
    public static IReadOnlyList<string> DeEstaMaquina()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .ToList();
        }
        catch { return []; }
    }
}
