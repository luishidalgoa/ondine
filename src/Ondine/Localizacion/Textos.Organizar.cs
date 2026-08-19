namespace Ondine.Localizacion;

/// <summary>
/// Textos de la pantalla «Organizar» (<c>OrganizarView</c> y sus filas,
/// <c>OrganizarRow</c>): catálogos de referencia, plantilla de biblioteca, tabla
/// de triaje, resolutor de conflictos y cola de revisión.
///
/// <para>
/// El glosario en inglés lo fija la ayuda (<c>Textos.Ayuda.cs</c>), que ya
/// describe esta pantalla: «Organise», «Trim», «Analyse», «Correct», «With
/// changes», «Apply»… Si aquí se tradujera un rótulo de otra forma, la ayuda
/// estaría explicando botones que no existen.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ═══ Barra de contexto: serie y modo ════════════════════════════════════

    public string OrganizarSerie => Idioma.Elegir(
        "Series (reference catalogue)",
        "Serie (catálogo de referencia)");

    public string OrganizarSerieAyuda => Idioma.Elegir(
        "The catalogue the files in the folder are identified against",
        "El catálogo contra el que se identifican los ficheros de la carpeta");

    public string OrganizarCatalogos => Idioma.Elegir("Catalogues…", "Catálogos…");

    public string OrganizarCatalogosAyuda => Idioma.Elegir(
        "Import, review or remove reference catalogues",
        "Importar, revisar o quitar catálogos de referencia");

    public string OrganizarExplorarAyuda => Idioma.Elegir(
        "Browse the chosen catalogue: search by number or by title to check a proposal",
        "Explorar el catálogo elegido: busca por número o por título para comprobar una propuesta");

    // Cita literalmente el rótulo de la segunda opción del desplegable: si se
    // cambia una, hay que cambiar la otra.
    public string OrganizarModoAyuda => Idioma.Elegir(
        "What this catalogue trusts when identifying. \"The number wins\": if the file carries an episode number it is used even when the title is weak, without falling back to \"review\".",
        "En qué se fía este catálogo al identificar. «El número manda»: si el fichero trae número de episodio, se usa aunque el título flojee, sin caer en «revisar».");

    public string OrganizarModoAuto => Idioma.Elegir("Priority: automatic", "Prioridad: automática");
    public string OrganizarModoNumero => Idioma.Elegir("Priority: the number wins", "Prioridad: el número manda");

    // ═══ Plantilla de biblioteca ════════════════════════════════════════════

    public string OrganizarPlantilla => Idioma.Elegir("Library template", "Plantilla de biblioteca");

    // Cita el renombrado libre de la pestaña de herramientas, que es otra cosa:
    // ahí se busca y reemplaza sobre el nombre que ya hay, aquí se construye uno
    // nuevo desde el catálogo.
    public string OrganizarPlantillaAyuda => Idioma.Elegir(
        "How the final name is put together. This is not the \"free renaming\" of Tools: here the name is built from the catalogue.",
        "Cómo se compone el nombre final. No es el «Renombrado libre» de Herramientas: aquí el nombre se construye desde el catálogo.");

    // «Marca» es cada uno de los {campos} que se pueden insertar. En inglés,
    // «token», que es como se llaman en cualquier plantilla de nombres.
    public string OrganizarMarcas => Idioma.Elegir("Tokens", "Marcas");

    public string OrganizarMarcasAyuda => Idioma.Elegir(
        "Insert a token into the template",
        "Insertar una marca en la plantilla");

    public string OrganizarMarcasCursor => Idioma.Elegir(
        "It goes in wherever your cursor is",
        "Se inserta donde tengas el cursor");

    public string OrganizarVistaPreviaAyuda => Idioma.Elegir(
        "An example with a real episode from the catalogue",
        "Ejemplo con un episodio real del catálogo");

    public string OrganizarVistaPreviaSinCatalogo => Idioma.Elegir(
        "Choose a catalogue to see an example",
        "Elige un catálogo para ver un ejemplo");

    public string OrganizarVistaPreviaSinNombre => Idioma.Elegir(
        "⚠ That template leaves no name: add a token or some text",
        "⚠ Esa plantilla no deja nombre: añade alguna marca o texto");

    // El espacio final es parte del texto: detrás va el nombre de ejemplo.
    public string OrganizarVistaPreviaQuedaria => Idioma.Elegir("Would be: ", "Quedaría: ");

    // {0} = la serie del catálogo, {1} = el nombre de ejemplo entero.
    public string OrganizarPlantillaGlobo => Idioma.Elegir(
        "With \"{0}\" it would be:\n{1}",
        "Con «{0}» quedaría:\n{1}");

    // ═══ Resumen y filtros de la revisión ═══════════════════════════════════

    public string OrganizarVolver => Idioma.Elegir("← Back", "← Volver");

    public string OrganizarVolverAyuda => Idioma.Elegir(
        "Discard this analysis and go back to the start screen",
        "Descartar este análisis y volver a la pantalla de inicio");

    // Los cinco chips: el espacio inicial separa el texto del glifo de color, que
    // va en un Run aparte. Sin él, el punto y el número se pegan.
    public string OrganizarChipCorrectos => Idioma.Elegir(" {0} correct", " {0} correctos");
    public string OrganizarChipConCambios => Idioma.Elegir(" {0} with changes", " {0} con cambios");
    public string OrganizarChipEspeciales => Idioma.Elegir(" {0} specials", " {0} especiales");
    public string OrganizarChipConflictos => Idioma.Elegir(" {0} conflicts", " {0} conflictos");
    public string OrganizarChipErrores => Idioma.Elegir(" {0} errors", " {0} errores");

    public string OrganizarSoloDudas => Idioma.Elegir("Doubts only", "Solo dudas");

    public string OrganizarSoloDudasAyuda => Idioma.Elegir(
        "Leaves only what needs you to decide",
        "Deja solo lo que necesita que decidas tú");

    public string OrganizarBuscarAyuda => Idioma.Elegir(
        "Search the table (Ctrl+K): by original name or by proposal. Esc clears it.",
        "Buscar en la tabla (Ctrl+K): por nombre original o por propuesta. Esc lo limpia.");

    // «Verdes» es el color de la fila, no una metáfora: se conserva en inglés.
    public string OrganizarAceptarVerdes => Idioma.Elegir("Accept the greens", "Aceptar verdes");

    public string OrganizarAceptarVerdesAyuda => Idioma.Elegir(
        "Takes every confidently identified row as good",
        "Da por buenas todas las filas identificadas con confianza");

    public string OrganizarConfirmarEspeciales => Idioma.Elegir("Confirm specials…", "Confirmar especiales…");

    public string OrganizarConfirmarEspecialesAyuda => Idioma.Elegir(
        "Go through the specials one by one; they are never applied on their own",
        "Revisa uno a uno los especiales, que nunca se aplican solos");

    // ═══ Panel de catálogos importados ══════════════════════════════════════

    public string OrganizarCatalogosImportados => Idioma.Elegir("IMPORTED CATALOGUES", "CATÁLOGOS IMPORTADOS");

    public string OrganizarQueFormato => Idioma.Elegir("Which format?", "¿Qué formato?");

    public string OrganizarQueFormatoAyuda => Idioma.Elegir(
        "Opens the catalogue specification: fields, rules and a full example",
        "Abre la especificación del catálogo: campos, reglas y un ejemplo completo");

    public string OrganizarCrearEjemplo => Idioma.Elegir("Create an example…", "Crear ejemplo…");

    public string OrganizarCrearEjemploAyuda => Idioma.Elegir(
        "Saves a valid example catalogue for you to edit with your own episodes",
        "Guarda un catálogo de ejemplo válido para que lo edites con tus episodios");

    public string OrganizarGenerarIA => Idioma.Elegir("Generate with an AI…", "Generar con IA…");

    // «Anexo» es como llama la Wikipedia en español a los artículos de lista, y
    // en inglés no existe: allí es una página normal. Mismo criterio que en
    // Textos.Dialogos.cs, que traduce «anexo» por «source page».
    public string OrganizarGenerarIAAyuda => Idioma.Elegir(
        "Builds the request for an AI to turn an episode list page (Wikipedia, Fandom…) into a catalogue",
        "Arma el encargo para que una IA convierta un anexo de episodios (Wikipedia, Fandom…) en un catálogo");

    public string OrganizarImportarAyuda => Idioma.Elegir(
        "Add a reference catalogue in JSON format (reindex/1.0 schema)",
        "Añadir un catálogo de referencia en formato JSON (esquema reindex/1.0)");

    public string OrganizarImportarJson => Idioma.Elegir("Import JSON", "Importar JSON");

    public string OrganizarAbrirUbicacion => Idioma.Elegir(
        "Open the file's location",
        "Abrir la ubicación del fichero");

    public string OrganizarCopiarRuta => Idioma.Elegir("Copy the path", "Copiar la ruta");

    public string OrganizarSeleccionado => Idioma.Elegir("selected", "seleccionado");

    public string OrganizarUsar => Idioma.Elegir("Use", "Usar");

    public string OrganizarQuitarCatalogoAyuda => Idioma.Elegir(
        "Take this catalogue out of the app (it does not delete your JSON file)",
        "Quitar este catálogo de la app (no borra tu fichero JSON)");

    // ── Panel de «no hay catálogos» ─────────────────────────────────────────
    // Tres párrafos partidos en trozos porque llevan palabras destacadas dentro
    // (un Run con otro color). El corte va donde la frase lo admite en los dos
    // idiomas; los espacios de los extremos son parte del texto y sostienen la
    // separación entre trozos.

    public string OrganizarSinCatalogos1a => Idioma.Elegir("A ", "Un ");
    public string OrganizarSinCatalogosPalabra => Idioma.Elegir("catalogue", "catálogo");

    public string OrganizarSinCatalogos1b => Idioma.Elegir(
        " is the episode list of a series: number, air date and titles. Ondine compares your files against that list to know which episode each one is.",
        " es la lista de episodios de una serie: número, fecha de emisión y títulos. Ondine compara tus ficheros con esa lista para saber qué episodio es cada uno.");

    // Entre estos dos va «reindex/1.0», que es el identificador del esquema y no
    // se traduce. En inglés el sustantivo va detrás del identificador y en
    // castellano delante, por eso los dos trozos no son simétricos.
    public string OrganizarSinCatalogos2a => Idioma.Elegir(
        "It is a JSON file using the ",
        "Es un archivo JSON con el esquema ");

    public string OrganizarSinCatalogos2b => Idioma.Elegir(
        " schema. Without a catalogue there is nothing to compare against, so analysing stays disabled.",
        ". Sin catálogo no hay nada con lo que comparar, así que el análisis queda deshabilitado.");

    public string OrganizarSinCatalogos3a => Idioma.Elegir(
        "The minimum is the series and a list of episodes with their number and their title; the air date is not required, but it is the most reliable clue there is. Press ",
        "Lo mínimo es la serie y una lista de episodios con su número y su título; la fecha de emisión no hace falta, pero es la pista más fiable que hay. Pulsa ");

    public string OrganizarSinCatalogos3b => Idioma.Elegir(
        " to start from a valid one, or ",
        " para partir de uno válido, o ");

    public string OrganizarSinCatalogos3c => Idioma.Elegir(
        " to see every field and every rule.",
        " para ver todos los campos y las reglas.");

    // ═══ Panel de ficheros ══════════════════════════════════════════════════

    public string OrganizarFicheros => Idioma.Elegir("FILES", "FICHEROS");

    public string OrganizarCarpeta => Idioma.Elegir("Folder to organise", "Carpeta a organizar");

    public string OrganizarCarpetaAyuda => Idioma.Elegir(
        "Choose the folder whose files you want to organise",
        "Elegir la carpeta cuyos ficheros quieres organizar");

    // ── La ventana de películas ───────────────────────────────────────────────
    public string PeliculasTitulo => Idioma.Elegir("Sort out films", "Ordenar las películas");

    // {0} = cuántas tienen trabajo.
    public string PeliculasResumen => Idioma.Elegir(
        "{0} films would be renamed or moved.",
        "{0} películas se renombrarían o se moverían.");

    public string PeliculasResumenNinguno => Idioma.Elegir(
        "Nothing to do: every film already follows the convention.",
        "No hay nada que hacer: todas cumplen ya la convención.");

    // {0} = cuántas se quedan como están.
    public string PeliculasResumenQuietos => Idioma.Elegir(
        " {0} stay as they are.",
        " {0} se quedan como están.");

    public string PeliculasPorqueVa => Idioma.Elegir("moves", "se coloca");
    public string PeliculasPorqueEnColeccion => Idioma.Elegir("renamed in place", "se renombra donde está");
    public string PeliculasPorqueYaEsta => Idioma.Elegir("already right", "ya está bien");
    public string PeliculasPorqueSinTitulo => Idioma.Elegir("no title in the name", "sin título en el nombre");
    public string PeliculasPorqueEsExtra => Idioma.Elegir("an extra, left alone", "un extra, no se toca");
    public string PeliculasPorqueOcupado => Idioma.Elegir("name already taken there", "ese nombre ya está ocupado");

    public string PeliculasVerSoloLosQueVan => Idioma.Elegir(
        "Hide the ones already right",
        "Esconder las que ya están bien");

    // {0} = cuántas se van a tocar.
    public string PeliculasBoton => Idioma.Elegir("Apply to {0}", "Aplicar a {0}");
    public string PeliculasBotonNada => Idioma.Elegir("Apply", "Aplicar");

    // Es una simulación hasta que se pulsa, y hay que decirlo donde se lee. Y la
    // segunda frase es la que evita la decepción: sin base de datos, un título
    // mal escrito en el fichero sigue mal escrito después.
    public string PeliculasPie => Idioma.Elegir(
        "Nothing has been touched yet. Names come from the file and its folder — there is no database behind this yet, so a misspelt title stays misspelt.",
        "Todavía no se ha tocado nada. Los nombres salen del fichero y de su carpeta —aún no hay ninguna base de datos detrás—, así que un título mal escrito seguirá mal escrito.");

    public string PeliculasDeshacer => Idioma.Elegir("Undo", "Deshacer");

    // {0} = cuántas se movieron · {1} = cuántas fallaron.
    public string PeliculasHecho => Idioma.Elegir("{0} done.", "{0} hechas.");
    public string PeliculasHechoConFallos => Idioma.Elegir(
        "{0} done, {1} could not be touched.",
        "{0} hechas, {1} no se pudieron tocar.");
    public string PeliculasDeshecho => Idioma.Elegir("{0} put back.", "{0} devueltas a su sitio.");

    public string PeliculasAbrir => Idioma.Elegir("Sort out films…", "Ordenar las películas…");
    public string PeliculasAbrirAyuda => Idioma.Elegir(
        "Show what would change so each film ends up as «Title (Year)/Title (Year).ext», which is what Plex and Jellyfin expect. A folder holding several films is left alone: only the file names inside are cleaned up.",
        "Enseña qué cambiaría para que cada película acabe como «Título (Año)/Título (Año).ext», que es lo que esperan Plex y Jellyfin. Una carpeta con varias películas dentro no se desmonta: solo se limpian los nombres de los ficheros.");

    // De qué es la carpeta. Va lo primero de la fila porque condiciona todo lo
    // demás: una película no tiene catálogo, ni temporada, ni número.
    public string OrganizarTipoBiblioteca => Idioma.Elegir("Library", "Biblioteca");
    public string OrganizarTipoSerie => Idioma.Elegir("TV series", "Serie");
    public string OrganizarTipoPelicula => Idioma.Elegir("Films", "Películas");

    public string OrganizarTipoBibliotecaAyuda => Idioma.Elegir(
        "What this folder holds. A series is identified against a catalogue, with seasons and episode numbers; a film has none of that — its name is built from the title and the year read from the file itself. Remembered per folder.",
        "Qué hay en esta carpeta. Una serie se identifica contra un catálogo, con temporadas y números de episodio; una película no tiene nada de eso — su nombre se compone con el título y el año leídos del propio fichero. Se recuerda por carpeta.");

    public string OrganizarVinculosAyuda => Idioma.Elegir(
        "Folders linked to this catalogue: pick one, link the current one or remove the link",
        "Carpetas vinculadas a este catálogo: elegir una, vincular la actual o quitarla");

    public string OrganizarElegirCarpeta => Idioma.Elegir(
        "Choose a folder to start",
        "Elige una carpeta para empezar");

    public string OrganizarSinVideos => Idioma.Elegir(
        "There are no videos in this folder or in its subfolders",
        "No hay vídeos en esta carpeta ni en sus subcarpetas");

    // {0} = la carpeta.
    public string OrganizarUnFichero => Idioma.Elegir("1 file in {0}", "1 fichero en {0}");
    // {0} = cuántos ficheros, {1} = la carpeta.
    public string OrganizarFicherosEn => Idioma.Elegir("{0} files in {1}", "{0} ficheros en {1}");
    // {0} = ficheros, {1} = en cuántas carpetas, {2} = la carpeta de partida.
    public string OrganizarFicherosEnCarpetas => Idioma.Elegir(
        "{0} files in {1} folders under {2}",
        "{0} ficheros en {1} carpetas de {2}");

    // Entre los dos va el rótulo del botón de analizar, destacado.
    public string OrganizarPromesa1 => Idioma.Elegir(
        "Nothing is renamed without your approval: ",
        "Nada se renombra sin tu aprobación: ");

    public string OrganizarPromesa2 => Idioma.Elegir(
        " only reads the names and proposes. Then you review and apply.",
        " solo lee los nombres y propone. Después revisas y aplicas.");

    public string OrganizarAnalizarCarpeta => Idioma.Elegir("Analyse the folder", "Analizar la carpeta");

    // ── Carpetas vinculadas ─────────────────────────────────────────────────

    // {0} = la serie del catálogo.
    public string OrganizarVinculada => Idioma.Elegir(
        "🔗 Linked to \"{0}\" · it will come up on its own next time",
        "🔗 Vinculada a «{0}» · vendrá sola la próxima vez");

    // {0} = la serie, {1} = cuántas carpetas guardadas.
    public string OrganizarSinVincularConOtras => Idioma.Elegir(
        "Not linked · \"{0}\" has {1} folder(s) saved",
        "Sin vincular · «{0}» tiene {1} carpeta(s) guardada(s)");

    public string OrganizarSinVincular => Idioma.Elegir(
        "Not linked · it will link itself when you analyse",
        "Sin vincular · se vinculará sola al analizar");

    // {0} = la serie. Es la cabecera del menú, que va deshabilitada.
    public string OrganizarVinculosCabecera => Idioma.Elegir(
        "Folders of \"{0}\"",
        "Carpetas de «{0}»");

    // Los dos espacios del principio sangran la entrada bajo la cabecera.
    public string OrganizarVinculosNinguna => Idioma.Elegir("  (none yet)", "  (ninguna todavía)");

    public string OrganizarVincularActual => Idioma.Elegir(
        "Link the current folder to this catalogue",
        "Vincular la carpeta actual a este catálogo");

    public string OrganizarQuitarVinculo => Idioma.Elegir(
        "Remove the link of the current folder",
        "Quitar el vínculo de la carpeta actual");

    public string OrganizarVincularSinCarpeta => Idioma.Elegir(
        "Choose a folder to be able to link it",
        "Elige una carpeta para poder vincularla");

    public string OrganizarVinculosSinCatalogo => Idioma.Elegir(
        "Choose a catalogue first: folders are linked to one in particular.",
        "Elige antes un catálogo: las carpetas se vinculan a uno concreto.");

    // ═══ Etapas de la identificación ════════════════════════════════════════

    public string OrganizarPaso1 => Idioma.Elegir(
        "Reading the file names",
        "Leyendo los nombres de los ficheros");

    public string OrganizarPaso2 => Idioma.Elegir(
        "Identifying them against the catalogue",
        "Identificándolos contra el catálogo");

    public string OrganizarPaso3 => Idioma.Elegir("Preparing the review", "Preparando la revisión");

    public string OrganizarPasoUnNombre => Idioma.Elegir("1 name read", "1 nombre leído");
    public string OrganizarPasoNombres => Idioma.Elegir("{0} names read", "{0} nombres leídos");

    // {0} = la serie del catálogo.
    public string OrganizarPasoContra => Idioma.Elegir("against \"{0}\"", "contra «{0}»");

    public string OrganizarPasoListo => Idioma.Elegir("Identification ready", "Identificación lista");
    public string OrganizarPasoUnListo => Idioma.Elegir("1 ready to apply", "1 listo para aplicar");
    public string OrganizarPasoListos => Idioma.Elegir("{0} ready to apply", "{0} listos para aplicar");

    public string OrganizarIdentificando => Idioma.Elegir("Identifying…", "Identificando…");

    // Lo que se está haciendo DENTRO de cada paso, mientras se hace.
    public string OrganizarPasoMirando => Idioma.Elegir(
        "Reading {0} folders", "Mirando {0} carpetas");
    public string OrganizarPasoCotejando => Idioma.Elegir(
        "Matching {0} names against «{1}»", "Cotejando {0} nombres contra «{1}»");
    public string OrganizarPasoTitulosGrabados => Idioma.Elegir(
        "Reading the title recorded inside {0} unclear files",
        "Leyendo el título grabado dentro de {0} ficheros que no quedan claros");
    public string OrganizarPasoOrdenando => Idioma.Elegir(
        "Sorting and grouping the proposals", "Ordenando y agrupando las propuestas");

    // ═══ Menú contextual de la fila ═════════════════════════════════════════

    public string OrganizarReproducir => Idioma.Elegir("Play", "Reproducir");

    // «Recortes» es el nombre de una pestaña y la ayuda ya lo fija como «Trim».
    public string OrganizarEnviarRecortes => Idioma.Elegir("Send to Trim…", "Enviar a Recortes…");

    public string OrganizarElegirEpisodioHistorias => Idioma.Elegir(
        "Choose episode or stories…",
        "Elegir episodio o historias…");

    // {0} = cuántas historias trae el episodio.
    public string OrganizarElegirEpisodioHistoriasN => Idioma.Elegir(
        "Choose episode or stories… ({0} inside)",
        "Elegir episodio o historias… ({0} dentro)");

    public string OrganizarElegirOtroEpisodio => Idioma.Elegir(
        "Choose another episode…",
        "Elegir otro episodio…");

    public string OrganizarAnadirHistoria => Idioma.Elegir(
        "Add a story from another episode…",
        "Añadir historia de otro episodio…");

    public string OrganizarQuitarHistorias => Idioma.Elegir(
        "Remove the added stories",
        "Quitar las historias añadidas");

    // {0} = cuántas historias añadidas hay.
    public string OrganizarQuitarHistoriasN => Idioma.Elegir(
        "Remove the {0} added stories",
        "Quitar las {0} historias añadidas");

    public string OrganizarDejarComoEstaMenu => Idioma.Elegir(
        "Leave it as it is (and stop asking)",
        "Dejarlo como está (y no volver a preguntar)");

    public string OrganizarApartarRevisar => Idioma.Elegir(
        "Set aside to review later",
        "Apartar para revisar luego");

    public string OrganizarAnadirALaCola => Idioma.Elegir("Add to the queue", "Añadir a la cola");
    public string OrganizarQuitarDeLaCola => Idioma.Elegir("Take out of the queue", "Quitar de la cola");

    // ═══ Tabla de triaje ════════════════════════════════════════════════════

    public string OrganizarMarcarTodosAyuda => Idioma.Elegir(
        "Tick or untick everything that is ready to apply",
        "Marcar o desmarcar todos los listos para aplicar");

    public string OrganizarMarcarFilaAyuda => Idioma.Elegir(
        "Apply the renaming to this file",
        "Aplicar el renombrado a este fichero");

    public string OrganizarColEstado => Idioma.Elegir("STATUS", "ESTADO");
    public string OrganizarColOriginal => Idioma.Elegir("ORIGINAL FILE", "FICHERO ORIGINAL");
    public string OrganizarColPropuesta => Idioma.Elegir("PROPOSAL", "PROPUESTA");
    public string OrganizarColPorQue => Idioma.Elegir("WHY", "POR QUÉ");

    public string OrganizarEnLaCola => Idioma.Elegir("In the queue", "En la cola");

    // Banda que separa cada temporada. El punto va delante del recuento.
    public string OrganizarGrupoUnFichero => Idioma.Elegir("· 1 file", "· 1 fichero");
    public string OrganizarGrupoFicheros => Idioma.Elegir("· {0} files", "· {0} ficheros");

    // ═══ Resolutor de conflictos ════════════════════════════════════════════

    public string OrganizarQuedariaComo => Idioma.Elegir("It would be:", "Quedaría como:");

    public string OrganizarEsteFichero => Idioma.Elegir("THIS FILE", "ESTE FICHERO");

    public string OrganizarElOtroFichero => Idioma.Elegir(
        "THE OTHER ONE (the one the app keeps)",
        "EL OTRO (el que la app conserva)");

    public string OrganizarEnviarEsteAPapelera => Idioma.Elegir(
        "Send this one to the Recycle Bin",
        "Enviar este a la Papelera");

    public string OrganizarEnviarEsteAyuda => Idioma.Elegir(
        "Sends THIS file to the Recycle Bin. The other one stays. Ctrl+Z to undo.",
        "Manda ESTE fichero a la Papelera. El otro se queda. Ctrl+Z para deshacer.");

    public string OrganizarEnviarOtroAPapelera => Idioma.Elegir(
        "Send the other one to the Recycle Bin",
        "Enviar el otro a la Papelera");

    public string OrganizarEnviarOtroAyuda => Idioma.Elegir(
        "Sends the OTHER file to the Recycle Bin and keeps this one as the good copy. Ctrl+Z to undo.",
        "Manda el OTRO fichero a la Papelera y deja este como la copia buena. Ctrl+Z para deshacer.");

    public string OrganizarAbrirCarpeta => Idioma.Elegir("Open folder", "Abrir carpeta");

    public string OrganizarRecordarDecision => Idioma.Elegir(
        "Remember this decision for this file",
        "Recordar esta decisión para este fichero");

    public string OrganizarPartirEnDos => Idioma.Elegir("Split it in two…", "Partirlo en dos…");

    public string OrganizarPartirEnDosAyuda => Idioma.Elegir(
        "Opens it in Trim with a cut already in place, ready for you to adjust",
        "Lo abre en Recortes con un corte ya puesto, listo para ajustarlo");

    public string OrganizarDejarComoEsta => Idioma.Elegir("Leave it as it is", "Dejarlo como está");

    // ═══ Barra de acciones ══════════════════════════════════════════════════

    public string OrganizarAnalizar => Idioma.Elegir("Analyse", "Analizar");

    public string OrganizarAnalizarAyuda => Idioma.Elegir(
        "Reads the names and proposes. It touches no file.",
        "Lee los nombres y propone. No toca ningún fichero.");

    public string OrganizarAplicarAyuda => Idioma.Elegir(
        "Renames only what was identified with confidence and what you have confirmed. Doubts are left as they are.",
        "Renombra solo lo identificado con confianza y lo que hayas confirmado. Las dudas se quedan como están.");

    public string OrganizarAplicarAyudaDetalle => Idioma.Elegir(
        "Renames ONLY the green files that are ticked. Conflicts and doubts are never touched, whatever state they are in.",
        "Renombra SOLO los ficheros en verde que estén marcados. Los conflictos y las dudas nunca se tocan, estén como estén.");

    // {0} = cuántos van a renombrarse.
    public string OrganizarAplicarMarcados => Idioma.Elegir("Apply {0} ticked", "Aplicar {0} marcados");
    // {0} = marcados, {1} = listos en total.
    public string OrganizarAplicarDe => Idioma.Elegir("Apply {0} of {1}", "Aplicar {0} de {1}");

    public string OrganizarQueFalta => Idioma.Elegir("What is missing…", "Qué falta…");

    public string OrganizarQueFaltaAyuda => Idioma.Elegir(
        "Compares the catalogue with what you have and says which episodes are missing. It counts by stories: a chapter you only have two of its three of comes out as incomplete.",
        "Compara el catálogo con lo que tienes y dice qué episodios faltan. Cuenta por historias: un capítulo del que solo tienes dos de sus tres sale como incompleto.");

    public string OrganizarPartirSegmentos => Idioma.Elegir("Split into segments…", "Partir en segmentos…");
    // {0} = cuántas filas se pueden partir.
    public string OrganizarPartirSegmentosN => Idioma.Elegir(
        "Split {0} into segments…",
        "Partir {0} en segmentos…");

    public string OrganizarPartirSegmentosAyuda => Idioma.Elegir(
        "Separates the episodes that bring several mini-stories into one file per story, numbered 1a, 1b, 1c. It cuts without re-encoding: no quality is lost.",
        "Separa los episodios que traen varias mini-historias en un fichero por historia, numerados 1a, 1b, 1c. Corta sin recodificar: no pierde calidad.");

    public string OrganizarDeshacerAyuda => Idioma.Elegir(
        "Returns the files of the last batch to their previous name",
        "Devuelve los ficheros del último lote a su nombre anterior");

    public string OrganizarDeshacerLote => Idioma.Elegir("Undo last batch", "Deshacer último lote");

    // El de la banda verde que sale justo después de aplicar: ahí «este» es el
    // lote que se acaba de hacer, y por eso no dice «el último».
    public string OrganizarDeshacerEsteLote => Idioma.Elegir("Undo this batch", "Deshacer este lote");

    public string OrganizarEstadoInicial => Idioma.Elegir(
        "Choose a folder and a catalogue",
        "Elige una carpeta y un catálogo");

    public string OrganizarMemoria => Idioma.Elegir("Decision memory…", "Memoria de decisiones…");

    public string OrganizarMemoriaAyuda => Idioma.Elegir(
        "See and forget the decisions you have been making",
        "Ver y olvidar las decisiones que has ido tomando");

    // ═══ La cola ════════════════════════════════════════════════════════════

    public string OrganizarColaAyuda => Idioma.Elegir(
        "The queue: what you have kept to look at later. It survives closing the app.",
        "La cola: lo que has guardado para mirar luego. Sobrevive al cierre de la app.");

    // El espacio inicial separa el texto del glifo ☰, que va en un Run aparte.
    public string OrganizarCola => Idioma.Elegir(" Queue", " Cola");
    // {0} = cuántos ficheros hay dentro.
    public string OrganizarColaN => Idioma.Elegir(" Queue · {0}", " Cola · {0}");

    public string OrganizarColaTitulo => Idioma.Elegir("IN THE QUEUE", "EN LA COLA");

    // Cita el rótulo del menú contextual: si cambia allí, cambia aquí.
    public string OrganizarColaVacia => Idioma.Elegir(
        "Nothing here yet. Right-click a file and pick \"Add to the queue\".",
        "Todavía no hay nada. Con el botón derecho sobre un fichero, «Añadir a la cola».");

    public string OrganizarSacarDeLaCola => Idioma.Elegir("Take out of the queue", "Sacar de la cola");

    public string OrganizarVaciarCola => Idioma.Elegir("Empty the queue", "Vaciar la cola");

    public string OrganizarColaNoEnAnalisis => Idioma.Elegir(
        "not in this analysis - click to open its folder",
        "no está en este análisis - pulsa para abrir su carpeta");

    public string OrganizarColaNoEstaEnDisco => Idioma.Elegir(
        "no longer on the disk",
        "ya no está en el disco");

    // {0} = el nombre del fichero.
    public string OrganizarColaYaNoEsta => Idioma.Elegir(
        "\"{0}\" is no longer on the disk. Take it out of the queue.",
        "«{0}» ya no está en el disco. Sácalo de la cola.");

    // {0} = cuántos ficheros hay en la cola.
    public string OrganizarVaciarColaMensaje => Idioma.Elegir(
        "The {0} files in the queue are going to be taken out.\n\nNo file is touched: only the list is forgotten.",
        "Se van a quitar los {0} ficheros de la cola.\n\nNo se toca ningún fichero: solo se olvida la lista.");

    // ═══ Estado del pie ═════════════════════════════════════════════════════

    // {0} = ficheros, {1} = listos para aplicar, {2} = por despachar.
    public string OrganizarResumen => Idioma.Elegir(
        "{0} files · {1} ready to apply · {2} to sort out",
        "{0} ficheros · {1} listos para aplicar · {2} por despachar");

    // Se pegan al resumen anterior, de ahí el punto del principio.
    public string OrganizarResumenYaBien => Idioma.Elegir(
        " · {0} were already right",
        " · {0} ya estaban bien");

    public string OrganizarResumenNube => Idioma.Elegir(
        " · {0} only in the cloud (not opened)",
        " · {0} solo en la nube (no se abren)");

    public string OrganizarImportaCatalogo => Idioma.Elegir(
        "Import a catalogue to start",
        "Importa un catálogo para empezar");

    public string OrganizarElegirCarpetaVideos => Idioma.Elegir(
        "Choose a folder with videos",
        "Elige una carpeta con vídeos");

    // {0} = la serie, {1} = cuántos ficheros.
    public string OrganizarCatalogoListo => Idioma.Elegir(
        "Catalogue {0} · {1} files ready to analyse",
        "Catálogo {0} · {1} ficheros listos para analizar");

    // {0} = cuántos ficheros se están renombrando.
    public string OrganizarRenombrando => Idioma.Elegir("Renaming {0} files…", "Renombrando {0} ficheros…");

    // ── Banda de aviso cuando el lote viene dudoso ──────────────────────────

    // {0} = cuántas dudas, {1} = cuántos ficheros. Detrás se pega la explicación,
    // de ahí el espacio final.
    public string OrganizarBannerDudas => Idioma.Elegir(
        "{0} of {1} files need you to decide. ",
        "{0} de {1} ficheros necesitan que decidas tú. ");

    public string OrganizarDudasSinFechas => Idioma.Elegir(
        "This catalogue brings no air dates, so the title is all there is to go on.",
        "Este catálogo no trae fechas de emisión, así que solo se puede tirar del título.");

    public string OrganizarDudasRevisa => Idioma.Elegir(
        "Look at the conflicts and the specials before applying.",
        "Revisa los conflictos y los especiales antes de aplicar.");

    // ═══ Confirmación de aplicar ════════════════════════════════════════════

    public string OrganizarConfirmarTitulo => Idioma.Elegir("Apply the renaming", "Aplicar el renombrado");

    public string OrganizarConfirmarUno => Idioma.Elegir(
        "1 confidently identified file will be renamed.",
        "Se renombra 1 fichero identificado con confianza.");

    // {0} = cuántos ficheros.
    public string OrganizarConfirmarVarios => Idioma.Elegir(
        "{0} confidently identified files will be renamed.",
        "Se renombran {0} ficheros identificados con confianza.");

    // {0} = la enumeración de lo que no se toca, ya montada.
    public string OrganizarConfirmarNoSeToca => Idioma.Elegir(
        "These stay exactly as they are: {0}.",
        "Se quedan exactamente como están: {0}.");

    public string OrganizarConfirmarUnaDuda => Idioma.Elegir("1 file with doubts", "1 fichero con dudas");
    public string OrganizarConfirmarDudas => Idioma.Elegir("{0} files with doubts", "{0} ficheros con dudas");
    public string OrganizarConfirmarUnDesmarcado => Idioma.Elegir("1 you have unticked", "1 que has desmarcado");
    public string OrganizarConfirmarDesmarcados => Idioma.Elegir("{0} you have unticked", "{0} que has desmarcado");

    // Une los dos trozos anteriores. Los espacios de los extremos son parte del
    // texto: van entre dos frases.
    public string OrganizarConfirmarY => Idioma.Elegir(" and ", " y ");

    public string OrganizarConfirmarRegistro => Idioma.Elegir(
        "A record of the batch is saved before anything is touched: you will be able to undo the whole of it whenever you want.",
        "Se guarda un registro del lote antes de tocar nada: podrás deshacerlo entero cuando quieras.");

    // ── Resultado ───────────────────────────────────────────────────────────

    // {0} = renombrados, {1} = el apunte de los compañeros (puede ir vacío).
    public string OrganizarAplicadoOk => Idioma.Elegir(
        "{0} files renamed{1}.",
        "{0} ficheros renombrados{1}.");

    // {0} = renombrados, {1} = compañeros, {2} = los que fallaron.
    public string OrganizarAplicadoConFallos => Idioma.Elegir(
        "{0} files renamed{1} · {2} could not be done.",
        "{0} ficheros renombrados{1} · {2} no se pudieron.");

    // {0} = cuántos ficheros compañeros viajaron con los vídeos. Las extensiones
    // no se traducen.
    public string OrganizarAplicadoCompaneros => Idioma.Elegir(
        " (+{0} .nfo/.srt companions)",
        " (+{0} compañeros .nfo/.srt)");

    public string OrganizarNadaQueRenombrar => Idioma.Elegir(
        "Nothing was left to rename.",
        "No quedó nada que renombrar.");

    // {0} = el mensaje del error.
    public string OrganizarNoSeGuardoRegistro => Idioma.Elegir(
        "The batch record could not be saved, so nothing gets renamed: {0}",
        "No se pudo guardar el registro del lote, no se renombra nada: {0}");

    // ═══ Diálogos ═══════════════════════════════════════════════════════════

    // Título de los avisos de esta pantalla: es el nombre de la pestaña.
    public string OrganizarTitulo => Idioma.Elegir("Organise", "Organizar");

    public string OrganizarQuitarCatalogoTitulo => Idioma.Elegir("Remove catalogue", "Quitar catálogo");

    // {0} = la serie, {1} = de dónde salió el catálogo (los dos textos de abajo).
    public string OrganizarQuitarCatalogoPregunta => Idioma.Elegir(
        "Remove \"{0}\" from the app?{1}",
        "¿Quitar «{0}» de la app?{1}");

    // {0} = la ruta del fichero.
    public string OrganizarQuitarCatalogoInterno => Idioma.Elegir(
        "\n\nIt is an internal copy of the app (from an earlier version):\n{0}\n\nThat copy will be deleted. If you still have the original JSON, you will be able to import it again.",
        "\n\nEs una copia interna de la app (de una versión anterior):\n{0}\n\nSe borrará esa copia. Si aún tienes el JSON original, podrás volver a importarlo.");

    public string OrganizarQuitarCatalogoExterno => Idioma.Elegir(
        "\n\nThe app will stop using it. Your file is NOT touched:\n{0}",
        "\n\nLa app dejará de usarlo. Tu fichero NO se toca:\n{0}");

    public string OrganizarCatalogoYaNoEstaba => Idioma.Elegir(
        "That catalogue was already gone.",
        "Ese catálogo ya no estaba.");

    // {0} = el mensaje del error.
    public string OrganizarNoSeLeyoCatalogo => Idioma.Elegir(
        "The catalogue could not be read: {0}",
        "No se pudo leer el catálogo: {0}");

    public string OrganizarNoSeLeyoCarpeta => Idioma.Elegir(
        "The folder could not be read: {0}",
        "No se pudo leer la carpeta: {0}");

    public string OrganizarElegirCatalogoPrimero => Idioma.Elegir(
        "Choose or import a catalogue first.",
        "Primero elige o importa un catálogo.");

    public string OrganizarImportarTitulo => Idioma.Elegir(
        "Import a reference catalogue",
        "Importar catálogo de referencia");

    // Filtro del diálogo de abrir: solo se traducen los rótulos, nunca los
    // patrones (*.json) ni las barras que los separan.
    public string OrganizarFiltroAbrir => Idioma.Elegir(
        "Reindex catalogue (*.json)|*.json|All files|*.*",
        "Catálogo de reindexado (*.json)|*.json|Todos los archivos|*.*");

    public string OrganizarFiltroGuardar => Idioma.Elegir(
        "Reindex catalogue (*.json)|*.json",
        "Catálogo de reindexado (*.json)|*.json");

    public string OrganizarNoSePudoImportar => Idioma.Elegir(
        "Could not import: {0}",
        "No se pudo importar: {0}");

    // {0} = el mensaje del error, {1} = la dirección de la documentación.
    public string OrganizarNoSeAbrioDoc => Idioma.Elegir(
        "The documentation could not be opened: {0}\n\n{1}",
        "No se pudo abrir la documentación: {0}\n\n{1}");

    public string OrganizarGuardarEjemploTitulo => Idioma.Elegir(
        "Save an example catalogue",
        "Guardar catálogo de ejemplo");

    public string OrganizarEjemploGuardado => Idioma.Elegir(
        "Example saved.\n\nEdit it with your own episodes and then import it. If something does not fit, importing will tell you exactly what to correct.\n\nDo you want to open the specification of the format?",
        "Ejemplo guardado.\n\nEdítalo con tus episodios y luego impórtalo. Si algo no encaja, al importar se te dirá exactamente qué corregir.\n\n¿Quieres abrir la especificación del formato?");

    public string OrganizarNoSeGuardoEjemplo => Idioma.Elegir(
        "The example could not be saved: {0}",
        "No se pudo guardar el ejemplo: {0}");

    public string OrganizarAnalisisFallo => Idioma.Elegir(
        "The analysis failed: {0}",
        "El análisis falló: {0}");

    public string OrganizarPartirTitulo => Idioma.Elegir("Split into segments", "Partir en segmentos");

    // {0} = cuántos episodios, {1} = cuántos ficheros saldrán.
    public string OrganizarPartirMensaje => Idioma.Elegir(
        "{0} episodes bring several mini-stories inside.\n\n{1} files are going to be left, one per story, numbered 1a, 1b, 1c…\nThe cut does NOT re-encode: no quality is lost.\n\nThe originals go to the Recycle Bin (recoverable with Ctrl+Z) only if all of their pieces come out. The ones without a clear cut are left untouched.",
        "{0} episodios traen varias mini-historias dentro.\n\nSe van a dejar {1} ficheros, uno por historia, numerados 1a, 1b, 1c…\nEl corte NO recodifica: no se pierde calidad.\n\nLos originales van a la Papelera (recuperables con Ctrl+Z) solo si salen todos sus trozos. Los que no tengan un corte claro se dejan intactos.");

    // Botón de aceptar del cuadro anterior: cabe poco, así que va el verbo solo.
    public string OrganizarPartirBoton => Idioma.Elegir("Split", "Partir");

    public string OrganizarPapeleraTitulo => Idioma.Elegir(
        "Send to the Recycle Bin",
        "Enviar a la Papelera");

    // {0} = cuál de los dos ficheros (los dos textos de abajo), {1} = su nombre.
    public string OrganizarPapeleraPregunta => Idioma.Elegir(
        "Send {0} to the Recycle Bin?\n\n{1}\n\nYou will be able to get it back with Ctrl+Z or from the Recycle Bin.",
        "¿Enviar {0} a la Papelera?\n\n{1}\n\nPodrás recuperarlo con Ctrl+Z o desde la Papelera.");

    public string OrganizarPapeleraEste => Idioma.Elegir(
        "this duplicate copy",
        "esta copia repetida");

    public string OrganizarPapeleraOtro => Idioma.Elegir(
        "the other file (the one the app keeps)",
        "el otro fichero (el que la app conserva)");

    public string OrganizarPapeleraFalloTitulo => Idioma.Elegir("Could not be done", "No se pudo");

    public string OrganizarPapeleraFallo => Idioma.Elegir(
        "The file could not be sent to the recycle bin. Is it still open in another program?",
        "No se ha podido enviar el fichero a la papelera. ¿Sigue abierto en otro programa?");

    public string OrganizarElegirEpisodioPrincipal => Idioma.Elegir(
        "Choose the main episode of this file first; after that you can add stories from others to it.",
        "Primero elige el episodio principal de este fichero; después ya puedes añadirle historias de otros.");

    public string OrganizarMemoriaTitulo => Idioma.Elegir("Decision memory", "Memoria de decisiones");

    public string OrganizarSinDecisiones => Idioma.Elegir(
        "You have not made any decision worth remembering yet.",
        "Todavía no has tomado ninguna decisión que recordar.");

    // {0} = cuántas decisiones hay guardadas.
    public string OrganizarMemoriaPregunta => Idioma.Elegir(
        "You have {0} remembered decisions.\n\nDo you want to forget them all?",
        "Tienes {0} decisiones recordadas.\n\n¿Quieres olvidarlas todas?");

    // ═══ La fila: columna ESTADO ════════════════════════════════════════════

    // «Hecho» y no el «Listo» de Textos.Comun.cs: aquí es que el fichero YA se
    // renombró, no que esté preparado para renombrarse.
    public string OrganizarEstadoHecho => Idioma.Elegir("Done", "Hecho");

    // El chip es estrecho: «Split in 2» es lo más largo que cabe.
    public string OrganizarEstadoPartir => Idioma.Elegir("Split in 2", "Partir en 2");

    public string OrganizarEstadoCorrecto => Idioma.Elegir("Correct", "Correcto");
    public string OrganizarEstadoConCambios => Idioma.Elegir("With changes", "Con cambios");
    public string OrganizarEstadoEspecial => Idioma.Elegir("Special", "Especial");
    public string OrganizarEstadoConflicto => Idioma.Elegir("Conflict", "Conflicto");

    public string OrganizarTooltipHecho => Idioma.Elegir(
        "Renamed in this batch.",
        "Renombrado en este lote.");

    public string OrganizarTooltipSinCambios => Idioma.Elegir(
        "It is already named exactly as it should be: there is nothing to apply.\n\nThat the name matches in full the one the template would produce is the strongest confirmation there is that this is the episode.",
        "Ya se llama exactamente como debe: no hay nada que aplicar.\n\nQue el nombre coincida entero con el que produciría la plantilla es la confirmación más fuerte que hay de que el episodio es este.");

    // En las tres: {0} = la palabra del estado, {1} = el motivo entre paréntesis
    // (puede ir vacío).
    public string OrganizarTooltipSegura => Idioma.Elegir(
        "{0} · the identification is certain{1}.",
        "{0} · identificación segura{1}.");

    public string OrganizarTooltipRevisar => Idioma.Elegir(
        "{0}, but the identification is NOT confirmed{1}.\n\nThat is why it goes in amber: the name may already be right and still not be this episode. Open it to see what it matched against.",
        "{0}, pero la identificación NO está confirmada{1}.\n\nVa en ámbar por eso: el nombre puede estar ya bien y aun así no ser este episodio. Ábrela para ver contra qué ha casado.");

    public string OrganizarTooltipDecides => Idioma.Elegir(
        "{0} · this one is for you to decide{1}.",
        "{0} · esto lo tienes que decidir tú{1}.");

    // ═══ La fila: columna PROPUESTA ═════════════════════════════════════════

    public string OrganizarPropuestaNoParseable => Idioma.Elegir(
        "- name cannot be parsed · choose an episode by hand",
        "- nombre no parseable · elige episodio a mano");

    // {0} y {2} = los dos números de episodio, {1} y {3} = sus temporadas.
    public string OrganizarPropuestaDosTitulos => Idioma.Elegir(
        "E{0} ({1}) or E{2} ({3})? The title exists twice",
        "¿E{0} ({1}) o E{2} ({3})? El título existe dos veces");

    // {0} = el número del especial, {1} = su título.
    public string OrganizarPropuestaEspecial => Idioma.Elegir(
        "Special {0} - {1}? Confirm",
        "¿Especial {0} - {1}? Confirmar");

    public string OrganizarPropuestaQueEspecial => Idioma.Elegir(
        "Which special is it? Choose by hand",
        "¿Qué especial es? Elegir a mano");

    public string OrganizarPropuestaSinPropuesta => Idioma.Elegir("- no proposal", "- sin propuesta");
    public string OrganizarPropuestaSinCambios => Idioma.Elegir("(no change)", "(sin cambios)");

    // ═══ La fila: columna POR QUÉ ═══════════════════════════════════════════

    // «nº» es la abreviatura de número; en inglés, «no.». La columna es estrecha
    // y estas etiquetas van con la puntuación pegada detrás.
    public string OrganizarPorQueDecision => Idioma.Elegir("your decision", "decisión tuya");
    public string OrganizarPorQueNumFecha => Idioma.Elegir("no. + exact date", "nº + fecha exacta");
    public string OrganizarPorQueOrdinal => Idioma.Elegir("season no.", "nº de temporada");
    public string OrganizarPorQueTitulo => Idioma.Elegir("title", "título");
    public string OrganizarPorQueSegmentos => Idioma.Elegir("{0} segments · titles", "{0} segmentos · títulos");
    public string OrganizarPorQueNumFechaAprox => Idioma.Elegir("no. + date≈", "nº + fecha≈");
    public string OrganizarPorQueTituloDebil => Idioma.Elegir("weak title", "título débil");
    public string OrganizarPorQueSinSenales => Idioma.Elegir("no signals", "sin señales");

    public string OrganizarPorQueCandidatos => Idioma.Elegir("{0} candidates", "{0} candidatos");
    public string OrganizarPorQueAlternativas => Idioma.Elegir("{0} alternatives", "{0} alternativas");

    // ═══ La fila: tarjetas de candidato ═════════════════════════════════════

    public string OrganizarCandidatoActual => Idioma.Elegir(
        "＋ this is the one the app proposes right now",
        "＋ es la que propone la app ahora mismo");

    public string OrganizarCandidatoProbable => Idioma.Elegir("most likely", "más probable");
    public string OrganizarCandidatoAlternativa => Idioma.Elegir("alternative", "alternativa");

    public string OrganizarCandidatoSinNombre => Idioma.Elegir(
        "(no name to propose)",
        "(sin nombre que proponer)");

    // {0} = el número del episodio.
    public string OrganizarBotonDejar => Idioma.Elegir("Keep E{0}", "Dejar E{0}");
    public string OrganizarBotonCambiar => Idioma.Elegir("Change to E{0}", "Cambiar a E{0}");

    public string OrganizarDetalleDosEpisodios => Idioma.Elegir(
        "TWO EPISODES IN ONE SINGLE FILE",
        "DOS EPISODIOS EN UN MISMO FICHERO");

    public string OrganizarDetalleRepetido => Idioma.Elegir("DUPLICATE FILE", "FICHERO REPETIDO");
    public string OrganizarColDuracion => Idioma.Elegir("LENGTH", "DURACIÓN");

    public string OrganizarDuracionDesconocida => Idioma.Elegir(
        "Windows has no length on file for this one. It is not an error: nothing is deduced from it either",
        "Windows no tiene apuntada la duración de este. No es un error: tampoco se deduce nada de él");

    // {0} = cuánto dura una historia en esta carpeta, aprendido de ella misma.
    public string OrganizarDuracionTip => Idioma.Elegir(
        "Read from the file record, without downloading it. Here one story runs about {0}",
        "Leída de la ficha del fichero, sin descargarlo. Aquí una historia dura unos {0}");

    public string OrganizarDetalleConflicto => Idioma.Elegir("RESOLVE CONFLICT", "RESOLVER CONFLICTO");

    // La cabecera del desplegable cuando la app ya resolvio la fila y solo se abre para
    // cambiar la propuesta a mano. No es un conflicto: no hay nada que resolver.
    public string OrganizarDetalleCambiar =>
        Idioma.Elegir("CHANGE THE PROPOSAL", "CAMBIAR LA PROPUESTA");

    public string OrganizarDesconocido => Idioma.Elegir("(unknown)", "(desconocido)");

    // ═══ La fila: píldora de segmento ═══════════════════════════════════════

    // {0} = el título de la historia, {1} = el número del episodio.
    public string OrganizarHistoriaDe => Idioma.Elegir("\"{0}\" (ep. {1})", "«{0}» (ep. {1})");

    // {0} = el número del episodio.
    public string OrganizarEpisodioEntero => Idioma.Elegir(
        "the whole of episode {0}",
        "el episodio {0} entero");

    // {0} = la enumeración de historias, ya montada.
    public string OrganizarSegmentoTrae => Idioma.Elegir(
        "This file brings: {0}.\nWith the right button you can change which stories they are.",
        "Este fichero trae: {0}.\nCon el botón derecho puedes cambiar qué historias son.");

    public string OrganizarSegmentoAviso => Idioma.Elegir(
        "\n\nCAREFUL: it mixes stories from different episodes. The name says so, but the app will not know how to read it back and it will come out as a doubt when you analyse again.",
        "\n\nOJO: mezcla historias de episodios distintos. El nombre lo dice, pero la app no sabrá releerlo y al reanalizar saldrá como duda.");

    // ═══ La fila: motivos que escribe la propia pantalla ════════════════════
    // Salen en la columna «Por qué» y en su globo, así que se traducen igual que
    // los del motor.

    public string OrganizarMotivoDejadoComoEstaba => Idioma.Elegir(
        "You left it as it was",
        "Lo dejaste como estaba");

    public string OrganizarMotivoOtroALaPapelera => Idioma.Elegir(
        "The other file went to the Recycle Bin: this is the copy that is kept.",
        "El otro fichero fue a la Papelera: esta es la copia que se conserva.");

    // {0} = el número del episodio.
    public string OrganizarMotivoElegido => Idioma.Elegir(
        "You chose it: episode {0}",
        "Lo elegiste tú: episodio {0}");

    // {0} = la historia elegida, {1} = el número del episodio.
    public string OrganizarMotivoElegidoHistoria => Idioma.Elegir(
        "You chose it: story \"{0}\" of episode {1}",
        "Lo elegiste tú: la historia «{0}» del episodio {1}");

    // ═══ Banda de la tabla: los vídeos de la raíz ═══════════════════════════

    // Rótulo de la banda que separa los vídeos que NO cuelgan de ninguna
    // carpeta de temporada. Lo pinta LibraryScan.Etiqueta, no la vista.
    public string OrganizarSueltosRaiz => Idioma.Elegir(
        "Loose in the main folder",
        "Sueltos en la carpeta principal");

    // ═══ Panel «Registro» ═══════════════════════════════════════════════════
    // Los ve el usuario mientras trabaja, así que se traducen igual que el
    // resto. Van en orden de aparición dentro de la pantalla.
    //
    // Las comillas: en castellano « », en inglés " ", que es el criterio de
    // todo el fichero.

    // {0} = el nombre original del fichero.
    public string OrganizarLogEpisodioNormal => Idioma.Elegir(
        "\"{0}\" is a normal episode again.",
        "«{0}» vuelve a ser un episodio normal.");

    // {0} = la serie del catálogo, {1} = la carpeta.
    public string OrganizarLogCarpetaVinculada => Idioma.Elegir(
        "Folder linked to \"{0}\": {1}",
        "Carpeta vinculada a «{0}»: {1}");

    // {0} = la carpeta.
    public string OrganizarLogVinculoQuitado => Idioma.Elegir(
        "Link removed: {0}",
        "Vínculo quitado: {0}");

    // {0} = la serie del catálogo.
    public string OrganizarLogCatalogoQuitado => Idioma.Elegir(
        "Catalogue removed: \"{0}\".",
        "Catálogo quitado: «{0}».");

    // {0} = la serie, {1} = cuántos episodios trae.
    public string OrganizarLogCatalogoImportado => Idioma.Elegir(
        "Catalogue imported: {0} ({1} episodes).",
        "Catálogo importado: {0} ({1} episodios).");

    // {0} = la ruta donde se guardó.
    public string OrganizarLogEjemploGuardado => Idioma.Elegir(
        "Example catalogue saved to {0}.",
        "Catálogo de ejemplo guardado en {0}.");

    // {0} = cuántos ficheros. El «(s)» vale para uno y para varios en los dos
    // idiomas, igual que en el resto de recuentos de esta pantalla.
    public string OrganizarLogDejadosComoEstan => Idioma.Elegir(
        "{0} file(s) are left as they are: you decided that already and it was noted down in the catalogue.",
        "{0} fichero(s) se dejan como están: ya lo decidiste y quedó apuntado en el catálogo.");

    // {0} = cuántos dudosos se van a sondear.
    public string OrganizarLogBuscandoTitulos => Idioma.Elegir(
        "Looking for the title of {0} doubtful ones…",
        "Buscando el título de {0} dudosos…");

    // {0} = cuántos son marcadores de «archivos a petición».
    public string OrganizarLogSoloEnLaNube => Idioma.Elegir(
        "{0} are only in the cloud: they are not opened so as not to download them whole.",
        "{0} están solo en la nube: no se abren para no descargarlos enteros.");

    // {0} = cuántos títulos salieron del contenedor o del .nfo.
    public string OrganizarLogTitulosMetadatos => Idioma.Elegir(
        "{0} titles found in the metadata.",
        "{0} títulos encontrados en los metadatos.");

    // {0} = cuántos ficheros, {1} = la serie. Detrás se pega el remate de abajo
    // o un punto, así que esta frase se queda sin puntuación final.
    public string OrganizarLogAnalisis => Idioma.Elegir(
        "Analysis: {0} files against \"{1}\"",
        "Análisis: {0} ficheros contra «{1}»");

    // {0} = en cuántas temporadas. Cierra la frase anterior, de ahí la coma
    // inicial y el punto final.
    public string OrganizarLogAnalisisTemporadas => Idioma.Elegir(
        ", spread over {0} seasons.",
        ", repartidos en {0} temporadas.");

    // {0} = el nombre del fichero.
    public string OrganizarLogSaleDeLaCola => Idioma.Elegir(
        "\"{0}\" leaves the queue.",
        "«{0}» sale de la cola.");

    public string OrganizarLogALaCola => Idioma.Elegir(
        "\"{0}\" to the queue.",
        "«{0}» a la cola.");

    // {0} = el mensaje del error.
    public string OrganizarLogNoSeGuardoCola => Idioma.Elegir(
        "The queue could not be saved: {0}",
        "No se pudo guardar la cola: {0}");

    public string OrganizarLogColaVaciada => Idioma.Elegir("Queue emptied.", "Cola vaciada.");

    // {0} = cuántas filas verdes se han dado por buenas.
    public string OrganizarLogVerdesAceptadas => Idioma.Elegir(
        "{0} green rows accepted; ready to apply.",
        "{0} filas verdes aceptadas; listas para aplicar.");

    // En las tres: {0} = el nombre del fichero, {1} = el número del episodio.
    public string OrganizarLogElegidoAMano => Idioma.Elegir(
        "\"{0}\" → episode {1} (chosen by hand).",
        "«{0}» → episodio {1} (elegido a mano).");

    // «Explorador» es la ventana de explorar el catálogo, que la ayuda ya fija
    // como «Browse the catalogue».
    public string OrganizarLogElegidoExplorador => Idioma.Elegir(
        "\"{0}\" → episode {1} (chosen in the catalogue browser).",
        "«{0}» → episodio {1} (elegido en el explorador).");

    // Esta rompe el orden de las dos de arriba: {0} = el fichero, {1} = la
    // historia elegida, {2} = el episodio del que sale.
    public string OrganizarLogElegidaHistoria => Idioma.Elegir(
        "\"{0}\" → story \"{1}\" of episode {2}.",
        "«{0}» → la historia «{1}» del episodio {2}.");

    // {0} = el nombre del fichero, {1} = en cuántos trozos se parte.
    public string OrganizarLogPartiendo => Idioma.Elegir(
        "Splitting \"{0}\" into {1}…",
        "Partiendo «{0}» en {1}…");

    // Los dos espacios del principio sangran estas dos bajo la línea anterior:
    // son el detalle del fichero que se estaba partiendo.
    // {0} = el motivo que da el planificador.
    public string OrganizarLogSinCorteClaro => Idioma.Elegir(
        "  no clear cut: {0}",
        "  sin corte claro: {0}");

    // {0} = el mensaje del error.
    public string OrganizarLogNoSePudoPartir => Idioma.Elegir(
        "  it could not be split: {0}",
        "  no se pudo partir: {0}");

    // {0} = cuántos episodios se partieron.
    public string OrganizarLogPartidosOk => Idioma.Elegir(
        "{0} episodes split. Analyse again to see them numbered by segment.",
        "Partidos {0} episodios. Vuelve a analizar para verlos numerados por segmento.");

    // {0} = partidos, {1} = cuántos se quedaron sin corte, {2} = la lista de
    // sus nombres, ya montada (y recortada con «…» si son muchos).
    public string OrganizarLogPartidosConFallos => Idioma.Elegir(
        "{0} split. Without a clear cut ({1}): {2} - open them in Trim and mark the cut by hand.",
        "Partidos {0}. Sin corte claro ({1}): {2} - ábrelos en Recortes y marca el corte a mano.");

    // {0} = el fichero que se va, {1} = el que se queda.
    public string OrganizarLogOtroALaPapelera => Idioma.Elegir(
        "To the Recycle Bin: {0}. \"{1}\" becomes the good copy · Ctrl+Z to undo.",
        "A la papelera: {0}. «{1}» pasa a ser la copia buena · Ctrl+Z para deshacer.");

    // Los dos espacios de cada lado del punto lo separan de las dos frases.
    // {0} = el fichero enviado.
    public string OrganizarLogRepetidaALaPapelera => Idioma.Elegir(
        "Duplicate copy sent to the Recycle Bin: {0}  ·  press Ctrl+Z to undo.",
        "Copia repetida enviada a la papelera: {0}  ·  pulsa Ctrl+Z para deshacer.");

    // {0} = el fichero recuperado.
    public string OrganizarLogRestaurado => Idioma.Elegir(
        "Restored from the Recycle Bin: {0}. Analyse again to see it in the list.",
        "Restaurado de la papelera: {0}. Vuelve a analizar para verlo en la lista.");

    public string OrganizarLogNoSeRestauro => Idioma.Elegir(
        "It could not be restored: its place is already taken by another file.",
        "No se pudo restaurar: su sitio ya está ocupado por otro fichero.");

    // {0} = el nombre del fichero, {1} = el episodio añadido con su historia
    // («318b»), que se monta fuera porque la letra puede no estar.
    public string OrganizarLogHistoriaAnadida => Idioma.Elegir(
        "\"{0}\" → episode {1} is added to it. It ends up with a compound name; when you analyse again it will come out as a doubt (it is a file that is not an episode).",
        "«{0}» → se le añade el episodio {1}. Queda con nombre compuesto; al reanalizar saldrá como duda (es un fichero que no es un episodio).");

    // {0} = el nombre del fichero.
    public string OrganizarLogQuedaComoEsta => Idioma.Elegir(
        "\"{0}\" is left as it is. Noted down in the catalogue: it will not come out as a doubt again.",
        "«{0}» queda como está. Apuntado en el catálogo: no volverá a salir como duda.");

    // {0} = el nombre del fichero, {1} = el mensaje del error.
    public string OrganizarLogQuedaComoEstaSinApuntar => Idioma.Elegir(
        "\"{0}\" is left as it is, but it could not be noted down in the catalogue: {1}",
        "«{0}» queda como está, pero no se pudo apuntar en el catálogo: {1}");

    // {0} = el mensaje del error.
    public string OrganizarLogNoSeGuardoDecision => Idioma.Elegir(
        "The decision could not be saved: {0}",
        "No se pudo guardar la decisión: {0}");

    // {0} = el nombre original, {1} = el nombre propuesto, que ya está cogido.
    public string OrganizarLogSeOmite => Idioma.Elegir(
        "\"{0}\" is skipped: \"{1}\" already exists.",
        "Se omite «{0}»: «{1}» ya existe.");

    // {0} = el nombre original, {1} = el mensaje del error.
    public string OrganizarLogNoSeRenombro => Idioma.Elegir(
        "\"{0}\" could not be renamed: {1}",
        "No se pudo renombrar «{0}»: {1}");

    // {0} = cuántos ficheros volvieron a su nombre anterior.
    public string OrganizarLogLoteDeshecho => Idioma.Elegir(
        "Batch undone: {0} files returned to their previous name.",
        "Lote deshecho: {0} ficheros devueltos a su nombre anterior.");

    // {0} = devueltos, {1} = los que no se pudieron.
    public string OrganizarLogLoteDeshechoConFallos => Idioma.Elegir(
        "Batch undone: {0} returned · {1} could not be done.",
        "Lote deshecho: {0} devueltos · {1} no se pudieron.");

    // {0} = el mensaje del error.
    public string OrganizarLogNoSeAbrioUbicacion => Idioma.Elegir(
        "The location could not be opened: {0}",
        "No se pudo abrir la ubicación: {0}");

    public string OrganizarLogMemoriaVaciada => Idioma.Elegir(
        "Decision memory emptied.",
        "Memoria de decisiones vaciada.");

    // ═══ Reordenar por temporadas ════════════════════════════════════════════

    public string OrganizarReordenar => Idioma.Elegir("Sort into seasons…", "Ordenar por temporadas…");

    public string OrganizarReordenarAyuda => Idioma.Elegir(
        "Moves each episode into its season folder, creating it if it is not there. Only touches what is already sorted out: never a file in conflict.",
        "Mueve cada capítulo a la carpeta de su temporada, creándola si no está. Solo toca lo ya curado: nunca un fichero en conflicto.");

    public string ReordenarTitulo => Idioma.Elegir("Sort into seasons", "Ordenar por temporadas");

    public string ReordenarIdiomaCarpeta => Idioma.Elegir("Folder name:", "Nombre de la carpeta:");
    public string ReordenarIdiomaApp => Idioma.Elegir("As the app", "Como la app");

    // {0} = cuántos se moverían.
    public string ReordenarResumen => Idioma.Elegir(
        "{0} files would move to their season folder.",
        "{0} ficheros se moverían a la carpeta de su temporada.");

    public string ReordenarResumenNinguno => Idioma.Elegir(
        "Nothing to move: everything is already in its season folder.",
        "No hay nada que mover: todo está ya en la carpeta de su temporada.");

    // {0} = cuántos NO se mueven, por el motivo que sea.
    public string ReordenarResumenQuietos => Idioma.Elegir(
        " {0} stay where they are.",
        " {0} se quedan donde están.");

    // Lo que va a costar el movimiento, que por fuera no se ve. Un reordenado
    // dentro del mismo disco es instantáneo; entre discos o hacia una nube, no.
    // {0} = cuántos ficheros.
    public string ReordenarRiesgoVolumen => Idioma.Elegir(
        "{0} move to another drive: those get copied and deleted, not just renamed.",
        "{0} van a otro disco: esos se copian y se borran, no solo se renombran.");

    // {0} = cuántos ficheros · {1} = el nombre de la nube.
    public string ReordenarRiesgoNube => Idioma.Elegir(
        "{0} land inside {1} and will be uploaded again.",
        "{0} entran en {1} y se volverán a subir.");

    // {0} = cuántos ficheros.
    public string ReordenarRiesgoMarcador => Idioma.Elegir(
        "{0} are online-only: moving them downloads them in full.",
        "{0} están solo en la nube: moverlos se los baja enteros.");

    // Los motivos, tal y como se le cuentan a quien mira la lista.
    public string ReordenarPorqueVa => Idioma.Elegir("moves", "se mueve");
    public string ReordenarPorqueYaEsta => Idioma.Elegir("already there", "ya está en su sitio");
    public string ReordenarPorqueSinCurar => Idioma.Elegir("not sorted out yet", "sin curar");
    public string ReordenarPorqueSinTemporada => Idioma.Elegir("no season in the catalogue", "sin temporada en el catálogo");
    public string ReordenarPorqueOcupado => Idioma.Elegir("name already taken there", "ese nombre ya está ocupado");

    public string ReordenarVerSoloLosQueVan => Idioma.Elegir(
        "Show only the ones that move",
        "Enseñar solo los que se mueven");

    // {0} = cuántos se van a mover.
    public string ReordenarBoton => Idioma.Elegir("Move {0}", "Mover {0}");
    public string ReordenarBotonNada => Idioma.Elegir("Move", "Mover");

    // Es una simulación hasta que se pulsa: hay que decirlo donde se lee.
    public string ReordenarPie => Idioma.Elegir(
        "Nothing has been moved yet. This is what would happen.",
        "Todavía no se ha movido nada. Esto es lo que pasaría.");

    // {0} = cuántos se movieron.
    public string ReordenarHecho => Idioma.Elegir(
        "{0} files moved.",
        "{0} ficheros movidos.");

    // {0} = movidos, {1} = los que no se pudieron.
    public string ReordenarHechoConFallos => Idioma.Elegir(
        "{0} moved · {1} could not be done.",
        "{0} movidos · {1} no se pudieron.");

    public string ReordenarDeshacer => Idioma.Elegir("Undo the move", "Deshacer el movimiento");

    // {0} = cuántos volvieron a su sitio.
    public string ReordenarDeshecho => Idioma.Elegir(
        "{0} files returned to where they were.",
        "{0} ficheros devueltos a donde estaban.");

    public string ReordenarSinCatalogo => Idioma.Elegir(
        "Analyse the folder against a catalogue first: without it there is no way to know which season each file belongs to.",
        "Analiza antes la carpeta contra un catálogo: sin él no hay forma de saber de qué temporada es cada fichero.");

    // ═══ Decidir de una para todas las filas con la misma causa ══════════════

    // {0} = cuántos especiales se confirmarían en total, esta fila incluida.
    public string OrganizarConfirmarIguales => Idioma.Elegir(
        "Confirm the {0} certain ones",
        "Confirmar los {0} seguros");

    public string OrganizarConfirmarIgualesAyuda => Idioma.Elegir(
        "Accepts what the app proposes for every special that matched a single catalogue entry with no room for doubt. The ones that matched loosely are left alone: those you have to look at.",
        "Acepta lo que la app propone para todos los especiales que casaron con una sola entrada del catálogo sin margen de duda. Los que casaron flojo se quedan como están: esos hay que mirarlos.");

    // {0} = cuántos se confirmaron.
    public string OrganizarLogConfirmadosIguales => Idioma.Elegir(
        "{0} specials confirmed.",
        "{0} especiales confirmados.");

    // {0} = cuántas OTRAS filas tienen exactamente la misma causa.
    public string OrganizarDejarIguales => Idioma.Elegir(
        "Leave the other {0} the same",
        "Dejar igual las otras {0}");

    public string OrganizarDejarIgualesAyuda => Idioma.Elegir(
        "Applies this same decision to every row that is stuck for exactly the same reason. Only offered when that reason has one single right answer for all of them — never for two files fighting over the same episode, where each pair has its own winner.",
        "Aplica esta misma decisión a todas las filas atascadas exactamente por lo mismo. Solo se ofrece cuando esa causa tiene una única respuesta buena para todas — nunca con dos ficheros peleando por el mismo episodio, donde cada pareja tiene su propio ganador.");

    // {0} = cuántos ficheros quedaron apuntados en el catálogo de una vez.
    public string OrganizarLogQuedanComoEstan => Idioma.Elegir(
        "{0} files noted in the catalogue: they will not be asked about again.",
        "{0} ficheros apuntados en el catálogo: no se volverá a preguntar por ellos.");

    // {0} = cuántas se dejaron como estaban.
    public string OrganizarLogDejadasIguales => Idioma.Elegir(
        "{0} rows left as they were: the catalogue has no place for them.",
        "{0} filas dejadas como estaban: el catálogo no tiene sitio para ellas.");

    // ═══ Un fichero que solo está en la nube ═════════════════════════════════

    public string OrganizarSoloEnLaNubeTitulo => Idioma.Elegir(
        "This one is only in the cloud",
        "Este solo está en la nube");

    // {0} = el nombre del proveedor, {1} = el tamaño.
    public string OrganizarSoloEnLaNubeDetalle => Idioma.Elegir(
        "Playing it downloads all {1} from {0} first — there is no way to peek at part of it. If you only want to check which episode it is, it is quicker to watch it on {0} itself.",
        "Reproducirlo se baja antes los {1} enteros de {0} — no hay forma de asomarse a un trozo. Si solo quieres comprobar de qué episodio es, sale más rápido verlo en {0} mismo.");

    public string OrganizarSoloEnLaNubeGenerica => Idioma.Elegir("the cloud", "la nube");

    public string OrganizarDescargarYVer => Idioma.Elegir("Download and play", "Descargar y reproducir");

    public string OrganizarVerloEnLaNube => Idioma.Elegir("Show me the file", "Enséñame el fichero");

    /// <summary>
    /// Cuando su nube SÍ sabe abrirlo en la web. Se dice «verlo», no «abrir el
    /// enlace», porque lo que se ofrece es responder la pregunta —de qué episodio
    /// es— sin bajarse nada.
    /// </summary>
    public string OrganizarVerloEnLaWeb => Idioma.Elegir("Watch it on the web", "Verlo en la web");
}
