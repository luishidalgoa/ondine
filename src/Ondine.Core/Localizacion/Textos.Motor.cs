namespace Ondine.Localizacion;

/// <summary>
/// Los textos del motor: lo que se escribe en el panel de registro mientras
/// comprime, los motivos por los que un archivo se salta, el aviso de
/// actualización, el catálogo de sugerencias del renombrado y las entradas que
/// Ondine deja en el menú del Explorador.
///
/// <para>
/// Casi todo esto son líneas de registro, no rótulos de ventana. Se traducen
/// igual porque el panel de registro es lo primero que mira el usuario cuando
/// algo no sale como esperaba: dejarlo en castellano equivale a no explicarle
/// nada a quien abre la app en inglés.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── Quitar pistas sin recodificar ───────────────────────────────────────
    public string MotorPistasSinMarcar =>
        Idioma.Elegir("No tracks are ticked.", "No hay pistas marcadas.");

    public string MotorPistasFicheroDesaparecido =>
        Idioma.Elegir("The file is no longer there.", "El fichero ya no está.");

    // {0} = lo que se pidio · {1} = lo que se va a hacer · {2} = el contenedor
    // Se dice porque cambiarlo en silencio deja al usuario creyendo que tiene AC3
    // cuando tiene AAC, y eso solo se descubre al reproducirlo en el sitio equivocado.
    // {0} = los canales que traia. Se dice porque mezclar obliga a recodificar, y
    // quien pidio «sin tocar» el codec merece saber por que se le ha tocado.
    public string MotorAudioMezclado => Idioma.Elegir(
        "Audio: {0} channels mixed down to stereo, so the track had to be re-encoded.",
        "Audio: {0} canales bajados a estéreo, así que la pista ha tenido que recodificarse.");

    public string MotorAudioCambiado => Idioma.Elegir(
        "Audio: {0} does not fit in {2}, using {1} instead.",
        "Audio: {0} no cabe en {2}, se usa {1} en su lugar.");

    public string MotorPistasRemuxFallido =>
        Idioma.Elegir("ffmpeg could not repackage it.", "ffmpeg no pudo reempaquetarlo.");

    /// <summary>
    /// Cuando quitar pistas no puede asegurar el original antes de sobrescribirlo.
    ///
    /// <para>
    /// Se prefiere no hacer nada a hacerlo sin red: quitar un doblaje es rápido y se puede
    /// repetir, pero el original sobrescrito sin copia no vuelve.
    /// </para>
    /// </summary>
    public string MotorPistasSinRed => Idioma.Elegir(
        "The tracks were not removed: the original could not be moved to the recycle bin first, and it will not be overwritten without a way back. Free some space and try again.",
        "No se han quitado las pistas: no se ha podido poner a salvo el original antes de sobrescribirlo, y no se sobrescribe sin vuelta atrás. Libera espacio y vuelve a intentarlo.");

    // Cuando ni siquiera se pudo LANZAR la herramienta: no esta instalada, no esta en el
    // PATH, o el instalador no llego a bajarla. Es distinto de que fallara al procesar, y
    // se dice distinto: aqui lo que hay que hacer es reinstalar, no cambiar de fichero.
    // {0} = la ruta o el nombre con el que se intento.
    public string MotorNoSePudoLanzar =>
        Idioma.Elegir("Could not start «{0}». Check that ffmpeg is installed.",
                      "No se pudo iniciar «{0}». Comprueba que ffmpeg esta instalado.");

    // ── Cabecera del registro ───────────────────────────────────────────────
    public string MotorSoloAudioModo =>
        Idioma.Elegir("Audio only mode → {0}", "Modo solo audio → {0}");

    public string MotorCodificador =>
        Idioma.Elegir("Encoder: {0} [{1}] · quality {2}", "Codificador: {0} [{1}] · calidad {2}");

    // Las dos van dentro de los corchetes de la línea de arriba y se leen como
    // pareja: en castellano la forma hecha es «por hardware / por software», que
    // además evita que el inglés y el castellano queden idénticos.
    public string MotorCodificadorHardware =>
        Idioma.Elegir("hardware (GPU)", "por hardware (GPU)");

    public string MotorCodificadorSoftware =>
        Idioma.Elegir("software (CPU, slow)", "por software (CPU, lento)");

    public string MotorAvisoAv1Lento => Idioma.Elegir(
        "  WARNING: AV1 in software is VERY slow (it can take hours). For speed use H.265, which makes the most of your GPU.",
        "  AVISO: AV1 por software es MUY lento (puede tardar horas). Para ir rápido usa H.265, que aprovecha tu GPU.");

    // ── Archivos que se saltan ──────────────────────────────────────────────
    // {0} = número de archivo · {1} = total · {2} = nombre del archivo.
    public string MotorSaltoYaHecho =>
        Idioma.Elegir("[{0}/{1}] {2} → already done, skipping", "[{0}/{1}] {2} → ya hecho, salto");

    public string MotorSaltoDescargando =>
        Idioma.Elegir("[{0}/{1}] {2} → still downloading, skipping", "[{0}/{1}] {2} → descargando aún, salto");

    public string MotorSaltoIlegible =>
        Idioma.Elegir("[{0}/{1}] {2} → cannot be read, skipping", "[{0}/{1}] {2} → no se puede leer, salto");

    public string MotorSaltoSinVideo =>
        Idioma.Elegir("[{0}/{1}] {2} → no video track, skipping", "[{0}/{1}] {2} → sin pista de vídeo, salto");

    // {3} = códec ya presente · {4} = kbps.
    public string MotorSaltoYaComprimido => Idioma.Elegir(
        "[{0}/{1}] {2} → already compressed ({3}, {4} kbps), skipping",
        "[{0}/{1}] {2} → ya comprimido ({3}, {4} kbps), salto");

    public string MotorSaltoSinAudio =>
        Idioma.Elegir("[{0}/{1}] {2} → no audio, skipping", "[{0}/{1}] {2} → sin audio, salto");

    // Los motivos van en la columna Estado de la tabla, no en el registro: han de
    // caber en poco sitio, así que se quedan en dos o tres palabras.
    public string MotorMotivoYaHecho => Idioma.Elegir("Already done", "Ya hecho");
    public string MotorMotivoDescargando => Idioma.Elegir("Downloading", "Descargando");
    public string MotorMotivoIlegible => Idioma.Elegir("Cannot be read", "No se puede leer");
    public string MotorMotivoSinVideo => Idioma.Elegir("No video", "Sin vídeo");
    public string MotorMotivoSinAudio => Idioma.Elegir("No audio", "Sin audio");

    /// <summary>{0} = el códec en mayúsculas (HEVC, AV1): no se traduce.</summary>
    public string MotorMotivoYaEn => Idioma.Elegir("Already {0}", "Ya en {0}");

    // ── Modo solo audio ─────────────────────────────────────────────────────
    public string MotorSecoSoloAudio =>
        Idioma.Elegir("[{0}/{1}] {2} → audio only → {3}", "[{0}/{1}] {2} → solo audio → {3}");

    public string MotorExtrayendoAudio => Idioma.Elegir(
        "    extracting audio ({0} track/s) → {1}",
        "    extrayendo audio ({0} pista/s) → {1}");

    public string MotorRenombrado =>
        Idioma.Elegir("    renamed → {0}", "    renombrado → {0}");

    // El «OK» de estas dos líneas pasa a ser la palabra de la aplicación para lo
    // que sale bien (Done / Listo, la misma de Textos.Comun): un «OK» a secas se
    // escribe igual en los dos idiomas y no diría nada al traducirlo.
    public string MotorAudioListo =>
        Idioma.Elegir("    Done  audio → {0} MB", "    Listo  audio → {0} MB");

    public string MotorErrorExtraerAudio => Idioma.Elegir(
        "    ERROR while extracting audio (code {0})",
        "    ERROR al extraer audio (código {0})");

    public string MotorDetenido => Idioma.Elegir("    stopped.", "    detenido.");

    /// <summary>Columna Estado de un archivo del que solo se sacó la banda sonora.</summary>
    public string MotorEstadoSoloAudio => Idioma.Elegir("audio only", "solo audio");

    // ── Resumen de cada archivo ─────────────────────────────────────────────
    // «audio» solo sería idéntico en los dos idiomas; se nombra la pista, que es
    // lo que la línea enumera de verdad.
    public string MotorInfoAudio => Idioma.Elegir("audio tracks: {0}", "pistas de audio: {0}");

    public string MotorInfoDescartadas => Idioma.Elegir(" (dropping {0})", " (descarto {0})");

    // Se corta a «sub» a propósito: es una coletilla de una línea que ya lleva
    // idiomas y resolución detrás. El inglés marca el plural con «/s», igual que
    // «track/s» de arriba, porque puede venir una sola pista.
    public string MotorInfoSubtitulos => Idioma.Elegir(", {0} sub/s", ", {0} sub");

    public string MotorInfoReescalado => Idioma.Elegir(", rescaling to {0}p", ", reescalo a {0}p");

    public string MotorAvisoPrefijo => Idioma.Elegir("    WARNING: {0}", "    AVISO: {0}");

    public string MotorReintentoSinSubtitulos => Idioma.Elegir(
        "    retrying without subtitles…",
        "    reintentando sin subtítulos…");

    public string MotorVideoListo => Idioma.Elegir(
        "    Done  {0} MB → {1} MB  (-{2}%)",
        "    Listo  {0} MB → {1} MB  (-{2}%)");

    /// <summary>
    /// El fallo al codificar. {0} es el MOTIVO, no el código a secas.
    ///
    /// <para>
    /// Decía «ERROR al codificar (código 243)» y nada más: un número sin causa, con la salida
    /// de error de ffmpeg capturada dos líneas más arriba y tirada. Ahora se dice qué pasó.
    /// </para>
    /// </summary>
    public string MotorErrorCodificar => Idioma.Elegir(
        "    ERROR while encoding: {0}",
        "    ERROR al codificar: {0}");

    public string MotorFfmpegSinPermiso => Idioma.Elegir(
        "permission denied writing the result (check that you can write to the destination folder)",
        "sin permiso para escribir el resultado (comprueba que puedes escribir en la carpeta de destino)");

    public string MotorFfmpegSinEspacio => Idioma.Elegir(
        "no space left on the destination disk",
        "no queda espacio en el disco de destino");

    public string MotorFfmpegNoEncontrado => Idioma.Elegir(
        "a file or folder in the way does not exist",
        "algún fichero o carpeta del camino no existe");

    /// <summary>
    /// No se puede escribir en la carpeta de destino, dicho ANTES de empezar. {0} es la
    /// carpeta y {1} el motivo del sistema, sin traducir: es lo que se puede buscar.
    /// </summary>
    public string MotorNoSePuedeEscribir => Idioma.Elegir(
        """
            STOPPED: cannot write to the destination folder, so nothing was attempted.
            {0}
            The system says: {1}
            Check that your user can write there (a read-only mount, or one owned by someone else, looks exactly like this).
        """,
        """
            SE PARA: no se puede escribir en la carpeta de destino, así que no se ha intentado nada.
            {0}
            El sistema dice: {1}
            Comprueba que tu usuario puede escribir ahí (un montaje de solo lectura, o de otro dueño, se ve exactamente así).
        """);

    public string MotorFfmpegSoloElCodigo => Idioma.Elegir(
        "ffmpeg failed with code {0} and did not say why",
        "ffmpeg falló con el código {0} y no dijo por qué");

    public string MotorDetenidoConTemporal => Idioma.Elegir(
        "    stopped; temporary file deleted.",
        "    detenido; temporal eliminado.");

    // ── Subtítulos que no caben en el contenedor ────────────────────────────
    // Cuatro frases enteras en vez de montarlas por trozos: el castellano
    // concuerda género y número («incluirla», «las necesitas») y una traducción
    // hecha de cachos sueltos no se puede rehacer en otro idioma.
    // {0} = idiomas · {1} = tipos de subtítulo · {2} = contenedor.
    public string MotorSubsFueraImagenUna => Idioma.Elegir(
        "1 subtitle track ({0}) has been left out: it is image based (type {1}) and {2} does not support that format. Compress to MKV if you need it.",
        "1 pista de subtítulos ({0}) se ha quedado fuera: es de imagen (tipo {1}) y {2} no admite ese formato. Comprime a MKV si la necesitas.");

    // {0} = cuántas · {1} = idiomas · {2} = tipos · {3} = contenedor.
    public string MotorSubsFueraImagenVarias => Idioma.Elegir(
        "{0} subtitle tracks ({1}) have been left out: they are image based (type {2}) and {3} does not support that format. Compress to MKV if you need them.",
        "{0} pistas de subtítulos ({1}) se han quedado fuera: son de imagen (tipo {2}) y {3} no admite ese formato. Comprime a MKV si las necesitas.");

    // {0} = idiomas · {1} = contenedor.
    public string MotorSubsFueraUna => Idioma.Elegir(
        "1 subtitle track ({0}) has been left out: {1} could not include it. Compress to MKV if you need it.",
        "1 pista de subtítulos ({0}) se ha quedado fuera: {1} no ha podido incluirla. Comprime a MKV si la necesitas.");

    // {0} = cuántas · {1} = idiomas · {2} = contenedor.
    public string MotorSubsFueraVarias => Idioma.Elegir(
        "{0} subtitle tracks ({1}) have been left out: {2} could not include them. Compress to MKV if you need them.",
        "{0} pistas de subtítulos ({1}) se han quedado fuera: {2} no ha podido incluirlas. Comprime a MKV si las necesitas.");

    // ── Disco lleno ─────────────────────────────────────────────────────────
    public string MotorDiscoLleno => Idioma.Elegir(
        "    Disk full - paused. Free up space and it will carry on by itself.",
        "    Disco lleno - pausado. Libera espacio y continuará automáticamente.");

    public string MotorDiscoConEspacio => Idioma.Elegir(
        "    Space available - carrying on…",
        "    Espacio disponible - continuando…");

    // ── Buscar actualizaciones ──────────────────────────────────────────────
    // Son la mitad de una frase: la ventana las enseña detrás de «No se pudo
    // comprobar: …», así que empiezan en minúscula y sin punto final.
    public string MotorActualizacionVersionIlegible => Idioma.Elegir(
        "the latest release (“{0}”) does not have a recognisable version number",
        "la última publicación («{0}») no tiene un número de versión reconocible");

    public string MotorActualizacionSinInstalador => Idioma.Elegir(
        "version {0} is published but does not come with an installer attached",
        "la versión {0} está publicada pero no trae instalador adjunto");

    public string MotorActualizacionTiempoAgotado =>
        Idioma.Elegir("the request timed out", "se agotó el tiempo de espera");

    public string MotorActualizacionSinConexion =>
        Idioma.Elegir("there is no connection to GitHub ({0})", "no hay conexión con GitHub ({0})");

    // ── Menú del Explorador ─────────────────────────────────────────────────
    // Ojo: esto se ESCRIBE en el registro al activar la integración, así que se
    // queda en el idioma que hubiera entonces. Cambiar de idioma no reescribe el
    // menú de Windows; hay que desactivar y volver a activar la casilla.
    // El rótulo tiene que decir lo mismo que PreferenciasMenuExplorador, que lo
    // cita entre comillas.
    public string MenuExploradorComprimir =>
        Idioma.Elegir("Compress with Ondine", "Comprimir con Ondine");

    public string MenuExploradorEnviarA => Idioma.Elegir(
        "Add these videos to the Ondine list",
        "Añadir estos vídeos a la lista de Ondine");

    /// <summary>Nombre del tipo de archivo en «Abrir con» del Explorador.</summary>
    public string MenuExploradorTipoVideo => Idioma.Elegir("Video (Ondine)", "Vídeo (Ondine)");

    // ── Sugerencias del campo «Buscar» ──────────────────────────────────────
    // Se ven en el desplegable de autocompletado, a la derecha del patrón. Los
    // patrones («foo», «bar», [1080p], S01E02) son ejemplos literales y no se
    // traducen: es lo que el usuario va a escribir.
    public string SugerenciaInicioNombre => Idioma.Elegir(
        "start of the name (to put text in front)",
        "principio del nombre (para anteponer texto)");

    public string SugerenciaFinNombre => Idioma.Elegir(
        "end of the name (to add text at the end)",
        "final del nombre (para añadir texto al final)");

    public string SugerenciaTodoElTexto =>
        Idioma.Elegir("the whole text of the name", "todo el texto del nombre");

    public string SugerenciaCapturaNombre => Idioma.Elegir(
        "captures the whole name as group $1",
        "captura el nombre entero como grupo $1");

    public string SugerenciaNombreCompleto => Idioma.Elegir(
        "the complete name (to replace it entirely)",
        "el nombre completo (para sustituirlo del todo)");

    public string SugerenciaDigitos => Idioma.Elegir("one or more digits", "uno o más dígitos");

    public string SugerenciaEspacios => Idioma.Elegir("one or more spaces", "uno o más espacios");

    // «full stop» y «hyphen» son los nombres británicos del punto y el guion.
    public string SugerenciaSeparadores => Idioma.Elegir(
        "separators: full stop, underscore or hyphen",
        "separadores: punto, guion bajo o guion");

    public string SugerenciaCorchetes => Idioma.Elegir(
        "text in square brackets, e.g. [1080p]",
        "texto entre corchetes, p. ej. [1080p]");

    public string SugerenciaParentesis => Idioma.Elegir(
        "text in round brackets, e.g. (2019)",
        "texto entre paréntesis, p. ej. (2019)");

    public string SugerenciaTresPrimeros => Idioma.Elegir(
        "the first 3 characters (to trim them off)",
        "los 3 primeros caracteres (para recortarlos)");

    public string SugerenciaTresUltimos => Idioma.Elegir(
        "the last 3 characters (to trim them off)",
        "los 3 últimos caracteres (para recortarlos)");

    public string SugerenciaEmpiezaPor =>
        Idioma.Elegir("whatever starts with “foo”", "lo que empieza por «foo»");

    public string SugerenciaTerminaEn =>
        Idioma.Elegir("whatever ends in “bar”", "lo que termina en «bar»");

    public string SugerenciaEmpiezaYAcaba => Idioma.Elegir(
        "starts with “foo” and ends in “bar”",
        "empieza por «foo» y acaba en «bar»");

    public string SugerenciaAnteriorA =>
        Idioma.Elegir("everything before “bar”", "todo lo anterior a «bar»");

    public string SugerenciaEntreDos => Idioma.Elegir(
        "everything between “foo” and “bar”, both included",
        "todo entre «foo» y «bar», incluidos");

    // dd-mm-aaaa es la máscara de fecha escrita como se lee en cada idioma.
    public string SugerenciaFecha => Idioma.Elegir(
        "date dd-mm-yyyy in 3 groups ($1 $2 $3)",
        "fecha dd-mm-aaaa en 3 grupos ($1 $2 $3)");

    public string SugerenciaTemporadaEpisodio => Idioma.Elegir(
        "season and episode: S01E02 → $1 and $2",
        "temporada y episodio: S01E02 → $1 y $2");

    public string SugerenciaResolucion =>
        Idioma.Elegir("resolution tags in the name", "marcas de resolución en el nombre");

    public string SugerenciaCodecFuente =>
        Idioma.Elegir("codec/source tags in the name", "marcas de codec/fuente en el nombre");

    // ── Sugerencias del campo «Reemplazar por» ──────────────────────────────
    public string SugerenciaGrupo1 => Idioma.Elegir(
        "first capture group from the search",
        "primer grupo de captura de la búsqueda");

    public string SugerenciaGrupo2 => Idioma.Elegir("second capture group", "segundo grupo de captura");
    public string SugerenciaGrupo3 => Idioma.Elegir("third capture group", "tercer grupo de captura");
    public string SugerenciaDolarLiteral => Idioma.Elegir("a literal $ sign", "un símbolo $ literal");

    public string SugerenciaContadorSimple => Idioma.Elegir("plain counter: 0, 1, 2…", "contador simple: 0, 1, 2…");
    public string SugerenciaContadorDesdeUno => Idioma.Elegir("counter starting at 1", "contador que empieza en 1");
    public string SugerenciaContadorDosCifras => Idioma.Elegir("counter 01, 02, 03…", "contador 01, 02, 03…");
    public string SugerenciaContadorTresCifras => Idioma.Elegir("counter 001, 002, 003…", "contador 001, 002, 003…");
    public string SugerenciaContadorDeDosEnDos => Idioma.Elegir("counter in steps of 2", "contador de 2 en 2");

    public string SugerenciaContadorCombinado => Idioma.Elegir(
        "combined: 0010, 0012, 0014…",
        "combinado: 0010, 0012, 0014…");

    // Los ejemplos entre paréntesis son la salida real, así que van en el idioma
    // de cada versión: (July) / (julio), (Tue) / (mar).
    public string SugerenciaAnio4 => Idioma.Elegir("year with 4 digits (2026)", "año con 4 dígitos (2026)");
    public string SugerenciaAnio2 => Idioma.Elegir("year with 2 digits (26)", "año con 2 dígitos (26)");
    public string SugerenciaAnio1 => Idioma.Elegir("last digit of the year", "último dígito del año");
    public string SugerenciaMesNombre => Idioma.Elegir("name of the month (July)", "nombre del mes (julio)");
    public string SugerenciaMesAbreviado => Idioma.Elegir("short month (Jul)", "mes abreviado (jul)");
    public string SugerenciaMesCero => Idioma.Elegir("month with a leading zero (07)", "mes con cero delante (07)");
    public string SugerenciaMesSinCero => Idioma.Elegir("month without the zero (7)", "mes sin cero (7)");

    public string SugerenciaDiaSemana =>
        Idioma.Elegir("day of the week (Tuesday)", "día de la semana (martes)");

    public string SugerenciaDiaSemanaAbreviado =>
        Idioma.Elegir("short day of the week (Tue)", "día de la semana abreviado (mar)");

    public string SugerenciaDiaCero => Idioma.Elegir("day with a leading zero (05)", "día con cero delante (05)");
    public string SugerenciaDiaSinCero => Idioma.Elegir("day without the zero (5)", "día sin cero (5)");
    public string SugerenciaHoraCero => Idioma.Elegir("hour with a leading zero", "hora con cero delante");
    public string SugerenciaHoraSinCero => Idioma.Elegir("hour without the zero", "hora sin cero");
    public string SugerenciaMinutosCero => Idioma.Elegir("minutes with a leading zero", "minutos con cero delante");
    public string SugerenciaMinutosSinCero => Idioma.Elegir("minutes without the zero", "minutos sin cero");
    public string SugerenciaSegundosCero => Idioma.Elegir("seconds with a leading zero", "segundos con cero delante");
    public string SugerenciaSegundosSinCero => Idioma.Elegir("seconds without the zero", "segundos sin cero");
    public string SugerenciaMilisegundos3 => Idioma.Elegir("milliseconds (3 digits)", "milisegundos (3 dígitos)");
    public string SugerenciaMilisegundos2 => Idioma.Elegir("milliseconds (2 digits)", "milisegundos (2 dígitos)");
    public string SugerenciaMilisegundos1 => Idioma.Elegir("milliseconds (1 digit)", "milisegundos (1 dígito)");

    public string SugerenciaAleatorioAlfanumerico => Idioma.Elegir(
        "8 random characters (letters and digits)",
        "8 caracteres aleatorios (letras y dígitos)");

    public string SugerenciaAleatorioLetras => Idioma.Elegir("8 random letters", "8 letras aleatorias");
    public string SugerenciaAleatorioDigitos => Idioma.Elegir("6 random digits", "6 dígitos aleatorios");
    public string SugerenciaUuid => Idioma.Elegir("unique identifier (UUID v4)", "identificador único (UUID v4)");

    /// <summary>Lo que se pone de descripción a lo que viene del historial, no del catálogo.</summary>
    public string SugerenciaUsadoRecientemente => Idioma.Elegir("recently used", "usado recientemente");
}
