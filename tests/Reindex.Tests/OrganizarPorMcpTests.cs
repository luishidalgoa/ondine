using System.Text.Json.Nodes;
using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Las decisiones fila a fila de Organizar, por MCP: fijar el episodio de una duda, dejar un
/// fichero como está, y deshacer una tanda.
///
/// <para>
/// <b>Lo que de verdad se prueba aquí es que los dos lados guardan en el mismo sitio.</b> Si el
/// agente guardara sus decisiones en su propio fichero, todo funcionaría —cada uno recordaría lo
/// suyo— y el usuario tendría que decir dos veces lo mismo: una a la ventana y otra al agente. Eso
/// no da error, no rompe ninguna prueba obvia, y es la clase de queja que aparece a la semana.
/// Así que se escribe con <c>Organizar.Fijar</c> y se lee con <c>ReindexStore</c>, que es lo que
/// lee la app.
/// </para>
/// <para>
/// Y con la raíz redirigida a una carpeta temporal: estas pruebas escriben decisiones y diarios
/// de verdad, y sin redirigir se llevarían por delante los del usuario que las corre.
/// </para>
/// </summary>
public static class OrganizarPorMcpTests
{
    public static void Todas()
    {
        Program.Seccion("Organizar fila a fila por MCP");

        EnUnaRaizAparte(FijarGuardaDondeLeeLaApp);
        EnUnaRaizAparte(FijarLoQueNoSePuede);
        EnUnaRaizAparte(DejarComoEstaVaAlCatalogo);
        EnUnaRaizAparte(DeshacerNecesitaUnaTanda);
        EnUnaRaizAparte(RenombrarDejaAlgoQueDeshacer);
        ElEsquemaLoOfrece();
    }

    // ── Fijar ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fijar escribe una decisión completa, con la clave y la forma que lee la app.
    ///
    /// <para>
    /// Los campos se recorren <b>por reflexión</b> sobre <c>ReindexOverride</c>: si mañana alguien
    /// le añade uno al tipo del motor y la ventana lo rellena, esta prueba se pone roja hasta que
    /// el MCP también lo rellene. Es la misma red que vigila los mandos de Comprimir.
    /// </para>
    /// </summary>
    private static void FijarGuardaDondeLeeLaApp(string raiz)
    {
        var (carpeta, catalogo) = Montar(raiz);
        var video = Path.Combine(carpeta, "cinta vieja sin numerar.mkv");
        File.WriteAllText(video, "x");

        var h = Catalogo.Todas.Single(x => x.Nombre == "ondine_fijar_episodio");

        // Sin «confirmar» dice lo que haría y no toca nada.
        var ensayo = h.Ejecutar(new JsonObject
        {
            ["fichero"] = video, ["catalogo"] = catalogo, ["episodio"] = 3,
        });
        Program.Assert(!ensayo.EsError && ensayo.Texto.Contains("Tres"),
            $"el ensayo dice de qué episodio habla ({Recorte(ensayo.Texto)})");
        Program.Assert(ReindexStore.CargarDecisiones().Count == 0,
            "y sin «confirmar» no ha guardado nada");

        var hecho = h.Ejecutar(new JsonObject
        {
            ["fichero"] = video, ["catalogo"] = catalogo, ["episodio"] = 3,
            ["segmento"] = "b", ["confirmar"] = true,
        });
        Program.Assert(!hecho.EsError, $"y con «confirmar» se guarda ({Recorte(hecho.Texto)})");

        // ── Lo importante: la lee ReindexStore, que es lo que lee la ventana ──
        var guardadas = ReindexStore.CargarDecisiones();
        Program.Assert(guardadas.Count == 1, $"hay una decisión guardada ({guardadas.Count})");

        Program.Assert(guardadas.ContainsKey(video),
            "y la clave es la ruta completa, que es el «fingerprint» que usa la app cuando el "
            + "fichero no tiene uno de contenido");

        // Si la clave no es la que se espera, lo demás no se puede mirar. Se dice y se sale, en
        // vez de reventar y llevarse por delante el resto de la suite.
        if (!guardadas.TryGetValue(video, out var d)) return;
        Program.Assert(d.Num == 3, $"el número que se pidió ({d.Num})");
        Program.Assert(d.Temporada == 1, $"la temporada la pone el catálogo, no quien llama ({d.Temporada})");
        Program.Assert(d.Serie == "Serie", $"la serie, para saber de qué catálogo salió ({d.Serie})");
        Program.Assert(d.Seg == "b", $"el segmento ({d.Seg})");
        Program.Assert(d.Origen == "mcp",
            $"y el origen dice «mcp», que es lo que distingue una decisión del agente de una tuya ({d.Origen})");
        Program.Assert(d.NombreOriginal == "cinta vieja sin numerar.mkv",
            $"con el nombre de antes, o dentro de un año «episodio 3» no le dice nada a nadie ({d.NombreOriginal})");
        Program.Assert(d.FechaDecision.Length == 10, $"y cuándo se decidió ({d.FechaDecision})");

        // Ningún campo del tipo del motor se queda sin rellenar. Uno nuevo pone esto rojo.
        foreach (var p in typeof(ReindexOverride).GetProperties())
        {
            var v = p.GetValue(d);
            Program.Assert(v is not null && v.ToString() is { Length: > 0 },
                $"«{p.Name}» viene puesto: un campo que el MCP deja vacío y la ventana rellena "
                + "es una decisión que se ve distinta según quién la tomó");
        }

        // Y el análisis la respeta: es para lo que se guardó.
        var propuesta = Catalogo.Todas.Single(x => x.Nombre == "ondine_analizar")
            .Ejecutar(new JsonObject { ["carpeta"] = carpeta, ["catalogo"] = catalogo });
        Program.Assert(!propuesta.EsError && propuesta.Texto.Contains("Tres"),
            $"y el siguiente análisis ya no la pregunta: la aplica ({Recorte(propuesta.Texto)})");
    }

