namespace Ondine.Reindex.Tests;

/// <summary>
/// Que apagar la aceleración por hardware surta efecto <b>en el momento</b>.
///
/// <para>
/// <b>El fallo.</b> El codificador elegido se guardaba en un diccionario indexado solo por el
/// códec, y esa caché se consultaba <i>antes</i> de mirar el interruptor. Como el motor vive toda
/// la sesión —es un campo de la ventana principal—, bastaba con haber comprimido o previsualizado
/// una vez para que desmarcar «usar aceleración por hardware» en Preferencias no hiciera nada
/// hasta reiniciar la app.
/// </para>
/// <para>
/// Que un interruptor parezca no hacer nada es el peor fallo que puede tener un interruptor: el
/// usuario no concluye «no se ha aplicado», concluye «esto no sirve». Y si lo estaba apagando
/// porque su GPU le sacaba artefactos, seguía viéndolos.
/// </para>
/// <para>
/// <b>Cómo se prueba sin ffmpeg.</b> Se le inyecta al motor un ffmpeg de mentira: uno que dice
/// tener <c>hevc_nvenc</c> y que aprueba cualquier prueba de codificación. Así la comprobación
/// vale igual en un portátil con NVIDIA que en el ejecutor de CI, donde no hay ni ffmpeg — y
/// sobre todo, ejercita el CAMINO DE VERDAD (<see cref="Engine.SelectEncoderAsync"/>) en vez de
/// preguntarle a un ayudante si la clave de la caché está bien formada, que habría pasado con el
/// fallo puesto.
/// </para>
/// </summary>
public static class ElInterruptorDeHardwareTests
{
    public static void Todas()
    {
        Program.Seccion("El interruptor de la aceleración por hardware");

        ApagarloSurteEfectoAlMomento();
        YEncenderloTambien();
        LaCacheSigueAhorrandoTrabajo();
    }

    /// <summary>
    /// Un ffmpeg de mentira: dice qué codificadores tiene y aprueba las pruebas de codificación.
    /// Cuenta las veces que lo llaman, que es como se ve si la caché sigue funcionando.
    /// </summary>
    private sealed class FfmpegDeMentira
    {
        public int Llamadas { get; private set; }

        public Task<(int, string, string)> Ejecutar(string exe, string[] args)
        {
            Llamadas++;

            // «-encoders»: la lista. Se ofrece uno de hardware y uno de software para que la
            // elección tenga de verdad dos caminos posibles.
            if (args.Contains("-encoders"))
                return Task.FromResult((0, "V....D hevc_nvenc\nV....D libx265\n", ""));

            // Cualquier otra cosa es la prueba en vivo del codificador: aprobada.
            return Task.FromResult((0, "", ""));
        }
    }

    private static void ApagarloSurteEfectoAlMomento()
    {
        var antes = Engine.AllowHardware;
        try
        {
            var ffmpeg = new FfmpegDeMentira();
            var motor = new Engine();

            Engine.AllowHardware = true;
            var conGpu = motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult();
            Program.Assert(conGpu == "hevc_nvenc", $"con la aceleración puesta se elige la GPU ({conGpu})");

            // El mismo motor, sin reiniciar nada: es exactamente lo que pasa al guardar
            // Preferencias con la ventana abierta.
            Engine.AllowHardware = false;
            var sinGpu = motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult();

            Program.Assert(sinGpu == "libx265",
                $"y al apagarla, el MISMO motor deja de usarla sin reiniciar la app ({sinGpu})");
        }
        finally { Engine.AllowHardware = antes; }
    }

    /// <summary>
    /// Y en el otro sentido, que es el que se olvida: volver a encenderla tiene que devolver la
    /// GPU. Un arreglo que solo mirase «si está apagado, recalcula» dejaría al usuario en
    /// software hasta reiniciar, o sea el mismo fallo del revés.
    /// </summary>
    private static void YEncenderloTambien()
    {
        var antes = Engine.AllowHardware;
        try
        {
            var ffmpeg = new FfmpegDeMentira();
            var motor = new Engine();

            Engine.AllowHardware = false;
            Program.Assert(motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult() == "libx265",
                "apagada, software");

            Engine.AllowHardware = true;
            var vuelta = motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult();
            Program.Assert(vuelta == "hevc_nvenc", $"y al volver a encenderla, la GPU otra vez ({vuelta})");
        }
        finally { Engine.AllowHardware = antes; }
    }

    /// <summary>
    /// La caché sigue haciendo su trabajo: elegir el codificador cuesta arrancar ffmpeg una vez
    /// por candidato, y eso no se puede pagar en cada fichero de una tanda de doce. Arreglar el
    /// interruptor a base de quitar la caché habría cambiado un fallo por otro.
    /// </summary>
    private static void LaCacheSigueAhorrandoTrabajo()
    {
        var antes = Engine.AllowHardware;
        try
        {
            var ffmpeg = new FfmpegDeMentira();
            var motor = new Engine();
            Engine.AllowHardware = true;

            motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult();
            var trasLaPrimera = ffmpeg.Llamadas;

            for (int i = 0; i < 5; i++) motor.SelectEncoderAsync("hevc", ffmpeg.Ejecutar).GetAwaiter().GetResult();

            Program.Assert(ffmpeg.Llamadas == trasLaPrimera,
                $"cinco veces más no arrancan ffmpeg ni una vez ({ffmpeg.Llamadas} frente a {trasLaPrimera})");
        }
        finally { Engine.AllowHardware = antes; }
    }
}
