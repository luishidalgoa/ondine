namespace Ondine.Reindex.Tests;

/// <summary>
/// Un nombre de fichero que valga en los tres sistemas, no solo en el que lo escribe.
///
/// <para>
/// <b>El problema, dicho corto:</b> <c>Path.GetInvalidFileNameChars()</c> devuelve nueve
/// caracteres en Windows y <b>dos</b> en Linux —la barra y el nulo—. Así que un limpiador
/// escrito con esa API deja pasar en Linux todo lo que iba a quitar: los dos puntos de «Alien:
/// Covenant», la interrogación de «¿Quién engañó a Roger Rabbit?», el asterisco, las comillas.
/// </para>
/// <para>
/// Y eso importa porque <b>una biblioteca de vídeo casi nunca se queda donde se creó</b>: acaba
/// en un disco compartido, en un NAS o servida a un cliente de Windows. Un «:» colado desde
/// Linux rompe el fichero justo donde se va a ver, y no al crearlo. Además, el mismo fichero
/// visto desde los dos sistemas dejaría de llamarse igual.
/// </para>
/// <para>
/// El motivo ya estaba escrito en <c>LibraryTemplate</c> y en <c>RutaDeSalida</c>, cada uno con
/// su copia de la lista. Los otros dos —el renombrado de comprimir y el guardado del
/// catálogo— seguían con la API del sistema. Ahora la lista está en un solo sitio.
/// </para>
/// </summary>
public static class NombreValidoEnTodasPartesTests
{
    public static void Todas()
    {
        Program.Seccion("Nombres de fichero válidos en los tres sistemas");

        LosNueveSeQuitanSiempre();
        LoQueSiSePuedeQuedarSeQueda();
        NiPuntosNiEspaciosAlFinal();
        LosCuatroSitiosUsanLaMismaLista();
    }

    /// <summary>
    /// Los nueve de Windows, se ejecute esto donde se ejecute. Es el punto: la prueba tiene
    /// que fallar en Linux si alguien vuelve a la API del sistema, no pasar por estar en
    /// Windows.
    /// </summary>
    private static void LosNueveSeQuitanSiempre()
    {
        foreach (var c in new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
        {
            var limpio = NombreDeFichero.Limpiar($"Alien{c}Covenant");
            Program.Assert(!limpio.Contains(c),
                $"«{c}» no sobrevive a la limpieza ({limpio})");
        }

        // Los tres casos que salen de verdad en una biblioteca.
        Program.Assert(!NombreDeFichero.Limpiar("Alien: Covenant").Contains(':'),
            "«Alien: Covenant» sale sin los dos puntos");
        Program.Assert(!NombreDeFichero.Limpiar("¿Quién engañó a Roger Rabbit?").Contains('?'),
            "y una pregunta sin la interrogación de cierre");
        Program.Assert(!NombreDeFichero.Limpiar("AC/DC - Live").Contains('/'),
            "y una barra, que en Linux además partiría la ruta");
    }

    /// <summary>
    /// Y no se pasa de listo. Los acentos, la eñe, los paréntesis y los guiones son legales en
    /// los tres sistemas: quitarlos estropearía el nombre sin motivo.
    /// </summary>
    private static void LoQueSiSePuedeQuedarSeQueda()
    {
        var n = NombreDeFichero.Limpiar("El señor de los anillos (2001) [1080p] - versión extendida");
        foreach (var trozo in new[] { "señor", "(2001)", "[1080p]", "versión", "-" })
            Program.Assert(n.Contains(trozo), $"«{trozo}» se queda tal cual");
    }

    /// <summary>
    /// Un nombre que acaba en punto o en espacio es válido en Linux y <b>no se puede crear en
    /// Windows</b>: el sistema los recorta al escribir, así que el fichero acaba llamándose
    /// distinto de lo que se pidió, y quien lo busque por su nombre no lo encuentra.
    /// </summary>
    private static void NiPuntosNiEspaciosAlFinal()
    {
        Program.Assert(NombreDeFichero.Limpiar("Capítulo 1.") == "Capítulo 1",
            "el punto final se va");
        Program.Assert(NombreDeFichero.Limpiar("Capítulo 1   ") == "Capítulo 1",
            "y los espacios de después también");
    }

    /// <summary>
    /// El guardián de que no vuelvan a separarse: <b>ningún</b> fichero del motor puede llamar
    /// a <c>Path.GetInvalidFileNameChars()</c>. Es la llamada que parece correcta y en Linux no
    /// hace casi nada.
    /// </summary>
    private static void LosCuatroSitiosUsanLaMismaLista()
    {
        var raiz = LocalizarRaiz();
        var culpables = Directory
            .GetFiles(Path.Combine(raiz, "src", "Ondine.Core"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => Path.GetFileName(f) != "NombreDeFichero.cs")
            // Sin los comentarios. La primera versión leía el fichero entero y acusaba a los
            // tres que EXPLICAN por qué no usan esa API — una comprobación que no distingue
            // hablar de algo de hacerlo señala precisamente a quien lo hizo bien.
            .Where(f => File.ReadAllLines(f)
                            .Where(l => !l.TrimStart().StartsWith("//"))
                            .Any(l => l.Contains("GetInvalidFileNameChars()")))
            .Select(Path.GetFileName)
            .ToList();

        Program.Assert(culpables.Count == 0,
            culpables.Count == 0
                ? "nadie más llama a GetInvalidFileNameChars(): en Linux solo prohíbe «/»"
                : $"{culpables.Count} ficheros vuelven a la API del sistema: {string.Join(", ", culpables)}");
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src")))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
