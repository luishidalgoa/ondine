namespace Ondine.Recortes;

/// <summary>
/// Dónde escribe un tramo, sin pisar nada.
///
/// <para>
/// Hay <b>dos</b> formas de pisar, y las dos han pasado: que el fichero ya estuviera de un
/// intento anterior, y que dos tramos de la MISMA tanda pidan el mismo nombre porque el
/// usuario los llamó igual. Mirar solo el disco deja pasar la segunda — exportas cinco y
/// aparecen tres, sin un solo error.
/// </para>
/// <para>
/// La comprobación del disco entra <b>inyectada</b>: así la regla se prueba entera sin
/// tocar el disco, que es la única forma de probar el caso de la colisión doble sin montar
/// una carpeta de mentira.
/// </para>
/// </summary>
public static class RutaDeSalida
{
    /// <summary>Lo que se pone cuando el nombre se queda en nada al limpiarlo.</summary>
    public const string SinNombre = "tramo";

    public static string Libre(
        string carpeta,
        string nombre,
        string extension,
        Func<string, bool> existeEnDisco,
        ISet<string> yaReservadas)
    {
        var limpio = Limpiar(nombre);
        var ext = extension.StartsWith('.') ? extension : "." + extension;

        for (var i = 1; ; i++)
        {
            var cand = Path.Combine(carpeta, i == 1 ? limpio + ext : $"{limpio} ({i}){ext}");
            if (existeEnDisco(cand) || !yaReservadas.Add(cand)) continue;
            return cand;
        }
    }

    /// <summary>
    /// Los caracteres que se quitan SIEMPRE, corra donde corra esto.
    ///
    /// <para>
    /// El nombre de un tramo lo escribe el usuario en una caja de texto, así que llega lo
    /// que sea: dos puntos, signos de mayor y menor, barras. Sin limpiarlo, ffmpeg falla
    /// con un error de ruta que no menciona en ningún momento que el problema estaba en el
    /// nombre que acabas de escribir.
    /// </para>
    /// <para>
    /// No se usa <c>Path.GetInvalidFileNameChars()</c>, y esto lo enseño CI: en Linux esa
    /// lista trae solo <c>/</c> y el nulo, asi que <c>&lt;</c>, <c>:</c> o <c>?</c> pasaban
    /// enteros. El fichero se creaba tan campante y luego era inusable en Windows — y una
    /// biblioteca de video viaja entre sistemas todo el rato: disco externo, red, nube.
    /// </para>
    /// <para>
    /// Se quita la UNION de lo que prohibe cualquiera de los dos, que es la unica lista que
    /// da un nombre valido en los dos sitios.
    /// </para>
    /// </summary>
    private static readonly char[] Prohibidos =
        ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    private static string Limpiar(string nombre)
    {
        var limpio = new string(nombre
            .Where(c => !Prohibidos.Contains(c) && !char.IsControl(c))
            .ToArray()).Trim();

        // Un punto al final lo ignora Windows en silencio y deja «cap.» como «cap»: se
        // quita aquí para que el nombre que se ve sea el nombre que queda.
        limpio = limpio.TrimEnd('.', ' ');

        return limpio.Length > 0 ? limpio : SinNombre;
    }
}
