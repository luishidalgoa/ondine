using Ondine.Localizacion;

namespace Ondine.Trabajos;

/// <summary>
/// Cómo se lee un trabajo de la cola: el texto de su estado, el color con el que se refuerza
/// y el resumen de lo que va a hacer.
///
/// <para>
/// Es solo lectura: coge un <see cref="Trabajo"/> y lo deja en texto. La regla de qué puede
/// hacer cada trabajo vive en <see cref="ColaDeTrabajos"/> y tiene sus pruebas; aquí no se
/// decide nada del trabajo, solo cómo se cuenta.
/// </para>
/// <para>
/// <b>El color va en hexadecimal y no en pincel.</b> Esto vivía dentro de la app de WPF, con
/// su <c>SolidColorBrush</c> pegado; al haber dos interfaces habría dos copias de la tabla de
/// estados, y la que se quedara atrás no daría ningún error — enseñaría un trabajo «pendiente»
/// que en realidad falló. Cada interfaz convierte el color a su tipo de pincel, que es lo
/// único que de verdad cambia entre ellas.
/// </para>
/// </summary>
public readonly record struct RotuloDeCola(string Estado, string Color, string Resumen)
{
    public static RotuloDeCola De(Trabajo t)
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

        // El códec y la calidad a la vista: son lo que distingue un trabajo de otro, y toda la
        // cola existe justamente para poder tenerlos distintos.
        var plantilla = t.Ficheros.Count == 1 ? T.MainColaTrabajoUno : T.MainColaTrabajo;
        var calidad = t.Opciones.Quality == 0 ? "auto" : t.Opciones.Quality.ToString();

        return new RotuloDeCola(texto, color,
            string.Format(plantilla, t.Ficheros.Count, t.Opciones.VideoCodec, calidad));
    }
}
