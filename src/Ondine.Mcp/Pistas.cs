using System.Text;
using System.Text.Json.Nodes;

namespace Ondine.Mcp;

/// <summary>
/// Quitar pistas de un vídeo sin recodificar nada.
///
/// <para>
/// <b>Es el ahorro más barato que hay.</b> Un capítulo con cuatro doblajes y seis subtítulos
/// puede llevar la mitad de su peso en cosas que nadie va a usar, y quitarlas no toca el vídeo:
/// se copian los flujos que se quedan y se reempaqueta. Cero pérdida de calidad, segundos en vez
/// de minutos. Antes de recodificar una biblioteca, esto es lo primero que conviene mirar.
/// </para>
/// <para>
/// <b>El vídeo no se puede quitar</b>, y no es un descuido: lo impide
/// <see cref="SelectorDePistas.Planificar"/>, porque un fichero de vídeo sin vídeo no es nada.
/// </para>
/// <para>
/// Y esto SOBRESCRIBE el fichero, que es lo que lo distingue de comprimir. El motor pone el
/// original a salvo en la papelera antes de tocarlo y, si no puede, no hace nada: prefiere no
/// hacerlo a hacerlo sin red.
/// </para>
/// </summary>
internal static class QuitarPistas
{
    public static Resultado Ejecutar(JsonObject a)
    {
        var ruta = Texto(a, "fichero");
        if (ruta is null) return Resultado.Error("Falta «fichero».");
        if (!File.Exists(ruta)) return Resultado.Error($"No existe: {ruta}");

        var motor = new Engine();
        IReadOnlyList<Pista> pistas;
        try { (pistas, _) = motor.PistasDeAsync(ruta).GetAwaiter().GetResult(); }
        catch (Exception ex) { return Resultado.Error("No se han podido leer las pistas: " + ex.Message); }

        if (pistas.Count == 0) return Resultado.Error("Ese fichero no declara ninguna pista.");

        var indices = Numeros(a, "indices");
        var idiomas = Lista(a, "idiomas");

        // Sin decir qué quitar, esto es una pregunta: se contesta el inventario. Un agente
        // necesita ver los índices antes de poder elegir, y pedirle que adivine sería absurdo.
        if (indices.Count == 0 && idiomas.Count == 0)
            return Resultado.Ok(Inventario(ruta, pistas)
                + "\n\nDime qué quitar: «indices» con los números de arriba, o «idiomas» con los "
                + "códigos. El vídeo no se puede quitar.");

        // Por idioma se van las de audio y subtítulo de esos idiomas. El vídeo nunca.
        var porIdioma = pistas
            .Where(p => p.Tipo != TipoPista.Video
                     && idiomas.Any(l => string.Equals(l, p.Idioma, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Indice);

        var aQuitar = indices.Concat(porIdioma).Distinct().ToList();

        var noEstan = indices.Where(i => pistas.All(p => p.Indice != i)).ToList();
        if (noEstan.Count > 0)
            return Resultado.Error($"Ese fichero no tiene las pistas {string.Join(", ", noEstan)}. "
                                 + "Llama sin «indices» para ver las que hay.");

        var plan = SelectorDePistas.Planificar(pistas, aQuitar);

        if (!plan.HayCambios)
            return Resultado.Error("Eso no quita nada: o son pistas de vídeo, que no se pueden "
                                 + "quitar, o esos idiomas no están en el fichero.");

        var sb = new StringBuilder(Inventario(ruta, pistas));
        sb.AppendLine("\nSe quitarían:");
        foreach (var p in plan.Quitadas) sb.AppendLine($"  – {Linea(p)}");
        if (plan.QuedaSinAudio)
            sb.AppendLine("\nOJO: se queda SIN NINGUNA pista de audio. Se puede querer un vídeo "
                        + "mudo, pero casi nunca es lo que se pretendía.");

        if (!Bandera(a, "confirmar", false))
            return Resultado.Ensayo(sb + "\nEl vídeo no se recodifica: se copian los flujos que se "
                                       + "quedan y se reempaqueta. El original va a la papelera antes.");

        var (ok, mensaje, antes, despues) = motor
            .QuitarPistasAsync(ruta, plan, CancellationToken.None).GetAwaiter().GetResult();

        if (!ok) return Resultado.Error(mensaje);

        return Resultado.Ok($"Quitadas {plan.Quitadas.Count} pistas de «{Path.GetFileName(ruta)}».\n"
            + $"  {Comprimir.Peso(antes)} → {Comprimir.Peso(despues)} ({Comprimir.Variacion(antes, despues)})\n"
            + "  El vídeo está intacto: no se ha recodificado nada.\n"
            + "  El original está en la papelera del sistema, por si acaso.");
    }

    private static string Inventario(string ruta, IReadOnlyList<Pista> pistas)
    {
        var sb = new StringBuilder($"«{Path.GetFileName(ruta)}» tiene {pistas.Count} pistas:\n");
        foreach (var p in pistas) sb.AppendLine($"  {p.Indice}: {Linea(p)}");
        return sb.ToString();
    }

    private static string Linea(Pista p)
    {
        var partes = new List<string> { p.Tipo.ToString().ToLowerInvariant(), p.Codec };
        if (p.Idioma.Length > 0) partes.Add(p.Idioma);
        if (p.Canales is { } c) partes.Add($"{c} canales");
        if (p.BitsPorSegundo is { } bps and > 0) partes.Add($"{bps / 1000} kbps");
        if (p.Titulo.Length > 0) partes.Add($"«{p.Titulo}»");
        if (p.EsPredeterminada) partes.Add("por defecto");
        if (p.EsForzada) partes.Add("forzada");
        return string.Join(" · ", partes);
    }

    private static List<int> Numeros(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return [];
        if (v is JsonArray arr)
        {
            var salen = new List<int>();
            foreach (var x in arr)
            {
                if (x is null) continue;
                if (int.TryParse(x.ToString(), out var n)) salen.Add(n);
            }
            return salen;
        }
        return int.TryParse(v.ToString(), out var uno) ? [uno] : [];
    }

    private static List<string> Lista(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return [];
        if (v is JsonArray arr)
            return [.. arr.Where(x => x is not null).Select(x => x!.ToString().Trim()).Where(s => s.Length > 0)];
        var suelta = v.ToString().Trim();
        return suelta.Length > 0 ? [suelta] : [];
    }

    private static string? Texto(JsonObject a, string clave) =>
        a.TryGetPropertyValue(clave, out var v) && v is not null && v.ToString() is { Length: > 0 } s ? s : null;

    private static bool Bandera(JsonObject a, string clave, bool porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<bool>(); } catch { return porDefecto; }
    }
}
