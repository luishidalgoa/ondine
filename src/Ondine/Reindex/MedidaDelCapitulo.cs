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
    /// <summary>Contra qué se compara la duración de un fichero en esta serie.</summary>
    public enum Molde
    {
        /// <summary>Lo constante es la HISTORIA: el episodio dura lo que sumen las suyas.</summary>
        PorHistoria,
        /// <summary>Lo constante es el EPISODIO: dura lo mismo lleve dos historias o tres.</summary>
        PorEpisodio,
    }

    /// <summary>
    /// Cómo se mide esta serie, y cuánto.
    ///
    /// <para>
    /// Existe para que «la vara» sea UNA cosa. Antes andaba repartida entre un
    /// <c>TimeSpan?</c> suelto y las reglas que sabían qué hacer con él, y cada
    /// sitio que la usaba tenía que recordar la fórmula. Con un objeto, la fórmula
    /// vive donde vive la vara.
    /// </para>
    /// </summary>
    public sealed record Vara(Molde Molde, TimeSpan Unidad)
    {
        /// <summary>Lo que debería durar un fichero con <paramref name="historias"/> historias.</summary>
        public TimeSpan Espera(int historias) =>
            Molde == Molde.PorEpisodio ? Unidad : Unidad * Math.Max(1, historias);

        /// <summary>
        /// ¿Sirve el reloj para saber CUÁNTAS historias trae un fichero?
        ///
        /// <para>
        /// Solo si la unidad es la historia. Con el episodio por molde todos los
        /// ficheros duran lo mismo lleven dos historias o tres, así que la duración
        /// no distingue nada — y quien pregunte tiene que saberlo antes de sacar
        /// conclusiones. Sin esto, «¿dura lo que UNA historia?» contestaba que sí
        /// para cualquier episodio entero, y la app estuvo a punto de renombrar 33
        /// episodios completos como si fueran un trozo suyo.
        /// </para>
        /// </summary>
        public bool DistingueHistorias => Molde == Molde.PorHistoria;
    }

    /// <summary>
    /// Aprende la vara de esta carpeta: cuánto mide y, sobre todo, QUÉ es lo
    /// constante en esta serie.
    ///
    /// <para>
    /// El reloj comparaba siempre contra «lo que dura una historia × cuántas
    /// trae». Eso vale para las series donde la historia es la unidad y el
    /// episodio dura lo que sumen sus trozos. Pero hay series al revés: <b>el
    /// episodio dura siempre lo mismo</b> y dentro caben dos historias o tres
    /// según el día.
    /// </para>
    /// <para>
    /// Medido en una carpeta real de Crayon Shin-Chan: ficheros de 23:51 a 26:02,
    /// todos de unos 24 minutos, con episodios de 2 y de 3 historias mezclados.
    /// Dividiendo por historias salía 8:03 en unos y 13:01 en otros, y con la vara
    /// de 8:03 un episodio de 2 historias de 24 minutos se marcaba como sospechoso
    /// sin serlo.
    /// </para>
    /// <para>
    /// No hace falta configurarlo: <b>la serie lo dice sola</b>. Se mide cuánto
    /// varía cada lectura y gana la que menos. Con episodios de una sola historia
    /// las dos son la misma cuenta —dividir por uno no cambia nada— así que
    /// empatan y manda la de siempre.
    /// </para>
    /// </summary>
    public static Vara? Aprender(IEnumerable<(TimeSpan Duracion, int Historias)> observaciones)
    {
        var buenas = observaciones
            .Where(o => o.Historias > 0 && o.Duracion > TimeSpan.Zero)
            .ToList();
        if (buenas.Count < MinimoParaFiarse) return null;

        var porHistoria = buenas.Select(o => o.Duracion.TotalSeconds / o.Historias).ToList();
        var porEpisodio = buenas.Select(o => o.Duracion.TotalSeconds).ToList();

        // Dispersión RELATIVA, con mediana y desviación absoluta mediana: la media
        // y la desviación típica se van detrás de un tráiler de dos minutos o de un
        // especial, que es justo lo que hay suelto en una carpeta de verdad.
        //
        // Cambiar de molde tiene que EXIGIR evidencia, no ganar por un pelo. Si
        // todos los episodios tienen el mismo número de historias, una lectura es
        // la otra dividida por una constante y las dos dispersiones son la misma:
        // con un «menor que» a secas, quién gana lo decidía el ruido de la coma
        // flotante, y una serie normal acababa midiéndose por episodio sin motivo.
        //
        // Pedir la mitad deja pasar el caso real -en Shin-Chan la lectura por
        // historia se va al doble entre unos episodios y otros- y no el empate.
        return Dispersion(porEpisodio) < Dispersion(porHistoria) * MargenParaCambiarDeMolde
            ? new Vara(Molde.PorEpisodio, TimeSpan.FromSeconds(Mediana(porEpisodio)))
            : new Vara(Molde.PorHistoria, TimeSpan.FromSeconds(Mediana(porHistoria)));
    }

    private static double Mediana(List<double> xs)
    {
        var o = xs.OrderBy(x => x).ToList();
        int m = o.Count / 2;
        return o.Count % 2 == 1 ? o[m] : (o[m - 1] + o[m]) / 2.0;
    }

    /// <summary>
    /// Cuánto más estable tiene que ser la lectura por episodio para que se
    /// prefiera a la de siempre. La mitad: una diferencia clara, no un empate
    /// resuelto por decimales.
    /// </summary>
    private const double MargenParaCambiarDeMolde = 0.5;

    /// <summary>Desviación absoluta mediana partida por la mediana. Sin unidades, para poder comparar.</summary>
    private static double Dispersion(List<double> xs)
    {
        double med = Mediana(xs);
        if (med <= 0) return double.MaxValue;
        return Mediana(xs.Select(x => Math.Abs(x - med)).ToList()) / med;
    }

    /// <summary>¿Cuadra esta duración con este número de historias?</summary>
    public static bool Cuadra(TimeSpan? duracion, int historias, Vara? vara)
    {
        if (duracion is not { } d || vara is null || historias <= 0) return true;
        if (d <= TimeSpan.Zero || vara.Unidad <= TimeSpan.Zero) return true;

        double esperado = vara.Espera(historias).TotalSeconds;
        return d.TotalSeconds >= esperado * (1 - Margen)
            && d.TotalSeconds <= esperado * (1 + Margen);
    }

    /// <summary>Cuántas historias dice el reloj que trae este fichero. Null si no se sabe.</summary>
    /// <summary>
    /// Cuántas historias dice el reloj que trae este fichero.
    ///
    /// <para>
    /// Con el episodio por molde devuelve <c>null</c>: ahí el reloj no sabe cuántas
    /// historias hay, sabe cuántos EPISODIOS caben. Un fichero de 48 minutos en una
    /// serie de 24 son dos episodios, no seis historias, y decir lo segundo sería
    /// inventarse un número con cara de dato.
    /// </para>
    /// </summary>
    public static int? HistoriasQueSugiere(TimeSpan? duracion, Vara? vara) =>
        vara is null || vara.Molde == Molde.PorEpisodio
            ? null
            : HistoriasQueSugiere(duracion, vara.Unidad);

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
        public Vara? Global { get; init; }
        public IReadOnlyDictionary<int, Vara> PorTemporada { get; init; } =
            new Dictionary<int, Vara>();

        /// <summary>La vara con la que medir esta temporada. Cae a la global si no tiene la suya.</summary>
        public Vara? De(int? temporada) =>
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
            .Select(g => (Temporada: g.Key, Vara: Aprender(g.Select(o => (o.Duracion, o.Historias)))))
            .Where(x => x.Vara is not null)
            .ToDictionary(x => x.Temporada, x => x.Vara!);

        return new Varas
        {
            Global = Aprender(todas.Select(o => (o.Duracion, o.Historias))),
            PorTemporada = porTemporada,
        };
    }

    /// <summary>
    /// Cuántas historias dice el reloj que trae este fichero. <c>null</c> si no se
    /// puede saber. Sirve para redactar el aviso: «mide once minutos, y un episodio
    /// de dos historias dura veintidós».
    /// </summary>
    private static int? HistoriasQueSugiere(TimeSpan? duracion, TimeSpan? unidad)
    {
        if (duracion is not { } d || unidad is not { } u) return null;
        if (d <= TimeSpan.Zero || u <= TimeSpan.Zero) return null;

        int cuantas = (int)Math.Round(d.TotalSeconds / u.TotalSeconds, MidpointRounding.AwayFromZero);
        return cuantas < 1 ? 1 : cuantas;
    }
}
