using System.Globalization;
using System.Text;

namespace Ondine;

/// <summary>
/// Mandar un fichero o una carpeta a la papelera del sistema, en cualquier sistema.
///
/// <para>
/// <b>Esto no existía y la app lo prometía.</b> Lo que mandaba a la papelera era una llamada
/// a <c>shell32</c>, y quien la conectaba al motor era la ventana principal de WPF. Sin nadie
/// conectado, el motor —lo dice su propio comentario— borra sin más. Es decir: fuera de
/// Windows, lo que la app dice que va a la papelera <b>se perdía</b>.
/// </para>
/// <para>
/// En Linux la papelera no es una llamada del sistema: es un <b>acuerdo sobre carpetas</b>, la
/// especificación de freedesktop.org que siguen Nemo (Cinnamon, el de Linux Mint), Nautilus,
/// Dolphin y compañía. El fichero se mueve a <c>~/.local/share/Trash/files</c> y al lado se
/// deja un <c>.trashinfo</c> diciendo de dónde venía y cuándo.
/// </para>
/// <para>
/// <b>Y aquí decía «en Linux y macOS», que era mentira a medias.</b> freedesktop es el acuerdo
/// de los escritorios de Linux; en un Mac, <c>~/.local/share/Trash</c> es una carpeta oculta
/// cualquiera que el Finder no mira. Lo «enviado a la papelera» se iba a un sitio donde nadie
/// lo encontraría y del que no se puede recuperar — otra vez un fallo que parece funcionar.
/// Ahora cada sistema va por su sitio, y quién manda en cada uno lo decide
/// <see cref="Quien"/>, que es lo único de esto que se puede probar desde cualquier parte.
/// </para>
/// <para>
/// <b>Ese fichero de al lado es lo que importa.</b> Sin él, el gestor de archivos enseña lo
/// borrado pero «Restaurar» no sabe dónde devolverlo — y eso es peor que no haberlo movido,
/// porque parece recuperable y no lo es.
/// </para>
/// </summary>
public static class PapeleraDelSistema
{
    /// <summary>
    /// Cómo se manda en Windows: por la Shell, que es la única que hace la papelera de verdad
    /// -con su cuenta de lo que ocupa y su «deshacer» en el Explorador-.
    ///
    /// <para>
    /// <b>Viene puesta.</b> Antes era un hueco que rellenaba la ventana principal de WPF al
    /// arrancar, y ese es exactamente el reparto que ya falló una vez: la interfaz nueva no lo
    /// rellenó. Aquí no había pérdida de ficheros -sin nadie enchufado se cae a la forma de
    /// freedesktop-, pero el resultado en Windows era peor de explicar: lo «enviado a la
    /// papelera» acababa en <c>~/.local/share/Trash</c>, una carpeta que el Explorador no
    /// mira, sin «Restaurar» y sin aparecer en la papelera de verdad.
    /// </para>
    /// <para>
    /// La llamada nativa vive en este mismo ensamblado, así que no hacía falta pedírsela a
    /// nadie. Se deja como propiedad para poder sustituirla en pruebas.
    /// </para>
    /// </summary>
    public static Func<string, bool>? EnWindows { get; set; } = ruta => RecycleBin.Send(ruta);

    /// <summary>
    /// Cómo se manda en macOS: por el Finder, que es el único que deja «Volver a poner».
    ///
    /// <para>
    /// Viene puesta, por lo mismo que la de Windows. Ver <see cref="PapeleraDeMac"/> para lo
    /// que se gana y lo que cuesta de esa vía.
    /// </para>
    /// </summary>
    public static Func<string, bool>? EnMac { get; set; } = ruta => PapeleraDeMac.Mandar(ruta);

    /// <summary>Quién se lleva lo borrado.</summary>
    public enum ADonde
    {
        /// <summary>La Shell de Windows (shell32).</summary>
        Shell,
        /// <summary>El Finder de macOS.</summary>
        Finder,
        /// <summary>El acuerdo de carpetas de freedesktop.org.</summary>
        Freedesktop,
    }

    /// <summary>
    /// Quién manda, según el sistema.
    ///
    /// <para>
    /// Está aparte y toma el sistema como argumento por una razón práctica: es <b>la decisión
    /// que estaba mal</b> —macOS entraba por la puerta de Linux— y así se puede comprobar
    /// desde cualquier máquina. Que la llamada al Finder haga su trabajo solo se ve en un Mac;
    /// que no se le mande a la carpeta equivocada, se ve aquí.
    /// </para>
    /// <para>
    /// La casa de pruebas gana a todo lo demás a propósito: es como las pruebas de la papelera
    /// trabajan con ficheros de verdad sin tocar la de quien las ejecuta, y una papelera de
    /// carpetas es la única que se puede abrir y mirar después.
    /// </para>
    /// </summary>
    internal static ADonde Quien(bool windows, bool mac, bool casaDePruebas) =>
        casaDePruebas ? ADonde.Freedesktop
        : windows ? ADonde.Shell
        : mac ? ADonde.Finder
        : ADonde.Freedesktop;

