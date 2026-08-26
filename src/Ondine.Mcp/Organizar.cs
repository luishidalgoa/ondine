using System.Text;
using System.Text.Json.Nodes;
using Ondine.Reindex;

namespace Ondine.Mcp;

/// <summary>
/// Las decisiones de Organizar que no son «aplica lo seguro»: fijar a mano el episodio de una
/// duda, dejar un fichero como está, y deshacer una tanda.
///
/// <para>
/// <b>Esto era el último hueco, y el que más costaba explicar.</b> El agente sabía analizar y
/// aplicar lo que salía verde, que es el camino de la mayoría; lo que no podía era resolver una
/// duda. Y las dudas no son un caso raro: un episodio con el título mal escrito, dos ficheros que
/// reclaman el mismo número, un especial sin numerar. En la app eso se arregla con dos clics, y
/// por MCP había que abrir la ventana.
/// </para>
/// <para>
/// <b>Las decisiones se guardan donde las guarda la app</b>, en el mismo fichero y con la misma
/// clave, así que lo que resuelva el agente lo ve la ventana y al revés. Fue lo primero que
/// comprobé: si cada uno guardara en su sitio, «ya te lo dije» sería la queja de todas las
/// semanas.
/// </para>
/// </summary>
internal static class Organizar
{
    // ── Fijar el episodio de una duda ────────────────────────────────────────

    /// <summary>
    /// «Este fichero es el episodio 72». Se guarda para no volver a preguntarlo.
    ///
    /// <para>
    /// Se guarda con el nombre original al lado, por trazabilidad: dentro de un año, «episodio
    /// 72» sin más no le dice nada a nadie. Es la misma decisión que toma la ventana y va al
    /// mismo sitio.
    /// </para>
    /// </summary>
    public static Resultado Fijar(JsonObject a)
    {
        var fichero = Texto(a, "fichero");
        var catalogo = Texto(a, "catalogo");
        var num = Entero(a, "episodio", 0);

        if (fichero is null) return Resultado.Error("Falta «fichero».");
        if (!File.Exists(fichero)) return Resultado.Error($"No existe: {fichero}");
        if (catalogo is null) return Resultado.Error("Falta «catalogo»: el episodio se fija contra uno.");
        if (!File.Exists(catalogo)) return Resultado.Error($"No existe el catálogo: {catalogo}");
        if (num <= 0) return Resultado.Error("Falta «episodio»: el número que le corresponde en el catálogo.");

        ReindexCatalog cat;
        try { cat = ReindexCatalog.Load(catalogo); }
        catch (Exception ex) { return Resultado.Error("El catálogo no se puede leer: " + ex.Message); }

        var ep = cat.PorNum(num);
        if (ep is null)
        {
            // Los números que hay DE VERDAD, no «del 1 al Total»: «total» es un campo declarado
            // que muchos catálogos no traen —salía «va del 1 al 0» en uno de dos episodios— y
            // aunque venga, la numeración salta valores. Se dice cuántos hay y entre qué números,
            // que es lo que le sirve a quien acaba de pedir uno que no existe.
            var nums = cat.Episodios.Select(e => e.Num).Where(n => n > 0).ToList();
            return Resultado.Error($"El catálogo de «{cat.Serie}» no tiene el episodio {num}. "
                + (nums.Count == 0
                    ? "No tiene ninguno."
                    : $"Tiene {nums.Count}, entre el {nums.Min()} y el {nums.Max()}"
                      + (nums.Count < nums.Max() - nums.Min() + 1
                            ? ", y la numeración salta valores." : ".")));
        }

        var seg = Texto(a, "segmento");
        var titulo = ep.TitulosSalida.FirstOrDefault() ?? $"episodio {num}";

        var queHaria = $"«{Path.GetFileName(fichero)}» será el episodio {num}"
                     + (ep.Temporada is { } t ? $" (temporada {t})" : "")
                     + $": «{titulo}»"
                     + (seg is not null ? $", segmento «{seg}»" : "")
                     + $"\n  Serie: {cat.Serie}"
                     + "\n  Se recuerda, así que la próxima vez que analices esta carpeta ya no será una duda.";

        if (!Bandera(a, "confirmar", false)) return Resultado.Ensayo(queHaria);

        var decisiones = ReindexStore.CargarDecisiones();

        // La clave es la MISMA que usa la app: el «fingerprint» de la señal, que sin uno de
        // contenido es la ruta completa. Si aquí se usara otra, la ventana no vería esta
        // decisión y el agente no vería las suyas.
        decisiones[fichero] = new ReindexOverride
        {
            Num = num,
            Temporada = ep.Temporada,
            Serie = cat.Serie,
            Origen = "mcp",
            Seg = seg,
            FechaDecision = DateTime.Now.ToString("yyyy-MM-dd"),
            NombreOriginal = Path.GetFileName(fichero),
        };
        ReindexStore.GuardarDecisiones(decisiones);

        return Resultado.Ok("Decidido. " + queHaria);
    }

    // ── Dejar como está ──────────────────────────────────────────────────────

