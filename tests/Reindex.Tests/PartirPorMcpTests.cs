using System.Text.Json.Nodes;
using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Partir por MCP: los tiempos y lo que se niega a hacer.
///
/// <para>
/// El corte en sí lo hace <c>CortadorSinRecodificar</c>, que ya tiene sus pruebas. Lo que se
/// prueba aquí es la frontera: cómo se leen los tiempos que escribe un agente, y que no se
/// arranque a cortar cuando lo pedido no tiene sentido.
/// </para>
/// <para>
/// <b>Los tiempos importan más de lo que parece.</b> Un agente escribe «21:47» tanto como 1307,
/// y confundir minutos con segundos no da error: parte por otro sitio y el fichero sale mal
/// partido sin que nada chiste.
/// </para>
/// </summary>
public static class PartirPorMcpTests
{
    public static void Todas()
    {
        Program.Seccion("Partir por MCP");

        LosTiempos();
        LoQueNoSePuedePedir();
        ElEsquemaLoOfrece();
    }

    private static void LosTiempos()
    {
        Program.Assert(Partir.Momento("90") == 90, "un número son segundos");
        Program.Assert(Partir.Momento("1:30") == 90, "«1:30» es minuto y medio, no una hora y media");
        Program.Assert(Partir.Momento("0:00") == 0, "y el principio es cero");
        Program.Assert(Partir.Momento("1:00:00") == 3600, "«1:00:00» es una hora");
        Program.Assert(Partir.Momento("21:47") == 1307, "el corte del ejemplo real: 21 minutos y 47");
        Program.Assert(Partir.Momento("1:02:03.5") == 3723.5, "y los decimales cuentan");

        Program.Assert(Partir.Momento("") is null, "vacío no es un momento");
        Program.Assert(Partir.Momento("mañana") is null, "ni una palabra");
        Program.Assert(Partir.Momento("1:2:3:4") is null, "ni cuatro tramos, que no significan nada");

        // Y el reloj, de vuelta: es lo que se lee en la respuesta.
        Program.Assert(Partir.Reloj(1307) == "21:47", $"1307 s se enseña como 21:47 ({Partir.Reloj(1307)})");
        Program.Assert(Partir.Reloj(3723) == "1:02:03", $"y una hora larga con sus horas ({Partir.Reloj(3723)})");
        Program.Assert(Partir.Reloj(5) == "0:05", $"y cinco segundos no son «5» a secas ({Partir.Reloj(5)})");
    }

    /// <summary>
    /// Lo que no se puede pedir se dice antes de tocar el disco. Un fichero que no existe, un
    /// corte fuera del vídeo, las dos formas de pedirlo a la vez, o ninguna.
    /// </summary>
    private static void LoQueNoSePuedePedir()
    {
        var h = Catalogo.Todas.Single(x => x.Nombre == "ondine_partir");

        var sinFichero = h.Ejecutar(new JsonObject { ["cortes"] = new JsonArray("1:00") });
        Program.Assert(sinFichero.EsError && sinFichero.Texto.Contains("fichero"),
            "sin «fichero» no hay nada que partir");

        var noExiste = h.Ejecutar(new JsonObject
        {
            ["fichero"] = Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid().ToString("N") + ".mkv"),
            ["cortes"] = new JsonArray("1:00"),
        });
        Program.Assert(noExiste.EsError && noExiste.Texto.Contains("No existe"),
            $"un fichero que no está se dice ({Recorte(noExiste.Texto)})");
    }

    /// <summary>
    /// Y que el esquema ofrezca las dos formas de pedirlo, más lo que hace falta al recodificar.
    /// Sin «cortes» ni «desde»/«hasta» la herramienta no se puede usar para nada.
    /// </summary>
    private static void ElEsquemaLoOfrece()
    {
        var args = (Catalogo.Todas.Single(x => x.Nombre == "ondine_partir")
                    .Esquema["properties"] as JsonObject)!.Select(p => p.Key).ToHashSet();

        foreach (var suyo in new[] { "fichero", "cortes", "desde", "hasta", "salida",
                                     "sin_recodificar", "confirmar" })
            Program.Assert(args.Contains(suyo), $"«{suyo}» está en el esquema");

        // Recodificando hacen falta los mandos de codificación, o se recodifica a ciegas con lo
        // que haya por defecto.
        foreach (var suyo in new[] { "formato", "codec", "codificador", "calidad", "esmero" })
            Program.Assert(args.Contains(suyo), $"y «{suyo}», para cuando se recodifica");
    }

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 70 ? l[..70] + "…" : l;
}
