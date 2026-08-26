using System.Text;
using System.Text.Json.Nodes;

namespace Ondine.Mcp;

/// <summary>
/// Las Preferencias de Ondine, leídas y cambiadas desde un agente.
///
/// <para>
/// <b>Las claves no salen de aquí ni entran.</b> Ni la del modelo ni la de TMDb: se guardan
/// cifradas con la protección de datos del usuario y esto solo dice si HAY una puesta o no.
/// Sacarlas convertiría cada conversación con un agente en una copia en claro de dos
/// credenciales, y meterlas obligaría a escribirlas en el chat para llegar hasta aquí. Quien
/// tenga que poner una clave, la pone en la ventana de Preferencias.
/// </para>
/// <para>
/// <b>Cambiar un ajuste es escribir</b>, así que pide <c>confirmar</c> como todo lo que escribe.
/// Sin él contesta el ANTES y el DESPUÉS de lo que se tocaría, que en un cambio de configuración
/// es lo único que deja darlo por bueno: una lista de valores nuevos, sin los viejos al lado, no
/// se puede juzgar.
/// </para>
/// </summary>
internal static class Preferencias
{
    /// <summary>
    /// Lo que se puede tocar desde aquí, y con qué nombre. Es el mismo reparto que la ventana de
    /// Preferencias, menos las claves.
    /// </summary>
    private static readonly (string Arg, string Que)[] Mandos =
    [
        ("idioma_app", "Idioma de la aplicación: «es», «en», o vacío para el del sistema"),
        ("preset_por_defecto", "Preset que se aplica al abrir. Vacío = ninguno"),
        ("idioma_audio", "Idioma de audio preferido, en tres letras («spa», «eng»…)"),
        ("subcarpetas", "Analizar subcarpetas al añadir una carpeta"),
        ("buscar_actualizaciones", "Buscar versiones nuevas al arrancar"),
        ("tras_comprimir", "Qué hacer con el original: «preguntar», «papelera» o «conservar»"),
        ("margen_disco_mb", "Margen de disco por debajo del cual una tanda se pausa"),
        ("hardware", "Codificar con la GPU si la hay"),
        ("aceleracion", "Decodificar por hardware: «auto», «ninguna», o el nombre de una"),
        ("modelo_activo", "Usar un modelo de lenguaje para los catálogos"),
        ("modelo_url", "URL del modelo, compatible con OpenAI"),
        ("modelo_nombre", "Nombre del modelo a pedir"),
        ("peliculas_activo", "Identificar películas con TMDb"),
    ];

    public static IEnumerable<(string Arg, string Que)> Argumentos => Mandos;

    // ── Leer ─────────────────────────────────────────────────────────────────

    public static Resultado Leer(JsonObject _)
    {
        var s = SettingsStore.Load();
        var sb = new StringBuilder("Preferencias de Ondine\n");

        sb.AppendLine("\n  GENERAL");
        sb.AppendLine($"    idioma_app: {(s.Idioma.Length == 0 ? "(el del sistema)" : s.Idioma)}");
        sb.AppendLine($"    preset_por_defecto: {(s.DefaultPreset.Length == 0 ? "(ninguno)" : s.DefaultPreset)}");
        sb.AppendLine($"    subcarpetas: {Si(s.Recurse)}");
        sb.AppendLine($"    buscar_actualizaciones: {Si(s.CheckUpdatesOnStart)}");

        sb.AppendLine("\n  AL COMPRIMIR");
        sb.AppendLine($"    idioma_audio: {s.DefaultLang}");
        sb.AppendLine($"    tras_comprimir: {Nombre(s.AfterCompress)}");

        sb.AppendLine("\n  RENDIMIENTO Y DISCO");
        sb.AppendLine($"    margen_disco_mb: {s.MinFreeMb}");
        sb.AppendLine($"    hardware: {Si(s.UseHardware)}");
        sb.AppendLine($"    aceleracion: {s.AceleracionVideo}");

        sb.AppendLine("\n  MODELO");
        sb.AppendLine($"    modelo_activo: {Si(s.Ia.Activo)}");
        sb.AppendLine($"    modelo_url: {(s.Ia.BaseUrl.Length == 0 ? "(sin poner)" : s.Ia.BaseUrl)}");
        sb.AppendLine($"    modelo_nombre: {(s.Ia.Modelo.Length == 0 ? "(sin poner)" : s.Ia.Modelo)}");
        // La clave, solo si la hay. Nunca su valor, ni cifrado.
        sb.AppendLine($"    clave: {(s.Ia.TieneClave ? "puesta" : "sin poner")} (no se lee ni se escribe desde aquí)");

        sb.AppendLine("\n  PELÍCULAS");
        sb.AppendLine($"    peliculas_activo: {Si(s.Tmdb.Activo)}");
        sb.AppendLine($"    clave: {(s.Tmdb.ClaveCifrada.Length > 0 ? "puesta" : "sin poner")} (igual que la otra)");

        sb.Append("\nPara cambiar algo: ondine_ajustar_preferencias. Las claves, en la ventana de "
                + "Preferencias de la app: aquí no entran ni salen.");
        return Resultado.Ok(sb.ToString());
    }

