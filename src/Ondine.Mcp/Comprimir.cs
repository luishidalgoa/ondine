using System.Text;
using System.Text.Json.Nodes;
using Ondine.Audio;
using Ondine.Objetivo;

namespace Ondine.Mcp;

/// <summary>
/// Comprimir desde un agente, con los mismos mandos que tiene la ventana.
///
/// <para>
/// <b>Todos, y eso es el requisito.</b> Media docena de opciones habrían dado una herramienta
/// que sirve para la mitad de los casos y obliga a abrir la app para la otra mitad, que es
/// justo lo que un servidor MCP viene a evitar. Lo que se puede elegir en la pantalla de
/// Comprimir se puede pedir aquí: contenedor, códec, calidad, esmero, resolución, tamaño
/// objetivo, audio (códec, caudal y canales), idiomas, subtítulos, aceleración por hardware y
/// margen de disco. La lista la vigila una prueba contra <see cref="EncodeOptions"/>, para que
/// un mando nuevo en la ventana no se quede fuera de aquí sin que nadie se entere.
/// </para>
/// <para>
/// <b>Lo que no está, y por qué.</b> La vista previa de diez segundos no tiene sentido sin
/// alguien mirándola. La cola tampoco: aquí el agente encadena llamadas, que es su forma de
/// hacer cola. Y pausar o reanudar a media tanda pide una conversación que MCP no tiene.
/// </para>
/// <para>
/// <b>Esto tarda lo que tarde el vídeo.</b> Una temporada entera puede ser una hora larga, y la
/// llamada no contesta hasta el final. Por eso está <c>limite</c>: el agente va por tandas, ve
/// lo que salió y sigue.
/// </para>
/// </summary>
internal static class Comprimir
{
    // El reportero vive en Tandas: es el mismo para la tanda en segundo plano y para la que
    // espera, y tenerlo dos veces habría acabado con dos versiones que cuentan cosas distintas.

    // ── Las opciones ─────────────────────────────────────────────────────────

    private static readonly string[] Formatos = ["mkv", "mp4", "webm", "mp3", "m4a", "flac", "opus"];
    private static readonly string[] Codecs = ["hevc", "h264", "av1"];

    private static readonly Dictionary<string, Velocidad> Esmeros = new(StringComparer.OrdinalIgnoreCase)
    {
        ["muy_rapido"] = Velocidad.MuyRapido,
        ["rapido"] = Velocidad.Rapido,
        ["equilibrado"] = Velocidad.Equilibrado,
        ["lento"] = Velocidad.Lento,
        ["muy_lento"] = Velocidad.MuyLento,
    };

    private static readonly Dictionary<string, AudioElegido> CodecsDeAudio = new(StringComparer.OrdinalIgnoreCase)
    {
        ["copiar"] = AudioElegido.Copiar,
        ["aac"] = AudioElegido.Aac,
        ["ac3"] = AudioElegido.Ac3,
        ["eac3"] = AudioElegido.Eac3,
        ["opus"] = AudioElegido.Opus,
        ["flac"] = AudioElegido.Flac,
    };

    /// <summary>
    /// Los presets, con lo que pone cada uno. Son los mismos que el desplegable de la ventana:
    /// los de fábrica más los que haya guardado el usuario.
    /// </summary>
    public static Resultado Presets(JsonObject _)
    {
        var todos = PresetStore.Factory().Concat(PresetStore.LoadUser()).ToList();
        var sb = new StringBuilder($"{todos.Count} presets:\n");

        foreach (var p in todos)
        {
            var opt = DeUnPreset(p);
            sb.AppendLine($"\n  «{p.Name}»{(p.Factory ? "  (de fábrica)" : "")}");
            sb.Append(ComoQueda(opt));
            sb.AppendLine();
        }

        sb.Append("\nSe aplican con «preset»: «\"preset\": \"Ligero para móvil (720p)\"». Lo que "
                + "pongas además manda sobre lo que diga el preset.");
        return Resultado.Ok(sb.ToString());
    }

