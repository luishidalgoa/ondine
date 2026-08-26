using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ondine.Ia;
using Ondine.Localizacion;
using Ondine.Peliculas;

namespace Ondine.Ava;

/// <summary>
/// Preferencias, portado de <c>PreferencesWindow</c>.
///
/// <para>
/// <b>Las dos claves ya no van en un <c>PasswordBox</c>, porque Avalonia no lo tiene.</b> Van
/// en un <c>TextBox</c> con <c>PasswordChar</c>, que resuelve lo de siempre —no se ve en
/// pantalla, ni por encima del hombro ni en una captura— pero <b>no es lo mismo por dentro</b>:
/// el <c>PasswordBox</c> de WPF guardaba el valor fuera de una cadena normal, y aquí vive en
/// el <c>Text</c> del control como cualquier otro texto. En reposo no cambia nada —lo que se
/// guarda sigue cifrado, nunca en claro—, y quien pueda leer la memoria del proceso ya tenía
/// bastante; pero es una diferencia real y se escribe aquí en vez de dejar que parezca un
/// cambio de nombre.
/// </para>
/// <para>
/// <b>Y falta una casilla a propósito: la del menú del Explorador.</b> Se aplica escribiendo
/// en el registro de Windows, y <c>ShellIntegration</c> ni siquiera es visible desde aquí
/// —vive dentro del proyecto de WPF—. Enseñar una casilla que no hace nada es peor que no
/// enseñarla: parece que la app está rota en vez de que esa función no existe todavía en
/// este sistema. El equivalente en Linux y macOS es otra cosa (asociaciones de tipo,
/// <c>.desktop</c>), y eso es justo lo que hay en la Fase 5.
/// </para>
/// </summary>
public partial class Preferencias : Window
{
    /// <summary>
    /// El «ninguno» del desplegable de presets, que además hace de centinela: si es lo
    /// elegido, no hay preset por defecto.
    ///
    /// <para>
    /// Se guarda en un campo en vez de pedírselo a <see cref="Textos"/> cada vez porque así
    /// el texto que se mete en la lista y el que se compara al guardar son el MISMO.
    /// Pidiéndolo dos veces, cambiar de idioma con la ventana abierta dejaría un elemento en
    /// el idioma viejo que ya no casaría con nada, y «ninguno» pasaría por ser el nombre de
    /// un preset.
    /// </para>
    /// </summary>
    private readonly string _sinPreset = Textos.Instancia.PreferenciasSinPreset;

    /// <summary>
    /// Los códigos del desplegable de idioma, en el mismo orden que sus entradas. El
    /// primero es <c>""</c>: «el del sistema».
    /// </summary>
    private readonly List<string> _codigosIdioma = [];

    /// <summary>Los ajustes con los que se abrió, para no perder lo que aquí no se edita.</summary>
    private readonly Settings _previos = null!;

    /// <summary>
    /// Los codigos de la aceleracion, en el MISMO orden que los textos del desplegable: el
    /// guardado indexa esta lista con el SelectedIndex, igual que el de idioma. El orden es lo
    /// unico que los ata.
    /// </summary>
    private readonly List<string> _codigosAcel = [];

    /// <summary>Igual que los de la aceleracion: en el MISMO orden que los textos.</summary>
    private readonly List<string> _codigosCodificador = [];

    private static Textos T => Textos.Instancia;

    private readonly AjustesDeModelo _ia = null!;
    private readonly AjustesDeTmdb _tmdb = null!;

    /// <summary>Ajustes resultantes tras pulsar Guardar (null si se cancela).</summary>
    public Settings? Result { get; private set; }

    public Preferencias() => AvaloniaXamlLoader.Load(this);

    /// <param name="aceleraciones">
    /// Las aceleraciones de decodificacion que FUNCIONAN en esta maquina, sondeadas por el
    /// motor. Se pasan de fuera y no se sondean aqui por dos razones: la sonda arranca ffmpeg
    /// una vez por candidata -casi dos segundos- y una ventana no debe hacer eso mientras se
    /// abre; y asi esta ventana sigue siendo pura, que es lo que la deja probarse.
    /// </param>
    public Preferencias(Settings current, IEnumerable<string> presetNames,
                        IEnumerable<string>? aceleraciones = null,
                        IEnumerable<string>? codificadores = null) : this()
    {
        _previos = current;

        this.FindControl<Grid>("header")!.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };
        // Close(false) y NO Close(null): quien abre esta ventana pide ShowDialog<bool>, y un
        // nulo no se convierte en bool. Reventaba igual que devolver el objeto de ajustes, y
        // en el mismo sitio -dentro del cierre de la ventana, donde no lo recoge nadie-. O
        // sea: cerrar con la X o con Cancelar tambien se llevaba la aplicacion por delante.
        Btn("btnX").Click += (_, _) => Close(false);
        Btn("btnCancel").Click += (_, _) => Close(false);
        Btn("btnSave").Click += (_, _) => Guardar();

