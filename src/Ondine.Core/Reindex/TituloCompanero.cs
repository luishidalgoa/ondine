using System;
using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// El título que trae el <c>.nfo</c> de al lado del vídeo.
///
/// <para>
/// Se lee de <b>todos</b> los ficheros antes de identificar, y no solo de los que
/// ya han quedado en duda. La diferencia importa: un fichero puede quedar
/// <b>seguro y equivocado</b> —identificado por su número contra el episodio que
/// no era—, y entonces nadie llega a mirar el <c>.nfo</c> que lo habría
/// desmentido. Pasó: un <c>S01E534.mp4</c> se dio por el episodio 534 cuando su
/// <c>.nfo</c> decía «¡Kasukabetti Western!», que es el 497.
/// </para>
/// <para>
/// Antes se leía solo de los dudosos por no pagar el coste en toda la carpeta.
/// Medido, ese coste no existe: <b>91 ms los 59 ficheros</b> de una carpeta real,
/// 12 KB en total. Un <c>.nfo</c> es un XML de un párrafo — nada que ver con
/// sondear el vídeo, que en un fichero de nube se lo descarga entero.
/// </para>
/// <para>
/// Y cuando lo hay, sirve: en esa misma carpeta los 53 ficheros que quedaban en
/// duda tenían <c>.nfo</c>, y los 53 dieron título.
/// </para>
/// </summary>
public static class TituloCompanero
{
    /// <summary>
    /// El título del <c>.nfo</c> con el mismo nombre que el vídeo, o null si no lo
    /// hay, no se puede leer o no dice nada.
    ///
    /// <para>
    /// No lanza nunca. Que un fichero suelto tenga el <c>.nfo</c> corrupto, a medio
    /// sincronizar o sin permisos no puede tumbar la identificación de la carpeta
    /// entera: como mucho, ese se queda sin la ayuda y sigue el camino de siempre.
    /// </para>
    /// </summary>
    public static string? Leer(string? rutaVideo)
    {
        if (string.IsNullOrWhiteSpace(rutaVideo)) return null;
        try
        {
            var nfo = Path.ChangeExtension(rutaVideo, ".nfo");
            return File.Exists(nfo) ? NfoTitulo.Extraer(File.ReadAllText(nfo)) : null;
        }
        catch { return null; }
    }
}
