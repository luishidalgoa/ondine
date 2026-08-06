using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// A qué carpeta va cada capítulo, y —lo que más importa— cuándo NO hay que moverlo.
/// </summary>
public static class DestinoDeTemporadaTests
{
    private static string R(params string[] p) => Path.Combine(p);

    public static void Todas()
    {
        Program.Seccion("A qué carpeta va cada capítulo");

        var raiz = R("C:", "Plex", "Doraemon (1979)");

        // Lo normal: está suelto en la raíz y le toca su temporada.
        Program.Assert(
            DestinoDeTemporada.Carpeta(raiz, 3, enCastellano: false) == R(raiz, "Season 03"),
            "un capítulo de la 3 va a «Season 03» bajo la raíz de la serie");

        // El caso que da sentido a todo esto: descargado en otro sitio.
        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "Season 05", "cap.mkv"), raiz, 3, false)
                == R(raiz, "Season 03"),
            "si está en la temporada equivocada, se dice a cuál va");

        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "cap.mkv"), raiz, 3, false)
                == R(raiz, "Season 03"),
            "y si está suelto en la raíz, también");

        // EL IMPORTANTE: no mover lo que ya está bien. Un reordenado que toca
        // ficheros correctos es un riesgo cobrado a cambio de nada, y además
        // llena el historial de deshacer de ruido que tapa lo que sí cambió.
        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "Season 03", "cap.mkv"), raiz, 3, false) is null,
            "el que ya está en su carpeta no se toca");

        // Y la misma carpeta escrita en el otro idioma TAMPOCO se mueve. Cambiar
        // el ajuste de idioma no puede convertir una biblioteca entera en
        // «pendiente de reordenar»: la carpeta ya dice la temporada correcta.
        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "Temporada 03", "cap.mkv"), raiz, 3, false) is null,
            "«Temporada 03» ya es la temporada 3: cambiar de idioma no reordena la biblioteca");

        // Subcarpetas por debajo de la temporada: está dentro de la suya, se queda.
        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "Season 03", "extras", "cap.mkv"), raiz, 3, false) is null,
            "un fichero anidado bajo su temporada ya cuelga de ella");

        // Una temporada imposible no genera destino: sin nombre de carpeta no hay
        // a dónde ir, y desde luego no se inventa uno.
        Program.Assert(
            DestinoDeTemporada.Carpeta(raiz, -1, false) is null,
            "una temporada imposible no tiene carpeta destino");
        Program.Assert(
            DestinoDeTemporada.HayQueMover(R(raiz, "cap.mkv"), raiz, -1, false) is null,
            "y por tanto tampoco manda mover nada");
    }
}
