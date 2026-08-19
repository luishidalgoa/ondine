using System.IO;
using System.Windows;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Reindex;
using Ondine.Rutas;

namespace Ondine;

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
        _ => Textos.Instancia.ReordenarPorqueOcupado,
    };

    public Brush Color => (Brush)Application.Current.FindResource(Paso.Motivo switch
    {
        PlanDeReordenado.Porque.Va => "OrgOk",
        PlanDeReordenado.Porque.YaEsta => "Neutral500",
        PlanDeReordenado.Porque.Ocupado => "OrgDanger",
        _ => "OrgWarn",
    });
}

/// <summary>
/// Mover cada capítulo a la carpeta de su temporada.
///
/// <para>
/// Se enseña la simulación entera ANTES de tocar nada —qué se movería y, sobre
/// todo, qué no y por qué—. Un reordenado que empieza a mover en cuanto lo
/// pulsas es un reordenado que no se puede cancelar a tiempo, y aquí se está
/// hablando de la biblioteca de alguien.
/// </para>
/// </summary>
public partial class ReordenarWindow : Window
{
    private readonly IReadOnlyList<ReindexResolution> _resoluciones;
    private readonly string _raiz;
    private readonly Settings _ajustes;
    private List<PlanDeReordenado.Paso> _plan = new();
    private Mudanza.Parte? _hecho;

    // Los tres valores del desplegable, en el mismo orden en que se añaden.
    private static readonly string?[] Idiomas = [null, "en", "es"];

    public ReordenarWindow(IReadOnlyList<ReindexResolution> resoluciones, string raiz, Settings ajustes)
    {
        InitializeComponent();
        _resoluciones = resoluciones;
        _raiz = raiz;
        _ajustes = ajustes;

        btnCerrar.Click += (_, _) => Close();
        chkSoloLosQueVan.Checked += (_, _) => Pintar();
        chkSoloLosQueVan.Unchecked += (_, _) => Pintar();
        btnMover.Click += (_, _) => Mover();
        btnDeshacer.Click += (_, _) => Deshacer();

        cboIdioma.Items.Add(Textos.Instancia.ReordenarIdiomaApp);
        cboIdioma.Items.Add($"{Idioma.Nombre("en")} — {CarpetaDeTemporada.Nombre(3, false)}");
        cboIdioma.Items.Add($"{Idioma.Nombre("es")} — {CarpetaDeTemporada.Nombre(3, true)}");
        cboIdioma.SelectedIndex = Math.Max(0, Array.IndexOf(Idiomas, Vacio(_ajustes.CarpetaTemporadaIdioma)));
        cboIdioma.SelectionChanged += (_, _) => { Guardar(); Recalcular(); };

        Recalcular();
    }

    private static string? Vacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>En castellano o en inglés, resolviendo el «como la app».</summary>
    private bool EnCastellano
    {
        get
        {
            var elegido = Idiomas[Math.Clamp(cboIdioma.SelectedIndex, 0, Idiomas.Length - 1)];
            return (elegido ?? Idioma.Actual) == "es";
        }
    }

    private void Guardar()
    {
        _ajustes.CarpetaTemporadaIdioma = Idiomas[Math.Clamp(cboIdioma.SelectedIndex, 0, Idiomas.Length - 1)] ?? "";
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

        lblResumen.Text = (van == 0
                              ? Textos.Instancia.ReordenarResumenNinguno
                              : string.Format(Textos.Instancia.ReordenarResumen, van))
                          + (quietos > 0 && van > 0
                              ? string.Format(Textos.Instancia.ReordenarResumenQuietos, quietos) : "");

        PintarRiesgos();

        var visibles = chkSoloLosQueVan.IsChecked == true
            ? _plan.Where(p => p.Motivo == PlanDeReordenado.Porque.Va)
            : _plan;
        lista.ItemsSource = visibles.Select(p => new ReordenVista { Paso = p, Raiz = _raiz }).ToList();

        btnMover.IsEnabled = van > 0 && _hecho == null;
        btnMover.Content = van > 0
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
            cajaRiesgo.Visibility = Visibility.Collapsed;
            return;
        }

        lblRiesgo.Text = string.Join("\n", avisos.Select(a => a.Que switch
        {
            RiesgoDelReordenado.Riesgo.CruzaVolumen =>
                string.Format(Textos.Instancia.ReordenarRiesgoVolumen, a.Cuantos),
            RiesgoDelReordenado.Riesgo.Nube =>
                string.Format(Textos.Instancia.ReordenarRiesgoNube, a.Cuantos, a.Detalle),
            _ => string.Format(Textos.Instancia.ReordenarRiesgoMarcador, a.Cuantos),
        }));
        cajaRiesgo.Visibility = Visibility.Visible;
    }

    private void Mover()
    {
        _hecho = MudanzaDeTemporada.Aplicar(_plan);

        lblPie.Text = _hecho.Fallidos.Count == 0
            ? string.Format(Textos.Instancia.ReordenarHecho, _hecho.Movidos.Count)
            : string.Format(Textos.Instancia.ReordenarHechoConFallos,
                            _hecho.Movidos.Count, _hecho.Fallidos.Count);

        // Deshacer solo aparece si de verdad hay algo que deshacer: un botón que
        // no hace nada al pulsarlo enseña a desconfiar del resto.
        btnDeshacer.Visibility = _hecho.Movidos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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
        lblPie.Text = string.Format(Textos.Instancia.ReordenarDeshecho, vueltos);
        btnDeshacer.Visibility = Visibility.Collapsed;

        // Si alguno no pudo volver, la tabla de quien nos abrió sigue estando mal:
        // se le sigue diciendo que hubo movimiento.
        MovioAlgo = vueltos < eran;
        _hecho = null;
        Recalcular();
    }

    /// <summary>
    /// Si se llegó a mover algo. Quien abrió la ventana necesita saberlo para
    /// volver a analizar: las rutas de su tabla han dejado de existir.
    /// </summary>
    public bool MovioAlgo { get; private set; }
}
