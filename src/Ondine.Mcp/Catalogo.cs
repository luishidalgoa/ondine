using System.Text;
using System.Text.Json.Nodes;
using Ondine.Reindex;

namespace Ondine.Mcp;

/// <summary>
/// Las herramientas que expone el servidor.
///
/// <para>
/// Cada una corresponde a algo que una persona puede hacer en la aplicación, y por el mismo
/// camino: se llama a <c>Ondine.Core</c>, que es el motor que usan las dos interfaces. No hay
/// aquí ninguna regla nueva ni ningún atajo — si algo no se puede hacer en la ventana, tampoco
/// aquí.
/// </para>
/// <para>
/// <b>Las tres reglas que se mantienen</b>, y son las mismas que sostienen la app:
/// </para>
/// <list type="number">
/// <item>Analizar <b>propone</b>: lee y no escribe nada.</item>
/// <item>Lo que escribe pide <c>confirmar</c>. Sin él contesta lo que <i>haría</i>.</item>
/// <item>Lo borrado va a la <b>papelera del sistema</b>. No hay borrado de verdad.</item>
/// </list>
/// </summary>
internal static class Catalogo
{
    public static IReadOnlyList<Herramienta> Todas { get; } = Construir();

    private static List<Herramienta> Construir() =>
    [
        new("ondine_listar_videos",
            "Lista los vídeos de una carpeta, con su tamaño. No toca nada. Es el primer paso "
            + "para saber con qué se está trabajando.",
            Esquema(("carpeta", "string", "Ruta de la carpeta a mirar.", true),
                    ("subcarpetas", "boolean", "Si se recorren las subcarpetas. Por defecto sí.", false)),
            Escribe: false,
            ListarVideos),

        new("ondine_analizar",
            "Compara los vídeos de una carpeta con un catálogo de episodios y PROPONE un nombre "
            + "para cada uno. No renombra nada: devuelve, por fichero, en qué estado queda "
            + "(limpio, corregido, duda, sin identificar), con qué episodio ha casado y por qué. "
            + "Es el paso que hay que leer antes de aplicar.",
            Esquema(("carpeta", "string", "Carpeta con los vídeos.", true),
                    ("catalogo", "string", "Ruta del .json del catálogo de la serie.", true),
                    ("subcarpetas", "boolean", "Si se recorren las subcarpetas. Por defecto sí.", false),
                    ("plantilla", "string", "Patrón del nombre. Por defecto «<serie> - S<temp>E<num> - <título>».", false)),
            Escribe: false,
            Analizar),

        new("ondine_aplicar_renombrado",
            "Renombra los vídeos que el análisis dio por seguros. Los dudosos NO se tocan: hay "
            + "que resolverlos a mano en la aplicación. Sin «confirmar» dice lo que haría.",
            Esquema(("carpeta", "string", "Carpeta con los vídeos.", true),
                    ("catalogo", "string", "Ruta del .json del catálogo.", true),
                    ("subcarpetas", "boolean", "Si se recorren las subcarpetas. Por defecto sí.", false),
                    ("plantilla", "string", "Patrón del nombre.", false),
                    ("confirmar", "boolean", "Ponlo en true para renombrar de verdad.", false)),
            Escribe: true,
            AplicarRenombrado),

        new("ondine_partir",
            "Parte un vídeo en trozos, o se queda con un trozo, SIN recodificar: se copian los "
            + "flujos y el vídeo queda idéntico. Es para el fichero de 44 minutos que en realidad "
            + "son dos episodios pegados, o para quitar una intro. Dile «cortes» con los puntos "
            + "donde partir, o «desde»/«hasta» para un trozo suelto; en segundos o en «mm:ss». "
            + "Sin «confirmar» dice qué trozos saldrían y —esto importa— DÓNDE VA A CAER cada "
            + "corte de verdad: copiando, un corte solo puede caer en un fotograma clave, así que "
            + "se mueve un poco hacia atrás. Con «sin_recodificar»: false cae exacto, pero cuesta "
            + "como comprimir.",
            Esquema(("fichero", "string", "El vídeo a partir.", true),
                    ("cortes", "array", "Puntos donde partir: «21:47» o 1307. Uno por corte.", false),
                    ("desde", "string", "Para quedarte con un trozo: dónde empieza.", false),
                    ("hasta", "string", "Y dónde acaba. Por defecto, el final del vídeo.", false),
                    ("salida", "string", "Carpeta de destino. Por defecto, una «recortes» junto al original.", false),
                    ("sin_recodificar", "boolean", "Copiar los flujos (por defecto) o recodificar para que el corte caiga exacto.", false),
                    ("formato", "string", "Solo al recodificar: como en ondine_comprimir.", false),
                    ("codec", "string", "Solo al recodificar.", false),
                    ("codificador", "string", "Solo al recodificar.", false),
                    ("calidad", "integer", "Solo al recodificar.", false),
                    ("esmero", "string", "Solo al recodificar.", false),
                    ("confirmar", "boolean", "Ponlo en true para partirlo de verdad.", false)),
            Escribe: true,
            Partir.Ejecutar),

        new("ondine_quitar_pistas",
            "Quita doblajes y subtítulos que no quieres SIN recodificar el vídeo: se copian los "
            + "flujos que se quedan y se reempaqueta. Es el ahorro más barato que hay -cero "
            + "pérdida de calidad, segundos en vez de minutos- y conviene mirarlo antes de "
            + "recodificar una biblioteca. Llámalo sin «indices» ni «idiomas» para ver qué tiene "
            + "el fichero. El vídeo no se puede quitar. Sobrescribe el fichero, y el original va "
            + "a la papelera antes de tocarlo.",
            Esquema(("fichero", "string", "El vídeo.", true),
                    ("indices", "array", "Números de las pistas a quitar, los que devuelve esta misma herramienta.", false),
                    ("idiomas", "array", "Quita todas las pistas de audio y subtítulo de esos idiomas («por», «fra»…).", false),
                    ("confirmar", "boolean", "Ponlo en true para quitarlas de verdad.", false)),
            Escribe: true,
            QuitarPistas.Ejecutar),

        new("ondine_a_la_papelera",
            "Manda un fichero a la papelera del sistema. Nunca lo borra de verdad: se puede "
            + "recuperar desde el escritorio. Sin «confirmar» dice lo que haría.",
            Esquema(("ruta", "string", "El fichero.", true),
                    ("confirmar", "boolean", "Ponlo en true para mandarlo de verdad.", false)),
            Escribe: true,
            ALaPapelera),

        new("ondine_comprimir",
            "Comprime vídeos con TODOS los mandos de la pantalla de Comprimir: contenedor, "
            + "códec, calidad, esmero, resolución, tamaño objetivo, audio (códec, caudal y "
            + "canales), idiomas, subtítulos, aceleración por hardware y margen de disco. Los "
            + "originales no se tocan: el resultado va a otra carpeta. Sin «confirmar» devuelve "
            + "el pronóstico fichero a fichero, con lo que va a pesar cada uno. OJO: tarda lo "
            + "que tarde el vídeo -una temporada puede ser una hora- y la llamada no contesta "
            + "hasta el final; para ir por tandas, usa «limite».",
            Esquema(("carpeta", "string", "Carpeta con los vídeos. O usa «ficheros».", false),
                    ("ficheros", "array", "Rutas concretas, si no quieres una carpeta entera.", false),
                    ("subcarpetas", "boolean", "Si se recorren las subcarpetas. Por defecto sí.", false),
                    ("limite", "integer", "Como mucho, este número de vídeos. Para ir por tandas.", false),
                    ("salida", "string", "Carpeta de destino. Por defecto, una «comprimido» junto a cada original.", false),
                    ("formato", "string", "mkv (por defecto), mp4, webm, o solo audio: mp3, m4a, flac, opus.", false),
                    ("codec", "string", "hevc (por defecto), h264 o av1. Es el FORMATO de salida.", false),
                    ("preset", "string", "Parte de un preset por su nombre, como el desplegable de la app. Lo que pases además manda sobre él. Mira los que hay con ondine_presets.", false),
                    ("codificador", "string", "CON QUÉ codificar: «software» para el mejor por software, o un nombre (libx265, libsvtav1, hevc_nvenc…). Vacío = lo elige la app. Ojo: los codificadores de GPU son rápidos pero comprimen bastante menos; para archivar, «software».", false),
                    ("calidad", "integer", "CRF de 18 a 35: menos es mejor imagen y más peso. 0 = la elige la app.", false),
                    ("esmero", "string", "muy_rapido, rapido, equilibrado (por defecto), lento, muy_lento. Tiempo contra tamaño.", false),
                    ("alto", "integer", "Reescala si supera esa altura (1080, 720, 480). 0 = sin cambio.", false),
                    ("tamano_objetivo_mb", "integer", "Apunta a este tamaño por fichero. Manda sobre «calidad».", false),
                    ("audio_codec", "string", "copiar (por defecto), aac, ac3, eac3, opus, flac.", false),
                    ("audio_kbps", "integer", "Caudal del audio. Solo cuenta si se recodifica; puesto a solas, recodifica a AAC.", false),
                    ("audio_estereo", "boolean", "Baja a estéreo lo que traiga más canales.", false),
                    ("idioma", "string", "Idioma preferido: va primero y suena al abrir. Por defecto spa.", false),
                    ("idiomas", "array", "Idiomas de audio a conservar. «all» conserva todos, incluidos los sin etiqueta. Vacío = el preferido y eng.", false),
                    ("subtitulos", "array", "Idiomas de subtítulos a conservar. Vacío = todos.", false),
                    ("sin_subtitulos", "boolean", "Tirar todos los subtítulos.", false),
                    ("forzar", "boolean", "Rehacer aunque la salida ya exista o ya esté en un códec eficiente.", false),
                    ("hardware", "boolean", "Codificar con la GPU si hay. Por defecto sí.", false),
                    ("aceleracion", "string", "Decodificar por hardware: auto (por defecto), ninguna, cuda, qsv, vaapi, d3d11va, videotoolbox.", false),
                    ("margen_disco_mb", "integer", "Margen de disco por debajo del cual se pausa. Por defecto 200.", false),
                    ("tras_comprimir", "string", "Qué hacer con los originales: «papelera» o «conservar». Por defecto, lo que digan tus Preferencias.", false),
                    ("en_segundo_plano", "boolean", "Arranca y contesta al momento con un identificador, en vez de esperar a que acabe. Para tandas largas es lo que hay que usar: luego se pregunta con ondine_tanda.", false),
                    ("confirmar", "boolean", "Ponlo en true para comprimir de verdad.", false)),
            Escribe: true,
            Comprimir.Ejecutar),

        new("ondine_tanda",
            "Por dónde va una tanda que corre en segundo plano: qué fichero lleva, cuánto de ese "
            + "fichero, cuánto ha ahorrado hasta ahora, y si está en pausa esperando espacio en "
            + "disco. Cuando termina, devuelve el parte completo. Sin «id», la última.",
            Esquema(("id", "string", "El identificador que devolvió ondine_comprimir. Vacío = la última.", false)),
            Escribe: false,
            a => Tandas.Estado(Texto(a, "id"))),

        new("ondine_parar_tanda",
            "Para una tanda en marcha. Lo que ya estaba hecho se queda; el fichero a medias se "
            + "borra, igual que al cerrar la app a mitad. NO pide confirmar a propósito: pedir "
            + "permiso para un freno de mano es lo contrario de un freno de mano.",
            Esquema(("id", "string", "El identificador de la tanda. Vacío = la última.", false)),
            Escribe: false,
            a => Tandas.Parar(Texto(a, "id"))),

        new("ondine_presets",
            "Los presets de la app, con lo que pone cada uno: los de fábrica y los que hayas "
            + "guardado. Se aplican con «preset» en ondine_comprimir.",
            Esquema(),
            Escribe: false,
            Comprimir.Presets),

        new("ondine_previa",
            "Codifica DIEZ SEGUNDOS con los ajustes elegidos y deja el fichero en una ruta, para "
            + "que lo mire una persona antes de lanzar una tanda de una hora. No la mira el "
            + "agente: la ENSEÑA. Ojo: la previa codifica siempre lo más rápido posible, así que "
            + "no respeta el esmero y no dice nada del tiempo ni del tamaño finales — para eso "
            + "está ondine_medir.",
            Esquema(("fichero", "string", "El vídeo.", true),
                    ("desde", "string", "Desde qué momento: «12:30» o 750. Por defecto, el principio.", false),
                    ("salida", "string", "Dónde dejarla. Por defecto, un temporal del sistema.", false),
                    ("preset", "string", "Como en ondine_comprimir.", false),
                    ("formato", "string", "Como en ondine_comprimir.", false),
                    ("codec", "string", "Como en ondine_comprimir.", false),
                    ("codificador", "string", "Como en ondine_comprimir.", false),
                    ("calidad", "integer", "Como en ondine_comprimir.", false),
                    ("alto", "integer", "Como en ondine_comprimir.", false),
                    ("audio_codec", "string", "Como en ondine_comprimir.", false),
                    ("audio_kbps", "integer", "Como en ondine_comprimir.", false)),
            Escribe: false,
            Comprimir.Previa),

        new("ondine_pausar_tanda",
            "Pausa una tanda: SUSPENDE el ffmpeg en curso, así que la CPU queda libre y el "
            + "fichero a medias se queda a medias. Al seguir, continúa donde estaba sin "
            + "recodificar nada dos veces. Es el botón «Pausar» de la ventana.",
            Esquema(("id", "string", "La tanda. Vacío = la última.", false)),
            Escribe: false,
            a => Tandas.Pausar(Texto(a, "id"), true)),

        new("ondine_seguir_tanda",
            "Reanuda una tanda pausada, desde donde se quedó.",
            Esquema(("id", "string", "La tanda. Vacío = la última.", false)),
            Escribe: false,
            a => Tandas.Pausar(Texto(a, "id"), false)),

        new("ondine_medir",
            "Mide el tamaño REAL de un fichero con los ajustes que le des: codifica tres "
            + "muestras cortas y saca de ahí la cifra. Es el «Medir con una muestra» de la app, "
            + "y es lo que hay que usar antes de una tanda grande — el pronóstico de "
            + "«ondine_comprimir» sin confirmar es un modelo, esto es una medida. No escribe "
            + "nada: las muestras se tiran.",
            Esquema(("fichero", "string", "El vídeo a medir.", true),
                    ("formato", "string", "Como en ondine_comprimir.", false),
                    ("codec", "string", "Como en ondine_comprimir.", false),
                    ("codificador", "string", "Como en ondine_comprimir.", false),
                    ("calidad", "integer", "Como en ondine_comprimir.", false),
                    ("esmero", "string", "Como en ondine_comprimir.", false),
                    ("alto", "integer", "Como en ondine_comprimir.", false),
                    ("audio_codec", "string", "Como en ondine_comprimir.", false),
                    ("audio_kbps", "integer", "Como en ondine_comprimir.", false),
                    ("audio_estereo", "boolean", "Como en ondine_comprimir.", false),
                    ("hardware", "boolean", "Como en ondine_comprimir.", false),
                    ("aceleracion", "string", "Como en ondine_comprimir.", false)),
            Escribe: false,
            Comprimir.Medir),

        new("ondine_preferencias",
            "Lee las Preferencias de Ondine: idioma, preset por defecto, idioma de audio, qué "
            + "hacer con el original tras comprimir, margen de disco, aceleración por hardware "
            + "y los ajustes del modelo y de TMDb. De las claves solo dice si HAY una puesta: su "
            + "valor no sale de la máquina.",
            Esquema(),
            Escribe: false,
            Preferencias.Leer),

        new("ondine_ajustar_preferencias",
            "Cambia las Preferencias. Solo se toca lo que le pases: el resto se queda como "
            + "estaba, incluido lo que esta herramienta no ofrece. Las CLAVES del modelo y de "
            + "TMDb no se pueden poner desde aquí a propósito —habría que escribirlas en el "
            + "chat—: eso se hace en la ventana de Preferencias. Sin «confirmar» contesta el "
            + "antes y el después de cada cosa que cambiaría.",
            Esquema(("idioma_app", "string", "«es», «en», o vacío para el del sistema.", false),
                    ("preset_por_defecto", "string", "Preset que se aplica al abrir. Vacío = ninguno.", false),
                    ("idioma_audio", "string", "Idioma de audio preferido, en tres letras: spa, eng…", false),
                    ("subcarpetas", "boolean", "Analizar subcarpetas al añadir una carpeta.", false),
                    ("buscar_actualizaciones", "boolean", "Buscar versiones nuevas al arrancar.", false),
                    ("tras_comprimir", "string", "Qué hacer con el original: preguntar, papelera o conservar.", false),
                    ("margen_disco_mb", "integer", "Margen de disco (50 a 100000) por debajo del cual una tanda se pausa.", false),
                    ("hardware", "boolean", "Codificar con la GPU si la hay.", false),
                    ("codificador", "string", "Con qué codificar siempre: «software», un nombre, o vacío para que lo elija la app.", false),
                    ("aceleracion", "string", "Decodificar por hardware: auto, ninguna, o una que funcione en esta máquina.", false),
                    ("modelo_activo", "boolean", "Usar un modelo de lenguaje para los catálogos.", false),
                    ("modelo_url", "string", "URL del modelo, compatible con OpenAI.", false),
                    ("modelo_nombre", "string", "Nombre del modelo a pedir.", false),
                    ("peliculas_activo", "boolean", "Identificar películas con TMDb.", false),
                    ("confirmar", "boolean", "Ponlo en true para guardarlo de verdad.", false)),
            Escribe: true,
            Preferencias.Ajustar),

        new("ondine_donde_guarda",
            "Dice dónde guarda Ondine sus datos —catálogos, decisiones, ajustes— y qué "
            + "herramientas externas encuentra. Útil para saber si el entorno está listo antes "
            + "de intentar nada.",
            Esquema(),
            Escribe: false,
            _ => Resultado.Ok(Entorno())),
    ];

