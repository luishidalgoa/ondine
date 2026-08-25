namespace Ondine.Reindex.Tests;

/// <summary>
/// A dónde va lo borrado en cada sistema.
///
/// <para>
/// <b>Esto tapa un agujero que quedó escrito como si no lo fuera.</b> La papelera del sistema
/// se escribió con la especificación de freedesktop.org, y su propio comentario decía «en
/// Linux y macOS». La primera mitad es verdad; la segunda no: freedesktop es el acuerdo de
/// los escritorios de Linux, y <c>~/.local/share/Trash</c> en un Mac <b>no es la papelera</b>
/// — es una carpeta oculta cualquiera, que el Finder no mira y que no aparece en el Dock.
/// </para>
/// <para>
/// El resultado era el mismo que ya se había arreglado una vez para Windows, y por eso duele:
/// no se perdían ficheros, pero lo «enviado a la papelera» se iba a un sitio donde nadie lo
/// encontraría y del que no se puede recuperar. Un fallo silencioso que <b>parece</b>
/// funcionar, que es la peor clase.
/// </para>
/// <para>
/// Lo que se prueba aquí es <b>la decisión</b>, que es lo que estaba mal y lo que se puede
/// comprobar desde cualquier sistema. Que la llamada al Finder funcione solo se ve en un Mac,
/// y eso queda dicho donde toca.
/// </para>
/// </summary>
public static class PapeleraDeMacTests
{
    public static void Todas()
    {
        Program.Seccion("A qué papelera va cada sistema");

        CadaSistemaALaSuya();
        MacNoCaeEnLaDeFreedesktop();
        LasTresVienenPuestas();
    }

    private static void CadaSistemaALaSuya()
    {
        Program.Assert(
            PapeleraDelSistema.Quien(windows: true, mac: false, casaDePruebas: false)
                == PapeleraDelSistema.ADonde.Shell,
            "en Windows manda la Shell, que es la que hace la papelera de verdad");

        Program.Assert(
            PapeleraDelSistema.Quien(windows: false, mac: true, casaDePruebas: false)
                == PapeleraDelSistema.ADonde.Finder,
            "en macOS manda el Finder");

        Program.Assert(
            PapeleraDelSistema.Quien(windows: false, mac: false, casaDePruebas: false)
                == PapeleraDelSistema.ADonde.Freedesktop,
            "y en Linux, el acuerdo de carpetas de freedesktop");
    }

    /// <summary>
    /// El fallo, dicho como prueba. Sin esto, macOS entraba por el mismo sitio que Linux y
    /// nada se quejaba: se creaban las carpetas, se movía el fichero y se devolvía «hecho».
    /// </summary>
    private static void MacNoCaeEnLaDeFreedesktop()
    {
        Program.Assert(
            PapeleraDelSistema.Quien(windows: false, mac: true, casaDePruebas: false)
                != PapeleraDelSistema.ADonde.Freedesktop,
            "macOS NO cae en la de freedesktop: ahí el Finder no mira, y no habría «Restaurar»");

        // Con una casa de pruebas sí, y a propósito: es como las pruebas de la papelera
        // trabajan con ficheros de verdad sin tocar la papelera de quien las ejecuta.
        Program.Assert(
            PapeleraDelSistema.Quien(windows: true, mac: false, casaDePruebas: true)
                == PapeleraDelSistema.ADonde.Freedesktop,
            "con una casa de pruebas se usa la de carpetas en cualquier sistema, que es la que se puede mirar");
    }

    /// <summary>
    /// El cierre del agujero: que las tres vengan enchufadas. Las dos nativas eran huecos que
    /// rellenaba la interfaz al arrancar, y una interfaz nueva no los rellenó — dos veces.
    /// </summary>
    private static void LasTresVienenPuestas()
    {
        Program.Assert(PapeleraDelSistema.EnWindows is not null,
            "la de Windows viene puesta de fábrica");
        Program.Assert(PapeleraDelSistema.EnMac is not null,
            "y la de macOS también: nadie tiene que acordarse de conectarla");
    }
}
