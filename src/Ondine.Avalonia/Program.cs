using Avalonia;

namespace Ondine.Ava;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Una sola Ondine. Los autochequeos (--auto) abren y cierran ventanas a propósito y no
        // pueden toparse con la que tenga abierta el usuario.
        if (!args.Contains("--auto") && !Instancia.Arrancar(args)) return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        if (!args.Contains("--auto")) return;

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine();
        Console.WriteLine("== El tema portado se aplica de verdad ==");
        foreach (var r in Comprobacion.Resultados) Console.WriteLine("  " + r);

        var fallos = Comprobacion.Resultados.Count(r => r.StartsWith("✗") || r.StartsWith("REVENTO"));
        Console.WriteLine();
        Console.WriteLine($"-- {Comprobacion.Resultados.Count - fallos} pasan | {fallos} fallan --");
        Environment.ExitCode = fallos == 0 ? 0 : 1;
    }

    // Lo llama también el previsualizador del diseñador, por convención de Avalonia.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
