using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>
/// Qué falta en la biblioteca: compara lo que dice el catálogo con lo que hay en disco.
///
/// La cuenta va por SEGMENTO, no por episodio, y esa es la gracia. Un capítulo puede traer 2-3
/// mini-historias, y puede estar a medias: si tienes «1a» y «1b» pero no «1c», ese episodio no
/// está completo aunque aparezca en la carpeta. Contando por episodios eso se escapa.
/// </summary>
public static class CoberturaCatalogo
{
    /// <summary>Un episodio que falta, del todo o a trozos.</summary>
    public sealed record Hueco(CatalogEpisode Episodio, IReadOnlyList<string> Faltan, bool Entero)
    {
        /// <summary>«E12» o «E12b, E12c» — lo que se enseña en la lista.</summary>
        public string Codigo => Entero || Faltan.Count == 0
            ? $"E{Episodio.Num}"
            : string.Join(", ", Faltan.Select(s => $"E{Episodio.Num}{s}"));

        /// <summary>Los títulos de lo que falta, para que se sepa QUÉ es y no solo su número.</summary>
        public string Titulos
        {
            get
            {
                var t = Episodio.TitulosSalida;
                if (Entero || Faltan.Count == 0) return Episodio.TituloCompleto;
                return string.Join(" + ", Faltan
                    .Select(s => s[0] - 'a')
                    .Where(i => i >= 0 && i < t.Count)
                    .Select(i => t[i]));
            }
        }
    }

    public sealed record Informe(
        IReadOnlyList<Hueco> Huecos,
        int SegmentosTotales,
        int SegmentosPresentes,
        int EspecialesQueFaltan)
    {
        public int SegmentosQueFaltan => SegmentosTotales - SegmentosPresentes;
        public bool Completa => SegmentosQueFaltan <= 0;

        /// <summary>«De 636 historias tienes 343; faltan 293».</summary>
        public string Resumen => Completa
            ? string.Format(Textos.Instancia.ReindexCoberturaCompleta, SegmentosTotales)
            : string.Format(Textos.Instancia.ReindexCoberturaResumen,
                SegmentosTotales, SegmentosPresentes, SegmentosQueFaltan);
    }

    /// <summary>
    /// Todo lo que cubre un fichero: su episodio y, si el nombre es compuesto
    /// —«[1262+1264]»—, también las historias de los OTROS episodios que trae.
    ///
    /// <para>
    /// Existe porque contar solo <see cref="ReindexResolution.Episodio"/> hacía
    /// que la app dijera «te falta» de un episodio que estaba dentro del fichero
    /// que tenía delante, con su título escrito en el nombre. Está en un solo
    /// sitio para que el informe de «qué falta» y el cotejo de una lista no
    /// puedan volver a responder cosas distintas.
    /// </para>
    /// </summary>
    public static IEnumerable<(int Num, HashSet<int> Historias)> LoQueCubre(
        ReindexResolution r, ReindexCatalog catalogo)
    {
        if (r.Episodio is not { } ep) yield break;

        yield return (ep.Num, HistoriasQueCubre(r, ep));

        foreach (var mas in r.Archivo.TambienEpisodios)
        {
            var otro = catalogo.Episodios.FirstOrDefault(e => e.Num == mas.Num);
            if (otro is null) continue;

            // Con letra, esa historia; sin letra, el episodio entero — que es lo
            // que significa juntarlo sin más precisión.
            var cuantas = Math.Max(1, otro.TitulosSalida.Count);
            var suyas = new HashSet<int>();
            foreach (var c in mas.Segmento)
            {
                int i = char.ToLowerInvariant(c) - 'a';
                if (i >= 0 && i < cuantas) suyas.Add(i);
            }
            yield return (otro.Num, suyas.Count > 0 ? suyas : new HashSet<int>(Enumerable.Range(0, cuantas)));
        }
    }

