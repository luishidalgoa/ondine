using System.Text.RegularExpressions;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El arnés de la traducción.
///
/// <para>
/// Existe porque la traducción se rompe siempre igual: alguien añade un botón,
/// escribe el texto a pelo, compila, funciona, y nadie se entera hasta que un
/// usuario abre la aplicación en el otro idioma. Ninguna revisión humana pilla
/// eso de forma fiable en cuarenta ficheros; una prueba sí, y falla en el CI
/// antes de publicar.
/// </para>
/// <para>
/// No comprueban que la traducción sea BUENA, que eso no lo puede comprobar una
/// máquina. Comprueban que esté COMPLETA, que es lo que se olvida.
/// </para>
/// </summary>
public static class TraduccionTests
{
    private static readonly string Raiz = LocalizarRaiz();

    /// <summary>La app WPF. Es la única que tiene XAML.</summary>
    private static string App => Path.Combine(Raiz, "src", "Ondine");

    /// <summary>El motor. Aquí viven los textos y la mayor parte del código.</summary>
    private static string Motor => Path.Combine(Raiz, "src", "Ondine.Core");

    private static string Loc => Path.Combine(Motor, "Localizacion");

    /// <summary>
    /// Las dos carpetas con código, para las comprobaciones que barren <c>*.cs</c>.
    ///
    /// <para>
    /// Son dos y no una desde que el motor se separó en <c>Ondine.Core</c>. Mirar solo la
    /// app dejaría fuera <c>Reindex/</c>, <c>Peliculas/</c> y todo lo demás — y la
    /// comprobación seguiría pasando, que es lo peligroso: un guardián que ya no mira
    /// donde hace falta no avisa de nada y encima tranquiliza.
    /// </para>
    /// </summary>
    private static string[] Fuentes => [App, Motor];

    public static void Todas()
    {
        Program.Seccion("Traducción (que esté completa, no que sea buena)");

        if (!Directory.Exists(Loc))
        {
            Program.Assert(false, $"No encuentro la carpeta de localización. Busqué en «{Loc}»");
            return;
        }

        LasDosVersionesSonDistintas();
        TodaClaveDelXamlExiste();
        NingunTextoSueltoEnXaml();
        NingunTextoSueltoEnCodigo();
        LosMarcadoresCuadran();
        NiUnaRayaLarga();
        LaDeudaDeTraduccion();
    }

    /// <summary>
    /// Un trinquete sobre los literales en castellano que quedan en el código.
    ///
    /// <para>
    /// Esta prueba nació de un fallo del propio arnés: la de arriba solo mira el
    /// XAML, así que pasó en verde teniendo <b>69 cadenas visibles sin
    /// traducir</b> repartidas por el C#. Lo encontró una revisión adversarial,
    /// no una prueba, y eso no se puede repetir.
    /// </para>
    /// <para>
    /// No intenta distinguir por sí sola qué literal es de interfaz y qué literal
    /// es un marcador de plantilla, una expresión regular o una tabla de
    /// compatibilidad: eso no lo acierta ninguna heurística y una prueba que grita
    /// en falso se acaba desactivando. Lo que hace es <b>congelar el número que
    /// hay hoy</b>. Si sube, es que alguien ha escrito texto nuevo a pelo.
    /// </para>
    /// <para>
    /// Si bajas uno legítimamente, baja también su cifra aquí: el trinquete solo
    /// aprieta.
    /// </para>
    /// </summary>
    private static void NingunTextoSueltoEnCodigo()
    {
        // Lo que queda hoy, y por qué se queda. Ninguno es texto de interfaz.
        var permitido = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // «<título>» es un marcador de la plantilla de nombres que escribe el
            // usuario, y la expresión regular acepta con tilde y sin ella.
            // Traducirlo rompería las plantillas ya guardadas.
            ["LibraryTemplate.cs"] = 5,

            // Tabla de compatibilidad: nombres de preset guardados en versiones
            // o idiomas anteriores, para que sigan reconociéndose. No se pintan.
            ["Preset.cs"] = 3,

            // El nombre de respaldo de «es-419», que .NET no resuelve.
            ["IsoLanguages.cs"] = 1,

            // Texto dentro de la plantilla JSON de ejemplo que se escribe al
            // fichero de catálogo, no en pantalla.
            ["ReindexCatalog.cs"] = 1,

            // Reconoce «(España)» y «(Hispanoamérica)» EN LOS DATOS de entrada.
            // Traducirlo dejaría de reconocer los títulos que vienen así.
            ["TitleMatch.cs"] = 1,
        };

        var excesos = new List<string>();
        foreach (var f in Fuentes.SelectMany(c => Ficheros(c, "*.cs")))
        {
            if (f.Contains($"{Path.DirectorySeparatorChar}Localizacion{Path.DirectorySeparatorChar}")) continue;

            var nombre = Path.GetFileName(f);
            var cuantos = Regex.Matches(File.ReadAllText(f), @"""[^""\r\n]{4,}""")
                .Count(m => Regex.IsMatch(m.Value, "[áéíóúñÁÉÍÓÚÑ¿¡«»]"));

            var tope = permitido.TryGetValue(nombre, out var t) ? t : 0;
            if (cuantos > tope)
                excesos.Add($"{nombre}: {cuantos} (tope {tope})");
        }

