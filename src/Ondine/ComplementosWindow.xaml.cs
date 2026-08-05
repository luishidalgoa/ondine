using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ondine.Complementos;
using Ondine.Localizacion;
using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// La pantalla de complementos: elegir uno, pedirle qué hay en una fuente, y
/// marcar qué traer.
///
/// <para>
/// Lo que la hace útil no es la lista: es el <b>cotejo</b>. Una lista de
/// cuatrocientos vídeos sin más obliga a ir uno por uno acordándose de qué se
/// tiene. Con el catálogo abierto en Organizar al lado, cada fila dice si eso ya
/// está, está a medias -y qué historia falta- o no está. El botón que de verdad
/// se usa es «marcar los que faltan».
/// </para>
/// </summary>
public partial class ComplementosWindow : Window
{
    /// <summary>Una fila de la lista. Notifica porque la casilla se marca desde varios sitios.</summary>
    public sealed class Fila : INotifyPropertyChanged
    {
        public required string Id { get; init; }
        public required string Titulo { get; init; }
        public string? Miniatura { get; init; }
        public TimeSpan? Duracion { get; init; }

        public string Veredicto { get; set; } = "";
        public string Detalle { get; set; } = "";
        public Brush ColorFondo { get; set; } = Brushes.Transparent;
        public Brush ColorTexto { get; set; } = Brushes.Gray;

        /// <summary>Si no se sabe qué es, no se marca por defecto ni con «los que faltan».</summary>
        public bool Falta { get; set; }

        private bool _marcado;
        public bool Marcado
        {
            get => _marcado;
            set { if (_marcado == value) return; _marcado = value; Avisar(nameof(Marcado)); Cambio?.Invoke(); }
        }

