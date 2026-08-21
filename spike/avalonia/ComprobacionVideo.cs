using LibVLCSharp.Shared;

namespace Ondine.Spike;

/// <summary>
/// La segunda pregunta del estudio: ¿LibVLC sirve de reproductor?
///
/// <para>
/// Se mide lo que de verdad importaba, que no es «se ve algo». Son dos cosas:
/// </para>
/// <list type="number">
///   <item><b>Que la posición que pides sea la que sale.</b> En la app de hoy un clic en
///   la línea de tiempo se iba diez segundos atrás, así que aquí se pide una posición
///   exacta y se compara con la que queda.</item>
///   <item><b>Que AV1 reproduzca.</b> Es la deuda que el estudio dice que esta migración
///   paga: <c>MediaElement</c> va por DirectShow y con AV1 no puede, por mucha extensión
///   que se instale. Si LibVLC tampoco pudiera, ese argumento se cae.</item>
/// </list>
/// </summary>
public static class ComprobacionVideo
{
    public static readonly List<string> Resultados = [];

    private static void Dice(bool bien, string que) =>
        Resultados.Add($"{(bien ? "✓" : "✗")} {que}");

    /// <summary>
    /// Espera a que el reproductor sepa cuánto dura. VLC abre el medio en su propio hilo,
    /// así que preguntar de inmediato devuelve 0 y no es que haya fallado.
    /// </summary>
    private static async Task<bool> EsperarDuracion(MediaPlayer mp, int msTope = 8000)
    {
        for (var t = 0; t < msTope; t += 100)
        {
            if (mp.Length > 0) return true;
            await Task.Delay(100);
        }
        return false;
    }

    public static async Task Correr(LibVLC vlc, MediaPlayer mp, string h264, string av1)
    {
        // ── 1. Reproduce y sabe cuánto dura ──────────────────────────────────────
        mp.Play(new Media(vlc, new Uri(h264)));

        if (!await EsperarDuracion(mp))
        {
            Dice(false, "el H.264 ni siquiera abrió: sin esto no hay nada que medir");
            return;
        }

        Dice(true, $"H.264 abre y reporta duración: {mp.Length / 1000.0:0.00}s");

        // Un momento de reproducción real antes de tocar nada.
        await Task.Delay(700);
        Dice(mp.IsPlaying, "y está reproduciendo de verdad, no solo cargado");

        // ── 2. LA MEDIDA: pedir una posición y ver dónde cae ─────────────────────
        var desvios = new List<double>();
        foreach (var fraccion in new[] { 0.10, 0.50, 0.90, 0.25 })
        {
            var pedido = (long)(mp.Length * fraccion);
            mp.Time = pedido;

            // VLC busca al fotograma clave más cercano y avisa después; se le da margen.
            await Task.Delay(900);

            var real = mp.Time;
            var desvio = (real - pedido) / 1000.0;
            desvios.Add(Math.Abs(desvio));

            Resultados.Add($"    · pedido {pedido / 1000.0,6:0.000}s → real {real / 1000.0,6:0.000}s " +
                           $"· desvío {desvio,+7:+0.000;-0.000;0.000}s");
        }

        // El listón: dos segundos. No es exigente por capricho — es que el fallo que se
        // vio en WPF eran DIEZ segundos, y con un fichero de fotograma clave cada dos
        // segundos ese es el error máximo honesto de una búsqueda que no reconstruye.
        var peor = desvios.Max();
        Dice(peor <= 2.0,
            $"la búsqueda cae donde se pide: peor desvío {peor:0.000}s (listón 2,000s)");

        // ── 3. AV1, que es la deuda que esto viene a pagar ───────────────────────
        mp.Stop();
        mp.Play(new Media(vlc, new Uri(av1)));

        if (!await EsperarDuracion(mp))
        {
            Dice(false, "AV1 NO abre: se cae el argumento de que la migración arregla el códec");
            return;
        }

        await Task.Delay(700);
        Dice(mp.IsPlaying,
            $"AV1 reproduce ({mp.Length / 1000.0:0.00}s) — lo que MediaElement no puede hacer por DirectShow");

        mp.Stop();
    }
}