    /// <summary>Las opciones que pone un preset, que son índices de desplegable.</summary>
    private static EncodeOptions DeUnPreset(Preset p)
    {
        var opt = new EncodeOptions
        {
            VideoCodec = p.Codec switch { 1 => "h264", 2 => "av1", _ => "hevc" },
            Quality = p.Quality switch { 1 => 22, 2 => 24, 3 => 27, 4 => 30, _ => 0 },
            MaxHeight = p.Res switch { 1 => 1080, 2 => 720, 3 => 480, _ => 0 },
            AudioBitrate = p.Audio switch { 1 => 192, 2 => 160, 3 => 128, 4 => 96, _ => 0 },
            AudioCodec = p.ACodec switch
            {
                1 => AudioElegido.Aac, 2 => AudioElegido.Ac3, 3 => AudioElegido.Eac3,
                4 => AudioElegido.Opus, 5 => AudioElegido.Flac, _ => AudioElegido.Copiar,
            },
            Lang = string.IsNullOrWhiteSpace(p.Lang) ? "spa" : p.Lang,
        };
        Ondine.OpcionesSalida.PonerFormato(opt, p.Fmt);
        return opt;
    }

    /// <summary>
    /// Traduce los argumentos a las opciones del motor, y <b>rechaza lo que no entiende</b> en
    /// vez de caer en un valor por defecto.
    ///
    /// <para>
    /// Un «codec: h265» que se convierte en HEVC por su cuenta parece amable y no lo es: el
    /// agente cree que se le ha hecho caso, y la próxima vez pide «h266». Decirle qué valores
    /// hay le deja arreglarlo en la misma conversación.
    /// </para>
    /// </summary>
    internal static EncodeOptions? Opciones(JsonObject a, out string? error)
    {
        error = null;

        // ── El preset primero, si lo hay: es el punto de partida ─────────────
        // Y lo que se pase además manda sobre él, que es lo que hace la ventana: aplicas un
        // preset y luego tocas lo que quieras sin que se te deshaga.
        var preset = Texto(a, "preset");
        EncodeOptions opt;

        if (preset is not null)
        {
            var todos = PresetStore.Factory().Concat(PresetStore.LoadUser()).ToList();
            var suyo = todos.FirstOrDefault(p => string.Equals(p.Name, preset, StringComparison.OrdinalIgnoreCase));
            if (suyo is null)
            {
                error = $"No hay ningún preset «{preset}». Los que hay: "
                      + string.Join(", ", todos.Select(p => $"«{p.Name}»")) + ".";
                return null;
            }
            opt = DeUnPreset(suyo);
        }
        else opt = new EncodeOptions();

        var formato = (Texto(a, "formato") ?? (opt.AudioOnly ? opt.AudioFormat : opt.Container)).ToLowerInvariant();
        if (!Formatos.Contains(formato))
        {
            error = $"«formato» no puede ser «{formato}». Los que hay: {string.Join(", ", Formatos)}.";
            return null;
        }
        if (formato is "mkv" or "mp4" or "webm") opt.Container = formato;
        else { opt.AudioOnly = true; opt.AudioFormat = formato; }

        var codec = (Texto(a, "codec") ?? opt.VideoCodec).ToLowerInvariant();
        if (!Codecs.Contains(codec))
        {
            error = $"«codec» no puede ser «{codec}». Los que hay: {string.Join(", ", Codecs)}.";
            return null;
        }
        opt.VideoCodec = codec;

        var esmero = Texto(a, "esmero");
        if (esmero is not null)
        {
            if (!Esmeros.TryGetValue(esmero, out var v))
            {
                error = $"«esmero» no puede ser «{esmero}». Los que hay: {string.Join(", ", Esmeros.Keys)}.";
                return null;
            }
            opt.Velocidad = v;
        }

        var audioCodec = Texto(a, "audio_codec");
        var audioDelPreset = opt.AudioCodec;
        if (audioCodec is not null)
        {
            if (!CodecsDeAudio.TryGetValue(audioCodec, out var c))
            {
                error = $"«audio_codec» no puede ser «{audioCodec}». Los que hay: "
                      + string.Join(", ", CodecsDeAudio.Keys) + ".";
                return null;
            }
            opt.AudioCodec = c;
        }

        // El codificador por su NOMBRE, que es otra cosa que el códec: el códec es qué formato
        // sale y esto es con qué se hace. No se valida contra una lista escrita porque los
        // nombres los pone ffmpeg; lo que no exista o no arranque se resuelve como automático al
        // usarlo, y el registro dice cuál se ha usado de verdad.
        opt.Codificador = Texto(a, "codificador") ?? "";

        opt.Quality = Entero(a, "calidad", opt.Quality);
        if (opt.Quality != 0 && (opt.Quality < 18 || opt.Quality > 35))
        {
            error = "«calidad» va de 18 a 35, o 0 para que la elija la app. Menos es mejor imagen y más peso.";
            return null;
        }

        opt.MaxHeight = Entero(a, "alto", opt.MaxHeight);
        opt.AudioBitrate = Entero(a, "audio_kbps", opt.AudioBitrate);
        opt.TamanoObjetivoBytes = (long)Entero(a, "tamano_objetivo_mb", 0) * 1024 * 1024;

        // El caudal de audio SIN códec elegido significa recodificar a AAC, igual que en
        // Recortes y en la línea de órdenes: aquí tampoco hay dos desplegables que desempatar.
        if (opt.AudioBitrate > 0 && audioCodec is null && audioDelPreset == AudioElegido.Copiar)
            opt.AudioCodec = AudioElegido.Aac;

        if (Bandera(a, "audio_estereo", false)) opt.AudioMezcla = Mezcla.Estereo;

        // LO GUARDADO MANDA cuando no se pide otra cosa. Antes esto tenía sus propios valores
        // por defecto -«spa», hardware sí, aceleración automática- y por tanto un agente hacía
        // algo distinto de lo que hace la ventana con las mismas Preferencias delante. Quien
        // configuró la app una vez espera que se le haga caso por los dos caminos.
        var guardadas = SettingsStore.Load();

        opt.Lang = Texto(a, "idioma") ?? (preset is not null ? opt.Lang : guardadas.DefaultLang);
        opt.KeepLangs = Lista(a, "idiomas") ?? [];
        opt.SubLangs = Lista(a, "subtitulos");
        opt.NoSubs = Bandera(a, "sin_subtitulos", false);
        opt.Force = Bandera(a, "forzar", false);

        var salida = Texto(a, "salida");
        if (salida is not null) opt.Output = Path.GetFullPath(salida);

        return opt;
    }

