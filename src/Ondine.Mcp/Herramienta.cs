using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ondine.Mcp;

/// <summary>
/// Una herramienta que el agente puede llamar: cómo se llama, qué hace, qué argumentos toma y
/// qué pasa al ejecutarla.
///
/// <para>
/// El esquema va escrito a mano y no generado de una clase, y es a propósito: es lo ÚNICO que
/// el agente lee antes de decidir si llamar. Una descripción floja se paga en llamadas mal
/// hechas, así que aquí se escribe para que se lea, no para que compile.
/// </para>
/// </summary>
internal sealed record Herramienta(
    string Nombre,
    string Descripcion,
    JsonObject Esquema,
    bool Escribe,
    Func<JsonObject, Resultado> Ejecutar);

/// <summary>
/// Lo que devuelve una herramienta.
///
/// <para>
/// Texto y no una estructura, porque es lo que un modelo va a leer. Los datos van dentro en
/// forma legible —una tabla, una lista— en vez de un JSON que habría que volver a explicarle.
/// </para>
/// </summary>
internal sealed record Resultado(string Texto, bool EsError = false)
{
    public static Resultado Ok(string texto) => new(texto);
    public static Resultado Error(string texto) => new(texto, true);

    /// <summary>
    /// La respuesta de una herramienta que escribe y a la que no se le ha dado permiso: dice
    /// exactamente lo que haría.
    ///
    /// <para>
    /// No es un error ni un aviso: es la respuesta útil. Un agente que pregunta «qué pasaría»
    /// merece la lista entera, y quien lea la conversación después puede juzgar si el permiso
    /// estaba bien dado.
    /// </para>
    /// </summary>
    public static Resultado Ensayo(string queHaria) =>
        new("SIN CONFIRMAR — no se ha tocado nada. Esto es lo que haría:\n\n" + queHaria +
            "\n\nPara hacerlo de verdad, vuelve a llamar con \"confirmar\": true.");
}
