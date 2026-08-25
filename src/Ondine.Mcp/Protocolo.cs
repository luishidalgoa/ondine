using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ondine.Mcp;

/// <summary>
/// El transporte: JSON-RPC 2.0, un objeto por línea, por la entrada y la salida estándar.
///
/// <para>
/// Es lo que espera un cliente MCP por «stdio»: escribe una línea con la petición y lee una
/// línea con la respuesta. No hay cabeceras ni longitudes que contar.
/// </para>
/// <para>
/// <b>Nada se escribe en la salida estándar que no sea una respuesta.</b> Es la regla que más
/// fácil se rompe y la que deja el servidor inservible: un <c>Console.WriteLine</c> de
/// diagnóstico en medio se cuela como si fuera un mensaje del protocolo y el cliente se
/// desengancha. Lo que haya que contar va por la salida de ERROR, que nadie lee como
/// protocolo.
/// </para>
/// </summary>
internal static class Protocolo
{
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    public static void Escribir(JsonObject mensaje) =>
        Console.Out.WriteLine(mensaje.ToJsonString(Opts));

    /// <summary>Un aviso para quien mira los registros, nunca por la salida estándar.</summary>
    public static void Anotar(string linea) => Console.Error.WriteLine("[ondine-mcp] " + linea);

    public static JsonObject Respuesta(JsonNode? id, JsonNode? resultado) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["result"] = resultado,
    };

    /// <summary>
    /// Un error del PROTOCOLO: la petición no se entiende o el método no existe.
    ///
    /// <para>
    /// Distinto de una herramienta que falla. Eso se contesta como resultado con
    /// <c>isError</c>, porque el agente tiene que poder leerlo y reaccionar; un error de
    /// JSON-RPC, en cambio, dice que la conversación va mal.
    /// </para>
    /// </summary>
    public static JsonObject Fallo(JsonNode? id, int codigo, string mensaje) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject { ["code"] = codigo, ["message"] = mensaje },
    };
}
