using Ondine.Audio;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Qué pistas de audio se quedan, y por qué.
///
/// <para>
/// <b>De dónde sale.</b> Un usuario comprimió un fichero con inglés, castellano y portugués y el
/// registro le dijo «pistas de audio: spa+eng (descarto 1)». Ni cuál era la descartada, ni por
/// qué. La causa era una lista blanca implícita: cuando no hay idiomas elegidos, el motor
/// inventaba <c>{ el preferido, "eng" }</c> — el inglés se cuela por decisión del código, no del
/// usuario, y el portugués se caía sin que nada lo dijera.
/// </para>
/// <para>
/// <b>Y la regla vivía en dos sitios que no se ponían de acuerdo.</b> El motor leía «lista
/// vacía» como «el preferido y el inglés»; el estimador, como «todas». Con ese mismo fichero el
/// pronóstico sumaba tres pistas y el resultado llevaba dos, y ninguna prueba lo cubría: la única
/// mención de <c>KeepLangs</c> en el arnés comprobaba que la cola copiaba la lista.
/// </para>
/// <para>
/// Es la misma medicina que <see cref="PlanDeAudio"/>: la decisión baja al núcleo, en una función
/// pura, y los dos preguntan ahí.
/// </para>
/// </summary>
public static class PistasQueSeQuedanTests
{
    public static void Todas()
    {
        Program.Seccion("Qué pistas de audio se quedan");

        ElCasoDelUsuario();
        LaListaManda();
        DesmarcarElPreferidoLoQuita();
        ElPreferidoVaPrimero();
        TodasCuandoSeDiceTodas();
        ElParacaidas();
        LasPistasSinIdioma();
        ElPronosticoCuentaLoMismoQueElMotor();
        LasDosInterfacesMandanElCentinela();
    }

    /// <summary>
    /// El pronóstico cuenta las MISMAS pistas que el motor va a conservar.
    ///
    /// <para>
    /// Era el segundo defecto y se veía en el panel de estimación: con un fichero de tres idiomas
    /// y sin elegir ninguno, el estimador sumaba las tres pistas de audio —leía «lista vacía»
    /// como «todas»— y el motor guardaba dos. El tamaño previsto no cuadraba con el real y no
    /// había forma de saber por qué.
    /// </para>
    /// </summary>
    private static void ElPronosticoCuentaLoMismoQueElMotor()
    {
        var fila = new VideoRow
        {
            Name = "capitulo.mkv", Bytes = 700L * 1024 * 1024, Probed = true,
            Audio = "eng+spa+por", AudioCodec = "eac3", AudioBitrateKbps = 224, Channels = 2,
            Width = 1920, Height = 1080, Fps = 25, DurationSec = 1400, VideoBitrateKbps = 3800,
        };
        var opciones = new EncodeOptions { Container = "mkv", Lang = "spa" };   // KeepLangs vacía

        var cuantas = PistasQueSeQuedan.Cuantas(["eng", "spa", "por"], "spa", opciones.KeepLangs);
        Program.Assert(cuantas == 2, $"el motor conserva dos de las tres ({cuantas})");

        var e = Estimator.Compute(fila, opciones);
        Program.Assert(e.EstAudioKbps == 224 * cuantas,
            $"y el pronóstico suma esas dos, no las tres ({e.EstAudioKbps} frente a {224 * cuantas})");

        // Y con «todas» sí son tres, para que la prueba de arriba no pase por sumar siempre dos.
        var conTodas = Estimator.Compute(fila, new EncodeOptions
        {
            Container = "mkv", Lang = "spa", KeepLangs = { PistasQueSeQuedan.Todas },
        });
        Program.Assert(conTodas.EstAudioKbps == 224 * 3,
            $"y pidiendo todas, las tres ({conTodas.EstAudioKbps})");
    }

