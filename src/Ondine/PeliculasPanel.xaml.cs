using System.IO;
using System.Windows;
using System.Windows.Controls;
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
    }

    private void Ordenar()
    {
        if (_ficheros.Count == 0) return;

        var v = new PeliculasWindow(_ficheros, _carpeta) { Owner = Window.GetWindow(this) };
        v.ShowDialog();

        if (v.MovioAlgo) MovioAlgo?.Invoke();
    }
}
