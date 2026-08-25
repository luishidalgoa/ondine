namespace Ondine.Reindex.Tests;

/// <summary>
/// Traducir un fallo de ffmpeg a algo que se pueda leer.
///
/// <para>
/// <b>Esto sale de un caso real.</b> Un usuario intentó comprimir doce capítulos en Linux y los
/// doce fallaron con la misma línea, la única que había:
/// </para>
/// <code>    ERROR al codificar (código 243)</code>
/// <para>
/// Y nada más. El motor <b>sí capturaba</b> la salida de error de ffmpeg —la usa para detectar
/// «disco lleno»— y en cualquier otro fallo la tiraba, así que quedaba un número sin causa. Para
/// colmo, la caja del registro no dejaba desplazarse en esa versión, así que no había forma de
/// sacar más.
/// </para>
/// <para>
/// <b>Y 243 significa algo.</b> ffmpeg devuelve códigos AVERROR negativos, y el sistema los
/// trunca a un byte sin signo: <c>-13</c> se convierte en <c>243</c>. El 13 es
/// <c>EACCES</c> — permiso denegado. O sea que el número que no decía nada era «no puedo
/// escribir ahí», que es de las pocas cosas que el usuario puede arreglar en un minuto.
/// </para>
/// </summary>
public static class MotivoDeFfmpegTests
{
    public static void Todas()
    {
        Program.Seccion("El motivo de un fallo de ffmpeg");

        ElNumeroNegativoSeTraduce();
        LaUltimaLineaDeErrorEsLaQueImporta();
        SinNadaQueDecirNoSeInventa();
    }

    /// <summary>
    /// Los errno que salen de verdad al comprimir. Son los tres que un usuario puede arreglar
    /// solo, y por eso merecen nombre en vez de número.
    /// </summary>
    private static void ElNumeroNegativoSeTraduce()
    {
        var permiso = MotivoDeFfmpeg.De(243, "");
        Program.Assert(permiso.Contains("permiso", StringComparison.OrdinalIgnoreCase)
                    || permiso.Contains("permission", StringComparison.OrdinalIgnoreCase),
            $"243 se lee como permiso denegado, que es lo que es ({permiso})");

        var espacio = MotivoDeFfmpeg.De(228, "");   // -28 = ENOSPC
        Program.Assert(espacio.Contains("espacio", StringComparison.OrdinalIgnoreCase)
                    || espacio.Contains("space", StringComparison.OrdinalIgnoreCase),
            $"228 se lee como disco lleno ({espacio})");

        var noExiste = MotivoDeFfmpeg.De(254, "");  // -2 = ENOENT
        Program.Assert(noExiste.Length > 0, $"254 dice algo, no un número suelto ({noExiste})");

        // Un código normal no se disfraza de errno: ffmpeg devuelve 1 para «la conversión
        // falló» y eso no es un errno negativo.
        var uno = MotivoDeFfmpeg.De(1, "");
        Program.Assert(!uno.Contains("permiso", StringComparison.OrdinalIgnoreCase),
            $"el código 1 no se traduce como si fuera un errno ({uno})");
    }

    /// <summary>
    /// Cuando ffmpeg dice algo, eso manda. Sus últimas líneas son donde está el motivo real; lo
    /// de arriba es la descripción del fichero, que ocupa pantallas.
    /// </summary>
    private static void LaUltimaLineaDeErrorEsLaQueImporta()
    {
        var salida = string.Join('\n',
            "ffmpeg version 6.1.1 Copyright (c) 2000-2023 the FFmpeg developers",
            "  libavutil      58. 29.100 / 58. 29.100",
            "Input #0, matroska,webm, from 'cap01.mkv':",
            "  Duration: 00:23:15.02, start: 0.000000, bitrate: 4102 kb/s",
            "[out#0/matroska @ 0x55d1] Could not open file : Permission denied",
            "Conversion failed!");

        var m = MotivoDeFfmpeg.De(243, salida);
        Program.Assert(m.Contains("Permission denied"),
            $"se enseña la línea de ffmpeg que dice el motivo ({m})");
        Program.Assert(!m.Contains("libavutil"),
            "y no la cabecera de versiones, que ocupa y no dice nada");
    }

    /// <summary>
    /// Y si ffmpeg no escribió nada útil, se dice el código y se acabó. Inventar una causa es
    /// peor que no dar ninguna: manda a arreglar lo que no está roto.
    /// </summary>
    private static void SinNadaQueDecirNoSeInventa()
    {
        var m = MotivoDeFfmpeg.De(7, "   \n  \n");
        Program.Assert(m.Contains('7'), $"sin salida útil, al menos se dice el código ({m})");
    }
}