    /// <summary>
    /// Los ajustes que no viven en <see cref="EncodeOptions"/> sino en el motor: la aceleración
    /// y el margen de disco. Son estáticos porque en la app los pone Preferencias una vez.
    /// </summary>
    private static string? Ajustes(JsonObject a)
    {
        // Igual que arriba: lo guardado en Preferencias es el punto de partida, y el argumento
        // de la llamada lo pisa solo si viene.
        var guardadas = SettingsStore.Load();

        Engine.AllowHardware = Bandera(a, "hardware", guardadas.UseHardware);
        Engine.AceleracionPedida = Texto(a, "aceleracion") ?? guardadas.AceleracionVideo;
        Engine.MinFreeBytes = Entero(a, "margen_disco_mb", guardadas.MinFreeMb) * 1024L * 1024;

        return null;
    }

    // ── Comprimir ────────────────────────────────────────────────────────────

    public static Resultado Ejecutar(JsonObject a)
    {
        var videos = Entrada(a, out var error);
        if (error is not null) return Resultado.Error(error);

        var opt = Opciones(a, out error);
        if (opt is null) return Resultado.Error(error!);
        Ajustes(a);

        // Qué hacer con los originales al terminar. Esto lo aplicaba SOLO la ventana, así que
        // por MCP el ajuste de Preferencias no hacía nada: comprimías una temporada y los
        // originales se quedaban, con el disco igual de lleno que antes. Se pide o se hereda.
        var tras = (Texto(a, "tras_comprimir") ?? "").ToLowerInvariant();
        var aLaPapelera = tras switch
        {
            "papelera" => true,
            "conservar" => false,
            "" => SettingsStore.Load().AfterCompress == AfterCompress.RecycleOriginal,
            _ => (bool?)null,
        };
        if (aLaPapelera is null)
            return Resultado.Error($"«tras_comprimir» no puede ser «{tras}»: «papelera» o «conservar». "
                                 + "«preguntar» no vale aquí, que no hay a quién preguntar.");

        var limite = Entero(a, "limite", 0);
        if (limite > 0 && videos!.Count > limite) videos = [.. videos.Take(limite)];

        var motor = new Engine();

        // ── Sin permiso: el pronóstico, no una negativa ──────────────────────
        if (!Bandera(a, "confirmar", false))
            return Resultado.Ensayo(Pronostico(motor, videos!, opt, limite, aLaPapelera.Value));

        // ── En segundo plano: se arranca y se contesta al momento ────────────
        if (Bandera(a, "en_segundo_plano", false))
        {
            var id = Tandas.Arrancar(motor, videos!, opt,
                (hechos, _) => { if (aLaPapelera.Value) ALaPapelera(hechos); });

            return Resultado.Ok($"Tanda {id} en marcha, con {videos!.Count} vídeos.\n\n"
                + ComoQueda(opt) + "\n"
                + (aLaPapelera.Value ? "  Los originales irán a la papelera al terminar cada uno.\n" : "")
                + "\nPregunta por ella con ondine_tanda cuando quieras, y párala con "
                + "ondine_parar_tanda. Ojo: vive en este servidor, así que si se cierra el "
                + "cliente se va con él.");
        }

        var cuaderno = new Tandas.Cuaderno();
        List<FileResult> hechos;
        try
        {
            hechos = motor.CompressAsync(videos!, opt, cuaderno, CancellationToken.None)
                          .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return Resultado.Error($"La tanda se ha parado: {ex.Message}\n\n"
                                 + string.Join("\n", cuaderno.Lineas.TakeLast(12)));
        }

        if (aLaPapelera.Value) ALaPapelera(hechos);

        return Resultado.Ok(Parte(hechos, cuaderno, videos!.Count));
    }

