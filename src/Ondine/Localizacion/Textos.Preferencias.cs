namespace Ondine.Localizacion;

/// <summary>
/// Textos de la ventana de Preferencias, más los nombres de los presets de
/// fábrica, que se listan aquí (y en el desplegable de la ventana principal).
/// </summary>
public sealed partial class Textos
{
    // ── Ventana ─────────────────────────────────────────────────────────────
    // Sirve para el Title y para el rótulo de la cabecera: es el mismo texto y
    // se traduce una vez.
    public string PreferenciasTitulo => Idioma.Elegir("Preferences", "Preferencias");

    // ── Pestañas ────────────────────────────────────────────────────────────
    public string PreferenciasTabGeneral => Idioma.Elegir("General", "General");
    public string PreferenciasTabAlComprimir => Idioma.Elegir("When compressing", "Al comprimir");
    public string PreferenciasTabRendimiento => Idioma.Elegir("Performance and disk", "Rendimiento y disco");

    // ── General ─────────────────────────────────────────────────────────────
    public string PreferenciasPresetPorDefecto =>
        Idioma.Elegir("Preset to load on startup", "Preset por defecto al abrir");

    // El primer elemento del desplegable de presets. En castellano era
    // «— ninguno —»: las rayas se cambian por paréntesis, que no dependen de la
    // tipografía y no son una raya larga.
    public string PreferenciasSinPreset => Idioma.Elegir("(none)", "(ninguno)");

    public string PreferenciasIdiomaAudio =>
        Idioma.Elegir("Default audio language", "Idioma de audio por defecto");

    // spa, eng y por son códigos ISO 639: se queda la frase, no los códigos.
    public string PreferenciasIdiomaAudioAyuda => Idioma.Elegir(
        "Marked as the default audio track (ISO code, e.g. spa, eng, por).",
        "Se marca como pista de audio por defecto (código ISO, p. ej. spa, eng, por).");

    public string PreferenciasSubcarpetas => Idioma.Elegir("Scan subfolders", "Analizar subcarpetas");

    // Cita la casilla de la ventana principal: si allí se traduce «Subcarpetas»
    // de otra forma, este texto hay que cambiarlo a la vez o dejan de coincidir.
    public string PreferenciasSubcarpetasAyuda => Idioma.Elegir(
        "When scanning the source, it also goes into the folders inside. It is the same “Subfolders” check box as in the main window.",
        "Al analizar el origen, entra también en las carpetas de dentro. Es la misma casilla «Subcarpetas» de la ventana principal.");

    public string PreferenciasBuscarActualizaciones =>
        Idioma.Elegir("Check for updates when the app starts", "Buscar actualizaciones al abrir la app");

    // «Comprimir con Ondine» es la entrada REAL que se escribe en el registro
    // (ShellIntegration.VerbLabel, hoy fija en castellano). Si allí se localiza,
    // este texto ya la nombra bien en los dos idiomas; hasta entonces, en inglés
    // el menú del Explorador seguirá diciendo la versión castellana.
    public string PreferenciasMenuExplorador => Idioma.Elegir(
        "Add “Compress with Ondine” to the File Explorer menu",
        "Añadir «Comprimir con Ondine» al menú del Explorador");

    public string PreferenciasMenuExploradorAyuda => Idioma.Elegir(
        "Lets you select videos in File Explorer and send them to the Ondine list with a right click.",
        "Permite seleccionar vídeos en el Explorador y mandarlos a la lista de Ondine con el botón derecho.");

    // «Mostrar más opciones» y «Mayús» son cadenas del propio Windows: en inglés
    // van con el nombre oficial de Microsoft («Show more options», «Shift»), no
    // con una traducción libre.
    public string PreferenciasMenuExploradorNota => Idioma.Elegir(
        "On Windows 11 the entry appears inside “Show more options” (or with Shift + right click). Showing up in the top level menu requires a signed package, like the one PowerRename uses.",
        "En Windows 11 la entrada aparece dentro de «Mostrar más opciones» (o con Mayús + clic derecho). Salir en el menú de primer nivel exige un paquete firmado, como el de PowerRename.");

    // ── Al comprimir ────────────────────────────────────────────────────────
    public string PreferenciasTrasComprimirTitulo => Idioma.Elegir(
        "Once each file has been compressed…",
        "Cuando termine de comprimir cada archivo…");

    public string PreferenciasTrasComprimirPreguntar =>
        Idioma.Elegir("Ask me every time (before starting)", "Preguntarme cada vez (antes de empezar)");

