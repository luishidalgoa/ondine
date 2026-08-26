using System.Text.Json.Nodes;
using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Comprimir desde el MCP, con los mismos mandos que la ventana.
///
/// <para>
/// <b>El requisito era «todas las funcionalidades que tiene la interfaz»</b>, y ahí está la
/// trampa: media docena de opciones dan una herramienta que sirve para la mitad de los casos y
/// obliga a abrir la app para la otra mitad, que es justo lo que un servidor MCP viene a
/// evitar. Y no se nota al escribirla, se nota tres meses después, cuando alguien añade un
/// mando a la pantalla y aquí no aparece.
/// </para>
/// <para>
/// Por eso la prueba que importa de este fichero no comprueba un caso: compara la lista de
/// mandos del motor con el esquema de la herramienta, y falla cuando aparece uno nuevo sin
/// asignar. Es el mismo trato que se le dio a la documentación del MCP.
/// </para>
/// </summary>
public static class ComprimirPorMcpTests
{
    public static void Todas()
    {
        Program.Seccion("Comprimir por MCP");

        NoFaltaNingunMando();
        LoQueNoEntiendeLoDice();
        SinConfirmarNoComprime();
        ElCaudalSoloRecodificaSiSePide();
        LosIdiomasYLosSubtitulos();
    }

    /// <summary>
    /// Cada mando de <see cref="EncodeOptions"/> que la ventana usa tiene su argumento aquí.
    ///
    /// <para>
    /// La lista de exentos va escrita con su motivo, uno a uno. Ese es el punto: añadir algo a
    /// la exención obliga a escribir por qué, y eso se lee en la revisión; dejarse un mando sin
    /// asignar, no se leería.
    /// </para>
    /// </summary>
    private static void NoFaltaNingunMando()
    {
        // Mando del motor → argumento de la herramienta.
        var mapa = new Dictionary<string, string>
        {
            [nameof(EncodeOptions.Output)] = "salida",
            [nameof(EncodeOptions.Container)] = "formato",
            [nameof(EncodeOptions.AudioOnly)] = "formato",       // «mp3» y compañía lo encienden
            [nameof(EncodeOptions.AudioFormat)] = "formato",
            [nameof(EncodeOptions.VideoCodec)] = "codec",
            [nameof(EncodeOptions.Quality)] = "calidad",
            [nameof(EncodeOptions.Velocidad)] = "esmero",
            [nameof(EncodeOptions.MaxHeight)] = "alto",
            [nameof(EncodeOptions.TamanoObjetivoBytes)] = "tamano_objetivo_mb",
            [nameof(EncodeOptions.AudioCodec)] = "audio_codec",
            [nameof(EncodeOptions.AudioBitrate)] = "audio_kbps",
            [nameof(EncodeOptions.AudioMezcla)] = "audio_estereo",
            [nameof(EncodeOptions.Lang)] = "idioma",
            [nameof(EncodeOptions.KeepLangs)] = "idiomas",
            [nameof(EncodeOptions.SubLangs)] = "subtitulos",
            [nameof(EncodeOptions.NoSubs)] = "sin_subtitulos",
            [nameof(EncodeOptions.Force)] = "forzar",
        };

        // Y lo que NO se ofrece, con el motivo al lado.
        var exentos = new Dictionary<string, string>
        {
            [nameof(EncodeOptions.DryRun)] = "no hace falta: sin «confirmar» ya se contesta el pronóstico",
            [nameof(EncodeOptions.NameRule)] = "el renombrado libre es una receta guardada en Preferencias, "
                                             + "con su propia pantalla de vista previa",
            [nameof(EncodeOptions.BitrateVideoKbps)] = "lo calcula el tamaño objetivo; a mano no lo ofrece "
                                                     + "ni la ventana",
            [nameof(EncodeOptions.Desde)] = "es de Recortes, no de Comprimir",
            [nameof(EncodeOptions.Duracion)] = "es de Recortes, no de Comprimir",
            [nameof(EncodeOptions.NombreSalida)] = "es de Recortes, no de Comprimir",
        };

        var herramienta = Catalogo.Todas.Single(h => h.Nombre == "ondine_comprimir");
        var argumentos = (herramienta.Esquema["properties"] as JsonObject)!
            .Select(p => p.Key).ToHashSet();

        foreach (var prop in typeof(EncodeOptions).GetProperties())
        {
            var nombre = prop.Name;

            if (exentos.TryGetValue(nombre, out var porQue))
            {
                Program.Assert(!mapa.ContainsKey(nombre),
                    $"«{nombre}» está exento y no debería estar también asignado ({porQue})");
                continue;
            }

            // El mensaje se arma según el resultado: el primero decía «y no está en el esquema»
            // también cuando estaba, porque se concatenaba siempre. Un ✓ que se lee como un ✗ es
            // ruido, y del que confunde de verdad al repasar una tanda larga.
            var puesto = mapa.TryGetValue(nombre, out var arg) && argumentos.Contains(arg);
            Program.Assert(puesto,
                puesto
                    ? $"«{nombre}» se pide con «{arg}»"
                    : mapa.ContainsKey(nombre)
                        ? $"«{nombre}» dice pedirse con «{mapa[nombre]}», que no está en el esquema"
                        : $"«{nombre}» es un mando nuevo: ponlo en el esquema, o en los exentos con su motivo");
        }

        // Y los ajustes que no viven en EncodeOptions sino en el motor: los pone Preferencias en
        // la app, así que aquí tienen que poder ponerse también.
        foreach (var suyo in new[] { "hardware", "aceleracion", "margen_disco_mb", "limite" })
            Program.Assert(argumentos.Contains(suyo), $"y el ajuste «{suyo}» también se puede pedir");
    }

