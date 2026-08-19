using System.IO;
using System.Windows;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Reindex;
using Ondine.Rutas;

namespace Ondine;

/// <summary>Una línea del plan de películas, ya lista para pintar.</summary>
public sealed class PeliculaVista
{
    public required PlanDePeliculas.Paso Paso { get; init; }
    public required string Raiz { get; init; }

    public string Nombre => Path.GetFileName(Paso.Origen);

    /// <summary>
    /// De dónde a dónde, relativo a la raíz de la biblioteca. En películas lo que
    /// cambia suele ser también el NOMBRE del fichero, no solo la carpeta, así
    /// que se enseña la ruta completa relativa y no solo el directorio.
    /// </summary>
    public string Adonde
    {
        get
        {
            var de = Relativa(Paso.Origen);
            if (Paso.Destino is not { } d) return de;
            return $"{de}  →  {Relativa(d)}";
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
        PlanDePeliculas.Porque.Va => Textos.Instancia.PeliculasPorqueVa,
        PlanDePeliculas.Porque.EnColeccion => Textos.Instancia.PeliculasPorqueEnColeccion,
        PlanDePeliculas.Porque.YaEsta => Textos.Instancia.PeliculasPorqueYaEsta,
        PlanDePeliculas.Porque.SinTitulo => Textos.Instancia.PeliculasPorqueSinTitulo,
        _ => Textos.Instancia.PeliculasPorqueOcupado,
    };

    public Brush Color => (Brush)Application.Current.FindResource(Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Va => "OrgOk",
        PlanDePeliculas.Porque.EnColeccion => "OrgOk",
        PlanDePeliculas.Porque.YaEsta => "Neutral500",
        PlanDePeliculas.Porque.Ocupado => "OrgDanger",
        _ => "OrgWarn",
    });
}

/// <summary>
/// Poner una carpeta de películas como Plex y Jellyfin esperan encontrarla.
///
/// <para>
/// Mismo trato que el reordenado por temporadas, y por la misma razón: se enseña
/// la simulación entera ANTES de tocar nada —qué se movería y, sobre todo, qué
/// no y por qué— y solo después se aplica. Aquí se está hablando de la
/// biblioteca de alguien.
/// </para>
/// <para>
/// Lo que esta ventana NO hace: identificar la película contra ninguna base de
/// datos. Todo lo que sabe sale del nombre del fichero y de su carpeta, así que
/// no puede arreglar un título mal escrito ni distinguir dos películas que se
/// llamen igual. Eso llega con el proveedor de metadatos.
/// </para>
/// </summary>
public partial class PeliculasWindow : Window
{
    private readonly IReadOnlyList<string> _ficheros;
    private readonly string _raiz;
    private List<PlanDePeliculas.Paso> _plan = new();
    private Mudanza.Parte? _hecho;

    public PeliculasWindow(IReadOnlyList<string> ficheros, string raiz)
    {
        InitializeComponent();
        _ficheros = ficheros;
        _raiz = raiz;

        btnCerrar.Click += (_, _) => Close();
        chkSoloLosQueVan.Checked += (_, _) => Pintar();
        chkSoloLosQueVan.Unchecked += (_, _) => Pintar();
        btnMover.Click += (_, _) => Mover();
        btnDeshacer.Click += (_, _) => Deshacer();

        Recalcular();
    }

    private void Recalcular()
    {
        _plan = PlanDePeliculas.Montar(_ficheros, _raiz);
        Pintar();
    }

    private void Pintar()
    {
        int van = PlanDePeliculas.Cuantos(_plan);
        int quietos = _plan.Count - van;

        lblResumen.Text = (van == 0
                              ? Textos.Instancia.PeliculasResumenNinguno
                              : string.Format(Textos.Instancia.PeliculasResumen, van))
                          + (quietos > 0 && van > 0
                              ? string.Format(Textos.Instancia.PeliculasResumenQuietos, quietos) : "");

        PintarRiesgos();

        var visibles = chkSoloLosQueVan.IsChecked == true
            ? _plan.Where(p => p.Motivo is PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion)
            : _plan;
        lista.ItemsSource = visibles.Select(p => new PeliculaVista { Paso = p, Raiz = _raiz }).ToList();

        btnMover.IsEnabled = van > 0 && _hecho == null;
        btnMover.Content = van > 0
            ? string.Format(Textos.Instancia.PeliculasBoton, van)
            : Textos.Instancia.PeliculasBotonNada;
    }

    /// <summary>
    /// Lo que va a costar, con el mismo criterio que el reordenado de temporadas:
    /// cruzar de disco, entrar en una nube donde no estaba, y los ficheros que
    /// solo están en la nube. Se reaprovecha el cálculo traduciendo los pasos,
    /// porque el riesgo de mover un fichero no depende de por qué se mueve.
    /// </summary>
    private void PintarRiesgos()
    {
        var comoReordenado = _plan
            .Where(p => p.Motivo is PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion
                        && p.Destino is not null)
            .Select(p => new PlanDeReordenado.Paso(p.Origen, p.Destino, PlanDeReordenado.Porque.Va));

        var avisos = RiesgoDelReordenado.Mirar(comoReordenado, NubesDelEquipo.Registradas());

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
        var pares = _plan
            .Where(p => p.Motivo is PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion
                        && p.Destino is not null)
            .Select(p => (p.Origen, p.Destino!));

        _hecho = Mudanza.Aplicar(pares);

        lblPie.Text = _hecho.Fallidos.Count == 0
            ? string.Format(Textos.Instancia.PeliculasHecho, _hecho.Movidos.Count)
            : string.Format(Textos.Instancia.PeliculasHechoConFallos,
                            _hecho.Movidos.Count, _hecho.Fallidos.Count);

        btnDeshacer.Visibility = _hecho.Movidos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        MovioAlgo = _hecho.Movidos.Count > 0;

        // Se marca a mano en vez de recalcular: las rutas del plan son las de
        // ANTES, y volver a montarlo diría otra vez «se mueve» sobre ficheros que
        // ya no están ahí. Lo que se sabe de verdad es lo que devolvió la mudanza.
        var movidos = _hecho.Movidos.Select(m => m.Origen).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _plan = _plan.Select(p => movidos.Contains(p.Origen)
            ? p with { Motivo = PlanDePeliculas.Porque.YaEsta }
            : p).ToList();
        Pintar();
    }

    private void Deshacer()
    {
        if (_hecho is null) return;

        int eran = _hecho.Movidos.Count;
        int vueltos = Mudanza.Deshacer(_hecho);
        lblPie.Text = string.Format(Textos.Instancia.PeliculasDeshecho, vueltos);
        btnDeshacer.Visibility = Visibility.Collapsed;

        // Si alguno no pudo volver, la tabla de quien nos abrió sigue estando mal.
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
