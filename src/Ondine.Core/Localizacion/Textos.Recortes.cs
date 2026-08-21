namespace Ondine.Localizacion;

/// <summary>
/// Los textos de la página «Recortes» (<c>RecortesView</c>): el vídeo de origen,
/// la línea de tiempo, la lista de tramos y la exportación.
///
/// <para>
/// <b>Vocabulario fijado para toda la pantalla</b>, porque la pista y la lista de
/// la derecha hablan de lo mismo y tienen que llamarlo igual: <i>tramo</i> =
/// <c>segment</c>, <i>pista</i> = <c>timeline</c>, <i>junta</i> = <c>join</c>,
/// <i>cabezal</i> = <c>playhead</c>, <i>tirador</i> = <c>handle</c>. Se eligió
/// <c>segment</c> y no <c>clip</c> para no chocar con el nombre de la propia
/// página en inglés.
/// </para>
/// <para>
/// Lo que va entre paréntesis en los rótulos de ayuda son TECLAS FÍSICAS
/// (<c>Key.C</c>, <c>Ctrl+0</c>, <c>Esc</c>): se traduce el verbo, nunca la tecla.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── El nombre de la página ──────────────────────────────────────────────
    // Sale como título de diálogo y citado dentro de otros textos de aquí. Tiene
    // que decir lo MISMO que la pestaña de MainWindow (Tag="Recortes"): si allí
    // se traduce de otra forma, esta clave se cambia con ella.
    public string RecortesTitulo => Idioma.Elegir("Clips", "Recortes");

    // ── De dónde sale el vídeo ──────────────────────────────────────────────
    public string RecortesElegirVideo => Idioma.Elegir("Choose video…", "Elegir vídeo…");

    public string RecortesSinVideo => Idioma.Elegir("No video loaded", "Ningún vídeo cargado");

    // Cita la pestaña «Organizar»: si esa pestaña se traduce distinta de
    // «Organise», esta frase se corrige con ella.
    public string RecortesArrastraVideo => Idioma.Elegir(
        "Drag a video here, or open it from Organise with the right mouse button.",
        "Arrastra un vídeo aquí, o ábrelo desde Organizar con el botón derecho.");

    public string RecortesVaciar => Idioma.Elegir("Clear", "Vaciar");

    // El original separaba la consecuencia con una raya larga; aquí va con dos
    // puntos, que es lo que hace el mismo trabajo sin raya.
    public string RecortesVaciarTip => Idioma.Elegir(
        "Discards the video and the cuts and frees the memory: leaves Clips as if it had just been opened",
        "Descarta el vídeo y los cortes y libera la memoria: deja Recortes como recién abierto");

    // ── El reproductor ──────────────────────────────────────────────────────
    public string RecortesElegirParaEmpezar =>
        Idioma.Elegir("Choose a video to start cutting it", "Elige un vídeo para empezar a cortarlo");

    public string RecortesSinPrevisualizacion => Idioma.Elegir(
        "This video cannot be previewed here, but it can still be cut.",
        "Este vídeo no se puede previsualizar aquí, pero sí cortarlo.");

    // ── La espera (capa de carga) ───────────────────────────────────────────
    public string RecortesPreparandoVideo => Idioma.Elegir("Preparing the video…", "Preparando el vídeo…");

    public string RecortesAnalizandoVideo => Idioma.Elegir("Analysing the video…", "Analizando el vídeo…");

    public string RecortesDescargandoNube =>
        Idioma.Elegir("Downloading from the cloud…", "Descargando de la nube…");

    // {0} = porcentaje ya multiplicado por 100; el «:0» lo redondea.
    public string RecortesDescargandoNubePorcentaje => Idioma.Elegir(
        "Downloading from the cloud… {0:0} %",
        "Descargando de la nube… {0:0} %");

    // {0} = tamaño del fichero, ya formateado.
    public string RecortesEscCancelar => Idioma.Elegir("{0} · Esc to cancel", "{0} · Esc para cancelar");

    // ── Mientras se exporta (velo sobre el reproductor) ─────────────────────
    public string RecortesReproductorEnPausa => Idioma.Elegir(
        "The player is paused while exporting",
        "El reproductor está en pausa mientras se exporta");

    // «Vuelve solo» = vuelve por sí mismo, no «vuelve únicamente».
    public string RecortesReproductorEnPausaDetalle => Idioma.Elegir(
        "The video has to be free for it to be cut. It comes back on its own when it finishes.",
        "El vídeo tiene que estar libre para poder cortarlo. Vuelve solo al terminar.");

    // ── La pista ────────────────────────────────────────────────────────────
    // El doble espacio antes del paréntesis es deliberado: separa la acción de
    // su atajo sin meter un signo de puntuación.
    public string RecortesCortarAquiTip => Idioma.Elegir(
        "Cut here: splits in two the segment the playhead is on  (C)",
        "Cortar aquí: parte en dos el tramo por donde va el cabezal  (C)");

    // El salto de línea es el que llevaba el XAML (&#10;). «Ctrl + rueda»,
    // «Ctrl +» y «Ctrl −» son teclas: solo se traduce «rueda».
    // En una sola línea a propósito, sin concatenar: el arnés de traducción lee
    // los pares con una expresión regular y un «+» en medio se le escapa.
    public string RecortesPistaTip => Idioma.Elegir(
        "Click to move through the video. Drag the handles on the joins to fine-tune where it cuts.\nCtrl + wheel (or Ctrl + and Ctrl −) to zoom the timeline.",
        "Pincha para moverte por el vídeo. Arrastra los tiradores de las juntas para afinar dónde corta.\nCtrl + rueda (o Ctrl + y Ctrl −) para ampliar la línea de tiempo.");

    public string RecortesZoomTip => Idioma.Elegir(
        "Timeline zoom · Ctrl+0 to see it whole",
        "Aumento de la línea de tiempo · Ctrl+0 para verla entera");

    public string RecortesTiradorTip => Idioma.Elegir(
        "Drag to move the cut. It cannot go past the neighbouring segments.",
        "Arrastra para mover el corte. No puede pasarse de los tramos de al lado.");

    // OJO: el botón salta 10 s pero la flecha del teclado salta 5 s. El texto
    // se traduce tal cual estaba; si se corrige la cifra, se corrige en los dos
    // idiomas a la vez.
    public string RecortesAtrasTip => Idioma.Elegir("Back 10 s  (left arrow)", "Atrás 10 s  (flecha izquierda)");

    public string RecortesPlayTip => Idioma.Elegir("Play / pause  (space)", "Reproducir / pausar  (espacio)");

    public string RecortesAdelanteTip =>
        Idioma.Elegir("Forward 10 s  (right arrow)", "Adelante 10 s  (flecha derecha)");

    // ── La lista de tramos ──────────────────────────────────────────────────
    // Va en mayúsculas por estilo tipográfico, no por ser un acrónimo.
    public string RecortesTramosCabecera => Idioma.Elegir("SEGMENTS", "TRAMOS");

    public string RecortesAyudaTramos => Idioma.Elegir(
        "Each segment will be a file. Cut wherever you like and give them names.",
        "Cada tramo será un fichero. Corta por donde quieras y ponles nombre.");

    public string RecortesAyudaSinTramos => Idioma.Elegir(
        "No segments left: nothing would be exported.",
        "No queda ningún tramo: así no se exportaría nada.");

    // La ✂ es el pictograma del botón de cortar: no se traduce ni se cambia.
    public string RecortesAyudaUnTramo => Idioma.Elegir(
        "A single segment: the whole video will be exported. Cut with the ✂ on the timeline to split it.",
        "Un solo tramo: se exportará el vídeo entero. Corta con la ✂ de la pista para partirlo.");

    // {0} sale dos veces a propósito, igual que en el original.
    public string RecortesAyudaVariosTramos => Idioma.Elegir(
        "{0} segments = {0} files. Drag the joins on the timeline to fine-tune.",
        "{0} tramos = {0} ficheros. Arrastra las juntas de la pista para afinar.");

    // Insignia dentro de la tarjeta, que mide 300 px fijos: tiene que ser corta.
    public string RecortesInsigniaExportando => Idioma.Elegir("EXPORTING", "EXPORTANDO");

    // Una sola clave para el ✕ de la tarjeta y el ✕ del bloque de la pista:
    // antes eran dos frases casi iguales y se traducían dos veces.
    public string RecortesQuitarTramoTip => Idioma.Elegir(
        "Remove this segment (it will not be exported)",
        "Quitar este tramo (no se exportará)");

    public string RecortesNombreFicheroTip => Idioma.Elegir(
        "Name of the file that will be created (without extension)",
        "Nombre del fichero que se creará (sin extensión)");

    // ── Ajustes de salida ───────────────────────────────────────────────────
    public string RecortesFormato => Idioma.Elegir("Format", "Formato");
    public string RecortesCodec => Idioma.Elegir("Codec", "Códec");
    public string RecortesCalidad => Idioma.Elegir("Quality", "Calidad");

    // Abreviado en los dos idiomas: es la etiqueta de un desplegable estrecho
    // (una quinta parte del ancho de la fila) y entera no cabe.
    public string RecortesResolucionMax => Idioma.Elegir("Max. resolution", "Resolución máx.");

    public string RecortesAudio => Idioma.Elegir("Audio", "Audio");

    // ── La estimación ───────────────────────────────────────────────────────
    public string RecortesEstimacionSinVideo => Idioma.Elegir(
        "Choose a video to estimate the size",
        "Elige un vídeo para estimar el tamaño");

    public string RecortesEstimacionImposible =>
        Idioma.Elegir("This video cannot be estimated", "No se puede estimar este vídeo");

    // {0} = cuántos ficheros, {1} = duración total ya formateada. Dos claves
    // porque el original ya distinguía singular y plural.
    public string RecortesEstimacionDetalleUno => Idioma.Elegir(
        "{0} file · {1} of video · rough estimate, the real result depends on the content.",
        "{0} fichero · {1} de vídeo · estimación aproximada, el resultado real depende del contenido.");

    public string RecortesEstimacionDetalle => Idioma.Elegir(
        "{0} files · {1} of video · rough estimate, the real result depends on the content.",
        "{0} ficheros · {1} de vídeo · estimación aproximada, el resultado real depende del contenido.");

    // ── Dónde se guarda ─────────────────────────────────────────────────────
    public string RecortesGuardarEn => Idioma.Elegir("Save to…", "Guardar en…");

    public string RecortesGuardarEnTip =>
        Idioma.Elegir("By default, next to the original video", "Por defecto, junto al vídeo original");

    public string RecortesDondeDejarTitulo =>
        Idioma.Elegir("Where to put the clips", "Dónde dejar los recortes");

    // {0} = la carpeta.
    public string RecortesSeGuardaraEn => Idioma.Elegir("Will be saved to: {0}", "Se guardará en: {0}");

    // Se pega detrás de la anterior; los dos espacios de delante son la
    // separación, no un descuido.
    public string RecortesJuntoAlOriginal =>
        Idioma.Elegir("  (next to the original)", "  (junto al original)");

    // ── Botones de la exportación ───────────────────────────────────────────
    public string RecortesPausar => Idioma.Elegir("Pause", "Pausar");

    public string RecortesPausarTip => Idioma.Elegir(
        "Suspends ffmpeg where it is. Resuming carries on from there, not from the beginning.",
        "Suspende ffmpeg donde va. Reanudar sigue desde ahí, no desde el principio.");

    public string RecortesReanudar => Idioma.Elegir("Resume", "Reanudar");

    public string RecortesDetener => Idioma.Elegir("Stop", "Detener");

    public string RecortesDetenerTip => Idioma.Elegir(
        "Cuts the export short and kills the ffmpeg process, it does not leave it running in the background.",
        "Corta la exportación y mata el proceso de ffmpeg, no lo deja de fondo.");

    public string RecortesDeteniendo => Idioma.Elegir("Stopping…", "Deteniendo…");

    public string RecortesExportar => Idioma.Elegir("Export", "Exportar");

    public string RecortesExportarUnTramo => Idioma.Elegir("Export 1 segment", "Exportar 1 tramo");

    // {0} = cuántos tramos. Solo se usa con dos o más.
    public string RecortesExportarVariosTramos =>
        Idioma.Elegir("Export {0} segments", "Exportar {0} tramos");

    // ── Cómo va la exportación ──────────────────────────────────────────────
    // Estas dos las lee el indicador global de la ventana, no solo esta página.
    public string RecortesExportandoEstado => Idioma.Elegir("Exporting…", "Exportando…");

    public string RecortesExportandoPorcentaje =>
        Idioma.Elegir("Exporting · {0:0} %", "Exportando · {0:0} %");

    // {0} = número de tramo, {1} = cuántos hay, {2} = nombre del tramo.
    public string RecortesTramoEnCurso => Idioma.Elegir("Segment {0} of {1} · {2}", "Tramo {0} de {1} · {2}");

    // En mayúsculas dentro de la línea de progreso: es el motor el que se salta
    // un tramo y el porqué llega de él, sin traducir.
    public string RecortesSaltado => Idioma.Elegir("SKIPPED", "SALTADO");

    public string RecortesListoUnFichero => Idioma.Elegir("Done · {0} file", "Listo · {0} fichero");

    public string RecortesListoFicheros => Idioma.Elegir("Done · {0} files", "Listo · {0} ficheros");

    // {0} = los que salieron, {1} = los que había, {2} = los que no salieron.
    public string RecortesSinSalir =>
        Idioma.Elegir("{0} of {1} · {2} did not come out", "{0} de {1} · {2} sin salir");

    public string RecortesCancelado => Idioma.Elegir("Cancelled", "Cancelado");

    // ── Diálogos: vaciar ────────────────────────────────────────────────────
    public string RecortesVaciarTitulo => Idioma.Elegir("Clear Clips", "Vaciar Recortes");

    // {0} = cuántos tramos. Solo aparece con más de uno, así que el plural está
    // garantizado en los dos idiomas.
    public string RecortesVaciarPregunta => Idioma.Elegir(
        "You have {0} segments ready. Clearing discards them and releases the video. Carry on?",
        "Tienes {0} tramos preparados. Vaciar los descarta y suelta el vídeo. ¿Seguir?");

    // ── Diálogos: cargar otro vídeo ─────────────────────────────────────────
    public string RecortesElegirVideoTitulo =>
        Idioma.Elegir("Choose the video you want to cut", "Elegir el vídeo que quieres recortar");

    // Las máscaras y las barras «|» son sintaxis del diálogo de Windows: solo se
    // traducen los dos rótulos.
    public string RecortesFiltroVideos => Idioma.Elegir(
        "Videos|*.mkv;*.mp4;*.avi;*.mov;*.m4v;*.webm;*.ts|All|*.*",
        "Vídeos|*.mkv;*.mp4;*.avi;*.mov;*.m4v;*.webm;*.ts|Todos|*.*");

    public string RecortesExportandoAviso => Idioma.Elegir(
        "An export is running right now. Stop it before loading another video.",
        "Se está exportando ahora mismo. Detén la exportación antes de cargar otro vídeo.");

    // {0} = cuántos tramos, {1} = nombre del fichero, {2} = salto de línea (lo
    // pone quien llama, para que sea el del sistema).
    public string RecortesCargarOtroPregunta => Idioma.Elegir(
        "You have {0} segments ready on “{1}”.{2}{2}Loading another video discards them. Carry on?",
        "Tienes {0} tramos preparados sobre «{1}».{2}{2}Cargar otro vídeo los descarta. ¿Seguir?");

    // ── Diálogos: borrar el original ────────────────────────────────────────
    public string RecortesBorrarOriginalTitulo => Idioma.Elegir("Delete the original?", "¿Borrar el original?");

    // {0} = cuántos tramos, {1} = carpeta de destino, {2} = nombre del fichero,
    // {3} = su tamaño, {4} = salto de línea.
    public string RecortesBorrarOriginalPregunta => Idioma.Elegir(
        "The {0} segments are in:{4}{1}{4}{4}“{2}” ({3}) is no longer needed.{4}{4}It goes to the recycle bin, so it can be recovered.",
        "Los {0} tramos están en:{4}{1}{4}{4}«{2}» ({3}) ya no hace falta.{4}{4}Va a la papelera de reciclaje, así que se puede recuperar.");

    // Botones del diálogo anterior: dicen lo que hacen, no «sí»/«no» a secas.
    public string RecortesSiPapelera => Idioma.Elegir("Yes, to the recycle bin", "Sí, a la papelera");

    public string RecortesNoConservar => Idioma.Elegir("No, keep it", "No, conservarlo");

    // {0} = nombre del fichero.
    public string RecortesNoSePudoBorrar => Idioma.Elegir(
        "“{0}” could not be deleted. It is still where it was.",
        "No se pudo borrar «{0}». Sigue en su sitio.");

    // {0} = cuántos tramos.
    public string RecortesListoOriginalPapelera => Idioma.Elegir(
        "Done · {0} segments · original sent to the recycle bin (Ctrl+Z to recover)",
        "Listo · {0} tramos · original a la papelera (Ctrl+Z para recuperar)");

    // ── Papelera propia (Ctrl+Z) ────────────────────────────────────────────
    // {0} = nombre del fichero recuperado.
    public string RecortesRecuperado => Idioma.Elegir(
        "Recovered “{0}” · open it again to edit it",
        "Recuperado «{0}» · vuelve a abrirlo para editarlo");

    public string RecortesNadaQueRecuperar =>
        Idioma.Elegir("There is nothing to recover.", "No hay nada que recuperar.");

    // ── El panel «Registro» ─────────────────────────────────────────────────
    // Estas líneas salen POR PANTALLA, en el registro compartido de la ventana:
    // el usuario las lee cuando algo no ha salido como esperaba, así que se
    // traducen como cualquier otro texto. Todas empiezan por el nombre de la
    // página (ver RecortesTitulo), que es lo que las distingue de las que
    // escriben las demás pestañas.
    //
    // Lo que NO se traduce dentro de ellas: nombres de fichero, rutas y el
    // mensaje de error que llega del sistema o del motor.

    public string RecortesLogDescargaCancelada => Idioma.Elegir(
        "Clips: download cancelled; the video is still in the cloud.",
        "Recortes: descarga cancelada; el vídeo sigue en la nube.");

    // {0} = nombre del fichero, {1} = el fallo tal como llega del sistema.
    public string RecortesLogNoSePudoDescargar => Idioma.Elegir(
        "Clips: “{0}” could not be downloaded: {1}",
        "Recortes: no se pudo descargar «{0}»: {1}");

    // {0} = nombre del fichero, {1} = su tamaño, ya formateado.
    public string RecortesLogDescargado => Idioma.Elegir(
        "Clips: “{0}” downloaded ({1}).",
        "Recortes: «{0}» descargado ({1}).");

    // {0} = el nivel que dice WPF. La explicación de los números va detrás
    // porque el número solo no le dice nada a nadie.
    public string RecortesLogAceleracion => Idioma.Elegir(
        "Clips: graphics acceleration level {0} (0 = software/no GPU, 1 = partial, 2 = full GPU).",
        "Recortes: aceleración gráfica nivel {0} (0 = por software/sin GPU, 1 = parcial, 2 = GPU completa).");

    // {0} = milisegundos que estuvo parada, {1} = qué se estaba haciendo (las
    // dos claves de aquí abajo).
    public string RecortesLogInterfazSinResponder => Idioma.Elegir(
        "Clips: the interface stopped responding for {0} ms ({1}).",
        "Recortes: la interfaz se quedó {0} ms sin responder ({1}).");

    public string RecortesLogImportando => Idioma.Elegir("importing a video", "importando un vídeo");

    public string RecortesLogEnReposo => Idioma.Elegir("idle", "en reposo");

    // {0} = fotograma mediano en ms, {1} = el p99 en ms, {2} = porcentaje de
    // fotogramas por debajo de 30 fps. El original separaba el resumen con una
    // raya larga; aquí van dos puntos, que hacen el mismo trabajo sin raya.
    public string RecortesLogFluidez => Idioma.Elegir(
        "Clips: smoothness during the export: median frame {0:0} ms, p99 {1:0} ms, {2:0}% below 30 fps. If it felt jerky to you and this number is good, the brake comes from outside the app.",
        "Recortes: fluidez durante la exportación: fotograma mediano {0:0} ms, p99 {1:0} ms, {2:0}% por debajo de 30 fps. Si lo notaste a tirones y este número es bueno, el freno viene de fuera de la app.");

    // {0} = milisegundos que la entrada estuvo bloqueada.
    public string RecortesLogEntradaBloqueada => Idioma.Elegir(
        "Clips: input was blocked for {0} ms during the export.",
        "Recortes: la entrada estuvo bloqueada {0} ms durante la exportación.");

    public string RecortesLogDeshecho => Idioma.Elegir("Clips: undone.", "Recortes: deshecho.");

    public string RecortesLogRehecho => Idioma.Elegir("Clips: redone.", "Recortes: rehecho.");

    // {0} = nombre del fichero recuperado.
    public string RecortesLogRecuperado => Idioma.Elegir(
        "Clips: “{0}” recovered from the recycle bin.",
        "Recortes: «{0}» recuperado de la papelera.");

    // {0} = nombre del fichero.
    public string RecortesLogNoSePudoEnviarPapelera => Idioma.Elegir(
        "Clips: “{0}” could not be sent to the recycle bin; it is still where it was.",
        "Recortes: no se pudo enviar «{0}» a la papelera; sigue donde estaba.");

    // {0} = nombre del fichero, {1} = cuántos tramos se exportaron.
    public string RecortesLogAPapelera => Idioma.Elegir(
        "Clips: “{0}” to the recycle bin after exporting {1} segments.",
        "Recortes: «{0}» a la papelera tras exportar {1} tramos.");

    // {0} = cuántos ficheros, {1} = la carpeta (no se traduce).
    public string RecortesLogFicherosCreados => Idioma.Elegir(
        "Clips: {0} files created in {1}",
        "Recortes: {0} ficheros creados en {1}");

    // {0} = los que no salieron, {1} = cuántos tramos había, {2} = sus nombres.
    // El «NO» va en mayúsculas a propósito: es la línea que hay que ver de un
    // vistazo entre las del motor.
    public string RecortesLogNoSalieron => Idioma.Elegir(
        "Clips: {0} of {1} segments did NOT come out ({2}). The engine says why in the lines above.",
        "Recortes: NO salieron {0} de {1} tramos ({2}). El motor dice el porqué en las líneas de arriba.");

    // {0} = el motivo, tal como lo da el motor.
    public string RecortesLogMotorSalto => Idioma.Elegir(
        "Clips: the engine skipped this segment: {0}",
        "Recortes: el motor se saltó este tramo: {0}");

    // {0} = el mensaje de la excepción, sin traducir: viene del sistema.
    public string RecortesLogFallo => Idioma.Elegir("Clips: {0}", "Recortes: {0}");

    // {0} = el códec. Lo importante va delante: cortar SÍ funciona. Lo que no hay es
    // reproducción, y eso aquí importa menos que en el reproductor.
    public string RecortesModoFotogramas => Idioma.Elegir(
        "This video is {0}: Windows cannot play it here, so it is shown frame by frame. Cutting works exactly the same.",
        "Este vídeo es {0}: Windows no sabe reproducirlo aquí, así que se enseña por fotogramas. Cortar funciona igual.");
}
