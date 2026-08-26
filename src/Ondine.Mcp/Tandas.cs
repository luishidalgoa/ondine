using System.Text;

namespace Ondine.Mcp;

/// <summary>
/// Las tandas que corren en segundo plano, para que comprimir no sea una llamada de una hora.
///
/// <para>
/// <b>El problema.</b> Una llamada MCP contesta cuando termina, y comprimir una temporada tarda
/// lo que tarda: en una máquina modesta, con un codificador por software, más de una hora. Casi
/// ningún cliente espera tanto — y aunque esperase, durante esa hora no hay forma de saber si
/// avanza, ni de pararlo.
/// </para>
/// <para>
/// <b>La solución.</b> La tanda arranca, se devuelve un identificador y la llamada vuelve al
/// momento. Después se pregunta por ella cuantas veces haga falta —por dónde va, qué lleva
/// hecho, cuánto ha ahorrado— y se puede parar. Es lo que hace la ventana con su barra de
/// progreso, contado por otro camino.
/// </para>
/// <para>
/// <b>Y lo que hay que saber:</b> esto vive en la memoria del servidor. Si se cierra el cliente y
/// con él el servidor, la tanda se va con él y el fichero a medias se borra, igual que al cerrar
/// la app a mitad. No hay tandas que sobrevivan a quien las lanzó, y no las va a haber sin un
/// servicio aparte que las cuide.
/// </para>
/// </summary>
internal static class Tandas
{
    /// <summary>
    /// El cuaderno de una tanda: lo que el motor va contando, y por dónde va.
    ///
    /// <para>
    /// Guarda el progreso del fichero en curso porque es lo único que distingue «trabajando» de
    /// «colgado» cuando un capítulo tarda veinte minutos.
    /// </para>
    /// </summary>
    internal sealed class Cuaderno : IEngineReporter
    {
        private readonly object _cerrojo = new();

        public List<string> Lineas { get; } = [];
        public List<(string Ruta, string Motivo)> Saltados { get; } = [];
        public List<FileResult> Hechos { get; } = [];

        public int Indice { get; private set; }
        public int Total { get; private set; }
        public string EnCurso { get; private set; } = "";
        public double Parte { get; private set; }
        public bool EsperandoDisco { get; private set; }

        public void Log(string linea) { lock (_cerrojo) Lineas.Add(linea); }

        public void FileStart(int indice, int total, string nombre, double duracionSeg)
        {
            lock (_cerrojo) { Indice = indice; Total = total; EnCurso = nombre; Parte = 0; }
        }

        public void FileProgress(double parte, string linea) { lock (_cerrojo) Parte = parte; }

        public void FileDone(FileResult r) { lock (_cerrojo) { Hechos.Add(r); Parte = 1; } }

        public void FileSkipped(string ruta, string motivo) { lock (_cerrojo) Saltados.Add((ruta, motivo)); }

        public void DiskFull(bool pausada) { lock (_cerrojo) EsperandoDisco = pausada; }

        public (int Indice, int Total, string EnCurso, double Parte, bool Disco) Donde()
        {
            lock (_cerrojo) return (Indice, Total, EnCurso, Parte, EsperandoDisco);
        }
    }

    private sealed record Tanda(
        string Id,
        CancellationTokenSource Corte,
        Cuaderno Cuaderno,
        Task<List<FileResult>> Faena,
        int Pedidos);

    private static readonly Dictionary<string, Tanda> Vivas = new();
    private static string? _ultima;

    /// <summary>Arranca una tanda y devuelve su identificador, sin esperar a que acabe.</summary>
    public static string Arrancar(Engine motor, IReadOnlyList<string> videos, EncodeOptions opt,
                                  Action<List<FileResult>, Cuaderno>? alTerminar = null)
    {
        var id = "t" + Guid.NewGuid().ToString("N")[..6];
        var corte = new CancellationTokenSource();
        var cuaderno = new Cuaderno();

        var faena = Task.Run(async () =>
        {
            var hechos = await motor.CompressAsync(videos, opt, cuaderno, corte.Token);
            alTerminar?.Invoke(hechos, cuaderno);
            return hechos;
        }, corte.Token);

        lock (Vivas) { Vivas[id] = new Tanda(id, corte, cuaderno, faena, videos.Count); _ultima = id; }
        return id;
    }