    /// <summary>Lo que no se puede fijar se dice antes de escribir nada.</summary>
    private static void FijarLoQueNoSePuede(string raiz)
    {
        var (carpeta, catalogo) = Montar(raiz);
        var video = Path.Combine(carpeta, "algo.mkv");
        File.WriteAllText(video, "x");

        var h = Catalogo.Todas.Single(x => x.Nombre == "ondine_fijar_episodio");

        var sinNumero = h.Ejecutar(new JsonObject { ["fichero"] = video, ["catalogo"] = catalogo });
        Program.Assert(sinNumero.EsError && sinNumero.Texto.Contains("episodio"),
            $"sin número no hay nada que fijar ({Recorte(sinNumero.Texto)})");

        var noExiste = h.Ejecutar(new JsonObject
        {
            ["fichero"] = Path.Combine(carpeta, "no-esta.mkv"),
            ["catalogo"] = catalogo, ["episodio"] = 1, ["confirmar"] = true,
        });
        Program.Assert(noExiste.EsError && noExiste.Texto.Contains("No existe"),
            $"un fichero que no está se dice ({Recorte(noExiste.Texto)})");

        // Y el número tiene que existir en el catálogo: fijar el 900 de una serie de 6 es
        // exactamente el error que nadie descubriría hasta ver el nombre puesto.
        var fuera = h.Ejecutar(new JsonObject
        {
            ["fichero"] = video, ["catalogo"] = catalogo, ["episodio"] = 900, ["confirmar"] = true,
        });
        Program.Assert(fuera.EsError && fuera.Texto.Contains("900"),
            $"y un episodio que el catálogo no tiene también ({Recorte(fuera.Texto)})");

        // Y dice los números que HAY. Corriéndolo a mano salía «va del 1 al 0» en un catálogo de
        // dos episodios: «total» es un campo declarado que muchos no traen. Un mensaje de error
        // que miente es peor que uno escueto, porque el que lo lee se lo cree.
        Program.Assert(fuera.Texto.Contains("Tiene 6") && fuera.Texto.Contains("entre el 1 y el 6"),
            $"y entre qué números van los que tiene ({Recorte(fuera.Texto)})");
        Program.Assert(ReindexStore.CargarDecisiones().Count == 0,
            "sin haber guardado ninguna de las tres");
    }