    // «Papelera de reciclaje» es el nombre de Windows: en inglés, «Recycle Bin».
    public string PreferenciasTrasComprimirPapelera =>
        Idioma.Elegir("Send the original to the Recycle Bin automatically", "Enviar el original a la Papelera automáticamente");

    public string PreferenciasTrasComprimirConservar =>
        Idioma.Elegir("Keep the originals", "Conservar los originales");

    public string PreferenciasPapeleraNota => Idioma.Elegir(
        "Originals are sent to the Recycle Bin (recoverable); they are never deleted for good.",
        "Los originales se envían a la Papelera de reciclaje (recuperables); nunca se borran de forma definitiva.");

    // El original llevaba una raya larga; aquí va una coma, que dice lo mismo sin
    // meter un carácter que esta app no usa.
    public string PreferenciasBorradoNota => Idioma.Elegir(
        "Deletion happens as soon as the compressed version of each file is ready and verified, file by file, not at the end.",
        "El borrado ocurre en cuanto la versión comprimida de cada archivo está lista y verificada, archivo por archivo, no al final.");

    public string PreferenciasRutaNota => Idioma.Elegir(
        "An original is never deleted if the compressed file would be saved to that same path.",
        "Nunca se elimina un original si el comprimido se guardaría en su misma ruta.");

    // ── Rendimiento y disco ─────────────────────────────────────────────────
    public string PreferenciasMargenDisco =>
        Idioma.Elegir("Minimum free disk space before pausing", "Margen mínimo de disco antes de pausar");

    // Símbolo del SI: no se traduce. Está en el catálogo para poder ajustar el
    // orden o el espaciado si algún día entra un idioma que los cambie.
    public string PreferenciasUnidadMb => Idioma.Elegir("MB", "MB");

    public string PreferenciasMargenDiscoAyuda => Idioma.Elegir(
        "If free space drops below this margin, compression pauses and resumes on its own once space is freed.",
        "Si el espacio libre baja de este margen, la compresión se pausa y se reanuda sola al liberar espacio.");

    public string PreferenciasAceleracion =>
        Idioma.Elegir("Use hardware acceleration (GPU) when available", "Usar aceleración por hardware (GPU) si está disponible");

    // El castellano tutea; el inglés mantiene el mismo tono directo.
    public string PreferenciasAceleracionAyuda => Idioma.Elegir(
        "Turn it off only if your GPU produces artefacts. On CPU, compression is quite a bit slower.",
        "Desactívalo solo si tu GPU produce artefactos. Por CPU la compresión es bastante más lenta.");

    // ── Avisos de la ventana ────────────────────────────────────────────────
    public string PreferenciasMenuAvisoTitulo =>
        Idioma.Elegir("File Explorer menu", "Menú del Explorador");

    public string PreferenciasMenuAvisoAlta => Idioma.Elegir(
        "The File Explorer menu entry could not be added.",
        "No se pudo añadir la entrada al menú del Explorador.");

    public string PreferenciasMenuAvisoBaja => Idioma.Elegir(
        "The File Explorer menu entry could not be removed.",
        "No se pudo quitar la entrada del menú del Explorador.");

    // ── Presets de fábrica ──────────────────────────────────────────────────
    // Códecs, contenedores y resoluciones (HEVC, MP4, H.264, AV1, MP3, 720p) son
    // nombres técnicos y se quedan igual en los dos idiomas.
    //
    // OJO: el nombre visible de un preset es también lo que se guarda en
    // Settings.DefaultPreset. Al cambiar cualquiera de estos textos hay que añadir
    // el nombre viejo a PresetStore.NombresGuardados (Preset.cs) o el «preset por
    // defecto» de quien lo tuviera elegido se pierde sin decir nada.
    public string PresetEquilibrado => Idioma.Elegir("Balanced (HEVC)", "Equilibrado (HEVC)");
    public string PresetCompatibilidad =>
        Idioma.Elegir("Maximum compatibility (MP4/H.264)", "Máxima compatibilidad (MP4/H.264)");
    public string PresetArchivar => Idioma.Elegir("Archive (AV1, high quality)", "Archivar (AV1, alta calidad)");
    public string PresetMovil => Idioma.Elegir("Light for mobile (720p)", "Ligero para móvil (720p)");
    public string PresetSoloAudio => Idioma.Elegir("Audio only (MP3)", "Solo audio (MP3)");

    /// <summary>Marca de los presets que ha guardado el usuario. {0} es el nombre.</summary>
    public string PresetSufijoGuardado => Idioma.Elegir("{0}  ·  saved", "{0}  ·  guardado");
}
