using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Trabajos;

namespace Ondine;

/// <summary>
/// Un trabajo de la cola, tal como se ve en el panel.
///
/// <para>
/// Es solo pintura: coge un <see cref="Trabajo"/> y lo deja en texto y color. La regla de
/// qué puede hacer cada trabajo vive en <see cref="ColaDeTrabajos"/> y tiene sus pruebas;
/// aquí no se decide nada.
/// </para>
/// </summary>
public sealed class ColaFila
{
    public required int Id { get; init; }
    public required string Estado { get; init; }
    public required Brush ColorEstado { get; init; }
    public required string Resumen { get; init; }
    public required string Destino { get; init; }

    public static ColaFila De(Trabajo t)
    {
        var T = Textos.Instancia;

        var (texto, color) = t.Estado switch
        {
            EstadoDelTrabajo.EnCurso => (T.MainColaEstadoEnCurso, "#3D6BB3"),
            EstadoDelTrabajo.Hecho => (T.MainColaEstadoHecho, "#2E7D32"),
            EstadoDelTrabajo.Fallido => (T.MainColaEstadoFallido, "#B23A3A"),
            EstadoDelTrabajo.Cancelado => (T.MainColaEstadoCancelado, "#5A5A62"),
            EstadoDelTrabajo.AMedias =>
                (string.Format(T.MainColaEstadoAMedias, t.Salieron, t.Fallaron), "#B26A00"),
            _ => (T.MainColaEstadoPendiente, "#4A4A55"),
        };

        // El códec y la calidad a la vista: son lo que distingue un trabajo de otro, y
        // toda la cola existe justamente para poder tenerlos distintos.
        var plantilla = t.Ficheros.Count == 1 ? T.MainColaTrabajoUno : T.MainColaTrabajo;
        var calidad = t.Opciones.Quality == 0 ? "auto" : t.Opciones.Quality.ToString();

        return new ColaFila
        {
            Id = t.Id,
            Estado = texto,
            ColorEstado = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            Resumen = string.Format(plantilla, t.Ficheros.Count, t.Opciones.VideoCodec, calidad),
            Destino = t.Destino,
        };
    }
}
