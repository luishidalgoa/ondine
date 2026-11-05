namespace Ondine.Reindex.Tests;

/// <summary>
/// La salida de una compresión nunca puede caer sobre un fichero de la tanda.
///
/// <para>
/// El fallo: con «sobrescribir» activo, si el nombre de salida coincidía con el del original (mismo
/// nombre, misma carpeta, mismo contenedor), el motor borraba el original y escribía el comprimido en
/// su sitio. Como la salida y el original eran el mismo fichero, la Papelera no lo recogía: se perdía.
/// </para>
/// <para>
/// Lo que se fija aquí es la comparación que impide eso: por ruta completa y sin distinguir mayúsculas.
/// </para>
/// </summary>
public static class SalidaSobreOriginalTests
{
    public static void Todas()
    {
        Program.Seccion("La salida nunca sustituye un fichero de la tanda");

        LaMismaRutaCuenta();
        SinDistinguirMayusculas();
        UnaCarpetaDistintaNoCuenta();
        OtroFicheroDeLaTandaCuenta();
    }

    private static HashSet<string> Lote(params string[] rutas) =>
        new(rutas.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);

    private static void LaMismaRutaCuenta()
    {
        var original = Path.GetFullPath(Path.Combine("carpeta", "pelicula.mkv"));
        Program.Assert(Engine.SalidaEsDelLote(original, Lote(original)),
            "si la salida es el propio original, no se escribe");
    }

    private static void SinDistinguirMayusculas()
    {
        var original = Path.GetFullPath(Path.Combine("carpeta", "Pelicula.MKV"));
        var salida = Path.GetFullPath(Path.Combine("carpeta", "pelicula.mkv"));
        Program.Assert(Engine.SalidaEsDelLote(salida, Lote(original)),
            "y tampoco si solo cambian las mayúsculas: en Windows es el mismo fichero");
    }

    private static void UnaCarpetaDistintaNoCuenta()
    {
        var original = Path.GetFullPath(Path.Combine("origen", "pelicula.mkv"));
        var salida = Path.GetFullPath(Path.Combine("destino", "pelicula.mkv"));
        Program.Assert(!Engine.SalidaEsDelLote(salida, Lote(original)),
            "con el mismo nombre pero en otra carpeta, la salida es un fichero nuevo y se puede escribir");
    }

    private static void OtroFicheroDeLaTandaCuenta()
    {
        var a = Path.GetFullPath(Path.Combine("carpeta", "uno.mkv"));
        var b = Path.GetFullPath(Path.Combine("carpeta", "dos.mkv"));
        Program.Assert(Engine.SalidaEsDelLote(b, Lote(a, b)),
            "y la salida que caería sobre otro fichero de la tanda tampoco se escribe");
    }
}
