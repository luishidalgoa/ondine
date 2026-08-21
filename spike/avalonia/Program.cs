using Avalonia;

namespace Ondine.Spike;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        if (!args.Contains("--auto")) return;

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine();
        Console.WriteLine("== PRUEBA DE FUEGO 1: el DataGrid de Organizar ==");
        foreach (var r in Comprobacion.Resultados) Console.WriteLine("  " + r);

        if (ComprobacionVideo.Resultados.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("== PRUEBA DE FUEGO 2: el video con LibVLC ==");
            foreach (var r in ComprobacionVideo.Resultados) Console.WriteLine("  " + r);
        }

        var todo = Comprobacion.Resultados.Concat(ComprobacionVideo.Resultados).ToList();
        var fallos = todo.Count(r => r.StartsWith("✗") || r.StartsWith("REVENTO"));
        Console.WriteLine();
        Console.WriteLine($"-- {todo.Count(r => r.StartsWith("✓")) } pasan | {fallos} fallan --");
        Environment.ExitCode = fallos == 0 ? 0 : 1;
    }

    // Lo llama también el previsualizador del diseñador, por convención de Avalonia.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
