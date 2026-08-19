using Ondine.Reindex;

namespace Ondine.Peliculas;

/// <summary>
/// Decidir cuál de los candidatos de TMDb es esta película — y cuándo no
/// decidirlo.
///
/// <para>
/// Esto es lo que Ondine aporta encima del proveedor. El dato lo da TMDb; lo que
/// no da nadie es <b>la cascada con la confianza a la vista</b>, que dice por qué
/// señal ha acertado y se planta cuando no lo tiene claro. Es la misma idea que
/// ya gobierna las series, con otras señales.
/// </para>
/// <para>
/// La regla que manda: <b>una película mal identificada es peor que una sin
/// identificar</b>. Si «El Padrino II» acaba renombrado como «El Padrino», meses
/// después nadie sabe qué pasó ni cuál era cuál. Así que aquí se es más estricto
/// que en el resto de la app: lo dudoso se enseña y <b>no se aplica</b>.
/// </para>
/// </summary>
public static class IdentificacionDePelicula
{
    public enum Grado
    {
        /// <summary>No hay nada que aplicar.</summary>
        Ninguna,

        /// <summary>Hay una candidata plausible, pero se enseña y no se toca.</summary>
        Dudosa,

        /// <summary>Las señales cuadran: esto se puede aplicar.</summary>
        Segura,
    }

    /// <summary>Por qué señal se decidió, que es lo que se le enseña al usuario.</summary>
    public enum Porque
    {
        /// <summary>El buscador no devolvió nada.</summary>
        SinCandidatos,

        /// <summary>Devolvió cosas, pero ninguna se parece al título del fichero.</summary>
        TituloFlojo,

        /// <summary>Dos candidatas igual de buenas y nada que las separe.</summary>
        Empate,

        /// <summary>Título y año cuadran.</summary>
        AnioYTitulo,

        /// <summary>Cuadró por el título ORIGINAL, no por el traducido.</summary>
        TituloOriginal,

        /// <summary>El título cuadra y el año no. Puede ser un remake.</summary>
        SoloTitulo,

        /// <summary>El fichero no traía año; se decidió solo con el título.</summary>
        SinAnio,
    }

    /// <summary>Lo que se ha decidido, con la señal y la confianza a la vista.</summary>
    public sealed record Veredicto(Tmdb.Candidato? Elegido, Grado Grado, double Confianza, Porque Senal)
    {
        /// <summary>
        /// Solo lo seguro se aplica. Una duda se enseña, y la decide una persona:
        /// es la regla que ya sigue Organizar con los episodios.
        /// </summary>
        public bool SePuedeAplicar =>
            Elegido is not null && Grado == IdentificacionDePelicula.Grado.Segura;
    }

    /// <summary>
    /// Cuánto tiene que parecerse el título cuando <b>no hay año</b> para
    /// corroborarlo.
    ///
    /// <para>
    /// Es más alto que <see cref="TitleMatch.UmbralTitulo"/> (0,78) a propósito.
    /// Ese umbral vale para episodios, donde una equivocación se ve en el acto
    /// porque el número no cuadra con la lista. Aquí no hay lista: la única
    /// señal es el parecido, y en la franja de 0,78 a 0,95 caben cosas como «El
    /// padrino parte» contra «El padrino parte II», que es exactamente el error
    /// que no se puede cometer.
    /// </para>
    /// </summary>
    public const double CasiCalcado = 0.95;

    /// <summary>
    /// Cuánto tiene que descolgarse la segunda para que la primera gane. Por
    /// debajo de esto son un empate, y un empate no se resuelve a cara o cruz.
    /// </summary>
    private const double Distancia = 0.10;

    private readonly record struct Pesado(Tmdb.Candidato C, double Sim, bool PorOriginal);

