// El SDK de consola trae System.IO en los usings implícitos y el de WPF no, así que este
// fichero compila en los tests pero no en la app si no se pone a mano.
using System.IO;
using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>
/// Recorre la carpeta de una serie ENTERA, subcarpetas de temporada incluidas, y reparte
/// los ficheros por la temporada a la que pertenecen.
///
/// Existe porque una biblioteca de verdad no es una carpeta plana: «Doraemon (2005)» son
/// nueve «Season 20xx» y ni un solo vídeo en la raíz. Mirando solo el primer nivel, esa
/// carpeta con cientos de ficheros se veía como «No hay vídeos en esta carpeta», que es
/// justo lo contrario de lo que pasaba.
/// </summary>
public static class LibraryScan
{
    /// <summary>Carpetas cuyo nombre no dice ninguna temporada: van tras las que sí.</summary>
    private const int OrdenSinNumero = int.MaxValue - 1;

    /// <summary>Y los vídeos sueltos de la raíz, los últimos: son la excepción.</summary>
    private const int OrdenRaiz = int.MaxValue;

    /// <summary>
    /// Los especiales, tras las temporadas normales y antes de lo que no dice nada.
    ///
    /// <para>
    /// Antes esto salía solo, porque «Especiales» no se reconocía como temporada y caía en
    /// <see cref="OrdenSinNumero"/>. Ahora SÍ se reconoce —es la temporada 0, que es como la
    /// llaman Plex y Jellyfin— y sin esto se ordenaría la PRIMERA, delante de la temporada 1.
    /// Lo que era un efecto secundario pasa a estar dicho.
    /// </para>
    /// </summary>
    private const int OrdenEspeciales = int.MaxValue - 2;

    /// <summary>
    /// El rótulo de la banda de los vídeos sueltos. Deja de ser <c>const</c> porque
    /// ahora depende del idioma elegido, que se sabe en ejecución y no al compilar.
    /// </summary>
    public static string EtiquetaRaiz => Textos.Instancia.OrganizarSueltosRaiz;

    /// <summary>
    /// Los vídeos de <paramref name="raiz"/> y de todo lo que cuelga de ella, ya en el orden
    /// en que se van a leer: temporada por temporada y alfabético dentro de cada una.
    ///
    /// Las extensiones entran por parámetro en vez de leerse de <c>Engine</c> para que este
    /// fichero siga compilando —y testeándose— fuera del proyecto de Windows.
    /// </summary>
    public static string[] Escanear(string raiz, IReadOnlyCollection<string> extensiones)
    {
        if (!Directory.Exists(raiz)) return Array.Empty<string>();

        var opciones = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            // Una subcarpeta sin permisos no puede tumbar el escaneo de la biblioteca entera
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System,
        };

        return Directory.EnumerateFiles(raiz, "*", opciones)
            .Where(f => extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(f => (Ruta: f, Grupo: Grupo(raiz, f)))
            .OrderBy(x => Orden(x.Grupo))
            .ThenBy(x => x.Grupo, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => Path.GetFileName(x.Ruta), StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Ruta)
            .ToArray();
    }

    // ───────────────────────── varias carpetas a la vez ─────────────────────────

