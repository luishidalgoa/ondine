using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué es lo constante en esta serie: la HISTORIA o el EPISODIO.
///
/// <para>
/// El reloj comparaba siempre contra «lo que dura una historia × cuántas trae».
/// Eso vale para las series donde la historia es la unidad y el episodio dura lo
/// que sumen sus trozos. Pero hay series al revés: <b>el episodio dura siempre
/// lo mismo</b> y dentro caben dos historias o tres según el día.
/// </para>
/// <para>
/// Medido en una carpeta real de Crayon Shin-Chan: ficheros de 23:51 a 26:02,
/// todos de unos 24 minutos, con episodios de 2 y de 3 historias mezclados.
/// Dividiendo por historias sale 8:03 en unos y 13:01 en otros — y con la vara
/// de 8:03, un episodio de 2 historias de 24 minutos se marcaba como sospechoso
/// sin serlo.
/// </para>
/// <para>
/// La serie dice sola cuál es su molde: se mide la dispersión de las dos
/// lecturas y gana la que menos varíe. No hace falta configurarlo, y una serie
/// que cambie de formato tampoco arrastra el molde viejo.
/// </para>
/// </summary>
public static class MoldeDeLaVaraTests
{
    private static (TimeSpan, int) O(int min, int seg, int historias) =>
        (new TimeSpan(0, min, seg), historias);

    public static void Todas()
    {
        Program.Seccion("El molde de la vara");

        LaHistoriaEsLaUnidad();
        ElEpisodioEsLaUnidad();
        LoQueNoDebeCambiar();
    }

    // ── Series clásicas: cada historia dura lo mismo y el episodio suma ──
    private static void LaHistoriaEsLaUnidad()
    {
        // Doraemon: episodios de 1, 2 y 3 historias de ~8 min, así que el fichero
        // dura 8, 16 o 24. Lo constante es la historia.
        var obs = new[]
        {
            O(8, 5, 1), O(7, 58, 1), O(8, 2, 1), O(8, 10, 1),
            O(16, 4, 2), O(15, 55, 2), O(16, 12, 2),
            O(24, 6, 3), O(23, 58, 3),
        };
        var vara = MedidaDelCapitulo.Aprender(obs);

        Program.Assert(vara is { Molde: MedidaDelCapitulo.Molde.PorHistoria },
            "si el episodio suma sus historias, la unidad es la historia");
        Program.Assert(vara!.Unidad > new TimeSpan(0, 7, 30) && vara.Unidad < new TimeSpan(0, 8, 30),
            "y la vara son unos ocho minutos");

        // Con esa vara, cada tamaño cuadra con su número de historias.
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 8, 0), 1, vara), "8 min = 1 historia");
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 16, 0), 2, vara), "16 min = 2 historias");
        Program.Assert(!MedidaDelCapitulo.Cuadra(new TimeSpan(0, 24, 0), 1, vara),
            "y 24 min NO es una sola historia: ese aviso tiene que seguir saltando");
    }

    // ── Series donde el episodio manda ──
    private static void ElEpisodioEsLaUnidad()
    {
        // Shin-chan: todo dura 24 minutos, con 2 o 3 historias dentro según el día.
        var obs = new[]
        {
            O(24, 11, 3), O(24, 0, 3), O(24, 9, 3), O(23, 51, 3), O(23, 52, 3), O(23, 52, 3),
            O(26, 2, 2), O(24, 21, 2), O(24, 2, 2), O(24, 1, 2), O(23, 53, 2),
        };
        var vara = MedidaDelCapitulo.Aprender(obs);

        Program.Assert(vara is { Molde: MedidaDelCapitulo.Molde.PorEpisodio },
            "si el episodio dura siempre lo mismo, la unidad es el episodio");
        Program.Assert(vara!.Unidad > new TimeSpan(0, 23, 0) && vara.Unidad < new TimeSpan(0, 25, 0),
            "y la vara son unos veinticuatro minutos");

        // LO QUE ARREGLA: un episodio de DOS historias que dura 24 min ya no es
        // sospechoso — es exactamente lo que dura un episodio de esta serie.
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 24, 2), 2, vara),
            "24 min con 2 historias cuadra");
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 24, 11), 3, vara),
            "y con 3 también: el episodio dura lo mismo lleve lo que lleve");

        // Y sigue cazando lo que de verdad no cuadra.
        Program.Assert(!MedidaDelCapitulo.Cuadra(new TimeSpan(0, 8, 0), 3, vara),
            "un fichero de 8 min NO es un episodio entero de esta serie");
        Program.Assert(!MedidaDelCapitulo.Cuadra(new TimeSpan(0, 48, 0), 2, vara),
            "y uno de 48 tampoco: ahí hay dos episodios");
    }

    // ── Las guardas de siempre ──
    private static void LoQueNoDebeCambiar()
    {
        Program.Eq(null, MedidaDelCapitulo.Aprender(new[] { O(8, 0, 1), O(8, 1, 1) }),
            "con menos muestras de las que hacen falta no hay vara");

        // Sin vara, todo cuadra: no saber no es sospechar.
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 90, 0), 1, null),
            "sin vara no se opina de nada");

        // Una serie de episodios de una sola historia es el caso en que las dos
        // lecturas son idénticas. Ahí manda la de siempre, para no cambiar el
        // comportamiento de lo que ya funcionaba.
        var unaSola = new[] { O(22, 0, 1), O(22, 5, 1), O(21, 58, 1), O(22, 2, 1), O(22, 1, 1), O(21, 59, 1) };
        var v = MedidaDelCapitulo.Aprender(unaSola);
        Program.Assert(v is { Molde: MedidaDelCapitulo.Molde.PorHistoria },
            "con una historia por episodio las dos lecturas coinciden y manda la de siempre");
        Program.Assert(MedidaDelCapitulo.Cuadra(new TimeSpan(0, 22, 0), 1, v), "y 22 min cuadra");
        Program.Assert(!MedidaDelCapitulo.Cuadra(new TimeSpan(0, 44, 0), 1, v),
            "y el doble sigue sin cuadrar");
    }
}
