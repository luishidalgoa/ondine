namespace Ondine.Reindex.Tests;

/// <summary>
/// Cómo cierra un modal de Avalonia, que resultó ser un fallo de los que matan.
///
/// <para>
/// <b>Qué pasaba.</b> Tres ventanas —Preferencias, Renombrar y el explorador del catálogo—
/// cerraban con <c>Close(unObjeto)</c>: los ajustes, la regla de renombrado, el episodio
/// elegido. Y las diez llamadas que las abren en toda la aplicación piden
/// <c>ShowDialog&lt;bool&gt;</c>. Avalonia convierte lo que le des al tipo pedido <b>dentro</b>
/// de <c>Close</c>, así que salta un <c>InvalidCastException</c> —«Unable to cast object of type
/// Ondine.Settings to type System.Boolean»— en el camino del cierre de la ventana, donde <b>no
/// lo recoge nadie</b>.
/// </para>
/// <para>
/// El resultado que ve quien usa la app: cambias el idioma en Preferencias, pulsas Guardar y
/// <b>no cambia nada</b>. Y lo mismo la X y el botón Cancelar, que cerraban con
/// <c>Close(null)</c>: un nulo tampoco se convierte en <c>bool</c>.
/// </para>
/// <para>
/// <b>Por qué se comprueba leyendo el código y no abriendo la ventana.</b> Se intentó: una
/// comprobación de arranque que hace el viaje de vuelta de verdad. Pero la excepción se lanza
/// dentro del bucle de mensajes de Avalonia y desde fuera no se puede recoger, así que el
/// sabotaje no da un fallo legible — se lleva las 186 comprobaciones por delante. Una alarma
/// que apaga la luz no sirve de alarma. Aquí sí se ve, y sin abrir nada.
/// </para>
/// </summary>
public static class CerrarModalesTests
{
    public static void Todas()
    {
        Program.Seccion("Cómo cierran los modales");

        var raiz = LocalizarRaiz();
        var carpeta = Path.Combine(raiz, "src", "Ondine.Avalonia");
        if (!Directory.Exists(carpeta))
        {
            Program.Assert(false, "no encuentro src/Ondine.Avalonia");
            return;
        }

        var ficheros = Directory.GetFiles(carpeta, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        TodosLosModalesSeAbrenPidiendoUnBool(ficheros);
        NadieCierraConAlgoQueNoSeaUnBool(ficheros);
    }

    /// <summary>
    /// La premisa de la que depende todo lo de abajo: que en esta aplicación <b>todos</b> los
    /// modales se abren con <c>ShowDialog&lt;bool&gt;</c>. Si algún día uno pide otro tipo, esto
    /// se planta y hay que decidir a mano — mejor eso que una regla que deja de valer en
    /// silencio.
    /// </summary>
    private static void TodosLosModalesSeAbrenPidiendoUnBool(List<string> ficheros)
    {
        // Sin los comentarios, y no es un detalle: un comentario que escribe «<c>ShowDialog</c>»
        // casa con el patrón y devuelve «/c» como si fuera un tipo. La primera versión de esto
        // se plantó por eso — otra comprobación confundiendo hablar de algo con hacerlo.
        var tipos = ficheros
            .SelectMany(f => File.ReadAllLines(f)
                .Where(l => !l.TrimStart().StartsWith("//"))
                .SelectMany(l => System.Text.RegularExpressions.Regex
                    .Matches(l, @"ShowDialog<([A-Za-z0-9_?]+)>")
                    .Select(m => m.Groups[1].Value)))
            .Distinct()
            .ToList();

        Program.Assert(tipos.Count > 0, $"se encuentran los modales que se abren ({tipos.Count} tipos)");
        Program.Assert(tipos.All(t => t == "bool"),
            $"todos piden un bool, que es lo que hace válida la regla de abajo ({string.Join(", ", tipos)})");
    }

    /// <summary>
    /// Y entonces cerrar solo puede hacerse con <c>true</c> o <c>false</c>. El dato que la
    /// ventana quiera devolver va en una propiedad —<c>Result</c>, <c>Elegido</c>— que quien
    /// abre lee después, que es el patrón que ya usaba <c>Dialogo</c>.
    /// </summary>
    private static void NadieCierraConAlgoQueNoSeaUnBool(List<string> ficheros)
    {
        var malos = new List<string>();

        foreach (var f in ficheros)
        {
            var lineas = File.ReadAllLines(f);
            for (int i = 0; i < lineas.Length; i++)
            {
                var l = lineas[i];
                if (l.TrimStart().StartsWith("//") || l.TrimStart().StartsWith("///")) continue;

                foreach (System.Text.RegularExpressions.Match m in
                         System.Text.RegularExpressions.Regex.Matches(l, @"(?<![A-Za-z_.])Close\(([^)]*)\)"))
                {
                    var arg = m.Groups[1].Value.Trim();
                    if (arg is "" or "true" or "false") continue;
                    malos.Add($"{Path.GetFileName(f)}:{i + 1} → Close({arg})");
                }
            }
        }

        Program.Assert(malos.Count == 0,
            malos.Count == 0
                ? "nadie cierra con nada que no sea true o false: el dato va en una propiedad"
                : $"{malos.Count} cierran con algo que no es un bool y revientan al devolverlo: " +
                  string.Join(" · ", malos));
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src")))
            d = d.Parent;
        return d?.FullName ?? "";
    }
}
