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
}