    /// <summary>
    /// Los originales de lo que salió bien, a la papelera del sistema.
    ///
    /// <para>
    /// A la papelera y nunca borrados: es la regla de la casa y aquí importa más que en ningún
    /// otro sitio, porque lo que se está tirando es el original de un vídeo que acaba de
    /// recodificarse. Si el resultado no gusta, se recupera del escritorio.
    /// </para>
    /// <para>
    /// Y solo los que TERMINARON con un fichero de salida en el disco. Mandar a la papelera el
    /// original de algo que falló sería cambiar un fallo por una pérdida.
    /// </para>
    /// </summary>
    private static void ALaPapelera(List<FileResult> hechos)
    {
        foreach (var r in hechos.Where(x => x.OutBytes is > 0
                                         && x.SourcePath.Length > 0
                                         && File.Exists(x.OutputPath)))
            try { PapeleraDelSistema.Mandar(r.SourcePath); } catch { /* se queda, y no pasa nada */ }
    }

    /// <summary>
    /// Lo que va a pasar, fichero a fichero, sin tocar nada: cuánto pesa hoy, cuánto se prevé
    /// que pese y qué se le va a hacer.
    ///
    /// <para>
    /// Es el pronóstico del panel de estimación de la app, el mismo <see cref="Estimator"/>, y
    /// sale de sondear cada fichero con ffprobe. No codifica nada: para eso está
    /// <c>ondine_medir</c>, que sí codifica muestras y tarda.
    /// </para>
    /// </summary>
    private static string Pronostico(Engine motor, IReadOnlyList<string> videos, EncodeOptions opt,
                                     int limite, bool papelera)
    {
        var sb = new StringBuilder($"{videos.Count} vídeos. Esto es lo que haría:\n\n");
        sb.AppendLine(ComoQueda(opt));
        if (papelera) sb.AppendLine("  Y los originales irían a la papelera del sistema al terminar.");
        sb.AppendLine();

        long entra = 0, sale = 0;
        int sondeados = 0;

        foreach (var v in videos.Take(40))   // el pronóstico de cuarenta ya dice la tendencia
        {
            var fila = FilaDe(motor, v);
            if (fila is null) { sb.AppendLine($"  {Path.GetFileName(v)}  (no se ha podido sondear)"); continue; }

            var e = Estimator.Compute(fila, opt);
            entra += fila.Bytes;
            sale += e.Valid ? e.EstBytes : fila.Bytes;
            sondeados++;

            sb.AppendLine($"  {Path.GetFileName(v)}"
                        + $"\n      {Peso(fila.Bytes)} → ≈ {Peso(e.EstBytes)}"
                        + (e.Valid ? $"  (≈ {Variacion(fila.Bytes, e.EstBytes)})" : "  (sin pronóstico: falta sondeo)"));
        }

        if (videos.Count > 40) sb.AppendLine($"  … y {videos.Count - 40} más.");

        if (sondeados > 0 && entra > 0)
            sb.AppendLine($"\nEn total: {Peso(entra)} → ≈ {Peso(sale)} (≈ {Variacion(entra, sale)}).");

        sb.AppendLine("\nEs una estimación por modelo, no una medida. «ondine_medir» codifica "
                    + "muestras de verdad y da la cifra real de un fichero.");
        if (limite > 0) sb.AppendLine($"Y se ha aplicado «limite»: {videos.Count} de la tanda.");

        return sb.ToString();
    }

