namespace Ondine.Reindex;

/// <summary>
/// De qué es una carpeta: de una serie o de películas.
///
/// <para>
/// Hasta ahora Organizar solo entendía de series, y toda la cascada de
/// identificación gira alrededor de «número de episodio + temporada + título del
/// catálogo». A una película no le aplica nada de eso: no tiene temporada, ni
/// número, ni un catálogo de hermanas con las que compararse.
/// </para>
/// <para>
/// Por eso es una decisión al principio y no una casilla más: como el «¿qué tipo
/// de proyecto vas a empezar?» de otras herramientas. Preguntado una vez, evita
/// una pestaña llena de opciones que no aplican.
/// </para>
/// </summary>
public enum TipoDeBiblioteca
{
    /// <summary>Lo de siempre: catálogo, temporadas, números, segmentos.</summary>
    Serie = 0,

    /// <summary>Sin temporada ni número. El nombre se compone con título y año.</summary>
    Pelicula = 1,
}
