using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Threading.Tasks;

namespace Ondine.Ava;

/// <summary>
/// Que el tema portado se aplique de verdad.
///
/// <para>
/// Hace falta porque <b>un selector que no casa no da error</b>. Si
/// «^:pointerover /template/ Border#b» apunta a una parte que no existe, Avalonia no se
/// queja: el boton sale con el aspecto de fabrica y solo se nota mirandolo. Es el mismo
/// silencio que ya mordio en el spike con los bindings.
/// </para>
/// <para>
/// Compilar no prueba nada aqui. Por eso esto abre la ventana de verdad, busca las partes
/// que el ControlTemplate deberia haber creado, y mira si los valores son los nuestros o
/// los del tema Fluent que viene de serie.
/// </para>
/// </summary>
public static class Comprobacion
{
    public static readonly List<string> Resultados = [];

    private static void Dice(bool bien, string que) =>
        Resultados.Add($"{(bien ? "\u2713" : "\u2717")} {que}");

    public static void Correr(Window v)
    {
        // Solo los que llevan un Theme PUESTO por nosotros. La primera version contaba
        // todos los Button del arbol, y al anadir un ComboBox empezo a contar tambien el
        // que ese control trae dentro de su plantilla — que no usa el tema de Ondine ni
        // tiene por que. La prueba fallaba por un boton que no era suyo.
        var botones = v.GetVisualDescendants().OfType<Button>()
                       .Where(b => b.Theme is not null)
                       .ToList();

        Dice(botones.Count >= 5, $"los botones estan en el arbol visual: {botones.Count}");
        if (botones.Count == 0) return;

        // ── La plantilla es LA NUESTRA, no la de Fluent ───────────────────────────
        // Si el ControlTheme no se hubiera aplicado, estas dos partes no existirian:
        // el boton de Fluent no tiene ni «b» ni «haz».
        foreach (var (nombre, cuantos) in new[] { ("b", botones.Count), ("haz", botones.Count) })
        {
            var partes = botones
                .Select(b => b.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == nombre))
                .Count(x => x is not null);

            Dice(partes == cuantos,
                $"los {cuantos} botones tienen su parte «{nombre}» ({partes} encontradas) " +
                "— si faltara, el ControlTheme no se habria aplicado y no lo diria nadie");
        }

        // ── Y los valores son los nuestros ───────────────────────────────────────
        var primario = botones[0];
        var esAcento = primario.Foreground is ISolidColorBrush s &&
                       s.Color.ToString().EndsWith("968AE0", StringComparison.OrdinalIgnoreCase);
        Dice(esAcento, $"el primario pinta con el acento de Ondine ({primario.Foreground})");

