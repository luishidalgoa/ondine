namespace Ondine.Reindex.Tests;

/// <summary>
/// Los dos envoltorios que no se pueden probar aquí: el AppImage de Linux y el .app de macOS.
///
/// <para>
/// <b>Por qué esto existe.</b> Los dos se montan con herramientas que solo hay en su sistema
/// —<c>appimagetool</c>, <c>iconutil</c>, <c>codesign</c>, <c>hdiutil</c>— y los dos fallan de
/// la peor manera posible: <b>el paquete se monta bien y la app no arranca</b>. Un
/// <c>Exec=</c> que apunta a una ruta que en un AppImage no existe, o un
/// <c>CFBundleExecutable</c> que no es el nombre del binario, dan un fichero perfecto que al
/// abrirse no hace nada — sin mensaje, sin registro y sin nada que leer.
/// </para>
/// <para>
/// Así que lo que se vigila son los <b>cuatro o cinco datos que tienen que cuadrar con el
/// proyecto</b>, y se leen del proyecto, no de una copia. No prueba que el paquete funcione;
/// prueba que no está mal por las razones por las que suele estarlo.
/// </para>
/// </summary>
public static class PaquetesPortablesTests
{
    public static void Todas()
    {
        Program.Seccion("El AppImage y el paquete de macOS");

        var raiz = LocalizarRaiz();
        var csproj = Path.Combine(raiz, "src", "Ondine.Avalonia", "Ondine.Avalonia.csproj");
        if (!File.Exists(csproj))
        {
            Program.Assert(false, "no encuentro el csproj de la interfaz");
            return;
        }

        var proyecto = File.ReadAllText(csproj);
        var binario = Entre(proyecto, "<AssemblyName>", "</AssemblyName>");
        Program.Assert(binario == "Ondine.Avalonia",
            $"el binario de la interfaz se llama {binario}");

        ElAppImage(raiz, binario);
        ElPaqueteDeMac(raiz, binario);
        NingunoSeInventaLaVersion(raiz);
        ElNombreDiceElSistema(raiz);
        ElIconoNoDependeDeUnSoloSitio(raiz);
    }

    /// <summary>
    /// El AppImage. Lo que se comprueba es lo que ya salió mal al montarlo: el
    /// <c>.desktop</c> del paquete .deb lleva <c>Exec=/opt/ondine/…</c>, y en un AppImage
    /// <b>no hay /opt</b> porque no se instala en ninguna parte. Copiarlo tal cual da un
    /// AppImage que abre y no lanza nada.
    /// </summary>
    private static void ElAppImage(string raiz, string binario)
    {
        var guion = Path.Combine(raiz, "empaquetado", "linux", "hacer-appimage.sh");
        if (!File.Exists(guion)) { Program.Assert(false, "no encuentro hacer-appimage.sh"); return; }
        var s = File.ReadAllText(guion);

        Program.Assert(s.Contains("^Exec=.*|Exec=ondine"),
            "el Exec del lanzador se reescribe: la ruta de /opt no existe en un AppImage");

        // El AppRun tiene que llamar al binario que de verdad se publica. Si se le cambiara
        // el AssemblyName al proyecto, esto se queda apuntando a un nombre que ya no está.
        Program.Assert(s.Contains($"\"$AQUI/usr/bin/{binario}\""),
            $"el AppRun lanza {binario}, que es el binario que se publica");

        // appimagetool no deduce la arquitectura del contenido: sin ARCH en el entorno
        // falla, y el mensaje no dice que le falte una variable.
        Program.Assert(s.Contains("ARCH="),
            "se le pasa ARCH a appimagetool, que sin ella no sabe para qué arquitectura es");

        // El icono en la raíz del AppDir. Es el que se ve en el gestor de archivos, y sin
        // él appimagetool avisa pero termina: sale un AppImage con icono en blanco.
        Program.Assert(s.Contains("$appdir/ondine.png"),
            "el icono va también en la raíz del AppDir, que es de donde lo lee el escritorio");
    }

    /// <summary>
    /// El .app de macOS. Dos datos deciden si arranca o no, y ninguno da error al montarlo.
    /// </summary>
    private static void ElPaqueteDeMac(string raiz, string binario)
    {
        var guion = Path.Combine(raiz, "empaquetado", "macos", "hacer-dmg.sh");
        if (!File.Exists(guion)) { Program.Assert(false, "no encuentro hacer-dmg.sh"); return; }
        var s = File.ReadAllText(guion);

        // 1. El nombre del ejecutable dentro del bundle. Si no cuadra con el binario, el
        //    Finder abre el .app y no pasa nada de nada.
        Program.Assert(s.Contains($"<key>CFBundleExecutable</key>        <string>{binario}</string>"),
            $"el Info.plist declara {binario} como ejecutable del bundle");

        // 2. La firma ad hoc. En un Mac con chip de Apple el sistema no ejecuta un binario
        //    sin firmar: la app se cierra al abrirse y no hay mensaje que lo explique.
        Program.Assert(s.Contains("codesign --force --deep --sign -"),
            "se firma ad hoc: sin firma, en los Mac con chip de Apple la app no arranca");

        // Las dos arquitecturas. Dar solo la de Intel dejaría a los Mac nuevos con la app
        // traducida por Rosetta, más lenta y sin que nada lo diga.
        foreach (var rid in new[] { "osx-arm64", "osx-x64" })
            Program.Assert(s.Contains(rid), $"se publica para {rid}");

        // El mínimo del sistema tiene que estar declarado: sin él, macOS deja abrir la app
        // en versiones donde .NET 9 no arranca.
        Program.Assert(s.Contains("LSMinimumSystemVersion"),
            "y se declara el macOS mínimo, que .NET 9 no soporta cualquiera");

        // Los tipos que abre, que es el equivalente del MimeType del .desktop de Linux: sin
        // esto Ondine no sale en «Abrir con» al pulsar el botón derecho sobre un vídeo.
        Program.Assert(s.Contains("CFBundleDocumentTypes"),
            "y los tipos de fichero que abre, como el MimeType del lanzador de Linux");
    }

