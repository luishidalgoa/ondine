using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ondine.Localizacion;

namespace Ondine.Ava;

/// <summary>
/// La pantalla de una biblioteca de <b>películas</b>, portada de <c>PeliculasPanel</c>.
///
/// <para>
/// Es un componente aparte, no la pantalla de series con piezas escondidas. Se probó lo
/// segundo y estaba mal: al elegir «Películas» seguían a la vista el panel de catálogos,
/// «Partir en segmentos» y «Ordenar por temporadas», que no aplican a una película. Ocultar
/// cosas una a una deja una pantalla llena de huecos, y una pantalla con huecos enseña a
/// desconfiar de lo que queda.
/// </para>
/// <para>
/// Aquí no hay catálogo —no existe el anexo del que sacarlo—, ni plantilla, ni temporadas,
/// ni segmentos. Hay una carpeta y una acción.
/// </para>
/// </summary>
public partial class PeliculasPanel : UserControl
{
    private IReadOnlyList<string> _ficheros = [];

    /// <summary>Se pide cambiar de carpeta. Lo resuelve quien nos aloja, que ya tiene el selector.</summary>
    public event Action? PidenOtraCarpeta;

    /// <summary>
    /// Se pide analizar. El repaso lo pinta quien nos aloja, en la pantalla principal y con
    /// la forma del repaso de series — no en una ventana aparte. La primera versión abría un
    /// diálogo modal con una lista simple, y era la pantalla que menos confianza daba de la
    /// app: sin casilla por fila, aplicar era todas o ninguna.
    /// </summary>
    public event Action? PidenAnalizar;

    public PeliculasPanel()
    {
        AvaloniaXamlLoader.Load(this);

        Campo().IsReadOnly = true;
        Btn("btnCarpeta").Click += (_, _) => PidenOtraCarpeta?.Invoke();
        Btn("btnOrdenar").Click += (_, _) => { if (_ficheros.Count > 0) PidenAnalizar?.Invoke(); };
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CampoTexto Campo() => this.FindControl<CampoTexto>("txtCarpeta")!;

    /// <summary>Lo que hay ahora mismo: la carpeta y los vídeos que se han encontrado.</summary>
    public void Poner(string carpeta, IReadOnlyList<string> ficheros)
    {
        _ficheros = ficheros;
        Campo().Text = carpeta;

        Lbl("lblFicheros").Text = ficheros.Count switch
        {
            0 when !Directory.Exists(carpeta) => Textos.Instancia.OrganizarElegirCarpeta,
            0 => Textos.Instancia.OrganizarSinVideos,
            1 => Textos.Instancia.PeliculasUnaPelicula,
            var n => string.Format(Textos.Instancia.PeliculasCuantas, n),
        };

        Btn("btnOrdenar").IsEnabled = ficheros.Count > 0;
        Lbl("lblPie").Text = "";

        PintarQueSabe();
    }

    /// <summary>
    /// Lo que esta pantalla sabe hacer, según esté encendida la consulta a TMDb.
    ///
    /// <para>
    /// Se lee de los ajustes cada vez, y no se guarda: si se acaban de tocar las
    /// preferencias, lo que vale es lo de ahora. Y cambia de color además de texto —apagado
    /// es un aviso de lo que NO se puede hacer, encendido es información— porque un aviso
    /// ámbar permanente enseña a no leerlo.
    /// </para>
    /// </summary>
    private void PintarQueSabe()
    {
        bool con = SettingsStore.Load().Tmdb.Activo;

        Lbl("lblBaseDeDatos").Text = con ? Textos.Instancia.PeliculasConBaseDeDatos
                                         : Textos.Instancia.PeliculasSinBaseDeDatos;

        Lbl("lblBaseDeDatos").Foreground = Pincel(con ? "Neutral500" : "OrgWarn");
        var caja = this.FindControl<Border>("cajaBaseDeDatos")!;
        caja.Background = Pincel(con ? "Field" : "OrgWarnBg");
        caja.BorderBrush = Pincel(con ? "Divider" : "OrgWarnBorder");
    }

    private IBrush Pincel(string clave) =>
        this.TryFindResource(clave, out var v) && v is IBrush b ? b : Brushes.Gray;
}
