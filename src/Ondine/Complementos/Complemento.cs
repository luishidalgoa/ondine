using System.IO;
using Ondine.Localizacion;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ondine.Complementos;

/// <summary>
/// Un complemento: un programa de fuera que Ondine sabe llamar.
///
/// <para>
/// <b>Por qué procesos y no ensamblados cargados dentro.</b> Un sistema de
/// complementos en proceso pide contratos versionados, contextos de carga y, a
/// los dos meses, una librería que los gestione — y este proyecto no tiene ni
/// una dependencia de NuGet a propósito. Además la herramienta de terminal se
/// publica en fichero único y RECORTADO: el recortador se lleva por delante los
/// tipos que solo se usan por reflexión, que es justo como se cargaría un
/// complemento. Ya pasó una vez con los modelos de ffprobe.
/// </para>
/// <para>
/// Con procesos, en cambio, el precedente ya está en casa: ffmpeg y ffprobe se
/// invocan así desde el primer día. Un complemento que se cuelga no se lleva la
/// app, y se puede escribir en lo que sea — el de YouTube envolverá yt-dlp, y
/// eso es más natural en un script que en C#.
/// </para>
/// <para>
/// El precio: hay que hablar por texto. El contrato es JSON por la salida
/// estándar, una línea por mensaje, para poder ir leyendo el progreso sin
/// esperar a que termine.
/// </para>
/// </summary>
public sealed class Complemento
{
    /// <summary>Identificador estable. Es el nombre de su carpeta, no se escribe.</summary>
    [JsonIgnore] public string Id { get; set; } = "";

