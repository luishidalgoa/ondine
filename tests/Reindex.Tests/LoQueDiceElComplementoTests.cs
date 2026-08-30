using Ondine.Complementos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La frontera de confianza mientras el complemento corre: lo que dice, y hasta dónde se le cree.
///
/// <para>
/// <b>Un complemento es un proceso de un tercero con los permisos de quien lo instaló.</b> Lo que
/// escriba por su salida no es un dato de la aplicación: es una afirmación de alguien de fuera. Y
/// hasta ahora se le creía entera — decía haber dejado unos ficheros y esos ficheros entraban en
/// el flujo que renombra y mueve.
/// </para>
/// </summary>
public static class LoQueDiceElComplementoTests
{
    public static void Todas()
    {
        Program.Seccion("Lo que dice el complemento, y hasta dónde se le cree");

        LosFicherosQueDiceHaberTraido();
        ElAvanceQueLlega();
        UnaLineaNoPuedeSerInfinita();
        UnComplementoQueMienteNoCuela();
    }

    // ── Lo que dice haber traído ─────────────────────────────────────────────

    /// <summary>
    /// Un complemento dice «he dejado estos ficheros». Solo se le cree lo que esté <b>dentro de la
    /// carpeta que eligió el usuario</b>.
    ///
    /// <para>
    /// Esas rutas no se enseñan y ya: entran en el flujo de Organizar, que <b>renombra y mueve</b>.
    /// Un complemento que contestara con la ruta de un documento tuyo lo metía en la lista de lo
    /// recién descargado, y a partir de ahí se le trataba como a un capítulo más. No hacía falta
    /// ningún fallo del sistema: bastaba con escribir esa línea.
    /// </para>
    /// </summary>
    private static void LosFicherosQueDiceHaberTraido()
    {
        var destino = R("C:", "Descargas", "serie");

        var dice = new List<string>
        {
            R("C:", "Descargas", "serie", "uno.mkv"),
            R("C:", "Descargas", "serie", "temporada 1", "dos.mkv"),
            R("C:", "Documentos", "importante.docx"),
            R("C:", "Descargas", "serie", "..", "..", "otro.mkv"),
            R("C:", "Descargas", "serie-de-otro", "tres.mkv"),
        };

        var buenos = Mensaje.SoloDentroDe(destino, dice);

        Program.Assert(buenos.Count == 2, $"solo pasan los dos que están dentro ({buenos.Count})");
        Program.Assert(buenos.Any(f => f.EndsWith("uno.mkv")), "el de la carpeta");
        Program.Assert(buenos.Any(f => f.EndsWith("dos.mkv")), "y el de una subcarpeta suya");

        Program.Assert(!buenos.Any(f => f.Contains("importante")),
            "un fichero de otra parte del disco no entra por decirlo el complemento");
        Program.Assert(!buenos.Any(f => f.EndsWith("otro.mkv")),
            "ni uno que se sale con «..», que solo se ve resolviendo la ruta");
        Program.Assert(!buenos.Any(f => f.Contains("serie-de-otro")),
            "ni una carpeta que EMPIEZA IGUAL: «serie-de-otro» no está dentro de «serie», y "
            + "comparar sin el separador al final decía que sí");

        // Lo relativo se resuelve contra el DESTINO. Un complemento al que se le dijo «déjalo en
        // X» y contesta «uno.mkv» está diciendo «X/uno.mkv»: es lo que quiere decir cualquiera, y
        // resolverlo contra la carpeta desde la que arrancó la aplicación descartaba ficheros
        // buenos de complementos que no hacían nada malo.
        var relativos = Mensaje.SoloDentroDe(destino, ["uno.mkv", Path.Combine("temporada 1", "dos.mkv")]);
        Program.Assert(relativos.Count == 2,
            $"una ruta relativa se entiende dentro del destino ({string.Join(", ", relativos)})");
        Program.Assert(relativos[0] == Path.Combine(destino, "uno.mkv"),
            $"y queda resuelta, para que quien la use no tenga que adivinar ({relativos[0]})");

        Program.Assert(Mensaje.SoloDentroDe(destino, [Path.Combine("..", "fuera.mkv")]).Count == 0,
            "pero una relativa que se sale sigue saliéndose");

        // Sin carpeta de destino no hay nada que creerle. Pasa al listar, que no descarga: un
        // complemento al que nadie le dijo dónde dejar nada no tiene ficheros que declarar.
        Program.Assert(Mensaje.SoloDentroDe(null, dice).Count == 0,
            "sin destino, no se le cree ninguno");
        Program.Assert(Mensaje.SoloDentroDe("", dice).Count == 0, "ni con el destino vacío");

        // Y lo que no es una ruta no revienta nada.
        var raros = new List<string> { "", "   ", "\0" };
        Program.Assert(Mensaje.SoloDentroDe(destino, raros).Count == 0,
            "lo que no es una ruta se descarta sin más");

        // Un nombre absurdamente largo, en cambio, SÍ pasa — y está bien que pase. La regla es
        // estar dentro del destino, y ese lo está. Que luego no exista en el disco es otra cosa, y
        // no es asunto de esta frontera: aquí se decide qué se le cree, no qué hay.
        Program.Assert(Mensaje.SoloDentroDe(destino, [new('x', 300)]).Count == 1,
            "un nombre disparatado pero dentro pasa: la regla es dónde está, no cómo se llama");
    }