    /// <summary>El resumen de lo elegido, en una línea por cosa. Es lo que se va a aplicar.</summary>
    private static string ComoQueda(EncodeOptions opt)
    {
        var sb = new StringBuilder();
        if (opt.AudioOnly)
            sb.AppendLine($"  Solo audio: {opt.AudioFormat.ToUpperInvariant()}"
                        + (opt.AudioBitrate > 0 ? $" a {opt.AudioBitrate} kbps" : ""));
        else
        {
            sb.AppendLine($"  Contenedor: {opt.Container} · códec: {opt.VideoCodec}"
                        + $" · calidad: {(opt.Quality > 0 ? opt.Quality.ToString() : "automática")}"
                        + $" · esmero: {opt.Velocidad}");
            sb.AppendLine($"  Resolución: {(opt.MaxHeight > 0 ? opt.MaxHeight + "p como mucho" : "sin cambio")}"
                        + (opt.TamanoObjetivoBytes > 0
                            ? $" · tamaño objetivo: {opt.TamanoObjetivoBytes / 1048576} MB"
                            : ""));
            sb.AppendLine($"  Audio: {opt.AudioCodec}"
                        + (opt.AudioBitrate > 0 ? $" a {opt.AudioBitrate} kbps" : " (caudal automático)")
                        + (opt.AudioMezcla == Mezcla.Estereo ? " · bajado a estéreo" : ""));
        }

        var idiomas = opt.KeepLangs.Count > 0
            ? string.Join("+", opt.KeepLangs)
            : $"{opt.Lang}+eng (lo que pone la app cuando no eliges)";
        sb.AppendLine($"  Idiomas de audio: {idiomas} · preferido: {opt.Lang}");
        sb.AppendLine($"  Subtítulos: {(opt.NoSubs ? "ninguno" : opt.SubLangs is { Count: > 0 } s ? string.Join("+", s) : "todos")}");
        sb.Append($"  Salida: {opt.Output ?? "una carpeta «comprimido» junto a cada original"}");
        if (opt.Force) sb.Append(" · rehaciendo lo que ya exista");
        return sb.ToString();
    }

