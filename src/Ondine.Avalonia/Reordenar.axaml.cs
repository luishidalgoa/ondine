using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ondine.Localizacion;
using Ondine.Reindex;
using Ondine.Rutas;

namespace Ondine.Ava;

/// <summary>Una línea del plan, ya lista para pintar.</summary>
public sealed class ReordenVista
{
    public required PlanDeReordenado.Paso Paso { get; init; }
    public required string Raiz { get; init; }

    public string Nombre => Path.GetFileName(Paso.Origen);

    /// <summary>
    /// De dónde a dónde, en corto. Se enseña relativo a la raíz de la serie: la
    /// ruta absoluta repetida cien veces es una columna de ruido con la única
    /// parte que cambia escondida al final.
    /// </summary>
    public string Adonde
    {
        get
        {
            var de = Relativa(Path.GetDirectoryName(Paso.Origen));
            if (Paso.Destino is not { } d) return de;
            return $"{de}  →  {Relativa(Path.GetDirectoryName(d))}";
        }
    }

    private string Relativa(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return "";
        try
        {
            var rel = Path.GetRelativePath(Raiz, ruta);
            return rel == "." ? "/" : rel;
        }
        catch { return ruta; }
    }

    public string Etiqueta => Paso.Motivo switch
    {
        PlanDeReordenado.Porque.Va => Textos.Instancia.ReordenarPorqueVa,
        PlanDeReordenado.Porque.YaEsta => Textos.Instancia.ReordenarPorqueYaEsta,
        PlanDeReordenado.Porque.SinCurar => Textos.Instancia.ReordenarPorqueSinCurar,
        PlanDeReordenado.Porque.SinTemporada => Textos.Instancia.ReordenarPorqueSinTemporada,
        PlanDeReordenado.Porque.YaNoEsta => Textos.Instancia.ReordenarPorqueYaNoEsta,
        _ => Textos.Instancia.ReordenarPorqueOcupado,
    };

    /// <summary>
    /// El color de la etiqueta, sacado del tema por nombre igual que en WPF.
    ///
    /// <para>
    /// Cambia la forma de pedirlo: <c>FindResource</c> lanzaba si no estaba, y aquí se
    /// pregunta con <c>TryFindResource</c>. Si un color no estuviera, la etiqueta sale
    /// gris en vez de tumbar la ventana — que es lo que quieres cuando lo único que se
    /// pierde es el color de una insignia.
    /// </para>
    /// </summary>
    public IBrush Color
    {
        get
        {
            var clave = Paso.Motivo switch
            {
                PlanDeReordenado.Porque.Va => "OrgOk",
                PlanDeReordenado.Porque.YaEsta => "Neutral500",
                PlanDeReordenado.Porque.Ocupado => "OrgDanger",
                PlanDeReordenado.Porque.YaNoEsta => "OrgDanger",
                _ => "OrgWarn",
            };
            var app = Avalonia.Application.Current;
            return app is not null && app.TryFindResource(clave, out var v) && v is IBrush b
                ? b
                : Brushes.Gray;
        }
    }
}

/// <summary>
/// «Ordenar por temporadas», portado de <c>ReordenarWindow</c>.
///
/// <para>
/// Se enseña la simulación entera ANTES de tocar nada —qué se movería y, sobre todo, qué no
/// y por qué—. Un reordenado que empieza a mover en cuanto lo pulsas es un reordenado que no
/// se puede cancelar a tiempo, y aquí se está hablando de la biblioteca de alguien.
/// </para>
/// <para>
/// <b>Un hueco que se hereda del motor y conviene decir en voz alta:</b> el aviso de «esto
/// está en una nube» sale de <c>NubesDelEquipo.Registradas()</c>, que lee el registro de
/// Windows y devuelve la lista vacía en cualquier otro sistema. Portada la pantalla, en Linux
/// y macOS el aviso simplemente no aparece nunca. No se arregla aquí —es del motor, y el
/// equivalente (Nextcloud, Dropbox, iCloud) no se averigua igual—, pero tampoco se queda sin
/// escribir: un aviso que no salta se parece demasiado a un aviso que no hacía falta.
/// </para>
/// </summary>
public partial class Reordenar : Window
{
    private readonly IReadOnlyList<ReindexResolution> _resoluciones = [];
    private readonly string _raiz = "";
    private readonly Settings _ajustes = null!;
    private List<PlanDeReordenado.Paso> _plan = [];
    private Mudanza.Parte? _hecho;

