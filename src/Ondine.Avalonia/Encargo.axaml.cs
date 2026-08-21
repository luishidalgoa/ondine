using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine.Ava;

/// <summary>Un idioma tal y como se pinta en la lista de resultados.</summary>
public sealed class IdiomaFila
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public required bool Elegido { get; init; }
    /// <summary>Un visto si ya está elegido. Ocupa sitio fijo para que la columna no baile.</summary>
    public string Marca => Elegido ? "✓" : "";
}

/// <summary>
/// «Generar el catálogo con una IA», portado de <c>PromptWindow</c>.
///
/// <para>
/// La redacción del encargo vive en <see cref="CatalogPrompt"/>, que es código puro y con
/// pruebas; aquí solo se recogen los datos y se copia el resultado.
/// </para>
/// <para>
/// <b>Lo que cambia al portar, y no es cosmético:</b> los botones que van dentro de una
/// plantilla. En WPF llevaban un <c>Tag="{Binding Codigo}"</c> y el manejador leía ese Tag
/// del remitente. Aquí el código se saca del <c>DataContext</c> de la fila, que es lo que
/// Avalonia ya pone ahí — no hay que duplicar el dato en un Tag para volver a leerlo. Menos
/// sitios donde se puede desincronizar.
/// </para>
/// <para>
/// Y una trampa que es la misma con otro nombre: el foco del buscador no se puede pedir
/// mientras el emergente se está abriendo, porque todavía no puede recibirlo. En WPF se
/// aplazaba con <c>Dispatcher.BeginInvoke</c>; aquí con <c>Dispatcher.UIThread.Post</c>.
/// </para>
/// </summary>
public partial class Encargo : Window
{
    /// <summary>Códigos elegidos, en el orden en que se fueron marcando.</summary>
    private readonly List<string> _elegidos = ["es", "en"];

    public Encargo() => AvaloniaXamlLoader.Load(this);