    /// <summary>El parte de la tanda: qué salió, cuánto se ahorró y qué se quedó fuera.</summary>
    internal static string Parte(List<FileResult> hechos, Tandas.Cuaderno cuaderno, int pedidos)
    {
        var buenos = hechos.Where(r => r.OutBytes is > 0).ToList();
        var malos = hechos.Where(r => r.OutBytes is null or 0).ToList();

        long entra = buenos.Sum(r => r.InBytes), sale = buenos.Sum(r => r.OutBytes ?? 0);
        var sb = new StringBuilder($"Hechos {buenos.Count} de {pedidos}.");
        if (entra > 0) sb.Append($" {Peso(entra)} → {Peso(sale)} ({Variacion(entra, sale)}).");
        sb.AppendLine();

        foreach (var r in buenos)
            sb.AppendLine($"  ✓ {r.Name}  {Peso(r.InBytes)} → {Peso(r.OutBytes ?? 0)}  ({Variacion(r.InBytes, r.OutBytes ?? 0)})"
                        + (r.SubtitleWarning is { Length: > 0 } av ? $"\n      aviso: {av}" : ""));

        foreach (var r in malos)
            sb.AppendLine($"  ✗ {r.Name}: {r.Detalle ?? r.Status}");

        foreach (var (ruta, motivo) in cuaderno.Saltados)
            sb.AppendLine($"  – {Path.GetFileName(ruta)}: {motivo}");

        // Las líneas del motor que explican la tanda entera -codificador, quién decodifica, si
        // la aceleración se cayó- van al final: son pocas y son las que contestan «por qué».
        var deLaTanda = cuaderno.Lineas
            .Where(l => l.Contains("odificador", StringComparison.OrdinalIgnoreCase)
                     || l.Contains("ecodifica", StringComparison.OrdinalIgnoreCase)
                     || l.Contains("aceleraci", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
        if (deLaTanda.Count > 0)
            sb.AppendLine("\n" + string.Join("\n", deLaTanda.Select(l => "  · " + l)));

        sb.Append("\nLos originales siguen donde estaban: Ondine nunca los toca.");
        return sb.ToString();
    }

    // ── Previa ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Diez segundos codificados con los ajustes elegidos, a un fichero, para que los mire una
    /// persona.
    ///
    /// <para>
    /// Dije que esto no tenía sentido por MCP porque no hay nadie mirando, y estaba mirando mal:
    /// el agente no la mira, pero PUEDE ENSEÑARLA. «Te dejo diez segundos en esta ruta, ábrelos
    /// antes de que lance la temporada» es exactamente lo que hace falta antes de una hora de
    /// máquina.
    /// </para>
    /// <para>
    /// Y hereda el aviso de la app: la previa codifica siempre lo más rápido posible, así que NO
    /// respeta el esmero y no dice nada del tiempo ni del tamaño finales. Enseña cómo va a quedar
    /// la imagen, y nunca mejor que el resultado real.
    /// </para>
    /// </summary>
    public static Resultado Previa(JsonObject a)
    {
        var ruta = Texto(a, "fichero");
        if (ruta is null) return Resultado.Error("Falta «fichero».");
        if (!File.Exists(ruta)) return Resultado.Error($"No existe: {ruta}");

        var opt = Opciones(a, out var error);
        if (opt is null) return Resultado.Error(error!);
        Ajustes(a);

        var desde = (int)(Partir.Momento(a, "desde") ?? 0);
        var destino = Texto(a, "salida")
            ?? Path.Combine(Path.GetTempPath(), $"ondine-previa-{Guid.NewGuid():N}"[..24]
                            + Engine.OutputExtension(opt));

        var motor = new Engine();
        string? salio;
        try
        {
            salio = motor.PreviewAsync(ruta, opt, desde, destino, new Tandas.Cuaderno(),
                                       CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex) { return Resultado.Error("No se ha podido hacer la previa: " + ex.Message); }

        if (salio is null) return Resultado.Error("La previa no ha salido: ffmpeg no ha dejado fichero.");

        long peso = 0;
        try { peso = new FileInfo(salio).Length; } catch { }

        return Resultado.Ok($"Diez segundos desde {Partir.Reloj(desde)} en:\n  {salio}"
            + $"  ({Peso(peso)})\n\n"
            + ComoQueda(opt)
            + "\n\nEnséñasela a quien te lo pidió antes de lanzar la tanda. Ojo: la previa "
            + "codifica siempre lo más rápido posible, así que NO respeta el esmero y no dice "
            + "nada del tiempo ni del tamaño finales — para eso, ondine_medir. Y se borra sola "
            + "cuando el sistema limpie los temporales.");
    }

    // ── Medir ────────────────────────────────────────────────────────────────

    /// <summary>
    /// La medida de verdad: codifica muestras cortas con los ajustes elegidos y saca de ahí el
    /// tamaño. Es el botón «Medir con una muestra» de la app.
    /// </summary>
    public static Resultado Medir(JsonObject a)
    {
        var ruta = Texto(a, "fichero");
        if (ruta is null) return Resultado.Error("Falta «fichero».");
        if (!File.Exists(ruta)) return Resultado.Error($"No existe: {ruta}");

        var opt = Opciones(a, out var error);
        if (opt is null) return Resultado.Error(error!);
        if (opt.AudioOnly) return Resultado.Error("Medir es para vídeo: en «solo audio» el tamaño sale del caudal elegido.");
        Ajustes(a);

        var motor = new Engine();
        var cuaderno = new Tandas.Cuaderno();
        int kbps;
        try
        {
            kbps = motor.MeasureVideoBitrateAsync(ruta, opt, cuaderno, CancellationToken.None)
                        .GetAwaiter().GetResult();
        }
        catch (Exception ex) { return Resultado.Error("No se ha podido medir: " + ex.Message); }

        if (kbps <= 0) return Resultado.Error("No se ha podido medir: ffmpeg no devolvió muestras útiles.");

        var fila = FilaDe(motor, ruta);
        var duracion = fila?.DurationSec ?? 0;
        long previsto = (long)((kbps + 192.0) * 1000 / 8 * duracion * 1.02);
        long original = new FileInfo(ruta).Length;

        var sb = new StringBuilder($"Medido «{Path.GetFileName(ruta)}» codificando muestras de verdad.\n\n");
        sb.AppendLine(ComoQueda(opt));
        sb.AppendLine($"\n  Vídeo medido: {kbps} kbps");
        if (duracion > 0)
            sb.AppendLine($"  Tamaño previsto: ≈ {Peso(previsto)}, del original de {Peso(original)}"
                        + $"  (≈ {Variacion(original, previsto)})");
        sb.Append("  Incluye una estimación de audio de 192 kbps: el valor exacto depende de las pistas que conserves.");
        return Resultado.Ok(sb.ToString());
    }

    // ── Ayudas ───────────────────────────────────────────────────────────────

    /// <summary>Los vídeos sobre los que se trabaja: una carpeta, o una lista de ficheros.</summary>
    private static IReadOnlyList<string>? Entrada(JsonObject a, out string? error)
    {
        error = null;
        var ficheros = Lista(a, "ficheros");
        var carpeta = Texto(a, "carpeta");

        if (ficheros is { Count: > 0 })
        {
            var faltan = ficheros.Where(f => !File.Exists(f)).ToList();
            if (faltan.Count > 0) { error = "No existen: " + string.Join(", ", faltan); return null; }
            return ficheros;
        }

        if (carpeta is null) { error = "Hace falta «carpeta» o «ficheros»."; return null; }
        if (!Directory.Exists(carpeta)) { error = $"No existe la carpeta: {carpeta}"; return null; }

        var videos = Rutas.VideosQueLlegan.Expandir(
            [carpeta], Bandera(a, "subcarpetas", SettingsStore.Load().Recurse));
        if (videos.Count == 0) { error = $"No hay vídeos en {carpeta}."; return null; }
        return videos;
    }

    /// <summary>Una fila sondeada, que es lo que come el estimador.</summary>
    private static VideoRow? FilaDe(Engine motor, string ruta)
    {
        try
        {
            var info = motor.ProbeAsync(ruta).GetAwaiter().GetResult();
            return new VideoRow
            {
                Name = Path.GetFileName(ruta),
                Path = ruta,
                Bytes = new FileInfo(ruta).Length,
                Probed = true,
                Codec = info.Codec,
                Audio = string.Join("+", info.AudioLangs),
                Subs = string.Join("+", info.SubLangs),
                Width = info.Width,
                Height = info.Height,
                Fps = info.Fps,
                DurationSec = info.DurationSec,
                VideoBitrateKbps = info.VideoBitrateKbps,
                AudioBitrateKbps = info.AudioBitrateKbps,
                Channels = info.Channels,
                AudioCodec = info.AudioCodec,
            };
        }
        catch { return null; }
    }

    /// <summary>
    /// Un peso legible. En megas enteros, un capítulo de prueba de 140 kB salía como «0 MB» y el
    /// parte entero se leía como si no hubiera pasado nada.
    /// </summary>
    internal static string Peso(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1073741824.0:0.##} GB",
        >= 10_485_760 => $"{bytes / 1048576} MB",
        >= 1_048_576 => $"{bytes / 1048576.0:0.#} MB",
        _ => $"{Math.Max(bytes / 1024, 1)} kB",
    };

    /// <summary>
    /// Cuánto ha cambiado el peso, con su signo.
    ///
    /// <para>
    /// Y contempla que CREZCA, que pasa de verdad: subir el caudal del audio, o recodificar algo
    /// que ya venía apretado, deja un fichero más gordo. La primera versión daba el ahorro por
    /// hecho y escribía «--13 %» —dos signos menos— sobre un fichero que había engordado.
    /// </para>
    /// </summary>
    internal static string Variacion(long entra, long sale)
    {
        if (entra <= 0) return "";
        var ahorro = 100 - (int)(sale * 100 / entra);
        return ahorro >= 0 ? $"-{ahorro} %" : $"+{-ahorro} %, ha crecido";
    }

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

    /// <summary>
    /// Una lista de cadenas, y también una cadena suelta.
    ///
    /// <para>
    /// Un agente escribe <c>"idiomas": "spa"</c> tan a menudo como <c>["spa"]</c>. Rechazarlo
    /// sería correcto y no ayudaría a nadie.
    /// </para>
    /// </summary>
    private static List<string>? Lista(JsonObject a, string clave)
    {
        if (!a.TryGetPropertyValue(clave, out var v) || v is null) return null;

        if (v is JsonArray arr)
            return [.. arr.Where(x => x is not null).Select(x => x!.ToString().Trim()).Where(s => s.Length > 0)];

        var suelta = v.ToString().Trim();
        return suelta.Length > 0 ? [suelta] : null;
    }
}
