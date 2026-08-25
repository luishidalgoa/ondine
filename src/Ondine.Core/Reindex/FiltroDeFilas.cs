namespace Ondine.Reindex;

/// <summary>
/// Qué filas quedan a la vista en la tabla de Organizar.
///
/// <para>
/// Se compone de tres cosas que <b>se acumulan</b>: los distintivos de estado, la casilla de
/// «solo dudas» y lo escrito en el buscador. Se acotan entre sí, así que se puede pedir «los
/// conflictos que dicen playa» sin ir mirando fila por fila.
/// </para>
/// <para>
/// <b>Vive aquí y no en la pantalla por dos motivos.</b> El primero es de forma: arriba estaba
/// escrito sobre <c>CollectionViewSource</c>, que en Avalonia no existe, así que al portar se
/// reescribe el mecanismo — y la decisión no tiene por qué reescribirse con él.
/// </para>
/// <para>
/// El segundo es de riesgo: <b>este es el sitio con más formas de equivocarse en silencio de
/// toda la pantalla</b>. Un filtro que esconde una fila de más no da ningún error; hace que
/// ese fichero no exista para quien mira, y entonces no se marca y no se aplica. El fichero
/// se queda sin renombrar y nadie sabe por qué.
/// </para>
/// </summary>
public sealed class FiltroDeFilas
{
    /// <summary>Lo justo que el filtro necesita saber de una fila.</summary>
    public readonly record struct Fila(ReindexEstado Estado, bool EsDuda, FilaBuscable Buscable);

    private readonly IReadOnlyList<ReindexEstado> _estados;
    private readonly bool _soloDudas;
    private readonly BusquedaDeFilas.Consulta _consulta;

    private FiltroDeFilas(IReadOnlyList<ReindexEstado> estados, bool soloDudas,
                          BusquedaDeFilas.Consulta consulta)
    {
        _estados = estados;
        _soloDudas = soloDudas;
        _consulta = consulta;
    }

    /// <summary>
    /// El filtro de lo que hay puesto ahora mismo en la pantalla.
    ///
    /// <para>
    /// El texto se normaliza <b>una vez</b>, aquí, y no por fila: esto se recalcula a cada
    /// tecla sobre la tabla entera, que pueden ser cientos de filas.
    /// </para>
    /// </summary>
    public static FiltroDeFilas De(IReadOnlyList<ReindexEstado> estados, bool soloDudas, string? texto) =>
        new(estados, soloDudas, BusquedaDeFilas.Consulta.De(texto));

    /// <summary>
    /// Si no hay nada puesto. Quien lo usa puede entonces ahorrarse el recorrido entero y
    /// quitar el filtro en vez de aplicar uno que deja pasar todo.
    /// </summary>
    public bool NoFiltraNada => _estados.Count == 0 && !_soloDudas && _consulta.Vacia;

    /// <summary>Si esta fila se ve.</summary>
    public bool Pasa(Fila f)
    {
        if (_soloDudas && !f.EsDuda) return false;
        if (!BusquedaDeFilas.Pasa(f.Buscable, _consulta)) return false;

        // Los estados SE SUMAN: una fila tiene un estado y solo uno, así que cruzarlos daría
        // siempre cero. Sin ninguno puesto, cualquiera vale.
        return _estados.Count == 0 || _estados.Contains(f.Estado);
    }
}