    public static Veredicto Decidir(TituloDePelicula.Ficha ficha, IReadOnlyList<Tmdb.Candidato> candidatos)
    {
        if (candidatos is null || candidatos.Count == 0)
            return new(null, Grado.Ninguna, 0, Porque.SinCandidatos);

        var norm = TitleMatch.Norm(ficha.Titulo);
        if (norm.Length == 0) return new(null, Grado.Ninguna, 0, Porque.TituloFlojo);

        // Se cotejan LOS DOS títulos de cada candidata, y gana el mejor. Sin
        // esto no hay forma de saber que «The commuter» y «El pasajero» son la
        // misma película, y en una biblioteca real están las dos formas mezcladas.
        var pesados = candidatos
            .Select(c =>
            {
                var traducido = TitleMatch.Sim(norm, TitleMatch.Norm(c.Titulo));
                var original = c.Original is null ? 0 : TitleMatch.Sim(norm, TitleMatch.Norm(c.Original));
                return new Pesado(c, Math.Max(traducido, original), original > traducido);
            })
            .OrderByDescending(x => x.Sim)
            .ToList();

        var buenos = pesados.Where(x => x.Sim >= TitleMatch.UmbralTitulo).ToList();
        if (buenos.Count == 0)
            return new(null, Grado.Ninguna, pesados[0].Sim * 0.2, Porque.TituloFlojo);

        return ficha.Anio is { } anio ? ConAnio(buenos, anio) : SinAnioQueValga(buenos);
    }

    /// <summary>
    /// Con año en el fichero, que es el caso bueno: el año es lo que separa una
    /// película de su remake y de su saga.
    /// </summary>
    private static Veredicto ConAnio(List<Pesado> buenos, int anio)
    {
        // Un año de diferencia es el estreno en otro país, no otra película.
        // Exigirlo exacto plantaría la mitad de una biblioteca real.
        var porAnio = buenos.Where(x => x.C.Anio is { } a && Math.Abs(a - anio) <= 1).ToList();

        if (porAnio.Count == 1)
            return Seguro(porAnio[0], 0.85, Porque.AnioYTitulo);

        if (porAnio.Count > 1)
        {
            // Dos con el mismo año y el título parecido. Solo vale si una gana
            // de calle; si no, es un empate y no se toca.
            var mejor = porAnio[0];
            return mejor.Sim - porAnio[1].Sim >= Distancia
                ? Seguro(mejor, 0.80, Porque.AnioYTitulo)
                : new(null, Grado.Ninguna, 0.20, Porque.Empate);
        }

        // Ninguna cuadra por año: el título suena pero el año no. Puede ser el
        // remake, o la ficha del fichero estar mal. Se enseña y no se toca.
        return new(buenos[0].C, Grado.Dudosa, 0.35 + 0.25 * buenos[0].Sim, Porque.SoloTitulo);
    }

    /// <summary>
    /// Sin año no hay con qué corroborar, así que se exige más al título y no se
    /// admite competencia. Aun así hay que resolverlo: de las 75 películas de la
    /// biblioteca con la que se midió esto, <b>52 no traen año en el nombre</b>.
    /// </summary>
    private static Veredicto SinAnioQueValga(List<Pesado> buenos)
    {
        var mejor = buenos[0];

        if (buenos.Count > 1 && mejor.Sim - buenos[1].Sim < Distancia)
            return new(null, Grado.Ninguna, 0.20, Porque.Empate);

        if (mejor.Sim >= CasiCalcado)
            return Seguro(mejor, 0.70, Porque.SinAnio);

        return new(mejor.C, Grado.Dudosa, 0.35 + 0.25 * mejor.Sim, Porque.SinAnio);
    }

    /// <summary>
    /// Un veredicto seguro. <paramref name="suelo"/> es la confianza mínima de
    /// esa vía —con año se parte más arriba que sin él— y el parecido del título
    /// reparte el resto. <paramref name="senal"/> es la vía por la que se llegó,
    /// salvo que haya ganado el título original, que es lo primero que hay que
    /// contarle a quien lo mire.
    /// </summary>
    private static Veredicto Seguro(Pesado p, double suelo, Porque senal)
        => new(p.C, Grado.Segura, suelo + (1 - suelo) * p.Sim,
               p.PorOriginal ? Porque.TituloOriginal : senal);

    /// <summary>
    /// La ficha que se propone escribir en el disco: el título <b>del idioma de
    /// la app</b> y el año del proveedor.
    ///
    /// <para>
    /// El título traducido es lo que unifica una biblioteca donde media está en
    /// castellano y media en inglés, y el año lo aporta TMDb incluso cuando el
    /// nombre del fichero no lo traía — que es medio motivo de conectar esto.
    /// </para>
    /// </summary>
    public static TituloDePelicula.Ficha? Propuesta(Veredicto v)
        => v.Elegido is { } c ? new TituloDePelicula.Ficha(c.Titulo, c.Anio) : null;
}
