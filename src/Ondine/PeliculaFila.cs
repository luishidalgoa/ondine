using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Ondine.Localizacion;
using Ondine.Peliculas;
using Ondine.Reindex;

namespace Ondine;

/// <summary>
/// Una película en la tabla de repaso, ya lista para pintar.
///
/// <para>
/// Es el equivalente de <see cref="OrganizarRow"/> para películas, y es una clase
/// aparte a propósito. <c>OrganizarRow</c> lleva dentro un
/// <c>ReindexResolution</c>, una <c>LibraryTemplate</c> y un
/// <c>ReindexCatalog</c> —catálogo, plantilla y temporada—: una película no tiene
/// ninguna de las tres. Sacárselas para compartir la clase significaría abrir el
/// flujo de series, que es el que más se usa y el que más pruebas tiene, para
/// beneficiar al de películas. Lo que sí se comparte es la <b>forma</b> —los
/// mismos chips, la misma rejilla, la misma barra de aplicar—, y eso es lo que
/// hace que se reconozca; no hace falta compartir código para eso.
/// </para>
/// </summary>
public sealed class PeliculaFila : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public required PlanDePeliculas.Paso Paso { get; init; }
    public required string Raiz { get; init; }

    /// <summary>Lo que dijo TMDb de esta película, si se preguntó.</summary>
    public IdentificacionDePelicula.Veredicto? Veredicto { get; init; }

    private bool _marcado = true;

    /// <summary>
    /// Si esta fila entra en el próximo «Aplicar». Solo pesa cuando la fila tiene
    /// trabajo: lo que ya está bien, un extra o un nombre ocupado no se aplican
    /// jamás, marcados o no. Misma regla que en series.
    /// </summary>
    public bool Marcado
    {
        get => _marcado;
        set
        {
            if (_marcado == value) return;
            _marcado = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Marcado)));
        }
    }

    /// <summary>
    /// Si hay algo que aplicar aquí. Solo estas filas llevan casilla: ponerla en un
    /// conflicto insinuaría que marcándolo se aplica, y es al revés.
    /// </summary>
    public bool ListoParaAplicar =>
        Paso.Motivo is PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion;

    /// <summary>Y esta es la que de verdad decide: marcada Y con trabajo.</summary>
    public bool Entra => Marcado && ListoParaAplicar && Paso.Destino is not null;

    public string Original => Relativa(Paso.Origen);

    /// <summary>
    /// A dónde iría. En películas cambia el nombre del fichero además de la
    /// carpeta, así que se enseña la ruta relativa entera y no solo el directorio.
    /// </summary>
    public string Propuesta => Paso.Destino is { } d ? Relativa(d) : "";

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

    // ── El chip de estado: glifo + palabra + color, nunca solo color ──────────

    public string EstadoTexto => Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Va => Textos.Instancia.PeliculasPorqueVa,
        PlanDePeliculas.Porque.EnColeccion => Textos.Instancia.PeliculasPorqueEnColeccion,
        PlanDePeliculas.Porque.YaEsta => Textos.Instancia.PeliculasPorqueYaEsta,
        PlanDePeliculas.Porque.SinTitulo => Textos.Instancia.PeliculasPorqueSinTitulo,
        PlanDePeliculas.Porque.EsExtra => Textos.Instancia.PeliculasPorqueEsExtra,
        _ => Textos.Instancia.PeliculasPorqueOcupado,
    };

    public string EstadoGlifo => Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Va => "●",
        PlanDePeliculas.Porque.EnColeccion => "↻",
        PlanDePeliculas.Porque.YaEsta => "✓",
        PlanDePeliculas.Porque.EsExtra => "◦",
        PlanDePeliculas.Porque.Ocupado => "◆",
        _ => "▲",
    };

    /// <summary>Para ordenar por estado: agrupa lo que hay que mirar arriba.</summary>
    public int EstadoVisible => Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Ocupado => 0,
        PlanDePeliculas.Porque.SinTitulo => 1,
        PlanDePeliculas.Porque.Va => 2,
        PlanDePeliculas.Porque.EnColeccion => 3,
        PlanDePeliculas.Porque.YaEsta => 4,
        _ => 5,
    };

    private string Recurso => Paso.Motivo switch
    {
        PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion => "OrgOk",
        PlanDePeliculas.Porque.Ocupado => "OrgDanger",
        PlanDePeliculas.Porque.SinTitulo => "OrgWarn",
        _ => "Neutral500",
    };

    public Brush EstadoFg => Pincel(Recurso);
    public Brush EstadoBg => Recurso == "Neutral500" ? Pincel("Field") : Pincel(Recurso + "Bg");
    public Brush EstadoBorde => Recurso == "Neutral500" ? Pincel("Divider") : Pincel(Recurso + "Borde");

    private static Brush Pincel(string clave)
    {
        // «Borde» es el sufijo en castellano de este fichero; los recursos del tema
        // lo llaman «Border». Se traduce aquí en vez de en cada rama de arriba.
        var real = clave.EndsWith("Borde", StringComparison.Ordinal)
            ? clave[..^5] + "Border"
            : clave;
        return (Brush)Application.Current.FindResource(real);
    }

    // ── «Por qué», que es la columna que convierte un color en una razón ─────

    /// <summary>
    /// El motivo, escrito. Cuando TMDb ha dicho algo, manda su señal: es la
    /// información nueva, y la que decide si fiarse de la propuesta.
    /// </summary>
    public string PorQue
    {
        get
        {
            if (Veredicto is { } v) return Senal(v.Senal);

            return Paso.Motivo switch
            {
                PlanDePeliculas.Porque.YaEsta => Textos.Instancia.PeliculasPorQueYaCumple,
                PlanDePeliculas.Porque.EsExtra => Textos.Instancia.PeliculasPorQueExtra,
                PlanDePeliculas.Porque.Ocupado => Textos.Instancia.PeliculasPorQueOcupadoDetalle,
                PlanDePeliculas.Porque.SinTitulo => Textos.Instancia.PeliculasPorQueSinTituloDetalle,
                _ => Textos.Instancia.PeliculasPorQueDelNombre,
            };
        }
    }

    /// <summary>Lo mismo, con lo que propuso TMDb delante, para el tooltip.</summary>
    public string Explicacion
    {
        get
        {
            if (Veredicto is not { } v) return PorQue;

            var propuesta = IdentificacionDePelicula.Propuesta(v);
            return propuesta is null
                ? PorQue
                : string.Format(Textos.Instancia.PeliculasSegunTmdb,
                                TituloDePelicula.Canonico(propuesta)) + " · " + PorQue;
        }
    }

    /// <summary>Si lo que dijo TMDb entró en la propuesta o solo se enseña.</summary>
    public Brush PorQueColor => Veredicto is null
        ? Pincel("Neutral500")
        : Pincel(Veredicto.SePuedeAplicar ? "OrgOk" : "OrgWarn");

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
}
