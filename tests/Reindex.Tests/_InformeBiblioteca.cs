using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Pasa el lector de películas por una biblioteca DE VERDAD y enseña qué saca de
/// cada fichero. <b>No escribe nada, solo lee.</b>
///
/// <para>
/// No forma parte de la tanda —depende de un disco concreto, y las pruebas no
/// pueden—: se lanza a mano.
/// </para>
/// <code>dotnet run --project tests/Reindex.Tests -- --informe "C:uta\Movies"</code>
/// <para>
/// Existe porque las pruebas inventadas no ven lo que hay ahí fuera. Este arnés
/// encontró, en la primera pasada sobre 75 películas: que el 71 % vivía en
/// carpetas de colección que el destino habría explotado, que «(Frank Darabont,
/// 1994)» dejaba al director dentro del título, y que 52 de 75 ficheros no traen
/// año pero su carpeta sí. Ninguna de las tres se me habría ocurrido sentado.
/// Lo que encuentre se convierte en prueba con el nombre real dentro, que ya no
/// depende de ningún disco.
/// </para>
/// </summary>
public static class InformeBiblioteca
{
    public static void Correr(string raiz)
    {
        var extensiones = new[] { ".mkv", ".mp4", ".avi", ".m4v" };
        var ficheros = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Where(f => extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"── {ficheros.Count} ficheros en {raiz}\n");

        int conAnio = 0, sinAnio = 0;
        var carpetas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in ficheros)
        {
            var carpeta = Path.GetFileName(Path.GetDirectoryName(f)) ?? "";
            carpetas[carpeta] = carpetas.GetValueOrDefault(carpeta) + 1;

            var ficha = TituloDePelicula.Leer(Path.GetFileName(f));
            if (ficha.Anio is null) sinAnio++; else conAnio++;

            var destino = DestinoDePelicula.HayQueMover(f, raiz);
            var adonde = destino is null ? "(ya está)" : Path.GetFileName(Path.GetDirectoryName(destino));

            Console.WriteLine($"{carpeta,-28} | {Path.GetFileName(f),-58} | {ficha.Titulo,-46} | {ficha.Anio?.ToString() ?? "—",-6} | {adonde}");
        }

        Console.WriteLine($"\ncon año: {conAnio}   ·   SIN año: {sinAnio}");
        Console.WriteLine("\n── carpetas con más de un fichero (¿colecciones?) ──");
        foreach (var (c, n) in carpetas.Where(x => x.Value > 1).OrderByDescending(x => x.Value))
            Console.WriteLine($"  {n,3}  {c}");
    }
}
