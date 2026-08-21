using Ondine.Ia;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// Por aquí, y solo por aquí, un complemento puede preguntarle al modelo.
///
/// <para>
/// <b>El complemento nunca ve la clave.</b> Podría habérsele pasado la dirección
/// y la clave por una variable de entorno y que llamara él —es más simple—, pero
/// eso es entregarle a un programa de fuera la cuenta de quien lo instaló. Aquí
/// el complemento manda una pregunta por su salida, Ondine llama, y le devuelve
/// <b>solo el texto</b> por su entrada. Ni la clave, ni la dirección, ni el
/// nombre del modelo cruzan el puente.
/// </para>
/// <para>
/// Tres puertas, en este orden, y todas antes de llamar a nadie:
/// <list type="number">
///   <item>¿tiene permiso ESTE complemento? (se da a mano, uno a uno)</item>
///   <item>¿hay un modelo configurado y encendido?</item>
///   <item>¿cabe la pregunta, y queda cupo?</item>
/// </list>
/// El cupo existe porque esto cuesta dinero de verdad: sin él, un complemento
/// con un bucle mal escrito —o escrito a mala idea— vacía el saldo de alguien
/// mientras mira una barra de progreso.
/// </para>
/// </summary>
public sealed class PuenteDelModelo
{
    /// <summary>
    /// Cuántas preguntas puede hacer un complemento en UNA ejecución. No es un
    /// número calculado: es el orden de magnitud de «unos cuantos casos raros de
    /// una lista», y muy por debajo de «una por elemento», que es justo lo que
    /// no se quiere permitir.
    /// </summary>
    public const int MaxPreguntas = 40;

    /// <summary>Lo más larga que puede ser una pregunta. Lo que se cobra son caracteres.</summary>
    public const int MaxCaracteres = 8000;

    private readonly AjustesDeModelo _ajustes;
    private readonly bool _permitido;
    private readonly Func<string, CancellationToken, Task<ModeloConectado.Contestacion>> _preguntar;

    /// <summary>Cuántas preguntas se han llegado a hacer de verdad.</summary>
    public int Gastadas { get; private set; }

    /// <param name="permitido">
    /// Si ESTE complemento tiene permiso. Va como parámetro y no se consulta aquí
    /// dentro: quien construye el puente ya sabe de qué complemento habla, y
    /// pasarle el identificador para que lo mire otra vez es una oportunidad más
    /// de mirarlo mal.
    /// </param>
    /// <param name="preguntar">
    /// Cómo se le pregunta al modelo. Se puede sustituir para poder comprobar las
    /// tres puertas sin red: una comprobación de que un complemento sin permiso
    /// no gasta tu saldo no puede depender de que haya servidor.
    /// </param>
    public PuenteDelModelo(
        AjustesDeModelo ajustes,
        bool permitido,
        Func<string, CancellationToken, Task<ModeloConectado.Contestacion>>? preguntar = null)
    {
        _ajustes = ajustes;
        _permitido = permitido;
        _preguntar = preguntar ?? RealAsync;
    }

    // La instrucción de sistema la pone Ondine, no el complemento: es lo que
    // evita que un manifiesto convierta el modelo en otra cosa. Y pide brevedad
    // porque lo que se haga con la respuesta hay que comprobarlo después contra
    // el catálogo, y una parrafada no se puede comprobar.
    private async Task<ModeloConectado.Contestacion> RealAsync(string pregunta, CancellationToken corte) =>
        await ModeloConectado.PreguntarAsync(
            _ajustes, Textos.Instancia.IaSistemaComplemento, pregunta, corte: corte)
            .ConfigureAwait(false);

    /// <summary>
    /// Contesta a una pregunta del complemento. <b>Siempre</b> devuelve un
    /// mensaje, también cuando la respuesta es que no: un complemento que se
    /// queda esperando una línea que no llega parece colgado.
    /// </summary>
    public async Task<Mensaje> ResponderAsync(Mensaje pregunta, CancellationToken corte = default)
    {
        Mensaje No(string motivo) => new()
        {
            Tipo = Mensaje.TipoRespuesta,
            Id = pregunta.Id,
            MensajeError = motivo,
        };

        if (!_permitido) return No(Textos.Instancia.IaComplementoSinPermiso);
        if (!_ajustes.Listo) return No(Textos.Instancia.IaComplementoSinModelo);

        var texto = (pregunta.Texto ?? "").Trim();
        if (texto.Length == 0) return No(Textos.Instancia.IaComplementoPreguntaVacia);
        if (texto.Length > MaxCaracteres)
            return No(string.Format(Textos.Instancia.IaComplementoPreguntaLarga, MaxCaracteres));
        if (Gastadas >= MaxPreguntas)
            return No(string.Format(Textos.Instancia.IaComplementoCupo, MaxPreguntas));

        Gastadas++;
        var r = await _preguntar(texto, corte).ConfigureAwait(false);

        // Si falló, el complemento recibe el motivo tal cual lo dio la llamada.
        // Ese motivo puede traer el cuerpo del error del servidor, pero nunca la
        // clave: la clave solo va en una cabecera, y no se devuelve.
        return r.Texto is { } t
            ? new Mensaje { Tipo = Mensaje.TipoRespuesta, Id = pregunta.Id, Texto = t }
            : No(r.Error ?? Textos.Instancia.IaRespuestaIlegible);
    }
}
