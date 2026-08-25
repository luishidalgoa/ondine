using Ondine.Trabajos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El rótulo de un trabajo de la cola: el texto del estado, su color y el resumen.
///
/// <para>
/// Esto vivía en <c>ColaFila</c>, dentro de la app de WPF, mezclado con el
/// <c>SolidColorBrush</c> que necesitaba su plantilla. Al portar la ventana a Avalonia había
/// que elegir entre copiarlo -dos sitios donde cambiar un color y uno que se olvida- o
/// sacarlo. Sale, y de paso se puede probar: <b>decidir qué pone y de qué color no necesita
/// saber pintar</b>.
/// </para>
/// <para>
/// Lo que cada interfaz sigue haciendo por su cuenta es convertir el color en su propio tipo
/// de pincel, que es lo único que de verdad cambia entre las dos.
/// </para>
/// </summary>
public static class RotuloDeColaTests
{
    public static void Todas()
    {
        Program.Seccion("El rótulo de un trabajo de la cola");

        CadaEstadoDiceAlgoYTieneColor();
        AMediasCuentaLosDosLados();
        ElResumenLlevaElCodecYLaCalidad();
    }

    private static Trabajo Uno(EstadoDelTrabajo estado, int cuantos = 1,
                               string codec = "hevc", int calidad = 24,
                               int salieron = 0, int fallaron = 0)
    {
        var t = new Trabajo
        {
            Id = 7,
            Ficheros = Enumerable.Range(0, cuantos).Select(i => $"v{i}.mkv").ToList(),
            Opciones = new EncodeOptions { VideoCodec = codec, Quality = calidad },
            Destino = @"D:\salida",
        };
        t.Estado = estado;
        t.Salieron = salieron;
        t.Fallaron = fallaron;
        return t;
    }

    /// <summary>
    /// Ningún estado se queda sin rótulo. Es la razón de sacarlo: un estado nuevo en la cola
    /// -AMedias lo fue- se añade en el motor y aquí se cae solo al «pendiente» sin avisar,
    /// que en la pantalla se ve como un trabajo que nunca arranca.
    /// </summary>
    private static void CadaEstadoDiceAlgoYTieneColor()
    {
        foreach (var e in Enum.GetValues<EstadoDelTrabajo>())
        {
            var r = RotuloDeCola.De(Uno(e));
            Program.Assert(!string.IsNullOrWhiteSpace(r.Estado), $"«{e}» tiene texto");
            Program.Assert(r.Color.StartsWith("#") && r.Color.Length == 7,
                $"y un color en hexadecimal ({e} → {r.Color})");
        }

        var colores = Enum.GetValues<EstadoDelTrabajo>()
            .Select(e => RotuloDeCola.De(Uno(e)).Color).Distinct().Count();
        Program.Assert(colores >= 5,
            $"y los estados no comparten color: {colores} distintos para 6 estados");
    }

    /// <summary>
    /// «A medias» es el único estado cuyo rótulo lleva números, y son los dos: decir solo
    /// cuántos salieron deja al usuario sin saber si falta algo o si falló.
    /// </summary>
    private static void AMediasCuentaLosDosLados()
    {
        var r = RotuloDeCola.De(Uno(EstadoDelTrabajo.AMedias, cuantos: 5, salieron: 3, fallaron: 2));
        Program.Assert(r.Estado.Contains("3"), "«a medias» dice cuántos salieron");
        Program.Assert(r.Estado.Contains("2"), "y cuántos no");
    }

    /// <summary>
    /// El códec y la calidad a la vista: son lo que distingue un trabajo de otro, y la cola
    /// existe justamente para poder tenerlos distintos. Sin ellos, dos trabajos con los
    /// mismos ficheros se leen igual.
    /// </summary>
    private static void ElResumenLlevaElCodecYLaCalidad()
    {
        var r = RotuloDeCola.De(Uno(EstadoDelTrabajo.Pendiente, cuantos: 4, codec: "av1", calidad: 30));
        Program.Assert(r.Resumen.Contains("av1"), "el resumen lleva el códec");
        Program.Assert(r.Resumen.Contains("30"), "y la calidad");
        Program.Assert(r.Resumen.Contains("4"), "y cuántos ficheros son");

        var auto = RotuloDeCola.De(Uno(EstadoDelTrabajo.Pendiente, calidad: 0));
        Program.Assert(auto.Resumen.Contains("auto"),
            "calidad 0 se lee «auto» y no «0»: cero no es una calidad, es «decídelo tú»");
    }
}
