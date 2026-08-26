namespace Ondine.Reindex.Tests;

/// <summary>
/// Elegir el codificador por su nombre, y no solo «con GPU o sin ella».
///
/// <para>
/// <b>De dónde sale.</b> Un usuario midió su máquina antes de comprimir una biblioteca grande:
/// un episodio real, 120 segundos, calidad medida con SSIM contra el original. NVENC en una GTX
/// 1050 Ti (Pascal) a CQ 19 le dejó el fichero al <b>126 % del original</b> —más grande que la
/// entrada— y en su mejor caso utilizable no bajaba del 74 %. libx265 en CRF 20, con una
/// diferencia de SSIM de 0,002, se quedaba en el <b>19 %</b>. Cuatro veces más eficiente, por una
/// diferencia de calidad que no se ve.
/// </para>
/// <para>
/// Y Ondine prefería NVENC, porque su selección era «primero los de hardware, y el primero que
/// arranque gana». Para llegar a x265 había que apagar la aceleración entera desde Preferencias:
/// un interruptor global, con nombre equivocado para lo que se quería hacer, que además apagaba
/// el hardware para todo lo demás.
/// </para>
/// <para>
/// <b>Lo que se pide por su nombre manda.</b> Sobre la lista de preferencia y sobre el
/// interruptor: quien escribe «libx265» sabe lo que quiere. Y lo que no funciona en esa máquina
/// no se usa a la callada — se cae a la elección automática y se dice.
/// </para>
/// </summary>
public static class ElCodificadorElegidoTests
{
    public static void Todas()
    {
        Program.Seccion("El codificador elegido");

        SinPedirNadaComoSiempre();
        PorSuNombre();
        SoftwareSinTenerQueSaberElNombre();
        LoPedidoMandaSobreElInterruptor();
        LoQueNoEstaSeDiceYSeCaeALoAutomatico();
        LaCacheDistingueLoPedido();
    }

    /// <summary>
    /// Un ffmpeg de mentira con un codificador de hardware y dos de software, que es la forma de
    /// probar la elección sin depender de la máquina donde corran las pruebas.
    /// </summary>
    private sealed class FfmpegDeMentira
    {
        public int Llamadas { get; private set; }

        public Task<(int, string, string)> Ejecutar(string exe, string[] args)
        {
            Llamadas++;
            if (args.Contains("-encoders"))
                return Task.FromResult((0, "V....D hevc_nvenc\nV....D libx265\nV....D libsvtav1\n", ""));
            return Task.FromResult((0, "", ""));   // la prueba en vivo, aprobada
        }
    }

    private static string Elegido(string codec, string? pedido, bool hardware = true)
    {
        var antes = Engine.AllowHardware;
        try
        {
            Engine.AllowHardware = hardware;
            return new Engine()
                .SelectEncoderAsync(codec, pedido, new FfmpegDeMentira().Ejecutar)
                .GetAwaiter().GetResult();
        }
        finally { Engine.AllowHardware = antes; }
    }

    private static void SinPedirNadaComoSiempre()
    {
        Program.Assert(Elegido("hevc", null) == "hevc_nvenc",
            $"sin pedir nada gana el de hardware, como hasta ahora ({Elegido("hevc", null)})");
        Program.Assert(Elegido("hevc", "") == "hevc_nvenc", "y una cadena vacía es «lo que decidas tú»");
    }

    private static void PorSuNombre()
    {
        Program.Assert(Elegido("hevc", "libx265") == "libx265",
            $"pedir libx265 da libx265, con el hardware encendido y todo ({Elegido("hevc", "libx265")})");
        Program.Assert(Elegido("av1", "libsvtav1") == "libsvtav1",
            "y pedir libsvtav1 para AV1, libsvtav1");
        Program.Assert(Elegido("hevc", "LIBX265") == "libx265",
            "sin distinguir mayúsculas: el nombre lo escribe una persona");
    }

