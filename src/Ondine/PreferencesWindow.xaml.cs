using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ondine.Ia;
using Ondine.Localizacion;
using Ondine.Peliculas;

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

    /// <summary>
    /// Los codigos de la aceleracion, en el MISMO orden que los textos del desplegable: el
    /// guardado indexa esta lista con el SelectedIndex. Es el patron que ya usa el desplegable
    /// de idioma, y el orden es la unica cosa que los ata.
    /// </summary>
    private readonly List<string> _codigosAcel = new();

    /// <summary>Igual que los de la aceleracion: en el MISMO orden que los textos.</summary>
    private readonly List<string> _codigosCodificador = new();

    private static Textos T => Textos.Instancia;

    /// <summary>Ajustes resultantes tras pulsar Guardar (null si se cancela).</summary>
    public Settings? Result { get; private set; }

    /// <param name="aceleraciones">
    /// Las aceleraciones de decodificacion que FUNCIONAN en esta maquina, sondeadas por el
    /// motor. Se pasan de fuera y no se sondean aqui por dos razones: la sonda arranca ffmpeg
    /// una vez por candidata -casi dos segundos- y una ventana no debe hacer eso mientras se
    /// abre; y asi esta ventana sigue siendo pura, que es lo que la deja probarse.
    /// </param>
    public PreferencesWindow(Settings current, IEnumerable<string> presetNames,
                             IEnumerable<string>? aceleraciones = null,
                             IEnumerable<string>? codificadores = null)
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

        // El desplegable de la aceleracion: «Automatica», las que funcionan, y «Ninguna».
        //
        // Solo las que FUNCIONAN. La lista de ffmpeg -hwaccels no vale: en la maquina donde se
        // escribio esto ofrecia siete y arrancaban tres -pedir «cuda» sin NVIDIA no cae a
        // software, se muere-. Ofrecer las que no van seria invitar a elegir un fallo.
        _codigosAcel.Add(Ondine.Objetivo.AceleracionDeVideo.Auto);
        var textosAcel = new List<string> { T.PreferenciasAceleracionAuto };
        foreach (var a in aceleraciones ?? [])
        {
            _codigosAcel.Add(a);
            textosAcel.Add(a);      // «cuda», «qsv»… son nombres de ffmpeg: no se traducen
        }
        _codigosAcel.Add(Ondine.Objetivo.AceleracionDeVideo.Ninguna);
        textosAcel.Add(_codigosAcel.Count == 2 ? T.PreferenciasAceleracionNoHay      // solo auto y ninguna
                                               : T.PreferenciasAceleracionNinguna);

        // El codificador: «Automatico», «Por software», y los de hardware que ARRANCAN aqui.
        // Se sacan de la lista de aceleraciones? No: son cosas distintas -una decodifica y otro
        // codifica-, asi que se sondean por su cuenta a traves del motor.
        _codigosCodificador.Add("");
        var textosCod = new List<string> { T.PreferenciasCodificadorAuto };
        _codigosCodificador.Add(Engine.PorSoftware);
        textosCod.Add(T.PreferenciasCodificadorSoftware);
        foreach (var c in codificadores ?? [])
        {
            _codigosCodificador.Add(c);
            textosCod.Add(c);      // son nombres de ffmpeg: no se traducen
        }
        foreach (var t in textosCod) cboCodificador.Items.Add(t);
        var posCod = _codigosCodificador.FindIndex(c => string.Equals(c, current.Codificador,
                                                                     StringComparison.OrdinalIgnoreCase));
        cboCodificador.SelectedIndex = posCod >= 0 ? posCod : 0;

        foreach (var t in textosAcel) cboAcelVideo.Items.Add(t);
        var posAcel = _codigosAcel.FindIndex(c => string.Equals(c, current.AceleracionVideo,
                                                               StringComparison.OrdinalIgnoreCase));
        cboAcelVideo.SelectedIndex = posAcel >= 0 ? posAcel : 0;   // lo que aqui no valga, «Automatica»

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

        // Películas. Copia igual que el modelo, por el mismo motivo: si se
        // cancela la ventana, nada de esto se aplica.
        _tmdb = current.Tmdb.Clone();
        chkTmdb.IsChecked = _tmdb.Activo;
        PintarClaveTmdb();

        btnTmdbOlvidar.Click += (_, _) =>
        {
            _tmdb.PonerClave(null);
            txtTmdbClave.Clear();
            PintarClaveTmdb(Textos.Instancia.TmdbClaveOlvidada);
        };
        // Se repinta al escribir porque la línea de «cuál se está usando» cambia
        // en cuanto hay algo en el campo: enterarse al guardar es enterarse tarde.
        txtTmdbClave.PasswordChanged += (_, _) => PintarClaveTmdb();
        btnTmdbAbrir.Click += (_, _) => AbrirPaginaDeTmdb();
    }

    /// <summary>La página donde TMDb reparte las claves, dentro de la cuenta.</summary>
    private const string UrlApiDeTmdb = "https://www.themoviedb.org/settings/api";

    /// <summary>
    /// Abre esa página en el navegador. Si no se puede abrir, se enseña la
    /// dirección para copiarla a mano: dejar el botón sin hacer nada visible
    /// convierte «no tengo navegador por defecto» en «esto está roto».
    /// </summary>
    private void AbrirPaginaDeTmdb()
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(UrlApiDeTmdb) { UseShellExecute = true });
        }
        catch
        {
            lblTmdbClaveNota.Text = string.Format(Textos.Instancia.TmdbNoSeAbrio, UrlApiDeTmdb);
        }
    }

    // ── Identificar películas contra TMDb ──

    private readonly AjustesDeTmdb _tmdb;

    /// <summary>
    /// Qué clave hay y, sobre todo, <b>cuál se va a usar</b>. Las dos cosas se
    /// parecen y no son la misma: se puede no tener clave propia y funcionar
    /// —porque la trae la app— o no tener ninguna de las dos, que es lo que le
    /// pasa a quien compile el repo sin el secreto de la build.
    /// </summary>
    private void PintarClaveTmdb(string? aviso = null)
    {
        bool propia = _tmdb.TieneClave || txtTmdbClave.Password.Length > 0;

        btnTmdbOlvidar.Visibility = _tmdb.TieneClave ? Visibility.Visible : Visibility.Collapsed;
        lblTmdbClaveNota.Text = aviso
                                ?? (_tmdb.TieneClave ? Textos.Instancia.TmdbClaveGuardada
                                                     : Textos.Instancia.TmdbClaveAyuda);

        bool falta = !propia && ClaveDeTmdb.Empotrada is null;
        lblTmdbOrigen.Text = propia ? Textos.Instancia.TmdbUsandoLaTuya
                           : falta ? Textos.Instancia.TmdbSinNingunaClave
                                   : Textos.Instancia.TmdbUsandoLaDeLaApp;

        lblTmdbOrigen.Foreground = (Brush)FindResource(falta ? "OrgWarn" : "Neutral500");
        cajaTmdbClave.Background = (Brush)FindResource(falta ? "OrgWarnBg" : "Field");
        cajaTmdbClave.BorderBrush = (Brush)FindResource(falta ? "OrgWarnBorder" : "Divider");
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
        s.Codificador = cboCodificador.SelectedIndex >= 0
            ? _codigosCodificador[cboCodificador.SelectedIndex]
            : "";
        s.AceleracionVideo = cboAcelVideo.SelectedIndex >= 0
            ? _codigosAcel[cboAcelVideo.SelectedIndex]
            : Ondine.Objetivo.AceleracionDeVideo.Auto;

        _ia.Activo = chkIa.IsChecked == true;
        _ia.BaseUrl = txtIaUrl.Text.Trim();
        _ia.Modelo = txtIaModelo.Text.Trim();
        // Vacío = «no la cambies». Lo contrario haría que abrir preferencias para
        // tocar cualquier otra cosa borrase la clave de paso.
        if (txtIaClave.Password.Length > 0) _ia.PonerClave(txtIaClave.Password);
        s.Ia = _ia;

        _tmdb.Activo = chkTmdb.IsChecked == true;
        // Vacío = «no la cambies», igual que con la del modelo: entrar en
        // preferencias a tocar otra cosa no puede borrar la clave de paso.
        if (txtTmdbClave.Password.Length > 0) _tmdb.PonerClave(txtTmdbClave.Password);
        s.Tmdb = _tmdb;

        // Se aplica aquí y no al cerrar: los textos están enlazados, así que la
        // app entera cambia de idioma sola en cuanto se toca esto.
        Idioma.Actual = Idioma.Resolver(s.Idioma);

        Result = s;
        DialogResult = true;
    }
}