    // Los tres valores del desplegable, en el mismo orden en que se añaden.
    private static readonly string?[] Idiomas = [null, "en", "es"];

    public Reordenar() => AvaloniaXamlLoader.Load(this);

    public Reordenar(IReadOnlyList<ReindexResolution> resoluciones, string raiz, Settings ajustes) : this()
    {
        _resoluciones = resoluciones;
        _raiz = raiz;
        _ajustes = ajustes;

        Btn("btnCerrar").Click += (_, _) => Close();
        Chk("chkSoloLosQueVan").IsCheckedChanged += (_, _) => Pintar();
        Btn("btnMover").Click += (_, _) => Mover();
        Btn("btnDeshacer").Click += (_, _) => Deshacer();

        // En WPF se hacía `cboIdioma.Items.Add(...)`. En Avalonia el desplegable se
        // alimenta por ItemsSource, así que la lista se monta entera y se asigna.
        var cbo = Cbo("cboIdioma");
        cbo.ItemsSource = new List<string>
        {
            Textos.Instancia.ReordenarIdiomaApp,
            $"{Idioma.Nombre("en")} — {CarpetaDeTemporada.Nombre(3, false)}",
            $"{Idioma.Nombre("es")} — {CarpetaDeTemporada.Nombre(3, true)}",
        };
        cbo.SelectedIndex = Math.Max(0, Array.IndexOf(Idiomas, Vacio(_ajustes.CarpetaTemporadaIdioma)));
        cbo.SelectionChanged += (_, _) => { Guardar(); Recalcular(); };

        Recalcular();
    }

    private TextBlock Lbl(string n) => this.FindControl<TextBlock>(n)!;
    private Button Btn(string n) => this.FindControl<Button>(n)!;
    private CheckBox Chk(string n) => this.FindControl<CheckBox>(n)!;
    private ComboBox Cbo(string n) => this.FindControl<ComboBox>(n)!;

    private static string? Vacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>En castellano o en inglés, resolviendo el «como la app».</summary>
    private bool EnCastellano
    {
        get
        {
            var elegido = Idiomas[Math.Clamp(Cbo("cboIdioma").SelectedIndex, 0, Idiomas.Length - 1)];
            return (elegido ?? Idioma.Actual) == "es";
        }
    }

    private void Guardar()
    {
        _ajustes.CarpetaTemporadaIdioma =
            Idiomas[Math.Clamp(Cbo("cboIdioma").SelectedIndex, 0, Idiomas.Length - 1)] ?? "";
        SettingsStore.Save(_ajustes);
    }

    private void Recalcular()
    {
        _plan = PlanDeReordenado.Montar(_resoluciones, _raiz, EnCastellano);
        Pintar();
    }

    private void Pintar()
    {
        int van = PlanDeReordenado.Cuantos(_plan);
        int quietos = _plan.Count - van;

        Lbl("lblResumen").Text = (van == 0
                                     ? Textos.Instancia.ReordenarResumenNinguno
                                     : string.Format(Textos.Instancia.ReordenarResumen, van))
                                 + (quietos > 0 && van > 0
                                     ? string.Format(Textos.Instancia.ReordenarResumenQuietos, quietos) : "");

        PintarRiesgos();

        var visibles = Chk("chkSoloLosQueVan").IsChecked == true
            ? _plan.Where(p => p.Motivo == PlanDeReordenado.Porque.Va)
            : _plan;
        this.FindControl<ListBox>("lista")!.ItemsSource =
            visibles.Select(p => new ReordenVista { Paso = p, Raiz = _raiz }).ToList();

        var mover = Btn("btnMover");
        mover.IsEnabled = van > 0 && _hecho == null;
        mover.Content = van > 0
            ? string.Format(Textos.Instancia.ReordenarBoton, van)
            : Textos.Instancia.ReordenarBotonNada;
    }

