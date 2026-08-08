using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La vara del reloj, aprendida POR TEMPORADA.
///
/// <para>
/// Medido en la biblioteca real de Doraemon (1979): una historia dura ~6:12 en
/// 1979-1981, ~12:12 en 1985-1986 y <b>~23:35 en 1991-1993</b>. Con una sola
/// mediana global —8:36— la app medía 1986 con la vara de 1979 y marcaba como
/// sospechosos 67 ficheros que eran perfectamente normales para su año.
/// </para>
/// </summary>
public static class VaraPorTemporadaTests
{
    private static (TimeSpan, int, int?) O(int minutos, int segundos, int historias, int? temporada)
        => (new TimeSpan(0, minutos, segundos), historias, temporada);

    public static void Todas()
    {
        Program.Seccion("La vara del reloj, por temporada");

        // Dos formatos en la misma serie, con muestras de sobra en cada uno.
        var obs = new List<(TimeSpan, int, int?)>();
        for (int i = 0; i < 8; i++) obs.Add(O(6, 10, 1, 1979));
        for (int i = 0; i < 8; i++) obs.Add(O(12, 12, 1, 1986));

        var varas = MedidaDelCapitulo.UnidadPorTemporada(obs);

        Program.Assert(varas.Global is not null, "hay vara global");
        Program.Eq(new TimeSpan(0, 6, 10), varas.De(1979)?.Unidad, "1979 se mide con su propia vara");
        Program.Eq(new TimeSpan(0, 12, 12), varas.De(1986)?.Unidad, "y 1986 con la suya");

        // ESTE es el caso que fallaba: 13:23 en 1986 es normal, y con la vara
        // global de la serie entera salía como «esto son dos historias».
        var trece = new TimeSpan(0, 13, 23);
        Program.Assert(MedidaDelCapitulo.Cuadra(trece, 1, varas.De(1986)),
            "13:23 cuadra como UNA historia de 1986");
        Program.Assert(!MedidaDelCapitulo.Cuadra(trece, 1, varas.Global),
            "y con la vara global de la serie NO cuadraba: ese era el fallo");

        // Una temporada con pocas muestras no inventa su propia vara: una mediana
        // sacada de dos ficheros la mueve cualquier rareza, y entonces el aviso
        // saltaría justo en los buenos.
        var pocas = new List<(TimeSpan, int, int?)>(obs);
        pocas.Add(O(25, 0, 1, 1991));
        pocas.Add(O(24, 0, 1, 1991));
        var v2 = MedidaDelCapitulo.UnidadPorTemporada(pocas);
        Program.Eq(v2.Global?.Unidad, v2.De(1991)?.Unidad,
            "con menos muestras de las que hacen falta, se cae a la vara global");

        // Sin temporada tampoco se inventa nada.
        Program.Eq(v2.Global?.Unidad, v2.De(null)?.Unidad, "y sin temporada, la global");

        // Y si no hay ni para la global, no se opina de nada: no saber no es
        // sospechar.
        var casiNada = new List<(TimeSpan, int, int?)> { O(6, 10, 1, 1979), O(6, 12, 1, 1979) };
        var v3 = MedidaDelCapitulo.UnidadPorTemporada(casiNada);
        Program.Eq(null, v3.Global?.Unidad, "sin muestras suficientes no hay vara");
        Program.Eq(null, v3.De(1979)?.Unidad, "ni por temporada");
        Program.Assert(MedidaDelCapitulo.Cuadra(trece, 1, v3.De(1979)),
            "y sin vara, todo cuadra: callarse es la respuesta correcta");

        // La vara global sigue siendo la de siempre, para que nada de lo que ya
        // funcionaba cambie de comportamiento.
        Program.Eq(MedidaDelCapitulo.Aprender(obs.Select(o => (o.Item1, o.Item2)))?.Unidad,
            varas.Global?.Unidad, "la global es exactamente la de antes");
    }
}
