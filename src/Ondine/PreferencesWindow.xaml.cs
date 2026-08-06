using System.Windows;
using System.Windows.Input;
using Ondine.Ia;
using Ondine.Localizacion;

namespace Ondine;

public partial class PreferencesWindow : Window
{
    /// <summary>
    /// El «ninguno» del desplegable de presets, que además hace de centinela: si
    /// es lo elegido, no hay preset por defecto.
    ///
    /// <para>
    /// Se guarda en un campo en vez de pedírselo a <see cref="Textos"/> cada vez
    /// porque así el texto que se mete en la lista y el que se compara en
    /// <see cref="Save"/> son el MISMO. Pidiéndolo dos veces, cambiar de idioma
    /// con la ventana abierta dejaría un elemento en el idioma viejo que ya no
    /// casaría con nada, y «ninguno» pasaría por ser el nombre de un preset.
    /// </para>
    /// </summary>
    private readonly string _sinPreset = Textos.Instancia.PreferenciasSinPreset;

    /// <summary>
    /// Los códigos del desplegable de idioma, en el mismo orden que sus
    /// entradas. El primero es <c>""</c>: «el del sistema».
    ///
    /// <para>
    /// Va en una lista aparte y se casa por posición en vez de meter objetos
    /// en el desplegable. Con objetos hay que enlazar a una propiedad, y el
    /// enlace de WPF no alcanza los tipos que no son públicos: no da error, se
    /// queda callado y pinta el <c>ToString()</c> del objeto. Un desplegable
    /// que enseñaba «OpcionIdioma { Codigo = ... }» costó verlo solo porque se
    /// abrió la ventana.
    /// </para>
    /// </summary>
    private readonly List<string> _codigosIdioma = [];

    /// <summary>Los ajustes con los que se abrió, para no perder lo que aquí no se edita.</summary>
    private readonly Settings _previos;

    /// <summary>Ajustes resultantes tras pulsar Guardar (null si se cancela).</summary>
    public Settings? Result { get; private set; }

    public PreferencesWindow(Settings current, IEnumerable<string> presetNames)
    {
        InitializeComponent();
        _previos = current;

        header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        btnX.Click += (_, _) => Close();
        btnCancel.Click += (_, _) => Close();
        btnSave.Click += (_, _) => Save();

        // General
        // «El del sistema» va con código vacío, que es justo lo que significa
        // el ajuste vacío: no he elegido, decide tú.
        _codigosIdioma.Add("");
        cboIdioma.Items.Add(Textos.Instancia.PreferenciasIdiomaAppSistema);
        foreach (var c in Idioma.Disponibles)
        {
            _codigosIdioma.Add(c);
            // Cada idioma escrito EN ese idioma: quien busca «Español» lo
            // reconoce aunque la app esté ahora mismo en inglés.
            cboIdioma.Items.Add(Idioma.Nombre(c));
        }
        var pos = _codigosIdioma.IndexOf(current.Idioma);
        cboIdioma.SelectedIndex = pos >= 0 ? pos : 0;

        cboDefPreset.Items.Add(_sinPreset);
        foreach (var n in presetNames) cboDefPreset.Items.Add(n);
        // El ajuste guarda el NOMBRE del preset, que en los de fábrica está
        // traducido: si se eligió con la app en el otro idioma, hay que pasarlo
        // por NombreVigente o el preset por defecto parecería haber desaparecido.
        var guardado = PresetStore.NombreVigente(current.DefaultPreset);
        cboDefPreset.SelectedItem = cboDefPreset.Items.Contains(guardado) ? guardado : _sinPreset;
        txtDefLang.Text = current.DefaultLang;
        chkRecurse.IsChecked = current.Recurse;
        chkUpdates.IsChecked = current.CheckUpdatesOnStart;
        // el estado real lo manda el registro, no un ajuste guardado
        chkShell.IsChecked = ShellIntegration.IsRegistered();

        // Al comprimir
        rbAsk.IsChecked = current.AfterCompress == AfterCompress.Ask;
        rbRecycle.IsChecked = current.AfterCompress == AfterCompress.RecycleOriginal;
        rbKeep.IsChecked = current.AfterCompress == AfterCompress.Keep;

        // Rendimiento y disco
        txtMinFree.Text = current.MinFreeMb.ToString();
        chkHw.IsChecked = current.UseHardware;

        // Modelo. Se trabaja sobre una COPIA: probar la conexión necesita la
        // clave ya guardada, y si se cancela la ventana nada de esto se aplica.
        _ia = current.Ia.Clone();
        chkIa.IsChecked = _ia.Activo;
        txtIaUrl.Text = _ia.BaseUrl;
        txtIaModelo.Text = _ia.Modelo;
        PintarClave();

        btnIaOlvidar.Click += (_, _) =>
        {
            _ia.PonerClave(null);
            txtIaClave.Clear();
            lblIaProbar.Text = Textos.Instancia.IaClaveOlvidada;
            PintarClave();
        };
        btnIaProbar.Click += async (_, _) => await ProbarIa();
    }