    /// <summary>
    /// Qué historias de <paramref name="ep"/> tapa de verdad este fichero.
    ///
    /// <para>
    /// La regla de siempre —<b>sin letra de segmento, tapa el episodio entero</b>—
    /// es la buena para una biblioteca sin partir, y sigue siendo el respaldo.
    /// Pero se rompía en el caso más común: <c>S1986E985 - El controlador del
    /// mar</c> cuando el 985 trae dos historias. Sin letra se daba por completo, y
    /// entonces el cotejo de una lista de fuera contestaba «ya lo tienes» sobre un
    /// vídeo que traía justo la historia que falta.
    /// </para>
    /// <para>
    /// El nombre ya lo dice: nombra una de las dos. Así que <b>si el título del
    /// fichero se parece a algunas de las historias y no al episodio entero, tapa
    /// solo esas</b>. Y si no se parece a ninguna no se deduce nada: convertir un
    /// fichero completo en «te falta la mitad» es el error contrario, y también
    /// cuesta —te vuelves a bajar lo que ya tienes—.
    /// </para>
    /// <para>
    /// Está aquí y no repartido porque la misma cuenta la hacían tres sitios: el
    /// informe de «qué falta», el distintivo del explorador y el cotejo de listas.
    /// Tres copias de una regla son tres criterios en cuanto alguien toca una.
    /// </para>
    /// </summary>
    public static HashSet<int> HistoriasQueCubre(ReindexResolution r, CatalogEpisode ep)
    {
        int cuantas = Math.Max(1, ep.TitulosSalida.Count);
        var todas = new HashSet<int>(Enumerable.Range(0, cuantas));

        // La letra explícita manda: quien la escribió sabe más que cualquier
        // deducción sacada del título.
        var seg = r.Archivo.SubSegmento;
        if (!string.IsNullOrEmpty(seg))
        {
            var suyas = new HashSet<int>();
            foreach (var c in seg)
            {
                int i = char.ToLowerInvariant(c) - 'a';
                if (i >= 0 && i < cuantas) suyas.Add(i);
            }
            return suyas.Count > 0 ? suyas : todas;
        }

        if (cuantas == 1) return todas;

        var titulo = r.Archivo.TituloNombre;
        if (string.IsNullOrWhiteSpace(titulo)) return todas;

        // ¿A qué historias se parece el nombre? Se comparan sus trozos contra cada
        // historia, así que «A + C» tapa la a y la c, y «A + B» las tapa las dos
        // —que es como se resuelve solo el caso de la biblioteca sin partir—.
        //
        // Aquí NO se pregunta antes «¿se parece al episodio entero?». Se probó, y
        // con historias de nombre parecido el conjunto pasaba el umbral por pura
        // acumulación: «Uno de tres + Tres de tres» da 0,80 contra «Uno de tres +
        // Dos de tres + Tres de tres», y un fichero con dos de las tres se daba por
        // completo. Contar cuáles nombra es directo y no depende de esa suma.
        var nombradas = new HashSet<int>();
        for (int i = 0; i < cuantas && i < ep.TitulosSalida.Count; i++)
            if (TitleMatch.SimRaw(titulo, ep.TitulosSalida[i]) >= TitleMatch.UmbralSegmento ||
                Trozos(titulo).Any(t => TitleMatch.SimRaw(t, ep.TitulosSalida[i]) >= TitleMatch.UmbralSegmento))
                nombradas.Add(i);

        // Nada reconocible en el nombre: no se deduce, se respalda en la regla vieja.
        return nombradas.Count > 0 ? nombradas : todas;
    }

    // El MISMO separador de historias que usan el motor y el cotejo de listas.
    private static readonly System.Text.RegularExpressions.Regex RxTrozos =
        new(@"\s*[┃|+]\s*|\s+[-–—]\s+");

    private static IEnumerable<string> Trozos(string titulo) =>
        RxTrozos.Split(titulo).Select(t => t.Trim()).Where(t => t.Length > 0);

    /// <summary>Qué hay de un episodio: nada, a medias o entero.</summary>
    public enum Tengo { Nada, AMedias, Entero }

    /// <summary>Lo que hay de un episodio, y en qué ficheros está.</summary>
    public sealed record Tenencia(Tengo Que, IReadOnlyList<string> Ficheros);

    /// <summary>
    /// La cobertura mirada del derecho: <b>una respuesta por episodio</b>, y con la
    /// ruta de los ficheros que la sostienen.
    ///
    /// <para>
    /// <see cref="Calcular"/> devuelve la lista de huecos, que es lo que hace falta
    /// para enseñar «qué me falta». Pero en el explorador del catálogo se está
    /// mirando UN episodio y la pregunta es la contraria —«¿este lo tengo, y
    /// dónde?»—, así que buscar en una lista de huecos sería contestar por
    /// ausencia: no saldría el fichero.
    /// </para>
    /// <para>
    /// Todos los episodios del catálogo salen en el resultado, también los que no
    /// están. Una clave ausente obligaría a quien pinta a decidir qué significa, y
    /// «no lo encuentro» y «no lo tienes» no son lo mismo.
    /// </para>
    /// </summary>
    public static Dictionary<int, Tenencia> PorEpisodio(
        ReindexCatalog catalogo, IReadOnlyList<ReindexResolution> resoluciones)
    {
        // Qué segmentos cubre cada episodio, y con qué ficheros. Mismo criterio que
        // en Calcular: sin letra, el fichero es el episodio entero.
        var cubierto = new Dictionary<int, HashSet<int>>();
        var ficheros = new Dictionary<int, List<string>>();

        foreach (var r in resoluciones)
        {
            foreach (var (num, historias) in LoQueCubre(r, catalogo))
            {
                if (!cubierto.TryGetValue(num, out var set))
                {
                    cubierto[num] = set = new HashSet<int>();
                    ficheros[num] = new List<string>();
                }

                set.UnionWith(historias);

                if (!string.IsNullOrEmpty(r.Archivo.Path)) ficheros[num].Add(r.Archivo.Path);
            }
        }

        var mapa = new Dictionary<int, Tenencia>();
        foreach (var ep in catalogo.Regulares.Concat(catalogo.Especiales))
        {
            int cuantos = Math.Max(1, ep.TitulosSalida.Count);
            int tiene = cubierto.TryGetValue(ep.Num, out var set) ? set.Count : 0;
            mapa[ep.Num] = new Tenencia(
                tiene == 0 ? Tengo.Nada : tiene >= cuantos ? Tengo.Entero : Tengo.AMedias,
                ficheros.TryGetValue(ep.Num, out var f) ? f : Array.Empty<string>());
        }
        return mapa;
    }

