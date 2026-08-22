using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Ondine.Ava;

/// <summary>
/// Las miniaturas que traen los complementos, cargadas a mano.
///
/// <para>
/// <b>Esto no hacía falta en WPF, y ahí está lo interesante.</b> Allí bastaba
/// <c>&lt;Image Source="{Binding Miniatura}"/&gt;</c> con una cadena: WPF convierte solo una
/// ruta —y también una <b>URL</b>, que se descarga sin que nadie lo escriba en ninguna
/// parte—. Avalonia no hace esa conversión: si le das una cadena, la imagen no sale y
/// <b>no protesta</b>.
/// </para>
/// <para>
/// Al tener que escribirlo, aparece dicho lo que antes pasaba callado: <b>la dirección la
/// pone el complemento</b>, así que pintar su lista hace que esta máquina visite direcciones
/// elegidas por un programa de fuera. Era verdad en WPF igual; la diferencia es que ahora
/// está en un sitio donde se lee. Se descarga solo lo que es <c>http</c>/<c>https</c>, con un
/// tope de tamaño y sin cookies ni credenciales, y se guarda en memoria por dirección para
/// no repetir la visita al desplazar la lista.
/// </para>
/// </summary>
internal static class Miniaturas
{
    /// <summary>Un cliente para todas: crear uno por imagen agota los puertos del sistema.</summary>
    private static readonly HttpClient Cliente = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Tope por imagen. Una miniatura de lista no pesa esto ni de lejos; el tope está para
    /// que una dirección que devuelve un vídeo no se trague la memoria.
    /// </summary>
    private const int TopeBytes = 4 * 1024 * 1024;

    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// La miniatura de esa dirección, o <c>null</c> si no se puede. Nunca lanza: una
    /// miniatura que no sale es un hueco gris, no un fallo de la pantalla.
    /// </summary>
    public static async Task<Bitmap?> TraerAsync(string? donde)
    {
        if (string.IsNullOrWhiteSpace(donde)) return null;

        lock (Cache) if (Cache.TryGetValue(donde, out var ya)) return ya;

        Bitmap? mapa = null;
        try
        {
            if (File.Exists(donde))
            {
                await using var fs = File.OpenRead(donde);
                mapa = new Bitmap(fs);
            }
            else if (Uri.TryCreate(donde, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using var r = await Cliente.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                if (r.IsSuccessStatusCode && (r.Content.Headers.ContentLength ?? 0) <= TopeBytes)
                {
                    var bytes = await r.Content.ReadAsByteArrayAsync();
                    if (bytes.Length <= TopeBytes)
                    {
                        using var ms = new MemoryStream(bytes);
                        mapa = new Bitmap(ms);
                    }
                }
            }
        }
        catch { mapa = null; }

        lock (Cache) Cache[donde] = mapa;
        return mapa;
    }

    /// <summary>
    /// La pide y, cuando llega, la deja donde toque — en el hilo de la interfaz.
    /// Devuelve enseguida: la lista se pinta y las imágenes van cayendo.
    /// </summary>
    public static void Pedir(string? donde, Action<Bitmap?> ponerla)
    {
        if (string.IsNullOrWhiteSpace(donde)) return;
        _ = Task.Run(async () =>
        {
            var m = await TraerAsync(donde);
            if (m is not null) Dispatcher.UIThread.Post(() => ponerla(m));
        });
    }
}
