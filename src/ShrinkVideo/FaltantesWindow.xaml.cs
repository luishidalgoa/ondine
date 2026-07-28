using System.Text;
using System.Windows;
using ShrinkVideo.Reindex;

namespace ShrinkVideo;

/// <summary>Una fila de la lista de lo que falta, ya lista para pintar.</summary>
public sealed class FaltaVista
{
    public required CoberturaCatalogo.Hueco Hueco { get; init; }
    public string Codigo => Hueco.Codigo;
    public string Titulos => Hueco.Titulos;
    public string Etiqueta => Hueco.Entero ? "entero" : "a medias";
}

/// <summary>
/// Qué falta en la biblioteca, comparando el catálogo con lo que hay identificado.
///
/// Existe porque el catálogo ya sabe qué episodios hay y la app ya sabe cuáles tienes: la resta
/// era información que estaba ahí sin aprovechar. Y se cuenta por HISTORIAS, no por episodios,
/// que es lo que distingue «no lo tengo» de «lo tengo a medias» — un capítulo de tres historias
/// del que solo están dos parece completo en la carpeta y no lo está.
/// </summary>
public partial class FaltantesWindow : Window
{
    private readonly ReindexCatalog _catalogo;
    private readonly IReadOnlyList<ReindexResolution> _resoluciones;

    public FaltantesWindow(ReindexCatalog catalogo, IReadOnlyList<ReindexResolution> resoluciones)
    {
        InitializeComponent();
        _catalogo = catalogo;
        _resoluciones = resoluciones;

        lblTitulo.Text = $"Qué falta — {catalogo.Serie}";
        btnCerrar.Click += (_, _) => Close();
        chkTodas.Checked += (_, _) => Pintar();
        chkTodas.Unchecked += (_, _) => Pintar();
        btnCopiar.Click += (_, _) => Copiar();
        Pintar();
    }

    private CoberturaCatalogo.Informe _informe = null!;

    private void Pintar()
    {
        _informe = CoberturaCatalogo.Calcular(_catalogo, _resoluciones,
            soloTemporadasConAlgo: chkTodas.IsChecked != true);

        lblResumen.Text = _informe.Resumen;
        lblEspeciales.Text = _informe.EspecialesQueFaltan > 0
            ? $"Aparte, faltan {_informe.EspecialesQueFaltan} especial(es). Se cuentan por separado " +
              "porque casi nadie los tiene completos."
            : "";
        lista.ItemsSource = _informe.Huecos.Select(h => new FaltaVista { Hueco = h }).ToList();
        btnCopiar.IsEnabled = _informe.Huecos.Count > 0;

        if (_informe.Huecos.Count == 0)
            lblPie.Text = chkTodas.IsChecked == true
                ? "No falta nada del catálogo entero."
                : "No falta nada de las temporadas que has empezado.";
        else
            lblPie.Text = "Se cuenta por historias: un capítulo con tres y solo dos en disco sale como incompleto.";
    }

    /// <summary>
    /// Al portapapeles en texto plano: sirve para pegarlo en una nota o pasárselo a alguien,
    /// que es lo que se acaba haciendo con una lista de lo que te falta.
    /// </summary>
    private void Copiar()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{_catalogo.Serie} — {_informe.Resumen}");
        sb.AppendLine();
        foreach (var h in _informe.Huecos)
            sb.AppendLine($"{h.Codigo}\t{h.Titulos}{(h.Entero ? "" : "\t(a medias)")}");

        try
        {
            Clipboard.SetText(sb.ToString());
            lblPie.Text = $"Copiado: {_informe.Huecos.Count} línea(s).";
        }
        catch (Exception ex) { lblPie.Text = $"No se pudo copiar: {ex.Message}"; }
    }
}
