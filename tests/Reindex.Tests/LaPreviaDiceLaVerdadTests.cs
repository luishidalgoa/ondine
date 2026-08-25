namespace Ondine.Reindex.Tests;

/// <summary>
/// La vista previa codifica siempre lo más rápido posible, y eso hay que DECIRLO.
///
/// <para>
/// <b>Es a propósito.</b> La previa existe para mirar diez segundos y juzgar cómo va a quedar la
/// imagen; si respetara un «Esmero: muy lento» habría que esperar minutos para ver diez segundos,
/// que es justo lo que la previa viene a evitar. Además va más rápido que la opción más rápida
/// que el usuario puede elegir —<c>ultrafast</c> en x264/x265, <c>preset 12</c> en SVT-AV1—, con
/// banderas de tiempo real que no están en la escala de Esmero.
/// </para>
/// <para>
/// <b>Pero el globo de ayuda decía «con los ajustes actuales»</b>, y el Esmero es uno de ellos.
/// Un ajuste que la pantalla promete aplicar y no aplica es la misma familia de fallo que el
/// «Sin tocar» del audio: no da error, y solo se descubre comparando resultados. Aquí no se
/// cambia el comportamiento —la previa rápida está bien— se cambia lo que se promete.
/// </para>
/// </summary>
public static class LaPreviaDiceLaVerdadTests
{
    public static void Todas()
    {
        Program.Seccion("La vista previa dice lo que hace");

        ElGloboNoPrometeElEsmero();
        LaAyudaLoExplica();
        LasDosInterfacesEnsenanElGlobo();
    }

    /// <summary>
    /// El globo del botón nombra el Esmero para decir que NO se aplica. Nombrarlo importa: es
    /// como se llama el control en la pantalla, así que es lo que el usuario busca cuando la
    /// previa y el resultado no le cuadran.
    /// </summary>
    private static void ElGloboNoPrometeElEsmero()
    {
        foreach (var idioma in new[] { "es", "en" })
        {
            var antes = Ondine.Localizacion.Idioma.Actual;
            try
            {
                Ondine.Localizacion.Idioma.Actual = idioma;
                var t = Ondine.Localizacion.Textos.Instancia;

                var globo = t.MainPrevisualizarTip;
                var comoSeLlama = t.MainVelocidad;   // «Esmero» / «Effort»

                Program.Assert(globo.Contains(comoSeLlama, StringComparison.OrdinalIgnoreCase),
                    $"el globo de la previa nombra «{comoSeLlama}» ({idioma})");
                Program.Assert(!globo.Contains("los ajustes actuales") && !globo.Contains("the current settings"),
                    $"y ya no promete «los ajustes actuales», que incluían uno que no aplica ({idioma})");
            }
            finally { Ondine.Localizacion.Idioma.Actual = antes; }
        }
    }

    /// <summary>
    /// Y la Ayuda lo cuenta con sus dos mitades: para qué SÍ sirve la previa (la imagen) y para
    /// qué no (el tiempo y el tamaño, que se miden con «Medir»).
    /// </summary>
    private static void LaAyudaLoExplica()
    {
        foreach (var idioma in new[] { "es", "en" })
        {
            var antes = Ondine.Localizacion.Idioma.Actual;
            try
            {
                Ondine.Localizacion.Idioma.Actual = idioma;
                var t = Ondine.Localizacion.Textos.Instancia;

                Program.Assert(t.AyudaComprimirPreviaTitulo.Length > 5 && t.AyudaComprimirPrevia.Length > 120,
                    $"la Ayuda tiene su apartado de la previa ({idioma})");
                Program.Assert(t.AyudaComprimirPrevia.Contains(t.MainVelocidad, StringComparison.OrdinalIgnoreCase),
                    $"y nombra el Esmero, que es el ajuste que la previa no respeta ({idioma})");
            }
            finally { Ondine.Localizacion.Idioma.Actual = antes; }
        }
    }

    /// <summary>
    /// Que el globo esté escrito no basta: tiene que estar puesto en las dos pantallas. Un texto
    /// perfecto que ningún XAML enlaza no lo lee nadie.
    /// </summary>
    private static void LasDosInterfacesEnsenanElGlobo()
    {
        var raiz = LocalizarRaiz();
        var pantallas = new[]
        {
            Path.Combine(raiz, "src", "Ondine", "MainWindow.xaml"),
            Path.Combine(raiz, "src", "Ondine.Avalonia", "VentanaPrincipal.axaml"),
        };

        foreach (var f in pantallas)
        {
            var nombre = Path.GetFileName(f);
            var texto = File.Exists(f) ? File.ReadAllText(f) : "";
            Program.Assert(texto.Contains("MainPrevisualizarTip"),
                $"{nombre} enlaza el globo de la previa");
        }

        // Y la Ayuda, en las dos también.
        foreach (var f in new[] { Path.Combine(raiz, "src", "Ondine", "AyudaWindow.xaml"),
                                  Path.Combine(raiz, "src", "Ondine.Avalonia", "Ayuda.axaml") })
        {
            var nombre = Path.GetFileName(f);
            var texto = File.Exists(f) ? File.ReadAllText(f) : "";
            Program.Assert(texto.Contains("AyudaComprimirPrevia"),
                $"{nombre} tiene el apartado de la previa");
        }
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }
}
