namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué se hace con el paquete una vez descargado, que no es lo mismo en los cuatro.
///
/// <para>
/// La primera versión de esto solo distinguía dos casos: el instalador de Windows se ejecuta y
/// los demás «se dejan descargados». Eso arregló el fallo gordo —lanzar el <c>.exe</c> de
/// Windows en Linux acababa en el gestor de archivadores— pero se quedó corto: descargar el
/// <c>.deb</c> y abrirte la carpeta es <b>un paso más de los necesarios</b>, y el usuario lo
/// dijo tal cual.
/// </para>
/// <para>
/// Hay un término medio que es justo lo que hace el escritorio: <b>abrir el paquete con su
/// programa</b>. Un <c>.deb</c> abierto así saca el instalador gráfico con su botón «Instalar»
/// —lo mismo que el doble clic—, y un <c>.dmg</c> se monta y enseña la ventana para arrastrar
/// Ondine a Aplicaciones. Sin pedir contraseña por nuestra cuenta y sin prometer una
/// instalación silenciosa que no podemos dar.
/// </para>
/// <para>
/// El AppImage es el caso aparte: no se instala, se ejecuta. Y descargado <b>viene sin permiso
/// de ejecución</b>, así que abrirlo no haría nada — hay que ponérselo primero, o el fichero es
/// peso muerto.
/// </para>
/// </summary>
public static class ComoSeInstalaTests
{
    public static void Todas()
    {
        Program.Seccion("Qué se hace con el paquete descargado");

        CadaUnoLoSuyo();
        SoloWindowsSeInstalaSolo();
        ElAppImageNecesitaPermiso();
    }

    private static void CadaUnoLoSuyo()
    {
        Program.Assert(
            Updater.ComoSeInstala(Updater.Paquete.InstaladorDeWindows) == Updater.Entrega.EjecutarYSalir,
            "el instalador de Windows se ejecuta y la app se cierra para dejarle trabajar");

        Program.Assert(
            Updater.ComoSeInstala(Updater.Paquete.DebDeLinux) == Updater.Entrega.AbrirConSuPrograma,
            "el .deb se abre con su instalador gráfico, que es lo que hace el doble clic");

        Program.Assert(
            Updater.ComoSeInstala(Updater.Paquete.DmgDeMac) == Updater.Entrega.AbrirConSuPrograma,
            "y el .dmg se monta y enseña la ventana de arrastrar a Aplicaciones");

        Program.Assert(
            Updater.ComoSeInstala(Updater.Paquete.AppImageDeLinux) == Updater.Entrega.MarcarEjecutableYEnsenar,
            "el AppImage no se instala: se le da permiso de ejecución y se enseña dónde está");
    }

    /// <summary>
    /// Cerrarse solo lo puede hacer uno. Es lo que separa «ejecutar y salir» de todo lo demás:
    /// el instalador de Windows necesita que los ficheros no estén en uso, y los otros tres
    /// dejan a la aplicación viva mientras el usuario decide.
    /// </summary>
    private static void SoloWindowsSeInstalaSolo()
    {
        foreach (var p in Enum.GetValues<Updater.Paquete>())
            Program.Assert(
                Updater.SeInstalaSolo(p) == (Updater.ComoSeInstala(p) == Updater.Entrega.EjecutarYSalir),
                $"«{p}»: cerrarse y ejecutar el instalador van juntos, o ninguno");
    }

    /// <summary>
    /// Y el permiso de ejecución solo lo necesita el AppImage. Ponérselo a un <c>.deb</c> no
    /// haría daño pero tampoco nada, y decir que hace falta donde no hace falta es la clase de
    /// detalle que luego alguien copia a otro sitio.
    /// </summary>
    private static void ElAppImageNecesitaPermiso()
    {
        var conPermiso = Enum.GetValues<Updater.Paquete>()
            .Where(p => Updater.ComoSeInstala(p) == Updater.Entrega.MarcarEjecutableYEnsenar)
            .ToList();

        Program.Assert(conPermiso.Count == 1 && conPermiso[0] == Updater.Paquete.AppImageDeLinux,
            $"solo el AppImage necesita que se le dé permiso ({conPermiso.Count})");
    }
}