        // ── General ──
        // «El del sistema» va con código vacío, que es justo lo que significa el ajuste
        // vacío: no he elegido, decide tú.
        var idiomas = new List<string> { Textos.Instancia.PreferenciasIdiomaAppSistema };
        _codigosIdioma.Add("");
        foreach (var c in Idioma.Disponibles)
        {
            _codigosIdioma.Add(c);
            // Cada idioma escrito EN ese idioma: quien busca «Español» lo reconoce aunque
            // la app esté ahora mismo en inglés.
            idiomas.Add(Idioma.Nombre(c));
        }
        var cboIdioma = Cbo("cboIdioma");
        cboIdioma.ItemsSource = idiomas;
        var pos = _codigosIdioma.IndexOf(current.Idioma);
        cboIdioma.SelectedIndex = pos >= 0 ? pos : 0;

        var presets = new List<string> { _sinPreset };
        presets.AddRange(presetNames);
        var cboPreset = Cbo("cboDefPreset");
        cboPreset.ItemsSource = presets;
        // El ajuste guarda el NOMBRE del preset, que en los de fábrica está traducido: si se
        // eligió con la app en el otro idioma, hay que pasarlo por NombreVigente o el preset
        // por defecto parecería haber desaparecido.
        var guardado = PresetStore.NombreVigente(current.DefaultPreset);
        cboPreset.SelectedItem = presets.Contains(guardado) ? guardado : _sinPreset;

        Txt("txtDefLang").Text = current.DefaultLang;
        Chk("chkRecurse").IsChecked = current.Recurse;
        Chk("chkUpdates").IsChecked = current.CheckUpdatesOnStart;

        // ── Al comprimir ──
        Rb("rbAsk").IsChecked = current.AfterCompress == AfterCompress.Ask;
        Rb("rbRecycle").IsChecked = current.AfterCompress == AfterCompress.RecycleOriginal;
        Rb("rbKeep").IsChecked = current.AfterCompress == AfterCompress.Keep;

        // ── Rendimiento y disco ──
        Txt("txtMinFree").Text = current.MinFreeMb.ToString();
        Chk("chkHw").IsChecked = current.UseHardware;

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

        // El codificador, con lo que de verdad arranca en esta maquina.
        _codigosCodificador.Add("");
        var textosCod = new List<string> { T.PreferenciasCodificadorAuto };
        _codigosCodificador.Add(Engine.PorSoftware);
        textosCod.Add(T.PreferenciasCodificadorSoftware);
        foreach (var c in codificadores ?? [])
        {
            _codigosCodificador.Add(c);
            textosCod.Add(c);
        }
        var cboCod = Cbo("cboCodificador");
        cboCod.ItemsSource = textosCod;
        var posCod = _codigosCodificador.FindIndex(c => string.Equals(c, current.Codificador,
                                                                     StringComparison.OrdinalIgnoreCase));
        cboCod.SelectedIndex = posCod >= 0 ? posCod : 0;

        var cboAcelVideo = Cbo("cboAcelVideo");
        cboAcelVideo.ItemsSource = textosAcel;     // ItemsSource, no Items: con el puesto, tocar Items revienta
        var posAcel = _codigosAcel.FindIndex(c => string.Equals(c, current.AceleracionVideo,
                                                               StringComparison.OrdinalIgnoreCase));
        cboAcelVideo.SelectedIndex = posAcel >= 0 ? posAcel : 0;

        // ── Modelo. Se trabaja sobre una COPIA: probar la conexión necesita la clave ya
        //    guardada, y si se cancela la ventana nada de esto se aplica.
        _ia = current.Ia.Clone();
        Chk("chkIa").IsChecked = _ia.Activo;
        Txt("txtIaUrl").Text = _ia.BaseUrl;
        Txt("txtIaModelo").Text = _ia.Modelo;
        PintarClave();

