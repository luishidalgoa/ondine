using System.Text.Json.Serialization;
using Ondine.Ia;

namespace Ondine.Peliculas;

/// <summary>
/// Si se sale a internet a identificar películas, y con qué clave.
///
/// <para>
/// <b>Encendido de fábrica</b> desde el 20 de agosto de 2026. Nació apagado, y
/// se cambió por un motivo concreto: la clave ya la trae la app, así que
/// «apagado» no significaba prudencia, significaba que la función existía y
/// nadie la encontraba —había que saber que TMDb es una cosa, ir a Preferencias
/// y buscarla—. Una función que solo usa quien ya sabía que estaba no sirve de
/// nada.
/// </para>
/// <para>
/// Lo que compensa eso, y <b>tiene que seguir estando</b>: la pantalla de
/// películas dice que se van a buscar en TMDb, identificar es un paso aparte que
/// hay que pulsar —nunca pasa solo al abrir—, y se puede apagar aquí. Si
/// cualquiera de esas tres cosas desaparece, esto se convierte en salir a
/// internet a escondidas con los títulos de lo que hay en el disco de alguien.
/// </para>
/// <para>
/// La clave se guarda <b>cifrada</b> con la protección de datos de Windows y
/// atada a esta cuenta, reutilizando <see cref="Proteccion"/> — el mismo
/// razonamiento que con la clave del modelo: <c>settings.json</c> es un fichero
/// que se copia, se sube a una nube de respaldo y se pega en un informe de fallo
/// sin pensarlo.
/// </para>
/// </summary>
public sealed class AjustesDeTmdb
{
    /// <summary>
    /// Si está apagado no se pregunta nada a nadie, aunque haya clave. Lo que
    /// sigue funcionando sin esto es limpiar el nombre y sacar título y año, que
    /// ya es la mayor parte del trabajo y no sale de la máquina.
    ///
    /// <para>
    /// Ojo con el valor de fábrica: a quien ya tenga Ondine instalada se le
    /// enciende al actualizar, porque su <c>settings.json</c> no trae esta
    /// sección y hereda el valor de aquí. Es deliberado, y es la razón de que el
    /// aviso de la pantalla de películas diga lo que va a pasar: quien nunca abra
    /// Preferencias se enterará ahí.
    /// </para>
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>La clave del usuario, cifrada. En claro no se guarda nunca.</summary>
    public string ClaveCifrada { get; set; } = "";

    /// <summary>La clave en claro, solo para el momento de usarla.</summary>
    public string Clave() => Proteccion.Descifrar(ClaveCifrada);

    /// <summary>Guarda la clave cifrada. Con vacío, la borra y se vuelve a la de la build.</summary>
    public void PonerClave(string? clave) =>
        ClaveCifrada = string.IsNullOrWhiteSpace(clave) ? "" : Proteccion.Cifrar(clave);

    /// <summary>Hay clave propia guardada (sin descifrarla), para poder enseñar «••••••».</summary>
    [JsonIgnore]
    public bool TieneClave => ClaveCifrada.Length > 0;

    /// <summary>La que se va a usar: la del usuario si la puso, y si no la de la build.</summary>
    public ClaveDeTmdb.Elegida ClaveElegida() => ClaveDeTmdb.Actual(Clave());

    /// <summary>
    /// ¿Se puede preguntar? Hace falta que el usuario lo haya encendido <b>y</b>
    /// que haya alguna clave. Las dos cosas: encenderlo sin clave —una build
    /// compilada del repo sin el secreto— tiene que decirlo, no fallar al
    /// primer intento.
    /// </summary>
    [JsonIgnore]
    public bool Listo => Activo && ClaveElegida().Hay;

    public AjustesDeTmdb Clone() => (AjustesDeTmdb)MemberwiseClone();
}
