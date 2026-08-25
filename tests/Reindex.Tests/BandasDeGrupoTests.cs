namespace Ondine.Reindex.Tests;

/// <summary>
/// Las bandas que separan las temporadas en la tabla de Organizar.
///
/// <para>
/// Con doscientos ficheros de cinco temporadas, la tabla sin separar es un muro. Las bandas
/// dicen dónde empieza cada temporada y cuántos ficheros trae, y eso es lo que permite mirar
/// «la 3» sin contar filas.
/// </para>
/// <para>
/// <b>Baja al motor ahora y no al portar la pantalla, y hay un motivo concreto:</b> esto vivía
/// pegado a <c>CollectionViewSource</c>, que <b>en Avalonia no existe</b>. El mecanismo de
/// filtrar y agrupar cambia entero, así que si la decisión se quedara arriba habría que
/// reescribirla a la vez que la fontanería — y entonces un fallo no se sabría de cuál de las
/// dos cosas es.
/// </para>
/// <para>
/// Y falla callando: unas bandas mal puestas dan una tabla que se lee perfectamente y miente
/// sobre dónde empieza cada temporada.
/// </para>
/// </summary>
public static class BandasDeGrupoTests
{
    public static void Todas()
    {
        Program.Seccion("Las bandas que separan temporadas");

        LoNormal();
        CuandoNoSePonen();
        LoQueRompeUnaCuenta();
    }

    private static void LoNormal()
    {
        // Tres temporadas seguidas. La banda va en la PRIMERA de cada tramo y dice cuántas
        // hay en él.
        string[] grupos = ["T1", "T1", "T1", "T2", "T3", "T3"];
        var bandas = BandasDeGrupo.Calcular(grupos, ordenManual: false);

        Program.Assert(bandas.Count == 3, $"tres temporadas, tres bandas ({bandas.Count})");
        Program.Assert(bandas[0].Indice == 0 && bandas[0].Cuantos == 3,
            $"la primera abre en la fila 0 con 3 ficheros ({bandas[0].Indice}/{bandas[0].Cuantos})");
        Program.Assert(bandas[1].Indice == 3 && bandas[1].Cuantos == 1,
            $"la segunda en la 3 con 1 ({bandas[1].Indice}/{bandas[1].Cuantos})");
        Program.Assert(bandas[2].Indice == 4 && bandas[2].Cuantos == 2,
            $"y la tercera en la 4 con 2 ({bandas[2].Indice}/{bandas[2].Cuantos})");

        // La suma tiene que dar el total. Si no, hay filas que no están bajo ninguna banda y
        // la tabla enseña ficheros colgando de la temporada anterior.
        Program.Assert(bandas.Sum(b => b.Cuantos) == grupos.Length,
            "y entre todas cubren todas las filas, sin dejar ninguna colgando");
    }

    private static void CuandoNoSePonen()
    {
        // ══ Una sola temporada: la banda no separa nada ══════════════════════
        // Una banda que dice «Temporada 1 · 40 ficheros» encima de una tabla que solo tiene
        // la temporada 1 es una línea de ruido y un renglón menos de tabla.
        Program.Assert(BandasDeGrupo.Calcular(["T1", "T1", "T1"], false).Count == 0,
            "con una sola temporada no se pone ninguna banda: no separaría nada");

        Program.Assert(BandasDeGrupo.Calcular([], false).Count == 0, "y con la tabla vacía tampoco");
        Program.Assert(BandasDeGrupo.Calcular(["T1"], false).Count == 0, "ni con una sola fila");

        // ══ Con un orden por cabecera, las bandas MIENTEN ════════════════════
        // Ordenando por nombre las temporadas se entremezclan, así que «aquí empieza la 2»
        // deja de ser verdad. Se quitan hasta que se quite el orden.
        Program.Assert(BandasDeGrupo.Calcular(["T1", "T2", "T1", "T2"], ordenManual: true).Count == 0,
            "y con un orden por cabecera activo se quitan: entremezcladas ya no dicen la verdad");
    }

    private static void LoQueRompeUnaCuenta()
    {
        // Grupos que VUELVEN a aparecer. Pasa al ordenar por otra cosa y volver, o con un
        // catálogo cuyos números saltan: contar «cuántas T1 hay en total» daría 3 y lo que
        // hay son dos tramos de 2 y 1. La banda cuenta EL TRAMO, no el grupo.
        var bandas = BandasDeGrupo.Calcular(["T1", "T1", "T2", "T1"], false);
        Program.Assert(bandas.Count == 3,
            $"un grupo que reaparece abre otra banda, no engorda la primera ({bandas.Count})");
        Program.Assert(bandas[0].Cuantos == 2 && bandas[2].Cuantos == 1,
            "y cada tramo cuenta lo suyo");

        // Un grupo vacío es un grupo: son las filas sin temporada, y van juntas.
        var conVacios = BandasDeGrupo.Calcular(["", "", "T1"], false);
        Program.Assert(conVacios.Count == 2,
            $"las filas sin temporada son un grupo como otro ({conVacios.Count})");

        // Y el nombre del grupo viaja con la banda: quien la pinta necesita el rótulo.
        Program.Assert(conVacios[1].Grupo == "T1", "la banda sabe de qué grupo es");
    }
}
