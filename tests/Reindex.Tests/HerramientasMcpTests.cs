using System.Text.Json.Nodes;
using Ondine.Mcp;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El servidor MCP: las herramientas que un agente puede llamar.
///
/// <para>
/// Se prueban <b>ejecutándolas</b>, con una carpeta de verdad y un catálogo de verdad, no
/// mirando el código. Es la lección que dejó la auditoría de la interfaz: las comprobaciones
/// que preguntaban por el estado del modelo pasaban con el fallo puesto, porque el paso roto
/// era justo el que no daban. Aquí el paso es «llamar a la herramienta», así que se llama.
/// </para>
/// <para>
/// Y lo que se vigila son <b>las tres reglas</b>, que son las que hacen que dejar esto en manos
/// de un agente no dé miedo: analizar no toca nada, lo que escribe pide permiso, y lo borrado
/// va a la papelera.
/// </para>
/// </summary>
public static class HerramientasMcpTests
{
    public static void Todas()
    {
        Program.Seccion("MCP: las herramientas del agente");

        CadaUnaSePresenta();
        LaQueEscribePideConfirmar();
        AnalizarNoTocaNada();
        LaExtensionNoSeDuplica();
        SinConfirmarNoEscribeNada();
        ConConfirmarSoloLoSeguro();
        LaPapeleraTambienPidePermiso();
        ElMotorNecesitaGlobalizacion();
    }

    // ── Lo que el agente lee antes de llamar ─────────────────────────────────

    /// <summary>
    /// El esquema y la descripción son lo ÚNICO que ve el agente antes de decidir. Una
    /// herramienta sin descripción no es una herramienta a medias: es una que se llamará mal.
    /// </summary>
    private static void CadaUnaSePresenta()
    {
        Program.Assert(Catalogo.Todas.Count > 0, "hay herramientas que ofrecer");

        foreach (var h in Catalogo.Todas)
        {
            var bien = h.Nombre.StartsWith("ondine_")
                    && h.Descripcion.Length > 30
                    && h.Esquema["type"]?.ToString() == "object"
                    && h.Esquema["properties"] is JsonObject;

            Program.Assert(bien, $"«{h.Nombre}» se presenta con nombre, descripción y esquema");
        }

        var repetidos = Catalogo.Todas.GroupBy(h => h.Nombre).Where(g => g.Count() > 1).ToList();
        Program.Assert(repetidos.Count == 0,
            $"ningún nombre repetido ({string.Join(", ", repetidos.Select(g => g.Key))})");
    }

    /// <summary>
    /// La segunda regla, comprobada en el esquema y no en la prosa: si una herramienta escribe,
    /// <c>confirmar</c> tiene que estar declarado. Un argumento que la herramienta lee pero no
    /// anuncia no existe para el agente, y entonces el permiso no se pide nunca — se asume.
    /// </summary>
    private static void LaQueEscribePideConfirmar()
    {
        foreach (var h in Catalogo.Todas.Where(h => h.Escribe))
        {
            var props = h.Esquema["properties"] as JsonObject;
            Program.Assert(props?["confirmar"] is not null,
                $"«{h.Nombre}» escribe, así que declara «confirmar» en su esquema");
        }

        Program.Assert(Catalogo.Todas.Any(h => h.Escribe) && Catalogo.Todas.Any(h => !h.Escribe),
            "hay de las dos clases: si todas fueran iguales, esta prueba no diría nada");
    }

    // ── Las tres reglas, ejecutándolas ───────────────────────────────────────

    /// <summary>
    /// Regla uno. Analizar <b>propone</b>: después de analizar, la carpeta tiene que estar
    /// exactamente como estaba. Se compara la lista de nombres, que es lo que rompería un
    /// renombrado accidental.
    /// </summary>
    private static void AnalizarNoTocaNada()
    {
        using var casa = new Casa();

        var antes = casa.Nombres();
        var r = casa.Llamar("ondine_analizar");

        Program.Assert(!r.EsError, $"analizar contesta sin error ({Recorte(r.Texto)})");
        Program.Assert(casa.Nombres().SequenceEqual(antes), "analizar no ha tocado un solo fichero");
        Program.Assert(r.Texto.Contains("Limpio"), "y dice qué ha encontrado");
    }

