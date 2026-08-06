using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ondine;

/// <summary>Qué hacer con cada archivo original una vez comprimido correctamente.</summary>
public enum AfterCompress
{
    Ask,               // preguntar al empezar (con opción "no volver a mostrar")
    RecycleOriginal,   // enviar el original a la Papelera automáticamente
    Keep,              // conservar los originales
}

/// <summary>Preferencias de la app, persistidas en %AppData%\Ondine\settings.json.</summary>
public sealed class Settings
{
    // --- General ---

    /// <summary>
    /// Idioma de la INTERFAZ (<c>en</c> / <c>es</c>). Vacío = todavía no se ha
    /// elegido, y entonces manda el del sistema.
    ///
    /// <para>
    /// No confundir con <see cref="DefaultLang"/>, que es el idioma del AUDIO
    /// y va en código de tres letras porque es lo que entiende ffmpeg.
    /// </para>
    /// </summary>
    public string Idioma { get; set; } = "";

    /// <summary>
    /// En qué idioma se nombran las carpetas de temporada al ordenar
    /// (<c>en</c> → «Season 03», <c>es</c> → «Temporada 03»). Vacío = el mismo
    /// que la interfaz.
    ///
    /// <para>
    /// Va aparte de <see cref="Idioma"/> porque no es lo mismo: hay quien usa la
    /// app en castellano y mantiene la biblioteca con los nombres en inglés que
    /// espera encontrar el reproductor.
    /// </para>
    /// </summary>
    public string CarpetaTemporadaIdioma { get; set; } = "";

    public string DefaultPreset { get; set; } = "";      // preset a aplicar al abrir ("" = ninguno)
    public string DefaultLang { get; set; } = "spa";     // idioma de audio por defecto
    public bool Recurse { get; set; } = true;            // analizar subcarpetas
    public bool CheckUpdatesOnStart { get; set; } = true;

    // --- Al comprimir ---
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AfterCompress AfterCompress { get; set; } = AfterCompress.Ask;

    // --- Renombrado de la salida (estilo PowerRename) ---
    public RenameRule Rename { get; set; } = new();
    public List<string> RenameSearchHistory { get; set; } = new();    // valores recientes de «Buscar»
    public List<string> RenameReplaceHistory { get; set; } = new();   // valores recientes de «Reemplazar por»

    /// <summary>Complejidad del contenido aprendida al medir una muestra (1 = imagen real típica).</summary>
    public double ComplexityFactor { get; set; } = 1.0;

    /// <summary>
    /// El modelo de lenguaje conectado, si se ha configurado alguno. Opcional:
    /// la app funciona entera sin él.
    /// </summary>
    public Ia.AjustesDeModelo Ia { get; set; } = new();

    /// <summary>
    /// Los complementos a los que se les ha dado permiso para preguntarle al
    /// modelo, por su identificador.
    ///
    /// <para>
    /// Es una lista de los que SÍ y no un ajuste por complemento con valor por
    /// defecto: así lo que no está escrito es «no», y un complemento instalado
    /// después no hereda un permiso que nadie le dio.
    /// </para>
    /// </summary>
    public List<string> ComplementosConModelo { get; set; } = new();

    /// <summary>¿Tiene permiso este complemento? Lo que no está en la lista, no.</summary>
    public bool PuedeUsarModelo(string? idComplemento) =>
        !string.IsNullOrEmpty(idComplemento) &&
        ComplementosConModelo.Contains(idComplemento, StringComparer.OrdinalIgnoreCase);

    // --- Rendimiento y disco ---
    public int MinFreeMb { get; set; } = 200;            // margen mínimo de disco antes de pausar
    public bool UseHardware { get; set; } = true;        // usar aceleración por hardware si existe

    public Settings Clone()
    {
        var c = (Settings)MemberwiseClone();
        // MemberwiseClone es superficial: sin esto, la copia y el original
        // comparten el MISMO objeto de ajustes del modelo, y editar la copia
        // -que es justo lo que hace la ventana de preferencias- cambiaría los
        // ajustes de verdad aunque luego se cancele.
        c.Ia = Ia.Clone();
        c.ComplementosConModelo = new List<string>(ComplementosConModelo);
        return c;
    }
}

/// <summary>Carga/guarda las preferencias (junto a los presets).</summary>
public static class SettingsStore
{
    private static string Dir => DatosDeUsuario.Raiz;
    private static string FilePath => Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public static void Save(Settings s)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(s, JsonOpts));
        }
        catch { }
    }
}