    /// <summary>Tres idiomas, sin elegir ninguno: se queda el preferido y el inglés, y se DICE cuál cae.</summary>
    private static void ElCasoDelUsuario()
    {
        var plan = PistasQueSeQuedan.Para(Pistas(("eng", 1), ("spa", 2), ("por", 3)), "spa", []);

        Program.Assert(Idiomas(plan) == "spa+eng",
            $"sin idiomas elegidos se quedan el preferido y el inglés ({Idiomas(plan)})");

        var caida = plan.Single(p => !p.SeQueda);
        Program.Assert(caida.Idioma == "por", $"y la que cae es el portugués ({caida.Idioma})");
        Program.Assert(caida.Motivo == PorQuePista.NoEstaEnLosElegidos,
            $"con su motivo, que es lo que el registro no decía ({caida.Motivo})");

        // El inglés entra por decisión del CÓDIGO, no del usuario. Merece motivo propio: sin
        // esto, «por qué se ha quedado el inglés que yo no pedí» no tiene respuesta.
        var ingles = plan.Single(p => p.Idioma == "eng");
        Program.Assert(ingles.Motivo == PorQuePista.LaListaPorDefecto,
            $"y el inglés dice que viene de la lista por defecto ({ingles.Motivo})");
    }

    private static void LaListaManda()
    {
        var conPortugues = PistasQueSeQuedan.Para(
            Pistas(("eng", 1), ("spa", 2), ("por", 3)), "spa", ["spa", "eng", "por"]);
        Program.Assert(conPortugues.All(p => p.SeQueda) && conPortugues.Count == 3,
            $"con los tres marcados se quedan los tres ({Idiomas(conPortugues)})");

        var soloDos = PistasQueSeQuedan.Para(
            Pistas(("eng", 1), ("spa", 2), ("por", 3)), "spa", ["spa", "por"]);
        Program.Assert(Idiomas(soloDos) == "spa+por",
            $"y desmarcar el inglés lo quita: ya no se cuela ({Idiomas(soloDos)})");
    }

    /// <summary>
    /// El arreglo del tercer defecto: desmarcar el chip del idioma preferido lo QUITA.
    ///
    /// <para>
    /// Antes el motor calculaba las pistas del preferido sin consultar la lista, así que la
    /// interfaz ofrecía un control que el motor ignoraba justo para ese caso. El idioma preferido
    /// sigue mandando en el ORDEN —va primero y es el que queda por defecto al reproducir—, que
    /// es su otro trabajo, y ese sí se respeta.
    /// </para>
    /// </summary>
    private static void DesmarcarElPreferidoLoQuita()
    {
        var plan = PistasQueSeQuedan.Para(Pistas(("eng", 1), ("spa", 2)), "spa", ["eng"]);

        Program.Assert(Idiomas(plan) == "eng",
            $"desmarcado el castellano y siendo el preferido, se va ({Idiomas(plan)})");
        Program.Assert(plan.Single(p => p.Idioma == "spa").Motivo == PorQuePista.NoEstaEnLosElegidos,
            "y el motivo es que no está entre los elegidos, no una excepción del preferido");
    }

    private static void ElPreferidoVaPrimero()
    {
        // El castellano es la TERCERA del fichero y tiene que salir primera: es la que se lleva
        // el «-disposition default», o sea la que suena al abrir el vídeo.
        var plan = PistasQueSeQuedan.Para(
            Pistas(("eng", 1), ("por", 2), ("spa", 3)), "spa", ["eng", "por", "spa"]);

        Program.Assert(Idiomas(plan) == "spa+eng+por",
            $"el preferido primero, y el resto en el orden del fichero ({Idiomas(plan)})");
        Program.Assert(plan.First().Motivo == PorQuePista.EsElPreferido,
            "y se sabe por qué está primera");
        Program.Assert(plan.Where(p => p.SeQueda).Select(p => p.Indice).SequenceEqual([3, 1, 2]),
            "y se conservan los índices del fichero, que son los que van al -map");
    }

    private static void TodasCuandoSeDiceTodas()
    {
        var plan = PistasQueSeQuedan.Para(
            Pistas(("eng", 1), ("por", 2), ("", 3)), "spa", [PistasQueSeQuedan.Todas]);

        Program.Assert(plan.All(p => p.SeQueda),
            $"«all» conserva todas, incluida la que no trae idioma ({Idiomas(plan)})");
        Program.Assert(plan.All(p => p.Motivo == PorQuePista.TodasSeConservan),
            "y todas con el mismo motivo");

        // Aunque el preferido no esté en el fichero: «todas» es todas.
        Program.Assert(PistasQueSeQuedan.Para(Pistas(("jpn", 1)), "spa", ["all"]).Single().SeQueda,
            "y no depende de que el preferido esté dentro");
    }