    /// <summary>
    /// Deja la lista de carpetas lista para recorrer: rutas completas, sin vacías ni
    /// repetidas y SIN las que cuelgan de otra de la lista.
    ///
    /// <para>
    /// Lo último importa: si «Doraemon» y «Doraemon/Season 1979» entran las dos, los
    /// ficheros de la segunda salen dos veces y, peor, unos cuelgan de «Season 1979» y otros
    /// de la raíz según se mire. Con la de arriba basta, que ya incluye a la de abajo.
    /// </para>
    /// </summary>
    public static List<string> NormalizarRaices(IEnumerable<string?> raices)
    {
        var completas = raices
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => Path.GetFullPath(r!.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .Select(r => r.Length == 0 ? Path.DirectorySeparatorChar.ToString() : r)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return completas
            .Where(r => !completas.Any(otra =>
                !string.Equals(otra, r, StringComparison.OrdinalIgnoreCase) && CuelgaDe(r, otra)))
            .ToList();
    }

    private static bool CuelgaDe(string ruta, string raiz)
    {
        var base_ = raiz.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return ruta.Length > base_.Length
            && ruta.StartsWith(base_, StringComparison.OrdinalIgnoreCase)
            && (ruta[base_.Length] == Path.DirectorySeparatorChar || ruta[base_.Length] == Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Como <see cref="Escanear(string, IReadOnlyCollection{string})"/>, pero sobre varias
    /// carpetas a la vez, para una serie repartida entre discos. Cada fichero se agrupa y se
    /// ordena respecto a SU carpeta de origen, y los grupos del mismo nombre se funden: la
    /// «Season 1979» del disco A y la del disco B salen juntas.
    ///
    /// <para>
    /// Con una sola carpeta da exactamente lo mismo que la versión de una carpeta.
    /// </para>
    /// </summary>
    public static string[] Escanear(IReadOnlyList<string> raices, IReadOnlyCollection<string> extensiones)
    {
        var utiles = NormalizarRaices(raices).Where(Directory.Exists).ToList();
        if (utiles.Count == 0) return Array.Empty<string>();

        var opciones = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System,
        };

        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var todos = new List<(string Ruta, string Grupo)>();
        foreach (var raiz in utiles)
            foreach (var f in Directory.EnumerateFiles(raiz, "*", opciones))
            {
                if (!extensiones.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
                if (vistos.Add(f)) todos.Add((f, Grupo(raiz, f)));
            }

        return todos
            .OrderBy(x => Orden(x.Grupo))
            .ThenBy(x => x.Grupo, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => Path.GetFileName(x.Ruta), StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.Ruta)
            .ToArray();
    }

    /// <summary>
    /// De qué carpeta de la lista cuelga un fichero: la más profunda que lo contiene. Si no
    /// cuelga de ninguna (un fichero añadido por un complemento, por ejemplo) devuelve la
    /// primera, que es el comportamiento de siempre con una sola carpeta.
    /// </summary>
    public static string RaizDe(IReadOnlyList<string> raices, string ruta)
    {
        string? mejor = null;
        foreach (var r in raices)
            if (CuelgaDe(Path.GetFullPath(ruta), Path.GetFullPath(r)) &&
                (mejor == null || r.Length > mejor.Length))
                mejor = r;
        return mejor ?? (raices.Count > 0 ? raices[0] : "");
    }

    /// <summary>
    /// Las temporadas (grupos) que hay entre estos ficheros, con cuántos ficheros trae cada
    /// una, en el orden en que se enseñan. Para que se pueda elegir cuáles analizar.
    /// </summary>
    public static List<(string Grupo, int Cuenta)> Temporadas(
        IReadOnlyList<string> raices, IEnumerable<string> ficheros) =>
        ficheros
            .GroupBy(f => Grupo(RaizDe(raices, f), f), StringComparer.OrdinalIgnoreCase)
            .Select(g => (Grupo: g.Key, Cuenta: g.Count()))
            .OrderBy(x => Orden(x.Grupo))
            .ThenBy(x => x.Grupo, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Los ficheros que NO pertenecen a ninguna de las temporadas apartadas.</summary>
    public static string[] SinTemporadas(
        IReadOnlyList<string> raices, IEnumerable<string> ficheros, IReadOnlySet<string> apartadas) =>
        apartadas.Count == 0
            ? ficheros.ToArray()
            : ficheros.Where(f => !apartadas.Contains(Grupo(RaizDe(raices, f), f))).ToArray();

    /// <summary>
    /// A qué carpeta de temporada pertenece un fichero: el PRIMER tramo bajo la raíz. Se
    /// queda con el primero y no con el padre inmediato para que un «Season 2005/Extras»
    /// siga contando como 2005 en vez de abrir un grupo suelto.
    /// Cadena vacía = está en la propia raíz.
    /// </summary>
    public static string Grupo(string raiz, string ruta)
    {
        var dir = Path.GetDirectoryName(ruta);
        if (string.IsNullOrEmpty(dir)) return "";

        var rel = Path.GetRelativePath(raiz, dir);
        if (rel.Length == 0 || rel == ".") return "";
        if (rel.StartsWith("..", StringComparison.Ordinal)) return "";   // ni siquiera cuelga de la raíz

        return rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    }

    /// <summary>Cómo se llama el grupo en la tabla.</summary>
    public static string Etiqueta(string grupo) => grupo.Length == 0 ? EtiquetaRaiz : grupo;

    /// <summary>Por dónde va cada grupo al ordenar. Ver las constantes de arriba.</summary>
    public static int Orden(string grupo)
    {
        if (grupo.Length == 0) return OrdenRaiz;

        var t = SignalExtractor.TemporadaDeCarpeta(grupo);
        if (t is null) return OrdenSinNumero;
        return t == 0 ? OrdenEspeciales : t.Value;
    }
}
