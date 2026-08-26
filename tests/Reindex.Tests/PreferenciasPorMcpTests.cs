using System.Text.Json.Nodes;
using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Las Preferencias, leídas y cambiadas desde el MCP.
///
/// <para>
/// <b>Dos cosas se vigilan aquí antes que ninguna otra.</b> Que las claves no se escapen —ni la
/// del modelo ni la de TMDb, ni en claro ni cifradas— y que cambiar un ajuste no arrastre a los
/// demás: el historial de renombrado y el factor de complejidad que la app aprende midiendo
/// viven en el mismo fichero, y ya se perdieron una vez por construir un <c>Settings</c> nuevo
/// en vez de partir del que había.
/// </para>
/// <para>
/// Estas pruebas escriben en el fichero de ajustes de verdad, como las que ya había: se guarda
/// el original al empezar y se devuelve al terminar, pase lo que pase.
/// </para>
/// </summary>
public static class PreferenciasPorMcpTests
{
    public static void Todas()
    {
        Program.Seccion("Las Preferencias por MCP");

        LasClavesNoSalen();
        NoFaltaNingunAjuste();
        SoloSeTocaLoPedido();
        SinConfirmarNoGuarda();
        LoQueNoEntiendeLoDice();
    }

    /// <summary>
    /// Ninguna clave sale en la lectura. Se pone una de mentira, se lee, y se busca su rastro:
    /// ni el valor, ni el cifrado, ni un trozo.
    /// </summary>
    private static void LasClavesNoSalen()
    {
        ConLosAjustesASalvo(original =>
        {
            const string secreto = "sk-esto-no-puede-salir-de-aqui-1234567890";
            var s = original.Clone();
            s.Ia.PonerClave(secreto);
            s.Tmdb.ClaveCifrada = "cifrada-de-mentira-abcdef";
            SettingsStore.Save(s);

            var texto = Catalogo.Todas.Single(h => h.Nombre == "ondine_preferencias")
                                      .Ejecutar(new JsonObject()).Texto;

            Program.Assert(!texto.Contains(secreto), "la clave del modelo no sale en la lectura");
            Program.Assert(!texto.Contains("cifrada-de-mentira"), "ni la de TMDb, ni cifrada");
            Program.Assert(!texto.Contains(s.Ia.ClaveCifrada) || s.Ia.ClaveCifrada.Length == 0,
                "ni el cifrado de la primera");
            Program.Assert(texto.Contains("puesta"), "pero sí dice que hay una puesta, que es lo útil");

            // Y no hay forma de escribirla: no existe el argumento.
            var esquema = Catalogo.Todas.Single(h => h.Nombre == "ondine_ajustar_preferencias").Esquema;
            var args = (esquema["properties"] as JsonObject)!.Select(p => p.Key).ToList();
            Program.Assert(!args.Any(x => x.Contains("clave", StringComparison.OrdinalIgnoreCase)),
                $"y no se puede poner una clave desde aquí ({string.Join(", ", args)})");
        });
    }

    /// <summary>
    /// Cada ajuste de la ventana de Preferencias se puede tocar, o está exento con su motivo. Es
    /// el mismo trato que se le dio a los mandos de Comprimir, y por lo mismo: lo que no se
    /// vigila se queda atrás en cuanto alguien añade un ajuste a la pantalla.
    /// </summary>
    private static void NoFaltaNingunAjuste()
    {
        var mapa = new Dictionary<string, string>
        {
            [nameof(Settings.Idioma)] = "idioma_app",
            [nameof(Settings.DefaultPreset)] = "preset_por_defecto",
            [nameof(Settings.DefaultLang)] = "idioma_audio",
            [nameof(Settings.Recurse)] = "subcarpetas",
            [nameof(Settings.CheckUpdatesOnStart)] = "buscar_actualizaciones",
            [nameof(Settings.AfterCompress)] = "tras_comprimir",
            [nameof(Settings.MinFreeMb)] = "margen_disco_mb",
            [nameof(Settings.UseHardware)] = "hardware",
            [nameof(Settings.Codificador)] = "codificador",
            [nameof(Settings.AceleracionVideo)] = "aceleracion",
            [nameof(Settings.Ia)] = "modelo_activo",        // y modelo_url, y modelo_nombre
            [nameof(Settings.Tmdb)] = "peliculas_activo",
        };

        var exentos = new Dictionary<string, string>
        {
            [nameof(Settings.Rename)] = "el renombrado libre es una receta con su propia pantalla y su "
                                      + "vista previa; media receta guardada a ciegas no sirve de nada",
            [nameof(Settings.RenameSearchHistory)] = "es historial, no ajuste: lo escribe el uso",
            [nameof(Settings.RenameReplaceHistory)] = "igual que el anterior",
            [nameof(Settings.ComplexityFactor)] = "lo APRENDE la app midiendo de verdad; escribirlo a mano "
                                                + "estropearía todos los pronósticos siguientes",
            [nameof(Settings.ComplementosApagados)] = "son permisos de complementos: que un agente los "
                                                    + "encienda por su cuenta es otra conversación",
            [nameof(Settings.ComplementosConModelo)] = "lo mismo, y encima da acceso al modelo",
            [nameof(Settings.CarpetaTemporadaIdioma)] = "lo elige la pantalla de Organizar al mover a "
                                                      + "carpetas de temporada, con el ejemplo delante",
        };

        var args = (Catalogo.Todas.Single(h => h.Nombre == "ondine_ajustar_preferencias")
                    .Esquema["properties"] as JsonObject)!.Select(p => p.Key).ToHashSet();

        foreach (var prop in typeof(Settings).GetProperties())
        {
            var nombre = prop.Name;
            if (exentos.ContainsKey(nombre)) continue;

            var puesto = mapa.TryGetValue(nombre, out var arg) && args.Contains(arg);
            Program.Assert(puesto,
                puesto
                    ? $"«{nombre}» se toca con «{arg}»"
                    : $"«{nombre}» es un ajuste nuevo: ponlo en el esquema, o en los exentos con su motivo");
        }
    }

