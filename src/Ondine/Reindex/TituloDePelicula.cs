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

    /// <summary>
    /// Un año entre paréntesis o corchetes: la forma en que se escribe a
    /// propósito. Admite texto delante dentro del mismo paréntesis porque en una
    /// biblioteca real aparece «(Frank Darabont, 1994)», y exigir el año pegado
    /// al paréntesis dejaba al director dentro del título.
    /// </summary>
    [GeneratedRegex(@"[\(\[][^\)\]]*?(\d{4})[\)\]]")]
    private static partial Regex RxAnioMarcado();

    /// <summary>Cuatro cifras sueltas, con separador o borde a los lados.</summary>
    [GeneratedRegex(@"(?<![0-9])(\d{4})(?![0-9])")]
    private static partial Regex RxAnioSuelto();

    /// <summary>
    /// Igual, pero mirando también el nombre de la <b>carpeta</b> que lo contiene
    /// cuando el fichero no trae año.
    ///
    /// <para>
    /// Medido sobre una biblioteca real de 75 películas: <b>52 no traen año en el
    /// nombre del fichero</b>, y en buena parte de esos casos la carpeta sí lo
    /// tiene —«Grease (1978)/Grease.mp4»—. Leyendo solo el fichero se proponía
    /// «Grease/Grease.mp4», es decir, <b>tirar un año que ya estaba escrito</b>.
    /// </para>
    /// <para>
    /// El del fichero manda siempre que exista: es el que habla de ESTE fichero,
    /// mientras que la carpeta puede contener varias películas.
    /// </para>
    /// </summary>
    public static Ficha Leer(string nombre, string? carpeta)
    {
        var f = Leer(nombre);
        if (f.Anio is not null || string.IsNullOrWhiteSpace(carpeta)) return f;

        var deLaCarpeta = Leer(carpeta);
        return deLaCarpeta.Anio is null ? f : f with { Anio = deLaCarpeta.Anio };
    }

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

        // La marca de parte no es parte del título: «… (1939) cd1» es «…», parte 1.
        titulo = RxParte().Replace(titulo, "");
        titulo = SignalExtractor.SinMorralla(titulo);
        titulo = titulo.Trim().Trim('-', '–', '_', '.', ' ', '\t', '(', '[');
        titulo = EspaciosDeMas().Replace(titulo, " ").Trim();

        return new(titulo, anio);
    }

    /// <summary>
    /// Las palabras que delatan que un año es el de una reedición y no el del
    /// estreno.
    /// </summary>
    [GeneratedRegex(@"remaster|restaur|restored|reedici|anniversar|aniversar|edition|edici",
                    RegexOptions.IgnoreCase)]
    private static partial Regex RxReedicion();

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
        {
            // Salvo que por el medio haya una marca de REEDICIÓN. «Alien 1979
            // REMASTERED (2003)»: el de los paréntesis es el año del remaster y
            // el de estreno es el otro. Se exige la marca a propósito — sin ella,
            // un número anterior suele ser del título («The 1900 House (1999)») y
            // preferirlo rompería más de lo que arregla.
            foreach (Match antes in RxAnioSuelto().Matches(s))
            {
                if (antes.Index >= marcado.Index || antes.Index == 0) continue;
                if (!Posible(antes.Groups[1].Value)) continue;
                var enMedio = s[(antes.Index + antes.Length)..marcado.Index];
                if (RxReedicion().IsMatch(enMedio))
                    return (int.Parse(antes.Groups[1].Value), antes.Index);
            }

            return (int.Parse(marcado.Groups[1].Value), marcado.Index);
        }

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
    /// Los sufijos con los que Plex y Jellyfin marcan lo que <b>no es la
    /// película</b>: tráilers, tomas falsas, escenas eliminadas.
    ///
    /// <para>
    /// Reconocerlos importa más de lo que parece. En una carpeta ya bien puesta,
    /// con la película en <c>.mkv</c> y el extra en <c>.mp4</c>, renombrar el
    /// extra a «Título (Año).mp4» hace que el servidor lo lea como una
    /// <b>segunda versión de la película</b>: el extra desaparece de donde debía
    /// estar y aparece donde no debe.
    /// </para>
    /// </summary>
    private static readonly string[] Extras =
    {
        "trailer", "behindthescenes", "deleted", "deletedscene", "deletedscenes",
        "featurette", "interview", "scene", "short", "other", "sample", "clip", "extra",
    };

    /// <summary>Si este fichero es un extra y no la película.</summary>
    public static bool EsExtra(string nombre)
    {
        var s = Path.GetFileNameWithoutExtension(nombre ?? "").Trim();
        if (s.Length == 0) return false;

        // El nombre entero, que es como salen de algunos programas: «sample.mkv».
        if (Extras.Contains(s, StringComparer.OrdinalIgnoreCase)) return true;

        // O al final, tras uno de los separadores que se usan para marcarlo. Se
        // exige el separador a propósito: «The Sample Man» lleva la palabra
        // dentro y es una película como otra cualquiera.
        foreach (var e in Extras)
            foreach (var sep in new[] { "-", ".", "_", " " })
                if (s.EndsWith(sep + e, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>
    /// Qué parte de una película partida es este fichero, o <c>null</c> si es
    /// entera.
    ///
    /// <para>
    /// Se exige una <b>palabra</b> delante del número —«cd», «part», «disc»,
    /// «disco», «pt»—, nunca un número suelto: «Cars 2» es una película, no la
    /// segunda parte de «Cars».
    /// </para>
    /// </summary>
    public static int? Parte(string nombre)
    {
        var s = Path.GetFileNameWithoutExtension(nombre ?? "");
        var m = RxParte().Match(s);
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n is > 0 and < 10 ? n : null;
    }

    [GeneratedRegex(@"(?:^|[\s._\-])(?:cd|dvd|part|pt|disc|disk|disco|parte)[\s._\-]?(\d)\s*$",
                    RegexOptions.IgnoreCase)]
    private static partial Regex RxParte();

    /// <summary>
    /// El nombre canónico: <c>Título (Año)</c>, que es lo que Plex y Jellyfin
    /// esperan encontrar. Sin año no se pone un paréntesis vacío — eso no ayuda
    /// a ningún escáner y ensucia el nombre.
    /// </summary>
    public static string Canonico(Ficha f)
        => f.Anio is null ? f.Titulo : $"{f.Titulo} ({f.Anio})";
}