    // ── Las herramientas ─────────────────────────────────────────────────────

    private static Resultado ListarVideos(JsonObject a)
    {
        var carpeta = Texto(a, "carpeta");
        if (carpeta is null) return Resultado.Error("Falta «carpeta».");
        if (!Directory.Exists(carpeta)) return Resultado.Error($"No existe la carpeta: {carpeta}");

        var videos = Rutas.VideosQueLlegan.Expandir([carpeta], Bandera(a, "subcarpetas", true));
        if (videos.Count == 0) return Resultado.Ok($"No hay vídeos en {carpeta}.");

        var sb = new StringBuilder($"{videos.Count} vídeos en {carpeta}:\n");
        foreach (var v in videos)
        {
            long mb = 0;
            try { mb = new FileInfo(v).Length / 1048576; } catch { }
            sb.AppendLine($"  {Path.GetRelativePath(carpeta, v)}  ({mb} MB)");
        }
        return Resultado.Ok(sb.ToString());
    }

    private static Resultado Analizar(JsonObject a)
    {
        if (Planificar(a, out var error) is not { } plan) return Resultado.Error(error!);

        return Resultado.Ok(Contar(plan, "Análisis (no se ha tocado nada)"));
    }

    private static Resultado AplicarRenombrado(JsonObject a)
    {
        if (Planificar(a, out var error) is not { } plan) return Resultado.Error(error!);

        var seguros = plan.Where(p => p.Aplicable).ToList();
        if (seguros.Count == 0)
            return Resultado.Ok("No hay nada que aplicar: ninguna fila queda como segura.\n\n"
                              + Contar(plan, "Estado actual"));

        var queHaria = new StringBuilder($"{seguros.Count} ficheros se renombrarían:\n");
        foreach (var p in seguros)
            queHaria.AppendLine($"  {Path.GetFileName(p.Origen)}\n    → {Path.GetFileName(p.Destino!)}");

        var dudas = plan.Count - seguros.Count;
        if (dudas > 0)
            queHaria.AppendLine($"\nY {dudas} se quedan sin tocar: son dudas, y esas se resuelven "
                              + "en la aplicación, no aquí.");

        if (!Bandera(a, "confirmar", false)) return Resultado.Ensayo(queHaria.ToString());

        // A partir de aquí SÍ se toca el disco. La mudanza guarda su parte para poder deshacer,
        // igual que cuando la lanza la ventana: el agente no se salta el «deshacer».
        var parte = Mudanza.Aplicar(seguros.Select(p => (p.Origen, p.Destino!)));

        return Resultado.Ok($"Renombrados {parte.Movidos.Count} de {seguros.Count}."
            + (parte.Fallidos.Count > 0
                ? $"\nFallaron {parte.Fallidos.Count}:\n  " + string.Join("\n  ", parte.Fallidos)
                : "")
            // Los compañeros que se quedaron atrás se dicen, no se callan: el vídeo llega a su
            // sitio y el subtítulo se queda en el viejo, que para el servidor de medios es como
            // si no existiera. El motor los cuenta justamente para poder contarlo.
            + (parte.CompanerosSinMover.Count > 0
                ? $"\n\nY {parte.CompanerosSinMover.Count} acompañantes (subtítulos, carátulas) "
                  + "se quedaron donde estaban:\n  " + string.Join("\n  ", parte.CompanerosSinMover)
                : "")
            + "\n\nSe puede deshacer desde la aplicación, en Organizar.");
    }

