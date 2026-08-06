using System.IO;
using System.Windows;
using System.Windows.Threading;
using Ondine;
using Ondine.Reindex;

namespace Ondine.Ui.Smoke;

/// <summary>
/// Que cada pantalla se pueda CONSTRUIR y MEDIR sin reventar.
///
/// <para>
/// El arnés del motor tiene cientos de pruebas y ninguna toca una línea de XAML.
/// Por ahí se coló un <c>VerticalAlignment="Baseline"</c> —válido para el
/// compilador de marcado, inexistente en el enumerado— que no falló hasta que
/// alguien abrió esa pantalla. Un fallo de marcado no se ve leyendo el código:
/// se ve al construirlo.
/// </para>
/// <para>
/// <b>Qué cubre y qué no.</b> Cubre: que el XAML se analice, que TODOS los
/// recursos estáticos que nombra existan de verdad, y que una pasada de medida
/// y colocación no lance. NO cubre que se vea bien, ni las plantillas de datos
/// de una lista vacía —esas no se materializan sin elementos—, así que a las
/// listas se les dan elementos de mentira cuando se puede. Es una prueba de
/// humo, no un test visual.
/// </para>
/// </summary>
public static class Program
{
    private static int _ok, _fallos;

    [STAThread]
    public static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("── Humo de la interfaz ─────────────────────────────\n");

        // La Application de verdad, con SUS diccionarios: es lo que hace que
        // `{StaticResource FontUI}` resuelva. Montar un Application pelado
        // convertiría la prueba en un teatro que pasa siempre.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Ondine;component/Theme.xaml") });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("pack://application:,,,/Ondine;component/ThemeOrganizar.xaml") });

        var cat = ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0", "serie": "Serie de prueba",
          "episodios": [
            { "num": 1, "temporada": 1, "titulos": { "es": ["Uno a", "Uno b"] } },
            { "num": 2, "temporada": 1, "titulos": { "es": ["Dos"] } },
            { "num": 3, "temporada": 2, "especial": true, "titulos": { "es": ["Un especial"] } }
          ]
        }
        """);

        // Una resolución de verdad: con la lista vacía, las plantillas de datos
        // de las listas no se materializan y la prueba pasaría sin mirarlas.
        var res = new List<ReindexResolution>
        {
            new() { Archivo = SignalExtractor.Extract(Path.Combine("C:", "tv", "uno.mkv"), "T1") },
        };

        Probar("MainWindow", () => new MainWindow());
        Probar("OrganizarView", () => new OrganizarView());
        Probar("RecortesView", () => new RecortesView());
        Probar("AyudaWindow", () => new AyudaWindow());
        Probar("PromptWindow", () => new PromptWindow("Serie de prueba"));
        Probar("PreferencesWindow", () => new PreferencesWindow(new Settings(), new[] { "Un preset" }));
        Probar("RenameWindow", () => new RenameWindow(
            new RenameRule(), new[] { ("un fichero.mkv", DateTime.Now) }, new List<string>(), new List<string>()));
        Probar("CatalogoWindow", () => new CatalogoWindow(cat, loQueHay: res));
        Probar("FaltantesWindow", () => new FaltantesWindow(cat, res));
        Probar("ReordenarWindow", () => new ReordenarWindow(res, Path.Combine("C:", "tv"), new Settings()));
        Probar("ComplementosPanel", () => new ComplementosPanel(cat, res));

        // Fuera a propósito, y dicho en voz alta para que no parezca cobertura:
        //   · DialogWindow      — constructor privado, solo se llega por ShowDialog.
        //   · PistasWindow      — necesita un Engine y un vídeo de verdad.
        //   · ReproductorWindow — necesita un vídeo de verdad.
        Console.WriteLine("\n  · sin cubrir: DialogWindow, PistasWindow, ReproductorWindow " +
                          "(piden ventana modal o un vídeo real)");

        Console.WriteLine($"\n── {_ok} pasan · {_fallos} fallan ──");
        app.Shutdown();
        return _fallos == 0 ? 0 : 1;
    }

    /// <summary>
    /// Construye, mide y coloca. La medida importa tanto como la construcción:
    /// es cuando se aplican las plantillas de control y se resuelven los
    /// recursos que solo nombra el estilo, no el elemento.
    /// </summary>
    private static void Probar(string nombre, Func<FrameworkElement> hacer)
    {
        try
        {
            var e = hacer();

            // Una ventana no se mide como un control suelto: lo que se mide es su
            // contenido. Medirla a ella no baja al árbol y la prueba se quedaría
            // en «el constructor no lanzó».
            var aMedir = e is Window v ? v.Content as FrameworkElement : e;
            if (aMedir != null)
            {
                aMedir.Measure(new Size(1280, 800));
                aMedir.Arrange(new Rect(0, 0, 1280, 800));
                aMedir.UpdateLayout();
            }

            // Lo que la interfaz dejó pendiente en la cola: sin vaciarla, un fallo
            // dentro de un Dispatcher.BeginInvoke del constructor no llega nunca.
            Vaciar();

            if (e is Window w) w.Close();
            Bien(nombre);
        }
        catch (Exception ex)
        {
            Mal(nombre, ex);
        }
    }

    private static void Vaciar() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private static void Bien(string q)
    {
        _ok++;
        Console.WriteLine($"  ✓ {q}");
    }

    private static void Mal(string q, Exception ex)
    {
        _fallos++;
        // El mensaje de dentro es el que dice QUÉ recurso o qué propiedad falla; el
        // de fuera solo dice «error al establecer la propiedad Content».
        var raiz = ex;
        while (raiz.InnerException != null) raiz = raiz.InnerException;
        Console.WriteLine($"  ✗ {q}\n      {raiz.GetType().Name}: {raiz.Message}");
    }
}