    /// <summary>
    /// La extensión, UNA vez.
    ///
    /// <para>
    /// Esto salió de correr el servidor a mano contra una carpeta de prueba: los destinos
    /// llegaban como <c>… - S1E1 - El primero.mkv.mkv</c>. <see cref="LibraryTemplate.Render"/>
    /// ya devuelve el nombre <b>con</b> su extensión —lo hace justamente para eso, recibe el
    /// <c>FileSignals</c>— y aquí se le añadía otra encima.
    /// </para>
    /// <para>
    /// Compilaba, no daba error y el renombrado «funcionaba»: dejaba la biblioteca entera con
    /// dos extensiones. La clase de fallo que solo se ve mirando el resultado.
    /// </para>
    /// </summary>
    private static void LaExtensionNoSeDuplica()
    {
        using var casa = new Casa();

        var texto = casa.Llamar("ondine_analizar").Texto;
        var dobles = texto.Split('\n')
            .Where(l => l.Contains(".mkv.mkv") || l.Contains(".mp4.mp4"))
            .ToList();

        Program.Assert(dobles.Count == 0,
            $"ningún destino lleva la extensión dos veces ({dobles.Count}: {Recorte(string.Join(" | ", dobles))})");
    }

    /// <summary>
    /// Regla dos, la mitad importante: sin <c>confirmar</c> <b>no se escribe</b>. Y no se
    /// contesta con un «hace falta confirmar» a secas: se contesta la lista entera de lo que
    /// haría, que es lo que permite dar el permiso con conocimiento.
    /// </summary>
    private static void SinConfirmarNoEscribeNada()
    {
        using var casa = new Casa();

        var antes = casa.Nombres();
        var r = casa.Llamar("ondine_aplicar_renombrado");

        Program.Assert(casa.Nombres().SequenceEqual(antes), "sin confirmar no se ha renombrado nada");
        Program.Assert(!r.EsError, "y no es un error: es la respuesta útil");
        Program.Assert(r.Texto.Contains("SIN CONFIRMAR"), "lo dice claro");
        Program.Assert(r.Texto.Contains("confirmar"), "y dice cómo darle permiso");
        Program.Assert(r.Texto.Contains("El primero"), "enseñando la lista de lo que haría");
    }

    /// <summary>
    /// Regla dos, la otra mitad: con permiso renombra — <b>y solo lo seguro</b>. La fila en
    /// conflicto se queda donde está, igual que en la ventana: las dudas no se aplican en
    /// bloque, y quien decide es una persona.
    /// </summary>
    private static void ConConfirmarSoloLoSeguro()
    {
        using var casa = new Casa();

        var r = casa.Llamar("ondine_aplicar_renombrado", ("confirmar", true));
        var despues = casa.Nombres();

        Program.Assert(!r.EsError, $"aplicar contesta sin error ({Recorte(r.Texto)})");

        Program.Assert(despues.Any(n => n.StartsWith("Ondine Demo - S1E1 -")),
            $"el limpio se ha renombrado ({string.Join(" · ", despues)})");
        Program.Assert(despues.Contains("cosa rara sin nada que ver.mkv"),
            "y la duda se ha quedado tal cual: eso se resuelve con una persona delante");

        Program.Assert(despues.Count == casa.Nombres().Count && despues.Count == 3,
            $"ni un fichero de más ni de menos ({despues.Count})");
    }

    /// <summary>
    /// Regla tres. La papelera también pide permiso, y sin él el fichero sigue donde estaba.
    ///
    /// <para>
    /// No se prueba el borrado <i>con</i> permiso: mandaría algo a la papelera real de quien
    /// corra las pruebas. Lo que sí se comprueba es que el camino sin permiso no toca el disco,
    /// que es la mitad que puede hacer daño.
    /// </para>
    /// </summary>
    private static void LaPapeleraTambienPidePermiso()
    {
        using var casa = new Casa();

        var victima = Path.Combine(casa.Videos, "cosa rara sin nada que ver.mkv");
        var r = Ejecutar("ondine_a_la_papelera", new JsonObject { ["ruta"] = victima });

        Program.Assert(File.Exists(victima), "sin confirmar, el fichero sigue donde estaba");
        Program.Assert(r.Texto.Contains("SIN CONFIRMAR"), "y lo dice");
    }

    // ── Y lo que el motor necesita del proyecto que lo hospeda ───────────────