    /// <summary>Las temporadas del catálogo, en orden, para ofrecerlas donde haga falta elegir.</summary>
    public static IReadOnlyList<int> TemporadasDe(ReindexCatalog catalogo) =>
        catalogo.Regulares.Where(e => e.Temporada.HasValue).Select(e => e.Temporada!.Value)
                .Distinct().OrderBy(t => t).ToList();

    /// <summary>
    /// ¿Este catálogo numera las temporadas por AÑO? Doraemon (2005) lo hace, y ahí poner
    /// «Temporada 2005» chirría: es el año de emisión. Se mira si todas caen en un rango de años
    /// creíble, que es lo único que distingue un 2005-temporada de un 2005-año.
    /// </summary>
    public static bool TemporadasSonAnios(ReindexCatalog catalogo)
    {
        var t = TemporadasDe(catalogo);
        return t.Count > 0 && t.All(x => x >= 1900 && x <= 2200);
    }

    /// <summary>
    /// Compara el catálogo con lo identificado.
    ///
    /// Por defecto solo se miran las temporadas de las que hay ALGO: si tienes la 1 y el catálogo
    /// trae 16, listar las otras 15 enteras no informa de nada — ya sabes que no las tienes. Con
    /// <paramref name="soloTemporadasConAlgo"/> a false salen todas, para cuando lo que quieres es
    /// justo saber qué te queda por conseguir.
    /// </summary>
    /// <param name="temporada">
    /// Si se indica, se mira SOLO esa temporada y las cuentas son suyas. Con 16 temporadas, «qué
    /// falta» en bloque no dice mucho; lo útil es saber qué queda de la que estás completando.
    /// </param>
    public static Informe Calcular(ReindexCatalog catalogo,
                                   IReadOnlyList<ReindexResolution> resoluciones,
                                   bool soloTemporadasConAlgo = true,
                                   int? temporada = null)
    {
        // Qué segmentos cubre cada episodio. Sin letra, el fichero es el episodio entero.
        var cubierto = new Dictionary<int, HashSet<int>>();
        foreach (var r in resoluciones)
        {
            if (r.Episodio is not { } ep) continue;         // sin identificar no tapa ningún hueco
            if (!cubierto.TryGetValue(ep.Num, out var set))
                cubierto[ep.Num] = set = new HashSet<int>();

            set.UnionWith(HistoriasQueCubre(r, ep));
        }

        var temporadasConAlgo = resoluciones
            .Where(r => r.Episodio != null)
            .Select(r => r.Episodio!.Temporada)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToHashSet();

        var huecos = new List<Hueco>();
        int total = 0, presentes = 0;

        foreach (var ep in catalogo.Regulares.OrderBy(e => e.Num))
        {
            // Con una temporada pedida, manda ella: el resto ni se mira, ni para las cuentas.
            if (temporada.HasValue && ep.Temporada != temporada.Value) continue;
            if (!temporada.HasValue && soloTemporadasConAlgo && ep.Temporada.HasValue &&
                !temporadasConAlgo.Contains(ep.Temporada.Value)) continue;

            int cuantos = Math.Max(1, ep.TitulosSalida.Count);
            total += cuantos;
            var tiene = cubierto.TryGetValue(ep.Num, out var set) ? set : new HashSet<int>();
            presentes += tiene.Count;
            if (tiene.Count >= cuantos) continue;

            // Sin nada de nada, falta entero: no se enumeran sus letras una a una, que solo
            // alarga la lista sin decir más.
            if (tiene.Count == 0)
            {
                huecos.Add(new Hueco(ep, Array.Empty<string>(), true));
                continue;
            }
            var faltan = Enumerable.Range(0, cuantos)
                .Where(i => !tiene.Contains(i))
                .Select(i => ((char)('a' + i)).ToString())
                .ToList();
            huecos.Add(new Hueco(ep, faltan, false));
        }

        // Los especiales van aparte: casi nadie los tiene completos y, mezclados, hacen que la
        // biblioteca parezca mucho peor de lo que está.
        int especiales = catalogo.Especiales.Count(e =>
            !cubierto.TryGetValue(e.Num, out var s) || s.Count == 0);

        return new Informe(huecos, total, presentes, especiales);
    }
}
