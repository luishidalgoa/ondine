using System.IO;
using System.Text.RegularExpressions;

namespace Ondine.Reindex;

/// <summary>
/// Lee una película de su nombre de fichero: título y año.
///
/// <para>
/// Con las series hay un anexo del que sacar la verdad —una lista ordenada, con
/// números y títulos—. Con las películas no lo hay: <b>una película es solo una
/// película</b>. Así que lo primero y lo único que hay de entrada es el nombre
/// del fichero, y de ahí se saca lo que Plex y Jellyfin necesitan para
/// reconocerla: <c>Título (Año)</c>.
/// </para>
/// <para>
/// La trampa de esto no son los formatos raros de descarga. Es que <b>hay
/// títulos que son un año</b> —«1917», «2001: A Space Odyssey», «Blade Runner
/// 2049»— y la regla ingenua «el último número de cuatro cifras es el año» los
/// parte por la mitad. Las tres reglas de abajo existen por eso, y cada una
/// tiene su prueba.
/// </para>
/// </summary>
public static partial class TituloDePelicula
{
    /// <summary>Lo que se ha podido leer del nombre. <paramref name="Anio"/> es nulo si no había.</summary>
    public sealed record Ficha(string Titulo, int? Anio);

    /// <summary>
    /// El primer año del cine. Antes de esto no hay películas, así que un número
    /// menor no es un año por mucho que tenga cuatro cifras.
    /// </summary>
    private const int PrimerAnio = 1888;

    /// <summary>Un año entre paréntesis o corchetes: la forma en que se escribe a propósito.</summary>
    [GeneratedRegex(@"[\(\[](\d{4})[\)\]]")]
    private static partial Regex RxAnioMarcado();

    /// <summary>Cuatro cifras sueltas, con separador o borde a los lados.</summary>
    [GeneratedRegex(@"(?<![0-9])(\d{4})(?![0-9])")]
    private static partial Regex RxAnioSuelto();

    public static Ficha Leer(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return new("", null);

        var s = Path.GetFileNameWithoutExtension(nombre.Trim());
        if (s.Length == 0) return new("", null);

        // Los puntos y guiones bajos son separadores solo cuando NO hay espacios:
        // ahí el nombre viene de una descarga y «Blade.Runner.1982» son palabras.
        // Con espacios presentes, un punto es puntuación de verdad y sustituirlo
        // rompería títulos como «Dr. Strangelove».
        if (!s.Contains(' ')) s = s.Replace('.', ' ').Replace('_', ' ');

        var (anio, desde) = Anio(s);

        // El título es lo de DELANTE del año. Lo de detrás es siempre resolución,
        // códec y grupo: nunca he visto media palabra de título ahí.
        var titulo = anio is null ? s : s[..desde];

        titulo = SignalExtractor.SinMorralla(titulo);
        titulo = titulo.Trim().Trim('-', '–', '_', '.', ' ', '\t', '(', '[');
        titulo = EspaciosDeMas().Replace(titulo, " ").Trim();

        return new(titulo, anio);
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex EspaciosDeMas();

    /// <summary>
    /// El año de estreno, si lo hay, y dónde empieza —que es donde acaba el título.
    ///
    /// <para>
    /// Manda el que va <b>entre paréntesis o corchetes</b>: eso lo escribió
    /// alguien a propósito para decir «este es el año», y es lo que distingue
    /// «Blade Runner 2049 (2017)» de un título partido por la mitad.
    /// </para>
    /// </summary>
    private static (int? Anio, int Desde) Anio(string s)
    {
        var marcado = RxAnioMarcado().Matches(s).LastOrDefault(m => Posible(m.Groups[1].Value));
        if (marcado is not null)
            return (int.Parse(marcado.Groups[1].Value), marcado.Index);

        // Sin paréntesis, el último que sea posible — y nunca el que abre el
        // nombre: ahí un año es el título («1917», «2001 A Space Odyssey»), porque
        // nadie nombra un fichero empezando por el año de estreno.
        foreach (Match m in RxAnioSuelto().Matches(s).Reverse())
        {
            if (m.Index == 0) continue;
            if (!Posible(m.Groups[1].Value)) continue;
            return (int.Parse(m.Groups[1].Value), m.Index);
        }

        return (null, 0);
    }

    /// <summary>
    /// Si esas cuatro cifras pueden ser un año de estreno.
    ///
    /// <para>
    /// El techo es el año que viene: una película puede estar anunciada, pero un
    /// número más allá de eso es del título, no del estreno. Es lo único que
    /// salva a «Blade Runner 2049» cuando viene sin paréntesis.
    /// </para>
    /// </summary>
    private static bool Posible(string cuatro)
        => int.TryParse(cuatro, out var n) && n >= PrimerAnio && n <= DateTime.Now.Year + 1;

    /// <summary>
    /// El nombre canónico: <c>Título (Año)</c>, que es lo que Plex y Jellyfin
    /// esperan encontrar. Sin año no se pone un paréntesis vacío — eso no ayuda
    /// a ningún escáner y ensucia el nombre.
    /// </summary>
    public static string Canonico(Ficha f)
        => f.Anio is null ? f.Titulo : $"{f.Titulo} ({f.Anio})";
}
