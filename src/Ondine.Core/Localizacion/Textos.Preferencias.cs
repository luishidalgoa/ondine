namespace Ondine.Localizacion;

/// <summary>
/// Textos de la ventana de Preferencias, más los nombres de los presets de
/// fábrica, que se listan aquí (y en el desplegable de la ventana principal).
/// </summary>
public sealed partial class Textos
{
    // ── Idioma de la interfaz ───────────────────────────────────────────────
    // Va el primero de la pestaña General y separado del «idioma de audio»,
    // que está tres líneas más abajo y es otra cosa: aquel es la pista que se
    // elige dentro del vídeo, este es en qué habla la aplicación.
    public string PreferenciasIdiomaApp =>
        Idioma.Elegir("Application language", "Idioma de la aplicación");

    public string PreferenciasIdiomaAppAyuda => Idioma.Elegir(
        "Applies right away, without restarting. Not the same as the audio language below.",
        "Se aplica al momento, sin reiniciar. No es el idioma de audio de aquí abajo.");

    public string PreferenciasIdiomaAppSistema =>
        Idioma.Elegir("Same as the system", "El del sistema");

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

    // ═══ Modelo de lenguaje conectado (opcional) ═════════════════════════════

    public string IaPestania => Idioma.Elegir("Model", "Modelo");

    public string IaTitulo => Idioma.Elegir(
        "Connect a language model (optional)",
        "Conectar un modelo de lenguaje (opcional)");

    public string IaIntro => Idioma.Elegir(
        "Ondine works fully without this. It is an optional helper for the cases the rules do not resolve, and each add-on has to be given permission separately before it can use it.",
        "Ondine funciona entera sin esto. Es un apoyo opcional para los casos que las reglas no resuelven, y cada complemento necesita permiso aparte para poder usarlo.");

    public string IaActivo => Idioma.Elegir("Use a connected model", "Usar un modelo conectado");

    public string IaBaseUrl => Idioma.Elegir("Address (OpenAI-compatible)", "Dirección (compatible con OpenAI)");

    public string IaBaseUrlAyuda => Idioma.Elegir(
        "The base of the API. Examples: https://api.openai.com/v1 · http://localhost:11434/v1 (Ollama) · whatever your provider gives you.",
        "La base de la API. Ejemplos: https://api.openai.com/v1 · http://localhost:11434/v1 (Ollama) · la que te dé tu proveedor.");

    public string IaClave => Idioma.Elegir("Key", "Clave");

    public string IaClaveAyuda => Idioma.Elegir(
        "Stored encrypted with Windows data protection, tied to your user account. It is never written in plain text. Leave it empty for a local model that does not ask for one.",
        "Se guarda cifrada con la protección de datos de Windows, atada a tu cuenta de usuario. Nunca se escribe en claro. Déjala vacía para un modelo local que no pida ninguna.");

    public string IaClaveGuardada => Idioma.Elegir(
        "There is a key saved. Type a new one to replace it.",
        "Hay una clave guardada. Escribe otra para reemplazarla.");

    public string IaClaveBorrar => Idioma.Elegir("Forget the key", "Olvidar la clave");
    public string IaClaveOlvidada => Idioma.Elegir("Key forgotten.", "Clave olvidada.");

    public string IaModelo => Idioma.Elegir("Model", "Modelo");

    public string IaModeloAyuda => Idioma.Elegir(
        "The name that server gives it: gpt-4o-mini, llama3.1, qwen2.5:7b…",
        "El nombre que le dé ese servidor: gpt-4o-mini, llama3.1, qwen2.5:7b…");

    public string IaProbar => Idioma.Elegir("Test connection", "Probar conexión");
    public string IaProbando => Idioma.Elegir("Testing…", "Probando…");
    public string IaProbadoBien => Idioma.Elegir("It answers.", "Contesta.");

    // {0} = lo que dijo el error.
    public string IaProbadoMal => Idioma.Elegir("It does not answer: {0}", "No contesta: {0}");

    public string IaDireccionInvalida => Idioma.Elegir(
        "That address is not valid. It has to start with http:// or https://.",
        "Esa dirección no vale. Tiene que empezar por http:// o https://.");

    public string IaSinModelo => Idioma.Elegir(
        "Missing the model name.",
        "Falta el nombre del modelo.");

    // La regla de seguridad, contada donde se topa uno con ella.
    public string IaClavePorHttp => Idioma.Elegir(
        "The key is not sent over http:// to a machine that is not this one: it would travel readable. Use https://, or a model running on this computer.",
        "La clave no se manda por http:// a una máquina que no es esta: viajaría legible. Usa https://, o un modelo que corra en este ordenador.");

    // {0} = el código HTTP, {1} = lo que devolvió el servidor.
    public string IaRespondioMal => Idioma.Elegir("Answered {0}: {1}", "Contestó {0}: {1}");

    public string IaRespuestaIlegible => Idioma.Elegir(
        "The answer did not come in a shape that can be read.",
        "La respuesta no vino en una forma que se pueda leer.");

    // ── Identificar películas contra TMDb (opcional) ────────────────────────
    // Va en su propia pestaña y no dentro de «Modelo» porque son dos cosas
    // distintas que solo se parecen en que las dos salen a internet.
    public string TmdbPestania => Idioma.Elegir("Films", "Películas");

    public string TmdbTitulo => Idioma.Elegir(
        "Identify films against TMDb",
        "Identificar películas contra TMDb");

