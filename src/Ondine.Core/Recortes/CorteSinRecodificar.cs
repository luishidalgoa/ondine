namespace Ondine.Recortes;

/// <summary>Dónde va a caer un corte de verdad, y cuánto se mueve respecto a lo pedido.</summary>
/// <param name="Pedido">El segundo que se pidió.</param>
/// <param name="Real">El segundo en el que caerá. Igual al pedido si no se sabe.</param>
/// <param name="SeSabe">
/// Si hubo índice de fotogramas clave con el que decidirlo. Sin él no se promete nada:
/// devolver «cae donde pediste» sería mentir, y es la mentira que hace desconfiar.
/// </param>
public readonly record struct DondeCaeElCorte(double Pedido, double Real, bool SeSabe)
{
    public double Desfase => Pedido - Real;

    /// <summary>Si el corte se mueve de donde lo pusiste. Con menos de un milisegundo, no.</summary>
    public bool SeMueve => SeSabe && Desfase > 0.001;
}

/// <summary>
/// Cortar copiando los paquetes, sin volver a codificar.
///
/// <para>
/// La diferencia con recodificar no es de velocidad: es de naturaleza. Copiando no se
/// pierde <b>ni un ápice</b> de calidad y tarda un suspiro, pero el corte <b>solo puede
/// caer en un fotograma clave</b>. En un fichero con uno cada dos segundos, pedir el corte
/// en el 10,4 lo pone en el 10,0, y no hay ajuste que lo afine.
/// </para>
/// <para>
/// Ese es el trato, y hay que enseñarlo antes de cortar. Un corte que se mueve medio
/// segundo sin avisar es lo que hace que se desconfíe de la herramienta — y quien elige
/// esto normalmente lo elige a sabiendas, como en LosslessCut.
/// </para>
/// </summary>
public static class CorteSinRecodificar
{
    /// <summary>
    /// Dónde cae el corte, dado el índice de fotogramas clave del fichero (en segundos y
    /// ordenado).
    ///
    /// <para>
    /// <b>Siempre retrocede</b>, nunca adelanta. Adelantando se perdería contenido que
    /// pediste y no se vería venir; retrocediendo sobra un poco al principio, que se nota
    /// en cuanto lo miras y no se pierde nada.
    /// </para>
    /// </summary>
    public static DondeCaeElCorte DondeCae(IReadOnlyList<double> clavesSeg, double pedido)
    {
        if (clavesSeg.Count == 0) return new(pedido, pedido, SeSabe: false);

        // Sin holgura a proposito. Poner una -«por si el flotante»- fue el primer intento y
        // lo tumbo su prueba: con margen de un milisegundo, pedir el 11,999 saltaba al 12 y
        // el corte se ADELANTABA, que es lo unico que esta regla no puede hacer. Comparar a
        // secas ya deja el caso exacto donde toca, porque un fotograma clave nunca es mayor
        // que si mismo.
        var real = clavesSeg[0];
        foreach (var c in clavesSeg)
        {
            if (c > pedido) break;
            real = c;
        }

        return new(pedido, real, SeSabe: true);
    }

    /// <summary>
    /// Si se puede copiar de una caja a otra. La regla honesta es <b>no cambiar de caja</b>.
    ///
    /// <para>
    /// Copiar significa meter los mismos paquetes en otro contenedor, y no todo cabe en
    /// todo: WebM solo admite dos códecs, MP4 no sabe guardar subtítulos de imagen. Antes
    /// que adivinar combinación por combinación —y equivocarse en la rara—, se exige el
    /// mismo formato y se dice por qué.
    /// </para>
    /// </summary>
    public static bool SePuedeCopiar(string extensionOrigen, string extensionDestino) =>
        Normalizar(extensionOrigen) == Normalizar(extensionDestino);

    private static string Normalizar(string ext) =>
        ext.TrimStart('.').Trim().ToLowerInvariant();

    /// <summary>
    /// Lo que se le pasa a ffmpeg. El salto va ANTES del «-i» para que busque por índice en
    /// vez de decodificar todo lo anterior para tirarlo.
    /// </summary>
    public static IReadOnlyList<string> Argumentos(
        string origen, string destino, double desde, double duracion)
    {
        var (antes, despues) = Reindex.Tramos.ArgsFfmpeg(desde, duracion);

        var args = new List<string> { "-hide_banner", "-loglevel", "error" };
        args.AddRange(antes);
        args.Add("-i"); args.Add(origen);
        args.AddRange(despues);

        // TODAS las pistas: los packs traen varios idiomas de audio y subtítulos, y quien
        // corta sin recodificar es justo quien no quiere perder nada.
        //
        // Y NADA de «-avoid_negative_ts make_zero», aunque sea lo que se lee por ahí. Costó
        // una tarde en Organizar: con «-c copy» el corte arranca en el fotograma clave
        // ANTERIOR, y esos fotogramas de más quedan con marca de tiempo negativa, así que el
        // reproductor los descarta y el trozo empieza donde toca. Con ese parámetro se
        // desplazan a cero y pasan a verse — medido sobre un episodio real, el trozo
        // arrancaba 5 segundos antes y enseñaba el final de la historia anterior.
        args.AddRange(["-map", "0", "-c", "copy", destino, "-y"]);
        return args;
    }
}
