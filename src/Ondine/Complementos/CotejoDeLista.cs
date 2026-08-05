using Ondine.Reindex;

namespace Ondine.Complementos;

/// <summary>
/// Qué trae una lista de fuera que tú no tengas ya.
///
/// <para>
/// Es lo que convierte un complemento de importación en algo que sirve. Sin
/// esto, una lista de cuatrocientos vídeos es una lista de cuatrocientos vídeos:
/// hay que ir uno por uno acordándose de qué se tiene. Cotejándola contra el
/// catálogo que está abierto en Organizar, la misma lista se convierte en la
/// respuesta a la única pregunta que importa — <i>¿qué me falta?</i>.
/// </para>
/// <para>
/// Reutiliza el motor de identificación entero: el mismo índice de títulos y el
/// mismo parecido que resuelven los ficheros del disco. Un vídeo de una lista y
/// un fichero de una carpeta son el mismo problema -un título suelto contra un
/// catálogo-, y resolverlo dos veces sería tener dos criterios distintos para lo
/// mismo, que es peor que tener uno imperfecto.
/// </para>
/// </summary>
public static class CotejoDeLista
{
    public enum Estado
    {
        /// <summary>Está entero en la biblioteca. No hay nada que traer.</summary>
        YaEsta,
        /// <summary>Tienes algunas de sus historias, pero no todas.</summary>
        AMedias,
        /// <summary>El catálogo lo conoce y tú no lo tienes.</summary>
        Falta,
        /// <summary>No se ha podido casar con ningún episodio del catálogo.</summary>
        Desconocido,
    }

    /// <param name="Titulo">El título tal cual viene de la lista.</param>
    /// <param name="Episodio">Con cuál casó, o null.</param>
    /// <param name="Estado">Qué hacer con él.</param>
    /// <param name="Parecido">Cuánto casó, para poder dudar del veredicto.</param>
    /// <param name="HistoriasQueFaltan">
    /// Las letras de las historias que no tienes («a», «b»). Vacía si lo tienes
    /// entero o si no casó con nada.
    /// </param>
    public sealed record Veredicto(
        string Titulo,
        CatalogEpisode? Episodio,
        Estado Estado,
        double Parecido,
        IReadOnlyList<string> HistoriasQueFaltan);

    /// <summary>
    /// Coteja los títulos de una lista contra el catálogo y contra lo que ya hay
    /// resuelto en la carpeta.
    /// </summary>
    /// <param name="titulos">Lo que devolvió el complemento.</param>
    /// <param name="catalogo">El que está abierto en Organizar.</param>
    /// <param name="loQueHay">
    /// Las filas ya resueltas. De ahí sale qué historias están cubiertas, con la
    /// misma cuenta que usa el informe de «qué falta»: sin letra, el fichero tapa
    /// el episodio entero; con letra, solo esas.
    /// </param>
    public static List<Veredicto> Cotejar(
        IEnumerable<string> titulos,
        ReindexCatalog catalogo,
        IReadOnlyList<ReindexResolution> loQueHay)
    {
        var cubierto = new Dictionary<int, HashSet<int>>();
        foreach (var r in loQueHay)
        {
            if (r.Episodio is not { } ep) continue;
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

        var indice = new IndiceTitulos(catalogo);
        var veredictos = new List<Veredicto>();

        foreach (var titulo in titulos)
        {
            var bolsa = TitleBag.From(titulo ?? "");
            if (bolsa.Text.Length < 4)
            {
                veredictos.Add(new(titulo ?? "", null, Estado.Desconocido, 0, Array.Empty<string>()));
                continue;
            }

            var (ep, score) = indice.MejorPorTitulo(new[] { bolsa }, catalogo.Episodios);

            // Por debajo del umbral no se afirma nada. Decir «esto te falta»
            // sobre un vídeo que en realidad ya tienes hace que te lo bajes dos
            // veces; decir «ya lo tienes» sobre uno que no, te lo hace perder.
            // Las dos equivocaciones cuestan, así que ante la duda: no sé.
            if (ep is null || score < TitleMatch.UmbralTitulo)
            {
                veredictos.Add(new(titulo ?? "", ep, Estado.Desconocido, score, Array.Empty<string>()));
                continue;
            }

            int historias = Math.Max(1, ep.TitulosSalida.Count);
            var tengo = cubierto.TryGetValue(ep.Num, out var set2) ? set2 : new HashSet<int>();

            var faltan = Enumerable.Range(0, historias)
                .Where(i => !tengo.Contains(i))
                .Select(i => ((char)('a' + i)).ToString())
                .ToList();

            var estado = faltan.Count == 0 ? Estado.YaEsta
                       : faltan.Count == historias ? Estado.Falta
                       : Estado.AMedias;

            // Con una sola historia no hay «a» que nombrar: o está o no está, y
            // enseñar una letra donde no hay partes confunde más que informa.
            veredictos.Add(new(titulo ?? "", ep, estado, score,
                historias > 1 ? faltan : Array.Empty<string>()));
        }

        return veredictos;
    }
}