    /// <summary>
    /// «A este no lo toques». Se apunta en el catálogo, que es donde lo apunta la app.
    ///
    /// <para>
    /// Es para lo que no es un episodio: un avance, una carátula en vídeo, el capítulo que ya
    /// está bien nombrado y no quieres que nadie proponga otra cosa. Se apunta en el catálogo y
    /// no en las decisiones porque pertenece a la serie, no al fichero: quien se lleve el
    /// catálogo a otra máquina se lleva también esto.
    /// </para>
    /// </summary>
    public static Resultado DejarComoEsta(JsonObject a)
    {
        var catalogo = Texto(a, "catalogo");
        if (catalogo is null) return Resultado.Error("Falta «catalogo».");
        if (!File.Exists(catalogo)) return Resultado.Error($"No existe el catálogo: {catalogo}");

        var ficheros = Lista(a, "ficheros");
        var uno = Texto(a, "fichero");
        if (uno is not null) ficheros.Add(uno);

        if (ficheros.Count == 0)
            return Resultado.Error("Dime qué dejar como está: «fichero», o «ficheros» con varios.");

        // Se apunta por NOMBRE y no por ruta: es lo que hace la app, y así sigue valiendo cuando
        // el fichero cambia de carpeta.
        var nombres = ficheros.Select(Path.GetFileName).Where(n => n is { Length: > 0 }).Select(n => n!).ToList();

        var queHaria = "Se dejarían como están, y ningún análisis volverá a proponerles nombre:\n"
                     + string.Join("\n", nombres.Select(n => "  " + n));

        if (!Bandera(a, "confirmar", false)) return Resultado.Ensayo(queHaria);

        int puestos;
        try { puestos = ReindexCatalog.AnadirADejarComoEsta(catalogo, nombres); }
        catch (Exception ex) { return Resultado.Error("No se ha podido escribir en el catálogo: " + ex.Message); }

        return Resultado.Ok(puestos == 0
            ? "Ya estaban todos apuntados: no había nada que añadir."
            : $"Apuntados {puestos} en el catálogo.\n" + queHaria);
    }

    // ── Deshacer la última tanda ─────────────────────────────────────────────

    /// <summary>
    /// Devolver los ficheros de la última tanda a sus nombres de antes.
    ///
    /// <para>
    /// Y esto <b>no funcionaba por MCP</b> hasta ahora, por una razón que no se ve: deshacer se
    /// apoya en un diario que la app escribe al aplicar, y el renombrado por MCP no lo escribía.
    /// O sea que había una tanda aplicada y nada que deshacer. Ahora se escribe el diario en el
    /// mismo sitio y con el mismo formato, así que se puede deshacer desde aquí y también desde
    /// la ventana.
    /// </para>
    /// </summary>
    public static Resultado Deshacer(JsonObject a)
    {
        var lote = ReindexStore.UltimoLote();
        if (lote is null)
            return Resultado.Error("No hay ninguna tanda que deshacer. Se apunta una cada vez que "
                                 + "se aplica un renombrado, desde aquí o desde la app.");

        var queHaria = $"Se devolverían {lote.Movimientos.Count} ficheros a sus nombres de antes.\n"
                     + $"  Tanda: {lote.Etiqueta}\n"
                     + $"  Carpeta: {lote.Carpeta}\n"
                     + string.Join("\n", lote.Movimientos.Take(10)
                         .Select(m => $"    {Path.GetFileName(m.A)}\n      → {Path.GetFileName(m.De)}"))
                     + (lote.Movimientos.Count > 10 ? $"\n    … y {lote.Movimientos.Count - 10} más." : "");

        if (!Bandera(a, "confirmar", false)) return Resultado.Ensayo(queHaria);

        var (devueltos, fallidos) = ReindexStore.Deshacer(lote);
        ReindexStore.OlvidarLote(lote);

        return Resultado.Ok($"Devueltos {devueltos}"
            + (fallidos > 0 ? $", y {fallidos} no se han podido: alguien los habrá movido o "
                            + "renombrado por su cuenta." : ".")
            + $"\n  Tanda: {lote.Etiqueta}");
    }

    // ── El diario, que es lo que hace posible deshacer ───────────────────────

    /// <summary>
    /// Apunta lo que se acaba de mover, en el mismo diario que escribe la app.
    ///
    /// <para>
    /// Sin esto, «deshacer» no tiene nada que deshacer. Lo llama el renombrado por MCP justo
    /// después de mover, con la misma forma que la ventana: id con la fecha y la hora, la serie,
    /// la carpeta, y un movimiento por fichero con su de y su a.
    /// </para>
    /// </summary>
    public static void Apuntar(string serie, string carpeta, IEnumerable<(string De, string A)> movimientos)
    {
        var lista = movimientos.ToList();
        if (lista.Count == 0) return;

        var ahora = DateTime.Now;
        var lote = new LoteJournal
        {
            Id = ahora.ToString("yyyyMMdd-HHmmss"),
            Fecha = ahora.ToString("yyyy-MM-dd"),
            Hora = ahora.ToString("HH:mm"),
            Serie = serie,
            Carpeta = carpeta,
        };
        foreach (var (de, a) in lista)
            lote.Movimientos.Add(new MovimientoJournal { De = de, A = a });

        try { ReindexStore.EscribirJournal(lote); } catch { /* no poder deshacer no invalida lo hecho */ }
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────

    private static string? Texto(JsonObject a, string clave) =>
        a.TryGetPropertyValue(clave, out var v) && v is not null && v.ToString() is { Length: > 0 } s ? s : null;

    private static bool Bandera(JsonObject a, string clave, bool porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<bool>(); } catch { return porDefecto; }
    }

    private static int Entero(JsonObject a, string clave, int porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<int>(); } catch { return int.TryParse(v.ToString(), out var n) ? n : porDefecto; }
    }

    private static List<string> Lista(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return [];
        if (v is JsonArray arr)
            return [.. arr.Where(x => x is not null).Select(x => x!.ToString().Trim()).Where(s => s.Length > 0)];
        var suelta = v.ToString().Trim();
        return suelta.Length > 0 ? [suelta] : [];
    }
}