        Btn("btnIaOlvidar").Click += (_, _) =>
        {
            _ia.PonerClave(null);
            Txt("txtIaClave").Text = "";
            Lbl("lblIaProbar").Text = Textos.Instancia.IaClaveOlvidada;
            PintarClave();
        };
        Btn("btnIaProbar").Click += async (_, _) => await ProbarIa();

        // ── Películas. Copia igual que el modelo, por el mismo motivo.
        _tmdb = current.Tmdb.Clone();
        Chk("chkTmdb").IsChecked = _tmdb.Activo;
        PintarClaveTmdb();

        Btn("btnTmdbOlvidar").Click += (_, _) =>
        {
            _tmdb.PonerClave(null);
            Txt("txtTmdbClave").Text = "";
            PintarClaveTmdb(Textos.Instancia.TmdbClaveOlvidada);
        };
        // Se repinta al escribir porque la línea de «cuál se está usando» cambia en cuanto
        // hay algo en el campo: enterarse al guardar es enterarse tarde.
        Txt("txtTmdbClave").TextChanged += (_, _) => PintarClaveTmdb();
        Btn("btnTmdbAbrir").Click += (_, _) => AbrirPaginaDeTmdb();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private TextBox Txt(string n) => this.FindControl<TextBox>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private RadioButton Rb(string n) => this.FindControl<RadioButton>(n)!;
    private ComboBox Cbo(string n) => this.FindControl<ComboBox>(n)!;

    /// <summary>La página donde TMDb reparte las claves, dentro de la cuenta.</summary>
    private const string UrlApiDeTmdb = "https://www.themoviedb.org/settings/api";