    /// <summary>
    /// Lo que no se pide no se cambia, y lo que esta herramienta no ofrece tampoco se pierde.
    /// </summary>
    private static void SoloSeTocaLoPedido()
    {
        ConLosAjustesASalvo(original =>
        {
            var s = original.Clone();
            s.DefaultLang = "spa";
            s.MinFreeMb = 200;
            s.Recurse = true;
            s.RenameSearchHistory = ["1080p", "BluRay"];
            s.ComplexityFactor = 1.37;
            SettingsStore.Save(s);

            var r = Catalogo.Todas.Single(h => h.Nombre == "ondine_ajustar_preferencias")
                .Ejecutar(new JsonObject { ["idioma_audio"] = "jpn", ["confirmar"] = true });

            var vuelto = SettingsStore.Load();
            Program.Assert(!r.EsError && vuelto.DefaultLang == "jpn",
                $"se cambia lo pedido ({vuelto.DefaultLang})");
            Program.Assert(vuelto.MinFreeMb == 200 && vuelto.Recurse,
                "y no se toca lo que no se pidió");
            Program.Assert(vuelto.RenameSearchHistory.Count == 2 && Math.Abs(vuelto.ComplexityFactor - 1.37) < 0.001,
                "ni lo que esta herramienta no ofrece: el historial y el factor aprendido siguen ahí");
            Program.Assert(r.Texto.Contains("spa") && r.Texto.Contains("jpn"),
                $"y el parte dice el antes y el después ({Recorte(r.Texto)})");
        });
    }

    private static void SinConfirmarNoGuarda()
    {
        ConLosAjustesASalvo(original =>
        {
            var s = original.Clone();
            s.MinFreeMb = 200;
            SettingsStore.Save(s);

            var r = Catalogo.Todas.Single(h => h.Nombre == "ondine_ajustar_preferencias")
                .Ejecutar(new JsonObject { ["margen_disco_mb"] = 4321 });

            Program.Assert(SettingsStore.Load().MinFreeMb == 200, "sin «confirmar» no se guarda nada");
            Program.Assert(r.Texto.Contains("SIN CONFIRMAR"), "y se dice");
            Program.Assert(r.Texto.Contains("200") && r.Texto.Contains("4321"),
                $"con el antes y el después, que es lo único que deja juzgar un cambio de ajustes ({Recorte(r.Texto)})");
        });
    }

    private static void LoQueNoEntiendeLoDice()
    {
        ConLosAjustesASalvo(_ =>
        {
            var h = Catalogo.Todas.Single(n => n.Nombre == "ondine_ajustar_preferencias");

            var idioma = h.Ejecutar(new JsonObject { ["idioma_app"] = "fr", ["confirmar"] = true });
            Program.Assert(idioma.EsError && idioma.Texto.Contains("es"),
                $"un idioma de app que no existe se rechaza ({Recorte(idioma.Texto)})");

            var tras = h.Ejecutar(new JsonObject { ["tras_comprimir"] = "borrar", ["confirmar"] = true });
            Program.Assert(tras.EsError && tras.Texto.Contains("papelera"),
                $"y «borrar» también, diciendo los que hay ({Recorte(tras.Texto)})");

            var margen = h.Ejecutar(new JsonObject { ["margen_disco_mb"] = 3, ["confirmar"] = true });
            Program.Assert(margen.EsError && margen.Texto.Contains("50"),
                $"y un margen de disco de 3 MB, con el rango en la mano ({Recorte(margen.Texto)})");

            // Un argumento inventado no se traga en silencio: guardar «casi todo» y callar lo que
            // se ha ignorado es la peor forma de contestar a una petición de configuración.
            var raro = h.Ejecutar(new JsonObject { ["tema_oscuro"] = true, ["confirmar"] = true });
            Program.Assert(raro.EsError && raro.Texto.Contains("tema_oscuro"),
                $"y un ajuste que no existe se dice, no se ignora ({Recorte(raro.Texto)})");
        });
    }

    // ── Andamios ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Corre algo con el fichero de ajustes de verdad y lo devuelve como estaba. Sin esto, una
    /// prueba se llevaría por delante las Preferencias de quien la corre.
    /// </summary>
    private static void ConLosAjustesASalvo(Action<Settings> prueba)
    {
        var original = SettingsStore.Load();
        try { prueba(original); }
        finally { SettingsStore.Save(original); }
    }

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 90 ? l[..90] + "…" : l;
}