    public Encargo(string serieSugerida) : this()
    {
        Txt("txtSerie").Text = serieSugerida;

        // La lista de salida sale ya ordenada con los de andar por casa arriba. Va con
        // ItemTemplate y no con DisplayMemberBinding: el tema le pone su propia plantilla a
        // lo seleccionado, y sin plantilla se acaba viendo el nombre del tipo.
        var cbo = Cbo("cboSalida");
        cbo.ItemsSource = IsoLanguages.Buscar("");
        cbo.SelectedIndex = 0;   // español de España

        Txt("txtSerie").TextChanged += (_, _) => Refrescar();
        Txt("txtFuente").TextChanged += (_, _) => Refrescar();
        cbo.SelectionChanged += (_, _) => Refrescar();
        Txt("txtBuscarIdioma").TextChanged += (_, _) => RefrescarIdiomas();

        // Al abrir: lista limpia y cursor dentro. Si conservara lo escrito, reabrir enseñaría
        // cuatro resultados de la búsqueda anterior y parecería que no hay más idiomas.
        // El foco va aplazado porque durante Opened el emergente aún no puede recibirlo.
        Pop("popIdiomas").Opened += (_, _) =>
        {
            Txt("txtBuscarIdioma").Text = "";
            Dispatcher.UIThread.Post(() => Txt("txtBuscarIdioma").Focus(),
                                     DispatcherPriority.Input);
        };

        Btn("btnCerrar").Click += (_, _) => Close();
        Btn("btnCopiar").Click += async (_, _) => await Copiar();
        Btn("btnAbrirFuente").Click += (_, _) => AbrirFuente();

        RefrescarIdiomas();
        Refrescar();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private TextBox Txt(string n) => this.FindControl<TextBox>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private ComboBox Cbo(string n) => this.FindControl<ComboBox>(n)!;
    private Popup Pop(string n) => this.FindControl<Popup>(n)!;
    private ItemsControl Its(string n) => this.FindControl<ItemsControl>(n)!;

    private string IdiomaSalida => (Cbo("cboSalida").SelectedItem as IsoLanguage)?.Codigo ?? "es";

    private List<string> IdiomasMarcados => _elegidos.ToList();

    // ─────────────────────────── idiomas ───────────────────────────

    /// <summary>Repinta las insignias y los resultados. La lista es de 183: rehacerla entera no se nota.</summary>
    private void RefrescarIdiomas()
    {
        Its("listaSeleccionados").ItemsSource = _elegidos
            .Select(c => new IdiomaFila { Codigo = c, Nombre = IsoLanguages.Nombre(c), Elegido = true })
            .ToList();

        var encontrados = IsoLanguages.Buscar(Txt("txtBuscarIdioma").Text ?? "")
            .Select(i => new IdiomaFila
            {
                Codigo = i.Codigo, Nombre = i.Nombre, Elegido = _elegidos.Contains(i.Codigo),
            })
            .ToList();

        Its("listaResultados").ItemsSource = encontrados;
        Lbl("lblSinResultados").IsVisible = encontrados.Count == 0;
    }

    /// <summary>
    /// El código de la fila del botón que se pulsó. Sale del <c>DataContext</c>, que es
    /// donde Avalonia lo pone: en WPF había que duplicarlo en un <c>Tag</c>.
    /// </summary>
    private static string? CodigoDe(object? remitente) =>
        (remitente as Control)?.DataContext is IdiomaFila f ? f.Codigo : null;

    public void OnAlternarIdioma(object? remitente, RoutedEventArgs e)
    {
        if (CodigoDe(remitente) is not { } codigo) return;

        if (!_elegidos.Remove(codigo)) _elegidos.Add(codigo);
        RefrescarIdiomas();
        Refrescar();
    }

    public void OnQuitarIdioma(object? remitente, RoutedEventArgs e)
    {
        if (CodigoDe(remitente) is not { } codigo) return;

        _elegidos.Remove(codigo);
        RefrescarIdiomas();
        Refrescar();
    }

    private void Refrescar()
    {
        Txt("txtPrompt").Text = CatalogPrompt.Build(
            Txt("txtSerie").Text ?? "", Txt("txtFuente").Text ?? "",
            IdiomaSalida, IdiomasMarcados);

        // Avisar del error que más caro sale: no incluir el idioma en el que vienen tus
        // ficheros hoy, y descubrirlo cuando ya no reconoce ninguno.
        Lbl("lblAviso").Text = IdiomasMarcados.Count switch
        {
            // Quitarlos todos no rompe nada —el de salida siempre entra— pero conviene decirlo
            0 => string.Format(Textos.Instancia.PromptAvisoSinIdiomas, IsoLanguages.Nombre(IdiomaSalida)),
            1 => Textos.Instancia.PromptAvisoUnIdioma,
            _ => string.Format(Textos.Instancia.PromptAvisoVariosIdiomas,
                               IdiomasMarcados.Count, IsoLanguages.Nombre(IdiomaSalida)),
        };
    }

    private async Task Copiar()
    {
        try
        {
            if (Clipboard is null) throw new InvalidOperationException("sin portapapeles");

            var datos = new Avalonia.Input.DataTransfer();
            datos.Add(Avalonia.Input.DataTransferItem.CreateText(Txt("txtPrompt").Text ?? ""));
            await Clipboard.SetDataAsync(datos);

            Lbl("lblAviso").Text = Textos.Instancia.PromptCopiado;
        }
        catch (Exception ex)
        {
            // El portapapeles lo puede tener bloqueado otro proceso: se dice y ya está,
            // el texto sigue en pantalla para copiarlo a mano.
            Lbl("lblAviso").Text = Textos.Instancia.PromptNoSePudoCopiar + ex.Message;
        }
    }

    private void AbrirFuente()
    {
        var url = Txt("txtFuente").Text?.Trim() ?? "";
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            Lbl("lblAviso").Text = Textos.Instancia.PromptFuenteVacia;
            return;
        }
        try
        {
            // UseShellExecute vale en los tres sistemas: en Linux .NET lo resuelve con
            // xdg-open y en macOS con «open». Es lo mismo que hacía WPF, sin cambios.
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) { Lbl("lblAviso").Text = Textos.Instancia.PromptNoSePudoAbrir + ex.Message; }
    }
}
