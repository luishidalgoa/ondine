using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        Probar("PeliculasPanel", () => new PeliculasPanel());
        Probar("PeliculasWindow", () => new PeliculasWindow(
            new[]
            {
                Path.Combine("C:", "pelis", "Disney", "Up.mp4"),
                Path.Combine("C:", "pelis", "Grease (1978)", "Grease.mp4"),
            },
            Path.Combine("C:", "pelis")));
        Probar("ComplementosPanel", () => new ComplementosPanel(cat, res));

        LaTarjetaDiceLoQueSeVaAEscribir(cat);
        LosAvisosNoSePisan();

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
    /// La tarjeta del resolutor tiene que decir EXACTAMENTE el nombre que se va a
    /// escribir.
    ///
    /// <para>
    /// Vive aquí y no en el arnés del motor porque <c>OrganizarRow</c> es de WPF y
    /// aquel no compila con WPF. Y no se puede dejar sin prueba: con un fichero
    /// que junta dos episodios, la tarjeta decía «quedaría como S1993E1260 - El
    /// invento para hacer bonsáis» mientras el renombrado de verdad ponía
    /// «S1993E1260+1261 - … + La rueda auxiliar invisible». El nombre se componía
    /// en dos sitios, y uno se olvidaba de las historias añadidas.
    /// </para>
    /// </summary>
    private static void LaTarjetaDiceLoQueSeVaAEscribir(ReindexCatalog cat)
    {
        try
        {
            var res = new ReindexResolution
            {
                Archivo = SignalExtractor.Extract(
                    Path.Combine("C:", "tv", "Serie - S1986E1 - Uno a.mkv"), "Season 1986"),
                Estado = ReindexEstado.Corregido,
                Confianza = ReindexConfianza.Alta,
                Episodio = cat.PorNum(1),
                Score = 1.0,
            };

            var fila = new OrganizarRow(res, cat, new LibraryTemplate());
            fila.AnadirHistoria(cat.PorNum(2)!, null);   // el fichero junta el 1 y el 2

            var elegido = fila.Candidatos.FirstOrDefault(c => c.EsElegido);
            if (elegido is null) { Mal("tarjeta = nombre real", new Exception("no hay candidato elegido")); return; }

            if (elegido.NombreResultante != fila.NombreNuevo)
            {
                Mal("tarjeta = nombre real", new Exception(
                    $"la tarjeta dice «{elegido.NombreResultante}» y se escribiría «{fila.NombreNuevo}»"));
                return;
            }

            Bien("la tarjeta dice el nombre que se va a escribir (con dos episodios dentro)");
        }
        catch (Exception ex) { Mal("tarjeta = nombre real", ex); }
    }

    /// <summary>
    /// Los dos avisos de la revisión pueden estar puestos a la vez, así que no
    /// pueden compartir sitio.
    ///
    /// <para>
    /// Uno dice cuántas filas piden decisión y el otro cuántas se acaban de
    /// renombrar, con el botón de deshacer. Son cosas distintas y las dos pueden
    /// ser ciertas —aplicas siete y quedan cuarenta y seis por decidir—, pero
    /// estaban los dos en la misma fila de la rejilla y se pintaban encima:
    /// «46 de 59 ficheros necesitan que decidas tú» tachado por «7 ficheros
    /// renombrados», ilegibles los dos.
    /// </para>
    /// <para>
    /// Se mide con los dos VISIBLES y con la vista ya medida, porque colapsado no
    /// ocupa y el solape no aparece. Comparar la fila declarada no bastaría: dos
    /// hermanos en filas distintas de rejillas distintas también se pisarían.
    /// Aquí se miran los rectángulos de verdad.
    /// </para>
    /// </summary>
    private static void LosAvisosNoSePisan()
    {
        const string nombre = "los dos avisos de la revisión no se pisan";
        try
        {
            var vista = new OrganizarView();
            var revision = (Grid)vista.FindName("vistaRevision")!;
            var aviso = (Border)vista.FindName("bannerAviso")!;
            var aplicado = (Border)vista.FindName("bannerAplicado")!;

            revision.Visibility = Visibility.Visible;
            aviso.Visibility = Visibility.Visible;
            aplicado.Visibility = Visibility.Visible;

            vista.Measure(new Size(1280, 800));
            vista.Arrange(new Rect(0, 0, 1280, 800));
            vista.UpdateLayout();

            var a = aviso.TransformToAncestor(vista).TransformBounds(new Rect(aviso.RenderSize));
            var b = aplicado.TransformToAncestor(vista).TransformBounds(new Rect(aplicado.RenderSize));

            if (a.Height == 0 || b.Height == 0)
                throw new Exception("uno de los avisos no llegó a ocupar sitio; la prueba no medía nada");

            a.Intersect(b);
            if (a.Height > 0.5)
                throw new Exception($"se solapan {a.Height:n0} px en vertical: se pintan uno encima del otro");

            Bien(nombre);
        }
        catch (Exception ex) { Mal(nombre, ex); }
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
