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
