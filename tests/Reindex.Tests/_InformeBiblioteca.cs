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
/// <code>dotnet run --project tests/Reindex.Tests -- --informe RUTA</code>
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

        var plan = PlanDePeliculas.Montar(ficheros, raiz);
        foreach (var paso in plan)
        {
            var de = Path.GetRelativePath(raiz, paso.Origen);
            var a = paso.Destino is null ? "" : Path.GetRelativePath(raiz, paso.Destino);
            if (paso.Motivo == PlanDePeliculas.Porque.YaEsta) continue;
            Console.WriteLine($"{paso.Motivo,-12} | {de,-62} | {a}");
        }

        Console.WriteLine();
        foreach (var g in plan.GroupBy(p => p.Motivo).OrderByDescending(g => g.Count()))
            Console.WriteLine($"  {g.Count(),3}  {g.Key}");

        // Lo que de verdad importa comprobar: que NADIE sale de una carpeta de
        // coleccion. Si esto no es cero, la regla no esta funcionando.
        var salen = plan.Count(p => p.Destino is not null
            && !string.Equals(Path.GetDirectoryName(p.Origen), Path.GetDirectoryName(p.Destino),
                              StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"\n  cambian de carpeta: {salen}");
    }
}
