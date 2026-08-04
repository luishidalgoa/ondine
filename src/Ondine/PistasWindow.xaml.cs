using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Ondine.Localizacion;

namespace Ondine;

/// <summary>Una pista en la lista, con su casilla.</summary>
public sealed class PistaVista : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new(p));

    public required Pista Pista { get; init; }
    public string Resumen => Pista.Resumen;

    /// <summary>La de vídeo no se ofrece: sin ella el resultado ya no es este vídeo.</summary>
    public bool SePuedeQuitar => Pista.Tipo != TipoPista.Video;
    public string Nota => Pista.Tipo == TipoPista.Video ? Textos.Instancia.PistasNotaSeConserva : "";

    private bool _quitar;
    public bool Quitar
    {
        get => _quitar;
        set { _quitar = value; N(); Cambio?.Invoke(); }
    }

    public Action? Cambio { get; set; }
}

/// <summary>
/// Quitar pistas de audio o subtítulos sin recomprimir.
///
/// Un fichero con varios doblajes puede llevar cientos de megas de audio que no vas a escuchar
/// nunca. Tirarlos no le hace NADA a la imagen: el vídeo se copia tal cual, no se recodifica —
/// tarda segundos en vez de minutos y el resultado es idéntico bit a bit en la parte de vídeo.
/// </summary>
public partial class PistasWindow : Window
{
    private readonly Engine _engine;
    private readonly string _path;
    private readonly int _duracion;
    private readonly List<PistaVista> _vistas;

    public bool SeCambioAlgo { get; private set; }

    public PistasWindow(Engine engine, string path, IReadOnlyList<Pista> pistas, int duracionSeg)
    {
        InitializeComponent();
        _engine = engine;
        _path = path;
        _duracion = duracionSeg;

        // El rótulo de la barra lo pone el XAML con {i:T}: asignarlo aquí rompería
        // el enlace y el título se quedaría en el idioma de arranque.
        lblFichero.Text = Path.GetFileName(path);
        _vistas = pistas.Select(p => new PistaVista { Pista = p }).ToList();
        foreach (var v in _vistas) v.Cambio = Refrescar;
        lista.ItemsSource = _vistas;

        btnCerrar.Click += (_, _) => Close();
        btnCancelar.Click += (_, _) => Close();
        btnQuitar.Click += async (_, _) => await QuitarAsync();
        Refrescar();
    }

    private SelectorDePistas.Plan PlanActual() => SelectorDePistas.Planificar(
        _vistas.Select(v => v.Pista).ToList(),
        _vistas.Where(v => v.Quitar && v.SePuedeQuitar).Select(v => v.Pista.Indice).ToList());

    /// <summary>Megas con el separador de miles del sistema, ya en texto.</summary>
    private static string Mb(double bytes) => (bytes / 1048576.0).ToString("n0");

    private void Refrescar()
    {
        var t = Textos.Instancia;
        var plan = PlanActual();
        btnQuitar.IsEnabled = plan.HayCambios;

        long ahorro = plan.BytesQueSeAhorran(_duracion);
        lblAhorro.Text = !plan.HayCambios
            ? t.PistasNadaMarcado
            : ahorro > 0
                ? string.Format(t.PistasAhorro, plan.Quitadas.Count, Mb(ahorro))
                : string.Format(t.PistasAhorroDesconocido, plan.Quitadas.Count);

        // Quedarse sin audio es legítimo, pero nadie lo espera: mejor decirlo antes.
        lblAviso.Visibility = plan.HayCambios && plan.QuedaSinAudio ? Visibility.Visible : Visibility.Collapsed;
        lblAviso.Text = t.PistasAvisoSinAudio;
    }

    private async Task QuitarAsync()
    {
        var plan = PlanActual();
        if (!plan.HayCambios) return;

        var t = Textos.Instancia;
        var queSeVa = string.Join("\n", plan.Quitadas.Select(p => $"  · {p.Resumen}"));
        if (!DialogWindow.Confirmar(this, t.PistasTitulo,
                string.Format(t.PistasConfirmarCuerpo, Path.GetFileName(_path), queSeVa),
                t.Quitar, t.Cancelar))
            return;

        btnQuitar.IsEnabled = false;
        lblAhorro.Text = t.PistasReempaquetando;
        var (ok, msg, antes, despues) = await _engine.QuitarPistasAsync(_path, plan);
        if (ok)
        {
            SeCambioAlgo = true;
            string mbAntes = Mb(antes), mbDespues = Mb(despues), mbAhorrado = Mb(antes - despues);
            lblAhorro.Text = string.Format(t.PistasHecho, mbAntes, mbDespues, mbAhorrado);
            DialogWindow.Aviso(this, t.Listo,
                string.Format(t.PistasHechoCuerpo, mbAntes, mbDespues, mbAhorrado));
            Close();
        }
        else
        {
            lblAhorro.Text = t.PistasNoSePudo;
            btnQuitar.IsEnabled = true;
            // `msg` lo escribe ffmpeg: es un diagnóstico, no un texto de interfaz.
            DialogWindow.Aviso(this, t.PistasFalloTitulo, msg);
        }
    }
}