    public string TmdbIntro => Idioma.Elegir(
        "Series have a catalogue to import; films do not — a film is just a film. Turning this on lets Ondine look each one up on The Movie Database, which is what fixes a misspelt title, brings the year when the file has none, and knows that «The commuter» and «El pasajero» are the same film.",
        "Las series tienen un catálogo que importar; las películas no —una película es solo una película—. Encender esto deja que Ondine busque cada una en The Movie Database, que es lo que arregla un título mal escrito, trae el año cuando el fichero no lo tiene, y sabe que «The commuter» y «El pasajero» son la misma película.");

    public string TmdbActivo => Idioma.Elegir(
        "Look films up on TMDb",
        "Buscar las películas en TMDb");

    // Lo que sale de la máquina, dicho donde se decide encenderlo. Es la
    // pregunta que se hace cualquiera al ver que una app de disco sale a
    // internet, y merece respuesta exacta y no «datos anónimos».
    public string TmdbQueSeManda => Idioma.Elegir(
        "What leaves this computer: the cleaned-up title and the year, and nothing else. Not the file name — the resolution, the codec and the release group are no use for identifying anything and say where the file came from. Answers are kept on disk, so the same film is never asked twice and what has already been asked keeps working with no connection.",
        "Qué sale de este equipo: el título ya limpio y el año, y nada más. No el nombre del fichero —la resolución, el códec y el grupo de release no sirven para identificar nada y dicen de dónde salió el fichero—. Las respuestas se guardan en disco, así que la misma película no se pregunta dos veces y lo ya preguntado sigue funcionando sin conexión.");

    public string TmdbClave => Idioma.Elegir("Your own key (optional)", "Tu propia clave (opcional)");

    public string TmdbClaveAyuda => Idioma.Elegir(
        "Ondine already comes with a key, so you do not need one. Paste yours if you would rather use your own quota, or if you built Ondine yourself. Both credentials on the TMDb settings page work — the «API Key (v3 auth)» and the «API Read Access Token (v4 auth)»: Ondine tells them apart and sends each one the way TMDb expects.",
        "Ondine ya trae una clave, así que no necesitas ninguna. Pega la tuya si prefieres gastar tu propia cuota, o si has compilado Ondine tú. Valen las dos credenciales de la página de ajustes de TMDb —la «API Key (v3 auth)» y el «API Read Access Token (v4 auth)»—: Ondine las distingue y manda cada una como TMDb la espera.");

    public string TmdbClaveGuardada => Idioma.Elegir(
        "There is a key saved. Type over it to replace it.",
        "Hay una clave guardada. Escribe encima para reemplazarla.");

    public string TmdbClaveBorrar => Idioma.Elegir("Forget my key", "Olvidar mi clave");
    public string TmdbClaveOlvidada => Idioma.Elegir(
        "Your key is forgotten; the one that comes with Ondine will be used.",
        "Tu clave está olvidada; se usará la que trae Ondine.");

    public string TmdbUsandoLaDeLaApp => Idioma.Elegir(
        "Using the key that comes with Ondine.",
        "Se está usando la clave que trae Ondine.");

    public string TmdbUsandoLaTuya => Idioma.Elegir(
        "Using your key.",
        "Se está usando tu clave.");

    // El caso de quien compila el repo: sin el secreto de la build no hay clave.
    // Se dice aquí, que es donde se puede arreglar, en vez de dejar que se
    // descubra como un fallo de red al primer intento.
    public string TmdbSinNingunaClave => Idioma.Elegir(
        "This copy of Ondine was built without a key, so nothing can be looked up until you paste one. You can get one free at themoviedb.org, under Settings, API.",
        "Esta copia de Ondine se compiló sin clave, así que no se puede buscar nada hasta que pegues una. Se consigue gratis en themoviedb.org, en Ajustes, API.");

    // ── Cómo conseguir tu propia clave ──────────────────────────────────────
    // Va delante del campo donde se pega y no en la Ayuda: el sitio donde hace
    // falta saber esto es justo aquí. Mandar a alguien a otra pantalla a leer
    // cómo rellenar la que tiene abierta es perder a la mitad por el camino.
    public string TmdbComoTitulo => Idioma.Elegir(
        "Want to use your own key?",
        "¿Quieres usar tu propia clave?");

    public string TmdbComoPasos => Idioma.Elegir(
        """
        1. Sign in to TMDb, or create an account — it is free.
        2. Open the API page with the button below.
        3. Accept the terms of use and fill in the form: name «Ondine», type «Desktop application», and one line of description.
        4. You end up with two credentials. Copy the «API Read Access Token (v4 auth)» — the long one starting with eyJ — and paste it whole into the field above. The «API Key (v3 auth)» works too, but the token travels in the request header instead of inside the address, and addresses get written into logs along the way.
        5. Save.
        """,
        """
        1. Entra en TMDb con tu cuenta, o créala: es gratis.
        2. Abre la página de la API con el botón de abajo.
        3. Acepta los términos de uso y rellena el formulario: nombre «Ondine», tipo «aplicación de escritorio», y una línea de descripción.
        4. Te quedan dos credenciales. Copia el «API Read Access Token (v4 auth)» —el largo que empieza por eyJ— y pégalo entero en el campo de arriba. La «API Key (v3 auth)» también vale, pero el token viaja en la cabecera de la petición en vez de dentro de la dirección, y las direcciones se quedan escritas en los registros del camino.
        5. Guarda.
        """);

    public string TmdbComoAbrir => Idioma.Elegir(
        "Open the TMDb API page",
        "Abrir la página de la API de TMDb");

    // {0} = la dirección, para poder copiarla a mano si no hay navegador que abrir.
    public string TmdbNoSeAbrio => Idioma.Elegir(
        "The browser could not be opened. The address is: {0}",
        "No se pudo abrir el navegador. La dirección es: {0}");
}
