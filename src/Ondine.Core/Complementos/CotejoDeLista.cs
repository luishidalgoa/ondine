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
    /// <param name="SegmentosSinCasar">
    /// Los trozos del título de la lista que el catálogo no reconoce. Casi
    /// siempre es una historia que el catálogo no tiene apuntada, y decirlo
    /// importa: el vídeo trae algo que no está en ninguna cuenta, ni en la de lo
    /// que tienes ni en la de lo que falta.
    /// </param>
    /// <param name="TitulosQueFaltan">
    /// Las historias que faltan, por su nombre. «Te falta la b» obliga a ir al
    /// catálogo a mirar qué era la b.
    /// </param>
    public sealed record Veredicto(
        string Titulo,
        CatalogEpisode? Episodio,
        Estado Estado,
        double Parecido,
        IReadOnlyList<string> HistoriasQueFaltan,
        IReadOnlyList<string> SegmentosSinCasar,
        IReadOnlyList<string> TitulosQueFaltan);

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
        var cubierto = new Dictionary<EpisodeKey, HashSet<int>>();
        foreach (var r in loQueHay)
        foreach (var (num, temporada, historias) in CoberturaCatalogo.LoQueCubre(r, catalogo))
        {
            var clave = new EpisodeKey(temporada, num);
            if (!cubierto.TryGetValue(clave, out var set))
                cubierto[clave] = set = new HashSet<int>();

            // La MISMA cuenta que usa el informe de «qué falta» y el distintivo del
            // explorador. Tenerla repetida aquí era tener tres criterios para lo
            // mismo, y por eso este cotejo decía «ya lo tienes» de un episodio del
            // que solo había una de sus dos historias.
            set.UnionWith(historias);
        }

        var indice = new IndiceTitulos(catalogo);
        var veredictos = new List<Veredicto>();

        foreach (var titulo in titulos)
        {
            var entero = titulo ?? "";
            var vacio = Array.Empty<string>();

            // TROZO A TROZO, no la cadena entera. Un vídeo de media hora suele
            // traer dos historias, y basta con que el catálogo desconozca UNA
            // para que el parecido del conjunto caiga por debajo del umbral: el
            // episodio entero pasaba a «no se sabe» cuando por su primera
            // historia se sabía perfectamente cuál era.
            //
            // Cada trozo se compara como lo que es -un título suelto contra el
            // catálogo-, que es exactamente lo que el catálogo sabe responder.
            var trozos = Trocear(entero);

            CatalogEpisode? ep = null;
            double mejor = 0;
            var huerfanos = new List<string>();

            // Lo que el vídeo TRAE, cada cosa con el episodio al que pertenece.
            // Puede haber más de un episodio: un vídeo de media hora junta a menudo
            // dos entradas del catálogo, y antes solo se miraba la que mejor casaba
            // -así que la otra no aparecía ni entre lo que tienes ni entre lo que
            // falta, y el vídeo se daba por completo teniendo la mitad-.
            var trae = new List<(CatalogEpisode Ep, int Historia)>();

            foreach (var trozo in trozos)
            {
                var bolsa = TitleBag.From(trozo);
                if (bolsa.Text.Length < 4) { huerfanos.Add(trozo); continue; }

                var (suyo, score) = indice.MejorPorTitulo(new[] { bolsa }, catalogo.Episodios);

                // Por debajo del umbral no se afirma nada. Decir «esto te falta»
                // sobre un vídeo que en realidad ya tienes hace que te lo bajes
                // dos veces; decir «ya lo tienes» sobre uno que no, te lo hace
                // perder. Las dos cuestan, así que ante la duda: no sé.
                if (suyo is null || score < TitleMatch.UmbralTitulo)
                {
                    huerfanos.Add(trozo);
                    continue;
                }

                // El número que se ENSEÑA sigue siendo el del trozo que mejor casa:
                // es el que sitúa el vídeo. Pero ya no descarta a los demás.
                if (ep is null || score > mejor) { ep = suyo; mejor = score; }

                int cual = QueHistoria(suyo, trozo);
                trae.Add((suyo, cual >= 0 ? cual : 0));
            }

            if (ep is null)
            {
                veredictos.Add(new(entero, null, Estado.Desconocido, mejor, vacio, huerfanos, vacio));
                continue;
            }

            // Se pregunta por CADA cosa que trae, en su episodio. Un mismo trozo
            // repetido no cuenta dos veces.
            var piezas = trae.Distinct().ToList();
            var quedan = piezas
                .Where(p => !(cubierto.TryGetValue(catalogo.ClaveDe(p.Ep), out var s) && s.Contains(p.Historia)))
                .ToList();

            var nombres = quedan
                .Select(p => p.Historia < p.Ep.TitulosSalida.Count
                    ? p.Ep.TitulosSalida[p.Historia]
                    : p.Ep.TituloCompleto)
                .Distinct()
                .ToList();

            // Las letras solo tienen sentido dentro de UN episodio; con varios, un
            // «te falta la b» no dice de cuál. Por eso lo que manda son los nombres.
            var faltan = quedan
                .Where(p => p.Ep.Num == ep.Num && p.Ep.TitulosSalida.Count > 1)
                .Select(p => ((char)('a' + p.Historia)).ToString())
                .ToList();

            var estado = quedan.Count == 0 ? Estado.YaEsta
                       : quedan.Count == piezas.Count ? Estado.Falta
                       : Estado.AMedias;

            // Un trozo que el catálogo no reconoce es también algo que no tienes:
            // no está contado en ninguna de las dos listas de arriba, y darlo por
            // completo es justo el error que se quiere evitar.
            if (huerfanos.Count > 0 && estado == Estado.YaEsta) estado = Estado.AMedias;

            veredictos.Add(new(entero, ep, estado, mejor, faltan, huerfanos, nombres));
        }

        return veredictos;
    }

    /// <summary>
    /// El mismo separador de historias que usa el motor con los nombres de
    /// fichero. Uno propio aquí sería un segundo criterio para lo mismo, y en
    /// cuanto alguien tocara uno los dos dejarían de decir lo mismo.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex RxTrozos =
        new(@"\s*[┃|+]\s*|\s+[-–—]\s+");

    private static List<string> Trocear(string titulo)
    {
        var trozos = RxTrozos.Split(titulo)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();

        // Sin separadores el título es un trozo y ya. Devolver la lista vacía
        // dejaría sin cotejar justo lo más común.
        return trozos.Count > 0 ? trozos : new List<string> { titulo.Trim() };
    }

    /// <summary>
    /// Cuál de las historias del episodio es este trozo, o -1 si no se aclara.
    /// </summary>
    private static int QueHistoria(CatalogEpisode ep, string trozo)
    {
        int cual = -1;
        double mejor = TitleMatch.UmbralSegmento;
        for (int i = 0; i < ep.TitulosSalida.Count; i++)
        {
            var s = TitleMatch.SimRaw(trozo, ep.TitulosSalida[i]);
            if (s > mejor) { mejor = s; cual = i; }
        }
        return cual;
    }
}
