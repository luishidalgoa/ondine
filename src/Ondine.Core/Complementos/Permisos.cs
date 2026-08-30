using System.IO;

namespace Ondine.Complementos;

/// <summary>
/// El bit de ejecución en Unix, que es lo que separa un script instalado de un script que arranca.
///
/// <para>
/// <b>Por qué hace falta.</b> Un <c>.zip</c> hecho en Windows no guarda permisos de Unix, así que
/// el <c>.sh</c> del complemento sale de la instalación sin poder ejecutarse. Está donde tiene que
/// estar, con el contenido correcto, y el sistema contesta «permission denied» — que es de esos
/// fallos que no se parecen en nada a su causa.
/// </para>
/// </summary>
public static class Permisos
{
    /// <summary>
    /// Le pone permiso de ejecución, como <c>chmod +x</c>: donde ya hay lectura y en ningún sitio
    /// más. En Windows no hace nada.
    ///
    /// <para>
    /// Se añade donde hay lectura en vez de a todo el mundo por lo de siempre: un fichero que
    /// nadie más puede leer tampoco tiene por qué poder ejecutarlo nadie más. Y no lanza: esto se
    /// llama al instalar, y una excepción aquí tiraría una instalación que iba bien por un permiso
    /// que a lo mejor ya estaba puesto.
    /// </para>
    /// </summary>
    public static void AsegurarEjecutable(string ruta)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            if (!File.Exists(ruta)) return;

            // UN ENLACE NO SE TOCA. «chmod» sigue el enlace y cambia los permisos del OTRO
            // fichero: un complemento que trajera dentro un enlace a algo del sistema conseguiría
            // que Ondine le pusiera permiso de ejecución a lo apuntado, con los permisos de quien
            // corre la aplicación. Nadie lo vería, y no haría falta ni engañar a nadie: basta con
            // que el enlace esté en la carpeta.
            if (EsEnlace(ruta)) return;

            var modo = File.GetUnixFileMode(ruta);
            var quiere = ConEjecucion(modo);
            if (quiere != modo) File.SetUnixFileMode(ruta, quiere);
        }
        catch { /* un permiso que no se puede tocar se verá al arrancar, con su mensaje */ }
    }

    /// <summary>
    /// ¿Esto es un enlace —simbólico o de los de Windows— en vez de un fichero de verdad?
    ///
    /// <para>
    /// Se mira por los atributos y no resolviendo el destino, porque lo que importa aquí no es a
    /// dónde apunta: es que apunte a alguna parte. Un enlace que apunta dentro de la misma
    /// carpeta tampoco hay que tocarlo — el fichero de verdad ya se toca por su nombre.
    /// </para>
    /// <para>
    /// Si no se puede ni mirar, se dice que sí. Ante la duda, no tocar.
    /// </para>
    /// </summary>
    public static bool EsEnlace(string ruta)
    {
        try { return new FileInfo(ruta).Attributes.HasFlag(FileAttributes.ReparsePoint); }
        catch { return true; }
    }

    /// <summary>
    /// El modo que resulta de un <c>chmod +x</c>: ejecución donde ya hay lectura.
    ///
    /// <para>
    /// Es una función aparte, y pura, para que la DECISIÓN se pueda comprobar en cualquier
    /// sistema. Metida dentro de la llamada al sistema, solo se probaría corriendo en Linux — y
    /// entonces media red viaja sin comprobar en la máquina donde se escribe el código, que es
    /// justo donde se cometen los errores.
    /// </para>
    /// </summary>
    public static UnixFileMode ConEjecucion(UnixFileMode modo)
    {
        var quiere = modo;
        if (modo.HasFlag(UnixFileMode.UserRead)) quiere |= UnixFileMode.UserExecute;
        if (modo.HasFlag(UnixFileMode.GroupRead)) quiere |= UnixFileMode.GroupExecute;
        if (modo.HasFlag(UnixFileMode.OtherRead)) quiere |= UnixFileMode.OtherExecute;
        return quiere;
    }
}
