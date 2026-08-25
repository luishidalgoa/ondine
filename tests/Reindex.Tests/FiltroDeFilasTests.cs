namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué filas quedan a la vista en la tabla de Organizar.
///
/// <para>
/// El filtro se compone de tres cosas que se acumulan: los distintivos de estado —limpios,
/// corregidos, especiales, conflictos, errores—, la casilla de «solo dudas» y lo escrito en
/// el buscador. <b>Se acotan entre sí</b>: se puede buscar «playa» y quedarse solo con los
/// conflictos.
/// </para>
/// <para>
/// Baja al motor por lo mismo que las bandas: arriba vivía sobre <c>CollectionViewSource</c>,
/// que en Avalonia no existe. Y porque este es el sitio con más formas de equivocarse en
/// silencio de toda la pantalla — un filtro que esconde una fila de más no da ningún error,
/// simplemente hace que un fichero no exista para quien mira, <b>y entonces no se aplica</b>.
/// </para>
/// </summary>
public static class FiltroDeFilasTests
{
    /// <summary>Una fila de mentira, con lo justo que el filtro mira.</summary>
    private static FiltroDeFilas.Fila F(ReindexEstado estado, bool duda, string nombre) =>
        new(estado, duda, new FilaBuscable(false, nombre, nombre, nombre));

    public static void Todas()
    {
        Program.Seccion("Qué filas quedan a la vista");

        SinNadaPuestoSeVeTodo();
        LosDistintivosSeSuman();
        LasTresCosasSeAcotanEntreSi();
    }

    private static void SinNadaPuestoSeVeTodo()
    {
        var filas = new[]
        {
            F(ReindexEstado.Limpio, false, "uno.mkv"),
            F(ReindexEstado.Conflicto, true, "dos.mkv"),
        };

        var todo = FiltroDeFilas.De([], soloDudas: false, "");
        Program.Assert(todo.NoFiltraNada,
            "sin nada puesto el filtro se declara vacío, y quien lo usa puede ahorrarse recorrer");
        Program.Assert(filas.All(todo.Pasa), "y no esconde ninguna fila");

        // Espacios en el buscador no son una búsqueda. Sin esto, un espacio de más al pegar
        // una ruta escondería la tabla entera y parecería que no hay ficheros.
        Program.Assert(FiltroDeFilas.De([], false, "   ").NoFiltraNada,
            "y un buscador con solo espacios tampoco filtra: esconder la tabla por un espacio de más");
    }

    private static void LosDistintivosSeSuman()
    {
        var filas = new[]
        {
            F(ReindexEstado.Limpio, false, "limpio.mkv"),
            F(ReindexEstado.Corregido, false, "corregido.mkv"),
            F(ReindexEstado.Conflicto, true, "conflicto.mkv"),
            F(ReindexEstado.Error, false, "error.mkv"),
        };

        // Uno solo: solo ese.
        var soloConflictos = FiltroDeFilas.De([ReindexEstado.Conflicto], false, "");
        Program.Assert(filas.Count(soloConflictos.Pasa) == 1, "un distintivo deja solo su estado");

        // Dos: los dos. Se SUMAN, no se cruzan — cruzarlos daría siempre cero, porque una
        // fila tiene un estado y solo uno.
        var dos = FiltroDeFilas.De([ReindexEstado.Limpio, ReindexEstado.Error], false, "");
        Program.Assert(filas.Count(dos.Pasa) == 2,
            "dos distintivos suman sus estados; cruzarlos daría siempre cero");

        Program.Assert(!dos.NoFiltraNada, "y con distintivos puestos el filtro ya no está vacío");
    }

    private static void LasTresCosasSeAcotanEntreSi()
    {
        var filas = new[]
        {
            F(ReindexEstado.Conflicto, true,  "la playa.mkv"),
            F(ReindexEstado.Conflicto, false, "la montaña.mkv"),
            F(ReindexEstado.Limpio,    true,  "la playa otra vez.mkv"),
        };

        // Buscar + distintivo: las dos cosas a la vez. Es lo que permite «los conflictos que
        // dicen playa» sin ir mirando fila por fila.
        var cruce = FiltroDeFilas.De([ReindexEstado.Conflicto], false, "playa");
        Program.Assert(filas.Count(cruce.Pasa) == 1,
            $"buscar y filtrar por estado se acotan entre sí ({filas.Count(cruce.Pasa)})");

        // «Solo dudas» encima de todo lo demás.
        var dudas = FiltroDeFilas.De([ReindexEstado.Conflicto], soloDudas: true, "");
        Program.Assert(filas.Count(dudas.Pasa) == 1,
            $"«solo dudas» se acota con el estado, no lo sustituye ({filas.Count(dudas.Pasa)})");

        // Las tres juntas y sin resultados: es un caso legítimo y tiene que salir vacío, no
        // caerse al «no hay filtro».
        var nada = FiltroDeFilas.De([ReindexEstado.Limpio], true, "montaña");
        Program.Assert(filas.Count(nada.Pasa) == 0,
            "y las tres juntas pueden no dejar ninguna, que es una respuesta y no un error");
        Program.Assert(!nada.NoFiltraNada, "sin confundirlo con no tener filtro");
    }
}