    /// <summary>
    /// El paracaídas: si no casa ni un idioma, se conservan TODAS en vez de dejar el vídeo mudo.
    /// Estaba en el motor y no tenía prueba; es la clase de red de la que uno se acuerda el día
    /// que se cae.
    /// </summary>
    private static void ElParacaidas()
    {
        var plan = PistasQueSeQuedan.Para(Pistas(("eng", 1), ("por", 2)), "spa", ["jpn"]);

        Program.Assert(plan.All(p => p.SeQueda),
            $"sin ninguna coincidencia se conservan todas: nunca un vídeo mudo ({Idiomas(plan)})");
        Program.Assert(plan.All(p => p.Motivo == PorQuePista.NingunaCasaba),
            "y el motivo lo distingue de haberlas elegido");
    }

    private static void LasPistasSinIdioma()
    {
        // Sin etiqueta de idioma no hay chip en la interfaz, así que con una lista explícita se
        // caen. Se comprueba para que quede escrito: es el agujero que tapa «conservar todas».
        var plan = PistasQueSeQuedan.Para(Pistas(("spa", 1), ("", 2)), "spa", ["spa"]);
        Program.Assert(Idiomas(plan) == "spa" && !plan.Single(p => p.Idioma == "").SeQueda,
            $"una pista sin idioma no entra por una lista explícita ({Idiomas(plan)})");

        // Y el rótulo del registro la llama «?», no una cadena vacía que no se ve.
        Program.Assert(PistasQueSeQuedan.Rotulo("") == "?" && PistasQueSeQuedan.Rotulo("spa") == "spa",
            "y en el registro sale como «?», que al menos se ve");
    }

    /// <summary>
    /// Y las DOS interfaces mandan el centinela cuando la casilla esta marcada.
    ///
    /// <para>
    /// El centinela «all» existia desde hace tiempo en el motor y solo era alcanzable con
    /// <c>ondine comprimir --idiomas all</c>: una funcion que nadie podia descubrir. Ahora hay
    /// casilla, y esta prueba mira las dos caras porque la trampa de este proyecto es cablear una
    /// y olvidar la otra.
    /// </para>
    /// </summary>
    private static void LasDosInterfacesMandanElCentinela()
    {
        var raiz = LocalizarRaiz();
        var caras = new[]
        {
            Path.Combine(raiz, "src", "Ondine", "MainWindow.xaml.cs"),
            Path.Combine(raiz, "src", "Ondine.Avalonia", "VentanaPrincipal.axaml.cs"),
        };

        foreach (var f in caras)
        {
            var nombre = Path.GetFileName(Path.GetDirectoryName(f)!);
            var texto = File.Exists(f) ? File.ReadAllText(f) : "";
            var sinComentarios = string.Join(" ", texto.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));

            Program.Assert(sinComentarios.Contains("chkTodosIdiomas.IsChecked == true")
                           && sinComentarios.Contains("PistasQueSeQuedan.Todas"),
                $"{nombre} manda «all» cuando la casilla esta marcada");
            Program.Assert(sinComentarios.Contains("SincronizarIdiomasDeAudio"),
                $"{nombre} apaga los chips mientras esta marcada");
        }

        // Y la ayuda de la linea de ordenes lo cuenta: era el otro medio defecto -la funcion
        // existia y su propia ayuda no la mencionaba-.
        var cli = Path.Combine(raiz, "src", "Ondine.Cli", "Program.cs");
        var ayuda = File.Exists(cli) ? File.ReadAllText(cli) : "";
        Program.Assert(ayuda.Contains("--idiomas") && ayuda.Contains("all"),
            "y la ayuda de la CLI documenta el centinela «all»");
    }

    // ── Andamios ─────────────────────────────────────────────────────────────

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }

    private static IReadOnlyList<(int Indice, string? Idioma)> Pistas(params (string Idioma, int Indice)[] p) =>
        [.. p.Select(x => (x.Indice, (string?)x.Idioma))];

    private static string Idiomas(IReadOnlyList<PistaElegida> plan) =>
        string.Join("+", plan.Where(p => p.SeQueda).Select(p => PistasQueSeQuedan.Rotulo(p.Idioma)));
}
