using System.Text.Json.Serialization;
using Ondine.Ia;

namespace Ondine.Peliculas;

/// <summary>
/// Si se sale a internet a identificar películas, y con qué clave.
///
/// <para>
/// <b>Apagado de fábrica</b>, y no por prudencia decorativa: identificar contra
/// TMDb significa mandar a un servicio de fuera los títulos de lo que hay en el
/// disco de alguien. Una app que ordena tu disco no debería contar qué tienes
/// sin que se lo pidas, así que es el usuario quien lo enciende y puede
/// apagarlo.
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
    /// </summary>
    public bool Activo { get; set; }

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
