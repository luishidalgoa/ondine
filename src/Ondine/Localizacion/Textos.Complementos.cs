namespace Ondine.Localizacion;

/// <summary>
/// Textos de los complementos: lo que se enseña al descubrirlos, y sobre todo
/// los motivos por los que uno queda descartado.
///
/// <para>
/// Esos motivos importan más de lo que parece. Un complemento que no aparece y
/// no dice por qué es peor que uno que no está: quien lo instaló se queda
/// mirando una lista vacía sin nada que corregir.
/// </para>
/// </summary>
public sealed partial class Textos
{
    public string ComplementoSinNombre => Idioma.Elegir(
        "The manifest has no name",
        "El manifiesto no tiene nombre");

    public string ComplementoSinEjecutable => Idioma.Elegir(
        "The manifest does not say which program to run",
        "El manifiesto no dice qué programa hay que ejecutar");

    // {0} = la versión que declara, {1} = la que esta aplicación entiende.
    public string ComplementoContratoAjeno => Idioma.Elegir(
        "It speaks contract version {0} and this version of Ondine speaks {1}",
        "Habla la versión {0} del contrato y esta versión de Ondine habla la {1}");

    public string ComplementoSinCapacidades => Idioma.Elegir(
        "It declares nothing it can do",
        "No declara nada que sepa hacer");

    public string ComplementoEjecutableFuera => Idioma.Elegir(
        "Its program points outside its own folder. A plugin only runs what it brings with it",
        "Su programa apunta fuera de su propia carpeta. Un complemento solo ejecuta lo que trae dentro");

    public string ComplementoManifiestoIlegible => Idioma.Elegir(
        "Its plugin.json cannot be read: it is not valid JSON",
        "Su plugin.json no se puede leer: no es JSON válido");

    // {0} = el nombre del complemento, {1} = lo que dijo el sistema.
    public string ComplementoNoArranca => Idioma.Elegir(
        "\"{0}\" could not be started: {1}",
        "No se ha podido arrancar «{0}»: {1}");

    // {0} = el nombre, {1} = el código de salida, {2} = lo que escribió por su
    // salida de errores, recortado.
    public string ComplementoSalidaMala => Idioma.Elegir(
        "\"{0}\" stopped with code {1} without explaining itself. {2}",
        "«{0}» ha terminado con el código {1} sin explicarse. {2}");

    // {0} = la ruta declarada, tal cual la escribió el manifiesto.
    public string ComplementoEjecutableNoEsta => Idioma.Elegir(
        "Its program is not there: \"{0}\"",
        "Su programa no está: «{0}»");
}
