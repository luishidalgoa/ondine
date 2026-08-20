using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Ondine;
using Ondine.Reindex;
using Ondine.Rutas;

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
        Probar("ComplementosPanel", () => new ComplementosPanel(
            () => new ComplementosPanel.EstadoDeOrganizar(cat, res, "")));

        LaTarjetaDiceLoQueSeVaAEscribir(cat);
        LosAvisosNoSePisan();
        ElRepasoDePeliculasTieneLaFormaDelDeSeries();
        LasPreferenciasLleganHastaElFinal();
        LaPantallaDeSeriesNoVuelveEnModoPeliculas();

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
    /// <summary>
    /// El repaso de películas tiene la forma del de series, y el botón de
    /// identificar dice si se puede pulsar.
    ///
    /// <para>
    /// Dos cosas en una prueba porque son el mismo recorrido. La primera versión de
    /// esta pantalla era un diálogo modal con una lista simple y sin casillas:
    /// aplicar era «las once o ninguna», y era lo que menos confianza daba de la
    /// app. Esto fija la forma —chips, rejilla y barra de aplicar, donde está la de
    /// series— para que no se pierda.
    /// </para>
    /// <para>
    /// Los ajustes se escriben en una carpeta temporal con
    /// <c>DatosDeUsuario.RaizOverride</c>: sin eso la prueba leería las preferencias
    /// de verdad de quien la corre, y pasaría o fallaría según cómo tenga él la app.
    /// </para>
    /// </summary>
    private static void ElRepasoDePeliculasTieneLaFormaDelDeSeries()
    {
        const string nombre = "el repaso de películas tiene la forma del de series, con casilla por fila";
        var raizAntes = DatosDeUsuario.RaizOverride;
        string? tmp = null;
        try
        {
            tmp = Path.Combine(Path.GetTempPath(), "ondine-humo-" + Guid.NewGuid().ToString("N")[..8]);
            var datos = Path.Combine(tmp, "datos");
            var pelis = Path.Combine(tmp, "pelis");
            Directory.CreateDirectory(datos);
            Directory.CreateDirectory(Path.Combine(pelis, "Blade Runner (1982)"));

            File.WriteAllText(Path.Combine(pelis, "Grease 1978.mp4"), "x");
            File.WriteAllText(Path.Combine(pelis, "Blade Runner (1982)", "Blade Runner (1982).mkv"), "x");

            DatosDeUsuario.RaizOverride = datos;

            var ajustes = new Settings();
            ajustes.Tmdb.Activo = true;
            ajustes.Tmdb.PonerClave("una-clave-de-prueba");
            SettingsStore.Save(ajustes);

            var vista = new OrganizarView();

            // ApuntarA es la entrada de verdad: pone la carpeta Y la cuenta, así que
            // la tabla mide filas reales en vez de quedarse vacía y no comprobar nada.
            // Va ANTES de elegir el tipo: apuntar a una carpeta recoloca el
            // desplegable con el tipo recordado de esa carpeta, y en una carpeta nueva
            // eso es «serie». Al revés, la elección se perdería.
            vista.ApuntarA(pelis);
            ((ComboBox)vista.FindName("cboTipo")!).SelectedIndex = 1;   // Películas
            Vaciar();

            var panel = (PeliculasPanel)vista.FindName("vistaPeliculas")!;
            ((Button)panel.FindName("btnOrdenar")!).RaiseEvent(
                new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Vaciar();

            var tabla = (DataGrid)vista.FindName("tablaPelis")!;
            if (tabla.Items.Count < 2)
                throw new Exception($"la tabla salió con {tabla.Items.Count} filas: la prueba no está midiendo nada");

            // Lo que faltaba y era el problema: casilla por fila, y solo en lo que
            // de verdad se puede aplicar.
            var filas = tabla.Items.Cast<PeliculaFila>().ToList();
            if (!filas.Any(x => x.ListoParaAplicar))
                throw new Exception("ninguna fila se podía aplicar; el caso de prueba no vale");
            if (filas.Any(x => x.ListoParaAplicar && !x.Marcado))
                throw new Exception("las filas aplicables no venían marcadas");
            if (filas.Any(x => !x.ListoParaAplicar && x.Entra))
                throw new Exception("una fila que no se puede aplicar entraba en el «Aplicar»");

            // Y la forma: lo de películas a la vista, lo de series recogido.
            void Ver(string cual, Visibility esperada)
            {
                var e = (FrameworkElement)vista.FindName(cual)!;
                if (e.Visibility != esperada)
                    throw new Exception($"«{cual}» estaba {e.Visibility} y se esperaba {esperada}");
            }

            Ver("filaChipsPelis", Visibility.Visible);
            Ver("vistaRevisionPelis", Visibility.Visible);
            Ver("filaAccionesPelis", Visibility.Visible);
            Ver("vistaPeliculas", Visibility.Collapsed);
            Ver("vistaInicio", Visibility.Collapsed);
            Ver("vistaRevision", Visibility.Collapsed);
            Ver("filaChips", Visibility.Collapsed);
            Ver("filaAcciones", Visibility.Collapsed);

            // El botón de identificar, con TMDb encendido y clave puesta.
            if (!((Button)vista.FindName("btnIdentificar")!).IsEnabled)
                throw new Exception("con TMDb encendido y clave puesta el botón seguía apagado");
            if (((TextBlock)vista.FindName("lblTmdbApagado")!).Visibility == Visibility.Visible)
                throw new Exception("decía que TMDb está apagado cuando no lo está");

            // Y ahora apagado: mismo recorrido, botón quieto y motivo a la vista.
            ajustes.Tmdb.Activo = false;
            SettingsStore.Save(ajustes);

            var vista2 = new OrganizarView();
            vista2.ApuntarA(pelis);
            ((ComboBox)vista2.FindName("cboTipo")!).SelectedIndex = 1;
            ((Button)((PeliculasPanel)vista2.FindName("vistaPeliculas")!).FindName("btnOrdenar")!).RaiseEvent(
                new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Vaciar();

            if (((Button)vista2.FindName("btnIdentificar")!).IsEnabled)
                throw new Exception("apagado por el usuario y el botón se podía pulsar de todas formas");
            if (((TextBlock)vista2.FindName("lblTmdbApagado")!).Visibility != Visibility.Visible)
                throw new Exception("el botón estaba apagado y sin decir por qué: eso se lee como roto");

            Vaciar();
            Bien(nombre);
        }
        catch (Exception ex) { Mal(nombre, ex); }
        finally
        {
            DatosDeUsuario.RaizOverride = raizAntes;
            if (tmp != null) { try { Directory.Delete(tmp, true); } catch { } }
        }
    }

    /// <summary>
    /// Todo lo que hay en una pestaña de Preferencias se puede alcanzar.
    ///
    /// <para>
    /// Este fallo se cometió <b>tres veces</b>: el alto de la ventana era fijo,
    /// las pestañas no se desplazaban, y la regla era acordarse de subir el alto
    /// cada vez que se añadía una línea. Dejó fuera una casilla de «General», y
    /// más tarde el botón del final de «Películas». Una regla que hay que
    /// recordar a mano no es una regla; esto es lo que la sustituye.
    /// </para>
    /// <para>
    /// Mide con el tamaño REAL de la ventana —580×620—, no con los 1280×800 del
    /// resto del arnés: medir con más sitio del que hay es no medir nada.
    /// </para>
    /// </summary>
    private static void LasPreferenciasLleganHastaElFinal()
    {
        const string nombre = "en Preferencias se llega hasta el final de la pestaña más alta";
        try
        {
            var v = new PreferencesWindow(new Settings(), new[] { "Un preset" });
            var contenido = (FrameworkElement)v.Content;

            var pestanias = (TabControl)v.FindName("pestanias")!;
            pestanias.SelectedIndex = pestanias.Items.Count - 1;   // «Películas», la más alta

            contenido.Measure(new Size(v.Width, v.Height));
            contenido.Arrange(new Rect(0, 0, v.Width, v.Height));
            contenido.UpdateLayout();

            var sv = Buscar<ScrollViewer>(pestanias)
                     ?? throw new Exception("la pestaña no tiene por dónde desplazarse: lo que sobresalga queda inalcanzable");

            if (sv.ViewportHeight <= 0)
                throw new Exception("el área de la pestaña midió cero de alto; la prueba no estaba mirando nada");

            // El último control de la pestaña. Si esto se alcanza, se alcanza todo.
            var ultimo = (Button)v.FindName("btnTmdbAbrir")!;

            sv.ScrollToBottom();
            contenido.UpdateLayout();

            var caja = ultimo.TransformToAncestor(sv).TransformBounds(new Rect(ultimo.RenderSize));
            if (caja.Height <= 0)
                throw new Exception("el último control de la pestaña no llegó a ocupar sitio");

            // Con medio píxel de margen: la comparación es en unidades de WPF y
            // el redondeo del arranque no debe hacer fallar una prueba buena.
            if (caja.Bottom > sv.ViewportHeight + 0.5 || caja.Top < -0.5)
                throw new Exception(
                    $"desplazada al final, el último control sigue fuera de la vista " +
                    $"(abajo {caja.Bottom:0.#} sobre un alto de {sv.ViewportHeight:0.#})");

            v.Close();
            Vaciar();
            Bien(nombre);
        }
        catch (Exception ex) { Mal(nombre, ex); }
    }

    /// <summary>
    /// En modo «Películas», la pantalla de series no vuelve a aparecer por detrás.
    ///
    /// <para>
    /// El fallo tal y como se vio: eliges la carpeta, se cierra el explorador de
    /// archivos, y el panel de catálogos y el de ficheros de <b>series</b> quedan
    /// pintados ENCIMA del de películas, los dos a la vez y superpuestos. La causa
    /// es que volver al estado inicial ponía la vista de series a la vista <b>sin
    /// mirar el tipo de biblioteca</b>, y nadie volvía a mirarlo después.
    /// </para>
    /// <para>
    /// Se dispara con el botón «Volver» y no eligiendo carpeta porque abrir el
    /// explorador de archivos deja la prueba esperando a una persona. Es el mismo
    /// camino: los dos pasan por el estado inicial, que es donde estaba el fallo.
    /// </para>
    /// </summary>
    private static void LaPantallaDeSeriesNoVuelveEnModoPeliculas()
    {
        const string nombre = "en modo Películas la pantalla de series no vuelve por detrás";
        try
        {
            var vista = new OrganizarView();
            var cboTipo = (ComboBox)vista.FindName("cboTipo")!;
            var vistaInicio = (FrameworkElement)vista.FindName("vistaInicio")!;
            var vistaPeliculas = (FrameworkElement)vista.FindName("vistaPeliculas")!;
            var panelSerie = (FrameworkElement)vista.FindName("panelSerie")!;

            cboTipo.SelectedIndex = 1;   // Películas
            Vaciar();

            if (vistaPeliculas.Visibility != Visibility.Visible)
                throw new Exception("al elegir Películas no se enseñó la pantalla de películas");
            if (vistaInicio.Visibility == Visibility.Visible)
                throw new Exception("al elegir Películas seguía a la vista la pantalla de series");

            // Volver al estado inicial: es por donde pasa también elegir carpeta.
            ((Button)vista.FindName("btnVolver")!).RaiseEvent(
                new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Vaciar();

            if (vistaInicio.Visibility == Visibility.Visible)
                throw new Exception("la pantalla de series volvió a aparecer, superpuesta a la de películas");
            if (panelSerie.Visibility == Visibility.Visible)
                throw new Exception("volvió el panel de serie, que a una película no le aplica");
            if (vistaPeliculas.Visibility != Visibility.Visible)
                throw new Exception("y de paso se perdió la pantalla de películas");

            Vaciar();
            Bien(nombre);
        }
        catch (Exception ex) { Mal(nombre, ex); }
    }

    /// <summary>El primer descendiente de ese tipo, buscando por el árbol visual.</summary>
    private static T? Buscar<T>(DependencyObject raiz) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
        {
            var hijo = VisualTreeHelper.GetChild(raiz, i);
            if (hijo is T t) return t;
            if (Buscar<T>(hijo) is { } hondo) return hondo;
        }
        return null;
    }

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

        Console.WriteLine($"  \u2717 {q}\n      {raiz.GetType().Name}: {raiz.Message}");

        // Y las dos primeras lineas de la traza. Una NullReferenceException sin sitio
        // no dice nada: se sabe que algo era nulo y no donde, que es lo unico que hace
        // falta para arreglarlo.
        foreach (var linea in (raiz.StackTrace ?? "").Split('\n').Take(2)
                                  .Select(l => l.Trim()).Where(l => l.Length > 0))
            Console.WriteLine($"      {linea}");
    }
}