    /// <summary>Dónde vive. Tampoco se declara: se sabe al encontrarlo.</summary>
    [JsonIgnore] public string Carpeta { get; set; } = "";

    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("autor")] public string Autor { get; set; } = "";

    /// <summary>
    /// El programa que se ejecuta, relativo a su carpeta. Se guarda relativo a
    /// propósito: un manifiesto con rutas absolutas deja de funcionar en cuanto
    /// se copia la carpeta a otro equipo, que es lo que se hace para compartirlo.
    /// </summary>
    [JsonPropertyName("ejecutable")] public string Ejecutable { get; set; } = "";

    /// <summary>Argumentos fijos que van SIEMPRE delante del subcomando.</summary>
    [JsonPropertyName("argumentos")] public List<string> Argumentos { get; set; } = new();

    /// <summary>
    /// Qué sabe hacer. Se declara en vez de deducirse llamándolo: enseñar en la
    /// interfaz lo que un complemento ofrece no puede exigir arrancarlo, o abrir
    /// el menú lanzaría un proceso por cada uno instalado.
    /// </summary>
    [JsonPropertyName("capacidades")] public List<string> Capacidades { get; set; } = new();

    /// <summary>
    /// Versión del contrato que habla. Existe desde el principio y no cuando
    /// haga falta: añadirla después obliga a tratar «sin campo» como una versión
    /// más, y esa rama se arrastra para siempre.
    /// </summary>
    [JsonPropertyName("contrato")] public int Contrato { get; set; } = 1;

    /// <summary>
    /// Dónde aplica. Vacío o <c>["global"]</c> significa toda la aplicación; si
    /// no, la lista de modos en los que tiene sentido: <c>organizar</c>,
    /// <c>comprimir</c>, <c>recortes</c>. Un complemento puede declarar varios.
    ///
    /// <para>
    /// Se declara en vez de deducirse de lo que hace. Un complemento que trae
    /// vídeos de fuera sirve tanto en Organizar -para llenar huecos del catálogo-
    /// como suelto, y solo su autor sabe cuál de las dos cosas quería.
    /// </para>
    /// </summary>
    [JsonPropertyName("ambito")] public List<string> Ambito { get; set; } = new();

    /// <summary>
    /// Cómo aparece. <c>propia</c> -lo normal- le da su propio panel detrás del
    /// botón de complementos, como las extensiones de un navegador. <c>nativa</c>
    /// se mete en la interfaz de la aplicación.
    ///
    /// <para>
    /// Lo normal es <c>propia</c> a propósito: un complemento que reordena la
    /// pantalla de quien lo instala tiene que ser una decisión consciente, no lo
    /// que pasa por no escribir un campo.
    /// </para>
    /// </summary>
    [JsonPropertyName("integracion")] public string Integracion { get; set; } = IntegracionPropia;

    /// <summary>Su propio panel detrás del botón de complementos.</summary>
    public const string IntegracionPropia = "propia";

    /// <summary>Se mete en la interfaz de la aplicación.</summary>
    public const string IntegracionNativa = "nativa";

    /// <summary>El ámbito que vale para toda la aplicación.</summary>
    public const string AmbitoGlobal = "global";

    /// <summary>Los modos que la aplicación conoce hoy.</summary>
    public static readonly string[] Modos = ["organizar", "comprimir", "recortes"];

    /// <summary>La única versión del contrato que esta app sabe hablar.</summary>
    public const int ContratoActual = 1;

    /// <summary>Trae elementos de una fuente y los descarga a una carpeta.</summary>
    public const string CapacidadImportar = "importar";

    /// <summary>
    /// Qué le pasa a este complemento, si es que le pasa algo. <c>null</c> si
    /// está en condiciones de usarse.
    ///
    /// <para>
    /// Devuelve el motivo en vez de un booleano porque un complemento que no
    /// aparece y no dice por qué es peor que uno que no está: quien lo instaló
    /// se queda mirando una lista vacía sin nada que corregir.
    /// </para>
    /// </summary>
    public string? Reparo()
    {
        if (string.IsNullOrWhiteSpace(Nombre)) return Textos.Instancia.ComplementoSinNombre;
        if (string.IsNullOrWhiteSpace(Ejecutable)) return Textos.Instancia.ComplementoSinEjecutable;

        if (Contrato != ContratoActual)
            return string.Format(Textos.Instancia.ComplementoContratoAjeno, Contrato, ContratoActual);

        if (Capacidades.Count == 0) return Textos.Instancia.ComplementoSinCapacidades;

        // Un modo que esta versión no conoce NO se ignora en silencio: el
        // complemento no saldría en ninguna parte y su autor lo daría por
        // instalado. Callarlo convierte un error de escritura en un fantasma.
        var desconocido = Ambito.FirstOrDefault(a =>
            !string.Equals(a, AmbitoGlobal, StringComparison.OrdinalIgnoreCase) &&
            !Modos.Contains(a, StringComparer.OrdinalIgnoreCase));
        if (desconocido is not null)
            return string.Format(Textos.Instancia.ComplementoAmbitoDesconocido,
                desconocido, string.Join(", ", Modos));

        if (!string.Equals(Integracion, IntegracionPropia, StringComparison.OrdinalIgnoreCase) &&
            !EsNativa)
            return string.Format(Textos.Instancia.ComplementoIntegracionDesconocida, Integracion);

        // Una ruta que se sale de su carpeta no es un complemento mal escrito: es
        // un manifiesto pidiendo ejecutar cualquier cosa del disco. Se comprueba
        // sobre la ruta ya resuelta, porque «..\..\windows\system32\x.exe» solo
        // se ve por lo que es después de combinarla.
        var destino = Path.GetFullPath(Path.Combine(Carpeta, Ejecutable));
        var suya = Path.GetFullPath(Carpeta) + Path.DirectorySeparatorChar;
        if (!destino.StartsWith(suya, StringComparison.OrdinalIgnoreCase))
            return Textos.Instancia.ComplementoEjecutableFuera;

        if (!File.Exists(destino))
            return string.Format(Textos.Instancia.ComplementoEjecutableNoEsta, Ejecutable);

        return null;
    }

    /// <summary>La ruta del programa, ya resuelta. Solo vale si <see cref="Reparo"/> dio null.</summary>
    public string RutaEjecutable => Path.GetFullPath(Path.Combine(Carpeta, Ejecutable));

    public bool Puede(string capacidad) =>
        Capacidades.Contains(capacidad, StringComparer.OrdinalIgnoreCase);

    /// <summary>Vale en toda la aplicación, no solo en un modo.</summary>
    public bool EsGlobal =>
        Ambito.Count == 0 || Ambito.Contains(AmbitoGlobal, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// ¿Sale en este modo? Los globales salen en todos. Los que declaran modos,
    /// solo en los suyos.
    ///
    /// <para>
    /// Enseñar todos en todas partes convierte el botón en un cajón: con quince
    /// instalados, encontrar el que sirve aquí cuesta más que abrirlo a mano.
    /// </para>
    /// </summary>
    public bool SaleEn(string modo) =>
        EsGlobal || Ambito.Contains(modo, StringComparer.OrdinalIgnoreCase);

    /// <summary>Se mete en la interfaz de la aplicación en vez de traer la suya.</summary>
    public bool EsNativa =>
        string.Equals(Integracion, IntegracionNativa, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Lee un manifiesto. Devuelve null si no es JSON válido: un fichero roto en
    /// la carpeta de complementos no puede impedir que arranque la aplicación.
    /// </summary>
    public static Complemento? Leer(string rutaManifiesto)
    {
        try
        {
            var c = JsonSerializer.Deserialize<Complemento>(File.ReadAllText(rutaManifiesto));
            if (c is null) return null;
            c.Carpeta = Path.GetDirectoryName(rutaManifiesto) ?? "";
            c.Id = new DirectoryInfo(c.Carpeta).Name;
            return c;
        }
        catch { return null; }
    }
}