        Program.Assert(
            excesos.Count == 0,
            excesos.Count == 0
                ? $"Ningún fichero de código pasa su tope de literales en castellano"
                : $"{excesos.Count} ficheros con literales nuevos sin traducir: {string.Join(" · ", excesos.Take(5))}");
    }

    /// <summary>
    /// Los textos que están solo en castellano, esperando el resto de idiomas.
    ///
    /// <para>
    /// Durante el desarrollo se permiten y solo se cuentan: escribir las dos
    /// versiones de un texto que va a cambiar tres veces es trabajo tirado.
    /// <b>Al publicar no se permite ninguno.</b> El CI pone
    /// <c>ONDINE_RELEASE=1</c> en el trabajo que compila una versión, y ahí esto
    /// pasa de recuento a fallo.
    /// </para>
    /// <para>
    /// Sin esta prueba, «luego lo rellenamos» se convierte en nunca, que es
    /// exactamente lo que pasa siempre.
    /// </para>
    /// </summary>
    private static void LaDeudaDeTraduccion()
    {
        var pendientes = new List<string>();
        foreach (var f in Ficheros(Loc, "Textos.*.cs"))
        {
            var lineas = File.ReadAllLines(f);
            for (var i = 0; i < lineas.Length; i++)
            {
                var m = Regex.Match(lineas[i], @"public\s+string\s+(\w+)\s*=>\s*Idioma\.Pendiente\(");
                if (m.Success) pendientes.Add($"{Path.GetFileName(f)}:{i + 1} {m.Groups[1].Value}");
            }
        }

        var publicando = Environment.GetEnvironmentVariable("ONDINE_RELEASE") == "1";

        if (pendientes.Count == 0)
        {
            Program.Assert(true, "No hay textos pendientes de traducir");
            return;
        }

        if (publicando)
        {
            Program.Assert(false,
                $"PUBLICANDO con {pendientes.Count} textos sin traducir. Rellena los idiomas " +
                $"antes de cortar versión: {string.Join(" · ", pendientes.Take(6))}");
            return;
        }

        // Fuera de una publicación no es un fallo, pero sí tiene que verse: una
        // deuda que no se enseña es una deuda que se olvida.
        Program.Assert(true,
            $"{pendientes.Count} textos pendientes de traducir (permitido mientras se desarrolla). " +
            $"Los primeros: {string.Join(" · ", pendientes.Take(4))}");
    }

    /// <summary>Sube hasta encontrar la raíz del repositorio, para no depender del cwd.</summary>
    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !(Directory.Exists(Path.Combine(d.FullName, "src", "Ondine"))
                              && Directory.Exists(Path.Combine(d.FullName, "src", "Ondine.Core"))))
            d = d.Parent;
        return d?.FullName ?? "";
    }

    private static IEnumerable<string> Ficheros(string carpeta, string patron) =>
        Directory.Exists(carpeta)
            ? Directory.EnumerateFiles(carpeta, patron, SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                         && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            : [];

    private static string FuenteDeTextos() =>
        string.Concat(Ficheros(Loc, "Textos.*.cs").Select(File.ReadAllText));

    private const string ParElegir =
        @"Elegir\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)";

    // ── 1 ───────────────────────────────────────────────────────────────────
    // `Idioma.Elegir` obliga a pasar los dos idiomas, así que una traducción a
    // medias no compila. Lo que esta prueba añade es cazar la trampa fácil:
    // pasar el mismo texto dos veces para callar al compilador.
    private static void LasDosVersionesSonDistintas()
    {
        var iguales = Regex.Matches(FuenteDeTextos(), ParElegir)
            .Where(m => m.Groups[1].Value == m.Groups[2].Value)
            .Select(m => m.Groups[1].Value)
            .Where(t => !EsIgualAProposito(t))
            .Distinct()
            .ToList();

        Program.Assert(
            iguales.Count == 0,
            iguales.Count == 0
                ? "Ningún texto tiene el inglés y el castellano idénticos por descuido"
                : $"{iguales.Count} textos idénticos en los dos idiomas: {string.Join(" · ", iguales.Take(5))}");
    }

    /// <summary>Lo que es igual en los dos idiomas y no es un descuido.</summary>
    private static bool EsIgualAProposito(string t) =>
        t.Length <= 4                              // siglas y unidades: MB, GB, s, %
        || Regex.IsMatch(t, @"^[A-Z0-9./+\- ]+$")  // HEVC, H.264, AV1, MKV
        || Regex.IsMatch(t, @"^\d+p$")             // resoluciones: 1080p, 720p, 480p
        || Regex.IsMatch(t, @"^\d+ kbps$")     // caudales del audio: «192 kbps». El códec lo elige el desplegable de al lado
        || t is "Ondine" or "Plex" or "Jellyfin" or "Kodi" or "ffmpeg" or "ffprobe"
             or "Error" or "No" or "Total" or "Windows" or "Linux" or "macOS"
             or "General"   // rótulo de pestaña: se escribe igual en los dos idiomas
             or "Audio"     // ídem: etiqueta del desplegable de audio en Recortes y Comprimir
             or "Preset";   // ídem: rótulo del desplegable de presets de Comprimir

    // ── 2 ───────────────────────────────────────────────────────────────────
    // El único punto flojo del diseño: en `{i:T Clave}` el nombre va sin
    // comprobación del compilador, y un nombre mal escrito se ve como un hueco
    // en blanco en pantalla. Aquí se comprueba.
    private static void TodaClaveDelXamlExiste()
    {
        var definidas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in Ficheros(Loc, "Textos.*.cs"))
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"public\s+string\s+(\w+)\s*=>"))
                definidas.Add(m.Groups[1].Value);

        var huerfanas = new List<string>();
        foreach (var f in Ficheros(App, "*.xaml"))
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"\{\s*i:T\s+([A-Za-z_]\w*)"))
                if (!definidas.Contains(m.Groups[1].Value))
                    huerfanas.Add($"{Path.GetFileName(f)}: {m.Groups[1].Value}");

        Program.Assert(
            huerfanas.Count == 0,
            huerfanas.Count == 0
                ? $"Las {definidas.Count} claves usadas en XAML existen todas"
                : $"{huerfanas.Count} claves del XAML no existen: {string.Join(" · ", huerfanas.Take(5))}");
    }

    // ── 3 ───────────────────────────────────────────────────────────────────
    // La que de verdad impide la regresión: si alguien añade un control con su
    // texto escrito directamente, esto falla.
    private static void NingunTextoSueltoEnXaml()
    {
        string[] atributos = ["Content", "Header", "Text", "ToolTip", "Title"];
        var sueltos = new List<string>();

        foreach (var f in Ficheros(App, "*.xaml"))
        {
            var nombre = Path.GetFileName(f);
            // Los diccionarios de recursos definen estilos, no pantallas: ahí un
            // Content suelto es parte de una plantilla, no un texto de interfaz.
            if (nombre is "Theme.xaml" or "ThemeOrganizar.xaml" or "App.xaml") continue;

            var lineas = File.ReadAllLines(f);
            for (var i = 0; i < lineas.Length; i++)
                foreach (var a in atributos)
                    foreach (Match m in Regex.Matches(lineas[i], $@"\b{a}\s*=\s*""([^""]*)"""))
                    {
                        var v = m.Groups[1].Value;
                        if (v.Length == 0 || v.StartsWith('{')) continue;              // binding o recurso
                        if (!Regex.IsMatch(v, "[A-Za-zÁÉÍÓÚáéíóúÑñ]{3,}")) continue;   // símbolos y números
                        sueltos.Add($"{nombre}:{i + 1} {a}=\"{v}\"");
                    }
        }

        Program.Assert(
            sueltos.Count == 0,
            sueltos.Count == 0
                ? "No queda ningún texto escrito a pelo en el XAML"
                : $"{sueltos.Count} textos a pelo en XAML: {string.Join(" · ", sueltos.Take(5))}");
    }

    // ── 4 ───────────────────────────────────────────────────────────────────
    // Un {0} que existe en un idioma y no en el otro revienta en ejecución, y
    // solo en ese idioma: es el fallo que más tarda en aparecer.
    private static void LosMarcadoresCuadran()
    {
        var descuadres = new List<string>();

        foreach (Match m in Regex.Matches(FuenteDeTextos(), ParElegir))
        {
            var en = Marcadores(m.Groups[1].Value);
            var es = Marcadores(m.Groups[2].Value);
            if (!en.SetEquals(es))
                descuadres.Add($"«{Recortar(m.Groups[1].Value)}» [{string.Join(",", en.Order())}] frente a [{string.Join(",", es.Order())}]");
        }

        Program.Assert(
            descuadres.Count == 0,
            descuadres.Count == 0
                ? "Los marcadores de formato cuadran entre los dos idiomas"
                : $"{descuadres.Count} descuadres de marcadores: {string.Join(" · ", descuadres.Take(3))}");
    }

    private static HashSet<string> Marcadores(string t) =>
        [.. Regex.Matches(t, @"\{(\d+)").Select(m => m.Groups[1].Value)];

    private static string Recortar(string t) => t.Length <= 40 ? t : t[..37] + "…";

    // ── 5 ───────────────────────────────────────────────────────────────────
    private static void NiUnaRayaLarga()
    {
        var conRaya = new List<string>();
        foreach (var f in Ficheros(Loc, "Textos.*.cs"))
        {
            var lineas = File.ReadAllLines(f);
            for (var i = 0; i < lineas.Length; i++)
                if (Regex.IsMatch(lineas[i], @"Elegir\([^)]*[–—]"))
                    conRaya.Add($"{Path.GetFileName(f)}:{i + 1}");
        }

        Program.Assert(
            conRaya.Count == 0,
            conRaya.Count == 0
                ? "Ni una raya larga en los textos"
                : $"{conRaya.Count} textos con raya larga: {string.Join(" · ", conRaya.Take(5))}");
    }
}
