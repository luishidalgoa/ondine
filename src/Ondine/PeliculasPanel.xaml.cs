using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ondine.Localizacion;

namespace Ondine;

/// <summary>
/// La pantalla de una biblioteca de <b>películas</b>.
///
/// <para>
/// Es un componente aparte, no la pantalla de series con piezas escondidas. Se
/// probó lo segundo y estaba mal: al elegir «Películas» seguían a la vista el
/// panel de catálogos, «Partir en segmentos» y «Ordenar por temporadas», que no
/// aplican a una película. Ocultarlos uno a uno deja una pantalla llena de
/// huecos, y una pantalla con huecos enseña a desconfiar de lo que queda.
/// </para>
/// <para>
/// Aquí no hay catálogo —no existe el anexo del que sacarlo—, ni plantilla, ni
/// temporadas, ni segmentos. Hay una carpeta y una acción.
/// </para>
/// </summary>
public partial class PeliculasPanel : UserControl
{
    private string _carpeta = "";
    private IReadOnlyList<string> _ficheros = Array.Empty<string>();

    /// <summary>Se pide cambiar de carpeta. Lo resuelve quien nos aloja, que ya tiene el selector.</summary>
    public event Action? PidenOtraCarpeta;

    /// <summary>Se movió algo y las rutas de fuera han dejado de valer.</summary>
    public event Action? MovioAlgo;

    public PeliculasPanel()
    {
        InitializeComponent();
        txtCarpeta.IsReadOnly = true;
        btnCarpeta.Click += (_, _) => PidenOtraCarpeta?.Invoke();
        btnOrdenar.Click += (_, _) => Ordenar();
    }

    /// <summary>Lo que hay ahora mismo: la carpeta y los vídeos que se han encontrado.</summary>
    public void Poner(string carpeta, IReadOnlyList<string> ficheros)
    {
        _carpeta = carpeta;
        _ficheros = ficheros;

        txtCarpeta.Text = carpeta;

        lblFicheros.Text = ficheros.Count switch
        {
            0 when !Directory.Exists(carpeta) => Textos.Instancia.OrganizarElegirCarpeta,
            0 => Textos.Instancia.OrganizarSinVideos,
            1 => Textos.Instancia.PeliculasUnaPelicula,
            var n => string.Format(Textos.Instancia.PeliculasCuantas, n),
        };

        btnOrdenar.IsEnabled = ficheros.Count > 0;
        lblPie.Text = "";

        PintarQueSabe();
    }

    /// <summary>
    /// Lo que esta pantalla sabe hacer, según esté encendida la consulta a TMDb.
    ///
    /// <para>
    /// Se lee de los ajustes cada vez, y no se guarda: si se acaban de tocar las
    /// preferencias, lo que vale es lo de ahora. Y cambia de color además de
    /// texto — apagado es un aviso de lo que NO se puede hacer, encendido es
    /// información— porque un aviso ámbar permanente enseña a no leerlo.
    /// </para>
    /// </summary>
    private void PintarQueSabe()
    {
        bool con = SettingsStore.Load().Tmdb.Activo;

        lblBaseDeDatos.Text = con ? Textos.Instancia.PeliculasConBaseDeDatos
                                  : Textos.Instancia.PeliculasSinBaseDeDatos;

        lblBaseDeDatos.Foreground = (Brush)FindResource(con ? "Neutral500" : "OrgWarn");
        cajaBaseDeDatos.Background = (Brush)FindResource(con ? "Field" : "OrgWarnBg");
        cajaBaseDeDatos.BorderBrush = (Brush)FindResource(con ? "Divider" : "OrgWarnBorder");
    }

    private void Ordenar()
    {
        if (_ficheros.Count == 0) return;

        // Los ajustes se leen AQUÍ y no se guardan en el panel: si se han tocado
        // las preferencias con la app abierta, lo que vale es lo de ahora.
        var v = new PeliculasWindow(_ficheros, _carpeta, SettingsStore.Load())
        {
            Owner = Window.GetWindow(this),
        };
        v.ShowDialog();

        if (v.MovioAlgo) MovioAlgo?.Invoke();
    }
}
