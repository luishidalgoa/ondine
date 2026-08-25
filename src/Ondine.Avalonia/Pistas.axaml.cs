using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Ondine.Localizacion;

namespace Ondine.Ava;

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
/// «Quitar pistas», portado de <c>PistasWindow</c>.
///
/// <para>
/// Un fichero con varios doblajes puede llevar cientos de megas de audio que no vas a
/// escuchar nunca. Tirarlos no le hace NADA a la imagen: el vídeo se copia tal cual, no se
/// recodifica — tarda segundos en vez de minutos y el resultado es idéntico bit a bit en la
/// parte de vídeo.
/// </para>
/// <para>
/// <b>El coste del puerto está donde avisaba la Fase 4: los modales.</b> Aquí había tres
/// —confirmar, «listo» y el fallo— y los tres eran síncronos en WPF. Al pasar a
/// <c>await</c>, «quitar» deja de ser un método que va y vuelve: entre la pregunta y la
/// respuesta la ventana sigue viva y el usuario puede darle otra vez al botón. Por eso el
/// botón se apaga ANTES de preguntar y se vuelve a encender si se cancela, no solo si falla.
/// </para>
/// </summary>
public partial class Pistas : Window
{
    private readonly Engine _engine = null!;
    private readonly string _path = "";
    private readonly int _duracion;
    private readonly List<PistaVista> _vistas = [];

    public bool SeCambioAlgo { get; private set; }

        /// <summary>
    /// Sin marco del sistema hay que pedir el arrastre y los bordes: los da
    /// <see cref="ArrastrarLaVentana"/>. Esta ventana se quedaba clavada donde el sistema la
    /// abriera.
    /// </summary>
    public Pistas()
    {
        AvaloniaXamlLoader.Load(this);
        ArrastrarLaVentana.Enganchar(this);
    }

    public Pistas(Engine engine, string path, IReadOnlyList<Pista> pistas, int duracionSeg) : this()
    {
        _engine = engine;
        _path = path;
        _duracion = duracionSeg;

        // El rótulo de la barra lo pone el XAML con {i:T}: asignarlo aquí rompería
        // el enlace y el título se quedaría en el idioma de arranque.
        Lbl("lblFichero").Text = Path.GetFileName(path);
        _vistas = pistas.Select(p => new PistaVista { Pista = p }).ToList();
        foreach (var v in _vistas) v.Cambio = Refrescar;
        this.FindControl<ListBox>("lista")!.ItemsSource = _vistas;

        Btn("btnCerrar").Click += (_, _) => Close();
        Btn("btnCancelar").Click += (_, _) => Close();
        Btn("btnQuitar").Click += async (_, _) => await QuitarAsync();
        Refrescar();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;

    private SelectorDePistas.Plan PlanActual() => SelectorDePistas.Planificar(
        _vistas.Select(v => v.Pista).ToList(),
        _vistas.Where(v => v.Quitar && v.SePuedeQuitar).Select(v => v.Pista.Indice).ToList());

    /// <summary>Megas con el separador de miles del sistema, ya en texto.</summary>
    private static string Mb(double bytes) => (bytes / 1048576.0).ToString("n0");

    private void Refrescar()
    {
        var t = Textos.Instancia;
        var plan = PlanActual();
        Btn("btnQuitar").IsEnabled = plan.HayCambios;

        long ahorro = plan.BytesQueSeAhorran(_duracion);
        Lbl("lblAhorro").Text = !plan.HayCambios
            ? t.PistasNadaMarcado
            : ahorro > 0
                ? string.Format(t.PistasAhorro, plan.Quitadas.Count, Mb(ahorro))
                : string.Format(t.PistasAhorroDesconocido, plan.Quitadas.Count);

        // Quedarse sin audio es legítimo, pero nadie lo espera: mejor decirlo antes.
        var aviso = Lbl("lblAviso");
        aviso.IsVisible = plan.HayCambios && plan.QuedaSinAudio;
        aviso.Text = t.PistasAvisoSinAudio;
    }

    private async Task QuitarAsync()
    {
        var plan = PlanActual();
        if (!plan.HayCambios) return;

        var t = Textos.Instancia;
        var queSeVa = string.Join("\n", plan.Quitadas.Select(p => $"  · {p.Resumen}"));

        // Apagado ANTES de preguntar: el modal es asíncrono y la ventana sigue viva
        // mientras se decide. En WPF esto no hacía falta porque la llamada bloqueaba.
        Btn("btnQuitar").IsEnabled = false;

        if (!await Dialogo.Confirmar(this, t.PistasTitulo,
                string.Format(t.PistasConfirmarCuerpo, Path.GetFileName(_path), queSeVa),
                t.Quitar, t.Cancelar))
        {
            Btn("btnQuitar").IsEnabled = true;
            return;
        }

        Lbl("lblAhorro").Text = t.PistasReempaquetando;
        var (ok, msg, antes, despues) = await _engine.QuitarPistasAsync(_path, plan);
        if (ok)
        {
            SeCambioAlgo = true;
            string mbAntes = Mb(antes), mbDespues = Mb(despues), mbAhorrado = Mb(antes - despues);
            Lbl("lblAhorro").Text = string.Format(t.PistasHecho, mbAntes, mbDespues, mbAhorrado);
            await Dialogo.Aviso(this, t.Listo,
                string.Format(t.PistasHechoCuerpo, mbAntes, mbDespues, mbAhorrado));
            Close();
        }
        else
        {
            Lbl("lblAhorro").Text = t.PistasNoSePudo;
            Btn("btnQuitar").IsEnabled = true;
            // `msg` lo escribe ffmpeg: es un diagnóstico, no un texto de interfaz.
            await Dialogo.Aviso(this, t.PistasFalloTitulo, msg);
        }
    }
}
