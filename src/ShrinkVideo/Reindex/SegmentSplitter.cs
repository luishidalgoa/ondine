namespace ShrinkVideo.Reindex;

/// <summary>
/// Decide POR DÓNDE partir un episodio que trae varias mini-historias («segmentos»).
///
/// Muchas series de dibujos meten 2-3 historias en una misma emisión, separadas por un fundido
/// a negro con su tarjeta de título. El catálogo ya sabe CUÁNTAS son (los títulos del episodio),
/// y ffmpeg sabe DÓNDE están los fundidos: con esas dos cosas el corte se puede elegir solo, sin
/// que nadie se pelee con una línea de tiempo.
///
/// La regla que importa: entre los fundidos candidatos gana el más CERCANO al reparto esperado,
/// NO el que más dure. Medido sobre episodios reales, «el fundido más largo» se equivoca —hay
/// fundidos de escena más largos que el corte de verdad, y los créditos del final duran aún más—
/// mientras que la cercanía al reparto acierta incluso cuando las historias son muy desiguales.
/// </summary>
public static class SegmentSplitter
{
    /// <summary>Un fundido a negro detectado en el vídeo, en segundos.</summary>
    public readonly record struct Negro(double Inicio, double Fin)
    {
        public double Duracion => Fin - Inicio;
    }

    /// <summary>
    /// Un trozo a extraer. <see cref="Desde"/> arranca DESPUÉS del fundido y <see cref="Hasta"/>
    /// acaba justo cuando empieza el siguiente: así el negro no queda en ninguno de los dos.
    /// </summary>
    public readonly record struct Trozo(double Desde, double Hasta)
    {
        public double Duracion => Hasta - Desde;
    }

    public sealed record Plan(IReadOnlyList<Trozo> Trozos, bool Fiable, string Motivo);

    /// <summary>Un fundido más corto que esto es un parpadeo de escena, no un corte de historia.</summary>
    private const double DuracionMinima = 0.7;

    /// <summary>
    /// Cuánto se le permite alejarse del reparto teórico, como fracción de la duración total.
    /// Con 0,25 cabe de sobra un episodio tan desigual como «Se necesita ayudante (8:37) +
    /// Limpiaarrecifes (2:42) + Té en la bóveda (11:17)», y aun así se descartan los fundidos
    /// del arranque y de los créditos, que son los que engañan.
    /// </summary>
    private const double MargenRelativo = 0.25;

    /// <summary>
    /// Reparte el episodio en <paramref name="segmentos"/> trozos usando los fundidos detectados.
    /// Si no encuentra un fundido creíble para alguna junta, devuelve <c>Fiable = false</c> y NO
    /// se inventa el corte: partir por el medio a ciegas parte una escena y estropea los dos lados.
    /// </summary>
    public static Plan Planificar(double duracion, int segmentos, IReadOnlyList<Negro> negros)
    {
        if (segmentos <= 1 || duracion <= 0)
            return new Plan(new[] { new Trozo(0, duracion) }, true, "");

        // Se descartan los fundidos de arranque y de créditos: no separan historias, y el de los
        // créditos suele ser el más largo de todo el fichero.
        var utiles = negros
            .Where(n => n.Duracion >= DuracionMinima)
            .Where(n => n.Inicio > duracion * 0.03 && n.Inicio < duracion * 0.92)
            .OrderBy(n => n.Inicio)
            .ToList();

        var juntas = new List<Negro>();
        var margen = duracion * MargenRelativo;
        for (int i = 1; i < segmentos; i++)
        {
            double esperado = duracion * i / segmentos;
            // El mejor para esta junta, sin repetir los ya usados ni retroceder sobre el anterior.
            var minimo = juntas.Count > 0 ? juntas[^1].Fin : 0;
            var mejor = utiles
                .Where(n => n.Inicio > minimo && !juntas.Contains(n))
                .OrderBy(n => Math.Abs(n.Inicio - esperado))
                .FirstOrDefault();

            if (mejor == default || Math.Abs(mejor.Inicio - esperado) > margen)
                return new Plan(Array.Empty<Trozo>(), false,
                    $"No se ha encontrado un corte claro cerca del minuto {esperado / 60:0.0}. " +
                    "Ábrelo en Recortes y marca el corte a mano.");

            juntas.Add(mejor);
        }

        var trozos = new List<Trozo>();
        double desde = 0;
        foreach (var j in juntas)
        {
            trozos.Add(new Trozo(desde, j.Inicio));   // el trozo acaba donde EMPIEZA el negro
            desde = j.Fin;                            // y el siguiente arranca donde ACABA
        }
        trozos.Add(new Trozo(desde, duracion));
        return new Plan(trozos, true, "");
    }

    /// <summary>
    /// La letra que le toca a cada trozo: «a», «b», «c»… Es la notación con la que se numeran
    /// los segmentos («1a», «1b», «1c»), y la que el motor sabe leer de vuelta.
    /// </summary>
    public static string Letra(int indice) => ((char)('a' + indice)).ToString();
}
