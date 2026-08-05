using System.Windows;
using System.Windows.Input;
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

        // Se aplica aquí y no al cerrar: los textos están enlazados, así que la
        // app entera cambia de idioma sola en cuanto se toca esto.
        Idioma.Actual = Idioma.Resolver(s.Idioma);

        Result = s;
        DialogResult = true;
    }
}
