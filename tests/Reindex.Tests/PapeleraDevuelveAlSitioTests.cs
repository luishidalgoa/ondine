using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Lo que la papelera propia le entrega a la papelera del sistema.
///
/// <para>
/// Ondine tiene dos papeleras y encadenadas: lo que borras va primero a la <b>suya</b> —una
/// carpeta dentro de los datos de la aplicación, para que Ctrl+Z sea instantáneo— y de ahí, al
/// cabo de un rato o al cerrar, a la <b>del sistema</b>.
/// </para>
/// <para>
/// <b>Y en el paso de una a otra se perdían los ficheros.</b> Se le entregaba a la papelera del
/// sistema la ruta <i>interna</i> —<c>…/Ondine/papelera/&lt;guid&gt;/x.mkv</c>— teniendo al lado
/// la original. La papelera del sistema apunta la procedencia de lo que le dan: en Linux la
/// escribe en el <c>.trashinfo</c>, y en Windows y macOS la guardan la Shell y el Finder. Así
/// que «Restaurar» devolvía el vídeo <b>a la carpeta interna de la aplicación</b>, que es la
/// que se vacía al cerrar, recursivamente y sin pasar por ninguna papelera.
/// </para>
/// <para>
/// O sea: recuperar el fichero era la forma de perderlo. Y todo el camino «funcionaba» —el
/// fichero aparecía en la papelera del escritorio, con su nombre—, así que no había nada que
/// mirara mal.
/// </para>
/// </summary>
public static class PapeleraDevuelveAlSitioTests
{
    public static void Todas()
    {
        Program.Seccion("Qué se le entrega a la papelera del sistema");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-papelera-encadenada");
        var antes = PapeleraApp.EnviarASistema;
        try
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, true);
            Directory.CreateDirectory(raiz);

            SeEntregaLaRutaOriginal(raiz);
            SiElNombreEstaOcupadoNoSePisa(raiz);
        }
        finally
        {
            PapeleraApp.EnviarASistema = antes;
            try { Directory.Delete(raiz, true); } catch { }
        }
    }

    /// <summary>
    /// El fichero vuelve a su carpeta antes de irse, y es ESA ruta la que se le da al sistema.
    /// </summary>
    private static void SeEntregaLaRutaOriginal(string raiz)
    {
        var carpeta = Path.Combine(raiz, "Series", "Temporada 1");
        Directory.CreateDirectory(carpeta);
        var original = Path.Combine(carpeta, "cap01.mkv");
        File.WriteAllText(original, "contenido");

        string? entregada = null;
        PapeleraApp.EnviarASistema = r => { entregada = r; try { File.Delete(r); } catch { } return true; };

        var id = PapeleraApp.Enviar(original);
        Program.Assert(id is not null, "el fichero entra en la papelera propia");
        Program.Assert(!File.Exists(original), "y deja de estar donde estaba");

        PapeleraApp.VaciarAlCerrar();

        Program.Assert(entregada is not null, "al vaciar, se le entrega a la papelera del sistema");
        Program.Assert(entregada == original,
            $"y lo que se entrega es la ruta ORIGINAL, no la interna\n      esperada: {original}\n      recibida: {entregada}");
    }

    /// <summary>
    /// Y si mientras estaba en la papelera apareció otro fichero con su nombre, no se pisa: se
    /// devuelve con un nombre libre. Pisarlo sería perder uno de verdad, no mandarlo a la
    /// papelera.
    /// </summary>
    private static void SiElNombreEstaOcupadoNoSePisa(string raiz)
    {
        var carpeta = Path.Combine(raiz, "Pelis");
        Directory.CreateDirectory(carpeta);
        var original = Path.Combine(carpeta, "peli.mkv");
        File.WriteAllText(original, "el viejo");

        string? entregada = null;
        PapeleraApp.EnviarASistema = r => { entregada = r; try { File.Delete(r); } catch { } return true; };

        PapeleraApp.Enviar(original);

        // Alguien deja otro con el mismo nombre mientras el primero está en la papelera.
        File.WriteAllText(original, "el nuevo");

        PapeleraApp.VaciarAlCerrar();

        Program.Assert(entregada is not null && entregada != original,
            $"con el nombre ocupado se devuelve con otro nombre ({Path.GetFileName(entregada ?? "")})");
        Program.Assert(File.Exists(original) && File.ReadAllText(original) == "el nuevo",
            "y el que estaba en su sitio sigue ahí, intacto");
    }
}