    // ── Dejar como está ──────────────────────────────────────────────────────

    /// <summary>
    /// «Dejar como está» se apunta en el catálogo y no en las decisiones, porque pertenece a la
    /// serie: quien se lleve el catálogo a otra máquina se lleva también esto.
    /// </summary>
    private static void DejarComoEstaVaAlCatalogo(string raiz)
    {
        var (carpeta, catalogo) = Montar(raiz);
        var h = Catalogo.Todas.Single(x => x.Nombre == "ondine_dejar_como_esta");

        var ensayo = h.Ejecutar(new JsonObject { ["catalogo"] = catalogo, ["fichero"] = "avance.mkv" });
        Program.Assert(!ensayo.EsError && !ReindexCatalog.Load(catalogo).SeDejaComoEsta("avance.mkv"),
            "el ensayo no apunta nada");

        var hecho = h.Ejecutar(new JsonObject
        {
            ["catalogo"] = catalogo,
            ["ficheros"] = new JsonArray("avance.mkv", Path.Combine(carpeta, "caratula.mkv")),
            ["confirmar"] = true,
        });
        Program.Assert(!hecho.EsError, $"se apuntan ({Recorte(hecho.Texto)})");

        var cat = ReindexCatalog.Load(catalogo);
        Program.Assert(cat.SeDejaComoEsta("avance.mkv"), "el que se pasó por nombre");
        Program.Assert(cat.SeDejaComoEsta("caratula.mkv"),
            "y el que se pasó con su ruta: se guarda el nombre, que sigue valiendo si cambia de carpeta");

        // Repetirlo no duplica: es lo que pasa cuando un agente reintenta.
        var otra = h.Ejecutar(new JsonObject
        {
            ["catalogo"] = catalogo, ["fichero"] = "avance.mkv", ["confirmar"] = true,
        });
        Program.Assert(!otra.EsError && ReindexCatalog.Load(catalogo).DejarComoEsta.Count == 2,
            $"y repetirlo no lo duplica ({ReindexCatalog.Load(catalogo).DejarComoEsta.Count})");

        var sinNada = h.Ejecutar(new JsonObject { ["catalogo"] = catalogo, ["confirmar"] = true });
        Program.Assert(sinNada.EsError, "sin decir qué fichero, no");
    }

    // ── Deshacer ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Deshacer se apoya en el diario, y el renombrado por MCP no lo escribía: había tandas
    /// aplicadas y nada que deshacer. Aquí se comprueba con el diario que escribe el propio MCP.
    /// </summary>
    private static void DeshacerNecesitaUnaTanda(string raiz)
    {
        var h = Catalogo.Todas.Single(x => x.Nombre == "ondine_deshacer_renombrado");

        var vacio = h.Ejecutar(new JsonObject());
        Program.Assert(vacio.EsError && vacio.Texto.Contains("ninguna tanda"),
            $"sin tandas lo dice, en vez de decir que ha deshecho nada ({Recorte(vacio.Texto)})");

        // Una tanda de verdad: dos ficheros ya movidos, apuntados como los apunta el renombrado.
        var carpeta = Path.Combine(raiz, "tv");
        Directory.CreateDirectory(carpeta);
        var antes = Path.Combine(carpeta, "crudo 01.mkv");
        var ahora = Path.Combine(carpeta, "Serie - S01E01 - Uno.mkv");
        File.WriteAllText(ahora, "x");
        Organizar.Apuntar("Serie", carpeta, [(antes, ahora)]);

        var ensayo = h.Ejecutar(new JsonObject());
        Program.Assert(!ensayo.EsError && ensayo.Texto.Contains("crudo 01.mkv"),
            $"el ensayo dice a qué nombre volvería ({Recorte(ensayo.Texto)})");
        Program.Assert(File.Exists(ahora), "y no ha movido nada todavía");

        var hecho = h.Ejecutar(new JsonObject { ["confirmar"] = true });
        Program.Assert(!hecho.EsError, $"se deshace ({Recorte(hecho.Texto)})");
        Program.Assert(File.Exists(antes) && !File.Exists(ahora),
            "el fichero ha vuelto a su nombre de antes");

        Program.Assert(h.Ejecutar(new JsonObject()).EsError,
            "y la tanda ya no está: deshacer dos veces no vuelve a deshacer lo mismo");
    }