    // ── El avance ────────────────────────────────────────────────────────────

    /// <summary>
    /// El avance que llega, dejado en un número que se puede pintar.
    ///
    /// <para>
    /// El recorte a 0..1 ya estaba. Lo que faltaba es que <c>Math.Clamp</c> <b>no arregla un
    /// NaN</b>: las comparaciones con NaN son todas falsas, así que el recorte lo deja pasar tal
    /// cual y llega a la barra de progreso. Un complemento que dividiera entre cero al calcular su
    /// porcentaje —sin mala idea ninguna— mandaba eso.
    /// </para>
    /// </summary>
    private static void ElAvanceQueLlega()
    {
        Program.Assert(Con(0.5) == 0.5, "un avance normal pasa tal cual");
        Program.Assert(Con(0) == 0 && Con(1) == 1, "y los extremos");

        Program.Assert(Con(-5) == 0, $"lo de debajo de cero se queda en cero ({Con(-5)})");
        Program.Assert(Con(1_000_000) == 1, $"y lo de encima de uno, en uno ({Con(1_000_000)})");

        Program.Assert(Con(double.NaN) == 0,
            $"un NaN se queda en cero: «Math.Clamp» lo deja pasar, que es lo que faltaba ({Con(double.NaN)})");
        Program.Assert(Con(double.PositiveInfinity) == 1,
            $"y el infinito es todo lo que se puede avanzar ({Con(double.PositiveInfinity)})");
        Program.Assert(Con(double.NegativeInfinity) == 0,
            $"y el de al otro lado, nada ({Con(double.NegativeInfinity)})");

        Program.Assert(new Mensaje { Tipo = Mensaje.TipoProgreso }.AvanceSano is null,
            "un progreso sin avance sigue sin avance: no se inventa un cero");
    }

    private static double Con(double avance) =>
        new Mensaje { Tipo = Mensaje.TipoProgreso, Avance = avance }.AvanceSano ?? -1;

    // ── Las líneas ───────────────────────────────────────────────────────────

    /// <summary>
    /// Una línea no puede crecer sin fin.
    ///
    /// <para>
    /// El contrato es «una línea, un mensaje». <c>ReadLineAsync</c> lee hasta el salto de línea, y
    /// si no llega ninguno sigue guardando en memoria: un complemento que escriba sin parar y sin
    /// saltar de línea se lleva la aplicación por delante sin hacer nada ilegal. Con techo, esa
    /// línea se descarta y <b>se sigue leyendo las de después</b>, que es lo que hay que acertar:
    /// tirar el resto convertiría una línea gorda en el final de la descarga.
    /// </para>
    /// </summary>
    private static void UnaLineaNoPuedeSerInfinita()
    {
        var normales = Leer("uno\ndos\ntres\n", 64);
        Program.Assert(normales.Count == 3 && normales[2] == "tres",
            $"las líneas normales llegan enteras y en orden ({string.Join("|", normales)})");

        var conGorda = Leer("uno\n" + new string('x', 500) + "\ndos\n", 64);
        Program.Assert(conGorda.Count == 2,
            $"la que se pasa del techo se descarta ({conGorda.Count}: {string.Join("|", conGorda.Select(Corto))})");
        Program.Assert(conGorda[0] == "uno" && conGorda[1] == "dos",
            "y las de antes y después llegan: una línea gorda no termina la descarga");

        // Justo en el techo entra; uno más, no. El borde importa porque es donde se cuela un
        // «fuera por uno» que nadie ve.
        Program.Assert(Leer(new string('a', 64) + "\n", 64).Count == 1, "una línea de justo el techo entra");
        Program.Assert(Leer(new string('a', 65) + "\n", 64).Count == 0, "y una de uno más, no");

        // Sin salto final: lo último también cuenta como línea.
        var sinSalto = Leer("uno\ndos", 64);
        Program.Assert(sinSalto.Count == 2 && sinSalto[1] == "dos",
            $"lo último llega aunque no traiga salto de línea ({string.Join("|", sinSalto)})");

        // Y el retorno de carro de Windows no se cuela dentro del mensaje.
        var conRetorno = Leer("uno\r\ndos\r\n", 64);
        Program.Assert(conRetorno.Count == 2 && conRetorno[0] == "uno",
            $"el retorno de carro no se queda pegado al mensaje ({string.Join("|", conRetorno.Select(Corto))})");
    }