    private static Resultado ALaPapelera(JsonObject a)
    {
        var ruta = Texto(a, "ruta");
        if (ruta is null) return Resultado.Error("Falta «ruta».");
        if (!File.Exists(ruta) && !Directory.Exists(ruta)) return Resultado.Error($"No existe: {ruta}");

        if (!Bandera(a, "confirmar", false))
            return Resultado.Ensayo($"Mandar a la papelera del sistema:\n  {ruta}");

        return PapeleraDelSistema.Mandar(ruta)
            ? Resultado.Ok($"En la papelera del sistema: {Path.GetFileName(ruta)}. "
                         + "Se puede recuperar desde el escritorio.")
            : Resultado.Error($"No se pudo mandar a la papelera: {ruta}. El fichero sigue donde estaba.");
    }

    // ── El plan, que es lo que comparten analizar y aplicar ──────────────────

    /// <summary>Una fila del plan: de dónde a dónde, y por qué.</summary>
    internal sealed record Fila(string Origen, string? Destino, string Estado, string Motivo, bool Aplicable);

    /// <summary>
    /// Analiza y devuelve el plan. Lo usan las DOS herramientas —la que propone y la que
    /// aplica— a propósito: si fueran dos caminos, aplicar podría hacer algo distinto de lo
    /// que se acaba de leer, que es la peor forma de romper la confianza.
    /// </summary>
    private static List<Fila>? Planificar(JsonObject a, out string? error)
    {
        error = null;
        var carpeta = Texto(a, "carpeta");
        var rutaCatalogo = Texto(a, "catalogo");

        if (carpeta is null) { error = "Falta «carpeta»."; return null; }
        if (rutaCatalogo is null) { error = "Falta «catalogo»."; return null; }
        if (!Directory.Exists(carpeta)) { error = $"No existe la carpeta: {carpeta}"; return null; }
        if (!File.Exists(rutaCatalogo)) { error = $"No existe el catálogo: {rutaCatalogo}"; return null; }

        ReindexCatalog catalogo;
        try { catalogo = ReindexCatalog.Load(rutaCatalogo); }
        catch (Exception ex) { error = "El catálogo no se puede leer: " + ex.Message; return null; }

        var videos = Rutas.VideosQueLlegan.Expandir([carpeta], Bandera(a, "subcarpetas", true));
        if (videos.Count == 0) { error = $"No hay vídeos en {carpeta}."; return null; }

        var señales = videos
            .Select(v => SignalExtractor.Extract(v, LibraryScan.Grupo(carpeta, v)))
            .ToList();

        var plantilla = new LibraryTemplate(Texto(a, "plantilla") ?? LibraryTemplate.PatronPorDefecto);

        return ReindexEngine.Resolve(señales, catalogo)
            .Select(r =>
            {
                var origen = r.Archivo.Path;
                string? destino = null;

                // Render devuelve el nombre CON su extension -para eso recibe el FileSignals-,
                // asi que aqui no se le anade nada. Se le anadia, y los destinos salian
                // «... - S1E1 - El primero.mkv.mkv»: compilaba, no daba error, y habria dejado
                // la biblioteca entera con dos extensiones. Se vio ejecutandolo.
                if (r.Episodio is { } ep && plantilla.Render(catalogo, ep, r.Archivo) is { } nombre)
                    destino = Path.Combine(Path.GetDirectoryName(origen)!, nombre);

                // Aplicable = lo que la aplicación aplicaría en bloque, y ni una fila más. La
                // regla vive en el motor (AplicableEnBloque) y aquí solo se consulta: si algún
                // día se endurece, esto se endurece con ella.
                var aplicable = r.AplicableEnBloque
                                && destino is not null
                                && !string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase);

                return new Fila(origen, destino, r.Estado.ToString(), r.Motivo, aplicable);
            })
            .ToList();
    }

    private static string Contar(List<Fila> plan, string titulo)
    {
        var sb = new StringBuilder($"{titulo} · {plan.Count} ficheros\n");

        foreach (var g in plan.GroupBy(p => p.Estado).OrderBy(g => g.Key))
            sb.AppendLine($"  {g.Key}: {g.Count()}");

        sb.AppendLine();
        foreach (var p in plan)
        {
            sb.AppendLine($"  [{p.Estado}] {Path.GetFileName(p.Origen)}");
            if (p.Destino is not null && p.Aplicable)
                sb.AppendLine($"      → {Path.GetFileName(p.Destino)}");
            if (p.Motivo.Length > 0) sb.AppendLine($"      {p.Motivo}");
        }

        var seguros = plan.Count(p => p.Aplicable);
        sb.AppendLine($"\n{seguros} se pueden renombrar sin preguntar; "
                    + $"{plan.Count - seguros} necesitan una decisión.");
        return sb.ToString();
    }

    private static string Entorno()
    {
        var sb = new StringBuilder("Ondine, por MCP\n");
        sb.AppendLine($"  versión del motor: {Updater.Current}");
        sb.AppendLine($"  datos del usuario: {DatosDeUsuario.Raiz}");
        sb.AppendLine($"  ffmpeg:  {Engine.FfmpegPath}");
        sb.AppendLine($"  ffprobe: {Engine.FfprobePath}");
        sb.AppendLine($"  extensiones que reconoce: {string.Join(" ", Engine.VideoExtensions)}");

        // Y lo que hay para acelerar, que es lo primero que hace falta saber antes de una tanda
        // larga. Se sondea de verdad -arrancando ffmpeg por candidata- y cuesta un par de
        // segundos la primera vez.
        try
        {
            var motor = new Engine();
            sb.AppendLine($"  codificador de vídeo: {motor.SelectEncoderAsync("hevc").GetAwaiter().GetResult()}");
            var acel = motor.AceleracionesDisponiblesAsync().GetAwaiter().GetResult();
            sb.AppendLine("  decodificación por hardware: "
                        + (acel.Count == 0 ? "ninguna en esta máquina" : string.Join(", ", acel)));
        }
        catch (Exception ex) { sb.AppendLine($"  (no se ha podido sondear el hardware: {ex.Message})"); }

        return sb.ToString();
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────

    private static string? Texto(JsonObject a, string clave) =>
        a.TryGetPropertyValue(clave, out var v) && v is not null
        && v.GetValue<object>() is not null && v.ToString() is { Length: > 0 } s ? s : null;

    private static bool Bandera(JsonObject a, string clave, bool porDefecto)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return porDefecto;
        try { return v.GetValue<bool>(); } catch { return porDefecto; }
    }

    /// <summary>
    /// El esquema de los argumentos, en JSON Schema, que es lo que el agente lee para saber
    /// cómo llamar.
    /// </summary>
    private static JsonObject Esquema(params (string Nombre, string Tipo, string Que, bool Obligatorio)[] campos)
    {
        var props = new JsonObject();
        var obligatorios = new JsonArray();

        foreach (var c in campos)
        {
            props[c.Nombre] = new JsonObject { ["type"] = c.Tipo, ["description"] = c.Que };
            if (c.Obligatorio) obligatorios.Add(c.Nombre);
        }

        var esquema = new JsonObject { ["type"] = "object", ["properties"] = props };
        if (obligatorios.Count > 0) esquema["required"] = obligatorios;
        return esquema;
    }
}