    /// <summary>
    /// Manda una ruta a la papelera. Devuelve si se pudo.
    ///
    /// <param name="casa">
    /// Dónde vive la papelera del usuario. Se puede pasar para probarlo con ficheros de
    /// verdad sin tocar la papelera de quien ejecuta las pruebas; en la app real no se pasa.
    /// </param>
    /// </summary>
    public static bool Mandar(string ruta, string? casa = null)
    {
        if (string.IsNullOrWhiteSpace(ruta)) return false;
        if (!File.Exists(ruta) && !Directory.Exists(ruta)) return false;

        var quien = Quien(OperatingSystem.IsWindows(), OperatingSystem.IsMacOS(), casa is not null);

        // Si el nativo de ese sistema no estuviera enchufado se cae a la de carpetas. En
        // Windows y en macOS no la lee nadie, pero al menos no borra — y eso es lo único
        // que hace aceptable el respaldo.
        if (quien == ADonde.Shell && EnWindows is { } shell) return shell(ruta);
        if (quien == ADonde.Finder && EnMac is { } finder) return finder(ruta);

        return ALaDeFreedesktop(ruta, casa ?? Casa());
    }

    private static string Casa() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// La papelera de freedesktop: mover a <c>files/</c> y dejar la ficha en <c>info/</c>.
    ///
    /// <para>
    /// <b>La ficha se escribe ANTES de mover.</b> Si se hiciera al revés y algo fallara por el
    /// camino, quedaría un fichero en la papelera sin manera de saber de dónde salió. Así el
    /// peor caso es una ficha huérfana, que los gestores ignoran.
    /// </para>
    /// </summary>
    private static bool ALaDeFreedesktop(string ruta, string casa)
    {
        try
        {
            var papelera = Path.Combine(casa, ".local", "share", "Trash");
            var carpetaFicheros = Path.Combine(papelera, "files");
            var carpetaFichas = Path.Combine(papelera, "info");
            Directory.CreateDirectory(carpetaFicheros);
            Directory.CreateDirectory(carpetaFichas);

            var nombre = LibreEn(carpetaFicheros, carpetaFichas, Path.GetFileName(ruta.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

            var destino = Path.Combine(carpetaFicheros, nombre);
            var ficha = Path.Combine(carpetaFichas, nombre + ".trashinfo");

            File.WriteAllText(ficha, Ficha(Path.GetFullPath(ruta)), new UTF8Encoding(false));

            if (Directory.Exists(ruta)) Directory.Move(ruta, destino);
            else File.Move(ruta, destino);

            return true;
        }
        catch
        {
            // Si no se pudo mover, NO se borra. Un fallo aquí deja el fichero donde estaba,
            // que es exactamente lo que quien lo mandó a la papelera preferiría.
            return false;
        }
    }

    /// <summary>
    /// Un nombre que no esté cogido. Borrar dos ficheros que se llaman igual desde carpetas
    /// distintas —«temporada 1/cap01.mkv» y «temporada 2/cap01.mkv»— es de lo más normal, y
    /// si el segundo pisara al primero la papelera se habría comido uno <b>en silencio</b>.
    /// </summary>
    private static string LibreEn(string carpetaFicheros, string carpetaFichas, string nombre)
    {
        if (!Existe(nombre)) return nombre;

        var sinExt = Path.GetFileNameWithoutExtension(nombre);
        var ext = Path.GetExtension(nombre);
        for (int i = 2; i < 10_000; i++)
        {
            var probar = $"{sinExt} ({i}){ext}";
            if (!Existe(probar)) return probar;
        }
        // Diez mil con el mismo nombre no pasa; si pasara, mejor un nombre feo que perder uno.
        return $"{sinExt} ({Guid.NewGuid():N}){ext}";

        bool Existe(string n) =>
            File.Exists(Path.Combine(carpetaFicheros, n)) ||
            Directory.Exists(Path.Combine(carpetaFicheros, n)) ||
            File.Exists(Path.Combine(carpetaFichas, n + ".trashinfo"));
    }

    /// <summary>
    /// La ficha, con el formato exacto de la especificación.
    ///
    /// <para>
    /// La ruta va <b>codificada como una URL</b>. Un espacio o un acento en el nombre de la
    /// carpeta es lo normal, y en crudo la línea queda ambigua — hay gestores que entonces
    /// restauran a un sitio equivocado, y otros que directamente no ofrecen restaurar.
    /// </para>
    /// <para>
    /// La fecha va en hora <b>local</b> y sin zona, que es lo que dice la especificación;
    /// ponerla en UTC hace que el gestor enseñe una hora que no es la del reloj de quien
    /// borró el fichero.
    /// </para>
    /// </summary>
    private static string Ficha(string origen)
    {
        var cuando = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        return $"[Trash Info]\nPath={Codificada(origen)}\nDeletionDate={cuando}\n";
    }

    /// <summary>
    /// La ruta como la quiere la especificación: cada tramo codificado, las barras enteras.
    /// <c>Uri.EscapeDataString</c> sobre la ruta entera se comería también las barras.
    /// </summary>
    private static string Codificada(string ruta)
    {
        // En Windows las rutas llevan «\» y letra de unidad; se normaliza a barras para que
        // la ficha sea legible aunque ahí no la lea nadie.
        var partes = ruta.Replace('\\', '/').Split('/');
        return string.Join('/', partes.Select(Uri.EscapeDataString));
    }
}
