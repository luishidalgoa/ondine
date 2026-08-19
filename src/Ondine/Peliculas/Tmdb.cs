using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ondine.Peliculas;

/// <summary>
/// Preguntar a TMDb qué película es esta.
///
/// <para>
/// Esta clase <b>no decide nada</b>: pregunta y devuelve lo que le han dicho.
/// Quién se fía de cuál de esos candidatos, y con qué señales, es cosa de
/// <see cref="IdentificacionDePelicula"/>. Es el mismo reparto que con el modelo
/// de lenguaje, y por el mismo motivo: mezclar «qué contestó» con «me lo creo»
/// deja la decisión escondida dentro del transporte.
/// </para>
/// <para>
/// Lo que sale de esta máquina es el <b>título ya limpio y el año</b>. No el
/// nombre del fichero: la resolución, el códec y el nombre del grupo de release
/// no hacen falta para identificar nada y dicen de dónde salió el fichero. Es la
/// diferencia entre consultar una ficha y contar tu biblioteca.
/// </para>
/// </summary>
public static class Tmdb
{
    /// <summary>Una película que TMDb propone como respuesta a la búsqueda.</summary>
    /// <param name="Titulo">En el idioma que se pidió: es el que se escribirá en el disco.</param>
    /// <param name="Original">El del país de origen. Media biblioteca está nombrada con este.</param>
    public sealed record Candidato(int Id, string Titulo, string? Original, int? Anio);

    public const string Raiz = "https://api.themoviedb.org/3";

    private static readonly HttpClient Http = Cliente();

    private static HttpClient Cliente()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("Ondine");
        return h;
    }

    /// <summary>
    /// La URL de la búsqueda. Sin la clave: esa la pone
    /// <see cref="Peticion"/>, que es quien sabe por dónde tiene que viajar.
    /// </summary>
    public static string Url(string titulo, int? anio, string idioma)
    {
        var u = new StringBuilder(Raiz);
        u.Append("/search/movie?query=").Append(Uri.EscapeDataString((titulo ?? "").Trim()));
        u.Append("&include_adult=false");

        if (!string.IsNullOrWhiteSpace(idioma))
            u.Append("&language=").Append(Uri.EscapeDataString(idioma.Trim()));

        // Sin año NO se manda el parámetro. TMDb toma «year=» como filtro y una
        // búsqueda filtrada por el año vacío no devuelve nada.
        if (anio is { } a) u.Append("&year=").Append(a);

        return u.ToString();
    }

    /// <summary>
    /// La petición, con la clave puesta por donde corresponda.
    ///
    /// <para>
    /// TMDb da <b>dos credenciales distintas en la misma página de ajustes</b>:
    /// la «API Key (v3 auth)», que va como parámetro, y el «API Read Access
    /// Token (v4 auth)», que es un JWT y va en la cabecera. Quien pega la que no
    /// toca recibe un 401 pelado, así que se distingue por la forma en vez de
    /// obligar a acertar. Y un token nunca acaba en la URL: las URL se quedan
    /// escritas en los registros de cualquier proxy del camino.
    /// </para>
    /// </summary>
    public static HttpRequestMessage Peticion(string url, string clave)
    {
        var k = (clave ?? "").Trim();
        var token = EsTokenV4(k);

        var final = token || k.Length == 0
            ? url
            : url + (url.Contains('?') ? "&" : "?") + "api_key=" + Uri.EscapeDataString(k);

        var p = new HttpRequestMessage(HttpMethod.Get, final);
        p.Headers.Accept.ParseAdd("application/json");
        if (token) p.Headers.Authorization = new AuthenticationHeaderValue("Bearer", k);
        return p;
    }

    /// <summary>Si esto tiene la forma de un token v4 (un JWT) y no de una clave v3.</summary>
    public static bool EsTokenV4(string? clave)
    {
        var s = (clave ?? "").Trim();
        return s.StartsWith("ey", StringComparison.Ordinal) && s.Count(c => c == '.') == 2;
    }

    /// <summary>
    /// Los candidatos que trae una respuesta. Todo lo raro sale como lista
    /// vacía y no como excepción: una respuesta que no se entiende es «no sé»,
    /// no un fallo que se lleve por delante la ventana.
    /// </summary>
    public static IReadOnlyList<Candidato> Leer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<Candidato>();

        try
        {
            using var d = JsonDocument.Parse(json);
            if (!d.RootElement.TryGetProperty("results", out var r) || r.ValueKind != JsonValueKind.Array)
                return Array.Empty<Candidato>();

            var lista = new List<Candidato>();
            foreach (var e in r.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object) continue;

                var id = e.TryGetProperty("id", out var i) && i.TryGetInt32(out var n) ? n : 0;
                var titulo = Texto(e, "title");
                if (id == 0 || titulo.Length == 0) continue;

                var original = Texto(e, "original_title");
                lista.Add(new(id, titulo, original.Length == 0 ? null : original,
                              AnioDe(Texto(e, "release_date"))));
            }
            return lista;
        }
        catch { return Array.Empty<Candidato>(); }
    }

    private static string Texto(JsonElement e, string campo)
        => e.TryGetProperty(campo, out var v) && v.ValueKind == JsonValueKind.String
            ? (v.GetString() ?? "").Trim()
            : "";

    /// <summary>
    /// El año de una fecha de estreno. TMDb manda la fecha vacía más de lo que
    /// parece, y eso es «no se sabe» — nunca el año cero.
    /// </summary>
    private static int? AnioDe(string fecha)
        => fecha.Length >= 4 && int.TryParse(fecha[..4], out var a) && a > 1000 ? a : null;

    /// <summary>
    /// Pregunta de verdad. Devuelve <c>null</c> si <b>no se pudo preguntar</b> —
    /// sin red, clave rechazada, servidor caído— y una lista vacía si se
    /// preguntó y no había nada.
    ///
    /// <para>
    /// La diferencia importa: un «no se pudo» no se guarda en la caché como si
    /// fuera un «no existe», o un rato sin conexión dejaría la película marcada
    /// como imposible para siempre.
    /// </para>
    /// </summary>
    public static async Task<IReadOnlyList<Candidato>?> Preguntar(
        string titulo, int? anio, string idioma, string clave, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(clave)) return null;

        try
        {
            using var p = Peticion(Url(titulo, anio, idioma), clave);
            using var r = await Http.SendAsync(p, ct).ConfigureAwait(false);
            if (!r.IsSuccessStatusCode) return null;

            return Leer(await r.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
}