    /// <summary>
    /// Abre esa página en el navegador. Si no se puede abrir, se enseña la dirección para
    /// copiarla a mano: dejar el botón sin hacer nada visible convierte «no tengo navegador
    /// por defecto» en «esto está roto».
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
            Lbl("lblTmdbClaveNota").Text = string.Format(Textos.Instancia.TmdbNoSeAbrio, UrlApiDeTmdb);
        }
    }

    /// <summary>
    /// Qué clave hay y, sobre todo, <b>cuál se va a usar</b>. Las dos cosas se parecen y no
    /// son la misma: se puede no tener clave propia y funcionar —porque la trae la app— o no
    /// tener ninguna de las dos, que es lo que le pasa a quien compile el repo sin el
    /// secreto de la build.
    /// </summary>
    private void PintarClaveTmdb(string? aviso = null)
    {
        bool propia = _tmdb.TieneClave || (Txt("txtTmdbClave").Text?.Length ?? 0) > 0;

        Btn("btnTmdbOlvidar").IsVisible = _tmdb.TieneClave;
        Lbl("lblTmdbClaveNota").Text = aviso
                                       ?? (_tmdb.TieneClave ? Textos.Instancia.TmdbClaveGuardada
                                                            : Textos.Instancia.TmdbClaveAyuda);

        bool falta = !propia && ClaveDeTmdb.Empotrada is null;
        Lbl("lblTmdbOrigen").Text = propia ? Textos.Instancia.TmdbUsandoLaTuya
                                  : falta ? Textos.Instancia.TmdbSinNingunaClave
                                          : Textos.Instancia.TmdbUsandoLaDeLaApp;

        Lbl("lblTmdbOrigen").Foreground = Pincel(falta ? "OrgWarn" : "Neutral500");
        var caja = this.FindControl<Border>("cajaTmdbClave")!;
        caja.Background = Pincel(falta ? "OrgWarnBg" : "Field");
        caja.BorderBrush = Pincel(falta ? "OrgWarnBorder" : "Divider");
    }

    /// <summary>
    /// La clave guardada NO se rellena en el campo: no se saca a pantalla algo que ya está
    /// a salvo. Se dice que la hay, y escribir encima la reemplaza.
    /// </summary>
    private void PintarClave()
    {
        bool hay = _ia.TieneClave;
        Btn("btnIaOlvidar").IsVisible = hay;
        Lbl("lblIaClaveNota").Text = hay ? Textos.Instancia.IaClaveGuardada
                                         : Textos.Instancia.IaClaveAyuda;
    }

    /// <summary>
    /// Prueba con lo que hay AHORA en los campos, no con lo guardado: probar y que te
    /// conteste algo que no es lo que estás escribiendo es peor que no tener el botón.
    /// </summary>
    private async Task ProbarIa()
    {
        var prueba = _ia.Clone();
        prueba.Activo = true;
        prueba.BaseUrl = (Txt("txtIaUrl").Text ?? "").Trim();
        prueba.Modelo = (Txt("txtIaModelo").Text ?? "").Trim();
        var clave = Txt("txtIaClave").Text ?? "";
        if (clave.Length > 0) prueba.PonerClave(clave);

        Btn("btnIaProbar").IsEnabled = false;
        Lbl("lblIaProbar").Text = Textos.Instancia.IaProbando;
        try
        {
            var r = await ModeloConectado.ProbarAsync(prueba);
            Lbl("lblIaProbar").Text = r.Texto != null
                ? Textos.Instancia.IaProbadoBien
                : string.Format(Textos.Instancia.IaProbadoMal, r.Error);
        }
        finally { Btn("btnIaProbar").IsEnabled = true; }
    }

    private void Guardar()
    {
        // Se PARTE de los ajustes de entrada y solo se pisa lo que este diálogo edita.
        // Construyendo un Settings nuevo se perdía en cada guardado todo lo que no sale en
        // esta ventana: el historial de renombrado y el factor de complejidad que la app
        // aprende midiendo.
        var s = _previos.Clone();

        var cboIdioma = Cbo("cboIdioma");
        s.Idioma = cboIdioma.SelectedIndex >= 0 ? _codigosIdioma[cboIdioma.SelectedIndex] : "";
        s.DefaultPreset = Cbo("cboDefPreset").SelectedItem is string p && p != _sinPreset ? p : "";

        var lang = (Txt("txtDefLang").Text ?? "").Trim();
        s.DefaultLang = string.IsNullOrWhiteSpace(lang) ? "spa" : lang;
        s.Recurse = Chk("chkRecurse").IsChecked == true;
        s.CheckUpdatesOnStart = Chk("chkUpdates").IsChecked == true;
        s.AfterCompress = Rb("rbRecycle").IsChecked == true ? AfterCompress.RecycleOriginal
                        : Rb("rbKeep").IsChecked == true ? AfterCompress.Keep
                        : AfterCompress.Ask;
        s.MinFreeMb = int.TryParse((Txt("txtMinFree").Text ?? "").Trim(), out var mb)
            ? Math.Clamp(mb, 50, 100_000) : 200;
        s.UseHardware = Chk("chkHw").IsChecked == true;
        var cboCodificador = Cbo("cboCodificador");
        s.Codificador = cboCodificador.SelectedIndex >= 0
            ? _codigosCodificador[cboCodificador.SelectedIndex]
            : "";

        var cboAcel = Cbo("cboAcelVideo");
        s.AceleracionVideo = cboAcel.SelectedIndex >= 0
            ? _codigosAcel[cboAcel.SelectedIndex]
            : Ondine.Objetivo.AceleracionDeVideo.Auto;

        _ia.Activo = Chk("chkIa").IsChecked == true;
        _ia.BaseUrl = (Txt("txtIaUrl").Text ?? "").Trim();
        _ia.Modelo = (Txt("txtIaModelo").Text ?? "").Trim();
        // Vacío = «no la cambies». Lo contrario haría que abrir preferencias para tocar
        // cualquier otra cosa borrase la clave de paso.
        var claveIa = Txt("txtIaClave").Text ?? "";
        if (claveIa.Length > 0) _ia.PonerClave(claveIa);
        s.Ia = _ia;

        _tmdb.Activo = Chk("chkTmdb").IsChecked == true;
        var claveTmdb = Txt("txtTmdbClave").Text ?? "";
        if (claveTmdb.Length > 0) _tmdb.PonerClave(claveTmdb);
        s.Tmdb = _tmdb;

        // Se aplica aquí y no al cerrar: los textos están enlazados, así que la app entera
        // cambia de idioma sola en cuanto se toca esto.
        Idioma.Actual = Idioma.Resolver(s.Idioma);

        Result = s;

        // Close(true) y NO Close(Result): quien abre esta ventana espera un bool
        // -ShowDialog<bool>- y lee el resultado de la propiedad Result. Cerrar con el objeto
        // hacía que Avalonia intentara convertir un Settings en bool al devolverlo, y eso
        // revienta DENTRO de Close: la excepción sale por el await de quien esperaba, así que
        // guardar no guardaba y el aviso que salía hablaba de una conversión, no de ajustes.
        Close(true);
    }

    private IBrush Pincel(string clave) =>
        this.TryFindResource(clave, out var v) && v is IBrush b ? b : Brushes.Gray;
}
