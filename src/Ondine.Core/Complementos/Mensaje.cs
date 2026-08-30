using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ondine.Complementos;

/// <summary>
/// Una línea de las que escribe un complemento por su salida estándar.
///
/// <para>
/// Una línea, un mensaje, JSON. Se eligió así frente a devolver un solo JSON al
/// final por una razón concreta: traer cuarenta vídeos tarda minutos, y con una
/// única respuesta al final la aplicación se queda muda todo ese rato. Con una
/// línea por suceso se puede ir pintando el avance según llega, y un complemento
/// que se cuelga a mitad deja ver hasta dónde iba.
/// </para>
/// <para>
/// Lo que NO sea JSON válido se ignora en silencio. Los programas que se
/// envuelven -yt-dlp y compañía- escriben avisos por su cuenta, y un complemento
/// no debería romperse porque la herramienta que usa por dentro sea habladora.
/// </para>
/// </summary>
public sealed class Mensaje
{
    /// <summary>Un elemento de la fuente: lo que se enseña con su casilla.</summary>
    public const string TipoElemento = "elemento";
    /// <summary>Avance de una descarga en curso.</summary>
    public const string TipoProgreso = "progreso";
    /// <summary>Terminado, con lo que haya dejado en disco.</summary>
    public const string TipoHecho = "hecho";
    /// <summary>Algo salió mal, con su explicación.</summary>
    public const string TipoError = "error";

    /// <summary>
    /// El complemento le pregunta al modelo de lenguaje, si es que quien lo
    /// instaló le ha dado permiso. La pregunta va en <see cref="Texto"/> y el
    /// <see cref="Id"/> sirve para casar la respuesta.
    ///
    /// <para>
    /// Va por aquí y no dándole la clave al complemento a propósito: el
    /// complemento es un programa de fuera. Ver <c>PuenteDelModelo</c>.
    /// </para>
    /// </summary>
    public const string TipoPreguntar = "preguntar";

    /// <summary>
    /// La contestación de Ondine, que se le escribe al complemento por su
    /// ENTRADA estándar. Trae <see cref="Texto"/> o
    /// <see cref="MensajeError"/> —el motivo de que no—, nunca los dos.
    /// </summary>
    public const string TipoRespuesta = "respuesta";

    [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";

    // ── elemento ────────────────────────────────────────────────────────────
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("miniatura")] public string? Miniatura { get; set; }
    /// <summary>Segundos. En segundos y no en texto: formatear es cosa de quien pinta.</summary>
    [JsonPropertyName("duracion")] public double? Duracion { get; set; }

    // ── progreso ────────────────────────────────────────────────────────────
    /// <summary>De 0 a 1. Fuera de ese rango se recorta al pintarlo, no aquí.</summary>
    [JsonPropertyName("avance")] public double? Avance { get; set; }
    [JsonPropertyName("texto")] public string? Texto { get; set; }

    // ── hecho ───────────────────────────────────────────────────────────────
    /// <summary>Lo que dejó en disco, para poder llevarlo a Organizar sin buscarlo.</summary>
    [JsonPropertyName("ficheros")] public List<string> Ficheros { get; set; } = new();

    // ── error ───────────────────────────────────────────────────────────────
    [JsonPropertyName("mensaje")] public string? MensajeError { get; set; }

    /// <summary>
    /// Interpreta una línea. <c>null</c> si no es un mensaje del contrato.
    ///
    /// <para>
    /// Se descarta tanto lo que no es JSON como lo que es JSON pero no dice de
    /// qué tipo es: un mensaje sin tipo no se puede atender, y adivinarlo por los
    /// campos que trae sería inventarse el contrato en tiempo de ejecución.
    /// </para>
    /// </summary>
    public static Mensaje? Interpretar(string? linea)
    {
        if (string.IsNullOrWhiteSpace(linea)) return null;

        var recortada = linea.Trim();
        // Se mira antes de intentar el JSON: la salida de estas herramientas está
        // llena de líneas de texto normal, y provocar una excepción por cada una
        // para descartarla cuesta más que mirar el primer carácter.
        if (recortada[0] != '{') return null;

        try
        {
            var m = JsonSerializer.Deserialize<Mensaje>(recortada);
            return string.IsNullOrWhiteSpace(m?.Tipo) ? null : m;
        }
        catch { return null; }
    }

