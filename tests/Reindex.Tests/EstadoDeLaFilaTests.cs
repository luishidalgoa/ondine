using System.ComponentModel;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El estado de una fila de la lista de comprimir, leído como tres síes o noes.
///
/// <para>
/// La fila ya decía su color con <c>EstadoBrush</c>, que devuelve el nombre de un tono
/// —«Ok», «Err», «Live», «Muted»—. Eso le servía a WPF, donde la plantilla comparaba ese
/// texto con un <c>DataTrigger</c>. Avalonia no tiene disparadores por dato: la forma de
/// pintar según el estado es poner una CLASE, y una clase se ata a un booleano.
/// </para>
/// <para>
/// <b>Y aquí está el motivo de estas pruebas.</b> Al portar la ventana enlacé tres clases a
/// tres propiedades que no existían. Avalonia no se queja de un enlace roto: la clase no se
/// pone, el texto sale del color de siempre y <b>no pasa nada visible</b> — el estado deja de
/// distinguirse y nadie se enteraría hasta mirar una compresión fallida esperando ver rojo.
/// </para>
/// </summary>
public static class EstadoDeLaFilaTests
{
    public static void Todas()
    {
        Program.Seccion("El estado de una fila, en síes y noes");

        CadaEstadoEnciendeSuClaseYSoloSuya();
        LoPendienteNoEnciendeNinguna();
        AlCambiarElEstadoSeAvisaDeLasTres();
    }

    private static VideoRow Con(string estado) => new() { Name = "v.mkv", Estado = estado };

    /// <summary>
    /// Los tres estados con color, y que sea exclusivo: dos clases a la vez sobre el mismo
    /// texto dejan el color a suerte del orden en que se apliquen.
    /// </summary>
    private static void CadaEstadoEnciendeSuClaseYSoloSuya()
    {
        var terminado = Con("−214 MB");
        Program.Assert(terminado.EstadoEsOk, "terminado con ahorro enciende el verde");
        Program.Assert(!terminado.EstadoEsError && !terminado.EstadoEsEnMarcha, "y solo ese");

        var fallo = Con("Error: ffmpeg se fue");
        Program.Assert(fallo.EstadoEsError, "un error enciende el rojo");
        Program.Assert(!fallo.EstadoEsOk && !fallo.EstadoEsEnMarcha, "y solo ese");

        var vivo = Con("Comprimiendo… 42 %");
        Program.Assert(vivo.EstadoEsEnMarcha, "lo que está en marcha enciende el acento");
        Program.Assert(!vivo.EstadoEsOk && !vivo.EstadoEsError, "y solo ese");

        var pausa = Con("En pausa");
        Program.Assert(pausa.EstadoEsEnMarcha,
            "en pausa cuenta como en marcha: el trabajo sigue vivo, solo está detenido");
    }

    /// <summary>
    /// Lo pendiente y lo saltado se quedan con el color apagado de la columna. Que las tres
    /// digan no es la respuesta correcta, no un hueco: no todo estado tiene que gritar.
    /// </summary>
    private static void LoPendienteNoEnciendeNinguna()
    {
        foreach (var e in new[] { "…", "Saltado", "Ya comprimido" })
        {
            var f = Con(e);
            Program.Assert(!f.EstadoEsOk && !f.EstadoEsError && !f.EstadoEsEnMarcha,
                $"«{e}» no enciende ninguna: se queda con el color apagado");
        }
    }

    /// <summary>
    /// El aviso al cambiar de estado tiene que nombrar a las tres.
    ///
    /// <para>
    /// Esto es lo que se rompe sin hacer ruido. Una fila que cambia de «Comprimiendo» a
    /// «Error» y solo avisa de <c>EstadoBrush</c> se queda con la clase anterior puesta: la
    /// tabla enseña el rojo del texto y el color del acento a la vez, o ninguno. La lista
    /// arranca vacía y se llena mientras se trabaja, así que <b>todas las filas pasan por
    /// aquí</b> — no es un caso raro, es el caso normal.
    /// </para>
    /// </summary>
    private static void AlCambiarElEstadoSeAvisaDeLasTres()
    {
        var fila = Con("…");
        var avisados = new List<string>();
        ((INotifyPropertyChanged)fila).PropertyChanged += (_, e) => avisados.Add(e.PropertyName ?? "");

        fila.Estado = "Error: se acabó el disco";

        foreach (var p in new[] { nameof(VideoRow.EstadoEsOk), nameof(VideoRow.EstadoEsError),
                                  nameof(VideoRow.EstadoEsEnMarcha) })
            Program.Assert(avisados.Contains(p), $"al cambiar de estado se avisa de {p}");
    }
}
