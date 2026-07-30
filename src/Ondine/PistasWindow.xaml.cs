using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

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
    public string Nota => Pista.Tipo == TipoPista.Video ? "se conserva siempre" : "";

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

        lblTitulo.Text = "Quitar pistas";
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

    private void Refrescar()
    {
        var plan = PlanActual();
        btnQuitar.IsEnabled = plan.HayCambios;

        long ahorro = plan.BytesQueSeAhorran(_duracion);
        lblAhorro.Text = !plan.HayCambios
            ? "Marca alguna pista para quitarla."
            : ahorro > 0
                ? $"Se quitan {plan.Quitadas.Count} pista(s) · unos {ahorro / 1048576.0:n0} MB menos"
                : $"Se quitan {plan.Quitadas.Count} pista(s) · el ahorro será pequeño (no declaran su caudal)";

        // Quedarse sin audio es legítimo, pero nadie lo espera: mejor decirlo antes.
        lblAviso.Visibility = plan.HayCambios && plan.QuedaSinAudio ? Visibility.Visible : Visibility.Collapsed;
        lblAviso.Text = "Ojo: así el vídeo se queda SIN audio.";
    }

    private async Task QuitarAsync()
    {
        var plan = PlanActual();
        if (!plan.HayCambios) return;

        var queSeVa = string.Join("\n", plan.Quitadas.Select(p => $"  · {p.Resumen}"));
        if (!DialogWindow.Confirmar(this, "Quitar pistas",
                $"Se van a quitar de «{Path.GetFileName(_path)}»:\n\n{queSeVa}\n\n" +
                "El vídeo NO se recomprime: se copia tal cual, así que la imagen queda idéntica.\n" +
                "El fichero original va a la Papelera (recuperable con Ctrl+Z).",
                "Quitar", "Cancelar"))
            return;

        btnQuitar.IsEnabled = false;
        lblAhorro.Text = "Reempaquetando…";
        var (ok, msg, antes, despues) = await _engine.QuitarPistasAsync(_path, plan);
        if (ok)
        {
            SeCambioAlgo = true;
            double ahorrado = (antes - despues) / 1048576.0;
            lblAhorro.Text = $"Listo · {antes / 1048576.0:n0} MB → {despues / 1048576.0:n0} MB " +
                             $"({ahorrado:n0} MB menos)";
            DialogWindow.Aviso(this, "Listo",
                $"Pistas quitadas.\n\n{antes / 1048576.0:n0} MB → {despues / 1048576.0:n0} MB " +
                $"({ahorrado:n0} MB menos, sin tocar la imagen).");
            Close();
        }
        else
        {
            lblAhorro.Text = "No se pudo.";
            btnQuitar.IsEnabled = true;
            DialogWindow.Aviso(this, "No se pudo quitar", msg);
        }
    }
}