    /// <summary>
    /// Un valor que no existe se rechaza diciendo los que hay. Tragárselo y caer en el de por
    /// defecto es peor: el agente cree que se le ha hecho caso y repite el error.
    /// </summary>
    private static void LoQueNoEntiendeLoDice()
    {
        var malos = new (string Clave, string Valor, string Debe)[]
        {
            ("formato", "avi", "mkv"),
            ("codec", "h265", "hevc"),
            ("esmero", "turbo", "equilibrado"),
            ("audio_codec", "mp3", "aac"),
        };

        foreach (var (clave, valor, debe) in malos)
        {
            var opt = Comprimir.Opciones(new JsonObject { [clave] = valor }, out var error);
            Program.Assert(opt is null && error is not null && error.Contains(debe),
                $"«{clave}: {valor}» se rechaza y dice los que hay ({error})");
        }

        // Y la calidad fuera de rango, que es la que se cuela con un número plausible.
        Comprimir.Opciones(new JsonObject { ["calidad"] = 12 }, out var eCalidad);
        Program.Assert(eCalidad is not null && eCalidad.Contains("18"),
            $"un CRF de 12 se rechaza con el rango en la mano ({eCalidad})");

        var bien = Comprimir.Opciones(new JsonObject { ["formato"] = "mp4", ["codec"] = "av1" }, out var sinError);
        Program.Assert(bien is not null && sinError is null && bien.Container == "mp4" && bien.VideoCodec == "av1",
            "y lo que sí existe pasa");
    }

