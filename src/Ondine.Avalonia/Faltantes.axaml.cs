using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine.Ava;

/// <summary>Un hueco del catálogo, tal como se ve en la lista.</summary>
public sealed class FaltaVista
{
    public required CoberturaCatalogo.Hueco Hueco { get; init; }
    public string Codigo => Hueco.Codigo;
    public string Titulos => Hueco.Titulos;

    /// <summary>«entero» o «a medias»: uno hay que conseguirlo, el otro puede estar sin partir.</summary>
    public string Etiqueta => Hueco.Entero
        ? Textos.Instancia.FaltantesEntero
        : Textos.Instancia.FaltantesAMedias;
}

/// <summary>
/// «Qué falta», portado de <c>FaltantesWindow</c>.
///
/// <para>
/// <b>Se porta fácil porque la lógica no está aquí.</b> Quien decide qué falta es
/// <see cref="CoberturaCatalogo"/>, que vive en el motor y ya tenía sus pruebas. Esta
/// ventana solo pregunta y pinta, así que el puerto es traducir XAML y poco más — es la
/// prueba de que la Fase 1, bajar reglas al motor, se paga sola al llegar aquí.
/// </para>
/// <para>
/// Lo que sí cambia: el portapapeles. En WPF era <c>Clipboard.SetText</c>, estático y
/// síncrono; en Avalonia cuelga de la ventana y devuelve una tarea. Es la misma historia
/// que con los modales — cosas que en WPF eran una línea, aquí son <c>async</c>.
/// </para>
/// </summary>
public partial class Faltantes : Window
{
    private readonly ReindexCatalog _catalogo = null!;
    private readonly IReadOnlyList<ReindexResolution> _resoluciones = [];
    private readonly bool _porAnio;
    private CoberturaCatalogo.Informe _informe = null!;

    public Faltantes() => AvaloniaXamlLoader.Load(this);

    public Faltantes(ReindexCatalog catalogo, IReadOnlyList<ReindexResolution> resoluciones) : this()
    {
        _catalogo = catalogo;
        _resoluciones = resoluciones;

        Lbl("lblTitulo").Text = string.Format(Textos.Instancia.FaltantesTituloSerie, catalogo.Serie);
        Btn("btnCerrar").Click += (_, _) => Close();
        Btn("btnCopiar").Click += async (_, _) => await Copiar();

        var todas = Chk("chkTodas");
        todas.IsCheckedChanged += (_, _) => Pintar();

        // El rótulo depende de cómo numere el catálogo: hay series que van por año de
        // emisión y ahí «Temporada 2005» chirría — es el año.
        _porAnio = CoberturaCatalogo.TemporadasSonAnios(catalogo);
        Lbl("lblTemporada").Text = _porAnio
            ? Textos.Instancia.FaltantesAnio
            : Textos.Instancia.FaltantesTemporada;

        var cbo = Cbo("cboTemporada");
        var items = new List<string>
        {
            _porAnio ? Textos.Instancia.FaltantesTodosLosAnios
                     : Textos.Instancia.FaltantesTodasLasTemporadas,
        };
        items.AddRange(CoberturaCatalogo.TemporadasDe(catalogo).Select(t =>
            _porAnio ? t.ToString() : string.Format(Textos.Instancia.FaltantesTemporadaN, t)));

        cbo.ItemsSource = items;
        cbo.SelectedIndex = 0;
        cbo.SelectionChanged += (_, _) => Pintar();

        Pintar();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private ComboBox Cbo(string n) => this.FindControl<ComboBox>(n)!;

    /// <summary>La temporada elegida, o null si están todas (el primer elemento).</summary>
    private int? TemporadaElegida
    {
        get
        {
            var i = Cbo("cboTemporada").SelectedIndex - 1;
            if (i < 0) return null;
            var todas = CoberturaCatalogo.TemporadasDe(_catalogo);
            return i < todas.Count ? todas[i] : null;
        }
    }

    private void Pintar()
    {
        if (_catalogo is null) return;

        var temporada = TemporadaElegida;
        var todas = Chk("chkTodas");

        _informe = CoberturaCatalogo.Calcular(_catalogo, _resoluciones,
            soloTemporadasConAlgo: todas.IsChecked != true,
            temporada: temporada);

        // Con una temporada elegida, la casilla de «incluir las que no he empezado» no
        // pinta nada: ya estás mirando una concreta, esté empezada o no.
        todas.IsEnabled = temporada is null;

        Lbl("lblResumen").Text = temporada is null
            ? _informe.Resumen
            : string.Format(_porAnio ? Textos.Instancia.FaltantesResumenAnio
                                     : Textos.Instancia.FaltantesResumenTemporada,
                            temporada, _informe.Resumen);

        Lbl("lblEspeciales").Text = _informe.EspecialesQueFaltan > 0
            ? string.Format(_informe.EspecialesQueFaltan == 1
                                ? Textos.Instancia.FaltantesEspecialesUno
                                : Textos.Instancia.FaltantesEspeciales,
                            _informe.EspecialesQueFaltan)
            : "";

        this.FindControl<ListBox>("lista")!.ItemsSource =
            _informe.Huecos.Select(h => new FaltaVista { Hueco = h }).ToList();

        Btn("btnCopiar").IsEnabled = _informe.Huecos.Count > 0;

        Lbl("lblPie").Text = _informe.Huecos.Count > 0
            ? Textos.Instancia.FaltantesPie
            : temporada is not null
                ? string.Format(_porAnio ? Textos.Instancia.FaltantesNadaAnio
                                         : Textos.Instancia.FaltantesNadaTemporada, temporada)
                : todas.IsChecked == true
                    ? Textos.Instancia.FaltantesNadaCatalogo
                    : Textos.Instancia.FaltantesNadaEmpezadas;
    }

    /// <summary>
    /// Al portapapeles en texto plano: sirve para pegarlo en una nota o pasárselo a alguien,
    /// que es lo que se acaba haciendo con una lista de lo que te falta.
    /// </summary>
    private async Task Copiar()
    {
        // El sufijo sale de la MISMA clave que el distintivo de la fila: así lo pegado
        // dice lo mismo que se estaba viendo en pantalla.
        var aMedias = $"\t({Textos.Instancia.FaltantesAMedias})";

        var sb = new StringBuilder();
        sb.AppendLine($"{_catalogo.Serie} - {_informe.Resumen}");
        sb.AppendLine();
        foreach (var h in _informe.Huecos)
            sb.AppendLine($"{h.Codigo}\t{h.Titulos}{(h.Entero ? "" : aMedias)}");

        try
        {
            // En Avalonia el portapapeles cuelga de la VENTANA, no es estático como en WPF.
            // Y en la 12 se pone con DataTransfer: DataObject y DataFormats están jubilados.
            if (Clipboard is null) throw new InvalidOperationException("sin portapapeles");

            var datos = new Avalonia.Input.DataTransfer();
            datos.Add(Avalonia.Input.DataTransferItem.CreateText(sb.ToString()));
            await Clipboard.SetDataAsync(datos);

            Lbl("lblPie").Text = string.Format(
                _informe.Huecos.Count == 1 ? Textos.Instancia.FaltantesCopiadoUno
                                           : Textos.Instancia.FaltantesCopiado,
                _informe.Huecos.Count);
        }
        catch (Exception ex)
        {
            Lbl("lblPie").Text = string.Format(Textos.Instancia.FaltantesNoSePudoCopiar, ex.Message);
        }
    }
}