    /// <summary>
    /// Renombrar de verdad y deshacerlo de verdad, la vuelta entera.
    ///
    /// <para>
    /// <b>Este era el defecto.</b> Deshacer se apoya en un diario que la ventana escribe al
    /// aplicar, y el renombrado por MCP no lo escribía: la tanda quedaba aplicada y no había nada
    /// que deshacer. No daba ningún error —lo pedido se había hecho— y solo se notaba al querer
    /// volver atrás, que es el peor momento para descubrirlo. Así que se prueba con ficheros de
    /// verdad y por la puerta de delante: se renombra con la herramienta, y se deshace con la otra.
    /// </para>
    /// </summary>
    private static void RenombrarDejaAlgoQueDeshacer(string raiz)
    {
        var (carpeta, catalogo) = Montar(raiz);
        var sucio = Path.Combine(carpeta, "Serie.S01E02.1080p.WEB-DL.x264.mkv");
        File.WriteAllText(sucio, "x");

        // Un nombre así no lleva título, así que el análisis lo deja como duda y no lo renombra.
        // Es justo el caso para el que se hizo «fijar»: se resuelve, y entonces ya es seguro.
        var duda = Catalogo.Todas.Single(x => x.Nombre == "ondine_aplicar_renombrado")
            .Ejecutar(new JsonObject
            {
                ["carpeta"] = carpeta, ["catalogo"] = catalogo, ["confirmar"] = true,
            });
        Program.Assert(duda.EsError || duda.Texto.Contains("nada que aplicar"),
            $"de entrada no se toca: es una duda ({Recorte(duda.Texto)})");
        Program.Assert(File.Exists(sucio), "y el fichero sigue con su nombre");

        Catalogo.Todas.Single(x => x.Nombre == "ondine_fijar_episodio")
            .Ejecutar(new JsonObject
            {
                ["fichero"] = sucio, ["catalogo"] = catalogo,
                ["episodio"] = 2, ["confirmar"] = true,
            });

        var aplicado = Catalogo.Todas.Single(x => x.Nombre == "ondine_aplicar_renombrado")
            .Ejecutar(new JsonObject
            {
                ["carpeta"] = carpeta, ["catalogo"] = catalogo, ["confirmar"] = true,
            });
        Program.Assert(!aplicado.EsError && aplicado.Texto.Contains("Renombrados 1"),
            $"se renombra el fichero sucio ({Recorte(aplicado.Texto)})");
        Program.Assert(!File.Exists(sucio), "y ya no está con su nombre de antes");

        // Y ahora lo que faltaba: que haya quedado apuntado.
        var lote = ReindexStore.UltimoLote();
        Program.Assert(lote is not null,
            "el renombrado por MCP apunta su tanda en el diario, igual que la ventana");
        if (lote is null) return;

        Program.Assert(lote.Serie == "Serie",
            $"con la serie de la que era ({lote.Serie})");
        Program.Assert(lote.Movimientos.Count == 1 && lote.Movimientos[0].De == sucio,
            "y de qué nombre venía cada fichero");

        var vuelta = Catalogo.Todas.Single(x => x.Nombre == "ondine_deshacer_renombrado")
            .Ejecutar(new JsonObject { ["confirmar"] = true });
        Program.Assert(!vuelta.EsError, $"se puede deshacer ({Recorte(vuelta.Texto)})");
        Program.Assert(File.Exists(sucio),
            "y el fichero vuelve a llamarse como se llamaba: la vuelta entera, no media");
    }

