namespace Ondine.Verificar;

/// <summary>
/// Paso 1: la batería de pruebas completa, con la misma configuración que el CI.
///
/// <para>
/// Pasa solo si el proceso sale con código 0 <b>y</b> su resumen dice «0 fallan». Con las dos
/// condiciones, una batería que se corta a mitad o que cambia su formato de salida no puede pasar
/// en silencio. Si falla, se listan todas las pruebas que fallan, no solo las últimas líneas.
/// </para>
/// </summary>
public static class Pruebas
{
    public static IReadOnlyList<Hallazgo> Correr(Contexto ctx)
    {
        var r = Proceso.Correr(ctx.Raiz, "dotnet", "run", "--project", "tests/Reindex.Tests/Reindex.Tests.csproj", "-c", "Release");
        var lineas = r.Texto.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToList();

        // El resumen lo imprime Program.cs: «── N pasan · M fallan ──». Si no aparece, algo ha
        // cortado la batería antes de tiempo, y eso también es un fallo.
        var resumen = lineas.LastOrDefault(l => l.Contains(" pasan ") && l.Contains(" fallan "));

        if (r.Codigo == 0 && resumen is not null && resumen.Contains("· 0 fallan"))
            return [Hallazgo.Pasa(resumen.Trim('─', ' '))];

        var hallazgos = new List<Hallazgo>();
        if (resumen is null)
            hallazgos.Add(Hallazgo.Falla($"la batería acaba sin su resumen (código {r.Codigo}): algo la ha cortado antes de tiempo"));
        else
            hallazgos.Add(Hallazgo.Falla($"{resumen.Trim('─', ' ')} (código {r.Codigo})"));

        // Cada prueba que falla imprime su línea con ✗, con lo esperado y lo obtenido si lo hay.
        foreach (var l in lineas.Where(l => l.TrimStart().StartsWith('✗')))
            hallazgos.Add(Hallazgo.Falla(l.Trim()));

        if (hallazgos.Count == 1)
            hallazgos.AddRange(lineas.TakeLast(15).Select(l => Hallazgo.Info(l)));

        return hallazgos;
    }
}
