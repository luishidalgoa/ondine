// Harness de verificación de Ondine. Un único comando decide si un cambio es aceptable:
//
//   dotnet run --project tools/Verificar                  (compara con origin/main)
//   dotnet run --project tools/Verificar -- --base origin/main
//
// Ejecuta, en este orden: 1) la batería de pruebas, 2) las reglas de capas entre proyectos,
// 3) la coherencia del mapa de funcionalidades con el código, 4) el formato.
//
// Reglas del harness:
//   - Una comprobación que falla hace fallar el comando (código de salida 1).
//   - Un error inesperado dentro de una comprobación cuenta como fallo, nunca como paso.
//   - Un aviso no hace fallar el comando; se muestra para que alguien lo mire.
//   - Lo que no se puede comprobar en este sistema se marca como OMITIDO, nunca como pasado.

using Ondine.Verificar;

var base_ = "origin/main";
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--base" && i + 1 < args.Length) base_ = args[++i];
    else if (args[i] == "--help") { Console.WriteLine("Uso: dotnet run --project tools/Verificar -- [--base <rama>]"); return 0; }
}

var raiz = Raiz.Encontrar();
var contexto = new Contexto(raiz, base_);

var pasos = new (string Nombre, Func<Contexto, IReadOnlyList<Hallazgo>> Correr)[]
{
    ("1. Pruebas", Pruebas.Correr),
    ("2. Capas entre proyectos", Capas.Correr),
    ("3. Mapa de funcionalidades", Mapa.Correr),
    ("4. Formato", Formato.Correr),
};

var fallos = 0;
foreach (var (nombre, correr) in pasos)
{
    Console.WriteLine();
    Console.WriteLine($"── {nombre} ──");
    IReadOnlyList<Hallazgo> hallazgos;
    try
    {
        hallazgos = correr(contexto);
    }
    catch (Exception ex)
    {
        // Lo inesperado cuenta como fallo: nunca se traga para que el paso parezca limpio.
        hallazgos = [Hallazgo.Falla($"error inesperado: {ex.GetType().Name}: {ex.Message}")];
    }

    foreach (var h in hallazgos)
    {
        Console.WriteLine($"  {h.Simbolo} {h.Texto}");
        if (h.Nivel == Nivel.Falla) fallos++;
    }
    if (hallazgos.Count == 0) Console.WriteLine("  ✓ sin nada que decir");
}

Console.WriteLine();
Console.WriteLine(fallos == 0
    ? "VERIFICACIÓN APROBADA: el cambio es aceptable."
    : $"VERIFICACIÓN RECHAZADA: {fallos} comprobación(es) fallan.");
return fallos == 0 ? 0 : 1;