    /// <summary>
    /// El motor <b>necesita</b> globalización, y esto lo descubrió una prueba a mano.
    ///
    /// <para>
    /// El proyecto del MCP nació con <c>InvariantGlobalization=true</c> —parecía inofensivo en
    /// un servidor sin interfaz— y el mismo fichero contra el mismo catálogo pasó de
    /// <b>100 %</b> a <b>81 %</b> de parecido. La razón está abajo: la comparación de títulos
    /// descompone en Unicode para que «nino» case con «niño», y en modo invariante esa
    /// descomposición deja de plegar los acentos.
    /// </para>
    /// <para>
    /// Un 19 % no es cosmético: es la diferencia entre una fila verde que se aplica sola y una
    /// duda que hay que resolver a mano, o —peor— entre el episodio correcto y el de al lado.
    /// Y no habría dado ningún error: solo peores resultados, en silencio.
    /// </para>
    /// </summary>
    private static void ElMotorNecesitaGlobalizacion()
    {
        // Primero, POR QUÉ importa, en ejecutable: el plegado de acentos es lo que se pierde.
        Program.Assert(Math.Abs(TitleMatch.SimRaw("El nino grunon", "El niño gruñón") - 1.0) < 0.001,
            "el motor pliega los acentos: «nino» y «niño» son el mismo título");

        // Y ahora, que ningún proyecto que use el motor se lo quite. Ausente vale: el valor por
        // defecto ya es «false». Lo que no vale es ponerlo a «true» a propósito.
        var raiz = LocalizarRaiz();
        var proyectos = Directory.GetFiles(Path.Combine(raiz, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(raiz, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Where(f => File.ReadAllText(f).Contains("Ondine.Core.csproj"))
            .ToList();

        Program.Assert(proyectos.Count >= 4, $"encuentro los proyectos que usan el motor ({proyectos.Count})");

        foreach (var f in proyectos)
        {
            var texto = File.ReadAllText(f);
            var invariante = texto.Contains("<InvariantGlobalization>true<");
            Program.Assert(!invariante,
                $"{Path.GetFileNameWithoutExtension(f)} no le quita la globalización al motor");
        }
    }

    // ── Andamios ─────────────────────────────────────────────────────────────

    private static string Recorte(string s) =>
        s.Replace('\n', ' ') is var l && l.Length > 90 ? l[..90] + "…" : l;

    private static Resultado Ejecutar(string nombre, JsonObject argumentos) =>
        Catalogo.Todas.First(h => h.Nombre == nombre).Ejecutar(argumentos);

    /// <summary>
    /// Una casa de pruebas: carpeta con tres vídeos y un catálogo que los explica. Dos casan
    /// limpio, y el tercero entra en conflicto con el primero a propósito — sin una duda en la
    /// mesa no se puede comprobar que las dudas se respetan.
    /// </summary>
    private sealed class Casa : IDisposable
    {
        private readonly string _raiz;
        public string Videos { get; }
        private string Catalogo { get; }

        public Casa()
        {
            _raiz = Path.Combine(Path.GetTempPath(), "ondine-mcp-" + Guid.NewGuid().ToString("N")[..8]);
            Videos = Path.Combine(_raiz, "videos");
            Catalogo = Path.Combine(_raiz, "catalogo.json");
            Directory.CreateDirectory(Videos);

            File.WriteAllText(Catalogo, """
            {
              "esquema": "reindex/1.0",
              "serie": "Ondine Demo",
              "clave": "demo",
              "total": 2,
              "episodios": [
                {"num": 1, "temporada": 1, "fecha": "2001-01-05", "titulos": {"es": ["El primero"]}},
                {"num": 2, "temporada": 1, "fecha": "2001-01-12", "titulos": {"es": ["El segundo"]}}
              ]
            }
            """, System.Text.Encoding.UTF8);

            foreach (var n in new[]
            {
                "Ondine Demo 1x01 El primero.mkv",
                "Ondine.Demo.S01E02.El.segundo.mp4",
                "cosa rara sin nada que ver.mkv",
            })
                File.WriteAllText(Path.Combine(Videos, n), "x");
        }

        public List<string> Nombres() =>
            Directory.GetFiles(Videos).Select(Path.GetFileName).OrderBy(n => n).ToList()!;

        public Resultado Llamar(string herramienta, params (string Clave, bool Valor)[] banderas)
        {
            var a = new JsonObject { ["carpeta"] = Videos, ["catalogo"] = Catalogo };
            foreach (var (clave, valor) in banderas) a[clave] = valor;
            return Ejecutar(herramienta, a);
        }

        public void Dispose()
        {
            try { Directory.Delete(_raiz, true); } catch { /* si no se puede, es basura en el temporal */ }
        }
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }
}
