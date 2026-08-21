using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Reconoce los ficheros que están en el disco solo de nombre, y sabe devolverlos a su sitio.
///
/// Con la sincronización «bajo demanda» lo que hay en la carpeta puede ser un MARCADOR:
/// ocupa cero y el contenido vive en el servidor. Leer un solo byte dispara la descarga del
/// fichero ENTERO, de forma síncrona y silenciosa.
///
/// Nada de esto es de un proveedor concreto: lo define Windows (Cloud Files API) y lo usan
/// igual OneDrive, Nextcloud, Dropbox, Google Drive o iCloud. Por eso aquí no se nombra a
/// ninguno — se miran los atributos del fichero, que son los mismos para todos.
///
/// Medido sobre una biblioteca real: abrir con ffprobe un marcador de 277 MB se lo bajó
/// completo en 18 segundos.
/// </summary>
public static class Nube
{
    /// <summary>
    /// FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS. No está en el enum de .NET: es la marca
    /// moderna de «bajo demanda» (la clásica es OFFLINE, y conviven).
    /// </summary>
    public const int RecallOnDataAccess = 0x400000;

    /// <summary>FILE_ATTRIBUTE_PINNED: «mantener siempre en este dispositivo».</summary>
    public const int Anclado = 0x00080000;

    /// <summary>FILE_ATTRIBUTE_UNPINNED: «liberar espacio», devuélvelo a la nube.</summary>
    public const int Soltado = 0x00100000;

    public static bool EsMarcador(FileAttributes atributos) =>
        atributos.HasFlag(FileAttributes.Offline) ||
        ((int)atributos & RecallOnDataAccess) != 0;

    /// <summary>Igual, a partir de la ruta. Si no se puede leer, se asume que no lo es.</summary>
    public static bool EsMarcador(string ruta)
    {
        try { return EsMarcador(File.GetAttributes(ruta)); }
        catch { return false; }
    }

    /// <summary>
    /// Los atributos que dejan pedido «vuelve a la nube»: fuera ANCLADO, dentro SOLTADO, y
    /// el resto tal cual estaba. Es exactamente lo que hace `attrib +U -P`, que es la forma
    /// documentada de liberar espacio sin borrar nada.
    /// </summary>
    public static int AtributosParaLiberar(int actuales) => (actuales & ~Anclado) | Soltado;

    /// <summary>Una nube sincronizada en este equipo: quién es y dónde tiene su carpeta.</summary>
    public sealed record Sincronizacion(string Proveedor, string Raiz);

    /// <summary>
    /// De qué nube es este fichero, o <c>null</c> si de ninguna.
    ///
    /// <para>
    /// Medido: abrir un fichero de 65 MB que solo está en OneDrive y leer <b>un
    /// mega</b> bloquea más de cinco minutos sin terminar. Windows recupera el
    /// fichero al abrirlo y no hay forma de asomarse a él, así que reproducirlo
    /// para comprobar de qué episodio es sale carísimo y encima llena el disco.
    /// </para>
    /// <para>
    /// La salida es no abrirlo y mandar a su web, que es lo que ya ofrecen todos
    /// —«Ver en línea» en OneDrive, «Abrir en navegador» en Nextcloud—. Para eso
    /// basta saber qué raíz lo posee, y eso Windows lo publica igual para
    /// cualquier proveedor: no hace falta saber nada de ninguna nube concreta.
    /// </para>
    /// </summary>
    public static Sincronizacion? Duena(string? ruta, IEnumerable<Sincronizacion> raices)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return null;

        string plena;
        try { plena = Path.GetFullPath(ruta); } catch { return null; }

        Sincronizacion? mejor = null;
        foreach (var r in raices)
        {
            if (string.IsNullOrWhiteSpace(r.Raiz)) continue;

            // Con el separador al final. Sin él, «C:\OneDriveViejo» empezaría por
            // «C:\OneDrive» y un fichero de fuera se atribuiría a la nube — y se
            // acabaría abriendo la web equivocada.
            var raiz = Path.GetFullPath(r.Raiz).TrimEnd(Path.DirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
            if (!plena.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) continue;

            // Con nubes anidadas gana la MÁS específica: si no, lo de dentro se
            // atribuiría a la de fuera.
            if (mejor is null || raiz.Length > mejor.Raiz.Length + 1) mejor = new(r.Proveedor, raiz.TrimEnd(Path.DirectorySeparatorChar));
        }
        return mejor;
    }
}