    // ── El esquema ───────────────────────────────────────────────────────────

    private static void ElEsquemaLoOfrece()
    {
        Comprueba("ondine_fijar_episodio", "fichero", "catalogo", "episodio", "segmento", "confirmar");
        Comprueba("ondine_dejar_como_esta", "catalogo", "fichero", "ficheros", "confirmar");
        Comprueba("ondine_deshacer_renombrado", "confirmar");

        // Las tres escriben, y eso decide si un cliente en solo-lectura las ofrece.
        foreach (var nombre in new[] { "ondine_fijar_episodio", "ondine_dejar_como_esta",
                                       "ondine_deshacer_renombrado" })
            Program.Assert(Catalogo.Todas.Single(x => x.Nombre == nombre).Escribe,
                $"«{nombre}» está marcada como que escribe");
    }

    private static void Comprueba(string herramienta, params string[] esperados)
    {
        var args = (Catalogo.Todas.Single(x => x.Nombre == herramienta)
                    .Esquema["properties"] as JsonObject)!.Select(p => p.Key).ToHashSet();
        foreach (var suyo in esperados)
            Program.Assert(args.Contains(suyo), $"«{herramienta}» ofrece «{suyo}»");
    }

    // ── Montaje ──────────────────────────────────────────────────────────────

    private const string Json = """
    {
      "esquema": "reindex/1.0", "serie": "Serie",
      "episodios": [
        { "num": 1, "temporada": 1, "titulos": { "es": ["Uno"] } },
        { "num": 2, "temporada": 1, "titulos": { "es": ["Dos"] } },
        { "num": 3, "temporada": 1, "titulos": { "es": ["Tres"] } },
        { "num": 4, "temporada": 1, "titulos": { "es": ["Cuatro"] } },
        { "num": 5, "temporada": 1, "titulos": { "es": ["Cinco"] } },
        { "num": 6, "temporada": 1, "titulos": { "es": ["Seis"] } }
      ]
    }
    """;

    /// <summary>Una carpeta con su catálogo al lado, que es lo mínimo para pedir cualquiera de las tres.</summary>
    private static (string Carpeta, string Catalogo) Montar(string raiz)
    {
        var carpeta = Path.Combine(raiz, "Serie", "Season 01");
        Directory.CreateDirectory(carpeta);
        var catalogo = Path.Combine(raiz, "serie.json");
        File.WriteAllText(catalogo, Json);
        return (carpeta, catalogo);
    }

    /// <summary>
    /// Corre la prueba con la raíz de datos apuntando a una carpeta temporal. Sin esto, estas
    /// pruebas escribirían en las decisiones y el diario de verdad de quien las corre.
    /// </summary>
    private static void EnUnaRaizAparte(Action<string> prueba)
    {
        var raiz = Path.Combine(Path.GetTempPath(), "ondine-mcp-org-" + Guid.NewGuid().ToString("N")[..8]);
        var antes = ReindexStore.RaizOverride;
        try
        {
            Directory.CreateDirectory(raiz);
            ReindexStore.RaizOverride = raiz;
            prueba(raiz);
        }
        finally
        {
            ReindexStore.RaizOverride = antes;
            try { Directory.Delete(raiz, recursive: true); } catch { /* da igual, es temporal */ }
        }
    }

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 80 ? l[..80] + "…" : l;
}