    /// <summary>
    /// «software» a secas, sin tener que saberse los nombres. Es lo que quiere quien ha medido
    /// que su GPU no le sirve para archivar, y no tiene por qué recordar si el de AV1 se llama
    /// libsvtav1 o libaom-av1.
    /// </summary>
    private static void SoftwareSinTenerQueSaberElNombre()
    {
        Program.Assert(Elegido("hevc", "software") == "libx265",
            $"«software» en HEVC es x265 ({Elegido("hevc", "software")})");
        Program.Assert(Elegido("av1", "software") == "libsvtav1",
            $"y en AV1, el primero de los suyos que exista ({Elegido("av1", "software")})");
    }

    /// <summary>
    /// Un nombre explícito gana al interruptor de Preferencias, en los dos sentidos. Quien
    /// escribe el nombre de un codificador sabe lo que quiere; el interruptor es una preferencia
    /// general, y lo general no pisa lo concreto.
    /// </summary>
    private static void LoPedidoMandaSobreElInterruptor()
    {
        Program.Assert(Elegido("hevc", "hevc_nvenc", hardware: false) == "hevc_nvenc",
            "pedir NVENC por su nombre lo usa aunque la aceleración esté apagada");
        Program.Assert(Elegido("hevc", "libx265", hardware: true) == "libx265",
            "y pedir x265 lo usa aunque esté encendida");
        Program.Assert(Elegido("hevc", null, hardware: false) == "libx265",
            "sin pedir nada, el interruptor sigue mandando");
    }

    /// <summary>
    /// Lo que no está en esta build o no arranca en esta máquina no se usa a la callada: se cae a
    /// la elección automática. Un ajuste guardado que apunta a un codificador que no existe
    /// —copiado de otra máquina, o de una build de ffmpeg con menos cosas— no puede dejar la app
    /// sin comprimir.
    /// </summary>
    private static void LoQueNoEstaSeDiceYSeCaeALoAutomatico()
    {
        Program.Assert(Elegido("hevc", "hevc_qsv") == "hevc_nvenc",
            $"un QSV que esta build no trae se cae a lo automático ({Elegido("hevc", "hevc_qsv")})");
        Program.Assert(Elegido("hevc", "no_existe_este") == "hevc_nvenc",
            "y un nombre inventado, igual");

        // Y con la aceleración apagada, la caída va a software y no al hardware que se pidió.
        Program.Assert(Elegido("hevc", "hevc_qsv", hardware: false) == "libx265",
            "con la aceleración apagada, la caída respeta el interruptor");
    }

    /// <summary>
    /// La caché tiene que distinguir lo pedido, o el primer «libx265» de la sesión se quedaría
    /// pegado a todo lo demás. Es el mismo fallo que tenía con el interruptor de hardware.
    /// </summary>
    private static void LaCacheDistingueLoPedido()
    {
        var antes = Engine.AllowHardware;
        try
        {
            Engine.AllowHardware = true;
            var ffmpeg = new FfmpegDeMentira();
            var motor = new Engine();

            var primero = motor.SelectEncoderAsync("hevc", "libx265", ffmpeg.Ejecutar).GetAwaiter().GetResult();
            var segundo = motor.SelectEncoderAsync("hevc", null, ffmpeg.Ejecutar).GetAwaiter().GetResult();

            Program.Assert(primero == "libx265" && segundo == "hevc_nvenc",
                $"pedir uno no contamina la siguiente elección ({primero}, {segundo})");

            var repetido = motor.SelectEncoderAsync("hevc", "libx265", ffmpeg.Ejecutar).GetAwaiter().GetResult();
            var llamadas = ffmpeg.Llamadas;
            motor.SelectEncoderAsync("hevc", "libx265", ffmpeg.Ejecutar).GetAwaiter().GetResult();

            Program.Assert(repetido == "libx265" && ffmpeg.Llamadas == llamadas,
                "y repetir la misma petición no vuelve a arrancar ffmpeg");
        }
        finally { Engine.AllowHardware = antes; }
    }
}