    // ── Modelo de lenguaje ──

    private readonly AjustesDeModelo _ia;

    /// <summary>
    /// La clave guardada NO se rellena en el campo: no se saca a pantalla algo
    /// que ya está a salvo. Se dice que la hay, y escribir encima la reemplaza.
    /// </summary>
    private void PintarClave()
    {
        bool hay = _ia.TieneClave;
        btnIaOlvidar.Visibility = hay ? Visibility.Visible : Visibility.Collapsed;
        lblIaClaveNota.Text = hay ? Textos.Instancia.IaClaveGuardada : Textos.Instancia.IaClaveAyuda;
    }

    /// <summary>
    /// Prueba con lo que hay AHORA en los campos, no con lo guardado: probar y
    /// que te conteste algo que no es lo que estás escribiendo es peor que no
    /// tener el botón.
    /// </summary>
    private async Task ProbarIa()
    {
        var prueba = _ia.Clone();
        prueba.Activo = true;
        prueba.BaseUrl = txtIaUrl.Text.Trim();
        prueba.Modelo = txtIaModelo.Text.Trim();
        if (txtIaClave.Password.Length > 0) prueba.PonerClave(txtIaClave.Password);

        btnIaProbar.IsEnabled = false;
        lblIaProbar.Text = Textos.Instancia.IaProbando;
        try
        {
            var r = await ModeloConectado.ProbarAsync(prueba);
            lblIaProbar.Text = r.Texto != null
                ? Textos.Instancia.IaProbadoBien
                : string.Format(Textos.Instancia.IaProbadoMal, r.Error);
        }
        finally { btnIaProbar.IsEnabled = true; }
    }

    private void Save()
    {
        // La integración con el Explorador se aplica al guardar, no al marcar la casilla.
        bool quiere = chkShell.IsChecked == true;
        if (quiere != ShellIntegration.IsRegistered())
        {
            bool ok = quiere ? ShellIntegration.Register() : ShellIntegration.Unregister();
            if (!ok)
                DialogWindow.Aviso(this, Textos.Instancia.PreferenciasMenuAvisoTitulo,
                    quiere ? Textos.Instancia.PreferenciasMenuAvisoAlta
                           : Textos.Instancia.PreferenciasMenuAvisoBaja);
        }

        // Se PARTE de los ajustes de entrada y solo se pisa lo que este diálogo
        // edita. Construyendo un Settings nuevo se perdía en cada guardado todo
        // lo que no sale en esta ventana: el historial de renombrado y el factor
        // de complejidad que la app aprende midiendo.
        var s = _previos.Clone();

        s.Idioma = cboIdioma.SelectedIndex >= 0 ? _codigosIdioma[cboIdioma.SelectedIndex] : "";
        s.DefaultPreset = cboDefPreset.SelectedItem is string p && p != _sinPreset ? p : "";
        s.DefaultLang = string.IsNullOrWhiteSpace(txtDefLang.Text) ? "spa" : txtDefLang.Text.Trim();
        s.Recurse = chkRecurse.IsChecked == true;
        s.CheckUpdatesOnStart = chkUpdates.IsChecked == true;
        s.AfterCompress = rbRecycle.IsChecked == true ? AfterCompress.RecycleOriginal
                        : rbKeep.IsChecked == true ? AfterCompress.Keep
                        : AfterCompress.Ask;
        s.MinFreeMb = int.TryParse(txtMinFree.Text.Trim(), out var mb) ? Math.Clamp(mb, 50, 100_000) : 200;
        s.UseHardware = chkHw.IsChecked == true;

        _ia.Activo = chkIa.IsChecked == true;
        _ia.BaseUrl = txtIaUrl.Text.Trim();
        _ia.Modelo = txtIaModelo.Text.Trim();
        // Vacío = «no la cambies». Lo contrario haría que abrir preferencias para
        // tocar cualquier otra cosa borrase la clave de paso.
        if (txtIaClave.Password.Length > 0) _ia.PonerClave(txtIaClave.Password);
        s.Ia = _ia;

        // Se aplica aquí y no al cerrar: los textos están enlazados, así que la
        // app entera cambia de idioma sola en cuanto se toca esto.
        Idioma.Actual = Idioma.Resolver(s.Idioma);

        Result = s;
        DialogResult = true;
    }
}
