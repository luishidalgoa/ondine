namespace Ondine.Reindex.Tests;

/// <summary>
/// La reserva de un fichero: que dos copias de Ondine (o dos usos dentro de una misma) no tengan el mismo
/// vídeo a la vez. Sin esto, la app y el MCP podían comprimir y mandar a la Papelera el mismo fichero.
/// </summary>
public static class ReservaDeFicheroTests
{
    public static void Todas()
    {
        Program.Seccion("La reserva de un fichero entre copias de Ondine");

        LaSegundaReservaNoSeDaMientrasLaPrimeraVive();
        LaReservaSeLibera();
        EsperarTerminaCuandoNoSeLibera();
        RutasDistintasNoSeEstorban();
    }

    private static string Ruta(string nombre) =>
        Path.Combine(Path.GetTempPath(), "ondine-reserva-" + Guid.NewGuid().ToString("N")[..6], nombre);

    private static void LaSegundaReservaNoSeDaMientrasLaPrimeraVive()
    {
        var ruta = Ruta("pelicula.mkv");
        using var primera = ReservaDeFichero.Tomar(ruta);
        Program.Assert(primera is not null, "la primera copia consigue la reserva");

        using var segunda = ReservaDeFichero.Tomar(ruta);
        Program.Assert(segunda is null, "mientras la primera la tiene, otra no la consigue");
    }

    private static void LaReservaSeLibera()
    {
        var ruta = Ruta("serie.mp4");
        var primera = ReservaDeFichero.Tomar(ruta);
        primera!.Dispose();

        using var despues = ReservaDeFichero.Tomar(ruta);
        Program.Assert(despues is not null, "al soltarla, otra copia puede tomarla");
    }

    private static void EsperarTerminaCuandoNoSeLibera()
    {
        var ruta = Ruta("corto.mkv");
        using var retenida = ReservaDeFichero.Tomar(ruta);
        var inicio = DateTime.UtcNow;
        var r = ReservaDeFichero.Esperar(ruta, TimeSpan.FromMilliseconds(300));
        var tardo = DateTime.UtcNow - inicio;

        Program.Assert(r is null && tardo < TimeSpan.FromSeconds(3),
            "esperar a una reserva que no se libera termina a su plazo, y no se queda colgado");
    }

    private static void RutasDistintasNoSeEstorban()
    {
        using var a = ReservaDeFichero.Tomar(Ruta("uno.mkv"));
        using var b = ReservaDeFichero.Tomar(Ruta("dos.mkv"));
        Program.Assert(a is not null && b is not null,
            "dos ficheros distintos se reservan a la vez sin estorbarse");
    }
}
