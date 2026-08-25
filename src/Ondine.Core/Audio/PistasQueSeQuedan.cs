namespace Ondine.Audio;

/// <summary>Por qué una pista de audio se queda, o por qué no.</summary>
public enum PorQuePista
{
    /// <summary>Es del idioma preferido: se queda, y va primera.</summary>
    EsElPreferido,

    /// <summary>Su idioma está entre los elegidos.</summary>
    EstaEnLosElegidos,

    /// <summary>
    /// Su idioma no lo eligió nadie: entra por la lista de por defecto —el preferido y el
    /// inglés—, que la pone el código cuando no hay elección.
    ///
    /// <para>
    /// Tiene motivo propio a propósito. «¿Por qué se ha quedado el inglés, que yo no pedí?» es
    /// una pregunta razonable, y con un único motivo de «se queda» no tenía respuesta.
    /// </para>
    /// </summary>
    LaListaPorDefecto,

    /// <summary>Se dijo «todas».</summary>
    TodasSeConservan,

    /// <summary>No casaba ninguna, así que se conservan todas antes que dejar el vídeo mudo.</summary>
    NingunaCasaba,

    /// <summary>Se cae: su idioma no está entre los elegidos.</summary>
    NoEstaEnLosElegidos,
}

/// <summary>Una pista de audio y lo que va a pasar con ella.</summary>
/// <param name="Indice">El índice dentro del fichero: es el que va al <c>-map</c>.</param>
/// <param name="Idioma">El código de ffprobe, o cadena vacía si no trae etiqueta.</param>
public readonly record struct PistaElegida(int Indice, string Idioma, bool SeQueda, PorQuePista Motivo);

/// <summary>
/// Qué pistas de audio se conservan al comprimir, en qué orden, y por qué.
///
/// <para>
/// <b>Por qué existe.</b> Esta decisión estaba escrita en línea dentro del bucle de ficheros de
/// <c>Engine.CompressAsync</c> —tres expresiones LINQ seguidas—, así que no se podía probar ni
/// reutilizar. Y el estimador tenía SU PROPIA versión de la misma regla, que decía otra cosa: el
/// motor leía «lista vacía» como «el preferido y el inglés» y el estimador como «todas», de modo
/// que un fichero con tres idiomas se pronosticaba con tres pistas y salía con dos.
/// </para>
/// <para>
/// <b>Quién manda.</b> La lista de idiomas elegidos. El idioma preferido manda en el
/// <b>orden</b> —va primero, y es el que queda marcado por defecto al reproducir—, no en si se
/// conserva: antes se conservaba siempre, aunque su chip estuviera desmarcado, y eso convertía
/// ese control en un adorno para justo ese caso.
/// </para>
/// <para>
/// <b>Y siempre se sabe por qué.</b> Cada pista sale con su motivo, para que el registro pueda
/// decir cuál se ha caído y por qué en vez de «(descarto 1)», que es lo que había y no responde
/// a nada.
/// </para>
/// </summary>
public static class PistasQueSeQuedan
{
    /// <summary>El centinela que significa «no descartes ninguna».</summary>
    public const string Todas = "all";

    /// <summary>El idioma que se cuela cuando no hay ninguno elegido, además del preferido.</summary>
    public const string PorDefectoAdemas = "eng";

    /// <summary>
    /// La lista de idiomas que se va a usar de verdad: la elegida, o la de por defecto si no hay
    /// elección.
    ///
    /// <para>
    /// Es pública porque la interfaz necesita poder ENSEÑARLA: la lista implícita es la que hizo
    /// que a un usuario se le cayera el portugués sin saber que existía una lista.
    /// </para>
    /// </summary>
    public static IReadOnlyList<string> ListaEfectiva(IReadOnlyList<string>? elegidos, string? preferido)
    {
        if (elegidos is { Count: > 0 }) return elegidos;

        var pref = string.IsNullOrWhiteSpace(preferido) ? PorDefectoAdemas : preferido;
        return pref == PorDefectoAdemas ? [pref] : [pref, PorDefectoAdemas];
    }

    /// <summary>Cómo se llama una pista en el registro. Sin etiqueta de idioma, «?».</summary>
    public static string Rotulo(string? idioma) => string.IsNullOrEmpty(idioma) ? "?" : idioma;

    /// <summary>
    /// El plan, en el orden en que las pistas van a ir al fichero de salida: primero las del
    /// idioma preferido y después el resto, cada una en el orden en que venían.
    /// </summary>
    /// <param name="pistas">Las de audio del fichero, con su índice real.</param>
    /// <param name="preferido">El idioma preferido del usuario.</param>
    /// <param name="elegidos">Los idiomas marcados. Vacío = la lista por defecto.</param>
    public static IReadOnlyList<PistaElegida> Para(
        IReadOnlyList<(int Indice, string? Idioma)> pistas,
        string? preferido,
        IReadOnlyList<string>? elegidos)
    {
        if (pistas.Count == 0) return [];

        var lista = ListaEfectiva(elegidos, preferido);
        var porDefecto = elegidos is not { Count: > 0 };
        var todas = lista.Contains(Todas, StringComparer.OrdinalIgnoreCase);
        var pref = preferido ?? "";

        var plan = pistas.Select(p =>
        {
            var idioma = p.Idioma ?? "";

            if (todas) return new PistaElegida(p.Indice, idioma, true, PorQuePista.TodasSeConservan);

            if (!lista.Contains(idioma, StringComparer.OrdinalIgnoreCase))
                return new PistaElegida(p.Indice, idioma, false, PorQuePista.NoEstaEnLosElegidos);

            var motivo = idioma.Equals(pref, StringComparison.OrdinalIgnoreCase) ? PorQuePista.EsElPreferido
                       : porDefecto ? PorQuePista.LaListaPorDefecto
                       : PorQuePista.EstaEnLosElegidos;
            return new PistaElegida(p.Indice, idioma, true, motivo);
        }).ToList();

        // El paracaídas: antes un vídeo mudo, todas. Pasa con un fichero cuyas pistas no traen
        // etiqueta de idioma, o con una biblioteca en un idioma que no está en la lista.
        if (!plan.Any(p => p.SeQueda))
            plan = [.. plan.Select(p => p with { SeQueda = true, Motivo = PorQuePista.NingunaCasaba })];

        // Y el orden: el preferido primero. Es lo que se lleva el «-disposition default», o sea
        // la pista que suena al abrir el vídeo. OrderBy es estable, así que el resto se queda
        // como venía en el fichero.
        return [.. plan.OrderBy(p => p.Idioma.Equals(pref, StringComparison.OrdinalIgnoreCase) ? 0 : 1)];
    }

    /// <summary>
    /// Cuántas se conservan, que es lo único que necesita el estimador.
    ///
    /// <para>
    /// Existe para que el pronóstico no tenga que reimplementar la regla — que es exactamente lo
    /// que hacía, y decía otra cosa que el motor.
    /// </para>
    /// </summary>
    public static int Cuantas(IReadOnlyList<string?> idiomas, string? preferido, IReadOnlyList<string>? elegidos) =>
        Para([.. idiomas.Select((l, i) => (i, l))], preferido, elegidos).Count(p => p.SeQueda);
}