    // ── Cambiar ──────────────────────────────────────────────────────────────

    public static Resultado Ajustar(JsonObject a)
    {
        // Se parte de lo que hay y se pisa solo lo que venga, igual que hace la ventana. Un
        // Settings nuevo se llevaría por delante el historial de renombrado y el factor de
        // complejidad que la app aprende midiendo — ya costó una vez.
        var antes = SettingsStore.Load();
        var despues = antes.Clone();
        var cambios = new List<string>();

        string? error = null;

        Texto(a, "idioma_app", antes.Idioma, v =>
        {
            if (v is not ("" or "es" or "en")) { error = "«idioma_app» solo puede ser «es», «en» o vacío."; return; }
            despues.Idioma = v;
        }, cambios);

        Texto(a, "preset_por_defecto", antes.DefaultPreset, v => despues.DefaultPreset = v, cambios);
        Texto(a, "idioma_audio", antes.DefaultLang, v => despues.DefaultLang = v.Trim(), cambios);
        Bandera(a, "subcarpetas", antes.Recurse, v => despues.Recurse = v, cambios);
        Bandera(a, "buscar_actualizaciones", antes.CheckUpdatesOnStart, v => despues.CheckUpdatesOnStart = v, cambios);

        Texto(a, "tras_comprimir", Nombre(antes.AfterCompress), v =>
        {
            var elegido = v.ToLowerInvariant() switch
            {
                "preguntar" => AfterCompress.Ask,
                "papelera" => AfterCompress.RecycleOriginal,
                "conservar" => AfterCompress.Keep,
                _ => (AfterCompress?)null,
            };
            if (elegido is null) { error = "«tras_comprimir» solo puede ser «preguntar», «papelera» o «conservar»."; return; }
            despues.AfterCompress = elegido.Value;
        }, cambios);

        Entero(a, "margen_disco_mb", antes.MinFreeMb, v =>
        {
            if (v < 50 || v > 100_000) { error = "«margen_disco_mb» va de 50 a 100000."; return; }
            despues.MinFreeMb = v;
        }, cambios);

        Bandera(a, "hardware", antes.UseHardware, v => despues.UseHardware = v, cambios);

        Texto(a, "aceleracion", antes.AceleracionVideo, v =>
        {
            // Se comprueba contra las que ARRANCAN en esta máquina, no contra una lista escrita:
            // guardar «cuda» en un portátil sin NVIDIA deja un ajuste que no hace nada y que el
            // usuario ve puesto en Preferencias. Cuesta un par de segundos la primera vez.
            var hay = new List<string> { Objetivo.AceleracionDeVideo.Auto, Objetivo.AceleracionDeVideo.Ninguna };
            try { hay.AddRange(new Engine().AceleracionesDisponiblesAsync().GetAwaiter().GetResult()); }
            catch { /* sin sonda, se acepta lo que venga: el motor ya cae a la automática */ }

            if (hay.Count > 2 && !hay.Contains(v, StringComparer.OrdinalIgnoreCase))
            {
                error = $"«aceleracion» no puede ser «{v}» en esta máquina. Las que funcionan aquí: "
                      + string.Join(", ", hay) + ".";
                return;
            }
            despues.AceleracionVideo = v;
        }, cambios);

        Bandera(a, "modelo_activo", antes.Ia.Activo, v => despues.Ia.Activo = v, cambios);
        Texto(a, "modelo_url", antes.Ia.BaseUrl, v => despues.Ia.BaseUrl = v.Trim(), cambios);
        Texto(a, "modelo_nombre", antes.Ia.Modelo, v => despues.Ia.Modelo = v.Trim(), cambios);
        Bandera(a, "peliculas_activo", antes.Tmdb.Activo, v => despues.Tmdb.Activo = v, cambios);

        if (error is not null) return Resultado.Error(error);

        // Lo que se pidió y no se reconoce: se dice, en vez de guardar a medias sin avisar.
        var conocidos = Mandos.Select(m => m.Arg).Append("confirmar").ToHashSet();
        var raros = a.Select(p => p.Key).Where(k => !conocidos.Contains(k)).ToList();
        if (raros.Count > 0)
            return Resultado.Error($"No entiendo: {string.Join(", ", raros)}. Lo que hay: "
                                 + string.Join(", ", Mandos.Select(m => m.Arg)) + ".");

        // Sin un solo ajuste reconocido, esto no es «no hay nada que cambiar»: es que no se ha
        // pedido nada. Contestar «ok» a una llamada vacía deja al agente creyendo que ha
        // configurado algo.
        var pedidos = a.Select(x => x.Key).Where(k => k != "confirmar").ToList();
        if (pedidos.Count == 0)
            return Resultado.Error("No has pedido ningún cambio. Lo que se puede tocar: "
                                 + string.Join(", ", Mandos.Select(m => m.Arg))
                                 + ". Para ver cómo está ahora: ondine_preferencias.");

        if (cambios.Count == 0)
            return Resultado.Ok("No hay nada que cambiar: lo que has pedido ya está así.");

        var lista = string.Join("\n", cambios.Select(c => "  " + c));
        if (!Bandera(a, "confirmar", false))
            return Resultado.Ensayo(lista);

        SettingsStore.Save(despues);

        var sb = new StringBuilder("Guardado:\n" + lista);
        sb.Append("\n\nSi tienes Ondine abierto, al guardar desde su ventana de Preferencias se "
                + "pisará esto: la ventana escribe lo que tiene a la vista.");
        return Resultado.Ok(sb.ToString());
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────

    private static string Si(bool b) => b ? "sí" : "no";

    private static string Nombre(AfterCompress a) => a switch
    {
        AfterCompress.RecycleOriginal => "papelera",
        AfterCompress.Keep => "conservar",
        _ => "preguntar",
    };

    /// <summary>
    /// Lee un argumento de texto, lo aplica y apunta el cambio con el antes y el después. Si no
    /// viene, no se toca nada: <b>lo que no se pide no se cambia</b>, que es lo que hace que se
    /// pueda tocar un solo ajuste sin arrastrar los demás.
    /// </summary>
    private static void Texto(JsonObject a, string clave, string ahora, Action<string> poner, List<string> cambios)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return;
        var nuevo = v.ToString();
        poner(nuevo);
        if (nuevo != ahora) cambios.Add($"{clave}: {Vacio(ahora)} → {Vacio(nuevo)}");
    }

    private static void Bandera(JsonObject a, string clave, bool ahora, Action<bool> poner, List<string> cambios)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return;
        bool nuevo;
        try { nuevo = v.GetValue<bool>(); } catch { return; }
        poner(nuevo);
        if (nuevo != ahora) cambios.Add($"{clave}: {Si(ahora)} → {Si(nuevo)}");
    }

    private static void Entero(JsonObject a, string clave, int ahora, Action<int> poner, List<string> cambios)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return;
        int nuevo;
        try { nuevo = v.GetValue<int>(); } catch { if (!int.TryParse(v.ToString(), out nuevo)) return; }
        poner(nuevo);
        if (nuevo != ahora) cambios.Add($"{clave}: {ahora} → {nuevo}");
    }

    private static bool Bandera(JsonObject a, string clave, bool porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<bool>(); } catch { return porDefecto; }
    }

    private static string Vacio(string s) => s.Length == 0 ? "(vacío)" : s;
}
