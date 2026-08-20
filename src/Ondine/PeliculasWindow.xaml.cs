using System.IO;
using System.Windows;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Peliculas;
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
        PlanDePeliculas.Porque.EsExtra => Textos.Instancia.PeliculasPorqueEsExtra,
        _ => Textos.Instancia.PeliculasPorqueOcupado,
    };

    /// <summary>Lo que dijo TMDb de esta película, si se preguntó.</summary>
    public IdentificacionDePelicula.Veredicto? Veredicto { get; init; }

    /// <summary>
    /// Qué se encontró y por qué señal, en una línea. Vacío si no se ha
    /// preguntado —que es el estado de partida y no un fallo—.
    /// </summary>
    public string Segun
    {
        get
        {
            if (Veredicto is not { } v) return "";

            var senal = Senal(v.Senal);
            var propuesta = IdentificacionDePelicula.Propuesta(v);
            return propuesta is null
                ? senal
                : string.Format(Textos.Instancia.PeliculasSegunTmdb,
                                TituloDePelicula.Canonico(propuesta)) + " · " + senal;
        }
    }

    public Visibility VerSegun => Segun.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// En verde lo que se va a aplicar y en ámbar lo que no. Es la misma señal
    /// que el resto de la app: el color dice si hay que mirarlo.
    /// </summary>
    public Brush SegunColor => (Brush)Application.Current.FindResource(
        Veredicto?.SePuedeAplicar == true ? "OrgOk" : "OrgWarn");

    private static string Senal(IdentificacionDePelicula.Porque p) => p switch
    {
        IdentificacionDePelicula.Porque.AnioYTitulo => Textos.Instancia.PeliculasSenalAnioYTitulo,
        IdentificacionDePelicula.Porque.TituloOriginal => Textos.Instancia.PeliculasSenalTituloOriginal,
        IdentificacionDePelicula.Porque.SinAnio => Textos.Instancia.PeliculasSenalSinAnio,
        IdentificacionDePelicula.Porque.SoloTitulo => Textos.Instancia.PeliculasSenalSoloTitulo,
        IdentificacionDePelicula.Porque.Empate => Textos.Instancia.PeliculasSenalEmpate,
        IdentificacionDePelicula.Porque.TituloFlojo => Textos.Instancia.PeliculasSenalTituloFlojo,
        _ => Textos.Instancia.PeliculasSenalSinCandidatos,
    };

    public Brush Color => (Brush)Application.Current.FindResource(Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Va => "OrgOk",
        PlanDePeliculas.Porque.EnColeccion => "OrgOk",
        PlanDePeliculas.Porque.YaEsta => "Neutral500",
        PlanDePeliculas.Porque.EsExtra => "Neutral500",
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
/// Identificar contra TMDb es un paso <b>aparte y opcional</b>, con su propio
/// botón: primero se ve el plan tal y como sale de los nombres, y solo si se
/// pide se pregunta a nadie. Lo que vuelve con confianza se aplica al plan; lo
/// dudoso se enseña con la señal por la que se dudó y <b>no se toca</b>, porque
/// una película mal identificada es peor que una sin identificar.
/// </para>
/// </summary>
public partial class PeliculasWindow : Window
{
    private readonly IReadOnlyList<string> _ficheros;
    private readonly string _raiz;
    private readonly Settings _ajustes;
    private List<PlanDePeliculas.Paso> _plan = new();
    private Mudanza.Parte? _hecho;

    /// <summary>Lo que dijo TMDb de cada fichero, por ruta. Vacío hasta que se pida.</summary>
    private readonly Dictionary<string, IdentificacionDePelicula.Veredicto> _veredictos =
        new(StringComparer.OrdinalIgnoreCase);

    public PeliculasWindow(IReadOnlyList<string> ficheros, string raiz, Settings ajustes)
    {
        InitializeComponent();
        _ficheros = ficheros;
        _raiz = raiz;
        _ajustes = ajustes;

        btnCerrar.Click += (_, _) => Close();
        chkSoloLosQueVan.Checked += (_, _) => Pintar();
        chkSoloLosQueVan.Unchecked += (_, _) => Pintar();
        btnMover.Click += (_, _) => Mover();
        btnDeshacer.Click += (_, _) => Deshacer();
        btnIdentificar.Click += async (_, _) => await Identificar();

        PintarIdentificar();
        Recalcular();
    }

    /// <summary>
    /// El botón se puede pulsar cuando hay con qué preguntar. Cuando no, se
    /// queda visible y apagado <b>con el motivo al lado</b>: esconderlo dejaría
    /// la función invisible, y apagarlo sin explicación se lee como algo roto.
    /// </summary>
    private void PintarIdentificar()
    {
        bool listo = _ajustes.Tmdb.Listo;
        btnIdentificar.IsEnabled = listo && _hecho is null;
        lblTmdbApagado.Visibility = listo ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Pregunta a TMDb por cada película y vuelve a montar el plan con lo que se
    /// haya podido identificar <b>con seguridad</b>.
    ///
    /// <para>
    /// Es un paso aparte y a petición, no algo que pase al abrir la ventana: una
    /// app de disco que sale a internet sola, sin que se lo pidas, no es lo que
    /// nadie instaló. Lo ya preguntado sale de la caché, así que repetirlo no
    /// cuesta ni una consulta.
    /// </para>
    /// </summary>
    private async Task Identificar()
    {
        var clave = _ajustes.Tmdb.ClaveElegida().Clave;
        if (string.IsNullOrEmpty(clave)) return;

        btnIdentificar.IsEnabled = false;

        // El idioma de la app es el idioma en el que se pide el título, porque
        // es el que se va a escribir en el disco.
        var idioma = Idioma.Elegir("en-US", "es-ES");
        var cache = CacheDePeliculas.Abrir(CacheDePeliculas.Predeterminada());

        // Solo lo que trae ficha: un extra no la lleva, y por él no se pregunta.
        var aPreguntar = _plan
            .Where(p => p.Ficha is { } f && !string.IsNullOrWhiteSpace(f.Titulo))
            .ToList();

        int hechas = 0, seguras = 0, dudas = 0;
        bool falloRed = false;

        foreach (var paso in aPreguntar)
        {
            var ficha = paso.Ficha!;
            lblPie.Text = string.Format(Textos.Instancia.PeliculasIdentificando,
                                        hechas + 1, aPreguntar.Count);

            var candidatos = cache.Buscar(ficha.Titulo, ficha.Anio, idioma);
            if (candidatos is null)
            {
                var traidos = await Tmdb.Preguntar(ficha.Titulo, ficha.Anio, idioma, clave);

                // Un «no se pudo preguntar» NO se guarda: si se guardara, un rato
                // sin conexión dejaría esta película marcada como imposible para
                // siempre.
                if (traidos is null) { falloRed = true; hechas++; continue; }

                cache.Guardar(ficha.Titulo, ficha.Anio, idioma, traidos);
                candidatos = traidos;
            }

            var v = IdentificacionDePelicula.Decidir(ficha, candidatos);
            _veredictos[paso.Origen] = v;
            if (v.SePuedeAplicar) seguras++; else dudas++;
            hechas++;
        }

        cache.Volcar();
        Recalcular();
        PintarIdentificar();

        lblPie.Text = seguras == 0 && dudas == 0
            ? Textos.Instancia.PeliculasIdentificadaNinguna
            : dudas == 0
                ? string.Format(Textos.Instancia.PeliculasIdentificadasTodas, seguras)
                : string.Format(Textos.Instancia.PeliculasIdentificadas, seguras, dudas);

        if (falloRed) lblPie.Text += " " + Textos.Instancia.PeliculasSinRed;
    }

    private void Recalcular()
    {
        _plan = PlanDePeliculas.Montar(_ficheros, _raiz, identificada: FichaIdentificada);
        Pintar();
    }

    /// <summary>
    /// La ficha que va a usar el plan para este fichero: la de TMDb solo si se
    /// pudo identificar con seguridad. Una duda se enseña y no entra en el plan,
    /// que es la regla entera de esta pantalla.
    /// </summary>
    private TituloDePelicula.Ficha? FichaIdentificada(string origen)
        => _veredictos.TryGetValue(origen, out var v) && v.SePuedeAplicar
            ? IdentificacionDePelicula.Propuesta(v)
            : null;

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

        // «Solo las que tienen trabajo» esconde lo que ya está bien y los extras,
        // NO los problemas. Un fichero marcado «ocupado» o «sin título» es
        // exactamente lo que hay que mirar, y esconderlo por defecto deja fuera
        // de la vista lo único que puede salir mal.
        var visibles = chkSoloLosQueVan.IsChecked == true
            ? _plan.Where(p => p.Motivo is not (PlanDePeliculas.Porque.YaEsta or PlanDePeliculas.Porque.EsExtra))
            : _plan;
        lista.ItemsSource = visibles.Select(p => new PeliculaVista
        {
            Paso = p,
            Raiz = _raiz,
            Veredicto = _veredictos.TryGetValue(p.Origen, out var v) ? v : null,
        }).ToList();

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
        PintarIdentificar();

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
        bool aMedias = vueltos < eran;

        lblPie.Text = aMedias
            ? string.Format(Textos.Instancia.DeshacerAMedias, vueltos, eran - vueltos)
            : string.Format(Textos.Instancia.PeliculasDeshecho, vueltos);

        // Si alguno NO pudo volver —lo normal es que esté abierto en el
        // reproductor—, el registro se conserva y el botón se queda: es el único
        // sitio donde vive la lista de qué fue a dónde, y tirarlo dejaba ese
        // fichero desplazado para siempre sin forma de recuperarlo desde la app.
        // Reintentar es seguro: lo que ya volvió se salta solo.
        btnDeshacer.Visibility = aMedias ? Visibility.Visible : Visibility.Collapsed;

        // Si alguno no pudo volver, la tabla de quien nos abrió sigue estando mal.
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