    public TimeSpan? ComoDuracion =>
        Duracion is > 0 ? TimeSpan.FromSeconds(Duracion.Value) : null;

    /// <summary>
    /// El avance, dejado en un número que se puede pintar: entre 0 y 1, o nada.
    ///
    /// <para>
    /// El recorte a 0..1 ya estaba donde se pinta. Lo que faltaba: <b><c>Math.Clamp</c> no arregla
    /// un NaN</b>. Las comparaciones con NaN son todas falsas, así que el recorte lo deja pasar tal
    /// cual y llega a la barra de progreso. Y para mandarlo no hace falta mala idea — basta con
    /// que el complemento divida entre cero calculando su porcentaje, que es lo que pasa cuando la
    /// fuente no dice cuántos elementos trae.
    /// </para>
    /// <para>
    /// Vive aquí y no en cada pantalla porque hay dos, y arreglar una y dejar la otra es la forma
    /// habitual de que esto vuelva.
    /// </para>
    /// </summary>
    public double? AvanceSano => Avance is not { } a
        ? null
        : double.IsNaN(a) ? 0 : Math.Clamp(a, 0, 1);

    /// <summary>
    /// De los ficheros que un complemento dice haber traído, <b>los que están de verdad dentro de
    /// la carpeta que eligió el usuario</b>.
    ///
    /// <para>
    /// <b>Esto no es una comprobación de formato: es la frontera.</b> Lo que el complemento
    /// escribe por su salida no es un dato de la aplicación, es la afirmación de un tercero — y
    /// estas rutas no se enseñan y ya, entran en el flujo de Organizar, que <b>renombra y mueve</b>.
    /// Un complemento que contestara con la ruta de un documento tuyo lo colaba en la lista de lo
    /// recién descargado y a partir de ahí se le trataba como a un capítulo más. No hacía falta
    /// ningún fallo del sistema: bastaba con escribir esa línea.
    /// </para>
    /// <para>
    /// <b>Sin carpeta de destino no se le cree ninguno.</b> Un complemento al que nadie le dijo
    /// dónde dejar las cosas no tiene ficheros que declarar.
    /// </para>
    /// <para>
    /// Se descartan en silencio a propósito. Avisar con un error abortaría la importación de los
    /// que sí están bien —así trata la pantalla un error—, y quien manda rutas de fuera es un
    /// complemento roto o con mala idea, no un usuario que pueda corregir nada.
    /// </para>
    /// </summary>
    public static List<string> SoloDentroDe(string? destino, IEnumerable<string>? ficheros)
    {
        var buenos = new List<string>();
        if (string.IsNullOrWhiteSpace(destino) || ficheros is null) return buenos;

        string dentro;
        try { dentro = Path.GetFullPath(destino) + Path.DirectorySeparatorChar; }
        catch { return buenos; }

        foreach (var f in ficheros)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;
            try
            {
                // Lo relativo se resuelve CONTRA EL DESTINO, no contra la carpeta desde la que
                // arrancó la aplicación. Un complemento al que se le dijo «déjalo en X» y
                // contesta «uno.mkv» está diciendo «X/uno.mkv», que es lo que quiere decir
                // cualquiera. Resolverlo contra otra cosa descartaba ficheros buenos.
                //
                // Y luego resuelta, que es donde se ve lo que es: «../otro.mkv» solo se delata
                // después de combinarla. Con el separador al final, o «serie-de-otro» pasaría por
                // estar dentro de «serie».
                var suya = Path.GetFullPath(Path.Combine(destino, f));
                if (suya.StartsWith(dentro, StringComparison.OrdinalIgnoreCase)) buenos.Add(suya);
            }
            catch { /* lo que no es una ruta no es un fichero traído */ }
        }
        return buenos;
    }
}