        public event Action? Cambio;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Avisar(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    private readonly ObservableCollection<Fila> _filas = new();
    private readonly ReindexCatalog? _catalogo;
    private readonly IReadOnlyList<ReindexResolution> _loQueHay;
    private CancellationTokenSource? _corte;

    /// <param name="catalogo">
    /// El que esté abierto en Organizar, si lo hay. Sin él la ventana sigue
    /// sirviendo -se ve la lista y se puede traer- pero no puede decir qué falta,
    /// que es la mitad de su gracia.
    /// </param>
    /// <param name="loQueHay">Las filas ya resueltas de la carpeta abierta.</param>
    public ComplementosWindow(ReindexCatalog? catalogo, IReadOnlyList<ReindexResolution>? loQueHay)
    {
        InitializeComponent();
        _catalogo = catalogo;
        _loQueHay = loQueHay ?? Array.Empty<ReindexResolution>();

        header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        btnX.Click += (_, _) => Close();
        btnCerrar.Click += (_, _) => Close();

        lista.ItemsSource = _filas;
        btnListar.Click += async (_, _) => await ListarAsync();
        btnSoloFaltan.Click += (_, _) => { foreach (var f in _filas) f.Marcado = f.Falta; };
        btnNinguno.Click += (_, _) => { foreach (var f in _filas) f.Marcado = false; };
        btnTraer.Click += (_, _) => Traer();

        Closed += (_, _) => _corte?.Cancel();

        CargarComplementos();
    }

    private void CargarComplementos()
    {
        var h = Descubridor.Buscar();

        foreach (var c in h.Bueno.Where(c => c.Puede(Complemento.CapacidadImportar)))
            cboComplemento.Items.Add(new Opcion(c));
        if (cboComplemento.Items.Count > 0) cboComplemento.SelectedIndex = 0;

        if (h.Descartado.Count > 0)
        {
            cajaDescartados.Visibility = Visibility.Visible;
            listaDescartados.ItemsSource = h.Descartado
                .Select(d => $"{d.Cual.Id}: {d.Motivo}")
                .ToList();
        }

        if (cboComplemento.Items.Count == 0)
        {
            btnListar.IsEnabled = false;
            lblVacio.Text = string.Format(Textos.Instancia.ComplementosNinguno, Descubridor.Carpeta);
        }
    }

    /// <summary>Envoltorio para que el desplegable enseñe el nombre y guarde el objeto.</summary>
    private sealed record Opcion(Complemento Cual)
    {
        public override string ToString() => Cual.Nombre;
    }

    private async Task ListarAsync()
    {
        if (cboComplemento.SelectedItem is not Opcion op) return;

        _corte?.Cancel();
        _corte = new CancellationTokenSource();

        _filas.Clear();
        lista.Visibility = Visibility.Collapsed;
        lblVacio.Visibility = Visibility.Visible;
        lblVacio.Text = Textos.Instancia.ComplementosListando;
        btnListar.IsEnabled = false;
        btnTraer.IsEnabled = false;

        var fuente = txtFuente.Text?.Trim() ?? "";
        string? error = null;

        try
        {
            await foreach (var m in Invocador.CorrerAsync(
                op.Cual, Invocador.ComandoListar,
                fuente.Length > 0 ? new[] { fuente } : Array.Empty<string>(), _corte.Token))
            {
                if (m.Tipo == Mensaje.TipoError) { error = m.MensajeError; continue; }
                if (m.Tipo != Mensaje.TipoElemento) continue;

                var f = new Fila
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Miniatura = m.Miniatura,
                    Duracion = m.ComoDuracion,
                };
                f.Cambio += RefrescarPie;
                _filas.Add(f);
                // Se va pintando según llega: con listas largas, esperar al final
                // deja la ventana en blanco durante todo el rato que tarde.
                lista.Visibility = Visibility.Visible;
                lblVacio.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException) { }
        finally { btnListar.IsEnabled = true; }

        Cotejar();
        RefrescarPie();

        if (_filas.Count == 0)
        {
            lblVacio.Visibility = Visibility.Visible;
            lblVacio.Text = error ?? Textos.Instancia.ComplementosNadaEnLaFuente;
        }
        else if (error is not null)
        {
            lblEstado.Text = error;
        }
    }

    /// <summary>
    /// Pone el semáforo. Sin catálogo abierto no se inventa nada: se dice que no
    /// se puede saber, en vez de dejar las filas mudas y que parezca un fallo.
    /// </summary>
    private void Cotejar()
    {
        if (_filas.Count == 0) return;

        if (_catalogo is null)
        {
            foreach (var f in _filas)
            {
                f.Veredicto = Textos.Instancia.ComplementosSinCatalogo;
                f.Detalle = Duracion(f);
                f.Falta = false;
            }
            return;
        }

        var veredictos = CotejoDeLista.Cotejar(_filas.Select(f => f.Titulo), _catalogo, _loQueHay);

        for (int i = 0; i < _filas.Count && i < veredictos.Count; i++)
        {
            var f = _filas[i];
            var v = veredictos[i];

            (f.Veredicto, f.ColorFondo, f.ColorTexto, f.Falta) = v.Estado switch
            {
                CotejoDeLista.Estado.YaEsta =>
                    (Textos.Instancia.ComplementosYaEsta, Pastilla("#1E3A2A"), Tinta("#7FD1A6"), false),
                CotejoDeLista.Estado.AMedias =>
                    (string.Format(Textos.Instancia.ComplementosAMedias, string.Join(", ", v.HistoriasQueFaltan)),
                     Pastilla("#3A3320"), Tinta("#E0C07A"), true),
                CotejoDeLista.Estado.Falta =>
                    (Textos.Instancia.ComplementosFalta, Pastilla("#2A2440"), Tinta("#B5ABFC"), true),
                _ =>
                    (Textos.Instancia.ComplementosDesconocido, Pastilla("#26272E"), Tinta("#8A8FA3"), false),
            };

            f.Detalle = v.Episodio is { } ep
                ? $"{Duracion(f)}  ·  {Textos.Instancia.ComplementosEpisodio} {ep.Num}"
                : Duracion(f);
        }

        // La lista se repinta entera: las filas cambian de veredicto todas a la
        // vez y notificar propiedad a propiedad no aporta nada aquí.
        lista.Items.Refresh();
    }

    private static string Duracion(Fila f) => f.Duracion is { } d
        ? (d.TotalHours >= 1 ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}" : $"{d.Minutes}:{d.Seconds:00}")
        : "";

    private static Brush Pastilla(string hex) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private static Brush Tinta(string hex) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    private void RefrescarPie()
    {
        int marcados = _filas.Count(f => f.Marcado);
        btnTraer.IsEnabled = marcados > 0;
        lblEstado.Text = _filas.Count == 0
            ? ""
            : string.Format(Textos.Instancia.ComplementosResumen, marcados, _filas.Count,
                _filas.Count(f => f.Falta));
    }

    /// <summary>
    /// Todavía no baja nada: falta decidir dónde deja los ficheros y cómo se
    /// entregan a Organizar. Se avisa en vez de no hacer nada al pulsar, que es
    /// la forma más rápida de que alguien piense que la aplicación se ha colgado.
    /// </summary>
    private void Traer()
    {
        DialogWindow.Aviso(this, Textos.Instancia.ComplementosTraer,
            Textos.Instancia.ComplementosTraerPendiente);
    }
}
