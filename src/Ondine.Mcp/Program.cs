using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ondine.Mcp;

/// <summary>
/// El servidor MCP de Ondine: un bucle que lee peticiones por la entrada estándar y contesta
/// por la salida.
///
/// <para>
/// <b>Para qué:</b> que un agente pueda usar Ondine. Y sobre el MOTOR y no sobre la interfaz —
/// conducir la ventana a golpe de clic simulado sería frágil, lento y ciego. Aquí se llama al
/// mismo <c>Ondine.Core</c> que usan las dos interfaces, así que el agente hace lo mismo que un
/// humano por el mismo camino, con las mismas reglas.
/// </para>
/// <para>
/// <b>Cómo se prueba a mano</b>, que es lo primero que hace falta al tocar esto:
/// </para>
/// <code>
/// echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | ondine-mcp
/// </code>
/// </summary>
internal static class Program
{
    /// <summary>
    /// La versión del protocolo que se declara. Es una fecha porque MCP versiona así.
    /// </summary>
    private const string VersionDelProtocolo = "2024-11-05";

    private static int Main(string[] argumentos)
    {
        // La consola en UTF-8 de forma explícita: los títulos de episodio llevan acentos y
        // eñes, y en Windows la consola arranca en una página de códigos que se los come. Un
        // JSON con caracteres roídos no lo parsea nadie.
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = new UTF8Encoding(false);

        // El idioma que el usuario eligio en la aplicacion, igual que hacen las dos interfaces.
        // Sin esta linea el motor contestaba en ingles mientras el servidor hablaba castellano,
        // mezclados en la misma respuesta: «Analisis (no se ha tocado nada)» y debajo «The title
        // matches 100%». Lo lee el agente del usuario, asi que el idioma tambien le toca a el.
        Ondine.Localizacion.Idioma.Actual =
            Ondine.Localizacion.Idioma.Resolver(SettingsStore.Load().Idioma);

        if (argumentos.Contains("--herramientas"))
        {
            // Un atajo para mirarlo desde un terminal sin hablar JSON-RPC.
            foreach (var h in Catalogo.Todas)
                Console.WriteLine($"{h.Nombre}{(h.Escribe ? "  [escribe]" : "")}\n    {h.Descripcion}\n");
            return 0;
        }

        Protocolo.Anotar($"listo · {Catalogo.Todas.Count} herramientas · motor {Updater.Current}");

        string? linea;
        while ((linea = Console.In.ReadLine()) is not null)
        {
            if (linea.Trim().Length == 0) continue;

            JsonObject? peticion;
            try { peticion = JsonNode.Parse(linea) as JsonObject; }
            catch (JsonException ex)
            {
                Protocolo.Escribir(Protocolo.Fallo(null, -32700, "JSON ilegible: " + ex.Message));
                continue;
            }

            if (peticion is null)
            {
                Protocolo.Escribir(Protocolo.Fallo(null, -32600, "Se esperaba un objeto JSON-RPC."));
                continue;
            }

            try { Atender(peticion); }
            catch (Exception ex)
            {
                // Que una herramienta reviente no puede tumbar el servidor: el cliente se
                // quedaría esperando una respuesta que no llega y sin saber por qué.
                Protocolo.Anotar("reventó atendiendo: " + ex);
                Protocolo.Escribir(Protocolo.Fallo(peticion["id"], -32603, ex.Message));
            }
        }

        return 0;
    }

    private static void Atender(JsonObject peticion)
    {
        var metodo = peticion["method"]?.ToString() ?? "";
        var id = peticion["id"];

        // Sin id es una NOTIFICACIÓN y no se contesta. Contestarla es un error del protocolo, y
        // algunos clientes se desenganchan al recibir una respuesta que no esperaban.
        bool esNotificacion = id is null;

        switch (metodo)
        {
            case "initialize":
                Protocolo.Escribir(Protocolo.Respuesta(id, new JsonObject
                {
                    ["protocolVersion"] = VersionDelProtocolo,
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "ondine",
                        ["version"] = Updater.Current.ToString(),
                    },
                    // Lo que el agente lee antes de nada. Aquí van las reglas, porque son la
                    // diferencia entre un ayudante útil y uno que renombra media biblioteca.
                    ["instructions"] =
                        "Ondine prepara bibliotecas de series y películas para Plex y Jellyfin.\n\n"
                      + "TRES REGLAS, y son las mismas que tiene una persona delante:\n"
                      + "1. «analizar» PROPONE y no toca nada. Léelo antes de aplicar.\n"
                      + "2. Lo que escribe pide \"confirmar\": true. Sin él te dice lo que haría.\n"
                      + "3. Lo borrado va a la papelera del sistema, nunca se borra de verdad.\n\n"
                      + "Las dudas NO se aplican en bloque: si el análisis deja filas dudosas, "
                      + "eso se resuelve en la aplicación, con la persona delante.",
                }));
                break;

            case "notifications/initialized":
            case "notifications/cancelled":
                // Nada que hacer, y nada que contestar.
                break;

            case "ping":
                if (!esNotificacion) Protocolo.Escribir(Protocolo.Respuesta(id, new JsonObject()));
                break;

            case "tools/list":
                Protocolo.Escribir(Protocolo.Respuesta(id, new JsonObject
                {
                    ["tools"] = new JsonArray(Catalogo.Todas.Select(h => (JsonNode)new JsonObject
                    {
                        ["name"] = h.Nombre,
                        ["description"] = h.Descripcion,
                        ["inputSchema"] = h.Esquema.DeepClone(),
                    }).ToArray()),
                }));
                break;

            case "tools/call":
                Protocolo.Escribir(Llamar(peticion, id));
                break;

            default:
                if (!esNotificacion)
                    Protocolo.Escribir(Protocolo.Fallo(id, -32601, $"Método desconocido: {metodo}"));
                break;
        }
    }

    /// <summary>
    /// Ejecuta una herramienta.
    ///
    /// <para>
    /// Un fallo de la herramienta se contesta como RESULTADO con <c>isError</c>, no como error
    /// de JSON-RPC. La diferencia importa: un resultado con error lo lee el agente y puede
    /// reaccionar —cambiar la ruta, pedir permiso—; un error de protocolo dice que la
    /// conversación va mal, y ahí no hay nada que reaccionar.
    /// </para>
    /// </summary>
    private static JsonObject Llamar(JsonObject peticion, JsonNode? id)
    {
        var parametros = peticion["params"] as JsonObject;
        var nombre = parametros?["name"]?.ToString() ?? "";
        var argumentos = parametros?["arguments"] as JsonObject ?? new JsonObject();

        var herramienta = Catalogo.Todas.FirstOrDefault(h => h.Nombre == nombre);
        if (herramienta is null)
            return Protocolo.Fallo(id, -32602,
                $"No existe la herramienta «{nombre}». Las que hay: "
                + string.Join(", ", Catalogo.Todas.Select(h => h.Nombre)));

        Protocolo.Anotar($"llamando a {nombre}");

        Resultado resultado;
        try { resultado = herramienta.Ejecutar(argumentos); }
        catch (Exception ex) { resultado = Resultado.Error($"{ex.GetType().Name}: {ex.Message}"); }

        return Protocolo.Respuesta(id, new JsonObject
        {
            ["content"] = new JsonArray(new JsonObject
            {
                ["type"] = "text",
                ["text"] = resultado.Texto,
            }),
            ["isError"] = resultado.EsError,
        });
    }
}
