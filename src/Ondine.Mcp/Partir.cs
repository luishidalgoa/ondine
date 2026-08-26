using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Ondine.Recortes;
using Ondine.Reindex;

namespace Ondine.Mcp;

/// <summary>
/// Partir un vídeo en trozos, o quedarse con un trozo.
///
/// <para>
/// Es lo único que hace Ondine y no hace nadie más: separar un fichero de 44 minutos que en
/// realidad son dos episodios pegados, y hacerlo <b>sin recodificar</b>, copiando los flujos tal
/// cual. Cada mitad sale con su nombre y el vídeo es idéntico bit a bit.
/// </para>
/// <para>
/// <b>Y los cortes se mueven, que es lo que hay que decir por delante.</b> Copiando sin
/// recodificar, un corte solo puede caer en un fotograma clave: si pides el minuto 21:47 y el
/// fotograma clave más cercano por debajo está en 21:45,3, el corte va ahí. Nunca DESPUÉS —
/// adelantarlo comería el final del trozo anterior—. El ensayo dice, para cada corte, dónde va a
/// caer de verdad, que es la única forma de que no sorprenda.
/// </para>
/// <para>
/// Con <c>sin_recodificar: false</c> el corte cae exactamente donde se pide, porque se
/// recodifica. Cuesta lo que cuesta comprimir y el vídeo ya no es el mismo.
/// </para>
/// </summary>
// OJO CON EL NOMBRE: esta clase se llamó «Recortes» y chocaba con el namespace
// «Ondine.Recortes» del motor — desde otro namespace, el nombre suelto resolvía al del motor y
// el compilador buscaba un tipo «Ondine.Recortes.Momento». Se llama como su herramienta.
internal static class Partir
{
    public static Resultado Ejecutar(JsonObject a)
    {
        var ruta = Texto(a, "fichero");
        if (ruta is null) return Resultado.Error("Falta «fichero».");
        if (!File.Exists(ruta)) return Resultado.Error($"No existe: {ruta}");

        var motor = new Engine();
        double duracion;
        try { duracion = motor.ProbeAsync(ruta).GetAwaiter().GetResult().DurationSec; }
        catch (Exception ex) { return Resultado.Error("No se ha podido sondear: " + ex.Message); }

        if (duracion <= 0) return Resultado.Error("Ese fichero no dice cuánto dura: no se puede partir a ciegas.");

        var tramos = Pedidos(a, duracion, out var error);
        if (tramos is null) return Resultado.Error(error!);

        var sinRecodificar = Bandera(a, "sin_recodificar", true);
        var destino = Texto(a, "salida") ?? Path.Combine(Path.GetDirectoryName(ruta)!, "recortes");
        var nombres = Tramos.Nombrar(Path.GetFileNameWithoutExtension(ruta), tramos.Count);

        var sb = new StringBuilder($"«{Path.GetFileName(ruta)}» dura {Reloj(duracion)}. "
                                 + $"{tramos.Count} {(tramos.Count == 1 ? "trozo" : "trozos")}:\n");

        for (int i = 0; i < tramos.Count; i++)
            sb.AppendLine($"  {nombres[i]}   {Reloj(tramos[i].Inicio)} → {Reloj(tramos[i].Fin)}"
                        + $"   ({Reloj(tramos[i].Duracion)})");

        sb.AppendLine($"\n  Salida: {destino}");
        sb.AppendLine(sinRecodificar
            ? "  Sin recodificar: se copian los flujos y el vídeo queda idéntico."
            : "  RECODIFICANDO: el corte cae exacto, pero cuesta como comprimir y el vídeo cambia.");

        // ── Dónde van a caer los cortes de verdad ────────────────────────────
        // Solo copiando: recodificando caen donde se piden. Se sondea el fichero, que es la
        // única forma de saberlo — la separación entre fotogramas clave la decide quien lo
        // codificó, y va de medio segundo a diez.
        if (sinRecodificar && tramos.Count > 1)
        {
            sb.AppendLine("\n  Los cortes caen en el fotograma clave anterior:");
            foreach (var t in tramos.Skip(1))
            {
                try
                {
                    var claves = FotogramasClave.AntesDeAsync(ruta, t.Inicio).GetAwaiter().GetResult();
                    var donde = CorteSinRecodificar.DondeCae(claves, t.Inicio);
                    sb.AppendLine(donde.SeSabe
                        ? $"    pides {Reloj(t.Inicio)} → cae en {Reloj(donde.Real)}"
                          + (Math.Abs(donde.Real - t.Inicio) < 0.05 ? "  (exacto)" : "")
                        : $"    pides {Reloj(t.Inicio)} → no se ha podido saber dónde cae");
                }
                catch { sb.AppendLine($"    pides {Reloj(t.Inicio)} → no se ha podido saber dónde cae"); }
            }
        }

        if (!Bandera(a, "confirmar", false)) return Resultado.Ensayo(sb.ToString());

        Directory.CreateDirectory(destino);
        var hechos = new List<string>();
        var fallidos = new List<string>();
        var reservadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < tramos.Count; i++)
        {
            var t = tramos[i];
            var salida = RutaDeSalida.Libre(destino, nombres[i], Path.GetExtension(ruta),
                                            File.Exists, reservadas);
            try
            {
                if (sinRecodificar)
                {
                    var r = CortadorSinRecodificar
                        .CortarAsync(ruta, salida, t.Inicio, t.Duracion).GetAwaiter().GetResult();
                    if (r.Ok) hechos.Add(Path.GetFileName(r.Salida)); else fallidos.Add(nombres[i]);
                }
                else
                {
                    var opt = Comprimir.Opciones(a, out var errorOpt);
                    if (opt is null) return Resultado.Error(errorOpt!);
                    opt.Output = destino;
                    opt.Desde = t.Inicio;
                    opt.Duracion = t.Duracion;
                    opt.NombreSalida = Path.GetFileNameWithoutExtension(salida);

                    var res = motor.CompressAsync([ruta], opt, new Tandas.Cuaderno(), CancellationToken.None)
                                   .GetAwaiter().GetResult();
                    if (res.Any(x => x.OutBytes is > 0)) hechos.Add(Path.GetFileName(salida));
                    else fallidos.Add(nombres[i]);
                }
            }
            catch (Exception ex) { fallidos.Add($"{nombres[i]} ({ex.Message})"); }
        }

