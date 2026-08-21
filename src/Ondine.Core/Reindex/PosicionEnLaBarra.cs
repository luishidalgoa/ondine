namespace Ondine;

/// <summary>
/// A qué segundo corresponde una x sobre una barra de posición.
///
/// <para>
/// Parece una regla de tres y no lo es: <b>el recorrido útil de un deslizador no es todo su
/// ancho</b>. Empieza y acaba a medio tirador de los bordes, porque el tirador tiene que
/// caber. Calcularlo a ojo —<c>x / ancho * duración</c>— da una cuenta distinta de la que
/// hace el propio control al colocarse, y entonces <b>el globo de la previa dice una hora y
/// el clic te lleva a otra</b>: hasta unos diez segundos antes en la primera mitad de un
/// capítulo de media hora. Se ve como «pincho y se va para atrás».
/// </para>
/// <para>
/// Está en el motor porque las dos interfaces tienen la misma barra con el mismo globo, y
/// esto es aritmética: reescribirla en cada una es reescribir el mismo error.
/// </para>
/// </summary>
public static class PosicionEnLaBarra
{
    /// <summary>
    /// El segundo al que apunta <paramref name="x"/>. El recorrido va de medio tirador a
    /// ancho menos medio tirador, igual que hace el control.
    /// </summary>
    public static double SegundosDeX(double x, double ancho, double pulgar, double maximo)
    {
        double util = Math.Max(1, ancho - pulgar);
        return Math.Clamp((x - pulgar / 2) / util, 0, 1) * maximo;
    }
}
