using Avalonia;
using Visual = Avalonia.Visual;
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
    /// Recortes: que abra sin video y no prometa nada que no pueda hacer.
    ///
    /// <para>
    /// Esta pantalla <b>corta ficheros</b>, asi que su estado de partida importa mas que el de
    /// las otras: con los botones de exportar vivos y sin video cargado, pulsar seria pedirle
    /// que corte la nada. Lo que se mira es que arranque apagada.
    /// </para>
    /// <para>
    /// Y que la pista este montada. Es un Canvas dibujado a mano —bloques, juntas y cabezal
    /// colocados por pixel contra la duracion— y si no cuajara al portar, la pantalla abre
    /// con una franja vacia donde deberia estar la linea de tiempo.
    /// </para>
    /// </summary>
    public static async Task CorrerRecortes(Window dueno)
    {
        var vista = new RecortesView();
        var v = new Window { Width = 1100, Height = 720, Content = vista };
        v.Show(dueno);
        await Task.Delay(600);

        Control? Cual(string n) => vista.GetVisualDescendants().OfType<Control>()
                                       .FirstOrDefault(c => c.Name == n);

        Dice(true, "la pantalla de Recortes abre sin reventar");

        // Sin video: lo que corta esta apagado.
        var exportar = Cual("btnExportar") as Button;
        Dice(exportar is not null, "el boton de exportar esta");
        Dice(exportar?.IsEnabled == false,
            "y arranca apagado: sin video cargado no hay nada que cortar");

        // La pista existe y tiene alto: es un Canvas a mano, no un control de serie.
        var pista = Cual("pista");
        Dice(pista is not null, "la pista esta montada");
        Dice(pista?.Bounds.Height > 10, $"y ocupa su alto ({pista?.Bounds.Height:0})");

        // El rotulo dice que no hay video, en vez de dejar el hueco en blanco.
        var lbl = vista.GetVisualDescendants().OfType<TextBlock>()
                       .FirstOrDefault(t => t.Name == "lblVideo");
        Dice(!string.IsNullOrWhiteSpace(lbl?.Text),
            $"y se dice que no hay video en vez de dejarlo en blanco ({lbl?.Text})");

        v.Close();
    }

    /// <summary>
    /// Organizar: que la pantalla abra y pinte su estado de partida.
    ///
    /// <para>
    /// Es la mas grande de la app y la unica que se monta sola —lee los catalogos del disco y
    /// se pinta—, asi que lo primero que hay que saber es si <b>abre sin reventar</b>. Con
    /// mil doscientas lineas de XAML, un recurso que falte o una plantilla mal cerrada
    /// aparecen aqui y en ningun otro sitio.
    /// </para>
    /// <para>
    /// Y que arranque EN INICIO: la pantalla tiene dos caras —elegir carpeta y catalogo, o la
    /// tabla de repaso— y si arrancara en la de repaso saldria una tabla vacia sin explicar
    /// por que. Eso no da error, solo desconcierta.
    /// </para>
    /// </summary>
    public static async Task CorrerOrganizar(Window dueno)
    {
        var vista = new OrganizarView();
        var v = new Window { Width = 1200, Height = 780, Content = vista };
        v.Show(dueno);
        await Task.Delay(700);

        Control? Cual(string n) => vista.GetVisualDescendants().OfType<Control>()
                                       .FirstOrDefault(c => c.Name == n);

        Dice(true, "la pantalla de Organizar abre sin reventar");

        // Arranca en inicio, no en la tabla. Los dos se llaman vistaInicio y vistaRevision;
        // el primer intento buscaba «pagInicio», que no existe — y buscar algo que no esta
        // devuelve null, que se lee igual que «esta oculto». Otra vez lo mismo: una
        // comprobacion que no encuentra su objetivo acusa al codigo de lo que hace ella.
        var inicio = Cual("vistaInicio");
        var repaso = Cual("vistaRevision");
        Dice(inicio is not null, "la vista de inicio existe con ese nombre");
        Dice(inicio?.IsVisible == true, "y arranca en ella");
        Dice(repaso is not null && repaso.IsVisible == false,
            "no en la tabla de repaso, que sin analizar estaria vacia");

        // Las dos tablas existen con sus columnas: es lo que sostiene el repaso.
        var tablas = vista.GetVisualDescendants().OfType<DataGrid>().ToList();
        Dice(tablas.Count >= 1, $"la tabla del repaso esta montada ({tablas.Count})");
        var conColumnas = tablas.FirstOrDefault(t => t.Columns.Count > 3);
        Dice(conColumnas is not null,
            $"con sus columnas ({conColumnas?.Columns.Count})");

        // El recorrido de pasos: tres etapas, montadas en codigo.
        var etapas = Cual("panelEtapas");
        Dice(etapas is not null && etapas.GetVisualDescendants().OfType<TextBlock>().Count() >= 3,
            "el recorrido de etapas esta puesto, con sus tres pasos");

        v.Close();
    }

    /// <summary>
    /// El tema de la tabla de Organizar, antes de que haya ninguna pantalla que lo use.
    ///
    /// <para>
    /// La plantilla de la fila es la pieza mas fragil del puerto entero. El DataGrid de
    /// Avalonia busca sus partes <b>por nombre</b> —PART_Root, PART_CellsPresenter,
    /// PART_DetailsPresenter, PART_BottomGridLine— y si falta una, <b>la fila no pinta y no
    /// hay ningun error</b>: la tabla sale en blanco con los datos perfectamente cargados.
    /// </para>
    /// <para>
    /// Se comprueba aqui y no al portar la pantalla a proposito: si se mezclara con mil
    /// doscientas lineas de XAML nuevo, una tabla vacia no diria si el fallo es del tema o
    /// de la pantalla.
    /// </para>
    /// </summary>
    public static async Task CorrerTablaDeOrganizar(Window dueno)
    {
        // Dos filas de mentira con lo justo que la plantilla mira: el grupo y si abre banda.
        //
        // Con un TIPO DE VERDAD y no uno anonimo. El primer intento usaba anonimos y la
        // banda salia sobre las dos filas: un tipo anonimo es interno, el enlace no alcanza
        // sus propiedades, y un enlace que no resuelve deja el valor por defecto —IsVisible
        // en true—. O sea que el fallo era del fixture y acusaba al tema.
        var filas = new[]
        {
            new FilaDePrueba(true,  "Temporada 1", "2 ficheros", "cap01.mkv"),
            new FilaDePrueba(false, "Temporada 1", "",           "cap02.mkv"),
        };

        var tabla = new DataGrid
        {
            Theme = Avalonia.Application.Current!.TryFindResource("OrgGrid", out var t)
                ? t as Avalonia.Styling.ControlTheme : null,
            ItemsSource = filas,
        };
        tabla.Columns.Add(new DataGridTextColumn
        {
            Header = "Fichero",
            Binding = new Avalonia.Data.Binding("Nombre"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
        });

        // El tema de la fila va como estilo suelto: el DataGrid de Avalonia no expone nada
        // como el RowStyle de WPF. Es asi como lo va a poner la pantalla de verdad.
        var v = new Window { Width = 700, Height = 300, Content = tabla };
        if (Avalonia.Application.Current.TryFindResource("OrgRow", out var tf)
            && tf is Avalonia.Styling.ControlTheme temaFila)
        {
            var estilo = new Avalonia.Styling.Style(x => Avalonia.Styling.Selectors.Is<DataGridRow>(x));
            estilo.Setters.Add(new Avalonia.Styling.Setter(DataGridRow.ThemeProperty, temaFila));
            v.Styles.Add(estilo);
        }

        v.Show(dueno);
        await Task.Delay(500);

        var filasPintadas = tabla.GetVisualDescendants().OfType<DataGridRow>().ToList();
        Dice(filasPintadas.Count == 2,
            $"la tabla pinta sus dos filas ({filasPintadas.Count})");

        // Y que dentro de la fila esten las CELDAS. Sin PART_CellsPresenter la fila existe,
        // ocupa su alto y esta vacia — que es el fallo que esto viene a coger.
        var celdas = tabla.GetVisualDescendants().OfType<DataGridCell>().ToList();
        Dice(celdas.Count >= 2, $"con sus celdas dentro, no filas vacias ({celdas.Count})");

        var textos = tabla.GetVisualDescendants().OfType<TextBlock>()
                          .Select(x => x.Text ?? "").ToList();
        Dice(textos.Any(x => x.Contains("cap01")), "y el contenido de la celda llega");

        // La banda de temporada: la primera fila la abre y la segunda no.
        //
        // Se cuentan las BANDAS y su IsVisible, no los textos. Contar textos no valia:
        // GetVisualDescendants devuelve tambien los hijos de lo oculto —IsVisible quita del
        // dibujo, no del arbol— asi que los dos rotulos aparecian y parecia que la banda
        // salia sobre las dos filas. La comprobacion acusaba al tema de algo que hacia ella.
        var bandas = tabla.GetVisualDescendants().OfType<Border>()
                          .Where(b => b.Name == "banda").ToList();
        Dice(bandas.Count == 2, $"cada fila trae su banda en la plantilla ({bandas.Count})");
        Dice(bandas.Count(b => b.IsVisible) == 1,
            $"pero solo se ve la de la fila que abre grupo ({bandas.Count(b => b.IsVisible)} de 2)");

        var visible = bandas.FirstOrDefault(b => b.IsVisible);
        var rotulo = visible?.GetVisualDescendants().OfType<TextBlock>()
                             .Select(x => x.Text ?? "").ToList() ?? [];
        Dice(rotulo.Any(x => x.Contains("Temporada 1")) && rotulo.Any(x => x.Contains("2 ficheros")),
            "y dice de qué temporada es y cuántos ficheros trae");

        // La cabecera, con su tema.
        var cabeceras = tabla.GetVisualDescendants().OfType<DataGridColumnHeader>().ToList();
        Dice(cabeceras.Count >= 1, $"y la cabecera de columna esta ({cabeceras.Count})");

        v.Close();
    }

    /// <summary>Una fila para la prueba de la tabla. Publica: un enlace no ve lo interno.</summary>
    public sealed record FilaDePrueba(bool PrimeraDeGrupo, string Grupo, string GrupoConteo, string Nombre);

    /// <summary>
    /// Que la tipografia LLEGUE a la pantalla, no solo que este definida.
    ///
    /// <para>
    /// Que las dos fuentes existan ya lo vigila RecursosQueNoExistenTests leyendo ficheros.
    /// Lo que eso no puede ver es si alguien las APLICA: en WPF cada ventana pedia la suya en
    /// su cabecera y al portar se cayo en todas, asi que las pantallas heredaban la de serie
    /// de Fluent. Una fuente que no se pone no da error, sale otra y ya.
    /// </para>
    /// <para>
    /// Aqui se abre una ventana de verdad y se mira con que fuente ha quedado.
    /// </para>
    /// </summary>
    public static async Task CorrerTipografia(Window dueno)
    {
        var v = new Faltantes();
        v.Show(dueno);
        await Task.Delay(250);

        // Se compara contra el RECURSO, no contra una ventana recien creada. El primer
        // intento hacia lo segundo y no valia: una ventana sin mostrar todavia no tiene los
        // estilos aplicados, asi que su fuente era otra tercera cosa y la comparacion salia
        // bien tanto con el estilo puesto como sin el. Se vio quitando el estilo a proposito.
        var pedida = Avalonia.Application.Current!.TryFindResource("FontUI", out var r)
            ? (r as FontFamily)?.Name : null;

        Dice(pedida is not null, "la fuente de la interfaz esta en el tema");
        Dice(v.FontFamily.Name == pedida,
            $"y es la que lleva puesta la ventana ({v.FontFamily.Name}, se pedia {pedida})");

        v.Close();

        // Y la monoespaciada, que es la que de verdad se nota: las columnas de rutas y
        // codigos van alineadas por ancho de caracter, y con una proporcional se tuercen.
        //
        // Se mira en una pantalla que la USE. El primer intento la buscaba en «que falta»,
        // que no lleva ni un texto monoespaciado, asi que fallaba sobre codigo correcto:
        // buscar algo donde no lo hay no es comprobar, es tener suerte.
        var catalogo = Ondine.Reindex.ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0",
          "serie": "Serie de prueba",
          "episodios": [ { "num": 1, "temporada": 1, "titulos": { "es": ["Uno"] } } ]
        }
        """);
        var conMono = new Catalogo(catalogo);
        conMono.Show(dueno);
        await Task.Delay(300);

        var familias = conMono.GetVisualDescendants().OfType<TextBlock>()
                              .Select(t => t.FontFamily.Name).Distinct().ToList();
        Dice(familias.Count > 1,
            $"y hay texto en dos fuentes distintas, no todo en la misma ({string.Join(", ", familias)})");

        conMono.Close();
    }

    /// <summary>
    /// El panel de complementos: el indice que se retira y los tres estados de la derecha.
    ///
    /// <para>
    /// Lo que se vigila es el <b>indice que se encoge</b>. A 290 px fijos, en un panel de 460
    /// dejan al detalle 170: ahi la descripcion cae a seis lineas y los elementos se ven en
    /// miniaturas sin titulo. Si al portar se rompiera, el panel estrecho no protesta —sale
    /// apretado y punto—, y quien lo mire pensara que la pantalla es asi.
    /// </para>
    /// <para>
    /// Y que solo haya UNA cosa encima a la vez. En WPF cada boton tocaba visibilidades por
    /// su cuenta y se podia acabar con la lista y la tienda pintadas juntas; eso se resolvio
    /// dejando que un solo sitio decida. Aqui se comprueba que sigue siendo asi.
    /// </para>
    /// </summary>
    public static async Task CorrerComplementos(Window dueno)
    {
        // Sin catalogo ni carpeta analizada: es el estado en que se abre normalmente.
        var panel = new ComplementosPanel(() => new ComplementosPanel.EstadoDeOrganizar(null, [], ""));
        var v = new Window { Width = 900, Height = 560, Content = panel };
        v.Show(dueno);
        await Task.Delay(400);

        Control? Cual(string n) => panel.GetVisualDescendants().OfType<Control>()
                                        .FirstOrDefault(c => c.Name == n);

        // Ancho: con 900 px el indice esta puesto.
        var rejilla = panel.GetVisualDescendants().OfType<Grid>().FirstOrDefault(g => g.Name == "rejilla");
        Dice(Cual("cajaIndice")?.IsVisible == true, "con sitio, el indice de la izquierda esta");
        Dice(rejilla?.ColumnDefinitions[0].Width.Value > 200,
            $"y ocupa su columna ({rejilla?.ColumnDefinitions[0].Width.Value})");

        // Estrecho: se retira. El indice sirve para SALTAR entre complementos, y con dos o
        // tres eso se hace una vez; lo que se mira todo el rato es el detalle.
        v.Width = 500;
        await Task.Delay(400);
        Dice(Cual("cajaIndice")?.IsVisible == false, "al estrecharse, el indice se retira");
        Dice(rejilla?.ColumnDefinitions[0].Width.Value == 0,
            "y su columna se queda a cero, sin dejar un hueco muerto");

        v.Width = 900;
        await Task.Delay(400);
        Dice(Cual("cajaIndice")?.IsVisible == true, "y vuelve al ensancharse");

        // Una sola cosa encima a la vez: sin complemento elegido, el hueco central dice lo
        // que hay que hacer y ni la lista ni la tienda estan pintadas.
        int encima = new[] { "lista", "listaTienda" }.Count(n => Cual(n)?.IsVisible == true);
        Dice(encima == 0 && Cual("cajaEstado")?.IsVisible == true,
            $"sin nada elegido solo esta el mensaje del medio ({encima} listas encima)");

        v.Close();
    }

    /// <summary>
    /// El panel de peliculas: los cuatro estados del rotulo.
    ///
    /// <para>
    /// Es la pantalla mas simple del puerto —una carpeta y una accion— y aun asi tiene una
    /// trampa: el rotulo cambia segun no haya carpeta, no haya videos, haya uno o haya
    /// varios. Cuatro frases distintas para cuatro situaciones, y en castellano el singular
    /// no es la plural con un uno delante. La unica manera de que se caiga una es no
    /// mirarlas.
    /// </para>
    /// <para>
    /// Tambien se mira que el CAMPO CON ICONO pinte su icono. Es un control propio y su
    /// propiedad se registra distinto en Avalonia: si el enlace no la encuentra, el campo
    /// sale igual pero sin dibujo, <b>y no da ningun error</b>.
    /// </para>
    /// </summary>
    public static async Task CorrerPeliculas(Window dueno)
    {
        var panel = new PeliculasPanel();
        var v = new Window
        {
            Width = 640, Height = 420,
            Content = panel,
        };
        v.Show(dueno);
        await Task.Delay(300);

        var ficheros = panel.GetVisualDescendants().OfType<TextBlock>()
                            .FirstOrDefault(t => t.Name == "lblFicheros");
        var boton = panel.GetVisualDescendants().OfType<Button>()
                         .FirstOrDefault(b => b.Name == "btnOrdenar");

        // Sin carpeta: se pide elegir una, y no hay nada que analizar.
        panel.Poner("", []);
        await Task.Delay(150);
        var sinCarpeta = ficheros?.Text ?? "";
        Dice(sinCarpeta.Length > 0, $"sin carpeta se dice que hay que elegir una ({sinCarpeta})");
        Dice(boton?.IsEnabled == false, "y el boton de analizar esta apagado");

        // Carpeta que existe pero sin videos: OTRA cosa distinta de no tener carpeta.
        panel.Poner(Path.GetTempPath(), []);
        await Task.Delay(150);
        var sinVideos = ficheros?.Text ?? "";
        Dice(sinVideos != sinCarpeta,
            $"«no hay videos» no se dice igual que «elige carpeta» ({sinVideos})");
        Dice(boton?.IsEnabled == false, "y sigue sin haber nada que analizar");

        // Con una: el singular tiene su propia frase.
        panel.Poner(Path.GetTempPath(), ["una.mkv"]);
        await Task.Delay(150);
        var conUna = ficheros?.Text ?? "";
        Dice(boton?.IsEnabled == true, "con una pelicula el boton se enciende");

        // Con varias: otra frase, no la misma con un numero.
        panel.Poner(Path.GetTempPath(), ["una.mkv", "otra.mkv", "y otra.mkv"]);
        await Task.Delay(150);
        var conVarias = ficheros?.Text ?? "";
        Dice(conVarias != conUna,
            $"y con varias cambia la frase, no solo el numero ({conVarias})");
        Dice(conVarias.Contains('3'), "con la cuenta dentro");

        // El campo con icono: es un control propio y su propiedad se registra distinto.
        var campo = panel.GetVisualDescendants().OfType<CampoTexto>().FirstOrDefault();
        Dice(campo?.Icono is not null, "el campo de la carpeta lleva su icono");
        Dice(campo?.Text == Path.GetTempPath(), "y la ruta puesta");

        v.Close();
    }

    /// <summary>
    /// El reproductor, con un video de verdad hecho al vuelo.
    ///
    /// <para>
    /// Con video de verdad y no con una ruta inventada porque lo que hay que comprobar es
    /// que <b>LibVLC abre y reporta</b>: es la pieza que sustituye al MediaElement, y si no
    /// arrancara, todo lo demas de esta pantalla daria igual. Se hace uno de dos segundos
    /// con el ffmpeg que ya lleva la app.
    /// </para>
    /// <para>
    /// Lo otro que se mira es lo que <b>solo se nota con el tiempo pasando</b>: que los
    /// controles se aparten solos y vuelvan al mover el raton. Un puerto puede dejar eso
    /// muerto sin que nada se queje — los controles se quedan puestos y parece una decision.
    /// </para>
    /// </summary>
    public static async Task CorrerReproductor(Window dueno)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-auto-repro");
        Directory.CreateDirectory(carpeta);
        var mp4 = Path.Combine(carpeta, "prueba.mp4");

        try
        {
            if (!await HacerUnVideoDePrueba(mp4))
            {
                Dice(false, "no se pudo montar el video de prueba (¿falta ffmpeg?)");
                return;
            }

            var v = new Reproductor(mp4);
            v.Show(dueno);

            // LibVLC tarda en arrancar: abre el fichero, negocia salida de video y solo
            // entonces reporta duracion. Se le da margen de sobra.
            await Task.Delay(2500);

            // POR NOMBRE Y NO POR EL ARBOL VISUAL, y el motivo es del reproductor.
            //
            // Todo lo que va encima del video vive dentro de VideoView.Content, porque un
            // NativeControlHost se pinta por encima de lo que dibuja Avalonia y como hermanos
            // los controles quedaban tapados. Content se hospeda en una capa aparte, asi que
            // esos controles YA NO SON descendientes visuales de la ventana: buscarlos por el
            // arbol devuelve null, y un null aqui se lee igual que «esta mal».
            //
            // FindControl busca por el ambito de nombres del XAML, que es el mismo, y es lo
            // que usa el propio reproductor para todo. Se le sigue.
            var dur = v.FindControl<TextBlock>("lblDur");
            var barra = v.FindControl<Slider>("barra");
            var fallo = v.FindControl<StackPanel>("panelFallo");

            Dice(fallo?.IsVisible == false, "el video abre: no sale el panel de fallo");
            Dice(barra?.Maximum > 0.5, $"y LibVLC reporta la duracion ({barra?.Maximum:0.0}s)");
            Dice(dur?.Text is { Length: > 0 } and not "0:00", $"que llega al rotulo ({dur?.Text})");

            // La barra lleva su tema: sin el, el Slider de Fluent sale con otra forma y
            // sin el tirador que la previa necesita medir.
            var tirador = barra?.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();
            Dice(tirador is not null, "la barra lleva su tirador, que es lo que mide la previa");

            // ── Los controles que se apartan ──
            // Se miran las DOS direcciones y no solo una. El primer intento comprobaba «al
            // abrir estan a la vista» y fallaba sobre un codigo correcto: LibVLC tarda mas
            // de los 2,6 s del apagon en arrancar, asi que para cuando se miraba ya se
            // habian escondido con toda la razon. Medir contra el reloj de arranque de otro
            // no mide nada.
            var abajo = v.FindControl<Grid>("capaInferior");

            // Quieto: se esconden.
            await Task.Delay(3200);
            Dice(abajo?.Opacity < 0.1,
                $"los controles se apartan solos tras un rato quieto ({abajo?.Opacity:0.00})");

            // Y vuelven. Esta es la mitad que de verdad importa: unos controles que se
            // esconden y no vuelven dejan la ventana inservible.
            //
            // Se despiertan con una tecla y no con el raton porque fabricar un movimiento de
            // raton en Avalonia exige un IPointer, que no hay de donde sacar sin un
            // dispositivo de verdad. La tecla entra por el mismo sitio -las dos llaman a
            // Mostrar()-, asi que lo que se comprueba, que es la maquinaria de esconder y
            // volver, es lo mismo. Lo que NO queda comprobado aqui es el filtro de los 2 px
            // del raton; eso solo lo dice una mano.
            v.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.M });
            await Task.Delay(400);
            Dice(abajo?.Opacity > 0.9,
                $"y vuelven en cuanto se toca algo ({abajo?.Opacity:0.00})");

            v.Close();
        }
        finally
        {
            try { Directory.Delete(carpeta, true); } catch { }
        }
    }

    /// <summary>Dos segundos de patron de prueba, con el ffmpeg que ya lleva la app.</summary>
    private static async Task<bool> HacerUnVideoDePrueba(string destino)
    {
        try
        {
            var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                Ondine.Engine.FfmpegPath,
                $"-y -f lavfi -i testsrc=size=320x180:rate=10:duration=2 " +
                $"-c:v libx264 -pix_fmt yuv420p \"{destino}\"")
            { UseShellExecute = false, CreateNoWindow = true });
            if (p is null) return false;
            await p.WaitForExitAsync();
            return File.Exists(destino) && new FileInfo(destino).Length > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// El explorador de catalogos: el buscador y el panel del JSON.
    ///
    /// <para>
    /// Lo que se vigila del JSON no es que aparezca: es que <b>lo pintado sea el JSON</b>.
    /// El panel tiene al lado un boton de copiar que copia el texto de verdad, asi que si
    /// el coloreado se comiera un trozo, lo que se ve y lo que se pega dirian cosas
    /// distintas — y de las dos, la que se cree es la que se ve.
    /// </para>
    /// </summary>
    public static async Task CorrerCatalogo(Window dueno)
    {
        var catalogo = Ondine.Reindex.ReindexCatalog.Parse("""
        {
          "esquema": "reindex/1.0",
          "serie": "Serie de prueba",
          "episodios": [
            { "num": 1, "temporada": 1, "titulos": { "es": ["El planeta espejo"] } },
            { "num": 2, "temporada": 1, "titulos": { "es": ["La playa", "El armario"] } },
            { "num": 3, "temporada": 2, "titulos": { "es": ["Otra cosa"] } }
          ]
        }
        """);

        var v = new Catalogo(catalogo);
        v.Show(dueno);
        await Task.Delay(400);

        var lista = v.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
        List<object> Filas() =>
            (lista?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? [];

        Dice(Filas().Count == 3, $"salen los tres episodios ({Filas().Count})");

        // Sin carpeta analizada detras, el filtro de «los que faltan» no se ofrece: seria
        // decir que faltan todos sin haber mirado ningun disco.
        var soloFaltan = v.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault();
        Dice(soloFaltan?.IsVisible == false,
            "y sin carpeta detras no se ofrece filtrar por «los que faltan»");

        // El de dos historias trae DOS lineas, cada una con su letra. Es la razon de ser de
        // esta pantalla: que se vea que el capitulo trae dos y no un titulo larguisimo.
        var dos = Filas().OfType<EpisodioVista>().First(e => e.Ep.Num == 2);
        Dice(dos.Segmentos.Count == 2 && dos.Segmentos[0].Codigo == "E2a",
            $"el de dos historias sale con una linea por historia ({dos.Segmentos[0].Codigo})");

        // ── El buscador ──
        var buscar = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "txtBuscar");
        buscar!.Text = "armario";
        await Task.Delay(250);
        Dice(Filas().Count == 1, $"buscar acota la lista ({Filas().Count})");

        var cuenta = v.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "lblCuenta");
        Dice(cuenta?.Text?.Contains("1") == true, $"y la cuenta lo dice ({cuenta?.Text})");

        // ── El panel del JSON ──
        buscar.Text = "";
        await Task.Delay(200);
        lista!.SelectedIndex = 0;
        await Task.Delay(300);

        var borde = v.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "bordeJson");
        Dice(borde?.IsVisible == true, "elegir una fila abre el panel del JSON");

        var caja = v.GetVisualDescendants().OfType<SelectableTextBlock>().FirstOrDefault();
        var pintado = string.Concat(caja?.Inlines?.OfType<Avalonia.Controls.Documents.Run>()
                                        .Select(r => r.Text) ?? []);
        Dice(pintado.Contains("planeta espejo") && pintado.TrimStart().StartsWith('{'),
            $"y lo pintado es el JSON del episodio ({pintado.Length} caracteres)");

        // Mas de un color: si todo saliera del mismo, el coloreado no estaria pasando por
        // el motor y nadie lo notaria — el JSON se lee igual de gris.
        var colores = caja?.Inlines?.OfType<Avalonia.Controls.Documents.Run>()
                          .Select(r => r.Foreground?.ToString()).Distinct().Count() ?? 0;
        Dice(colores >= 3, $"con varios colores y no todo del mismo ({colores})");

        // Y cerrar el panel deselecciona: si la fila siguiera elegida, volver a pincharla no
        // dispararia el cambio de seleccion y el panel no reapareceria.
        v.GetVisualDescendants().OfType<Button>().First(b => b.Name == "btnCerrarJson")
         .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(200);
        Dice(borde?.IsVisible == false && lista.SelectedItem is null,
            "cerrar el panel tambien deselecciona, para poder volver a abrirlo");

        v.Close();
    }

    /// <summary>
    /// Preferencias: que lo que entra vuelva a salir.
    ///
    /// <para>
    /// Aqui hay veinte controles y cada uno se carga y se guarda por su cuenta. El fallo
    /// tipico de una pantalla asi no es que reviente: es que UNO se quede sin cablear al
    /// portar, y entonces ese ajuste vuelve a su valor de fabrica cada vez que alguien abre
    /// preferencias a tocar otra cosa. Nadie lo relaciona.
    /// </para>
    /// <para>
    /// La comprobacion es un viaje de ida y vuelta: se abre con unos valores, se pulsa
    /// guardar sin tocar nada, y tienen que salir los mismos.
    /// </para>
    /// <para>
    /// <b>Y se hace DOS VECES, todo encendido y todo apagado.</b> Eso se aprendio
    /// rompiendolo: con una sola pasada, desconectar la casilla de «buscar actualizaciones»
    /// no hacia saltar nada, porque se estaba probando con ella apagada — y una casilla sin
    /// cablear tambien sale apagada. Un booleano probado contra su propio valor de fabrica
    /// no prueba nada. Con las dos pasadas, cualquiera que se quede suelto falla en una.
    /// </para>
    /// </summary>
    public static async Task CorrerPreferencias(Window dueno)
    {
        var idiomaAntes = Ondine.Localizacion.Idioma.Actual;
        try
        {
            await UnViajeDeIdaYVuelta(dueno, true);
            await UnViajeDeIdaYVuelta(dueno, false);
        }
        finally { Ondine.Localizacion.Idioma.Actual = idiomaAntes; }
    }

    private static async Task UnViajeDeIdaYVuelta(Window dueno, bool encendido)
    {
        var como = encendido ? "encendido" : "apagado";

        var entra = new Ondine.Settings
        {
            DefaultLang = encendido ? "jpn" : "cat",
            Recurse = encendido,
            CheckUpdatesOnStart = encendido,
            AfterCompress = encendido ? Ondine.AfterCompress.Keep : Ondine.AfterCompress.RecycleOriginal,
            MinFreeMb = encendido ? 4321 : 1234,
            UseHardware = encendido,
        };
        entra.Ia.Activo = encendido;
        entra.Ia.BaseUrl = "http://ejemplo.invalido/v1";
        entra.Ia.Modelo = "un-modelo-de-prueba";
        entra.Tmdb.Activo = encendido;

        // Algo que esta ventana NO edita: tiene que seguir ahi al volver. Es la regla de
        // partir de los ajustes de entrada en vez de construir unos nuevos, que ya costo
        // una vez el historial de renombrado entero.
        entra.RenameSearchHistory = ["1080p", "BluRay"];

        var v = new Preferencias(entra, ["Archivar", "Movil"]);
        v.Show(dueno);
        await Task.Delay(400);

        // Guardar sin tocar nada.
        v.GetVisualDescendants().OfType<Button>().First(b => b.Name == "btnSave")
         .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(250);

        var sale = v.Result;
        Dice(sale is not null, $"guardar devuelve unos ajustes ({como})");
        if (sale is null) return;

        Dice(sale.DefaultLang == entra.DefaultLang,
            $"el idioma de audio vuelve igual, {como} ({sale.DefaultLang})");
        Dice(sale.Recurse == encendido, $"y la casilla de subcarpetas, {como}");
        Dice(sale.CheckUpdatesOnStart == encendido, $"y la de buscar actualizaciones, {como}");
        Dice(sale.AfterCompress == entra.AfterCompress,
            $"y el que de los tres redondos estaba elegido ({sale.AfterCompress})");
        Dice(sale.MinFreeMb == entra.MinFreeMb, $"y el margen de disco ({sale.MinFreeMb})");
        Dice(sale.UseHardware == encendido, $"y la aceleracion, {como}");
        Dice(sale.Ia.Activo == encendido && sale.Ia.BaseUrl == entra.Ia.BaseUrl
             && sale.Ia.Modelo == entra.Ia.Modelo, $"y los tres del modelo, {como}");
        Dice(sale.Tmdb.Activo == encendido, $"y el de peliculas, {como}");

        // Lo que la ventana no edita sigue ahi: se parte de los ajustes de entrada.
        Dice(sale.RenameSearchHistory.Count == 2,
            $"y lo que esta ventana NO edita no se pierde ({sale.RenameSearchHistory.Count})");

        v.Close();
    }

    /// <summary>
    /// La Ayuda: que se cambie de tutorial y que el texto llegue a la pantalla.
    ///
    /// <para>
    /// Que no falte ningun parrafo ya lo vigila AyudaPortadaTests comparando las dos
    /// versiones. Lo que se mira aqui es lo otro: que el indice CAMBIE de pagina -son
    /// cuatro paneles apilados y solo uno visible; si el enganche se cae, se queda siempre
    /// el primero y parece que los otros tutoriales no existen- y que los textos partidos
    /// en trozos con Run se pinten. Eso ultimo no es paranoia: un Run no es un control
    /// normal y su texto podria quedarse vacio sin que nada se queje.
    /// </para>
    /// </summary>
    public static async Task CorrerAyuda(Window dueno)
    {
        var v = new Ayuda();
        v.Show(dueno);
        await Task.Delay(350);

        string[] paginas = ["pagOrgComo", "pagOrgPasos", "pagComprimir", "pagRecortes"];
        List<string> Visibles() =>
            paginas.Where(p => v.FindControl<StackPanel>(p)?.IsVisible == true).ToList();

        Dice(Visibles().SequenceEqual(["pagOrgComo"]),
            $"al abrir se ve un solo tutorial, el primero ({string.Join(",", Visibles())})");

        // Cambiar de entrada en el indice. Si el enganche se cayo al portar, se queda el
        // primero y la Ayuda parece tener un unico tutorial.
        v.FindControl<RadioButton>("navRecortes")!.IsChecked = true;
        await Task.Delay(200);
        Dice(Visibles().SequenceEqual(["pagRecortes"]),
            $"elegir otro cambia de pagina y deja UNA sola ({string.Join(",", Visibles())})");

        // Y volver: el grupo tiene que apagar la anterior. Dos visibles a la vez seria un
        // tutorial pegado debajo del otro, que es peor que no cambiar.
        v.FindControl<RadioButton>("navComprimir")!.IsChecked = true;
        await Task.Delay(200);
        Dice(Visibles().SequenceEqual(["pagComprimir"]),
            $"y volver a cambiar no deja dos pegadas ({string.Join(",", Visibles())})");

        // La leyenda de colores va partida en Runs -un punto de color y su explicacion,
        // tres veces-. Es el unico texto de la app hecho asi.
        v.FindControl<RadioButton>("navOrgComo")!.IsChecked = true;
        await Task.Delay(200);
        var conRuns = v.GetVisualDescendants().OfType<TextBlock>()
                       .FirstOrDefault(t => t.Inlines is { Count: > 3 });
        var leyenda = conRuns?.Inlines?.Text ?? "";
        Dice(leyenda.Length > 20,
            $"los textos partidos en Runs llegan pintados ({leyenda.Length} caracteres)");

        v.Close();
    }

    /// <summary>
    /// «Generar el catalogo con una IA»: los idiomas del encargo.
    ///
    /// <para>
    /// Lo que se vigila es <b>el boton que vive dentro de una plantilla</b>. En WPF llevaba
    /// el codigo duplicado en un Tag; aqui sale del DataContext de la fila. Si eso no
    /// llegara, pulsar no haria nada — y «no hace nada» es justo lo que no se distingue de
    /// «lo he pulsado mal».
    /// </para>
    /// </summary>
    public static async Task CorrerEncargo(Window dueno)
    {
        var v = new Encargo("Doraemon");
        v.Show(dueno);
        await Task.Delay(350);

        var chips = v.GetVisualDescendants().OfType<ItemsControl>()
                     .FirstOrDefault(c => c.Name == "listaSeleccionados");
        int Chips() => (chips?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Count() ?? -1;

        var prompt = v.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Name == "txtPrompt");
        var aviso = v.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "lblAviso");

        Dice(Chips() == 2, $"arranca con los dos idiomas de casa ({Chips()})");
        Dice(prompt?.Text?.Contains("Doraemon") == true, "y el encargo ya lleva el nombre de la serie");

        // Abrir el emergente. El boton no lo abre por codigo: lo abre el enlace de dos
        // sentidos entre su IsChecked y el IsOpen del emergente, asi que marcarlo tambien
        // comprueba ese enlace — que si se rompe deja un boton que no hace nada.
        var mas = v.GetVisualDescendants().OfType<ToggleButton>()
                   .FirstOrDefault(b => b.Name == "btnAnadirIdioma");
        var pop = v.FindControl<Popup>("popIdiomas");
        mas!.IsChecked = true;
        await Task.Delay(300);

        Dice(pop?.IsOpen == true, "marcar el «+» abre el emergente: el enlace de dos sentidos esta puesto");

        // Y aqui una diferencia de Avalonia que cuesta una tarde: el contenido de un Popup
        // NO cuelga de la ventana, vive en su propio arbol visual. Buscar desde la ventana
        // no encuentra nada y parece que la lista esta vacia. Se busca desde su hijo.
        var dentro = pop?.Child as Visual;
        var filas = dentro is null ? [] : dentro.GetVisualDescendants().OfType<Button>()
                     .Where(b => b.Name == "btnAlternarIdioma").ToList();
        Dice(filas.Count > 50, $"la lista de idiomas se puebla ({filas.Count})");

        // El de la primera fila que NO este ya elegido: pulsarlo tiene que anadir uno.
        var libre = filas.FirstOrDefault(b => b.DataContext is IdiomaFila { Elegido: false });
        var comoSeLlama = (libre?.DataContext as IdiomaFila)?.Nombre ?? "";
        libre?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(200);

        Dice(Chips() == 3,
            $"pulsar una fila anade el idioma ({comoSeLlama} → {Chips()} insignias)");
        Dice(aviso?.Text?.Contains("3") == true,
            $"y el aviso lleva la cuenta nueva ({aviso?.Text})");

        v.Close();
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
            List<object> Todas() =>
                (lista?.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? [];
            int Filas() => Todas().Count;

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

            // El color de la insignia se pide con TryFindResource, que si no encuentra el
            // color devuelve gris en vez de tumbar la ventana. Eso es lo que se queria...
            // y es lo que tapo que los once colores de estado no estuvieran en la paleta:
            // TODAS las insignias salian grises y nada protestaba. Asi que se comprueba que
            // el gris de reserva NO se este usando.
            var insignia = (Todas().FirstOrDefault() as ReordenVista)?.Color;
            Dice(insignia is not null && !ReferenceEquals(insignia, Avalonia.Media.Brushes.Gray),
                "la insignia coge su color del tema y no el gris de reserva");

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

    /// <summary>
    /// La ventana principal: que abra, que arranque donde debe y que la tabla PINTE.
    ///
    /// <para>
    /// Es la última del puerto y la que aloja a las demás, así que esto es también lo más
    /// cerca que se puede estar de arrancar la aplicación: novecientas líneas de XAML donde
    /// un recurso que falte, una plantilla mal cerrada o un tema que reemplace en vez de
    /// heredar aparecen aquí y en ningún otro sitio.
    /// </para>
    /// <para>
    /// <b>Lo que se mira de la tabla es la CELDA pintada, no la fila del modelo.</b> Ese
    /// error ya se cometió una vez en esta misma tanda: preguntar al objeto de datos pasa
    /// aunque el DataGrid se haya quedado sin plantilla y la tabla salga en blanco, que es
    /// justo lo que pasó al poner un ControlTheme sin BasedOn.
    /// </para>
    /// </summary>
    public static async Task CorrerVentanaPrincipal(Window dueno)
    {
        var v = new VentanaPrincipal { Width = 1180, Height = 780 };
        v.Show(dueno);
        await Task.Delay(700);

        Control? Cual(string n) => v.GetVisualDescendants().OfType<Control>()
                                    .FirstOrDefault(c => c.Name == n);

        Dice(true, "la ventana principal abre sin reventar");

        // ══ Arranca en Comprimir ═════════════════════════════════════════════
        // Las tres páginas comparten filas y se turnan por visibilidad. Si arrancara en
        // Organizar saldría su pantalla de inicio y la lista de comprimir no estaría.
        var comprimir = Cual("tabComprimir") as RadioButton;
        Dice(comprimir?.IsChecked == true, "y arranca en la página de comprimir");
        Dice(Cual("pageOrganizar")?.IsVisible == false, "con Organizar recogida");
        Dice(Cual("pageRecortes")?.IsVisible == false, "y Recortes también");

        // ══ La tabla, pintada ════════════════════════════════════════════════
        var tabla = Cual("lst") as DataGrid;
        Dice(tabla is not null, "la tabla de la lista está montada");
        Dice(tabla?.Columns.Count == 8, $"con sus ocho columnas ({tabla?.Columns.Count})");

        // Y que ordena por columna, que es lo que se gana al cambiar de control: el
        // GridView de WPF no sabía, y había un ayudante de veintiséis líneas para suplirlo.
        var porValor = tabla?.Columns.Count(c => c.SortMemberPath is not null) ?? 0;
        Dice(porValor == 8, $"y todas ordenan por su campo ({porValor}/8)");

        v.FilasDePrueba(("una.mkv", "Error: se acabó el disco"), ("otra.mp4", "…"));
        await Task.Delay(400);

        var filas = tabla?.GetVisualDescendants().OfType<DataGridRow>().ToList() ?? [];
        Dice(filas.Count == 2, $"dos filas metidas, dos filas pintadas ({filas.Count})");

        // La CELDA, no la fila: es lo único que demuestra que el tema de la tabla se aplicó.
        var celdas = tabla?.GetVisualDescendants().OfType<TextBlock>()
                          .Select(t => t.Text ?? "").ToList() ?? [];
        Dice(celdas.Any(t => t.Contains("una.mkv")),
            "y el nombre del fichero se lee en su celda");

        // ══ El color del estado ══════════════════════════════════════════════
        // Las clases se atan a tres booleanos de la fila. Enlazarlas a algo que no existe
        // no da error: la clase no se pone y el estado deja de distinguirse. Se comprueba
        // sobre el texto pintado, que es donde se vería.
        var rotuloError = tabla?.GetVisualDescendants().OfType<TextBlock>()
                               .FirstOrDefault(t => (t.Text ?? "").StartsWith("Error"));
        Dice(rotuloError is not null, "el estado de error se pinta");
        Dice(rotuloError?.Classes.Contains("estadoErr") == true,
            "y lleva puesta su clase: sin ella el enlace estaría roto y no se vería");

        // ══ La píldora: apagada Y sin latir ══════════════════════════════════
        // Las dos cosas, no una. Oculta pero con la clase puesta es exactamente el fallo
        // que esto viene a evitar: la animación corriendo sobre algo que nadie ve.
        var pildora = Cual("pillFondo");
        var punto = Cual("pillDot");
        Dice(pildora?.IsVisible == false, "la píldora de trabajo en segundo plano arranca oculta");
        Dice(punto?.Classes.Contains("late") == false,
            "y su punto NO late: escondido y animándose es el 5 % de un núcleo por nada");

        // ══ Los dos desplegables de audio, que no pueden contradecirse ═══════
        // Se prueba PULSANDO, no leyendo el modelo: el fallo del que sale esto era justo un
        // ajuste que no hacía nada -«Sin tocar» en un desplegable y «128 kbps» en el de al
        // lado-, y el motor desempataba en silencio recodificando. Ahora manda el códec y el
        // caudal se apaga; si el cableado se cae, el caudal se queda encendido y esto lo dice.
        // Se ponen los dos valores a mano y NO se da por hecho con cuál arranca: la ventana
        // aplica el preset por defecto del usuario nada más abrirse, así que el estado inicial
        // depende de la máquina donde corra esto. La primera versión de esta comprobación se
        // fiaba de él y acusaba al código de un fallo que era suyo.
        var codecAudio = Cual("cboACodec") as ComboBox;
        var caudal = Cual("cboAud") as ComboBox;
        Dice(codecAudio is not null && caudal is not null, "los dos desplegables de audio están montados");

        if (codecAudio is not null) codecAudio.SelectedIndex = 1;   // AAC
        await Task.Delay(200);
        Dice(caudal?.IsEnabled == true, "al elegir un códec, el caudal se enciende");

        if (codecAudio is not null) codecAudio.SelectedIndex = 0;   // «Sin tocar»
        await Task.Delay(200);
        Dice(caudal?.IsEnabled == false, "y con «Sin tocar» se apaga: copiando no se aplica ningún caudal");

        // ══ La cola ══════════════════════════════════════════════════════════
        Dice(Cual("panelCola")?.IsVisible == false, "el panel de la cola arranca oculto");

        // ══ El registro se despliega ═════════════════════════════════════════
        // El plegado era un converter de booleano a visibilidad; aquí es un enlace directo
        // al botón. Se prueba pulsándolo de verdad: con el enlace roto se queda como esté.
        // Se mira el MARCO, que es lo que lleva el enlace, y no la caja de dentro. La caja
        // dice IsVisible=true siempre: esa propiedad es la suya, no la de si se ve. Mi primer
        // intento preguntaba por la caja y fallaba acusando al código de algo que hacía la
        // comprobación — la tercera vez en esta tanda que pasa lo mismo.
        var tgl = Cual("tglLog") as ToggleButton;
        var marcoLog = Cual("txtLog")?.Parent as Control;
        Dice(marcoLog is not null, "el registro está montado dentro de su marco");
        Dice(marcoLog?.IsVisible == false, "y arranca plegado");

        if (tgl is not null) tgl.IsChecked = true;
        await Task.Delay(250);
        Dice(marcoLog?.IsVisible == true, "y al pulsar «Registro» se despliega");

        // ══ El panel lateral se pliega al estrechar ══════════════════════════
        // Esto vigila la parte frágil del puerto: las columnas que se pliegan no tienen
        // campo propio en Avalonia y se cogen por su POSICIÓN. Mover una columna en el XAML
        // rompería esto sin que nada más avise — de ahí que se compruebe el ancho de verdad
        // antes y después de estrechar la ventana.
        // ESTO COSTÓ TRES INTENTOS, y merece la pena dejarlos escritos.
        //
        // El primero leía el ancho de la columna 1 y no valía para nada: el propio código
        // acababa de escribir ahí ese número, así que la comprobación se leía a sí misma —
        // movida la columna a otro sitio en el XAML, pasaba igual.
        //
        // El segundo miraba el ancho PINTADO del panel al estrechar, y acusaba al código de
        // algo que no hacía: un control oculto no se vuelve a medir, así que sus Bounds se
        // quedan con el último valor que tuvieron. Estaba plegado y la comprobación leía 252.
        //
        // Lo que sí ata las dos cosas es mirar la TABLA. Si la columna que el código
        // dimensiona no fuera la del panel, la tabla se quedaría con el ancho fijo y el
        // panel con el elástico: 1.180 px de ventana y una tabla de 262. Eso no se puede
        // fingir desde el propio código.
        var lateral = Cual("sideCol") as Grid;
        var tablaAncho = tabla?.Bounds.Width ?? -1;
        Dice(lateral is not null, "el panel lateral está montado");
        Dice(lateral is not null && Grid.GetColumn(lateral) == 1,
            "y vive en la columna 1, que es la que el código dimensiona");
        Dice(lateral?.Bounds.Width > 200, $"con la ventana ancha se ve entero ({lateral?.Bounds.Width:0} px)");
        Dice(tablaAncho > 600,
            $"y la tabla se queda con el resto ({tablaAncho:0} px), no con el ancho fijo del panel");

        v.Width = 800;
        await Task.Delay(400);

        // Al plegarse: la columna a cero y el panel fuera. Las dos, porque una columna a
        // cero con el panel dentro seguiría empujando, y un panel oculto en una columna de
        // 262 dejaría un hueco muerto — que es exactamente lo que pasaba en Organizar.
        var col = (Cual("rowTabla") as Grid)?.ColumnDefinitions[1].Width.Value ?? -1;
        Dice(col == 0, $"al estrechar, su columna se va a cero ({col})");
        Dice(lateral?.IsVisible == false, "y el panel se retira: sin eso quedaría un hueco muerto");

        v.Close();
    }

    /// <summary>
    /// Que TODO control tenga algo dibujado dentro.
    ///
    /// <para>
    /// <b>Esta comprobación existe porque faltaba y se publicó una versión sin ella.</b> El
    /// desplegable estaba vestido con un <c>ControlTheme</c> de solo colores, sin plantilla y
    /// sin <c>BasedOn</c>, y un <c>ControlTheme</c> <b>sustituye</b> al del tema base: se
    /// quedó sin plantilla ninguna. En la ventana principal salieron nueve rótulos —Idioma,
    /// Formato, Códec, Calidad, Esfuerzo…— con un hueco vacío debajo de cada uno, y en
    /// Preferencias no había manera de elegir el idioma. La aplicación no servía para nada.
    /// </para>
    /// <para>
    /// Y lo que más duele: <b>había una comprobación de arranque con 154 verificaciones y
    /// ninguna miraba esto.</b> Todas preguntaban si un tema se había aplicado —si tal parte
    /// de la plantilla tenía tal color—, y un control sin plantilla no tiene partes que
    /// preguntar: se cae de la lista en silencio en vez de fallar. Lo encontró una persona
    /// abriendo la aplicación.
    /// </para>
    /// <para>
    /// Así que esto no pregunta por colores. Pregunta lo anterior a todo eso: <b>¿hay algo
    /// dibujado?</b> Un control colocado, visible y con tamaño cero o sin un solo hijo visual
    /// es un control que no está, diga lo que diga el XAML.
    /// </para>
    /// </summary>
    public static async Task CorrerTodoSeDibuja(Window dueno)
    {
        // Las dos pantallas con más controles de formulario, que son donde esto se notó.
        var principal = new VentanaPrincipal { Width = 1180, Height = 780 };
        principal.Show(dueno);
        await Task.Delay(600);

        var prefs = new Preferencias(SettingsStore.Load(), []);
        prefs.Show(dueno);
        await Task.Delay(500);

        foreach (var (donde, raiz) in new (string, Visual)[] { ("la ventana", principal), ("Preferencias", prefs) })
        {
            // Solo lo que está PUESTO y visible: un control oculto a propósito no se dibuja
            // y eso no es un fallo. Y solo los que salen del tema base -desplegables,
            // casillas, cajas de texto y botones-, que es donde vive este riesgo.
            var sospechosos = raiz.GetVisualDescendants()
                                  .OfType<Control>()
                                  .Where(c => c is ComboBox or CheckBox or TextBox or Button or Slider)
                                  .Where(c => c.IsVisible && c.IsEffectivelyVisible)
                                  .ToList();

            Dice(sospechosos.Count > 5,
                $"{donde}: hay controles de formulario que mirar ({sospechosos.Count})");

            var vacios = sospechosos
                // Fuera las piezas de dentro de una plantilla (PART_*). Un boton de
                // paginar de una barra de desplazamiento mide cero cuando no hay nada que
                // desplazar, y el de subir de un deslizador mide cero con el tirador al
                // final: son ceros legitimos. Lo que se busca es un control con el que el
                // usuario tiene que poder hablar y que no esta dibujado, y esos tienen el
                // nombre que les pusimos nosotros -cboQ, cboFmt-, no PART_.
                .Where(c => c.Name is null || !c.Name.StartsWith("PART_"))
                .Where(c => c.GetVisualChildren().Count() == 0 || c.Bounds.Height <= 0)
                .Select(c => $"{c.GetType().Name}{(c.Name is null ? "" : "#" + c.Name)}")
                .Distinct()
                .ToList();

            Dice(vacios.Count == 0,
                vacios.Count == 0
                    ? $"{donde}: todos se dibujan — ninguno se quedó sin plantilla"
                    : $"{donde}: {vacios.Count} sin dibujar: {string.Join(", ", vacios.Take(6))}");
        }

        // Y el caso concreto, dicho por su nombre: que los desplegables se vean. Es el que
        // se rompió, y una cuenta agregada puede volver a pasar con otro control mientras
        // este sigue mal.
        var listas = principal.GetVisualDescendants().OfType<ComboBox>()
                              .Where(c => c.IsEffectivelyVisible).ToList();
        Dice(listas.Count >= 8, $"los desplegables de la ventana están puestos ({listas.Count})");
        Dice(listas.All(c => c.Bounds.Height >= 20),
            $"y todos tienen alto de verdad ({listas.Count(c => c.Bounds.Height >= 20)}/{listas.Count})");

        prefs.Close();
        principal.Close();
    }

    /// <summary>
    /// Que la ventana se pueda mover y estirar.
    ///
    /// <para>
    /// <b>No se podía, y se publicó así.</b> Con <c>SystemDecorations="None"</c> no hay marco
    /// del sistema, así que arrastrar por el título y estirar por los bordes hay que pedirlos
    /// —en WPF los daba WindowChrome gratis—. El comentario del XAML decía que se pedían con
    /// <c>BeginMoveDrag</c>; describía lo que había que hacer y nadie lo hizo. La ventana
    /// quedaba clavada donde el sistema la abriera. Lo encontró una persona en Linux Mint.
    /// </para>
    /// <para>
    /// <b>Lo que aquí se puede comprobar y lo que no.</b> Arrastrar de verdad necesita un
    /// ratón de verdad: <c>BeginMoveDrag</c> le pide al gestor de ventanas que tome el
    /// control, y eso no se simula. Lo que sí: que la franja del título <b>exista y ocupe su
    /// alto</b> —sin ella no hay por dónde agarrar— y que el cálculo de qué borde hay bajo el
    /// puntero sea correcto, que es donde está el error posible.
    /// </para>
    /// </summary>
    public static async Task CorrerMoverLaVentana(Window dueno)
    {
        var v = new VentanaPrincipal { Width = 1000, Height = 700 };
        v.Show(dueno);
        await Task.Delay(500);

        var barra = v.GetVisualDescendants().OfType<Control>()
                     .FirstOrDefault(c => c.Name == "barraTitulo");
        Dice(barra is not null, "la franja del título existe: es por donde se agarra la ventana");
        Dice(barra?.Bounds.Height >= 30,
            $"y ocupa su alto ({barra?.Bounds.Height:0} px), que es la zona de arrastre");

        // Y RECIBE EL RATON EN SUS HUECOS. Una rejilla sin Background no se puede pulsar
        // donde no hay nada dibujado: solo le llegan los eventos que suben desde un hijo. Con
        // la barra sin fondo, la ventana se arrastraba desde la marca y desde el menu, y en el
        // resto de la franja no pasaba nada. Lo encontro una persona usandola.
        Dice(barra is Panel rejilla && rejilla.Background is not null,
            "y tiene fondo, que es lo que hace que se pueda agarrar por el hueco vacio");

        // El menu vive DENTRO de la franja, y un menu de Avalonia se abre en la pulsacion.
        // Si el arrastre no distinguiera de donde viene el clic, se lo llevaria y habria que
        // pulsar dos veces cada menu.
        var menu = v.GetVisualDescendants().OfType<Menu>().FirstOrDefault();
        Dice(menu is not null, "la barra de menus esta dentro de la franja de arrastre");
        Dice(menu is null || menu.GetVisualAncestors().Any(x => (x as Control)?.Name == "barraTitulo"),
            "y por eso el arrastre tiene que preguntar de donde viene la pulsacion");

        // ── Los bordes ────────────────────────────────────────────────────────
        var tam = new Size(1000, 700);

        Dice(VentanaPrincipal.BordeEn(new Point(500, 350), tam, 6) is null,
            "en el medio de la ventana no hay borde que estirar");
        Dice(VentanaPrincipal.BordeEn(new Point(2, 350), tam, 6) == WindowEdge.West,
            "el borde izquierdo estira a lo ancho");
        Dice(VentanaPrincipal.BordeEn(new Point(500, 698), tam, 6) == WindowEdge.South,
            "el de abajo, a lo alto");

        // Las esquinas ganan a los lados. Es lo único que se puede escribir mal aquí sin
        // que se note: con el orden al revés, una esquina se lee como lado y estirar en
        // diagonal -que es como se estira de verdad- deja de ser posible.
        Dice(VentanaPrincipal.BordeEn(new Point(1, 1), tam, 6) == WindowEdge.NorthWest,
            "y en una esquina gana la esquina, no el lado: si no, no hay diagonal");
        Dice(VentanaPrincipal.BordeEn(new Point(999, 699), tam, 6) == WindowEdge.SouthEast,
            "las cuatro");

        v.Close();
    }

    /// <summary>
    /// Los cuatro que se escaparon: lo que una auditoría encontró y estas comprobaciones no.
    ///
    /// <para>
    /// <b>Merece la pena entender por qué se escaparon</b>, porque el patrón se repite: cada
    /// una de las que ya había preguntaba por <i>el estado del modelo</i> en vez de recorrer
    /// <i>el camino que recorre el usuario</i>. La de Preferencias es el ejemplo perfecto:
    /// abría la ventana con <c>Show</c>, pulsaba «Guardar» y leía <c>v.Result</c> — y todo eso
    /// funcionaba. Lo que estaba roto era el paso que la comprobación no daba: cerrar
    /// devolviendo el resultado por <c>ShowDialog&lt;bool&gt;</c>, que es como la abre la
    /// aplicación de verdad.
    /// </para>
    /// <para>
    /// Así que estas cuatro pasan por donde pasa el usuario, aunque cueste más de escribir.
    /// </para>
    /// </summary>
    public static async Task CorrerLoQueSeEscapo(Window dueno)
    {
        await ElViajeDeVueltaDeUnModal(dueno);
        CadaFilaDePeliculasSePinta();
        await CambiarDePaginaCambiaLaPagina(dueno);
        await LaTablaDeOrganizarTieneVista(dueno);
    }

    /// <summary>
    /// Que un modal devuelva de verdad lo que dice devolver.
    ///
    /// <para>
    /// Preferencias, Renombrar y el explorador del catálogo cerraban con
    /// <c>Close(unObjeto)</c> mientras quien los abría pedía <c>ShowDialog&lt;bool&gt;</c>.
    /// Avalonia intenta convertir ese objeto en el tipo pedido <b>dentro</b> de <c>Close</c>,
    /// y revienta: la excepción sale por el <c>await</c> de quien esperaba. Guardar no
    /// guardaba, renombrar no renombraba y elegir episodio a mano no elegía nada.
    /// </para>
    /// </summary>
    private static async Task ElViajeDeVueltaDeUnModal(Window dueno)
    {
        var v = new Preferencias(SettingsStore.Load(), ["Archivar"]);

        // ShowDialog<bool> y no Show: es lo único que distingue esta comprobación de la que
        // ya había, y es exactamente donde estaba el fallo.
        var tarea = v.ShowDialog<bool>(dueno);
        await Task.Delay(400);

        v.GetVisualDescendants().OfType<Button>().First(b => b.Name == "btnSave")
         .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        // SIN RED, Y A PROPOSITO. Se intento envolver esto en un try para que el sabotaje diera
        // un ✗ legible, y no funciona: la excepcion se lanza en el camino de CIERRE de la
        // ventana, dentro del bucle de mensajes de Avalonia, y desde aqui no se puede recoger.
        // El sabotaje se lleva la comprobacion entera por delante — lo que tambien es una
        // alarma, pero mala, porque esconde las demas.
        //
        // Asi que esta comprobacion vale para lo que vale: confirma que el viaje de vuelta
        // funciona. Que nadie vuelva a cerrar con un objeto lo vigila una prueba de codigo
        // fuente, CerrarModalesTests, que es donde ese fallo si se puede cazar sin abrir nada.
        var devuelto = await tarea;
        Dice(devuelto, "Preferencias devuelve «sí» al guardar, por el mismo camino que usa la app");
        Dice(v.Result is not null, "y los ajustes llegan en Result");
    }

    /// <summary>
    /// Que pintar una fila de películas no mate el proceso.
    ///
    /// <para>
    /// Su ayudante de colores se llamaba a sí mismo. No es una excepción que se pueda
    /// capturar: un <c>StackOverflowException</c> <b>se lleva el proceso por delante</b>, sin
    /// diálogo y sin registro. Si vuelve, esta comprobación no falla — <b>desaparece</b>, y
    /// eso también se ve.
    /// </para>
    /// </summary>
    private static void CadaFilaDePeliculasSePinta()
    {
        foreach (var motivo in Enum.GetValues<Ondine.Reindex.PlanDePeliculas.Porque>())
        {
            var fila = new PeliculaFila
            {
                Paso = new Ondine.Reindex.PlanDePeliculas.Paso("/x/peli.mkv", "/x/Peli (2001).mkv", motivo),
                Raiz = "/x",
            };

            // Los tres a la vez: el que se desbordaba era el del borde, por el sufijo.
            Dice(fila.EstadoFg is not null && fila.EstadoBg is not null && fila.EstadoBorde is not null,
                $"una fila de películas «{motivo}» se pinta sin llevarse el proceso por delante");
        }
    }

    /// <summary>
    /// Que cambiar de pestaña cambie la página.
    ///
    /// <para>
    /// En Avalonia hay <b>un solo evento</b> para marcar y desmarcar, así que al cambiar de
    /// página saltan dos: el de la nueva y el de la vieja al apagarse. Sin preguntar quién
    /// avisa, la última pasada era la de la página que acabas de dejar.
    /// </para>
    /// </summary>
    private static async Task CambiarDePaginaCambiaLaPagina(Window dueno)
    {
        var v = new VentanaPrincipal { Width = 1180, Height = 780 };
        v.Show(dueno);
        await Task.Delay(600);

        Control? Cual(string n) => v.GetVisualDescendants().OfType<Control>()
                                    .FirstOrDefault(c => c.Name == n);

        var organizar = Cual("pageOrganizar");
        var recortes = Cual("pageRecortes");
        Dice(organizar?.IsVisible == false, "se arranca con Organizar recogida");

        // Se pulsa por donde se pulsa: las pestañas son el modelo de estado, y marcarlas es
        // lo que hace el menú del conmutador.
        if (Cual("tabOrganizar") is RadioButton org) org.IsChecked = true;
        await Task.Delay(350);
        Dice(organizar?.IsVisible == true, "al elegir Organizar, se ve Organizar");
        Dice(recortes?.IsVisible == false, "y Recortes no");

        if (Cual("tabRecortes") is RadioButton rec) rec.IsChecked = true;
        await Task.Delay(350);
        Dice(recortes?.IsVisible == true, "al elegir Recortes, se ve Recortes");
        Dice(organizar?.IsVisible == false, "y Organizar se recoge");

        if (Cual("tabComprimir") is RadioButton com) com.IsChecked = true;
        await Task.Delay(350);
        Dice(organizar?.IsVisible == false && recortes?.IsVisible == false,
            "y al volver a Comprimir se recogen las dos");

        v.Close();
    }

    /// <summary>
    /// Que la tabla de Organizar tenga VISTA y no la colección a pelo.
    ///
    /// <para>
    /// El comentario del campo lo decía entero —«hay que dársela como ItemsSource, y si se le
    /// da la colección a pelo el filtro no se aplica y no hay ningún error»— y se le daba la
    /// colección a pelo. El campo se quedaba en nulo, así que «Analizar» reventaba con un
    /// <c>NullReferenceException</c> que el <c>catch</c> presentaba como «el análisis falló», y
    /// con él caían los chips de filtro, el buscador, el orden por cabecera y las bandas.
    /// </para>
    /// </summary>
    private static async Task LaTablaDeOrganizarTieneVista(Window dueno)
    {
        var vista = new OrganizarView();
        var v = new Window { Width = 1200, Height = 780, Content = vista };
        v.Show(dueno);
        await Task.Delay(700);

        var tabla = vista.GetVisualDescendants().OfType<DataGrid>()
                         .FirstOrDefault(t => t.Name == "tabla");
        Dice(tabla is not null, "la tabla de Organizar está montada");
        Dice(tabla?.ItemsSource is Avalonia.Collections.DataGridCollectionView,
            $"y recibe una VISTA, no la colección a pelo ({tabla?.ItemsSource?.GetType().Name})");

        v.Close();
    }
}