    /// <summary>
    /// La versión sale del <c>.csproj</c> en los tres guiones.
    ///
    /// <para>
    /// El contrato del CHANGELOG obliga a que la versión valga lo mismo en los cuatro
    /// proyectos y lo comprueba en cada tag. Un guión con la versión escrita a mano se
    /// escapa de esa comprobación: publicaría «Ondine-1.12.0.dmg» el día que la app ya va por
    /// la 1.14, y el paquete parecería viejo sin serlo.
    /// </para>
    /// </summary>
    private static void NingunoSeInventaLaVersion(string raiz)
    {
        foreach (var nombre in new[] { "linux/hacer-deb.sh", "linux/hacer-appimage.sh",
                                       "macos/hacer-dmg.sh" })
        {
            var guion = Path.Combine(raiz, "empaquetado", nombre.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(guion)) { Program.Assert(false, $"no encuentro {nombre}"); continue; }

            var s = File.ReadAllText(guion);
            Program.Assert(s.Contains("<Version>"),
                $"{nombre} saca la versión del csproj, no la lleva escrita");
        }
    }

    /// <summary>
    /// El nombre del fichero dice PARA QUÉ SISTEMA es.
    ///
    /// <para>
    /// Esto es un fallo de verdad, no una hipótesis: se publicó la v1.14.0 con los paquetes
    /// llamados <c>Ondine-1.14.0-x64.dmg</c> y <c>Ondine-1.14.0-x86_64.AppImage</c>, y en la
    /// página de versiones hay diez ficheros juntos. Alguien con un Linux de 64 bits vio
    /// «x64», se lo bajó, y su Mint lo detectó como <b>un comprimido cualquiera</b>: un
    /// <c>.dmg</c> es una imagen de disco de macOS.
    /// </para>
    /// <para>
    /// La extensión ya lo dice, pero solo a quien se sepa las extensiones. La arquitectura
    /// sola es peor que no poner nada, porque <b>coincide entre sistemas</b> —x64 es x64 en
    /// los tres— y da una pista falsa que se lee con confianza.
    /// </para>
    /// <para>
    /// El <c>.deb</c> se queda fuera: su nombre lo fija la política de Debian
    /// (<c>nombre_version_arquitectura.deb</c>) y ahí «amd64» no se puede tocar. A cambio, ese
    /// es el único de los tres que el escritorio abre e instala él solo.
    /// </para>
    /// </summary>
    private static void ElNombreDiceElSistema(string raiz)
    {
        foreach (var (guion, marca) in new[]
                 {
                     ("macos/hacer-dmg.sh", "macos"),
                     ("linux/hacer-appimage.sh", "linux"),
                 })
        {
            var ruta = Path.Combine(raiz, "empaquetado", guion.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(ruta)) { Program.Assert(false, $"no encuentro {guion}"); continue; }

            var s = File.ReadAllText(ruta);
            Program.Assert(s.Contains($"-{marca}-"),
                $"el paquete de {guion} lleva «{marca}» en el nombre, no solo la arquitectura");
        }
    }

    /// <summary>
    /// El icono se instala en dos sitios, y hace falta.
    ///
    /// <para>
    /// En Linux Mint el lanzador salió en el menú <b>con un engranaje genérico</b>: el icono
    /// estaba dentro del paquete y el <c>.desktop</c> lo pedía por su nombre. Lo que falla en
    /// medio es el tema de iconos, que son varias piezas —el índice del tema, la caché de
    /// GTK, el cargador de SVG— y basta con que una no esté para que la búsqueda por nombre
    /// no devuelva nada. Ninguna de ellas avisa.
    /// </para>
    /// <para>
    /// <c>/usr/share/pixmaps</c> es la ruta anterior a los temas: se mira por nombre, sin
    /// índice y sin caché. Tres kilobytes por no depender de que todo lo demás esté bien.
    /// </para>
    /// </summary>
    private static void ElIconoNoDependeDeUnSoloSitio(string raiz)
    {
        var guion = Path.Combine(raiz, "empaquetado", "linux", "hacer-deb.sh");
        if (!File.Exists(guion)) { Program.Assert(false, "no encuentro hacer-deb.sh"); return; }
        var s = File.ReadAllText(guion);

        Program.Assert(s.Contains("/usr/share/icons/hicolor/256x256/apps/ondine.png"),
            "el icono va en el árbol del tema, que es lo primero que mira el escritorio");
        Program.Assert(s.Contains("/usr/share/pixmaps/ondine.png"),
            "y también en pixmaps, que es lo que queda cuando el tema de iconos falla");
        Program.Assert(s.Contains("hicolor-icon-theme"),
            "y el paquete que crea ese árbol se declara como dependencia");
    }

    private static string Entre(string texto, string desde, string hasta)
    {
        var i = texto.IndexOf(desde, StringComparison.Ordinal);
        if (i < 0) return "";
        i += desde.Length;
        var j = texto.IndexOf(hasta, i, StringComparison.Ordinal);
        return j < 0 ? "" : texto[i..j].Trim();
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "empaquetado")))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
