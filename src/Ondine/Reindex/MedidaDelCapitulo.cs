namespace Ondine.Reindex;

/// <summary>
/// Cuánto dura UNA historia en esta serie, y si una propuesta cuadra con el reloj.
///
/// <para>
/// El nombre de un fichero no siempre dice cuántas historias trae. Un
/// «Doraemon (1979) S1981E618 [618] - Doraemon, te odio.avi» anuncia una sola, y
/// aun así la app estuvo a punto de darlo por bueno como un episodio que el
/// catálogo cuenta con DOS. Por el título solo no se distingue: el 0,84 de
/// parecido sale igual si el fichero trae media hora o once minutos.
/// </para>
/// <para>
/// El reloj sí lo distingue. Si en esta serie una historia dura unos once
/// minutos, un fichero de once minutos NO puede ser un episodio de dos, y uno de
/// treinta y tres no es de una. Y funciona en los dos sentidos: caza tanto la
/// propuesta que junta de más como la que parte de menos.
/// </para>
/// <para>
/// No hay ningún número escrito a mano aquí. La duración de una historia se
/// APRENDE de la propia carpeta: la mediana de (duración ÷ historias) entre los
/// ficheros que la app identificó con confianza. Una serie de once minutos y otra
/// de cuarenta y cinco se miden cada una con su vara, y una serie que cambia de
/// formato a mitad no arrastra el número viejo.
/// </para>
/// </summary>
public static class MedidaDelCapitulo
{
    /// <summary>
    /// Cuántas observaciones hacen falta para fiarse de la mediana. Con menos, un
    /// par de ficheros raros la mueven entera y el aviso saldría en los buenos.
    /// </summary>
    public const int MinimoParaFiarse = 5;

    /// <summary>
    /// Cuánto puede alejarse la duración real de la esperada sin que sea sospecha.
    ///
    /// <para>
    /// Un 35 % puede sonar mucho, y es a propósito: entre la cabecera, los
    /// créditos y los cortes, dos capítulos de la misma serie se llevan minutos.
    /// El aviso tiene que saltar cuando el fichero mide la MITAD o el DOBLE de lo
    /// que debería -que es el caso que importa-, no cuando le sobran dos minutos.
    /// Un margen apretado convierte esto en ruido, y una comprobación que avisa de
    /// más se acaba ignorando.
    /// </para>
    /// </summary>
    public const double Margen = 0.35;

    /// <summary>
    /// Lo que dura una historia en esta serie: la mediana de (duración ÷ historias)
    /// entre los ficheros que se pudieron medir. <c>null</c> si no hay bastantes.
    /// </summary>
    /// <param name="observaciones">
    /// Pares (lo que dura el fichero, cuántas historias trae el episodio que le tocó).
    /// Solo deben entrar los identificados con confianza: aprender de las dudas es
    /// aprender del error.
    /// </param>
    public static TimeSpan? Unidad(IEnumerable<(TimeSpan Duracion, int Historias)> observaciones)
    {
        var porHistoria = observaciones
            .Where(o => o.Historias > 0 && o.Duracion > TimeSpan.Zero)
            .Select(o => o.Duracion.TotalSeconds / o.Historias)
            .OrderBy(s => s)
            .ToList();

        if (porHistoria.Count < MinimoParaFiarse) return null;

        // Mediana y no media: en una carpeta se cuela un tráiler de dos minutos o un
        // fichero doble mal identificado, y la media se va detrás de ellos.
        int medio = porHistoria.Count / 2;
        double segundos = porHistoria.Count % 2 == 1
            ? porHistoria[medio]
            : (porHistoria[medio - 1] + porHistoria[medio]) / 2.0;

        return TimeSpan.FromSeconds(segundos);
    }

    /// <summary>Las varas de una serie: la de cada temporada, y la de respaldo.</summary>
    public sealed class Varas
    {
        public TimeSpan? Global { get; init; }
        public IReadOnlyDictionary<int, TimeSpan> PorTemporada { get; init; } =
            new Dictionary<int, TimeSpan>();

        /// <summary>La vara con la que medir esta temporada. Cae a la global si no tiene la suya.</summary>
        public TimeSpan? De(int? temporada) =>
            temporada is { } t && PorTemporada.TryGetValue(t, out var u) ? u : Global;
    }

    /// <summary>
    /// La vara de cada temporada, con la global de respaldo.
    ///
    /// <para>
    /// Una sola mediana para toda la serie es un modelo equivocado en cuanto la
    /// serie cambia de formato, y las largas cambian. Medido en Doraemon (1979):
    /// una historia dura ~6:12 en 1979-1981, ~12:12 en 1985-1986 y <b>~23:35 en
    /// 1991-1993</b>. Con la mediana global —8:36— la app medía 1986 con la vara
    /// de 1979, y 67 ficheros perfectamente normales para su año salían marcados
    /// como sospechosos de traer dos historias.
    /// </para>
    /// <para>
    /// Una temporada con menos de <see cref="MinimoParaFiarse"/> muestras NO
    /// estrena vara propia: una mediana sacada de dos ficheros la mueve cualquier
    /// rareza, y entonces el aviso salta justo en los buenos. Esas se miden con la
    /// global, que es lo que se hacía antes con todas.
    /// </para>
    /// </summary>
    public static Varas UnidadPorTemporada(
        IEnumerable<(TimeSpan Duracion, int Historias, int? Temporada)> observaciones)
    {
        var todas = observaciones.ToList();

        var porTemporada = todas
            .Where(o => o.Temporada is not null)
            .GroupBy(o => o.Temporada!.Value)
            .Select(g => (Temporada: g.Key, Vara: Unidad(g.Select(o => (o.Duracion, o.Historias)))))
            .Where(x => x.Vara is not null)
            .ToDictionary(x => x.Temporada, x => x.Vara!.Value);

        return new Varas
        {
            Global = Unidad(todas.Select(o => (o.Duracion, o.Historias))),
            PorTemporada = porTemporada,
        };
    }

    /// <summary>
    /// ¿Cuadra que un fichero de esta duración sean <paramref name="historias"/>
    /// historias? Con <c>null</c> en cualquiera de los dos datos no se opina: no
    /// saber no es lo mismo que sospechar.
    /// </summary>
    public static bool Cuadra(TimeSpan? duracion, int historias, TimeSpan? unidad)
    {
        if (duracion is not { } d || unidad is not { } u || historias <= 0) return true;
        if (d <= TimeSpan.Zero || u <= TimeSpan.Zero) return true;

        double esperado = u.TotalSeconds * historias;
        return d.TotalSeconds >= esperado * (1 - Margen)
            && d.TotalSeconds <= esperado * (1 + Margen);
    }

    /// <summary>
    /// Cuántas historias dice el reloj que trae este fichero. <c>null</c> si no se
    /// puede saber. Sirve para redactar el aviso: «mide once minutos, y un episodio
    /// de dos historias dura veintidós».
    /// </summary>
    public static int? HistoriasQueSugiere(TimeSpan? duracion, TimeSpan? unidad)
    {
        if (duracion is not { } d || unidad is not { } u) return null;
        if (d <= TimeSpan.Zero || u <= TimeSpan.Zero) return null;

        int cuantas = (int)Math.Round(d.TotalSeconds / u.TotalSeconds, MidpointRounding.AwayFromZero);
        return cuantas < 1 ? 1 : cuantas;
    }
}
