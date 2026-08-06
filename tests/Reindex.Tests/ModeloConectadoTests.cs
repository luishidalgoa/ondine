using Ondine.Ia;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La conexión a un modelo por API: a dónde se llama, qué se lee de la respuesta
/// y —sobre todo— a dónde NO se manda la clave.
///
/// <para>
/// Aquí no se habla con ningún servidor. Lo que se prueba es lo que se puede
/// equivocar sin que nadie lo note: una URL que quien la pega escribe de cinco
/// formas distintas, una respuesta a la que le falta un campo, y una clave que
/// se va por un canal sin cifrar.
/// </para>
/// </summary>
public static class ModeloConectadoTests
{
    public static void Todas()
    {
        Program.Seccion("Conexión a un modelo por API");

        Direcciones();
        Respuestas();
        DondeNoVaLaClave();
        LaClaveGuardada();
    }

    // ── La clave, cifrada de verdad y de vuelta ──
    private static void LaClaveGuardada()
    {
        var a = new AjustesDeModelo();
        Program.Assert(!a.TieneClave, "recién hecho no hay clave");
        Program.Eq("", a.Clave(), "y pedirla da vacío, no null");

        // Solo en Windows: la protección de datos es suya. Fuera, la clase se
        // niega a guardar en vez de escribirla en claro, y eso también se dice.
        if (!OperatingSystem.IsWindows())
        {
            a.PonerClave("sk-lo-que-sea");
            Program.Assert(!a.TieneClave,
                "fuera de Windows no se guarda: antes eso que dejarla en claro");
            return;
        }

        a.PonerClave("sk-una-clave-de-prueba-áéñ");
        Program.Assert(a.TieneClave, "ahora sí hay clave");
        Program.Eq("sk-una-clave-de-prueba-áéñ", a.Clave(), "y vuelve tal cual, acentos incluidos");

        // Lo que de verdad importa: que lo que se escribe en settings.json NO
        // sea la clave. Si esto pasara sin que el cifrado funcione, el test
        // estaría verde y la clave en claro en disco.
        Program.Assert(!a.ClaveCifrada.Contains("sk-una", StringComparison.Ordinal),
            "lo guardado no contiene la clave");

        a.PonerClave(null);
        Program.Assert(!a.TieneClave && a.Clave() == "", "y se puede olvidar");

        // Un valor corrupto -settings.json editado a mano, copiado de otra
        // máquina- no debe reventar: da vacío y la app pide la clave otra vez.
        a.ClaveCifrada = "esto no es base64 válido ni cifrado";
        Program.Eq("", a.Clave(), "una clave ilegible da vacío, no una excepción");
    }

    // ── La URL, escrita como cada uno se la encuentra ──
    private static void Direcciones()
    {
        // Los tres sitios de donde sale una URL de estas: la documentación de
        // OpenAI dice «/v1», la de Ollama dice «:11434/v1», y quien copia del
        // ejemplo de curl se trae la ruta entera del endpoint.
        Program.Eq("https://api.openai.com/v1/chat/completions",
            ModeloConectado.Endpoint("https://api.openai.com/v1"), "la base tal cual");
        Program.Eq("https://api.openai.com/v1/chat/completions",
            ModeloConectado.Endpoint("https://api.openai.com/v1/"), "con barra al final");
        Program.Eq("https://api.openai.com/v1/chat/completions",
            ModeloConectado.Endpoint("  https://api.openai.com/v1  "), "con espacios alrededor");

        // Pegar el endpoint entero es lo que hace cualquiera que copie del ejemplo
        // de curl. Concatenar sin mirar daría «/chat/completions/chat/completions».
        Program.Eq("https://api.openai.com/v1/chat/completions",
            ModeloConectado.Endpoint("https://api.openai.com/v1/chat/completions"),
            "si ya trae el endpoint, no se duplica");

        Program.Eq("http://localhost:11434/v1/chat/completions",
            ModeloConectado.Endpoint("http://localhost:11434/v1"), "un modelo local");

        // Sin «/v1» no se inventa nada: añadirlo a ciegas rompe con cualquier
        // servidor que sirva la API en la raíz, y el error saldría como «404»
        // sin que se entienda de dónde vino ese trozo de ruta.
        Program.Eq("https://algo.interno/chat/completions",
            ModeloConectado.Endpoint("https://algo.interno"), "sin /v1 no se añade a ciegas");

        Program.Eq(null, ModeloConectado.Endpoint(""), "sin dirección no hay endpoint");
        Program.Eq(null, ModeloConectado.Endpoint("   "), "ni con espacios");
        Program.Eq(null, ModeloConectado.Endpoint("no es una url"), "ni con algo que no lo es");
    }

    // ── Lo que se lee de la respuesta ──
    private static void Respuestas()
    {
        Program.Eq("Hola", ModeloConectado.LeerRespuesta(
            """{"choices":[{"message":{"role":"assistant","content":"Hola"}}]}"""),
            "el contenido del primer mensaje");

        // Todo lo demás es null, no excepción. Un modelo que contesta raro no
        // debe tumbar la pantalla desde la que se le preguntó.
        Program.Eq(null, ModeloConectado.LeerRespuesta("""{"choices":[]}"""), "sin opciones, nada");
        Program.Eq(null, ModeloConectado.LeerRespuesta("""{"error":{"message":"no"}}"""), "un error, nada");
        Program.Eq(null, ModeloConectado.LeerRespuesta("esto no es json"), "algo que no es JSON, nada");
        Program.Eq(null, ModeloConectado.LeerRespuesta(""), "vacío, nada");
        Program.Eq(null, ModeloConectado.LeerRespuesta(
            """{"choices":[{"message":{"role":"assistant"}}]}"""), "sin contenido, nada");
    }

    // ── A dónde NO va la clave ──
    private static void DondeNoVaLaClave()
    {
        // Mandar una clave de API por http:// la deja legible para cualquiera en
        // el camino. Con un servidor de casa no hay camino que valga -no sale de
        // la máquina-, y exigir HTTPS ahí obligaría a montar certificados para
        // usar un modelo local, así que se distingue.
        Program.Assert(ModeloConectado.PuedeLlevarClave("https://api.openai.com/v1"),
            "por HTTPS sí");
        Program.Assert(!ModeloConectado.PuedeLlevarClave("http://api.openai.com/v1"),
            "por HTTP a un servidor de fuera NO");
        Program.Assert(ModeloConectado.PuedeLlevarClave("http://localhost:11434/v1"),
            "a localhost sí, aunque sea HTTP: no sale de la máquina");
        Program.Assert(ModeloConectado.PuedeLlevarClave("http://127.0.0.1:11434/v1"),
            "y a 127.0.0.1 igual");
        Program.Assert(ModeloConectado.PuedeLlevarClave("http://[::1]:11434/v1"),
            "y a ::1, que es lo mismo en IPv6");

        // Una máquina de la red de casa NO es la propia máquina: ahí sí hay
        // camino, aunque sea corto.
        Program.Assert(!ModeloConectado.PuedeLlevarClave("http://192.168.1.40:11434/v1"),
            "otra máquina de la red no cuenta como local");

        Program.Assert(!ModeloConectado.PuedeLlevarClave(""), "y sin dirección, no");
    }
}
