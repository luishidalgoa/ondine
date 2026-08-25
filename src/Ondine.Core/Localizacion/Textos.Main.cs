namespace Ondine.Localizacion;

/// <summary>
/// Los textos de la ventana principal (<c>MainWindow</c>): barra de título y
/// menús, conmutador de páginas, origen y destino, panel de opciones, tabla de
/// vídeos, panel lateral (detalle y estimación), acciones y todos los mensajes
/// que la ventana escribe en la línea de estado.
///
/// <para>
/// <b>Vocabulario fijado para toda la pantalla</b>, porque la tabla, los menús y
/// la línea de estado hablan de lo mismo y tienen que llamarlo igual:
/// <i>analizar</i> = <c>analyse</c>, <i>origen</i> = <c>source</c>,
/// <i>destino</i> = <c>destination</c>, <i>Papelera</i> = <c>Recycle Bin</c>
/// (el nombre que le da Windows), <i>pista</i> = <c>track</c>, <i>preset</i>
/// se queda igual.
/// </para>
/// <para>
/// Los nombres de las páginas (<see cref="MainPaginaComprimir"/> y compañía)
/// salen en tres sitios a la vez: el desplegable del conmutador, el rótulo de la
/// página actual y el título de algún diálogo. Por eso hay UNA clave para cada
/// una y no una por sitio.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ═══ Lo que no se traduce ═══════════════════════════════════════════════
    //
    // Nombres técnicos, marcas y códigos. Están en el catálogo porque el XAML no
    // admite texto suelto (la prueba lo caza), no porque haya nada que traducir:
    // los dos idiomas dicen lo mismo a propósito. La prueba de traducción ya
    // contempla estos casos en `EsIgualAProposito`, que es donde se declaran los
    // textos que de verdad son iguales en los dos idiomas.

    public string MainMarca => Idioma.Elegir("Ondine", "Ondine");

    // Valor inicial del desplegable de idioma principal: código ISO 639, no un
    // texto. Lo pisa en cuanto arranca `Settings.DefaultLang`.
    public string MainIdiomaPistaPorDefecto => Idioma.Elegir("spa", "spa");

    public string MainFormatoMkv => Idioma.Elegir("MKV", "MKV");
    public string MainFormatoMp4 => Idioma.Elegir("MP4", "MP4");
    public string MainFormatoWebm => Idioma.Elegir("WebM", "WebM");

    public string MainCodecH265 => Idioma.Elegir("H.265", "H.265");
    public string MainCodecH264 => Idioma.Elegir("H.264", "H.264");
    public string MainCodecAv1 => Idioma.Elegir("AV1", "AV1");

    public string MainResolucion1080 => Idioma.Elegir("1080p", "1080p");
    public string MainResolucion720 => Idioma.Elegir("720p", "720p");
    public string MainResolucion480 => Idioma.Elegir("480p", "480p");

    public string MainAudioKbps192 => Idioma.Elegir("192 kbps", "192 kbps");
    public string MainAudioKbps160 => Idioma.Elegir("160 kbps", "160 kbps");
    public string MainAudioKbps128 => Idioma.Elegir("128 kbps", "128 kbps");
    public string MainAudioKbps96 => Idioma.Elegir("96 kbps", "96 kbps");

    // ═══ Aviso de versión nueva ═════════════════════════════════════════════
    // El rótulo del aviso es solo el valor de partida: en cuanto hay una versión
    // de verdad lo reescribe MainVersionNuevaDetalle con el número.
    public string MainActualizacionDisponible =>
        Idioma.Elegir("There is a new version available.", "Hay una versión nueva disponible.");

    public string MainActualizarAhora => Idioma.Elegir("Update now", "Actualizar ahora");
    public string MainActualizarDespues => Idioma.Elegir("Later", "Después");

    // ═══ Menús ══════════════════════════════════════════════════════════════
    public string MainMenuArchivo => Idioma.Elegir("File", "Archivo");
    public string MainMenuElegirOrigen => Idioma.Elegir("Choose source folder…", "Elegir carpeta de origen…");
    public string MainMenuAnadirArchivos => Idioma.Elegir("Add files…", "Añadir archivos…");
    public string MainMenuAbrirDestino => Idioma.Elegir("Open destination folder", "Abrir carpeta de destino");

    // Sirve de rótulo del menú y de título del aviso de «hay algo comprimiendo».
    public string MainMenuSalir => Idioma.Elegir("Exit", "Salir");

    public string MainMenuSeleccion => Idioma.Elegir("Selection", "Selección");
    public string MainMenuHerramientas => Idioma.Elegir("Tools", "Herramientas");
    public string MainMenuRenombrarSalida => Idioma.Elegir("Rename output…", "Renombrar salida…");

    // Con puntos suspensivos porque abre una ventana; el título de esa ventana es
    // PreferenciasTitulo, que va sin ellos.
    public string MainComplementosTip => Idioma.Elegir(
        "Plugins that work on this page", "Complementos que sirven en esta página");

    public string MainComplementosGestionar => Idioma.Elegir(
        "Manage plugins...", "Gestionar complementos...");

    public string MainMenuComplementos => Idioma.Elegir("Plugins...", "Complementos...");

    public string MainMenuPreferencias => Idioma.Elegir("Preferences…", "Preferencias…");

    public string MainMenuAyuda => Idioma.Elegir("Help", "Ayuda");
    public string MainMenuTutoriales =>
        Idioma.Elegir("Tutorials · how it works…", "Tutoriales · cómo funciona…");

    // Rótulo del menú y título del propio cuadro de «Acerca de».
    public string MainMenuAcercaDe => Idioma.Elegir("About Ondine", "Acerca de Ondine");

    // ═══ Selección (menú, menú contextual y botones del lateral) ════════════
    // Las tres primeras salen en el menú «Selección», en el menú contextual de la
    // tabla y en los botones del panel lateral: una clave para los tres sitios,
    // que es la única forma de que no acaben diciendo tres cosas distintas.
    public string MainSeleccionarTodo => Idioma.Elegir("Select all", "Seleccionar todo");
    public string MainQuitarSeleccion => Idioma.Elegir("Clear selection", "Quitar selección");
    public string MainInvertirSeleccion => Idioma.Elegir("Invert selection", "Invertir selección");

    public string MainSeleccionarTodoTip => Idioma.Elegir(
        "Selects every video in the table (Ctrl+A). What is selected is what gets processed.",
        "Selecciona todos los vídeos de la tabla (Ctrl+A). Se procesa lo que esté seleccionado.");

    // «Mayús» es el nombre de la tecla en un teclado en castellano; en inglés, «Shift».
    public string MainQuitarSeleccionTip => Idioma.Elegir(
        "Leaves the table with nothing selected. Remember: you can select by dragging, or with Ctrl/Shift + click.",
        "Deja la tabla sin nada seleccionado. Recuerda: puedes seleccionar arrastrando, o con Ctrl/Mayús + clic.");

    // Vale para el menú y para la descripción emergente del botón del lateral. El
    // castellano lleva artículo («la selección») porque así estaba en el botón, que
    // es donde más se lee.
    public string MainEnviarSeleccionPapelera =>
        Idioma.Elegir("Send the selection to the Recycle Bin", "Enviar la selección a la Papelera");

    // Tecla física: en un teclado inglés la tecla se llama «Del».
    public string MainTeclaSuprimir => Idioma.Elegir("Del", "Supr");

    // ═══ Conmutador de páginas ══════════════════════════════════════════════
    // «Comprimir» sale además como título del aviso previo a comprimir y como
    // rótulo de su botón: es el mismo verbo y se traduce una vez.
    public string MainPaginaComprimir => Idioma.Elegir("Compress", "Comprimir");
    public string MainPaginaOrganizar => Idioma.Elegir("Organise", "Organizar");

    // El nombre de la página de recortes lo dicta su propia pantalla: se reutiliza
    // RecortesTitulo en vez de copiarlo, que es como las dos se acaban separando.
    public string MainPaginaRecortes => RecortesTitulo;

    public string MainCambiarPagina => Idioma.Elegir("Switch page", "Cambiar de página");

    // ── Píldora de tarea en segundo plano ───────────────────────────────────
    // OJO: el «✓» de las dos últimas es funcional, no adorno: ActualizarIndicadorGlobal
    // mira si la etiqueta empieza por «✓» para apagar el punto que late. Si se quita
    // de un idioma, en ese idioma el punto sigue latiendo con la tarea ya terminada.
    public string MainPildoraTip =>
        Idioma.Elegir("Back to the compression page", "Volver a la página de compresión");

    public string MainPildoraComprimiendo => Idioma.Elegir("Compressing", "Comprimiendo");

    // {0} = hechos, {1} = total, {2} = porcentaje ya multiplicado por 100.
    public string MainPildoraProgreso =>
        Idioma.Elegir("Compressing {0}/{1} · {2:0} %", "Comprimiendo {0}/{1} · {2:0} %");

    // {0} = cuántos. Dos claves porque el castellano hace plural y el inglés no.
    public string MainPildoraFinUno => Idioma.Elegir("✓ {0} done", "✓ {0} hecho");
    public string MainPildoraFinVarios => Idioma.Elegir("✓ {0} done", "✓ {0} hechos");

    // ═══ Origen y destino ═══════════════════════════════════════════════════
    // «Origen» vale de rótulo del campo y de título del aviso de origen inválido.
    public string MainOrigen => Idioma.Elegir("Source", "Origen");

    public string MainElegirCarpetaTip => Idioma.Elegir("Choose folder", "Elegir carpeta");
    public string MainAnadirArchivos => Idioma.Elegir("Add files", "Añadir archivos");
    public string MainAnadirArchivosTip =>
        Idioma.Elegir("Add one or more individual files", "Añadir uno o varios archivos sueltos");

    // El rótulo de destino son dos trozos con colores distintos: el espacio final
    // de este es lo que los separa, así que va dentro del texto en los dos idiomas.
    public string MainDestino => Idioma.Elegir("Destination ", "Destino ");
    public string MainOpcional => Idioma.Elegir("(optional)", "(opcional)");

    // «comprimido» NO se traduce: es el nombre literal de la carpeta que crea
    // EffectiveOutput. Traducirlo aquí prometería una carpeta que no existe.
    public string MainDestinoTip => Idioma.Elegir(
        "Empty = a “comprimido” subfolder next to the source",
        "Vacío = subcarpeta «comprimido» junto al origen");

    public string MainElegirDestinoTip => Idioma.Elegir("Choose destination", "Elegir destino");

    // ═══ Panel de opciones ══════════════════════════════════════════════════
    public string MainPreset => Idioma.Elegir("Preset", "Preset");

    public string MainPresetTip => Idioma.Elegir(
        "Ready-made combinations of settings. Choosing one fills in every option below.",
        "Combinaciones de ajustes ya preparadas. Al elegir una se rellenan todas las opciones de abajo.");

    public string MainGuardarPreset => Idioma.Elegir("Save current preset", "Guardar preset actual");

    public string MainGuardarPresetTip => Idioma.Elegir(
        "Saves the current settings under a name so you can reuse them later.",
        "Guarda los ajustes actuales con un nombre para reutilizarlos luego.");

    // PowerRename es el nombre de la herramienta de Microsoft: no se traduce.
    public string MainRenombrarTip => Idioma.Elegir(
        "Rename the output files PowerRename style (search/replace, regex, counter, date…)",
        "Renombrar los archivos de salida al estilo PowerRename (buscar/reemplazar, regex, contador, fecha…)");

    // El «✓» avisa de que hay una regla activa: el rótulo es la única señal de que
    // los ficheros van a salir con otro nombre, así que se conserva en los dos.
    public string MainRenombrarSalida => Idioma.Elegir("Rename output", "Renombrar salida");
    public string MainRenombrarSalidaActiva => Idioma.Elegir("Rename output ✓", "Renombrar salida ✓");

    public string MainIdiomaPrincipal => Idioma.Elegir("Main language", "Idioma principal");

    public string MainIdiomaPrincipalTip => Idioma.Elegir(
        "The language marked as the default audio track. It fills up with the languages detected when analysing.",
        "Idioma que se marca como pista de audio por defecto. Se llena con los idiomas detectados al analizar.");

    public string MainFormato => Idioma.Elegir("Format", "Formato");

    public string MainFormatoTip => Idioma.Elegir(
        "Container of the final file. MKV takes several audio tracks and subtitles without any trouble; MP4 is the most compatible one (phones, Plex, smart TVs); WebM is for the web. The “audio only” options drop the video and pull out the sound track.",
        "Contenedor del archivo final. MKV admite varios audios y subtítulos sin problemas; MP4 es el más compatible (móviles, Plex, smart TV); WebM es para web. Las opciones «solo audio» descartan el vídeo y extraen la pista sonora.");

    // El orden de estos cuatro está acoplado a los índices de BuildOptions: se
    // traduce el rótulo, nunca se reordena la lista.
    public string MainFormatoMp3 => Idioma.Elegir("MP3 · audio only", "MP3 · solo audio");
    public string MainFormatoM4a => Idioma.Elegir("M4A · audio only", "M4A · solo audio");
    public string MainFormatoFlac => Idioma.Elegir("FLAC · audio only", "FLAC · solo audio");
    public string MainFormatoOpus => Idioma.Elegir("Opus · audio only", "Opus · solo audio");

    public string MainCodec => Idioma.Elegir("Codec", "Códec");

    public string MainCodecTip => Idioma.Elegir(
        "Video codec. H.265 is the recommended balance: it saves a lot and uses your GPU. H.264 compresses less but anything at all plays it. AV1 saves the most, but your machine has no hardware acceleration for it and it would take hours.",
        "Códec de vídeo. H.265 es el equilibrio recomendado: ahorra mucho y usa tu GPU. H.264 comprime menos pero lo reproduce cualquier cosa. AV1 es el que más ahorra, pero tu equipo no lo acelera por hardware y tardaría horas.");

    // Vale para el desplegable de calidad y para las dos filas de «Calidad» de la
    // estimación: es la misma idea, y en la estimación el hueco es de 54 px.
    public string MainCalidad => Idioma.Elegir("Quality", "Calidad");

    // ── Llegar a un tamaño concreto ─────────────────────────────────────────
    // Va como una opción más del desplegable de calidad porque es lo mismo dicho
    // al revés: o eliges calidad y ves qué pesa, o eliges cuánto pesa y el motor
    // saca la calidad. No son dos ajustes: son dos formas de pedir lo mismo.
    public string MainCalidadObjetivo => Idioma.Elegir("Target size…", "Tamaño objetivo…");

    // ── Cuánto se esmera el codificador ─────────────────────────────────────
    // Cinco pasos y no los diez de x265: nadie sabe qué hay entre «superfast» y
    // «veryfast», y ofrecerlo solo hace la elección más difícil sin mejorarla.
    // ── El códec del audio ──────────────────────────────────────────────────
    // «Sin tocar» primero porque es lo que la app hacía y sigue siendo lo mejor
    // cuando el contenedor lo admite: copiar no pierde nada y no cuesta tiempo.
    public string MainAudioCodec => Idioma.Elegir("Audio codec", "Códec de audio");

    public string MainAudioCodecTip => Idioma.Elegir(
        "What to do with each audio track. «Untouched» copies the bytes as they are — nothing " +
        "lost, no time spent — whenever the container allows it. AC3 is what an older living-room " +
        "receiver understands; Opus weighs about half for the same quality. If what you pick does " +
        "not fit the chosen format, it is changed and said so in the log.",
        "Qué hacer con cada pista de audio. «Sin tocar» copia los bytes tal cual —no se pierde " +
        "nada ni cuesta tiempo— siempre que el contenedor lo admita. AC3 es lo que entiende un " +
        "receptor de salón antiguo; Opus pesa como la mitad con la misma calidad. Si lo que " +
        "eliges no cabe en el formato elegido, se cambia y se dice en el registro.");

    public string MainAudioSinTocar => Idioma.Elegir("Untouched", "Sin tocar");

    // ── La mezcla ───────────────────────────────────────────────────────────
    public string MainMezcla => Idioma.Elegir("Channels", "Canales");

    public string MainMezclaTip => Idioma.Elegir(
        "A 5.1 track weighs about twice a stereo one and does nothing for headphones or the " +
        "bedroom TV. Mixing it down is what turns an 8 GB film into a 5 GB one without touching " +
        "the video. It never goes the other way: asking for stereo on something already stereo " +
        "does nothing at all.",
        "Una pista 5.1 pesa como el doble que una estéreo y no aporta nada en unos auriculares ni " +
        "en la tele del cuarto. Bajarla es lo que convierte una película de 8 GB en una de 5 sin " +
        "tocar el vídeo. Nunca va al revés: pedir estéreo sobre algo que ya lo es no hace nada.");

    public string MainMezclaComoEste => Idioma.Elegir("As they come", "Como vengan");
    public string MainMezclaEstereo => Idioma.Elegir("Down to stereo", "Bajar a estéreo");

    // Los nombres de los códecs no se traducen: son nombres propios. Pasan por el
    // catálogo igual que MKV o MP4 —misma cadena en los dos idiomas— porque el arnés
    // exige que TODO texto de pantalla salga de aquí, y esa regla vale justo porque no
    // admite excepciones que luego alguien amplía.
    public string MainAudioAac => Idioma.Elegir("AAC", "AAC");
    public string MainAudioAc3 => Idioma.Elegir("AC3", "AC3");
    public string MainAudioEac3 => Idioma.Elegir("E-AC3", "E-AC3");
    public string MainAudioOpus => Idioma.Elegir("Opus", "Opus");
    public string MainAudioFlac => Idioma.Elegir("FLAC", "FLAC");

    public string MainVelocidad => Idioma.Elegir("Effort", "Esmero");

    public string MainVelocidadTip => Idioma.Elegir(
        "How hard the encoder works. Slower means smaller at the same quality — nothing else " +
        "changes. Each codec family has its own scale; this picks the right one for whichever " +
        "ends up being used.",
        "Cuánto trabaja el codificador. Más lento significa más pequeño con la misma calidad, y " +
        "nada más cambia. Cada familia de códecs tiene su propia escala; esto elige la que toca " +
        "al que acabe usándose.");

    public string MainVelocidadMuyRapido => Idioma.Elegir("Fastest", "Lo más rápido");
    public string MainVelocidadRapido => Idioma.Elegir("Fast", "Rápido");
    public string MainVelocidadEquilibrado => Idioma.Elegir("Balanced", "Equilibrado");
    public string MainVelocidadLento => Idioma.Elegir("Slow", "Lento");
    public string MainVelocidadMuyLento => Idioma.Elegir("Slowest", "Lo más lento");

    public string MainObjetivoMB => Idioma.Elegir("MB per file", "MB por fichero");

    public string MainObjetivoTip => Idioma.Elegir(
        "The size each file should end up at. The bitrate is worked out per file, because a " +
        "short clip and a film need very different ones to land on the same size. The result " +
        "is approximate: it is a single pass, not two.",
        "El tamaño al que debe quedar cada fichero. El bitrate se calcula por fichero, porque " +
        "un clip corto y una película necesitan bitrates muy distintos para pesar lo mismo. El " +
        "resultado es aproximado: es una pasada, no dos.");

    // {0} = los MB que harían falta como mínimo
    public string MainObjetivoNoCabe => Idioma.Elegir(
        "{0} MB is not enough for this: the video would come out unwatchable. It needs at least {1} MB.",
        "Con {0} MB no da: el vídeo saldría ilegible. Hacen falta al menos {1} MB.");

    public string MainObjetivoNoCabeNiElAudio => Idioma.Elegir(
        "Not even the audio fits in {0} MB. Lowering the video quality would not help — take " +
        "audio tracks out, or aim higher.",
        "En {0} MB no cabe ni el audio. Bajar la calidad del vídeo no arreglaría nada: quita " +
        "pistas de audio, o apunta más alto.");


    public string MainCalidadTip => Idioma.Elegir(
        "Target quality (CRF): a lower number = better picture and a bigger file. It does not fix the size, it fixes the quality, so the final weight depends on how complex the video is. “Automatic” uses 27, balanced.",
        "Calidad objetivo (CRF): número más bajo = mejor imagen y archivo más grande. No fija el tamaño, fija la calidad, así que el peso final depende de lo complejo que sea el vídeo. «Automática» usa 27, equilibrado.");

    public string MainCalidadAuto => Idioma.Elegir("Automatic", "Automática");
    public string MainCalidad22 => Idioma.Elegir("22 · very high", "22 · muy alta");
    public string MainCalidad24 => Idioma.Elegir("24 · high", "24 · alta");
    public string MainCalidad27 => Idioma.Elegir("27 · balanced", "27 · equilibrada");
    public string MainCalidad30 => Idioma.Elegir("30 · heavily compressed", "30 · muy comprimida");

    // Abreviado en los dos idiomas: la columna del panel de opciones no da más.
    public string MainResolucionMax => Idioma.Elegir("Max. resolution", "Resolución máx.");

    public string MainResolucionTip => Idioma.Elegir(
        "Lowers the resolution if the video goes above it (it never raises it). Going from 1080p down to 720p cuts the weight to less than half and hardly shows on small screens.",
        "Reduce la resolución si el vídeo la supera (nunca la aumenta). Bajar de 1080p a 720p recorta el peso a menos de la mitad y apenas se nota en pantallas pequeñas.");

    public string MainResolucionSinCambio => Idioma.Elegir("No change", "Sin cambio");

    public string MainAudio => Idioma.Elegir("Audio", "Audio");

    public string MainAudioTip => Idioma.Elegir(
        "Bitrate for the audio, and it only applies when it is being re-encoded — which is decided by “Audio codec”, right next to it. With “Untouched” the bytes are copied and this is greyed out. Mind you, the weight of the audio multiplies by every language you keep.",
        "Caudal del audio, y solo cuenta cuando se está recodificando — eso lo decide «Códec de audio», el de al lado. Con «Sin tocar» se copian los bytes y esto se apaga. Ojo: el peso del audio se multiplica por cada idioma que conserves.");

    /// <summary>
    /// El índice 0 del caudal en Comprimir: que lo elija la app.
    ///
    /// <para>
    /// Antes decía «Máxima (copiar original)», y eso ya no es lo que hace: copiar o no lo decide
    /// el desplegable del códec. Cuando se recodifica, esto significa que el caudal lo pone la
    /// app según los canales que queden (192 kbps, o 448 en un 5.1).
    /// </para>
    /// </summary>
    public string MainAudioAuto => Idioma.Elegir("Automatic", "Automático");

    /// <summary>
    /// El índice 0 del caudal en RECORTES, donde sí significa copiar: esa pantalla no tiene
    /// desplegable de códec, así que su único control de audio decide las dos cosas.
    /// </summary>
    public string MainAudioCopiar => Idioma.Elegir("Maximum (copy the original)", "Máxima (copiar original)");

    // Cita la casilla «Analizar subcarpetas» de Preferencias (PreferenciasSubcarpetas).
    public string MainSubcarpetas => Idioma.Elegir("Subfolders", "Subcarpetas");

    public string MainSubcarpetasTip => Idioma.Elegir(
        "When analysing, it also goes into the folders inside the source. It is the same setting as “Scan subfolders” in Preferences: what you change here is saved.",
        "Al analizar, entra también en las carpetas de dentro del origen. Es el mismo ajuste que «Analizar subcarpetas» de Preferencias: lo que cambies aquí se guarda.");

    public string MainReprocesar => Idioma.Elegir("Redo the done ones", "Reprocesar hechos");

    public string MainReprocesarTip => Idioma.Elegir(
        "Compresses again even if the output file already exists, and without skipping the videos that were already well compressed.",
        "Vuelve a comprimir aunque ya exista el archivo de salida, y sin saltarse los vídeos que ya estaban bien comprimidos.");

    public string MainSoloSimular => Idioma.Elegir("Dry run only", "Solo simular");

    public string MainSoloSimularTip => Idioma.Elegir(
        "Dry run: it goes down the list and says what it would do with each video, but it neither creates nor deletes any file.",
        "Ensayo en seco: recorre la lista y dice qué haría con cada vídeo, pero no crea ni borra ningún archivo.");

    // ── Idiomas detectados ──────────────────────────────────────────────────
    public string MainAudioDetectado => Idioma.Elegir("Audio detected", "Audio detectado");
    public string MainSubtitulos => Idioma.Elegir("Subtitles", "Subtítulos");

    // Cita el botón «Analizar» (MainAnalizar).
    public string MainPistaIdiomas =>
        Idioma.Elegir("(press Analyse to detect them)", "(pulsa Analizar para detectarlos)");

    // El espacio de delante es parte del texto: va pegado a los chips de idioma.
    public string MainDetectando => Idioma.Elegir(" detecting…", " detectando…");

    public string MainNadaQueAnalizar => Idioma.Elegir("(nothing to analyse)", "(nada que analizar)");
    public string MainSinSubtitulosDetectados =>
        Idioma.Elegir("(no subtitles detected)", "(sin subtítulos detectados)");

    // ═══ Tabla de vídeos ════════════════════════════════════════════════════
    // Cabeceras en mayúsculas por diseño; el ancho está fijado en píxeles, así que
    // las dos versiones tienen que ser cortas.
    public string MainColVideo => Idioma.Elegir("VIDEO", "VÍDEO");
    public string MainColCarpeta => Idioma.Elegir("FOLDER", "CARPETA");
    public string MainColTamano => Idioma.Elegir("SIZE", "TAMAÑO");
    public string MainColDuracion => Idioma.Elegir("LENGTH", "DURACIÓN");
    public string MainColCodec => Idioma.Elegir("CODEC", "CÓDEC");
    public string MainColAudio => Idioma.Elegir("AUDIO", "AUDIO");
    public string MainColSubs => Idioma.Elegir("SUBS", "SUBS");
    public string MainColEstado => Idioma.Elegir("STATUS", "ESTADO");

    // ── Menú contextual de la tabla ─────────────────────────────────────────
    // Quitar de la LISTA no toca el fichero; enviar a la Papelera sí. La
    // diferencia tiene que leerse de un vistazo en los dos idiomas.
    public string MainCtxQuitar => Idioma.Elegir("Remove from the list", "Quitar de la lista");

    // {0} = cuántas filas.
    public string MainCtxQuitarVarias => Idioma.Elegir("Remove {0} from the list", "Quitar {0} de la lista");

    public string MainCtxPapelera =>
        Idioma.Elegir("Send the file to the Recycle Bin…", "Enviar el archivo a la Papelera…");

    // {0} = cuántos ficheros.
    public string MainCtxPapeleraVarios =>
        Idioma.Elegir("Send {0} files to the Recycle Bin…", "Enviar {0} archivos a la Papelera…");

    public string MainCtxPistas =>
        Idioma.Elegir("Remove audio or subtitle tracks…", "Quitar pistas de audio o subtítulos…");

    public string MainCtxAbrirCarpeta => Idioma.Elegir("Open containing folder", "Abrir carpeta contenedora");
    public string MainCtxCopiarRuta => Idioma.Elegir("Copy path", "Copiar ruta");

    // ── Estado vacío ────────────────────────────────────────────────────────
    public string MainVacioTitulo =>
        Idioma.Elegir("Your videos will show up here", "Aquí aparecerán tus vídeos");

    // Cita el campo «Origen» y el botón «Analizar»: si cambian, cambia esto.
    public string MainVacioTexto => Idioma.Elegir(
        "Choose a folder under Source (or drag videos here) and press Analyse. The ones worth compressing get ticked on their own.",
        "Elige una carpeta en Origen (o arrastra vídeos aquí) y pulsa Analizar. Se marcarán solos los que convenga comprimir.");

    // ── Estado de cada fila ─────────────────────────────────────────────────
    // Caben en 132 px: cortos los dos. «Pendiente» y «Error» ya están en Comun.
    // {0} = el códec que ya trae el fichero, en mayúsculas.
    public string MainEstadoYaEn => Idioma.Elegir("Already in {0}", "Ya en {0}");

    public string MainEstadoEnCola => Idioma.Elegir("Queued", "En cola");
    public string MainEstadoComprimiendo => Idioma.Elegir("Compressing…", "Comprimiendo…");

    // {0} = porcentaje entero.
    public string MainEstadoComprimiendoPct => Idioma.Elegir("Compressing… {0}%", "Comprimiendo… {0}%");

    public string MainEstadoErrorAlBorrar => Idioma.Elegir("delete failed", "error al borrar");

    // ═══ Panel lateral ══════════════════════════════════════════════════════
    public string MainDetalle => Idioma.Elegir("Details", "Detalle");

    public string MainDetalleTip => Idioma.Elegir(
        "Details of the selected video and a 10 s preview with the current settings.",
        "Ficha del vídeo seleccionado y previsualización de 10 s con los ajustes actuales.");

    public string MainEstimacion => Idioma.Elegir("Estimate", "Estimación");

    public string MainEstimacionTip => Idioma.Elegir(
        "Forecast of size and saving for the selected video with the current settings.",
        "Pronóstico de tamaño y ahorro del vídeo seleccionado con los ajustes actuales.");

    // Ficha del vídeo. {0} carpeta · {1} tamaño · {2} duración · {3} códec · {4} idiomas de audio.
    // En inglés el rótulo dice «audio tracks» y no «audio» a secas porque lo que
    // sigue es una lista de idiomas: «audio: spa+eng» se lee como si fuera el códec.
    public string MainDetalleInfo => Idioma.Elegir(
        "{0}  ·  {1}  ·  {2}\n{3}  ·  audio tracks: {4}",
        "{0}  ·  {1}  ·  {2}\n{3}  ·  audio: {4}");

    // {0} = idiomas de subtítulos. El castellano abrevia porque el panel es
    // estrecho; en inglés «subtitles» cabe y se entiende mejor que «subs».
    public string MainDetalleSubs => Idioma.Elegir("  ·  subtitles: {0}", "  ·  subs: {0}");

    // {0} = el momento del vídeo, ya formateado como m:ss.
    public string MainPrevisualizarDesde => Idioma.Elegir("Preview from {0}", "Previsualizar desde {0}");

    public string MainPrevisualizar10s => Idioma.Elegir("Preview 10 s", "Previsualizar 10 s");

    public string MainPrevisualizarTip => Idioma.Elegir(
        "Renders 10 s with the current settings so you can see the result. It deletes itself.",
        "Renderiza 10 s con los ajustes actuales para ver el resultado. Se borra sola.");

    // ── Estimación ──────────────────────────────────────────────────────────
    public string MainEstTamano => Idioma.Elegir("ESTIMATED SIZE", "TAMAÑO ESTIMADO");
    public string MainEstVideo => Idioma.Elegir("VIDEO", "VÍDEO");
    public string MainEstAudio => Idioma.Elegir("AUDIO", "AUDIO");
    public string MainAhorro => Idioma.Elegir("Saving", "Ahorro");

    public string MainEstSinSeleccion =>
        Idioma.Elegir("Select a video you have analysed", "Selecciona un vídeo analizado");

    // {0} = porcentaje ahorrado, {1} = tamaño ahorrado ya formateado.
    public string MainEstAhorroResumen => Idioma.Elegir("−{0}% · you save {1}", "−{0}% · ahorras {1}");

    // {0} y {1} = caudales en kbps.
    public string MainEstDetalle => Idioma.Elegir(
        "Video ≈ {0} kbps · audio ≈ {1} kbps. Rough estimate; the real result depends on the content.",
        "Vídeo ≈ {0} kbps · audio ≈ {1} kbps. Estimación aproximada; el resultado real depende del contenido.");

    // ── Medir con una muestra ───────────────────────────────────────────────
    public string MainMedir => Idioma.Elegir("Measure with a sample", "Medir con una muestra");

    // Título de los avisos de la medición.
    public string MainMedirTitulo => Idioma.Elegir("Measure", "Medir");

    public string MainMedirTip => Idioma.Elegir(
        "Encodes 3 short chunks of the video with these very settings and measures what they really take up. It takes a few seconds and turns the forecast into a reliable measurement, which also corrects the rest of the list.",
        "Codifica 3 fragmentos cortos del vídeo con estos mismos ajustes y mide lo que ocupan de verdad. Tarda unos segundos y convierte el pronóstico en una medición fiable, que además corrige el resto de la lista.");

    public string MainMedirNota => Idioma.Elegir(
        "The forecast is a guide: quality is set with CRF, so the real size depends on the content.",
        "El pronóstico es orientativo: la calidad se fija con CRF, así que el tamaño real depende del contenido.");

    public string MainMedirEspera => Idioma.Elegir(
        "Wait for the compression under way to finish.",
        "Espera a que termine la compresión en curso.");

    public string MainMedirSoloAudio => Idioma.Elegir(
        "In “audio only” mode the size is already exact: there is nothing to measure.",
        "En modo «solo audio» el tamaño ya es exacto: no hace falta medir.");

    public string MainMidiendo => Idioma.Elegir("Measuring with real samples…", "Midiendo con muestras reales…");

    public string MainMidiendoFragmentos => Idioma.Elegir(
        "Encoding 3 chunks with these settings…",
        "Codificando 3 fragmentos con estos ajustes…");

    public string MainMedirFallo => Idioma.Elegir("This video could not be measured.", "No se pudo medir este vídeo.");
    public string MainMedirFalloCorto => Idioma.Elegir("Could not measure.", "No se pudo medir.");

    // {0} = kbps medidos, {1} = uno de los tres veredictos de abajo, {2} = factor.
    // El veredicto lleva dentro la comparación («de lo típico» / «than usual»)
    // porque en inglés «Content average than usual» no se sostiene.
    public string MainMedidoResultado => Idioma.Elegir(
        "Measured: video ≈ {0} kbps. Content {1} (×{2:0.00}); the rest of the list already uses this calibration.",
        "Medido: vídeo ≈ {0} kbps. Contenido {1} (×{2:0.00}); el resto de la lista ya usa esta calibración.");

    public string MainMedidoFacil => Idioma.Elegir("easier than usual", "más fácil de lo típico");
    public string MainMedidoExigente => Idioma.Elegir("more demanding than usual", "más exigente de lo típico");
    public string MainMedidoNormal => Idioma.Elegir("average", "normal");

    public string MainMedicionAplicada =>
        Idioma.Elegir("Measurement applied to the estimate.", "Medición aplicada a la estimación.");

    public string MainMedicionCancelada => Idioma.Elegir("Measurement cancelled.", "Medición cancelada.");

    // {0} = el mensaje de la excepción, que lo escribe .NET y no pasa por aquí.
    public string MainMedirError => Idioma.Elegir("Error while measuring: {0}", "Error al medir: {0}");

    // ── Previsualización de 10 s ────────────────────────────────────────────
    public string MainPrevisualizarTitulo => Idioma.Elegir("Preview", "Previsualizar");

    // Vale para el aviso de «medir» y para el de «previsualizar»: los dos piden lo mismo.
    public string MainSeleccionaVideoAnalizado =>
        Idioma.Elegir("Select a video you have analysed.", "Selecciona un vídeo analizado.");

    // El «■» es el icono de parar: se queda en los dos idiomas.
    public string MainCancelarPrevisualizacion =>
        Idioma.Elegir("■  Cancel preview", "■  Cancelar previsualización");

    public string MainGenerandoPrevisualizacion =>
        Idioma.Elegir("Generating preview (10 s)…", "Generando previsualización (10 s)…");

    public string MainPrevisualizacionLista =>
        Idioma.Elegir("Preview ready (it deletes itself).", "Previsualización lista (se borra sola).");

    public string MainPrevisualizacionFallo =>
        Idioma.Elegir("The preview could not be generated.", "No se pudo generar la previsualización.");

    public string MainPrevisualizacionCancelada =>
        Idioma.Elegir("Preview cancelled.", "Previsualización cancelada.");

    // {0} = el mensaje de la excepción.
    public string MainPrevisualizacionError =>
        Idioma.Elegir("Error in the preview: {0}", "Error en la previsualización: {0}");

    // ═══ Borrar ═════════════════════════════════════════════════════════════
    public string MainEliminarTitulo => Idioma.Elegir("Delete", "Eliminar");
    public string MainEliminarSeleccion => Idioma.Elegir("Delete selection", "Eliminar selección");

    // Rótulo del botón y título del aviso de borrar carpetas.
    public string MainEliminarCarpeta => Idioma.Elegir("Delete folder", "Eliminar carpeta");

    public string MainEliminarCarpetaTip =>
        Idioma.Elegir("Send a whole folder to the Recycle Bin", "Enviar una carpeta entera a la Papelera");

    public string MainSinVideosSeleccionados =>
        Idioma.Elegir("No videos are selected.", "No hay vídeos seleccionados.");

    public string MainEliminarMarcadosTitulo => Idioma.Elegir("Delete the ticked ones", "Eliminar marcados");

    // {0} = cuántos vídeos, {1} = megas en total.
    public string MainEliminarMarcadosPregunta => Idioma.Elegir(
        "Send {0} video(s) ({1:n0} MB) to the Recycle Bin?",
        "¿Enviar {0} vídeo(s) ({1:n0} MB) a la Papelera de reciclaje?");

    // {0} = el nombre del último fichero enviado, que es el que recupera Ctrl+Z.
    public string MainALaPapeleraDeshacer => Idioma.Elegir(
        "To the Recycle Bin. Ctrl+Z brings “{0}” back.",
        "A la Papelera. Ctrl+Z para recuperar «{0}».");

    public string MainEnviadosALaPapelera => Idioma.Elegir("Sent to the Recycle Bin.", "Enviados a la Papelera.");

    public string MainEliminarCarpetaSinNada => Idioma.Elegir(
        "Tick some video or point to a source folder.",
        "Marca algún vídeo o indica una carpeta de origen.");

    // {0} = cuántas carpetas, {1} = la lista de rutas, una por línea. Las
    // mayúsculas de COMPLETAS son el aviso: se lleva todo lo que haya dentro.
    public string MainEliminarCarpetaPregunta => Idioma.Elegir(
        "Send these {0} WHOLE folder(s) to the Recycle Bin?\n\n{1}",
        "¿Enviar estas {0} carpeta(s) COMPLETAS a la Papelera?\n\n{1}");

    public string MainCarpetasALaPapelera =>
        Idioma.Elegir("Folder(s) sent to the Recycle Bin.", "Carpeta(s) enviadas a la Papelera.");

    // ── Papelera propia: quitar de la lista y deshacer ──────────────────────
    public string MainQuitadoUno => Idioma.Elegir(
        "1 video removed from the list (the file stays where it was).",
        "1 vídeo quitado de la lista (el archivo sigue en su sitio).");

    // {0} = cuántos.
    public string MainQuitadosVarios => Idioma.Elegir(
        "{0} videos removed from the list (the files stay where they were).",
        "{0} vídeos quitados de la lista (los archivos siguen en su sitio).");

    public string MainNadaQueRecuperar =>
        Idioma.Elegir("There is nothing to bring back.", "No hay nada que recuperar.");

    public string MainNoSePudoRecuperar => Idioma.Elegir(
        "Could not bring it back (its place is taken).",
        "No se pudo recuperar (su sitio ya está ocupado).");

    // {0} = el nombre del fichero.
    public string MainRecuperado => Idioma.Elegir("“{0}” brought back.", "Recuperado «{0}».");

    // ═══ Pistas (quitar audios o subtítulos sin recomprimir) ════════════════
    public string MainPistasTitulo => Idioma.Elegir("Tracks", "Pistas");
    public string MainLeyendoPistas => Idioma.Elegir("Reading the tracks…", "Leyendo las pistas…");

    public string MainPistasNoLeidas => Idioma.Elegir(
        "The tracks of this file could not be read.",
        "No se han podido leer las pistas de este fichero.");

    public string MainPistasSoloVideo => Idioma.Elegir(
        "This file only carries video: there are no audio or subtitle tracks to remove.",
        "Este fichero solo trae vídeo: no hay pistas de audio ni de subtítulos que quitar.");

    // {0} = el nombre del fichero.
    public string MainPistasQuitadas =>
        Idioma.Elegir("Tracks removed from “{0}”.", "Pistas quitadas de «{0}».");

    // ═══ Acciones ═══════════════════════════════════════════════════════════
    public string MainAnalizar => Idioma.Elegir("Analyse", "Analizar");

    public string MainAnalizarTip => Idioma.Elegir(
        "Looks for videos in the source and reads their tracks (length, codec, languages). It is needed for the estimate and the preview.",
        "Busca vídeos en el origen y lee sus pistas (duración, códec, idiomas). Hace falta para la estimación y la previsualización.");

    public string MainComprimirSeleccion => Idioma.Elegir("Compress selection", "Comprimir selección");

    public string MainComprimirTip => Idioma.Elegir(
        "Compresses the videos selected in the table. The originals are not touched unless you allow it in the warning beforehand.",
        "Comprime los vídeos seleccionados en la tabla. Los originales no se tocan salvo que lo autorices en el aviso previo.");

    // ── La cola de trabajos ─────────────────────────────────────────────────
    // Cada trabajo se lleva SUS ajustes. Sin esto, para comprimir dos cosas con
    // opciones distintas habia que esperar a que acabase la primera.
    public string MainEncolar => Idioma.Elegir("Add to queue", "Añadir a la cola");

    public string MainEncolarTip => Idioma.Elegir(
        "Puts the ticked files in the queue with the settings as they are RIGHT NOW, and leaves " +
        "you free to change them for the next job. The queue runs one job after another.",
        "Mete los ficheros marcados en la cola con los ajustes tal y como están AHORA, y te deja " +
        "libre para cambiarlos para el trabajo siguiente. La cola va despachando uno tras otro.");

    public string MainColaTitulo => Idioma.Elegir("Queue", "Cola");

    // {0} = trabajos que esperan · {1} = ficheros que quedan, contando el que corre
    public string MainColaResumen => Idioma.Elegir(
        "{0} waiting · {1} files to go",
        "{0} esperando · {1} ficheros por hacer");

    public string MainColaVacia => Idioma.Elegir(
        "Nothing queued. Tick some files, set them up and press «Add to queue».",
        "Nada en la cola. Marca ficheros, ajústalos y pulsa «Añadir a la cola».");

    // {0} = cuantos ficheros trae el trabajo · {1} = codec · {2} = calidad
    public string MainColaTrabajo => Idioma.Elegir(
        "{0} files · {1} · quality {2}",
        "{0} ficheros · {1} · calidad {2}");

    public string MainColaTrabajoUno => Idioma.Elegir(
        "1 file · {1} · quality {2}",
        "1 fichero · {1} · calidad {2}");

    public string MainColaEstadoPendiente => Idioma.Elegir("waiting", "esperando");
    public string MainColaEstadoEnCurso => Idioma.Elegir("running", "en curso");
    public string MainColaEstadoHecho => Idioma.Elegir("done", "hecho");
    public string MainColaEstadoFallido => Idioma.Elegir("failed", "falló");
    public string MainColaEstadoCancelado => Idioma.Elegir("cancelled", "cancelado");

    // {0} = los que salieron · {1} = los que no
    public string MainColaEstadoAMedias => Idioma.Elegir(
        "{0} out, {1} failed", "{0} salieron, {1} no");

    public string MainColaSubir => Idioma.Elegir("Move up", "Subir");
    public string MainColaBajar => Idioma.Elegir("Move down", "Bajar");
    public string MainColaQuitar => Idioma.Elegir("Remove", "Quitar");

    public string MainColaNoSeMueve => Idioma.Elegir(
        "The job that is running does not move: it is already writing to disk.",
        "El trabajo en curso no se mueve: ya está escribiendo en el disco.");

    // {0} = los ficheros repetidos, separados por comas
    public string MainColaYaEnCola => Idioma.Elegir(
        "These are already queued: {0}. Queuing them twice is allowed —two formats of the same " +
        "original, for instance— but the second job would read a file the first one may have " +
        "sent to the bin. Add them anyway?",
        "Estos ya están en la cola: {0}. Encolarlos dos veces se puede —dos formatos del mismo " +
        "original, por ejemplo— pero el segundo trabajo leería un fichero que el primero puede " +
        "haber mandado a la papelera. ¿Se añaden igual?");

    public string MainPausar => Idioma.Elegir("Pause", "Pausar");
    public string MainReanudar => Idioma.Elegir("Resume", "Reanudar");

    public string MainPausarTip => Idioma.Elegir(
        "Freezes the compression without losing what the file under way has already got through. Handy to free up the machine for a while.",
        "Congela la compresión sin perder lo ya avanzado del archivo en curso. Útil para liberar el equipo un rato.");

    public string MainDetener => Idioma.Elegir("Stop", "Detener");

    public string MainDetenerTip => Idioma.Elegir(
        "Cuts the process short and discards the half-done file. What is already finished is kept.",
        "Corta el proceso y descarta el archivo a medias. Lo ya terminado se conserva.");

    public string MainAbrirDestino => Idioma.Elegir("Open destination", "Abrir destino");

    public string MainAbrirDestinoTip => Idioma.Elegir(
        "Opens the folder where the compressed files are saved in File Explorer.",
        "Abre en el explorador la carpeta donde se guardan los comprimidos.");

    public string MainRegistro => Idioma.Elegir("Log", "Registro");

    // ── Líneas que la ventana escribe en el «Registro» ──────────────────────
    // Las escribe MainWindow, no el motor, pero salen intercaladas con las suyas
    // (Textos.Motor.cs): mismo sangrado de cuatro espacios y misma voz, o el
    // panel se lee como si lo hubieran redactado dos personas distintas.
    // {0} = el nombre del fichero original.
    public string MainLogOriginalAPapelera =>
        Idioma.Elegir("    original to the Recycle Bin: {0}", "    original a la Papelera: {0}");

    public string MainLogPapeleraFallo => Idioma.Elegir(
        "    could not send to the Recycle Bin: {0}",
        "    no se pudo enviar a la Papelera: {0}");

    // ═══ Línea de estado ════════════════════════════════════════════════════
    // Valor de reposo, antes de que pase nada. Comun.Listo dice «Done» y este
    // dice «Ready»: aquí no ha terminado nada todavía.
    public string MainProgresoListo => Idioma.Elegir("Ready.", "Listo.");

    public string MainPreferenciasGuardadas => Idioma.Elegir("Preferences saved.", "Preferencias guardadas.");

    public string MainRenombradoActivo => Idioma.Elegir("Renaming rule active.", "Regla de renombrado activa.");

    public string MainRenombradoDesactivado => Idioma.Elegir(
        "Renaming off: the output keeps the original name.",
        "Renombrado desactivado: la salida conserva el nombre original.");

    // {0} = cuántos vídeos.
    public string MainAnadiendoDesdeExplorador => Idioma.Elegir(
        "Adding {0} video(s) from File Explorer…",
        "Añadiendo {0} vídeo(s) desde el Explorador…");

    public string MainYaEstabanEnLista =>
        Idioma.Elegir("Those videos were already in the list.", "Esos vídeos ya estaban en la lista.");

    // {0} = cuántos.
    public string MainAnadidosDesdeExplorador => Idioma.Elegir(
        "{0} video(s) added from File Explorer.",
        "{0} vídeo(s) añadidos desde el Explorador.");

    public string MainSinVideosQueAnadir =>
        Idioma.Elegir("There were no videos to add there.", "Ahí no había vídeos que añadir.");

    public string MainOrigenInvalido => Idioma.Elegir(
        "Point to a valid source file or folder.",
        "Indica un archivo o carpeta de origen válido.");

    // {0} = cuántos vídeos.
    public string MainVideosEncontrados => Idioma.Elegir(
        "{0} video(s) found. Reading their tracks…",
        "{0} vídeo(s) encontrados. Leyendo pistas…");

    // {0} = total, {1} = por comprimir, {2} = ya comprimidos.
    public string MainResumenPreseleccion => Idioma.Elegir(
        "{0} video(s) · {1} to compress · {2} already compressed (left unticked).",
        "{0} vídeo(s) · {1} por comprimir · {2} ya comprimidos (sin marcar).");

    // {0} = total.
    public string MainResumenTodosComprimidos => Idioma.Elegir(
        "{0} video(s): they are all well compressed already.",
        "{0} vídeo(s): todos están ya bien comprimidos.");

    public string MainResumenEnLista => Idioma.Elegir("{0} video(s) in the list.", "{0} vídeo(s) en la lista.");

    public string MainRutaCopiada => Idioma.Elegir("Path copied.", "Ruta copiada.");
    public string MainRutasCopiadas => Idioma.Elegir("{0} paths copied.", "{0} rutas copiadas.");

    // {0} = el mensaje de la excepción.
    public string MainNoSePudoCopiar => Idioma.Elegir("Could not copy: {0}", "No se pudo copiar: {0}");
    public string MainNoSePudoAbrirCarpeta =>
        Idioma.Elegir("Could not open the folder: {0}", "No se pudo abrir la carpeta: {0}");

    // {0} = cuántos vídeos.
    public string MainProcesando => Idioma.Elegir("Processing {0} video(s)…", "Procesando {0} vídeo(s)…");

    // El «✓» es parte del mensaje de terminado en los dos idiomas.
    // {0} = GB de entrada, {1} = GB de salida, {2} = porcentaje, {3} = archivos.
    public string MainTerminadoResumen => Idioma.Elegir(
        "Finished ✓  {0:n2} GB → {1:n2} GB  (-{2}%)  ·  {3} file(s)",
        "Terminado ✓  {0:n2} GB → {1:n2} GB  (-{2}%)  ·  {3} archivo(s)");

    public string MainTerminado => Idioma.Elegir("Finished ✓", "Terminado ✓");
    public string MainCancelado => Idioma.Elegir("Cancelled.", "Cancelado.");

    // El original separaba con raya larga; aquí van dos puntos, que hacen el mismo
    // trabajo sin meter un carácter que esta app no usa.
    public string MainEnPausa => Idioma.Elegir("Paused: FFmpeg suspended", "En pausa: FFmpeg suspendido");

    public string MainComprimiendoBarra =>
        Idioma.Elegir("Compressing… (watch the bar)", "Comprimiendo… (mira la barra)");

    public string MainDiscoLleno => Idioma.Elegir(
        "⛔ Disk full: paused. Free up space and it will carry on by itself.",
        "⛔ Disco lleno: pausado. Libera espacio y continuará solo.");

    // ═══ Presets guardados por el usuario ═══════════════════════════════════
    public string MainNombrePresetPrompt => Idioma.Elegir("Preset name:", "Nombre del preset:");
    public string MainGuardarPresetTitulo => Idioma.Elegir("Save preset", "Guardar preset");

    // {0} = el nombre que ha escrito.
    public string MainPresetGuardado => Idioma.Elegir("Preset “{0}” saved.", "Preset «{0}» guardado.");

    // ═══ Aviso previo a comprimir ═══════════════════════════════════════════
    // {0} = cuántos archivos.
    public string MainAvisoComprimirCuantos => Idioma.Elegir(
        "You are about to compress {0} file(s).",
        "Vas a comprimir {0} archivo(s).");

    public string MainAvisoComprimirPapelera => Idioma.Elegir(
        "Send each original to the Recycle Bin once its compressed version is ready",
        "Enviar cada original a la Papelera cuando su comprimido esté listo");

    public string MainAvisoComprimirNoPreguntar => Idioma.Elegir(
        "Do not ask again (it changes in Preferences)",
        "No volver a preguntar (se cambia en Preferencias)");

    // El original llevaba una raya larga; aquí va una coma, que dice lo mismo.
    public string MainAvisoComprimirNota => Idioma.Elegir(
        "Originals go to the Recycle Bin (recoverable), file by file as each one finishes, never at the end and never for good.",
        "Los originales van a la Papelera de reciclaje (recuperables), archivo por archivo según van terminando, nunca al final ni de forma definitiva.");

    public string MainComprimirSinSeleccion => Idioma.Elegir(
        "Analyse and select at least one video (drag, or Ctrl/Shift+click).",
        "Analiza y selecciona al menos un vídeo (arrastra o Ctrl/Shift+click).");

    // ═══ Subtítulos que se han quedado fuera ════════════════════════════════
    public string MainAvisoSubsPie =>
        Idioma.Elegir("  ·  ⚠ without some subtitles", "  ·  ⚠ sin algunos subtítulos");

    public string MainSubsTitulo => Idioma.Elegir("Subtitles not included", "Subtítulos no incluidos");

    // {0} = el nombre del vídeo, {1} = el motivo, que lo escribe el motor.
    public string MainSubsUnoAfectado => Idioma.Elegir("“{0}”:\n\n{1}", "«{0}»:\n\n{1}");

    // {0} = cuántos han salido incompletos, {1} = cuántos se han comprimido.
    public string MainSubsVariosAfectados => Idioma.Elegir(
        "{0} of the {1} videos have come out without part of their subtitles:\n\n",
        "{0} de los {1} vídeos han salido sin parte de sus subtítulos:\n\n");

    public string MainSubsAfectadosLista => Idioma.Elegir("\n\nAffected:\n· ", "\n\nAfectados:\n· ");

    // {0} = cuántos más, cuando la lista se corta en ocho.
    public string MainSubsYMas => Idioma.Elegir("\n… and {0} more", "\n… y {0} más");

    // ═══ Al cerrar ══════════════════════════════════════════════════════════
    public string MainSalirEnCurso => Idioma.Elegir(
        "There is a compression under way. Close and cancel it?",
        "Hay una compresión en curso. ¿Cerrar y cancelarla?");

    // ═══ FFmpeg y «Acerca de» ═══════════════════════════════════════════════
    public string MainFaltaFfmpegTitulo => Idioma.Elegir("FFmpeg is missing", "Falta FFmpeg");

    // La orden de winget es literal: no se traduce ni una palabra de ella.
    public string MainFaltaFfmpegTexto => Idioma.Elegir(
        "FFmpeg cannot be found. Install it (for example with:  winget install Gyan.FFmpeg) and open the app again.",
        "No se encuentra FFmpeg. Instálalo (por ejemplo con:  winget install Gyan.FFmpeg) y vuelve a abrir la app.");

    // {0} = la versión. ShrinkStudio es el nombre viejo de la app: no se traduce.
    public string MainAcercaDeTexto => Idioma.Elegir(
        "Ondine v{0}\n\nGets your library ready before Plex or Jellyfin scan it: it compresses, sorts against a catalogue and splits the episodes that carry several stories.\n\nIt used to be called ShrinkStudio. It uses FFmpeg underneath. It never touches the originals unless you ask it to.",
        "Ondine v{0}\n\nPrepara tu biblioteca antes de que Plex o Jellyfin la escaneen: comprime, ordena contra un\ncatálogo y parte los episodios que traen varias historias.\n\nAntes se llamaba ShrinkStudio. Usa FFmpeg por debajo. Nunca toca los originales salvo que\nlo pidas explícitamente.");

    // Solo se ve en las compilaciones locales de desarrollo.
    public string MainVersionDevTip => Idioma.Elegir(
        "Local development build (not the version published on GitHub)",
        "Compilación local de desarrollo (no es la versión publicada en GitHub)");

    // ═══ Diálogos del sistema de ficheros ═══════════════════════════════════
    public string MainElegirCarpetaTitulo => Idioma.Elegir("Choose the folder", "Elige la carpeta");

    public string MainElegirVideosTitulo => Idioma.Elegir(
        "Choose one or more videos (Ctrl/Shift to pick several)",
        "Elige uno o varios vídeos (Ctrl/Shift para seleccionar varios)");

    // Filtro del diálogo de abrir: la barra vertical separa rótulo y patrón, y el
    // orden importa. Se traducen los rótulos, jamás las extensiones.
    public string MainFiltroVideos => Idioma.Elegir(
        "Videos|*.mkv;*.mp4;*.avi;*.m4v;*.mov;*.wmv;*.webm;*.mpg;*.mpeg;*.flv|All files|*.*",

        "Vídeos|*.mkv;*.mp4;*.avi;*.m4v;*.mov;*.wmv;*.webm;*.mpg;*.mpeg;*.flv|Todos|*.*");

    /// <summary>
    /// El nombre del tipo en el selector de ficheros, a secas.
    ///
    /// <para>
    /// Distinto de <c>MainFiltroVideos</c>, que es la cadena con tuberías y comodines que pide
    /// WPF. Al portar se le pasó esa al selector de Avalonia, que espera una etiqueta: en el
    /// desplegable salía la frase entera con sus asteriscos.
    /// </para>
    /// </summary>
    public string MainTipoVideos => Idioma.Elegir("Video files", "Vídeos");

    /// <summary>Para abrir algo con una extensión que no está en la lista.</summary>
    public string MainTipoTodos => Idioma.Elegir("All files", "Todos los archivos");

    // ═══ Actualizaciones ════════════════════════════════════════════════════
    // Rótulo del menú y del botón del pie: el mismo texto en los dos sitios.
    public string MainBuscarActualizaciones => Idioma.Elegir("Check for updates", "Buscar actualizaciones");

    public string MainBuscandoActualizaciones =>
        Idioma.Elegir("Checking for updates…", "Buscando actualizaciones…");

    // {0} = el motivo del fallo, que llega del propio actualizador.
    public string MainNoSePudoComprobar =>
        Idioma.Elegir("Could not check: {0}", "No se pudo comprobar: {0}");

    // {0} = la versión que ya tienes.
    public string MainYaAlDia => Idioma.Elegir(
        "You already have the latest version (v{0}).",
        "Ya tienes la última versión (v{0}).");

    // {0} = la etiqueta de la versión nueva, {1} = la que tienes.
    public string MainVersionNuevaDetalle => Idioma.Elegir(
        "New version available: {0}  (you have v{1}).",
        "Versión nueva disponible: {0}  (tienes v{1}).");

    // {0} = la etiqueta de la versión nueva. El original separaba con raya larga.
    public string MainVersionNuevaProgreso => Idioma.Elegir(
        "Version {0} available: see the notice at the top.",
        "Versión {0} disponible: mira el aviso de arriba.");

    public string MainActualizarTitulo => Idioma.Elegir("Update", "Actualizar");

    // {0} = el nombre del fichero que se descarga.
    public string MainDescargando => Idioma.Elegir("Downloading {0}…", "Descargando {0}…");

    // {0} = el nombre del fichero, {1} = porcentaje ya multiplicado por 100.
    public string MainDescargandoPct =>
        Idioma.Elegir("Downloading {0}…  {1:n0}%", "Descargando {0}…  {1:n0}%");

    public string MainDescargandoActualizacion =>
        Idioma.Elegir("Downloading the update…", "Descargando la actualización…");

    public string MainDescargaLista => Idioma.Elegir(
        "Download ready. Opening the installer; the app will close to update itself.",
        "Descarga lista. Abriendo el instalador; la app se cerrará para actualizarse.");

    public string MainAbriendoInstalador => Idioma.Elegir("Opening the installer…", "Abriendo el instalador…");

    /// <summary>
    /// Qué hacer con lo descargado cuando el paquete NO se instala solo.
    ///
    /// <para>
    /// Solo el instalador de Windows se ejecuta y se encarga él. Un <c>.deb</c> pide permisos
    /// de administrador, un <c>.dmg</c> se monta y se arrastra y un AppImage se marca
    /// ejecutable: ninguno de los tres es «lanzar y salir». Lanzarlos de todas formas es lo
    /// que acabó, en Linux, con el gestor de archivadores diciendo que no podía abrir el
    /// fichero — y con Ondine cerrándose detrás.
    /// </para>
    /// <para>
    /// Así que se descarga, se enseña dónde está y se dice qué hacer. {0} es el fichero.
    /// </para>
    /// </summary>
    public string MainDescargaHechaDeb => Idioma.Elegir(
        "«{0}» has been downloaded and its installer is now open. Press Install; your library and your settings stay as they are.",
        "«{0}» está descargado y se ha abierto su instalador. Pulsa «Instalar»: tu biblioteca y tus ajustes se quedan como están.");

    public string MainDescargaHechaAppImage => Idioma.Elegir(
        "«{0}» has been downloaded, it is already executable and its folder is open. Open it; you can then delete the old one.",
        "«{0}» está descargado, ya tiene permiso de ejecución y se ha abierto su carpeta. Ábrelo: después puedes borrar el anterior.");

    public string MainDescargaHechaDmg => Idioma.Elegir(
        "«{0}» has been downloaded and mounted. Drag Ondine onto Applications, replacing the one that is there.",
        "«{0}» está descargado y montado. Arrastra Ondine a Aplicaciones, encima de la que ya está.");

    /// <summary>El rótulo de la barra cuando lo descargado se queda esperando al usuario.</summary>
    public string MainDescargaEsperandoATi => Idioma.Elegir(
        "Downloaded — it is up to you now",
        "Descargado · te toca a ti");

    public string MainDescargaFallo =>
        Idioma.Elegir("The update could not be downloaded.", "No se pudo descargar la actualización.");

    public string MainDescargaFalloProgreso =>
        Idioma.Elegir("Failed to download the update.", "Fallo al descargar la actualización.");

    // {0} = el mensaje de la excepción.
    public string MainDescargaFalloDetalle => Idioma.Elegir(
        "The update could not be downloaded:\n{0}",
        "No se pudo descargar la actualización:\n{0}");

    // ═══ Analizar: progreso, parada y ficheros que solo están en la nube ═════

    public string MainDetenerAnalisis => Idioma.Elegir("Stop", "Detener");
    public string MainAnalisisDetenido => Idioma.Elegir("Scan stopped.", "Análisis detenido.");

    // {0} = por cuál va, {1} = cuántos hay.
    public string MainAnalizandoNde => Idioma.Elegir(
        "Reading track info: {0} of {1}",
        "Leyendo las pistas: {0} de {1}");

    public string MainEnLaNubeSinSondear => Idioma.Elegir(
        "In the cloud — not opened",
        "En la nube — sin abrir");

    // {0} = cuántos se saltaron.
    public string MainSaltadosEnLaNube => Idioma.Elegir(
        "{0} files are only in the cloud and were not opened: reading them would download them whole. Make them available offline first if you want to compress them.",
        "{0} ficheros están solo en la nube y no se han abierto: leerlos los descargaría enteros. Ponlos disponibles sin conexión si quieres comprimirlos.");
}