    /// <summary>
    /// Lo que va a costar el movimiento, dicho antes de pulsar. No bloquea nada:
    /// hay bibliotecas que viven en la nube a propósito y reordenarlas es
    /// legítimo. Lo que no es legítimo es que se entere por la barra de tareas.
    /// </summary>
    private void PintarRiesgos()
    {
        // Las raíces se preguntan al sistema aquí y no dentro del cálculo: leer
        // el registro es lo único que no se puede probar sin un Windows con
        // nubes instaladas, así que se queda fuera de la decisión.
        var avisos = RiesgoDelReordenado.Mirar(_plan, NubesDelEquipo.Registradas());

        if (avisos.Count == 0)
        {
            this.FindControl<Border>("cajaRiesgo")!.IsVisible = false;
            return;
        }

        Lbl("lblRiesgo").Text = string.Join("\n", avisos.Select(a => a.Que switch
        {
            RiesgoDelReordenado.Riesgo.CruzaVolumen =>
                string.Format(Textos.Instancia.ReordenarRiesgoVolumen, a.Cuantos),
            RiesgoDelReordenado.Riesgo.Nube =>
                string.Format(Textos.Instancia.ReordenarRiesgoNube, a.Cuantos, a.Detalle),
            _ => string.Format(Textos.Instancia.ReordenarRiesgoMarcador, a.Cuantos),
        }));
        this.FindControl<Border>("cajaRiesgo")!.IsVisible = true;
    }

    private void Mover()
    {
        _hecho = MudanzaDeTemporada.Aplicar(_plan);

        Lbl("lblPie").Text = _hecho.Fallidos.Count == 0
            ? string.Format(Textos.Instancia.ReordenarHecho, _hecho.Movidos.Count)
            : string.Format(Textos.Instancia.ReordenarHechoConFallos,
                            _hecho.Movidos.Count, _hecho.Fallidos.Count);

        // Un subtítulo que se quedó atrás no rompe nada visible, pero para el
        // servidor de medios ha dejado de existir. Callarlo era lo peor de todo.
        if (_hecho.CompanerosSinMover.Count > 0)
            Lbl("lblPie").Text += " " + string.Format(Textos.Instancia.CompanerosSinMover,
                                                      _hecho.CompanerosSinMover.Count);

        // Deshacer solo aparece si de verdad hay algo que deshacer: un botón que
        // no hace nada al pulsarlo enseña a desconfiar del resto.
        Btn("btnDeshacer").IsVisible = _hecho.Movidos.Count > 0;
        MovioAlgo = _hecho.Movidos.Count > 0;

        // El plan se marca a mano en vez de recalcularlo: las resoluciones que
        // entraron traen las rutas de ANTES, así que volver a montarlo diría otra
        // vez «se mueve» sobre ficheros que ya no están ahí. Lo que se sabe de
        // verdad es lo que devolvió la mudanza, y es lo que se pinta.
        var movidos = _hecho.Movidos.Select(m => m.Origen).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _plan = _plan.Select(p => movidos.Contains(p.Origen)
            ? p with { Motivo = PlanDeReordenado.Porque.YaEsta }
            : p).ToList();
        Pintar();
    }

    private void Deshacer()
    {
        if (_hecho is null) return;

        int eran = _hecho.Movidos.Count;
        int vueltos = MudanzaDeTemporada.Deshacer(_hecho);
        bool aMedias = vueltos < eran;

        Lbl("lblPie").Text = aMedias
            ? string.Format(Textos.Instancia.DeshacerAMedias, vueltos, eran - vueltos)
            : string.Format(Textos.Instancia.ReordenarDeshecho, vueltos);

        // Si alguno NO pudo volver —lo normal es que esté abierto en el
        // reproductor—, el registro se conserva y el botón se queda: es el único
        // sitio donde vive la lista de qué fue a dónde, y tirarlo dejaba ese
        // fichero desplazado para siempre sin forma de recuperarlo desde la app.
        // Reintentar es seguro: lo que ya volvió se salta solo.
        Btn("btnDeshacer").IsVisible = aMedias;

        // Si alguno no pudo volver, la tabla de quien nos abrió sigue estando mal:
        // se le sigue diciendo que hubo movimiento.
        MovioAlgo = aMedias;
        if (!aMedias) _hecho = null;
        Recalcular();
    }

    /// <summary>
    /// Si se llegó a mover algo. Quien abrió la ventana necesita saberlo para
    /// volver a analizar: las rutas de su tabla han dejado de existir.
    /// </summary>
    public bool MovioAlgo { get; private set; }
}
