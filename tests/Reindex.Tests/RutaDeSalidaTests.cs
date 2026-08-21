using Ondine.Recortes;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Elegir dónde escribe un tramo sin pisar nada.
///
/// <para>
/// Hay <b>dos</b> formas de pisar y las dos han pasado en esta app: que el fichero ya
/// estuviera en la carpeta de un intento anterior, y que dos tramos de la MISMA tanda
/// quieran el mismo nombre porque el usuario los llamó igual. Comprobar solo el disco
/// deja pasar la segunda, y es la que más rabia da: exportas cinco y aparecen tres.
/// </para>
/// <para>
/// <b>Nada de rutas de Windows escritas a mano.</b> La primera versión comparaba contra
/// <c>"\\intro.mkv"</c> y pasaba aquí y fallaba en CI, que corre en Linux: allí el
/// separador es otro. Se compara contra lo que arma <c>Path.Combine</c>, que es lo que
/// hace el código.
/// </para>
/// </summary>
public static class RutaDeSalidaTests
{
    /// <summary>Una carpeta cualquiera, válida en cualquier sistema.</summary>
    private static readonly string Carpeta = Path.Combine(Path.GetTempPath(), "recortes");

    private static string Esperada(string fichero) => Path.Combine(Carpeta, fichero);

    public static void Todas()
    {
        Program.Seccion("Dónde escribe un tramo sin pisar nada");

        var enDisco = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enLaTanda = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool Existe(string r) => enDisco.Contains(r);

        // ── Camino libre ──────────────────────────────────────────────────────────
        var a = RutaDeSalida.Libre(Carpeta, "intro", ".mkv", Existe, enLaTanda);
        Program.Assert(a == Esperada("intro.mkv"),
            "con la carpeta vacía se usa el nombre tal cual");

        // ── Dos tramos de la misma tanda con el mismo nombre ──────────────────────
        var b = RutaDeSalida.Libre(Carpeta, "intro", ".mkv", Existe, enLaTanda);
        Program.Assert(b == Esperada("intro (2).mkv"),
            "el segundo de la misma tanda no pisa al primero, aunque el primero no esté en disco todavía");

        var c = RutaDeSalida.Libre(Carpeta, "intro", ".mkv", Existe, enLaTanda);
        Program.Assert(c == Esperada("intro (3).mkv"), "y el tercero sigue la cuenta");

        // ── Lo que ya estaba en el disco ──────────────────────────────────────────
        enDisco.Add(Esperada("final.mkv"));
        var d = RutaDeSalida.Libre(Carpeta, "final", ".mkv", Existe, new HashSet<string>());
        Program.Assert(d == Esperada("final (2).mkv"),
            "un fichero de un intento anterior no se sobrescribe en silencio");

        // ── Las dos cosas a la vez ────────────────────────────────────────────────
        enDisco.Add(Esperada("mezcla.mkv"));
        enDisco.Add(Esperada("mezcla (2).mkv"));
        var tanda = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var e1 = RutaDeSalida.Libre(Carpeta, "mezcla", ".mkv", Existe, tanda);
        Program.Assert(e1 == Esperada("mezcla (3).mkv"), "se salta lo que hay en disco");
        var e2 = RutaDeSalida.Libre(Carpeta, "mezcla", ".mkv", Existe, tanda);
        Program.Assert(e2 == Esperada("mezcla (4).mkv"), "y además lo que ya reservó esta tanda");

        // ══ Los nombres que no valen, y valen IGUAL en los dos sistemas ═══════════
        // El nombre de un tramo lo escribe el usuario en una caja de texto, así que llega
        // lo que sea. Y esto lo enseñó CI: `Path.GetInvalidFileNameChars()` en Linux trae
        // solo «/» y el nulo, así que «<», «:» o «?» pasaban enteros. El fichero se creaba
        // tan campante y luego era inusable en Windows — y una biblioteca de vídeo viaja
        // entre sistemas todo el rato: disco externo, red, nube.
        var sucio = Path.GetFileName(
            RutaDeSalida.Libre(Carpeta, "cap 1: el <inicio>", ".mkv", _ => false, new HashSet<string>()));

        foreach (var malo in new[] { ':', '<', '>', '/', '\\', '*', '?', '"', '|' })
            Program.Assert(!sucio.Contains(malo),
                $"«{malo}» no llega al nombre del fichero, corra donde corra esto");

        Program.Assert(sucio.StartsWith("cap 1", StringComparison.Ordinal),
            "pero lo que sí vale del nombre se conserva: no se tira el nombre entero");

        var vacio = RutaDeSalida.Libre(Carpeta, "   ", ".mkv", _ => false, new HashSet<string>());
        Program.Assert(Path.GetFileNameWithoutExtension(vacio) == RutaDeSalida.SinNombre,
            "un nombre en blanco no deja un fichero sin nombre: se pone uno");

        var soloSignos = RutaDeSalida.Libre(Carpeta, "???", ".mkv", _ => false, new HashSet<string>());
        Program.Assert(Path.GetFileNameWithoutExtension(soloSignos) == RutaDeSalida.SinNombre,
            "y un nombre que se queda en nada al limpiarlo, tampoco");
    }
}