    /// <summary>Por dónde va, o cómo acabó.</summary>
    public static Resultado Estado(string? id)
    {
        var t = Buscar(id, out var error);
        if (t is null) return Resultado.Error(error!);

        var (indice, total, enCurso, parte, disco) = t.Cuaderno.Donde();
        var sb = new StringBuilder($"Tanda {t.Id}: ");

        if (!t.Faena.IsCompleted)
        {
            sb.AppendLine($"trabajando, {indice} de {(total > 0 ? total : t.Pedidos)}.");
            if (enCurso.Length > 0)
                sb.AppendLine($"  ahora: {enCurso}  ({(int)(parte * 100)} % de este fichero)");
            if (disco)
                sb.AppendLine("  EN PAUSA: se ha llenado el disco. Libera espacio y sigue sola, "
                            + "sin perder la cola.");
            sb.Append(Avance(t));
            sb.Append("\n\nVuelve a preguntar cuando quieras, o párala con ondine_parar_tanda.");
            return Resultado.Ok(sb.ToString());
        }

        if (t.Faena.IsCanceled || (t.Faena.IsFaulted && t.Faena.Exception?.InnerException is OperationCanceledException))
        {
            lock (Vivas) Vivas.Remove(t.Id);
            return Resultado.Ok($"Tanda {t.Id}: parada.\n{Avance(t)}\n\nLo que ya estaba hecho se "
                              + "queda; el fichero a medias se ha borrado.");
        }

        if (t.Faena.IsFaulted)
        {
            lock (Vivas) Vivas.Remove(t.Id);
            var por = t.Faena.Exception?.InnerException?.Message ?? "sin motivo";
            return Resultado.Error($"Tanda {t.Id}: se ha parado sola. {por}\n{Avance(t)}");
        }

        lock (Vivas) Vivas.Remove(t.Id);
        return Resultado.Ok(Comprimir.Parte(t.Faena.Result, t.Cuaderno, t.Pedidos));
    }

    /// <summary>La para. Lo hecho se queda; el fichero a medias se borra, como al cerrar la app.</summary>
    public static Resultado Parar(string? id)
    {
        var t = Buscar(id, out var error);
        if (t is null) return Resultado.Error(error!);

        if (t.Faena.IsCompleted) return Estado(t.Id);

        t.Corte.Cancel();
        return Resultado.Ok($"Tanda {t.Id}: parando. {Avance(t)}\n\nPregunta por ella en unos "
                          + "segundos para ver cómo quedó.");
    }

    private static Tanda? Buscar(string? id, out string? error)
    {
        error = null;
        lock (Vivas)
        {
            // Sin id, la última: es lo que quiere decir «¿cómo va?» cuando solo hay una.
            var cual = id is { Length: > 0 } ? id : _ultima;
            if (cual is null || !Vivas.TryGetValue(cual, out var t))
            {
                error = Vivas.Count == 0
                    ? "No hay ninguna tanda en marcha. Se arranca con ondine_comprimir y "
                    + "«en_segundo_plano»: true."
                    : $"No encuentro la tanda «{id}». Las que hay: {string.Join(", ", Vivas.Keys)}.";
                return null;
            }
            return t;
        }
    }

    /// <summary>Lo que llevamos ahorrado, que es la pregunta de debajo de «¿cómo va?».</summary>
    private static string Avance(Tanda t)
    {
        var hechos = t.Cuaderno.Hechos.Where(r => r.OutBytes is > 0).ToList();
        if (hechos.Count == 0) return "  todavía sin ningún fichero terminado.";

        long entra = hechos.Sum(r => r.InBytes), sale = hechos.Sum(r => r.OutBytes ?? 0);
        return $"  {hechos.Count} terminados: {Comprimir.Peso(entra)} → {Comprimir.Peso(sale)} "
             + $"({Comprimir.Variacion(entra, sale)}).";
    }
}
