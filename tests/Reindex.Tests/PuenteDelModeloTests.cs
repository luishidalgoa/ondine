using Ondine.Complementos;
using Ondine.Ia;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El puente por el que un complemento puede preguntarle al modelo.
///
/// <para>
/// Lo que se prueba aquí es todo lo que pasa <b>antes</b> de llamar a nadie: el
/// permiso, el cupo y el tamaño. Son las tres puertas, y las tres tienen que
/// poder comprobarse sin red de por medio —si para verificar que un complemento
/// sin permiso no gasta tu saldo hiciera falta un servidor, no se verificaría—.
/// </para>
/// </summary>
public static class PuenteDelModeloTests
{
    private static AjustesDeModelo Configurado() => new()
    {
        Activo = true,
        BaseUrl = "https://api.example.invalid/v1",
        Modelo = "un-modelo",
    };

    private static Mensaje Pregunta(string texto) => new()
    {
        Tipo = Mensaje.TipoPreguntar,
        Id = "1",
        Texto = texto,
    };

    /// <summary>Un modelo de mentira que contesta al momento. Cuenta las llamadas.</summary>
    private sealed class Falso
    {
        public int Llamadas;
        public Task<ModeloConectado.Contestacion> Responder(string _, CancellationToken __)
        {
            Llamadas++;
            return Task.FromResult(new ModeloConectado.Contestacion("da igual qué", null));
        }
    }

    public static void Todas()
    {
        Program.Seccion("El puente del complemento al modelo");

        SinPermisoNoSePregunta();
        SinModeloTampoco();
        ElCupo();
        ElTamanio();
        LaClaveNoSale();
        LoNormal();
    }

    // ── La primera puerta: el permiso ──
    private static void SinPermisoNoSePregunta()
    {
        var f = new Falso();
        var p = new PuenteDelModelo(Configurado(), permitido: false, preguntar: f.Responder);
        var r = p.ResponderAsync(Pregunta("¿cuál es?")).GetAwaiter().GetResult();

        Program.Eq(Mensaje.TipoRespuesta, r.Tipo,
            "se contesta igualmente, para que el complemento no se quede esperando");
        Program.Assert(r.Texto is null && r.MensajeError != null, "pero con un no, no con una respuesta");
        Program.Eq(0, f.Llamadas, "y no se llama a nadie");
        Program.Eq(0, p.Gastadas, "ni se gasta nada");
    }

    // ── Permiso dado, pero no hay modelo que valga ──
    private static void SinModeloTampoco()
    {
        var f = new Falso();
        var p = new PuenteDelModelo(new AjustesDeModelo(), permitido: true, preguntar: f.Responder);
        var r = p.ResponderAsync(Pregunta("¿cuál es?")).GetAwaiter().GetResult();

        Program.Assert(r.MensajeError != null, "sin modelo configurado no se pregunta");
        Program.Eq(0, f.Llamadas, "ni se llama");
    }

    // ── El cupo: un complemento no puede vaciarte la cuenta ──
    private static void ElCupo()
    {
        var f = new Falso();
        var p = new PuenteDelModelo(Configurado(), permitido: true, preguntar: f.Responder);
        for (int i = 0; i < PuenteDelModelo.MaxPreguntas; i++)
            p.ResponderAsync(Pregunta("hola")).GetAwaiter().GetResult();

        Program.Eq(PuenteDelModelo.MaxPreguntas, p.Gastadas, "se gastan las que hay");
        Program.Eq(PuenteDelModelo.MaxPreguntas, f.Llamadas, "una llamada por cada una");

        var pasada = p.ResponderAsync(Pregunta("una más")).GetAwaiter().GetResult();
        Program.Assert(pasada.MensajeError != null, "la que pasa del cupo se rechaza");
        Program.Eq(PuenteDelModelo.MaxPreguntas, f.Llamadas, "y no llega a llamarse");
    }

    // ── El tamaño: una pregunta enorme cuesta dinero de verdad ──
    private static void ElTamanio()
    {
        var f = new Falso();
        var p = new PuenteDelModelo(Configurado(), permitido: true, preguntar: f.Responder);
        var r = p.ResponderAsync(Pregunta(new string('x', PuenteDelModelo.MaxCaracteres + 1)))
                 .GetAwaiter().GetResult();

        Program.Assert(r.MensajeError != null, "una pregunta demasiado larga no pasa");
        Program.Eq(0, f.Llamadas, "y no se llama");

        // Vacía tampoco: preguntar la nada es gastar por nada.
        Program.Assert(p.ResponderAsync(Pregunta("   ")).GetAwaiter().GetResult().MensajeError != null,
            "y una pregunta vacía tampoco");
        Program.Eq(0, f.Llamadas, "sigue sin llamarse");
    }

    // ── Lo importante de todo esto ──
    private static void LaClaveNoSale()
    {
        var a = Configurado();
        a.ClaveCifrada = "esto-seria-la-clave";

        var f = new Falso();
        var p = new PuenteDelModelo(a, permitido: true, preguntar: f.Responder);
        var r = p.ResponderAsync(Pregunta("hola")).GetAwaiter().GetResult();

        // El complemento recibe SOLO el texto de la respuesta, o un motivo. Nunca
        // la clave, ni la dirección, ni el nombre del modelo: es un programa de
        // fuera, y darle la clave sería regalarle la cuenta de quien lo instaló.
        var serializado = System.Text.Json.JsonSerializer.Serialize(r);
        Program.Assert(!serializado.Contains("esto-seria-la-clave", StringComparison.Ordinal),
            "la respuesta que ve el complemento no lleva la clave");
        Program.Assert(!serializado.Contains("api.example.invalid", StringComparison.Ordinal),
            "ni la dirección del servidor");
        Program.Assert(!serializado.Contains("un-modelo", StringComparison.Ordinal),
            "ni el nombre del modelo");
    }

    // ── Y cuando todo está en orden ──
    private static void LoNormal()
    {
        var f = new Falso();
        var p = new PuenteDelModelo(Configurado(), permitido: true, preguntar: f.Responder);
        var r = p.ResponderAsync(Pregunta("¿de qué episodio es?")).GetAwaiter().GetResult();

        Program.Eq(Mensaje.TipoRespuesta, r.Tipo, "es una respuesta");
        Program.Eq("1", r.Id, "con el mismo id que la pregunta, para poder casarlas");
        Program.Eq("da igual qué", r.Texto, "y trae lo que dijo el modelo");
        Program.Assert(r.MensajeError is null, "sin error");
    }
}
