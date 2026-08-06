using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// A qué carpeta le toca vivir a un capítulo, y cuándo ya está donde debe.
///
/// <para>
/// Aquí NO se toca el disco: solo se hace cuentas con rutas. Separarlo es lo que
/// permite probar la decisión —que es donde están los errores caros— sin mover
/// un solo fichero para averiguarlo.
/// </para>
/// </summary>
public static class DestinoDeTemporada
{
    /// <summary>La carpeta que le toca, o <c>null</c> si la temporada no da nombre.</summary>
    public static string? Carpeta(string raiz, int temporada, bool enCastellano)
    {
        var nombre = CarpetaDeTemporada.Nombre(temporada, enCastellano);
        return nombre is null ? null : Path.Combine(raiz, nombre);
    }

    /// <summary>
    /// A dónde hay que moverlo, o <c>null</c> si se queda como está.
    ///
    /// <para>
    /// Devolver <c>null</c> es la respuesta más importante de las dos. Mover un
    /// fichero que ya estaba bien es correr un riesgo a cambio de nada, y además
    /// llena el historial de deshacer de ruido que tapa lo que sí cambió.
    /// </para>
    /// </summary>
    public static string? HayQueMover(string fichero, string raiz, int temporada, bool enCastellano)
    {
        if (Carpeta(raiz, temporada, enCastellano) is not { } destino) return null;

        var donde = Path.GetDirectoryName(Path.GetFullPath(fichero));
        if (string.IsNullOrEmpty(donde)) return null;

        // Se sube desde el fichero hasta la raíz de la serie buscando una carpeta
        // que YA diga esta temporada. Se mira el nombre y no la ruta exacta por
        // dos motivos: el fichero puede colgar de una subcarpeta suya («extras»),
        // y la carpeta puede estar escrita en el otro idioma.
        var raizPlena = Path.GetFullPath(raiz);
        for (var d = donde; !string.IsNullOrEmpty(d); d = Path.GetDirectoryName(d))
        {
            // «Temporada 03» y «Season 03» son la MISMA temporada. Si no se
            // reconocieran las dos, cambiar el ajuste de idioma convertiría una
            // biblioteca entera en pendiente de reordenar, sin que nada estuviera
            // mal: se moverían cientos de ficheros para dejarlos donde ya estaban.
            if (SignalExtractor.TemporadaDeCarpeta(Path.GetFileName(d)) == temporada)
                return null;

            if (string.Equals(d, raizPlena, StringComparison.OrdinalIgnoreCase)) break;
        }

        return destino;
    }
}
