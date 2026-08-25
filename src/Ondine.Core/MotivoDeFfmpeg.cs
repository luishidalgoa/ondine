namespace Ondine;

/// <summary>
/// Traduce un fallo de ffmpeg a algo que se pueda leer y arreglar.
///
/// <para>
/// <b>Esto sale de un caso real.</b> Doce capítulos, doce fallos, y una sola línea de registro
/// para los doce: «ERROR al codificar (código 243)». El motor capturaba la salida de error de
/// ffmpeg —la usa para detectar «disco lleno»— y en cualquier otro fallo la tiraba, así que
/// quedaba un número sin causa. Y la caja del registro tampoco dejaba desplazarse, así que no
/// había manera de sacar más.
/// </para>
/// <para>
/// <b>Y 243 significa algo.</b> ffmpeg devuelve códigos AVERROR negativos, y el sistema los
/// trunca a un byte sin signo: <c>-13</c> sale como <c>243</c>. El 13 es <c>EACCES</c>, permiso
/// denegado — o sea que el número que no decía nada era «no puedo escribir ahí», que es de las
/// pocas cosas que se arreglan en un minuto si alguien te lo dice.
/// </para>
/// </summary>
public static class MotivoDeFfmpeg
{
    /// <summary>
    /// Los errno que aparecen de verdad al comprimir. No se ponen los cincuenta: los que un
    /// usuario puede arreglar solo, y el resto se queda en su número, que al menos es honesto.
    /// </summary>
    private static string? PorErrno(int errno) => errno switch
    {
        13 => Localizacion.Textos.Instancia.MotorFfmpegSinPermiso,
        28 => Localizacion.Textos.Instancia.MotorFfmpegSinEspacio,
        2 => Localizacion.Textos.Instancia.MotorFfmpegNoEncontrado,
        _ => null,
    };

    /// <summary>
    /// El motivo, a partir del código de salida y de lo que ffmpeg haya escrito.
    ///
    /// <para>
    /// Manda lo que diga ffmpeg: sus últimas líneas son donde está el motivo, y lo de arriba es
    /// la descripción del fichero, que ocupa pantallas y no dice nada. Si además el código es un
    /// errno disfrazado, se nombra: son dos pistas y no se descarta ninguna.
    /// </para>
    /// </summary>
    public static string De(int codigo, string salidaDeError)
    {
        var partes = new List<string>();

        // Un codigo entre 129 y 255 puede ser un AVERROR negativo truncado a un byte.
        // ffmpeg devuelve 1 para «la conversión falló» y eso NO es un errno, así que solo se
        // interpretan los que caen en ese rango.
        if (codigo is > 128 and < 256 && PorErrno(256 - codigo) is { } porQue)
            partes.Add(porQue);

        var ultimas = UltimasLineasUtiles(salidaDeError, 3);
        if (ultimas.Count > 0) partes.AddRange(ultimas);

        if (partes.Count == 0)
            return string.Format(Localizacion.Textos.Instancia.MotorFfmpegSoloElCodigo, codigo);

        return string.Join(" · ", partes);
    }

    /// <summary>
    /// Las últimas líneas con contenido, saltándose el ruido que ffmpeg escribe siempre.
    ///
    /// <para>
    /// Se descartan la cabecera de versiones y las líneas de configuración: son treinta líneas
    /// fijas que no cambian con el error y que, puestas en el registro, esconden lo que sí
    /// importa.
    /// </para>
    /// </summary>
    private static List<string> UltimasLineasUtiles(string salida, int cuantas)
    {
        if (string.IsNullOrWhiteSpace(salida)) return [];

        var utiles = salida
            .Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Where(l => !l.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            .Where(l => !l.StartsWith("built with", StringComparison.OrdinalIgnoreCase))
            .Where(l => !l.StartsWith("configuration:", StringComparison.OrdinalIgnoreCase))
            .Where(l => !l.StartsWith("lib", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // «Conversion failed!» es la última línea de cualquier fallo y no dice nada por sí
        // sola: la de antes es la que tiene el motivo.
        return utiles.TakeLast(cuantas).ToList();
    }
}
