using System.Diagnostics;

namespace Ondine.Verificar;

public enum Nivel { Pasa, Falla, Aviso, Omitido, Info }

/// <summary>Una línea del resultado de una comprobación. Solo <see cref="Nivel.Falla"/> hace fallar el comando.</summary>
public sealed record Hallazgo(Nivel Nivel, string Texto)
{
    public static Hallazgo Pasa(string t) => new(Nivel.Pasa, t);
    public static Hallazgo Falla(string t) => new(Nivel.Falla, t);
    public static Hallazgo Aviso(string t) => new(Nivel.Aviso, t);
    public static Hallazgo Omitido(string t) => new(Nivel.Omitido, t);
    public static Hallazgo Info(string t) => new(Nivel.Info, t);

    public string Simbolo => Nivel switch
    {
        Nivel.Pasa => "✓",
        Nivel.Falla => "✗",
        Nivel.Aviso => "!",
        Nivel.Omitido => "○",
        _ => "·",
    };
}

/// <summary>Dónde está el repositorio y contra qué rama se compara lo cambiado.</summary>
public sealed class Contexto
{
    public Contexto(string raiz, string rama)
    {
        Raiz = raiz;
        Rama = rama;
    }

    public string Raiz { get; }
    public string Rama { get; }

    /// <summary>Ruta absoluta a partir de una relativa a la raíz del repositorio.</summary>
    public string Ruta(string relativa) => Path.Combine(Raiz, relativa.Replace('/', Path.DirectorySeparatorChar));
}

public static class Raiz
{
    /// <summary>Sube desde el directorio actual hasta el que tiene AGENTS.md y src/.</summary>
    public static string Encontrar()
    {
        var d = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (d is not null)
        {
            if (File.Exists(Path.Combine(d.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(d.FullName, "src")))
                return d.FullName;
            d = d.Parent;
        }
        throw new InvalidOperationException("No encuentro la raíz del repositorio (AGENTS.md y src/).");
    }
}

/// <summary>Ejecuta un programa y devuelve su salida. Si no se puede ejecutar, lanza: eso es un fallo.</summary>
public static class Proceso
{
    public readonly record struct Salida(int Codigo, string Texto);

    public static Salida Correr(string raiz, string programa, params string[] argumentos)
    {
        var psi = new ProcessStartInfo(programa)
        {
            WorkingDirectory = raiz,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        foreach (var a in argumentos) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"no se pudo lanzar «{programa}»");
        var salida = p.StandardOutput.ReadToEndAsync();
        var error = p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        return new Salida(p.ExitCode, salida.Result + error.Result);
    }

    /// <summary>Como <see cref="Correr"/>, pero un código distinto de cero es un error.</summary>
    public static string Git(string raiz, params string[] argumentos)
    {
        var r = Correr(raiz, "git", argumentos);
        if (r.Codigo != 0) throw new InvalidOperationException($"git {string.Join(' ', argumentos)} falló: {r.Texto.Trim()}");
        return r.Texto;
    }
}
