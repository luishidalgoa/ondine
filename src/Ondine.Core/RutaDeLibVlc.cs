namespace Ondine;

/// <summary>
/// Dónde está libvlc, cuando hay que ir a buscarla.
///
/// <para>
/// En Windows va dentro de la aplicación —el paquete de VideoLAN la trae— y en Linux la trae
/// el sistema en una ruta donde el cargador la encuentra sola. En los dos casos no hay nada
/// que decidir.
/// </para>
/// <para>
/// <b>macOS es el caso raro.</b> Un Mac no trae libvlc, y la que se puede tener —la de
/// VLC.app— vive dentro de un paquete de aplicación, que no es una ruta de bibliotecas: nadie
/// la busca ahí. El paquete de VideoLAN para Mac no vale de sustituto (2018, solo Intel y sin
/// decodificadores; está contado en el <c>.csproj</c>), así que hay que mirar a mano.
/// </para>
/// <para>
/// Es una función pura a propósito: se le pasa el sitio donde mirar y quién sabe si existe.
/// La decisión —el orden de las candidatas y que no se invente ninguna— se puede comprobar
/// desde cualquier sistema, y eso es lo que aquí importa.
/// </para>
/// </summary>
public static class RutaDeLibVlc
{
    /// <summary>
    /// La primera carpeta con libvlc, o <c>null</c> si no hay ninguna.
    ///
    /// <param name="casa">La carpeta del usuario.</param>
    /// <param name="existe">Si una carpeta está. Se pasa para poder probarlo.</param>
    /// </summary>
    public static string? EnMac(string casa, Func<string, bool> existe)
    {
        // El orden no es casual: la de VLC.app primero porque es la que VideoLAN publica
        // universal -sirve en Intel y en chip de Apple- y la que se actualiza sola con VLC.
        // Las de Homebrew después, y son dos rutas distintas: /opt/homebrew en los Mac con
        // chip de Apple y /usr/local en los Intel. Quien tenga las dos cosas se lleva VLC.
        string[] candidatas =
        [
            "/Applications/VLC.app/Contents/MacOS/lib",
            // Con barras a mano y no con Path.Combine: estas rutas son de macOS, donde el
            // separador es siempre «/». Path.Combine usa el del sistema que EJECUTA, así que
            // armar una ruta de Mac desde Windows salía con barras invertidas — y no es un
            // detalle de pruebas: es que la ruta dejaba de ser la ruta.
            casa.TrimEnd('/') + "/Applications/VLC.app/Contents/MacOS/lib",
            "/opt/homebrew/lib",
            "/usr/local/lib",
        ];

        // Y si no hay ninguna, NADA. Devolver la primera de la lista «por si acaso» haría que
        // el reproductor fallara más tarde con un mensaje sobre una biblioteca que no carga,
        // en vez de decir aquí mismo que falta VLC y cómo se instala.
        return candidatas.FirstOrDefault(existe);
    }

    /// <summary>La misma, preguntándole al disco. Es la que usa la app.</summary>
    public static string? EnEsteMac() =>
        EnMac(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Directory.Exists);
}
