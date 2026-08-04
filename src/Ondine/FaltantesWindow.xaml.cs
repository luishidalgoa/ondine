using System.Text;
using System.Windows;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

/// <summary>Una fila de la lista de lo que falta, ya lista para pintar.</summary>
public sealed class FaltaVista
{
    public required CoberturaCatalogo.Hueco Hueco { get; init; }
    public string Codigo => Hueco.Codigo;
    public string Titulos => Hueco.Titulos;
    public string Etiqueta => Hueco.Entero
        ? Textos.Instancia.FaltantesEntero
        : Textos.Instancia.FaltantesAMedias;
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

        lblTitulo.Text = string.Format(Textos.Instancia.FaltantesTituloSerie, catalogo.Serie);
        btnCerrar.Click += (_, _) => Close();
        chkTodas.Checked += (_, _) => Pintar();
        chkTodas.Unchecked += (_, _) => Pintar();
        btnCopiar.Click += (_, _) => Copiar();

        // El rótulo depende de cómo numere el catálogo: hay series que van por año de emisión y
        // ahí «Temporada 2005» chirría — es el año.
        _porAnio = CoberturaCatalogo.TemporadasSonAnios(catalogo);
        lblTemporada.Text = _porAnio ? Textos.Instancia.FaltantesAnio : Textos.Instancia.FaltantesTemporada;

        cboTemporada.Items.Add(_porAnio
            ? Textos.Instancia.FaltantesTodosLosAnios
            : Textos.Instancia.FaltantesTodasLasTemporadas);
        foreach (var t in CoberturaCatalogo.TemporadasDe(catalogo))
            cboTemporada.Items.Add(_porAnio
                ? t.ToString()
                : string.Format(Textos.Instancia.FaltantesTemporadaN, t));
        cboTemporada.SelectedIndex = 0;
        cboTemporada.SelectionChanged += (_, _) => Pintar();

        Pintar();
    }

    private readonly bool _porAnio;

    /// <summary>La temporada elegida, o null si están todas (el primer elemento de la lista).</summary>
    private int? TemporadaElegida
    {
        get
        {
            if (cboTemporada.SelectedIndex <= 0) return null;
            var todas = CoberturaCatalogo.TemporadasDe(_catalogo);
            int i = cboTemporada.SelectedIndex - 1;
            return i >= 0 && i < todas.Count ? todas[i] : null;
        }
    }

    private CoberturaCatalogo.Informe _informe = null!;

    private void Pintar()
    {
        var temporada = TemporadaElegida;
        _informe = CoberturaCatalogo.Calcular(_catalogo, _resoluciones,
            soloTemporadasConAlgo: chkTodas.IsChecked != true,
            temporada: temporada);

        // Con una temporada elegida, la casilla de «incluir las que no he empezado» no pinta
        // nada: ya estás mirando una concreta, esté empezada o no.
        chkTodas.IsEnabled = temporada == null;

        lblResumen.Text = temporada == null
            ? _informe.Resumen
            : string.Format(_porAnio
                                ? Textos.Instancia.FaltantesResumenAnio
                                : Textos.Instancia.FaltantesResumenTemporada,
                            temporada, _informe.Resumen);
        lblEspeciales.Text = _informe.EspecialesQueFaltan > 0
            ? string.Format(_informe.EspecialesQueFaltan == 1
                                ? Textos.Instancia.FaltantesEspecialesUno
                                : Textos.Instancia.FaltantesEspeciales,
                            _informe.EspecialesQueFaltan)
            : "";
        lista.ItemsSource = _informe.Huecos.Select(h => new FaltaVista { Hueco = h }).ToList();
        btnCopiar.IsEnabled = _informe.Huecos.Count > 0;

        if (_informe.Huecos.Count == 0)
            lblPie.Text = temporada != null
                ? string.Format(_porAnio
                                    ? Textos.Instancia.FaltantesNadaAnio
                                    : Textos.Instancia.FaltantesNadaTemporada,
                                temporada)
                : chkTodas.IsChecked == true
                    ? Textos.Instancia.FaltantesNadaCatalogo
                    : Textos.Instancia.FaltantesNadaEmpezadas;
        else
            lblPie.Text = Textos.Instancia.FaltantesPie;
    }

    /// <summary>
    /// Al portapapeles en texto plano: sirve para pegarlo en una nota o pasárselo a alguien,
    /// que es lo que se acaba haciendo con una lista de lo que te falta.
    /// </summary>
    private void Copiar()
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
            Clipboard.SetText(sb.ToString());
            lblPie.Text = string.Format(_informe.Huecos.Count == 1
                                            ? Textos.Instancia.FaltantesCopiadoUno
                                            : Textos.Instancia.FaltantesCopiado,
                                        _informe.Huecos.Count);
        }
        catch (Exception ex)
        {
            lblPie.Text = string.Format(Textos.Instancia.FaltantesNoSePudoCopiar, ex.Message);
        }
    }
}