        var caja = primario.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "b");
        Dice(caja is not null && caja.CornerRadius.TopLeft == 8,
            $"y con el radio de esquina de Ondine, no el de Fluent ({caja?.CornerRadius})");

        // ── El haz nace apagado ──────────────────────────────────────────────────
        // Encendido siempre serian tantas animaciones como botones haya en pantalla, y
        // eso se paga en CPU aunque no se vea. Ya costo medirlo en la version de WPF.
        var haz = primario.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "haz");
        Dice(haz is not null && haz.Opacity == 0,
            "el haz nace apagado: encendido siempre son tantas animaciones como botones");

        // ── El deshabilitado se atenua ───────────────────────────────────────────
        var apagado = botones.FirstOrDefault(b => !b.IsEnabled);
        var suCaja = apagado?.GetVisualDescendants().OfType<Border>().FirstOrDefault(x => x.Name == "b");
        Dice(suCaja is not null && suCaja.Opacity < 0.6,
            $"un boton apagado se atenua ({suCaja?.Opacity}) — y se atenua la caja, no el haz");
    }

    /// <summary>
    /// Los campos: que lleven el tema de Ondine y no el de Fluent, y que sus estados
    /// reaccionen.
    ///
    /// <para>
    /// Van como estilos implicitos, asi que el riesgo aqui es el contrario que en los
    /// botones: no que el selector no case, sino que el ControlTheme <b>no se aplique a
    /// todos</b>. Un TextBox dentro de un popup o de una plantilla podria quedarse con el
    /// de serie, y eso solo se ve mirando.
    /// </para>
    /// </summary>
    public static void CorrerCampos(Window v)
    {
        var caja = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        var marco = caja?.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "caja");
        Dice(marco is not null, "la caja de texto usa la plantilla de Ondine, no la de Fluent");
        Dice(marco is not null && marco.CornerRadius.TopLeft == 6,
            $"con su radio de esquina ({marco?.CornerRadius})");

        // El texto de ayuda se ve con la caja vacia y se va al escribir. En WPF era un
        // DataTrigger sobre Text.IsEmpty; aqui es la pseudoclase :empty.
        var pista = caja?.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "pista");
        Dice(pista is not null && pista.IsVisible,
            "el texto de ayuda se ve con la caja vacia");

        if (caja is not null)
        {
            caja.Text = "algo";
            v.UpdateLayout();
            Dice(pista is not null && !pista.IsVisible,
                "y desaparece al escribir — la pseudoclase :empty hace lo del DataTrigger de WPF");
            caja.Text = "";
        }

        // La casilla: el tic aparece al marcar.
        var casilla = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        var tic = casilla?.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
                          .FirstOrDefault(p => p.Name == "tic");
        Dice(tic is not null, "la casilla usa la plantilla de Ondine");
        Dice(tic is not null && tic.IsVisible,
            "y marcada enseña el tic");

        if (casilla is not null)
        {
            casilla.IsChecked = false;
            v.UpdateLayout();
            Dice(tic is not null && !tic.IsVisible, "y desmarcada lo esconde");
            casilla.IsChecked = true;
        }

        // El desplegable se viste con Setters, no con plantilla: aqui basta comprobar que
        // los valores son los nuestros.
        var combo = v.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault();
        Dice(combo is not null && combo.CornerRadius.TopLeft == 6,
            $"el desplegable lleva los valores de Ondine ({combo?.CornerRadius})");
    }

    /// <summary>
    /// «Renombrar», la pantalla con mas piezas nuevas del puerto.
    ///
    /// <para>
    /// Se miran las dos que no existian hasta aqui. La <b>vista previa</b>, que ya no es un
    /// ListView sino un DataGrid: si el enlace de las columnas no cuaja, la tabla sale
    /// vacia o con las celdas en blanco y la ventana no protesta. Y el <b>desplegable de
    /// sugerencias</b>, que en Avalonia necesita robarle las teclas al campo por la fase de
    /// tunel — algo que no existe en WPF, donde bastaba con un «Preview…». Si el tunel no
    /// engancha, la flecha abajo mueve el cursor en vez de abrir la lista, y nadie lo nota
    /// hasta que le hace falta.
    /// </para>
    /// </summary>
    public static async Task CorrerRenombrar(Window dueno)
    {
        var ficheros = new List<(string, DateTime)>
        {
            ("Capitulo 01 1080p.mkv", new DateTime(2020, 1, 1)),
            ("Capitulo 02 1080p.mkv", new DateTime(2020, 1, 2)),
        };

        var v = new Renombrar(new Ondine.RenameRule(), ficheros, [], []);
        v.Show(dueno);
        await Task.Delay(400);

        // ── La vista previa ──────────────────────────────────────────────────
        var tabla = v.GetVisualDescendants().OfType<DataGrid>().FirstOrDefault();
        Dice(tabla?.Columns.Count == 2, $"la vista previa tiene sus dos columnas ({tabla?.Columns.Count})");

        // Leido de las CELDAS PINTADAS, no de la lista que las alimenta. Esto se aprendio
        // rompiendolo: con el enlace de una columna apuntando a una propiedad que no
        // existe, mirar la lista sigue dando bien —los datos estan— y la tabla sale con la
        // columna en blanco. Lo que hay que comprobar es lo que se ve.
        List<string> Celdas() => tabla is null ? [] :
            tabla.GetVisualDescendants().OfType<DataGridRow>()
                 .SelectMany(f => f.GetVisualDescendants().OfType<TextBlock>())
                 .Select(t => t.Text ?? "").Where(x => x.Length > 0).ToList();

        List<object> Filas() =>
            (tabla?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? [];

        Dice(Filas().Count == 2, $"y una fila por fichero ({Filas().Count} de 2)");

        var celdas = Celdas();
        Dice(celdas.Count >= 4,
            $"con las cuatro celdas pintadas, no solo los datos detras ({celdas.Count})");
        Dice(celdas.Any(c => c.Contains("Capitulo 01")),
            "y la columna del nombre original trae el nombre");

        string Nuevo(int i) => (Filas().ElementAtOrDefault(i) as RenamePreviewRow)?.Nuevo ?? "";

        // Sin regla no cambia nada, y se DICE que no cambia: una columna vacia se lee como
        // «se queda sin nombre».
        Dice(!string.IsNullOrWhiteSpace(Nuevo(0)),
            $"sin regla, la columna nueva dice que no cambia ({Nuevo(0)})");

        // ── En vivo ──────────────────────────────────────────────────────────
        var buscar = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "txtSearch");
        var cambiar = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "txtReplace");
        buscar!.Text = "1080p";
        cambiar!.Text = "HD";
        await Task.Delay(200);

        Dice(Nuevo(0).Contains("HD"),
            $"escribir repinta la vista previa al momento ({Nuevo(0)})");
        Dice(Celdas().Any(c => c.Contains("HD")),
            "y lo repintado llega a la celda, no se queda en el dato");

        // ── El desplegable de sugerencias ────────────────────────────────────
        // La flecha abajo tiene que ABRIR la lista, no mover el cursor. Es lo que prueba
        // que el enganche por tunel esta puesto.
        var pop = v.GetVisualDescendants().OfType<Popup>().FirstOrDefault(p => p.Name == "popSearch");
        buscar.Focus();
        await Task.Delay(150);
        pop!.IsOpen = false;

        buscar.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Down,
        });
        await Task.Delay(150);
        // Esto prueba que la flecha abre la lista. Lo que NO prueba —y conviene no
        // presumirlo— es que el enganche por tunel le gane al campo de texto: disparar la
        // tecla a mano no hace competir a nadie, y con un KeyDown normal pasa igual. Que
        // el Enter acepte la sugerencia en vez de irse al boton por defecto solo lo dice
        // un teclado de verdad; queda apuntado con lo demas que hay que probar en mano.
        Dice(pop.IsOpen, "la flecha abajo abre las sugerencias");

        v.Close();
    }

    /// <summary>
    /// «Ordenar por temporadas», con ficheros de verdad en una carpeta temporal.
    ///
    /// <para>
    /// Con ficheros de verdad y no de mentira porque el motor pregunta al disco: el plan
    /// mira si el origen sigue estando y si el destino esta ocupado. Inventar eso seria
    /// comprobar otra cosa.
    /// </para>
    /// <para>
    /// Lo que se vigila es la casilla de «ver solo los que van». Es un filtro, y un filtro
    /// que se desengancha al portar no se nota: la lista sigue llena. Solo que enseña
    /// TODO, incluido lo que no se va a mover, justo antes de que alguien pulse.
    /// </para>
    /// </summary>
    public static async Task CorrerReordenar(Window dueno)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-auto-reordenar");
        Directory.CreateDirectory(carpeta);
        var uno = Path.Combine(carpeta, "cap1.mkv");
        var dos = Path.Combine(carpeta, "cap2.mkv");
        File.WriteAllText(uno, "x");
        File.WriteAllText(dos, "x");

        try
        {
            var catalogo = Ondine.Reindex.ReindexCatalog.Parse("""
            {
              "esquema": "reindex/1.0",
              "serie": "Serie de prueba",
              "episodios": [ { "num": 1, "temporada": 1, "titulos": { "es": ["Uno"] } } ]
            }
            """);
            var episodio = catalogo.Episodios[0];

            var resoluciones = new List<Ondine.Reindex.ReindexResolution>
            {
                // Curado y con temporada: este SE MUEVE.
                new()
                {
                    Archivo = new() { Path = uno, NombreArchivo = "cap1.mkv", Extension = ".mkv" },
                    Estado = Ondine.Reindex.ReindexEstado.Limpio,
                    Episodio = episodio,
                },
                // En conflicto: este se queda. No se sabe de que temporada es -por eso esta
                // en conflicto- y moverlo a una carpeta decidida a medias es peor.
                new()
                {
                    Archivo = new() { Path = dos, NombreArchivo = "cap2.mkv", Extension = ".mkv" },
                    Estado = Ondine.Reindex.ReindexEstado.Conflicto,
                    Episodio = episodio,
                },
            };

            var v = new Reordenar(resoluciones, carpeta, new Ondine.Settings());
            v.Show(dueno);
            await Task.Delay(300);

            var lista = v.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
            int Filas() => (lista?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? -1;

            Dice(Filas() == 1, $"de serie solo enseña lo que se mueve ({Filas()} de 2)");

            var mover = v.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "btnMover");
            Dice(mover?.IsEnabled == true, "y el boton de mover esta vivo, porque hay uno que va");
            Dice(mover?.Content?.ToString()?.Contains("1") == true,
                $"con la cuenta en el rotulo ({mover?.Content})");

            // La casilla es un filtro: si se desengancha al portar, la lista sigue llena
            // y nadie lo nota. Aqui tiene que aparecer el que NO se mueve.
            var chk = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
            chk!.IsChecked = false;
            await Task.Delay(150);
            Dice(Filas() == 2, $"quitar el filtro saca tambien el que se queda ({Filas()} de 2)");

            var riesgo = v.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "cajaRiesgo");
            Dice(riesgo?.IsVisible == false,
                "y sin nada que avisar la caja de riesgos no ocupa sitio");

            // El boton de deshacer no esta: no se ha movido nada todavia. Un boton que no
            // hace nada al pulsarlo ensena a desconfiar del resto.
            var deshacer = v.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "btnDeshacer");
            Dice(deshacer?.IsVisible == false, "ni hay que deshacer nada antes de mover");

            v.Close();
        }
        finally
        {
            try { Directory.Delete(carpeta, true); } catch { }
        }
    }

    /// <summary>
    /// «Quitar pistas», con tres pistas de mentira pero reglas de verdad.
    ///
    /// <para>
    /// Lo que se comprueba no es que se vea: es que <b>las casillas siguen enganchadas</b>.
    /// El enlace de IsChecked cambia de sintaxis al portar y, si se rompe, la ventana
    /// sigue pintandose igual — solo que marcar no hace nada. Eso, en una pantalla que
    /// borra pistas de un fichero, se descubre tarde.
    /// </para>
    /// </summary>
    public static async Task CorrerPistas(Window dueno)
    {
        var pistas = new List<Ondine.Pista>
        {
            new(0, Ondine.TipoPista.Video, "hevc", "", null, 4_000_000),
            new(1, Ondine.TipoPista.Audio, "aac", "spa", 2, 128_000),
            new(2, Ondine.TipoPista.Audio, "aac", "eng", 6, 384_000),
        };

        var v = new Pistas(new Ondine.Engine(), "peli.mkv", pistas, 3600);
        v.Show(dueno);
        await Task.Delay(300);

        var casillas = v.GetVisualDescendants().OfType<CheckBox>().ToList();
        var quitar = v.GetVisualDescendants().OfType<Button>()
                      .FirstOrDefault(b => b.Name == "btnQuitar");
        var aviso = v.GetVisualDescendants().OfType<TextBlock>()
                     .FirstOrDefault(t => t.Name == "lblAviso");

        Dice(casillas.Count == 3, $"una casilla por pista ({casillas.Count} de 3)");

        // La regla de seguridad: la de video no se ofrece. Sin ella el resultado ya no es
        // este video, asi que no es una opcion que se le pueda dar a nadie por error.
        Dice(casillas.Count == 3 && casillas[0].IsEnabled == false,
            "la de video no se puede quitar");
        Dice(casillas.Count == 3 && casillas[1].IsEnabled && casillas[2].IsEnabled,
            "y las de audio si");

        Dice(quitar?.IsEnabled == false, "sin nada marcado no hay nada que quitar");

        // Marcar una: si el enlace se rompio al portar, esto no despierta el boton.
        casillas[1].IsChecked = true;
        await Task.Delay(150);
        Dice(quitar?.IsEnabled == true,
            "marcar una despierta el boton — la casilla sigue enganchada al motor");
        Dice(aviso?.IsVisible == false, "y con un audio todavia puesto no hay aviso");

        // Marcar las dos: quedarse sin audio es legitimo, pero nadie lo espera.
        casillas[2].IsChecked = true;
        await Task.Delay(150);
        Dice(aviso?.IsVisible == true, "quitar todo el audio avisa antes, no despues");

        v.Close();
    }

    /// <summary>
    /// «Que falta», abierta con un catalogo de verdad.
    ///
    /// <para>
    /// Lo que se comprueba es que la ventana <b>pinte lo que dice el motor</b>, no que se
    /// vea. El catalogo de la prueba tiene 3 episodios y solo uno resuelto, asi que tienen
    /// que salir 2 huecos: si saliera otra cifra, la pantalla estaria contando por su
    /// cuenta en vez de preguntarle a CoberturaCatalogo — que es justo lo que la Fase 1
    /// vino a evitar.
    /// </para>
    /// </summary>
    public static async Task CorrerFaltantes(Window dueno)
    {
        var catalogo = Ondine.Reindex.ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0",
          "serie": "Serie de prueba",
          "episodios": [
            { "num": 1, "temporada": 1, "titulos": { "es": ["Uno"] } },
            { "num": 2, "temporada": 1, "titulos": { "es": ["Dos"] } },
            { "num": 3, "temporada": 1, "titulos": { "es": ["Tres"] } }
          ]
        }
        """);

        var v = new Faltantes(catalogo, []);
        v.Show(dueno);
        await Task.Delay(300);

        var lista = v.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
        int Huecos() => (lista?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? -1;

        // Sin una sola resolucion no hay ninguna temporada EMPEZADA, y de serie solo se
        // miran las empezadas: lo normal es tener media biblioteca y querer saber que falta
        // de lo que ya tienes, no que te listen entera una serie que no has tocado.
        Dice(Huecos() == 0,
            $"de serie solo mira lo empezado, y aqui no hay nada empezado: {Huecos()} huecos");

        var chk0 = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        chk0!.IsChecked = true;
        await Task.Delay(150);

        Dice(Huecos() == 3,
            $"y al pedir el catalogo entero salen los 3 que dice el motor ({Huecos()})");

        var titulo = v.GetVisualDescendants().OfType<TextBlock>()
                      .FirstOrDefault(t => t.Name == "lblTitulo");
        Dice(titulo?.Text?.Contains("Serie de prueba") == true,
            $"y el titulo lleva el nombre de la serie ({titulo?.Text})");

        // La casilla se apaga al elegir una temporada concreta: ya estas mirando una, este
        // empezada o no, asi que «incluir las que no he empezado» no pinta nada.
        var cbo = v.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault();
        var chk = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        Dice(chk?.IsEnabled == true, "con «todas» la casilla esta disponible");

        if (cbo is not null && cbo.ItemCount > 1)
        {
            cbo.SelectedIndex = 1;
            await Task.Delay(150);
            Dice(chk?.IsEnabled == false,
                "y al elegir una temporada concreta se apaga, porque ya no decide nada");
        }

        v.Close();
    }

    /// <summary>
    /// El dialogo, abierto de verdad y contestado desde el codigo.
    ///
    /// <para>
    /// Lo que se mide no es que se vea: es que <b>devuelva lo que se pulso</b> y que
    /// cerrarlo con Esc cuente como «no». Esa segunda parte es la que importa, porque
    /// Confirmar se usa antes de tocar ficheros y un «cancelar» que se leyera como «si»
    /// borraria cosas.
    /// </para>
    /// </summary>
    public static async Task CorrerDialogo(Window dueno)
    {
        // Aceptar
        var tarea = Dialogo.Confirmar(dueno, "prueba", "un mensaje con una ruta: C:/algo");
        await Task.Delay(300);

        var d = dueno.OwnedWindows.OfType<Dialogo>().FirstOrDefault();
        if (d is null) { Dice(false, "el dialogo no llego a abrirse"); return; }

        Dice(true, "el dialogo se abre como modal de su ventana");

        var titulo = d.GetVisualDescendants().OfType<TextBlock>()
                      .FirstOrDefault(t => t.Name == "lblTitulo");
        Dice(titulo?.Text == "prueba", $"y lleva el titulo que se le paso ({titulo?.Text})");

        var si = d.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "btnSi");
        Dice(si is not null && si.IsVisible, "el boton de aceptar esta puesto");

        // Los rotulos salen del catalogo compartido, no de una cadena suelta.
        Dice(si?.Content?.ToString() == Ondine.Localizacion.Textos.Instancia.Si,
            $"y su rotulo sale del catalogo, igual que en WPF ({si?.Content})");

        si!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dice(await tarea, "pulsar aceptar devuelve true");

        // ══ Y LO QUE IMPORTA: cerrar sin aceptar es «no» ═════════════════════════
        var otra = Dialogo.Confirmar(dueno, "prueba", "esta se cierra sin contestar");
        await Task.Delay(300);

        var d2 = dueno.OwnedWindows.OfType<Dialogo>().FirstOrDefault();

        // Se pulsa ESC de verdad, no se llama a Close(). Llamar a Close() sin argumento
        // devuelve false por el framework, no por este codigo: la comprobacion habria
        // pasado igual con el manejador de Esc borrado o puesto en true. Es el mismo
        // error que ya se colo una vez en la escala del codificador — una prueba que no
        // puede fallar por culpa del codigo que dice verificar.
        d2?.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Escape,
        });

        Dice(!await otra,
            "cerrar con Esc cuenta como NO — se pregunta antes de tocar ficheros, " +
            "y un «cancelar» leido como «si» borraria cosas");
    }
}
