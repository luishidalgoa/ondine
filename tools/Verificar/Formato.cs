namespace Ondine.Verificar;

/// <summary>
/// Paso 4: el formato. Usa «dotnet format whitespace», que es lo que el repo puede exigir hoy sin
/// reformatear el estilo de todo el código de una vez.
/// </summary>
public static class Formato
{
    // Los proyectos que se comprueban en cualquier sistema.
    private static readonly string[] Multiplataforma =
    [
        "src/Ondine.Core/Ondine.Core.csproj",
        "src/Ondine.Mcp/Ondine.Mcp.csproj",
        "src/Ondine.Cli/Ondine.Cli.csproj",
        "src/Ondine.Avalonia/Ondine.Avalonia.csproj",
        "tests/Reindex.Tests/Reindex.Tests.csproj",
        "tools/Verificar/Verificar.csproj",
    ];

    // La interfaz de Windows solo se puede comprobar en Windows: fuera de ahí se dice que se omite.
    private const string Wpf = "src/Ondine/Ondine.csproj";

    public static IReadOnlyList<Hallazgo> Correr(Contexto ctx)
    {
        var hallazgos = new List<Hallazgo>();

        foreach (var proyecto in Multiplataforma)
            hallazgos.Add(Comprobar(ctx, proyecto));

        if (OperatingSystem.IsWindows())
            hallazgos.Add(Comprobar(ctx, Wpf));
        else
            hallazgos.Add(Hallazgo.Omitido($"{Wpf}: solo se comprueba en Windows (en este sistema no se puede)"));

        return hallazgos;
    }

    private static Hallazgo Comprobar(Contexto ctx, string proyecto)
    {
        var r = Proceso.Correr(ctx.Raiz, "dotnet", "format", "whitespace", proyecto, "--verify-no-changes");
        return r.Codigo switch
        {
            0 => Hallazgo.Pasa($"{proyecto}: formato correcto"),
            // dotnet format devuelve 2 cuando encuentra algo que corregir: eso es un fallo de formato.
            2 => Hallazgo.Falla($"{proyecto}: hay espacios por corregir; ejecuta «dotnet format whitespace {proyecto}»"),
            // Cualquier otro código es algo que no se ha podido comprobar: cuenta como fallo.
            _ => Hallazgo.Falla($"{proyecto}: no se pudo comprobar el formato (código {r.Codigo}): {r.Texto.Trim().Split('\n').LastOrDefault()}"),
        };
    }
}
