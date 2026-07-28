namespace ShrinkVideo.Reindex;

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
            ? $"No falta nada: están las {SegmentosTotales} historias."
            : $"De {SegmentosTotales} historias tienes {SegmentosPresentes}; faltan {SegmentosQueFaltan}.";
    }

    /// <summary>
    /// Compara el catálogo con lo identificado.
    ///
    /// Por defecto solo se miran las temporadas de las que hay ALGO: si tienes la 1 y el catálogo
    /// trae 16, listar las otras 15 enteras no informa de nada — ya sabes que no las tienes. Con
    /// <paramref name="soloTemporadasConAlgo"/> a false salen todas, para cuando lo que quieres es
    /// justo saber qué te queda por conseguir.
    /// </summary>
    public static Informe Calcular(ReindexCatalog catalogo,
                                   IReadOnlyList<ReindexResolution> resoluciones,
                                   bool soloTemporadasConAlgo = true)
    {
        // Qué segmentos cubre cada episodio. Sin letra, el fichero es el episodio entero.
        var cubierto = new Dictionary<int, HashSet<int>>();
        foreach (var r in resoluciones)
        {
            if (r.Episodio is not { } ep) continue;         // sin identificar no tapa ningún hueco
            if (!cubierto.TryGetValue(ep.Num, out var set))
                cubierto[ep.Num] = set = new HashSet<int>();

            int cuantos = Math.Max(1, ep.TitulosSalida.Count);
            var seg = r.Archivo.SubSegmento;
            if (string.IsNullOrEmpty(seg))
                for (int i = 0; i < cuantos; i++) set.Add(i);
            else
                foreach (var c in seg)
                {
                    int i = char.ToLowerInvariant(c) - 'a';
                    if (i >= 0 && i < cuantos) set.Add(i);
                }
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
            if (soloTemporadasConAlgo && ep.Temporada.HasValue &&
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