        var parte = new StringBuilder($"{hechos.Count} de {tramos.Count} en {destino}:\n");
        foreach (var h in hechos) parte.AppendLine($"  ✓ {h}");
        foreach (var f in fallidos) parte.AppendLine($"  ✗ {f}");
        parte.Append("\nEl original no se ha tocado.");

        return fallidos.Count == tramos.Count
            ? Resultado.Error(parte.ToString())
            : Resultado.Ok(parte.ToString());
    }

    /// <summary>
    /// Los trozos que se piden, de las dos formas en que se piden: unos puntos de corte, o un
    /// trozo suelto con su principio y su fin.
    /// </summary>
    private static List<Tramo>? Pedidos(JsonObject a, double duracion, out string? error)
    {
        error = null;

        var cortes = Momentos(a, "cortes");
        var desde = Momento(a, "desde");
        var hasta = Momento(a, "hasta");

        if (cortes.Count > 0 && (desde is not null || hasta is not null))
        {
            error = "O «cortes», o «desde»/«hasta». Las dos cosas juntas no significan nada claro.";
            return null;
        }

        if (cortes.Count > 0)
        {
            var fuera = cortes.Where(c => c <= 0 || c >= duracion).ToList();
            if (fuera.Count > 0)
            {
                error = $"Estos cortes caen fuera del vídeo, que dura {Reloj(duracion)}: "
                      + string.Join(", ", fuera.Select(Reloj)) + ".";
                return null;
            }

            var tramos = Tramos.Entero(duracion);
            foreach (var c in cortes.Distinct().OrderBy(x => x)) tramos = Tramos.Partir(tramos, c);
            return tramos;
        }

        if (desde is null && hasta is null)
        {
            error = "Dime qué quieres: «cortes» con los puntos donde partir, o «desde» y «hasta» "
                  + "para quedarte con un trozo. Se admiten segundos o «mm:ss».";
            return null;
        }

        var ini = desde ?? 0;
        var fin = hasta ?? duracion;
        if (ini < 0 || fin > duracion + 0.5 || fin <= ini)
        {
            error = $"Ese trozo no cabe: el vídeo va de 0 a {Reloj(duracion)}.";
            return null;
        }

        return [new Tramo(ini, Math.Min(fin, duracion))];
    }

    // ── Tiempos ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Un momento, en segundos o en reloj. Un agente escribe «21:47» tan a menudo como 1307, y
    /// las dos formas dicen lo mismo.
    /// </summary>
    internal static double? Momento(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return null;
        return Momento(v.ToString());
    }

    internal static double? Momento(string texto)
    {
        var t = texto.Trim();
        if (t.Length == 0) return null;

        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var seg))
            return seg;

        var trozos = t.Split(':');
        if (trozos.Length is < 2 or > 3) return null;

        double total = 0;
        foreach (var p in trozos)
        {
            if (!double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)) return null;
            total = total * 60 + n;
        }
        return total;
    }

    private static List<double> Momentos(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return [];

        var crudos = v is JsonArray arr
            ? arr.Where(x => x is not null).Select(x => x!.ToString())
            : [v.ToString()];

        return [.. crudos.Select(Momento).Where(x => x is not null).Select(x => x!.Value)];
    }

    /// <summary>Un tiempo en reloj, que es como se lee un vídeo.</summary>
    internal static string Reloj(double segundos)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, segundos));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes}:{t.Seconds:00}";
    }

    private static string? Texto(JsonObject a, string clave) =>
        a.TryGetPropertyValue(clave, out var v) && v is not null && v.ToString() is { Length: > 0 } s ? s : null;

    private static bool Bandera(JsonObject a, string clave, bool porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<bool>(); } catch { return porDefecto; }
    }
}
