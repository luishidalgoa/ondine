using Avalonia.Media;
using Ondine.Trabajos;

namespace Ondine.Ava;

/// <summary>
/// Un trabajo de la cola, tal como se ve en el panel.
///
/// <para>
/// La gemela de la de WPF, y a propósito tan corta: lo que dice y de qué color lo decide
/// <see cref="RotuloDeCola"/>, en el motor. Lo único que cambia entre las dos interfaces es
/// el tipo de pincel — de ahí que la tabla de estados no esté aquí.
/// </para>
/// </summary>
public sealed class ColaFila
{
    public required int Id { get; init; }
    public required string Estado { get; init; }
    public required IBrush ColorEstado { get; init; }
    public required string Resumen { get; init; }
    public required string Destino { get; init; }

    public static ColaFila De(Trabajo t)
    {
        var r = RotuloDeCola.De(t);
        return new ColaFila
        {
            Id = t.Id,
            Estado = r.Estado,
            ColorEstado = Brush.Parse(r.Color),
            Resumen = r.Resumen,
            Destino = t.Destino,
        };
    }
}
