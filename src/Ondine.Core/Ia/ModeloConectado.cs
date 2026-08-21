using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ondine.Ia;

/// <summary>
/// Habla con un modelo de lenguaje por API.
///
/// <para>
/// Se usa el <b>estándar de OpenAI</b> (<c>POST /chat/completions</c>) y no un
/// protocolo propio, porque es el que hablan casi todos: OpenAI, Groq, Together,
/// OpenRouter, LM Studio y Ollama —este último desde su ruta <c>/v1</c>—. Así el
/// ajuste es «pega tu dirección, tu clave y el nombre del modelo» en vez de una
/// lista de proveedores que hay que ir ampliando cada vez que sale uno nuevo.
/// </para>
/// <para>
/// Esta clase <b>no decide nada</b> sobre lo que el modelo conteste. Pregunta y
/// devuelve el texto. Quién se fía de esa respuesta, y contra qué la comprueba,
/// es cosa de quien pregunta.
/// </para>
/// </summary>
public static class ModeloConectado
{
    private static readonly HttpClient Http = Cliente();

    private static HttpClient Cliente()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("Ondine");
        return h;
    }

    private const string Ruta = "/chat/completions";

    /// <summary>
    /// La dirección a la que se llama de verdad, o <c>null</c> si lo escrito no
    /// es una dirección.
    ///
    /// <para>
    /// Existe porque esta dirección se copia de sitios que la escriben distinto:
    /// la documentación de OpenAI dice <c>/v1</c>, la de Ollama
    /// <c>:11434/v1</c>, y quien copia del ejemplo de <c>curl</c> se trae la
    /// ruta entera. Concatenar sin mirar da <c>/chat/completions/chat/completions</c>
    /// y un 404 que nadie sabe de dónde salió.
    /// </para>
    /// <para>
    /// Lo que NO hace: añadir <c>/v1</c> por su cuenta. Hay servidores que
    /// sirven la API en la raíz, y meter un trozo de ruta inventado convierte un
    /// ajuste mal escrito en un fallo que parece del servidor.
    /// </para>
    /// </summary>
    public static string? Endpoint(string? baseUrl)
    {
        var s = (baseUrl ?? "").Trim().TrimEnd('/');
        if (s.Length == 0) return null;
        if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return null;
        if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return null;

        return s.EndsWith(Ruta, StringComparison.OrdinalIgnoreCase) ? s : s + Ruta;
    }

    /// <summary>
    /// ¿Se le puede mandar la clave a esta dirección?
    ///
    /// <para>
    /// Una clave de API por <c>http://</c> viaja legible para cualquiera que
    /// esté en el camino. Con un modelo en la propia máquina no hay camino —no
    /// sale de ella—, y exigir HTTPS ahí obligaría a montar certificados para
    /// usar Ollama en casa. Por eso se distingue, y por eso <b>otra máquina de
    /// la red de casa NO cuenta como local</b>: ahí sí hay camino, aunque sea
    /// corto y de confianza.
    /// </para>
    /// </summary>
    public static bool PuedeLlevarClave(string? baseUrl)
    {
        var s = (baseUrl ?? "").Trim();
        if (!Uri.TryCreate(s, UriKind.Absolute, out var u)) return false;
        if (u.Scheme == Uri.UriSchemeHttps) return true;
        if (u.Scheme != Uri.UriSchemeHttp) return false;
        return u.IsLoopback;
    }

    /// <summary>
    /// El texto que contestó el modelo, o <c>null</c> si la respuesta no trae
    /// ninguno —esté vacía, sea un error, o no sea ni JSON—.
    ///
    /// <para>
    /// Todo lo raro sale como <c>null</c> y no como excepción. Un modelo que
    /// contesta de forma inesperada no debe tumbar la pantalla desde la que se
    /// le preguntó: como mucho, no aporta nada.
    /// </para>
    /// </summary>
    public static string? LeerRespuesta(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0) return null;

            if (!choices[0].TryGetProperty("message", out var msg) ||
                !msg.TryGetProperty("content", out var c) ||
                c.ValueKind != JsonValueKind.String) return null;

            var texto = c.GetString();
            return string.IsNullOrWhiteSpace(texto) ? null : texto;
        }
        catch { return null; }
    }

    /// <param name="Texto">Lo que contestó, si contestó.</param>
    /// <param name="Error">Por qué no, si no. Uno de los dos siempre es null.</param>
    public sealed record Contestacion(string? Texto, string? Error);

    /// <summary>
    /// Le pregunta al modelo y devuelve lo que diga.
    ///
    /// <para>
    /// <paramref name="temperatura"/> va a cero por defecto: aquí no se quiere
    /// que el modelo sea creativo, se quiere que dé la misma respuesta ante la
    /// misma entrada. Una sugerencia que cambia entre dos ejecuciones no se
    /// puede comprobar ni reproducir.
    /// </para>
    /// </summary>
    public static async Task<Contestacion> PreguntarAsync(
        AjustesDeModelo ajustes, string sistema, string pregunta,
        double temperatura = 0, CancellationToken corte = default)
    {
        if (Endpoint(ajustes.BaseUrl) is not { } url)
            return new(null, Localizacion.Textos.Instancia.IaDireccionInvalida);
        if (string.IsNullOrWhiteSpace(ajustes.Modelo))
            return new(null, Localizacion.Textos.Instancia.IaSinModelo);

        var clave = ajustes.Clave();

        // La comprobación se hace AQUÍ, en el único sitio por el que pasa la
        // clave, y no en la pantalla de ajustes. Una regla de seguridad que vive
        // en la interfaz se salta la próxima vez que alguien llame desde otro
        // sitio; esta no se puede saltar sin querer.
        if (!string.IsNullOrEmpty(clave) && !PuedeLlevarClave(ajustes.BaseUrl))
            return new(null, Localizacion.Textos.Instancia.IaClavePorHttp);

        var cuerpo = JsonSerializer.Serialize(new
        {
            model = ajustes.Modelo,
            temperature = temperatura,
            messages = new[]
            {
                new { role = "system", content = sistema },
                new { role = "user", content = pregunta },
            },
        });

        try
        {
            using var pet = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
            };
            if (!string.IsNullOrEmpty(clave))
                pet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clave);

            using var resp = await Http.SendAsync(pet, corte).ConfigureAwait(false);
            var texto = await resp.Content.ReadAsStringAsync(corte).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return new(null, string.Format(Localizacion.Textos.Instancia.IaRespondioMal,
                                               (int)resp.StatusCode, Recortar(texto)));

            return LeerRespuesta(texto) is { } r
                ? new(r, null)
                : new(null, Localizacion.Textos.Instancia.IaRespuestaIlegible);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return new(null, ex.Message);
        }
    }

    /// <summary>
    /// La prueba del botón «Probar conexión»: le manda lo más corto que se puede
    /// mandar. Vale más que preguntar la lista de modelos, porque comprueba
    /// justo lo que se va a usar —ese modelo, con esa clave— y no solo que el
    /// servidor esté levantado.
    /// </summary>
    public static async Task<Contestacion> ProbarAsync(
        AjustesDeModelo ajustes, CancellationToken corte = default) =>
        await PreguntarAsync(ajustes,
            "Responde exactamente: OK",
            "Di OK.", corte: corte).ConfigureAwait(false);

    // El cuerpo de un error puede venir con una página entera dentro. Se recorta
    // para que quepa en un rótulo sin tapar la pantalla.
    private static string Recortar(string s)
    {
        s = (s ?? "").Trim().ReplaceLineEndings(" ");
        return s.Length <= 200 ? s : s[..200] + "…";
    }
}
