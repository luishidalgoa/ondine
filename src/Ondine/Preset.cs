using System.IO;
using System.Text.Json;
using Ondine.Localizacion;

namespace Ondine;

/// <summary>Una configuración guardada (índices de los controles de la UI).</summary>
public sealed class Preset
{
    /// <summary>
    /// El nombre que se ve en el desplegable y, a la vez, con lo que se guarda el
    /// preset por defecto en <c>Settings.DefaultPreset</c>. En los de fábrica sale
    /// traducido, así que un nombre guardado puede venir del otro idioma: para
    /// compararlo, <see cref="PresetStore.NombreVigente"/>.
    /// </summary>
    public string Name { get; set; } = "";
    public bool Factory { get; set; }
    public string Lang { get; set; } = "spa";
    public int Fmt { get; set; }       // índice de cboFmt
    public int Codec { get; set; }     // índice de cboCodec
    public int Quality { get; set; }   // índice de cboQ
    public int Res { get; set; }       // índice de cboRes
    public int Audio { get; set; }     // índice de cboAud

    /// <summary>
    /// Ya no se usa: «analizar subcarpetas» es un ajuste de exploración del usuario
    /// (Preferencias), no parte de la receta de codificación. Se conserva la propiedad
    /// para que los presets guardados con versiones antiguas se sigan leyendo.
    /// </summary>
    [Obsolete("La recursión la manda Preferencias, no el preset.")]
    public bool Recurse { get; set; } = true;

    // El sufijo va como plantilla y no pegado con «+»: hay idiomas que lo pondrían
    // delante del nombre, y con {0} eso se arregla en el texto, no en el código.
    public override string ToString() =>
        Factory ? Name : string.Format(Textos.Instancia.PresetSufijoGuardado, Name);
}

/// <summary>Presets de fábrica + los guardados por el usuario en %AppData%\Ondine\presets.json.</summary>
public static class PresetStore
{
    private static string Dir => DatosDeUsuario.Raiz;
    private static string FilePath => Path.Combine(Dir, "presets.json");

    // La lista se rehace cada vez que se recarga el desplegable, así que los
    // nombres salen siempre en el idioma en curso.
    private static Textos T => Textos.Instancia;

    public static List<Preset> Factory() => new()
    {
        new() { Name = T.PresetEquilibrado,     Factory = true, Fmt = 0, Codec = 0, Quality = 0, Res = 0, Audio = 0 },
        new() { Name = T.PresetCompatibilidad,  Factory = true, Fmt = 1, Codec = 1, Quality = 2, Res = 0, Audio = 1 },
        new() { Name = T.PresetArchivar,        Factory = true, Fmt = 0, Codec = 2, Quality = 2, Res = 0, Audio = 0 },
        new() { Name = T.PresetMovil,           Factory = true, Fmt = 1, Codec = 0, Quality = 0, Res = 2, Audio = 3 },
        new() { Name = T.PresetSoloAudio,       Factory = true, Fmt = 3, Codec = 0, Quality = 0, Res = 0, Audio = 3 },
    };

    /// <summary>
    /// Todos los nombres con los que se ha guardado alguna vez cada preset de
    /// fábrica, en el mismo orden que <see cref="Factory"/>.
    ///
    /// <para>
    /// Son literales CONGELADOS, no traducciones: no se tocan aunque el nombre
    /// visible cambie, y por eso no salen del catálogo de textos. Su único trabajo
    /// es reconocer un preset que quedó guardado en <c>Settings.DefaultPreset</c>
    /// con la app en el otro idioma (o en una versión anterior), porque ahí lo que
    /// se guarda es el nombre, y un nombre traducido deja de casar. Al cambiar un
    /// nombre visible, el viejo se añade a su fila.
    /// </para>
    /// </summary>
    private static readonly string[][] NombresGuardados =
    [
        ["Equilibrado (HEVC)",                 "Balanced (HEVC)"],
        ["Máxima compatibilidad (MP4/H.264)",  "Maximum compatibility (MP4/H.264)"],
        ["Archivar (AV1, alta calidad)",       "Archive (AV1, high quality)"],
        ["Ligero para móvil (720p)",           "Light for mobile (720p)"],
        ["Solo audio (MP3)",                   "Audio only (MP3)"],
    ];

    /// <summary>
    /// El nombre que tiene HOY un preset guardado en los ajustes: si viene de otro
    /// idioma, su equivalente actual; si no se reconoce (preset del usuario, que lo
    /// nombró él), el mismo que entró.
    /// </summary>
    public static string NombreVigente(string? guardado)
    {
        if (string.IsNullOrEmpty(guardado)) return "";
        var fabrica = Factory();
        for (var i = 0; i < NombresGuardados.Length && i < fabrica.Count; i++)
            if (Array.IndexOf(NombresGuardados[i], guardado) >= 0)
                return fabrica[i].Name;
        return guardado;
    }

    public static List<Preset> LoadUser()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public static void SaveUser(List<Preset> user)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
