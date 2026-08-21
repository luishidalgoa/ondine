namespace Ondine.Reindex.Tests;

/// <summary>
/// La cuenta del globo de la previa: a qué segundo apunta el ratón sobre la barra.
///
/// <para>
/// Esto ya falló una vez, y de la peor manera: el globo decía una hora y el clic llevaba a
/// otra. La causa es que <b>el recorrido útil de un deslizador no es todo su ancho</b> —el
/// tirador tiene que caber, así que empieza y acaba a medio tirador de los bordes—. Con la
/// regla de tres ingenua la diferencia llegaba a diez segundos en la primera mitad de un
/// capítulo de media hora, y se leía como «pincho y se va para atrás».
/// </para>
/// <para>
/// Vive aquí, y no en el arnés de WPF donde estaba, porque las dos interfaces tienen la
/// misma barra: así corre en CI sobre Linux como todo lo demás.
/// </para>
/// </summary>
public static class PosicionEnLaBarraTests
{
    public static void Todas()
    {
        Program.Seccion("El globo de la previa apunta donde lleva el clic");

        const double ancho = 500, pulgar = 18, dur = 1800;   // media hora

        // Los extremos, que son los que la regla de tres sí acierta.
        Program.Assert(SegundosDeX(pulgar / 2, ancho, pulgar, dur) == 0,
            "a medio tirador del borde izquierdo, el segundo cero");
        Program.Assert(Math.Abs(SegundosDeX(ancho - pulgar / 2, ancho, pulgar, dur) - dur) < 0.001,
            "y a medio tirador del derecho, el final");

        // ══ El caso que costó el fallo ═══════════════════════════════════════
        // En el primer cuarto de la barra. La cuenta ingenua va POR DELANTE de la buena,
        // y la buena es la que usa el control al colocarse: por eso el globo prometía un
        // segundo y el clic caía ANTES. Se leía como «pincho y se va para atrás».
        double x = ancho * 0.25;
        double bien = SegundosDeX(x, ancho, pulgar, dur);
        double aOjo = x / ancho * dur;
        Program.Assert(bien < aOjo,
            $"la cuenta ingenua promete más de lo que da el clic ({aOjo:0.0}s frente a {bien:0.0}s)");
        Program.Assert(aOjo - bien > 5,
            $"y la diferencia es de las que se notan: {aOjo - bien:0.0} segundos");

        // ══ Nada se sale del vídeo ═══════════════════════════════════════════
        // El ratón puede quedarse fuera de la barra mientras se arrastra, y un segundo
        // negativo o pasado del final se le pasa tal cual al reproductor.
        Program.Assert(SegundosDeX(-40, ancho, pulgar, dur) == 0, "una x negativa se queda en cero");
        Program.Assert(SegundosDeX(9999, ancho, pulgar, dur) == dur, "y una pasada del ancho, en el final");

        // ══ Casos degenerados que sí ocurren ═════════════════════════════════
        // Una barra de ancho cero pasa de verdad: se pregunta antes del primer dibujado.
        Program.Assert(SegundosDeX(0, 0, 0, dur) is >= 0 and <= dur, "una barra sin ancho todavía no revienta");
        Program.Assert(SegundosDeX(100, ancho, pulgar, 0) == 0, "y sin duración conocida, cero");

        // Sin tirador la cuenta ES la regla de tres: así queda dicho que la corrección
        // depende del tirador y no es un ajuste arbitrario.
        Program.Assert(Math.Abs(SegundosDeX(x, ancho, 0, dur) - aOjo) < 0.001,
            "sin tirador las dos cuentas coinciden: la corrección es exactamente su ancho");
    }

    private static double SegundosDeX(double x, double ancho, double pulgar, double maximo) =>
        PosicionEnLaBarra.SegundosDeX(x, ancho, pulgar, maximo);
}
