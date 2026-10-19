using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ondine.Reindex;

/// <summary>
/// Normalización y similitud de títulos. Las funciones son un port LITERAL de las de los
/// scripts originales: los umbrales (0,78 / 0,86) están calibrados contra ELLAS, así que
/// «mejorar» el algoritmo descalibra los umbrales y rompe la identificación.
/// </summary>
public static partial class TitleMatch
{
    /// <summary>Match fuerte por título completo.</summary>
    public const double UmbralTitulo = 0.78;
    /// <summary>Segmentos sueltos: cadenas más cortas ⇒ más ruido ⇒ umbral más alto.</summary>
    public const double UmbralSegmento = 0.86;

    // Sufijos de doblaje: «(España)», «(Hispanoamérica)», «(segundo doblaje)»… no distinguen
    // episodios, solo versiones — quitarlos ANTES de comparar.
    [GeneratedRegex(@"\(\s*(?:espa[nñ]a|hispanoam[eé]rica|latinoam[eé]rica|latino|castellano)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RxDoblajePais();

    [GeneratedRegex(@"\([^)]*doblaje[^)]*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RxDoblaje();

    /// <summary>
    /// «primera parte», «parte segunda», «2 parte»… — todas las formas de numerar una
    /// parte, en cualquiera de los dos órdenes. Se aplica sobre el texto YA limpio
    /// (minúsculas, sin diacríticos, espacios simples), así que aquí «2.ª parte» ya
    /// llega como «2 parte» y no hace falta un patrón para el marcador ordinal.
    /// </summary>
    [GeneratedRegex(@"\b(?:(\w+) (parte|part)|(parte|part) (\w+))\b", RegexOptions.CultureInvariant)]
    private static partial Regex RxParte();

    /// <summary>
    /// Cómo se dice cada número cuando acompaña a «parte». Solo hasta cinco: por encima
    /// de eso ya nadie escribe la palabra, y cuanto más larga la lista más probable es
    /// pisar una palabra normal del título.
    /// </summary>
    private static readonly Dictionary<string, string> OrdinalesDeParte = new(StringComparer.Ordinal)
    {
        ["1"] = "1", ["2"] = "2", ["3"] = "3", ["4"] = "4", ["5"] = "5",
        ["primera"] = "1", ["segunda"] = "2", ["tercera"] = "3", ["cuarta"] = "4", ["quinta"] = "5",
        ["primero"] = "1", ["segundo"] = "2", ["tercero"] = "3", ["cuarto"] = "4", ["quinto"] = "5",
        ["first"] = "1", ["second"] = "2", ["third"] = "3", ["fourth"] = "4", ["fifth"] = "5",
        ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4", ["five"] = "5",
        ["i"] = "1", ["ii"] = "2", ["iii"] = "3", ["iv"] = "4", ["v"] = "5",
    };

    /// <summary>
    /// norm(s): quita sufijos de doblaje → minúsculas → sin diacríticos (á→a, ñ→n) →
    /// todo lo que no sea [a-z0-9] pasa a espacio, colapsado y recortado.
    /// </summary>
    public static string Norm(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        // 1. sufijos de doblaje (antes de tocar mayúsculas: el patrón ya es insensible)
        s = RxDoblajePais().Replace(s, " ");
        s = RxDoblaje().Replace(s, " ");

        // 2. minúsculas + 3. descomponer y tirar diacríticos
        var descompuesto = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        // 4. colapsar todo lo no alfanumérico a un solo espacio
        var sb = new StringBuilder(descompuesto.Length);
        bool espaciopendiente = false;
        foreach (var ch in descompuesto)
        {
            // los diacríticos quedan sueltos tras NFD: se descartan (ñ→n, á→a)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;

            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (espaciopendiente && sb.Length > 0) sb.Append(' ');
                espaciopendiente = false;
                sb.Append(ch);
            }
            else espaciopendiente = true;   // trim implícito: no se emite si no viene nada detrás
        }

        return CanonizarPartes(sb.ToString());
    }

    /// <summary>
    /// Deja todas las formas de numerar una parte en una sola: <c>parte 2</c>.
    ///
    /// <para>
    /// «segunda parte», «parte segunda», «2.ª parte» y «parte 2» son lo mismo dicho de
    /// cuatro maneras, y hasta ahora no lo eran para la comparación. El caso que lo
    /// destapó: un catálogo que escribe «Hola, marciano (primera parte)» y «(segunda
    /// parte)» frente a unos ficheros que dicen «(parte 1)» y «(parte 2)». El dígito
    /// competía contra la palabra, así que no aportaba nada y los DOS ficheros puntuaban
    /// 0,792 contra los DOS episodios. Empate perfecto: ganaba siempre el primero y el
    /// otro caía en conflicto como si fuera un duplicado.
    /// </para>
    /// <para>
    /// Se canoniza al dígito y no a la palabra porque el dígito no tiene género -«parte
    /// segunda» y «segundo capítulo» no comparten forma- ni idioma.
    /// </para>
    /// <para>
    /// Solo se toca la palabra que va PEGADA a «parte»: «La primera vez» es un título,
    /// no una numeración, y ahí «primera» se queda como está.
    /// </para>
    /// </summary>
    private static string CanonizarPartes(string limpio) =>
        RxParte().Replace(limpio, m =>
        {
            // Dos formas: «<numero> parte» o «parte <numero>». Solo una de las dos casa.
            var (numero, palabra) = m.Groups[1].Success
                ? (m.Groups[1].Value, m.Groups[2].Value)
                : (m.Groups[4].Value, m.Groups[3].Value);

            return OrdinalesDeParte.TryGetValue(numero, out var digito)
                ? $"{palabra} {digito}"
                : m.Value;   // no es un número: era una palabra normal del título
        });

    /// <summary>
    /// Similitud 0..1 entre dos cadenas YA normalizadas, equivalente a
    /// <c>difflib.SequenceMatcher(None, a, b).ratio()</c> de Python (Ratcliff-Obershelp):
    /// 2·M/T, con M = suma de los bloques coincidentes hallados recursivamente.
    ///
    /// <para>
    /// <b>Sin reservar memoria.</b> La primera versión armaba el «b2j» de difflib -un
    /// diccionario de listas- en cada llamada y, dentro, un diccionario nuevo por cada
    /// carácter de «a». Una simulación llama a esto millones de veces (cada fichero contra
    /// cada episodio del catálogo): sobre los 1.413 ficheros de una serie larga eran ~30 s,
    /// casi todos en el recolector de basura. Ahora el mismo cálculo corre sobre dos filas
    /// de enteros en la pila. El resultado es EL MISMO, bit a bit —misma búsqueda, mismo
    /// desempate—: lo vigila <c>TitleMatchTests</c> contra la implementación de referencia.
    /// </para>
    /// </summary>
    public static double Sim(string a, string b)
    {
        int total = a.Length + b.Length;
        if (total == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;
        if (string.Equals(a, b, StringComparison.Ordinal)) return 1.0;

        // Dos filas de la tabla de bloques (la anterior y la actual). Un título cabe de
        // sobra en la pila; si alguien pasa un texto largo se pide a la memoria normal.
        int hueco = 2 * (b.Length + 1);
        Span<int> filas = hueco <= 512 ? stackalloc int[hueco] : new int[hueco];

        int coincidentes = SumaBloques(a, b, filas, 0, a.Length, 0, b.Length);
        return 2.0 * coincidentes / total;
    }

    /// <summary>Similitud sobre las cadenas sin normalizar (las normaliza ella).</summary>
    public static double SimRaw(string a, string b) => Sim(Norm(a), Norm(b));

    /// <summary>
    /// Suma recursiva de los tamaños de los bloques coincidentes: se busca el bloque común
    /// más largo y se repite a izquierda y derecha, igual que get_matching_blocks.
    /// </summary>
    private static int SumaBloques(string a, string b, Span<int> filas,
                                   int alo, int ahi, int blo, int bhi)
    {
        if (alo >= ahi || blo >= bhi) return 0;
        var (i, j, size) = BloqueMasLargo(a, b, filas, alo, ahi, blo, bhi);
        if (size == 0) return 0;
        return size
             + SumaBloques(a, b, filas, alo, i, blo, j)
             + SumaBloques(a, b, filas, i + size, ahi, j + size, bhi);
    }

    /// <summary>
    /// El bloque coincidente más largo del rango. Ante empate gana el que empiece antes en
    /// «a», y luego el que empiece antes en «b» — el mismo desempate que difflib, del que
    /// dependen los valores exactos con los que se calibraron los umbrales.
    ///
    /// <para>
    /// <c>anterior[x]</c> es la longitud del bloque que termina en a[i-1] y b[blo+x-1]; la
    /// posición 0 es «antes del rango» y vale siempre 0. Es el «j2len» de difflib, pero en
    /// una fila en vez de un diccionario: recorriendo «i» hacia delante y «j» hacia delante
    /// y quedándose con el primero que estrictamente mejora, se visitan los pares en el
    /// mismo orden que él y el desempate sale igual.
    /// </para>
    /// </summary>
    private static (int i, int j, int size) BloqueMasLargo(string a, string b, Span<int> filas,
                                                          int alo, int ahi, int blo, int bhi)
    {
        int ancho = bhi - blo + 1;
        var anterior = filas.Slice(0, ancho);
        var actual = filas.Slice(filas.Length / 2, ancho);
        anterior.Clear();
        actual.Clear();

        int besti = alo, bestj = blo, bestsize = 0;
        for (int i = alo; i < ahi; i++)
        {
            char ca = a[i];
            for (int j = blo; j < bhi; j++)
            {
                int x = j - blo + 1;
                if (b[j] != ca) { actual[x] = 0; continue; }

                int k = anterior[x - 1] + 1;
                actual[x] = k;
                if (k > bestsize) { besti = i - k + 1; bestj = j - k + 1; bestsize = k; }
            }
            var cambio = anterior; anterior = actual; actual = cambio;
        }
        return (besti, bestj, bestsize);
    }

    /// <summary>
    /// Cota SUPERIOR barata de <see cref="Sim"/>: ningún bloque puede casar caracteres que
    /// no estén en ambas cadenas. Sirve para descartar en O(n) los pares imposibles antes de
    /// pagar el O(n·m) real — sin ella, 600 ficheros × 1800 episodios no termina nunca.
    /// </summary>
    public static double CotaSuperior(in TitleBag a, in TitleBag b)
    {
        int total = a.Length + b.Length;
        if (total == 0) return 1.0;
        return 2.0 * a.Comunes(in b) / total;
    }
}

/// <summary>
/// Un título normalizado con su recuento de caracteres precalculado, para poder descartar
/// comparaciones sin recorrer las cadenas otra vez.
/// </summary>
public readonly struct TitleBag
{
    // 26 letras + 10 dígitos + espacio
    private const int Ranuras = 37;
    private readonly int[] _cuenta;
    public string Text { get; }
    public int Length => Text.Length;

    public TitleBag(string normalizado)
    {
        Text = normalizado ?? "";
        _cuenta = new int[Ranuras];
        foreach (var ch in Text)
        {
            int idx = Indice(ch);
            if (idx >= 0) _cuenta[idx]++;
        }
    }

    private static int Indice(char ch) => ch switch
    {
        >= 'a' and <= 'z' => ch - 'a',
        >= '0' and <= '9' => 26 + (ch - '0'),
        ' ' => 36,
        _ => -1,
    };

    /// <summary>Caracteres que ambas cadenas comparten (intersección de multiconjuntos).</summary>
    public int Comunes(in TitleBag otro)
    {
        int total = 0;
        for (int i = 0; i < Ranuras; i++) total += Math.Min(_cuenta[i], otro._cuenta[i]);
        return total;
    }

    public static TitleBag From(string sinNormalizar) => new(TitleMatch.Norm(sinNormalizar));
}
