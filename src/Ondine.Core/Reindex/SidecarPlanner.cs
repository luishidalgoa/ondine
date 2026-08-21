using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Los ficheros compañeros de un vídeo (.nfo, .srt, carátulas con el mismo nombre) tienen
/// que viajar con él al renombrar: si el vídeo cambia de nombre y su .nfo se queda con el
/// viejo, el reproductor de biblioteca deja de asociarlos y el metadato se vuelve huérfano.
///
/// Función pura: entra (origen, destino, lo que hay en la carpeta), salen los pares a mover.
/// El disco lo toca quien aplica, no esto.
/// </summary>
public static class SidecarPlanner
{
    /// <summary>
    /// Las extensiones que NUNCA son un compañero. Vive aquí y no en <c>Engine</c>
    /// porque esto tiene que decidirse sin WPF ni ffmpeg —el arnés de pruebas
    /// enlaza esta carpeta y no aquella—, y porque la pregunta que se hace aquí
    /// no es «qué sabe abrir la app» sino «qué NO puede ser un subtítulo».
    /// </summary>
    private static readonly string[] Videos =
        { ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".webm", ".mpg", ".mpeg", ".flv", ".ts", ".m2ts" };

    /// <summary>
    /// Compañero = mismo nombre base que el vídeo + «.» + lo que sea («.nfo», «.es.srt»).
    /// El sufijo se conserva entero: un subtítulo «.es.srt» sigue siendo «.es.srt».
    ///
    /// <para>
    /// Con una excepción que costó caro: <b>otro vídeo nunca es compañero</b>. Hay
    /// nombres muy normales en los que el nombre base de un vídeo es prefijo del
    /// de otro —«Up.mkv» y «Up.2009.mkv» en la misma carpeta—, y sin esta regla el
    /// segundo viajaba como si fuera un subtítulo: acababa dentro de la carpeta de
    /// una película que no es la suya. Peor aún, se movía <b>a espaldas del
    /// plan</b>: una fila marcada «no lo toco» se movía igual por ser compañera de
    /// otra, y el pie de la ventana promete que nada se ha tocado.
    /// </para>
    /// </summary>
    public static List<(string De, string A)> Planear(
        string origenVideo, string destinoVideo, IEnumerable<string> enCarpeta)
    {
        var plan = new List<(string, string)>();
        if (string.Equals(origenVideo, destinoVideo, StringComparison.OrdinalIgnoreCase))
            return plan;   // el vídeo no se mueve: sus compañeros tampoco

        var baseOrigen = SinExtensionDeVideo(origenVideo);
        var baseDestino = SinExtensionDeVideo(destinoVideo);
        var carpetaDestino = Path.GetDirectoryName(destinoVideo) ?? "";

        foreach (var f in enCarpeta)
        {
            if (string.Equals(f, origenVideo, StringComparison.OrdinalIgnoreCase)) continue;

            var nombre = Path.GetFileName(f);
            if (!nombre.StartsWith(baseOrigen + ".", StringComparison.OrdinalIgnoreCase)) continue;

            // Otro vídeo es una película o un capítulo, no un acompañante suyo.
            if (Videos.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)) continue;

            var sufijo = nombre[baseOrigen.Length..];   // «.nfo», «.es.srt»…
            plan.Add((f, Path.Combine(carpetaDestino, baseDestino + sufijo)));
        }
        return plan;
    }

    private static string SinExtensionDeVideo(string ruta) =>
        Path.GetFileNameWithoutExtension(ruta);
}