    /// <summary>
    /// La regla de la casa, en la herramienta que más puede costar: sin permiso no se codifica
    /// nada. Aquí importa el doble, porque una tanda mal lanzada no es un fichero mal nombrado:
    /// son horas de máquina.
    /// </summary>
    private static void SinConfirmarNoComprime()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "ondine-mcp-comp-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(carpeta);
        try
        {
            var falso = Path.Combine(carpeta, "capitulo.mkv");
            File.WriteAllText(falso, "no soy un vídeo");
            var antes = File.GetLastWriteTimeUtc(falso);

            var h = Catalogo.Todas.Single(n => n.Nombre == "ondine_comprimir");
            var r = h.Ejecutar(new JsonObject { ["carpeta"] = carpeta });

            Program.Assert(r.Texto.Contains("SIN CONFIRMAR"), $"sin «confirmar» contesta el ensayo ({Recorte(r.Texto)})");
            Program.Assert(File.GetLastWriteTimeUtc(falso) == antes, "y no ha tocado el fichero");
            Program.Assert(Directory.GetFiles(carpeta).Length == 1, "ni ha escrito nada nuevo en la carpeta");

            // El ensayo cuenta lo que se va a aplicar, no solo que hace falta confirmar: es lo
            // que permite dar el permiso con conocimiento.
            Program.Assert(r.Texto.Contains("Contenedor") && r.Texto.Contains("Idiomas de audio"),
                "y el ensayo dice con qué ajustes iría");

            Program.Assert(h.Escribe, "la herramienta se declara como que escribe");
        }
        finally { try { Directory.Delete(carpeta, true); } catch { } }
    }

    /// <summary>
    /// El caudal a solas recodifica a AAC, igual que en Recortes y en la línea de órdenes: aquí
    /// tampoco hay dos desplegables que desempatar. Con un códec pedido, manda el códec.
    /// </summary>
    private static void ElCaudalSoloRecodificaSiSePide()
    {
        var soloCaudal = Comprimir.Opciones(new JsonObject { ["audio_kbps"] = 128 }, out _);
        Program.Assert(soloCaudal!.AudioCodec == Ondine.Audio.AudioElegido.Aac,
            $"pedir 128 kbps a secas es pedir AAC ({soloCaudal.AudioCodec})");

        var conCodec = Comprimir.Opciones(
            new JsonObject { ["audio_kbps"] = 128, ["audio_codec"] = "eac3" }, out _);
        Program.Assert(conCodec!.AudioCodec == Ondine.Audio.AudioElegido.Eac3,
            $"y con un códec pedido manda el códec ({conCodec.AudioCodec})");

        var sinNada = Comprimir.Opciones(new JsonObject(), out _);
        Program.Assert(sinNada!.AudioCodec == Ondine.Audio.AudioElegido.Copiar && sinNada.AudioBitrate == 0,
            "sin pedir nada, el audio se copia: es lo que hace la app de fábrica");
    }

    private static void LosIdiomasYLosSubtitulos()
    {
        // Una cadena suelta donde cabía una lista: un agente escribe «"idiomas": "spa"» tanto
        // como «["spa"]», y rechazarlo sería correcto y no ayudaría a nadie.
        var suelta = Comprimir.Opciones(new JsonObject { ["idiomas"] = "spa" }, out _);
        Program.Assert(suelta!.KeepLangs.SequenceEqual(["spa"]), "una cadena suelta vale como lista de uno");

        var lista = Comprimir.Opciones(
            new JsonObject { ["idiomas"] = new JsonArray("spa", "eng", "por") }, out _);
        Program.Assert(lista!.KeepLangs.Count == 3, "y la lista, como lista");

        var todas = Comprimir.Opciones(new JsonObject { ["idiomas"] = "all" }, out _);
        Program.Assert(todas!.KeepLangs.Contains(Ondine.Audio.PistasQueSeQuedan.Todas),
            "«all» llega tal cual al motor, que es quien lo entiende");

        var sinSubs = Comprimir.Opciones(new JsonObject { ["sin_subtitulos"] = true }, out _);
        Program.Assert(sinSubs!.NoSubs && sinSubs.SubLangs is null, "y «sin_subtitulos» los tira todos");

        var porDefecto = Comprimir.Opciones(new JsonObject(), out _);
        Program.Assert(porDefecto!.SubLangs is null && !porDefecto.NoSubs,
            "sin decir nada, los subtítulos se conservan: null es «todos»");
    }

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 80 ? l[..80] + "…" : l;
}
