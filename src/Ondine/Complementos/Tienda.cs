using System.Net.Http;
using System.Threading;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// De dónde salen los complementos que se pueden instalar.
///
/// <para>
/// <b>Hoy el índice lo publica el propio proyecto y no acepta a nadie más.</b>
/// Eso es una decisión de confianza, no una limitación técnica: quien escriba en
/// ese fichero decide qué se ejecuta en el equipo de quien instale. Se puede
/// abrir a que publique cualquiera sin tocar nada de aquí -el <c>sha256</c> ya es
/// obligatorio y ya se verifica-, pero abrirlo es fácil y cerrarlo no.
/// </para>
/// <para>
/// La dirección se puede cambiar por si alguien quiere el suyo, y por eso el
/// aviso está donde se pone: apuntar a otro índice es confiar en quien lo
/// mantenga tanto como en el proyecto.
/// </para>
/// </summary>
public static class Tienda
{
    /// <summary>El índice oficial. Se sirve del propio repositorio, sin infraestructura aparte.</summary>
    public const string IndiceOficial =
        "https://raw.githubusercontent.com/luishidalgoa/ondine/main/complementos/indice.json";

    /// <summary>
    /// Lo más grande que se acepta descargar. Un límite existe porque sin él una
    /// respuesta enorme -por error o a propósito- se traga la memoria entera
    /// antes de que a nadie le dé tiempo a cancelar.
    /// </summary>
    public const long MaximoBytes = 80L * 1024 * 1024;

    private static readonly HttpClient Http = Cliente();

    private static HttpClient Cliente()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("Ondine-Complementos");
        return h;
    }

    /// <param name="Indice">El índice, si se pudo traer y entender.</param>
    /// <param name="Error">Por qué no, si no.</param>
    public sealed record Traida(Indice? Indice, string? Error);

    public static async Task<Traida> TraerIndiceAsync(string url, CancellationToken corte = default)
    {
        if (!EsHttps(url)) return new(null, Textos.Instancia.IndiceSoloHttps);

        try
        {
            var texto = await Http.GetStringAsync(url, corte).ConfigureAwait(false);
            var i = Indice.Leer(texto);
            return i is null
                ? new(null, Textos.Instancia.TiendaIndiceIlegible)
                : new(i, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new(null, string.Format(Textos.Instancia.TiendaSinRed, ex.Message)); }
    }

    /// <param name="Bytes">El paquete, si se pudo traer. Sin verificar todavía.</param>
    /// <param name="Error">Por qué no, si no.</param>
    public sealed record Paquete(byte[]? Bytes, string? Error);

    /// <summary>
    /// Baja un paquete. NO lo verifica: de eso se encarga el instalador, que es
    /// quien sabe contra qué. Aquí solo se trae y se vigila el tamaño.
    /// </summary>
    public static async Task<Paquete> TraerPaqueteAsync(Indice.Entrada entrada, CancellationToken corte = default)
    {
        if (entrada.Reparo() is { } malo) return new(null, malo);

        try
        {
            using var respuesta = await Http
                .GetAsync(entrada.Paquete, HttpCompletionOption.ResponseHeadersRead, corte)
                .ConfigureAwait(false);
            respuesta.EnsureSuccessStatusCode();

            // Se mira lo que dice ANTES de bajarlo, y lo que ocupa DESPUÉS: la
            // cabecera puede mentir o no venir, así que sirve para cortar pronto
            // en el caso normal pero no puede ser la única comprobación.
            var dice = respuesta.Content.Headers.ContentLength;
            if (dice is > MaximoBytes)
                return new(null, string.Format(Textos.Instancia.TiendaDemasiadoGrande, MaximoBytes / (1024 * 1024)));

            var bytes = await respuesta.Content.ReadAsByteArrayAsync(corte).ConfigureAwait(false);
            if (bytes.LongLength > MaximoBytes)
                return new(null, string.Format(Textos.Instancia.TiendaDemasiadoGrande, MaximoBytes / (1024 * 1024)));

            return new(bytes, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new(null, string.Format(Textos.Instancia.TiendaSinRed, ex.Message)); }
    }

    private static bool EsHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