    // ── De extremo a extremo ─────────────────────────────────────────────────

    /// <summary>
    /// Un complemento de verdad que <b>miente</b>, arrancado por el invocador.
    ///
    /// <para>
    /// Lo de arriba comprueba la regla; esto comprueba que la regla está <b>puesta en el camino</b>.
    /// Y hacía falta: el filtro podría estar perfecto en su función y no llamarse desde donde los
    /// mensajes entran en la aplicación — que es justo lo que pasaba, porque las dos pantallas
    /// añadían la lista tal cual.
    /// </para>
    /// </summary>
    private static void UnComplementoQueMienteNoCuela()
    {
        var raiz = Path.Combine(Path.GetTempPath(), "ondine-miente-" + Guid.NewGuid().ToString("N")[..8]);
        var plug = Path.Combine(raiz, "plug");
        var destino = Path.Combine(raiz, "destino");
        Directory.CreateDirectory(plug);
        Directory.CreateDirectory(destino);
        try
        {
            var fuera = Path.Combine(raiz, "importante.docx");
            File.WriteAllText(fuera, "un documento tuyo");
            File.WriteAllText(Path.Combine(destino, "bueno.mkv"), "x");

            File.WriteAllText(Path.Combine(plug, "plugin.json"), """
            { "nombre": "Mentiroso", "version": "1.0.0", "contrato": 1,
              "ejecutable": "m.cmd", "capacidades": ["importar", "descargar"] }
            """);

            // Contesta con DOS ficheros: uno que está en el destino y otro que no. El de fuera
            // llega por su ruta entera, que es el caso que importa.
            var json = "{\"tipo\":\"hecho\",\"ficheros\":[\"bueno.mkv\",\"%R%\"]}"
                .Replace("%R%", fuera.Replace(@"\", @"\\"));

            File.WriteAllText(Path.Combine(plug, "m.cmd"),
                "@echo off\r\necho " + json.Replace("\"", "^\"") + "\r\n");
            File.WriteAllText(Path.Combine(plug, "m.sh"),
                "#!/bin/sh\ncat <<'FIN'\n" + json + "\nFIN\n");

            var c = Complemento.Leer(Path.Combine(plug, "plugin.json"));
            if (c is null) { Program.Assert(false, "el complemento se lee"); return; }
            if (c.Reparo() is { } r) { Program.Assert(false, $"el complemento arranca ({r})"); return; }

            var dijo = new List<Mensaje>();
            Task.Run(async () =>
            {
                await foreach (var m in Invocador.CorrerAsync(
                    c, Invocador.ComandoTraer, ["--destino", destino], default, null, destino))
                    dijo.Add(m);
            }).GetAwaiter().GetResult();

            var hecho = dijo.FirstOrDefault(m => m.Tipo == Mensaje.TipoHecho);
            Program.Assert(hecho is not null,
                $"contesta con un «hecho» ({string.Join(" | ", dijo.Select(m => m.Tipo + " " + m.MensajeError))})");
            if (hecho is null) return;

            Program.Assert(hecho.Ficheros.Count == 1,
                $"solo se le cree uno de los dos ({string.Join(", ", hecho.Ficheros)})");
            Program.Assert(hecho.Ficheros[0] == Path.Combine(destino, "bueno.mkv"),
                $"y es el que está donde se le dijo ({hecho.Ficheros[0]})");
            Program.Assert(!hecho.Ficheros.Any(f => f.EndsWith(".docx")),
                "el documento de fuera no entra: de aquí se va al flujo que renombra y mueve");
            Program.Assert(File.Exists(fuera), "y sigue donde estaba, sin tocar");
        }
        finally { try { Directory.Delete(raiz, recursive: true); } catch { } }
    }

    private static List<string> Leer(string texto, int techo)
    {
        var fuera = new List<string>();
        Task.Run(async () =>
        {
            using var lector = new StringReader(texto);
            await foreach (var l in Invocador.LineasConTecho(lector, techo)) fuera.Add(l);
        }).GetAwaiter().GetResult();
        return fuera;
    }

    private static string Corto(string s) => s.Length > 30 ? s[..30] + "…" : s;

    /// <summary>Una ruta del sistema donde corra la prueba, escrita a trozos.</summary>
    private static string R(params string[] trozos) =>
        OperatingSystem.IsWindows()
            ? Path.Combine(trozos)
            : Path.Combine(new[] { Path.DirectorySeparatorChar.ToString() }
                .Concat(trozos.Skip(1)).ToArray());
}
