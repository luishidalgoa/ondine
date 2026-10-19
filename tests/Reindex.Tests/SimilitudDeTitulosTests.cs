namespace Ondine.Reindex.Tests;

/// <summary>
/// La similitud de títulos, contra la implementación de referencia.
///
/// <para>
/// <c>TitleMatch.Sim</c> se reescribió sin reservar memoria para que «Analizar» no tarde
/// medio minuto en una serie larga. Los umbrales (0,78 / 0,86) están calibrados contra el
/// <c>difflib</c> original, así que la reescritura solo vale si da <b>exactamente</b> los
/// mismos números: aquí se guarda la versión lenta y literal, y se compara con ella sobre
/// pares generados, incluidos los que tienen letras repetidas, que es donde se notaría un
/// desempate distinto.
/// </para>
/// </summary>
public static class SimilitudDeTitulosTests
{
    public static void Todas()
    {
        Program.Seccion("Similitud de títulos: la rápida da lo mismo que la de referencia");

        ConocidosDanLoQueDifflib();
        AleatoriosCoincidenConLaDeReferencia();
    }

    private static void ConocidosDanLoQueDifflib()
    {
        // Valores de difflib.SequenceMatcher(None, a, b).ratio().
        Program.Assert(TitleMatch.Sim("abcd", "abcd") == 1.0, "iguales: 1");
        Program.Assert(TitleMatch.Sim("", "") == 1.0, "dos vacías: 1");
        Program.Assert(TitleMatch.Sim("abc", "") == 0.0, "una vacía: 0");
        Program.Assert(Math.Abs(TitleMatch.Sim("abcd", "bcde") - 0.75) < 1e-12, "abcd/bcde = 0,75");
        Program.Assert(Math.Abs(TitleMatch.Sim("private", "pirate") - 2.0 * 5 / 13) < 1e-12,
            "private/pirate = 10/13 (el 0,769 de difflib)");
    }

    private static void AleatoriosCoincidenConLaDeReferencia()
    {
        // Alfabeto corto a propósito: con pocas letras hay muchísimos empates de bloque, y
        // el desempate es justo lo que podría haberse roto.
        var rnd = new Random(20260819);
        const string alfabeto = "abc de";
        int distintos = 0, pares = 6000;

        string Texto(int max)
        {
            int n = rnd.Next(0, max + 1);
            var c = new char[n];
            for (int i = 0; i < n; i++) c[i] = alfabeto[rnd.Next(alfabeto.Length)];
            return new string(c);
        }

        for (int n = 0; n < pares; n++)
        {
            // Hasta 300: cruza el umbral de la pila y prueba también la ruta de textos largos.
            var a = Texto(n % 50 == 0 ? 300 : 40);
            var b = Texto(n % 50 == 0 ? 300 : 40);
            if (Referencia(a, b) != TitleMatch.Sim(a, b)) distintos++;
        }

        Program.Assert(distintos == 0,
            $"{pares} pares aleatorios dan el mismo número que la referencia ({distintos} distintos)");
    }

    // ── la implementación de referencia: la de antes, con diccionarios, tal cual ──

    private static double Referencia(string a, string b)
    {
        int total = a.Length + b.Length;
        if (total == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;
        if (string.Equals(a, b, StringComparison.Ordinal)) return 1.0;

        var b2j = new Dictionary<char, List<int>>();
        for (int j = 0; j < b.Length; j++)
        {
            if (!b2j.TryGetValue(b[j], out var lista)) b2j[b[j]] = lista = new List<int>();
            lista.Add(j);
        }
        return 2.0 * Suma(a, b, b2j, 0, a.Length, 0, b.Length) / total;
    }

    private static int Suma(string a, string b, Dictionary<char, List<int>> b2j,
                            int alo, int ahi, int blo, int bhi)
    {
        if (alo >= ahi || blo >= bhi) return 0;
        var (i, j, size) = Mas(a, b2j, alo, ahi, blo, bhi);
        if (size == 0) return 0;
        return size + Suma(a, b, b2j, alo, i, blo, j) + Suma(a, b, b2j, i + size, ahi, j + size, bhi);
    }

    private static (int i, int j, int size) Mas(string a, Dictionary<char, List<int>> b2j,
                                                int alo, int ahi, int blo, int bhi)
    {
        int besti = alo, bestj = blo, bestsize = 0;
        var j2len = new Dictionary<int, int>();
        for (int i = alo; i < ahi; i++)
        {
            var nuevo = new Dictionary<int, int>();
            if (b2j.TryGetValue(a[i], out var posiciones))
                foreach (var j in posiciones)
                {
                    if (j < blo) continue;
                    if (j >= bhi) break;
                    int k = (j2len.TryGetValue(j - 1, out var prev) ? prev : 0) + 1;
                    nuevo[j] = k;
                    if (k > bestsize) { besti = i - k + 1; bestj = j - k + 1; bestsize = k; }
                }
            j2len = nuevo;
        }
        return (besti, bestj, bestsize);
    }
}
