namespace Ondine.Localizacion;

/// <summary>
/// Los textos de la ventana de Ayuda: el índice de tutoriales y las cuatro
/// páginas de contenido.
///
/// <para>
/// Casi todo son párrafos largos, y eso trae una servidumbre: cada vez que se
/// cita un botón de otra pantalla («Analizar», «Catálogos…», «Qué falta…») la
/// cita tiene que decir EXACTAMENTE lo mismo que el botón real. Si alguien
/// retoca la etiqueta de un botón y no pasa por aquí, la ayuda manda al usuario
/// a buscar algo que ya no existe con ese nombre.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── Barra de título ─────────────────────────────────────────────────────
    // Se usa dos veces: el Title de la ventana (barra de tareas, Alt+Tab) y el
    // TextBlock de la barra propia. Una sola clave para que no se separen.
    public string AyudaTitulo => Idioma.Elegir("Help - Ondine", "Ayuda - Ondine");

    // ── Índice lateral ──────────────────────────────────────────────────────
    public string AyudaNavCabecera => Idioma.Elegir("Tutorials", "Tutoriales");
    public string AyudaNavOrgComo => Idioma.Elegir("Organise · how it works", "Organizar · cómo funciona");
    public string AyudaNavOrgPasos => Idioma.Elegir("Organise · step by step", "Organizar · paso a paso");
    public string AyudaNavComprimir => Idioma.Elegir("Compress · step by step", "Comprimir · paso a paso");

    // «Recortes» es el nombre de una pestaña, no una descripción: en inglés va
    // como «Trim» (verbo corto, cabe en el índice) y así tiene que aparecer en
    // todas las citas de esta ventana y en la pestaña real.
    public string AyudaNavRecortes => Idioma.Elegir("Trim", "Recortes");

    // ── Organizar · cómo funciona ───────────────────────────────────────────
    public string AyudaOrgComoTitulo => Idioma.Elegir(
        "How Organise identifies your files",
        "Cómo identifica Organizar tus ficheros");

    public string AyudaOrgComoIntro => Idioma.Elegir(
        "Organise checks the videos in a folder against a catalogue (a JSON file with the episodes of the series) and proposes the canonical name for each one. It never touches the originals until you press Apply, and it never invents an episode: anything doubtful is left for you to decide.",
        "Organizar coteja los vídeos de una carpeta contra un catálogo (un JSON con los episodios de la serie) y propone el nombre canónico de cada uno. Nunca toca los originales hasta que pulsas Aplicar, y nunca se inventa un episodio: lo dudoso lo deja para que decidas tú.");

    public string AyudaOrgComoLeeTitulo => Idioma.Elegir(
        "WHAT IT READS FROM EACH FILE",
        "QUÉ LEE DE CADA FICHERO");

    public string AyudaOrgComoChipNombre => Idioma.Elegir("File name", "Nombre del fichero");
    public string AyudaOrgComoChipNombreSub => Idioma.Elegir("number · date · title", "número · fecha · título");
    public string AyudaOrgComoChipCarpeta => Idioma.Elegir("Folder", "Carpeta");
    public string AyudaOrgComoChipCarpetaSub => Idioma.Elegir("the season", "la temporada");
    public string AyudaOrgComoChipMetadato => Idioma.Elegir("Video metadata", "Metadato del vídeo");

    // «solo rescate» = el metadato es el último recurso, no una fuente más.
    public string AyudaOrgComoChipMetadatoSub => Idioma.Elegir(
        "embedded title · last resort only",
        "título embebido · solo rescate");

    public string AyudaOrgComoChipCatalogo => Idioma.Elegir("Your catalogue (JSON)", "Tu catálogo (JSON)");
    public string AyudaOrgComoChipCatalogoSub => Idioma.Elegir("titles · number · date", "títulos · número · fecha");

    public string AyudaOrgComoNotaMetadato => Idioma.Elegir(
        "Metadata is only read from files that are doubtful and that are on disk: opening one that lives only in the cloud (OneDrive) would download the whole thing.",
        "El metadato solo se lee de los ficheros dudosos y que estén en disco: abrir uno que está solo en la nube (OneDrive) lo descargaría entero.");

    public string AyudaOrgComoReglasTitulo => Idioma.Elegir(
        "HOW IT DECIDES · THE FIRST RULE THAT FITS WINS",
        "CÓMO DECIDE · GANA LA PRIMERA REGLA QUE ENCAJA");

    public string AyudaOrgComoRegla0Etiqueta => Idioma.Elegir("always wins", "siempre manda");
    public string AyudaOrgComoRegla0 => Idioma.Elegir("your manual decision", "tu decisión manual");

    // Etiqueta de la derecha en dos filas del diagrama: hay sitio para una
    // palabra, no para «a safe match».
    public string AyudaOrgComoEtiquetaSeguro => Idioma.Elegir("certain", "seguro");

    public string AyudaOrgComoRegla1 => Idioma.Elegir(
        "number + air date that match",
        "número + fecha de emisión que cuadran");

    public string AyudaOrgComoRegla2 => Idioma.Elegir(
        "the title matches the catalogue",
        "el título coincide con el catálogo");

    public string AyudaOrgComoEtiquetaRevisar => Idioma.Elegir("to review", "a revisar");

    public string AyudaOrgComoRegla3 => Idioma.Elegir(
        "number + approximate date (±2 days)",
        "número + fecha aproximada (±2 días)");

    public string AyudaOrgComoRegla4 => Idioma.Elegir(
        "the title is similar, but only just",
        "el título se parece, pero poco");

    public string AyudaOrgComoEtiquetaConflicto => Idioma.Elegir("conflict", "conflicto");

    public string AyudaOrgComoConflicto => Idioma.Elegir(
        "two files on the same number, or no clues at all",
        "dos ficheros al mismo número, o sin pistas");

    // Los espacios de delante y de detrás son la separación entre las tres
    // entradas de la leyenda: van dentro del texto porque son tres Run en la
    // misma línea. Si se pierden, la leyenda queda pegada.
    public string AyudaOrgComoLeyendaVerde => Idioma.Elegir(" green: applied on its own   ", " verde: se aplica solo   ");
    public string AyudaOrgComoLeyendaAmbar => Idioma.Elegir(" amber: you decide   ", " ámbar: lo decides tú   ");
    public string AyudaOrgComoLeyendaRojo => Idioma.Elegir(" red: conflict", " rojo: conflicto");

    public string AyudaOrgComoRequisitosTitulo => Idioma.Elegir(
        "WHAT YOUR FILES NEED",
        "QUÉ NECESITAN TUS FICHEROS");

    public string AyudaOrgComoRequisito1 => Idioma.Elegir(
        "• At least one strong clue per file: a recognisable number (S04E01, 4x01, [438], E72…) or a title close to the one in the catalogue.",
        "• Al menos una pista fuerte por fichero: un número reconocible (S04E01, 4x01, [438], E72…) o un título parecido al del catálogo.");

    public string AyudaOrgComoRequisito2 => Idioma.Elegir(
        "• Sorted into season folders (or with Sxx in the name) to get the season right.",
        "• Organizados por carpetas de temporada (o con Sxx en el nombre) para acertar la temporada.");

    // «morralla de la descarga» = las etiquetas del grupo que ripea. En inglés,
    // «download clutter»: ni «rubbish» ni «junk», que suenan a fichero roto.
    public string AyudaOrgComoRequisito3 => Idioma.Elegir(
        "• The catalogue has to include that episode. The download clutter (AMZN, WEB-DL, x265…) is ignored on its own, so there is no need to clean it up.",
        "• El catálogo debe incluir ese episodio. La morralla de la descarga (AMZN, WEB-DL, x265…) la ignora sola, así que no hace falta limpiarla.");

    // ── Organizar · paso a paso ─────────────────────────────────────────────
    public string AyudaOrgPasosTitulo => Idioma.Elegir(
        "Using Organise step by step",
        "Usar Organizar paso a paso");

    // Los dos espacios detrás del número alinean el párrafo: se conservan.
    public string AyudaOrgPaso1 => Idioma.Elegir(
        "1.  Load a catalogue with «Catalogues…» (or import one). It is the JSON file with the episodes of the series.",
        "1.  Carga un catálogo con «Catálogos…» (o impórtalo). Es el JSON con los episodios de la serie.");

    public string AyudaOrgPaso2 => Idioma.Elegir(
        "2.  Choose the folder with the videos. The template at the top defines how they will be named (series, season, episode, title).",
        "2.  Elige la carpeta con los vídeos. La plantilla de arriba define cómo se nombrarán (serie, temporada, episodio, título).");

    public string AyudaOrgPasosVinculadas => Idioma.Elegir(
        "Linked folders: the chain button (next to choosing the folder) saves the folder along with this catalogue, so that the next time you pick it the folder comes on its own. There you can see them all, jump to one or remove the link. Analysing links it by itself, and under the field it says whether it is linked or not.",
        "Carpetas vinculadas: el botón de la cadena (junto a elegir carpeta) guarda la carpeta con este catálogo, para que la próxima vez que lo elijas venga sola. Ahí las ves todas, saltas a una o quitas el vínculo. Al analizar se vincula sola, y bajo el campo pone si está vinculada o no.");

    public string AyudaOrgPaso3 => Idioma.Elegir(
        "3.  Press «Analyse». Each file gets a status: green «Correct» (already named properly), amber «With changes» (it will be renamed) or «Review/Conflict» (you decide).",
        "3.  Pulsa «Analizar». Cada fichero recibe un estado: verde «Correcto» (ya bien nombrado), ámbar «Con cambios» (se renombrará) o «Revisar/Conflicto» (decides tú).");

    public string AyudaOrgPaso4 => Idioma.Elegir(
        "4.  Review. The status chips and their colour tell you what to trust; open a row to see which episode it matched and choose a different one if needed.",
        "4.  Revisa. Los chips de estado y su color te dicen de qué fiarte; abre una fila para ver contra qué episodio ha casado y elegir otro si hace falta.");

    // «píldora» es la etiqueta redondeada de la fila; en inglés, «pill», que es
    // como se llama ese elemento en la propia interfaz.
    public string AyudaOrgPasosCorregirFila => Idioma.Elegir(
        "Correcting a row that looks right: right-clicking any row gives you «Choose episode or stories…», even on a green one. If the episode comes with several mini-stories, there you choose whether the file is the whole episode or only some of them - ticking b and c, for example, leaves it as «E1bc» with those two titles, and the row shows it in a pill.",
        "Corregir una fila que parece correcta: con el botón derecho sobre cualquier fila tienes «Elegir episodio o historias…», aunque esté en verde. Si el episodio trae varias mini-historias, ahí eliges si el fichero es el episodio entero o solo algunas - marcando la b y la c, por ejemplo, queda como «E1bc» con esos dos títulos, y la fila lo enseña en una píldora.");

    // «dejar_como_esta» es una clave del JSON del catálogo: se queda en
    // castellano en los dos idiomas, o el usuario no la encontrará en su fichero.
    public string AyudaOrgPasosFueraCatalogo => Idioma.Elegir(
        "Files that are not in the catalogue (special chapters that appear in no appendix, shorts, presentations): they come up as a conflict over and over because there is nothing to match them against. With the right button, «Leave it as it is (and stop asking)» notes it down IN THE CATALOGUE: the row stays in the list, but green and with no proposal. The file is not touched. It goes in the catalogue JSON and not in the settings so that the decision travels with it if you take it to another machine; to undo it, remove its line from «dejar_como_esta».",
        "Ficheros que no están en el catálogo (capítulos especiales que no salen en ningún anexo, cortos, presentaciones): salen en conflicto una y otra vez porque no hay contra qué casarlos. Con el botón derecho, «Dejarlo como está (y no volver a preguntar)» lo apunta EN EL CATÁLOGO: la fila sigue en la lista, pero en verde y sin propuesta. El fichero no se toca. Va en el JSON del catálogo y no en los ajustes para que la decisión viaje con él si te lo llevas a otro equipo; para deshacerlo, quita su línea del «dejar_como_esta».");

    public string AyudaOrgPasosHistoriasMezcladas => Idioma.Elegir(
        "An odd case: a file that mixes stories from DIFFERENT episodes. With «Add a story from another episode…» they add up, and the name ends with the compound code («E1b+2b») and every title. It is written that way on purpose: the app will not be able to read that name back, and on the next analysis it will come up as a doubt, which is the right thing for a file that is no episode at all. «Remove the added stories» undoes it.",
        "Caso raro: un fichero que mezcla historias de episodios DISTINTOS. Con «Añadir historia de otro episodio…» se van sumando, y el nombre queda con el código compuesto («E1b+2b») y todos los títulos. Se escribe así a propósito: la app no sabrá releer ese nombre y al reanalizar saldrá como duda, que es lo correcto para un fichero que no es ningún episodio. «Quitar las historias añadidas» lo deshace.");

    public string AyudaOrgPaso5 => Idioma.Elegir(
        "5.  Tick the ones you want and press «Apply». Only then are the files renamed. You can «Undo batch» right afterwards.",
        "5.  Marca los que quieras y pulsa «Aplicar». Solo entonces se renombran los ficheros. Puedes «Deshacer lote» justo después.");

    // «a medias» = ni completo ni vacío. «Half done» lo dice sin inventar un
    // estado que la interfaz no tenga.
    public string AyudaOrgPasosQueFalta => Idioma.Elegir(
        "«What is missing…» compares the catalogue with what you have and lists what is not there. It counts by stories, not by chapters: if a chapter brings three and you only have two, it comes up as «half done», something that looks complete when you look at the folder. You can look at one particular season, which is the usual thing when you are completing one, and the drop-down says «Season» or «Year» depending on how your catalogue numbers them. Looking at them all, it only takes into account the ones you have started; you can ask it to include the rest. The list is copied to the clipboard.",
        "«Qué falta…» compara el catálogo con lo que tienes y lista lo que no está. Cuenta por historias, no por capítulos: si un capítulo trae tres y solo tienes dos, sale como «a medias», algo que mirando la carpeta parece completo. Puedes mirar una temporada concreta, lo normal cuando estás completando una, y el desplegable dice «Temporada» o «Año» según cómo las numere tu catálogo. Mirándolas todas, solo tiene en cuenta las que has empezado; puedes pedir que incluya las demás. La lista se copia al portapapeles.");

    // La ventana de «Ordenar por temporadas» llegó en la 1.7.0 y se quedó sin
    // contar aquí. Lo que hay que decir de ella no es que mueve ficheros —eso se
    // ve— sino lo que NO mueve y lo que puede costar.
    // De qué es la carpeta. Es lo primero que se elige y condiciona el resto, así
    // que va antes que los pasos y no perdido entre ellos.
    // De qué es la carpeta. Es lo primero que se elige y condiciona el resto, así
    // que va antes que los pasos y no perdido entre ellos.
    public string AyudaOrgPasosTipo => Idioma.Elegir(
        "«Library»: say whether the folder holds a TV series or films, and the rest of the tab follows. A series is identified against a catalogue, with seasons and episode numbers. A film has none of that — there is no episode list to build a catalogue from, because a film is just a film — so with «Films» selected, «Analyse» opens its own window: it shows what would change so each film ends up as «Title (Year)/Title (Year).ext», which is what Plex and Jellyfin expect, and nothing is touched until you accept. A folder holding several films is treated as a collection and is NOT taken apart: only the file names inside are cleaned up. What it cannot do yet: identify films against a public database, so a misspelt title stays misspelt and two films with the same name cannot be told apart. The choice is remembered per folder.",
        "«Biblioteca»: di si la carpeta tiene una serie o películas, y el resto de la pestaña se adapta. Una serie se identifica contra un catálogo, con temporadas y números de episodio. Una película no tiene nada de eso —no hay anexo de episodios del que sacar un catálogo, porque una película es solo una película—, así que con «Películas» puesto, «Analizar» abre su propia ventana: enseña qué cambiaría para que cada una acabe como «Título (Año)/Título (Año).ext», que es lo que esperan Plex y Jellyfin, y no toca nada hasta que aceptas. Una carpeta con varias películas dentro se trata como colección y NO se desmonta: solo se limpian los nombres de los ficheros. Lo que aún no sabe hacer: identificarlas contra una base de datos pública, así que un título mal escrito seguirá mal escrito y dos películas que se llamen igual no se pueden distinguir. Lo elegido se recuerda por carpeta.");

    public string AyudaOrgPasosReordenar => Idioma.Elegir(
        "«Sort into seasons…»: takes each chapter to its season folder, creating the folder if it is not there. It shows you the whole simulation first — what would move and, above all, what would not and why — and it only moves what is already sorted out: a file still in conflict does not know which season it belongs to, so it stays put. Subtitles and .nfo files travel with their video, nothing is ever overwritten, and it can be undone. You choose the folder name, «Season 03» or «Temporada 03», whichever language the app is in: that name is read by Plex or Jellyfin, not by you, and «Season NN» is the one both recognise without fail. If the move is going to be expensive — another drive, a synced folder, or files that are online-only — it says so before you press.",
        "«Ordenar por temporadas…»: lleva cada capítulo a la carpeta de su temporada, y la crea si no existe. Antes te enseña la simulación entera —qué se movería y, sobre todo, qué no y por qué— y solo mueve lo ya curado: un fichero en conflicto no se sabe de qué temporada es, así que se queda donde está. Los subtítulos y las fichas viajan con su vídeo, nunca se sobrescribe nada, y se puede deshacer. El nombre de la carpeta lo eliges tú, «Season 03» o «Temporada 03», siga la app el idioma que siga: esa carpeta la lee Plex o Jellyfin, no tú, y «Season NN» es la que ambos reconocen sin fallar. Y si el movimiento va a salir caro —otro disco, una carpeta sincronizada, o ficheros que solo están en la nube— te lo dice antes de que pulses.");

    public string AyudaOrgPasosRepetido => Idioma.Elegir(
        "Duplicate file: if two files are the same episode, opening the row shows you BOTH paths (this one and the other one being kept) and you choose which one to send to the Recycle Bin (nothing is ever deleted on its own). Ctrl+Z brings it back.",
        "Fichero repetido: si dos ficheros son el mismo episodio, al abrir la fila verás las DOS rutas (este y el otro que se conserva) y eliges cuál mandar a la Papelera (nunca se borra nada solo). Ctrl+Z lo recupera.");

    public string AyudaOrgPasosSegmentos => Idioma.Elegir(
        "«Split into segments…»: plenty of cartoon series put 2-3 mini-stories in the same chapter. If your catalogue lists them separately, this button leaves one file per story, numbered 1a, 1b, 1c. It looks for the fade to black that separates them and cuts there WITHOUT re-encoding (no quality is lost and it takes a second). The originals go to the Recycle Bin only if all of their pieces come out, and anything without a clear cut is left untouched so you can look at it in Trim.",
        "«Partir en segmentos…»: muchas series de dibujos meten 2-3 mini-historias en un mismo capítulo. Si tu catálogo las lista por separado, este botón deja un fichero por historia, numerados 1a, 1b, 1c. Busca el fundido a negro que las separa y corta ahí SIN recodificar (no pierde calidad y tarda un segundo). Los originales van a la Papelera solo si salen todos sus trozos, y lo que no tenga un corte claro se queda intacto para que lo mires en Recortes.");

    public string AyudaOrgPasosOrdenar => Idioma.Elegir(
        "Press a column header to sort; a doubtful file can be set aside «to review later» with the right button.",
        "Pulsa la cabecera de una columna para ordenar; un fichero dudoso puedes apartarlo «para revisar luego» con el botón derecho.");

    // ── Comprimir · paso a paso ─────────────────────────────────────────────
    public string AyudaComprimirTitulo => Idioma.Elegir(
        "Compressing videos in batches",
        "Comprimir vídeos por lotes");

    public string AyudaComprimirPaso1 => Idioma.Elegir(
        "1.  In «Source» choose a folder (or drag videos onto the list). With «Subfolders» ticked it goes into the seasons.",
        "1.  En «Origen» elige una carpeta (o arrastra vídeos a la lista). Con «Subcarpetas» marcado entra en las temporadas.");

    public string AyudaComprimirPaso2 => Idioma.Elegir(
        "2.  Press «Analyse»: it lists every video with its size, duration, codec and languages.",
        "2.  Pulsa «Analizar»: lista cada vídeo con su tamaño, duración, códec e idiomas.");

    public string AyudaComprimirPaso3 => Idioma.Elegir(
        "3.  Adjust the preset or the options (format, codec, quality, resolution, audio). The main language is marked as the default track; the ones you do not choose are dropped to save space.",
        "3.  Ajusta el preset o las opciones (formato, códec, calidad, resolución, audio). El idioma principal se marca como pista por defecto; los que no elijas se descartan para ahorrar espacio.");

    public string AyudaComprimirPaso4 => Idioma.Elegir(
        "4.  Look at the forecast savings; if you want the real figure, «Measure with a sample» encodes a few short fragments.",
        "4.  Mira el pronóstico de ahorro; si quieres la cifra real, «Medir con una muestra» codifica unos fragmentos cortos.");

    public string AyudaComprimirPaso5 => Idioma.Elegir(
        "5.  Select the videos and press «Compress selection». You will see live progress, with Pause and Stop. It never touches the originals unless you ask it to (and then they go to the Recycle Bin).",
        "5.  Selecciona los vídeos y pulsa «Comprimir selección». Verás el progreso en vivo, con Pausar y Detener. Nunca toca los originales salvo que lo pidas (y entonces van a la Papelera).");

    // Dos papeleras distintas en la misma frase: la interna de la app y la de
    // Windows. En inglés, «its own bin» y «Windows Recycle Bin», que es el
    // nombre real del sistema.
    public string AyudaComprimirPapelera => Idioma.Elegir(
        "«Send to the Recycle Bin» uses a bin of the app's own: you can bring back the last one with Ctrl+Z straight away. As they pile up, or when you close the app, those files move on to the Windows Recycle Bin (still recoverable there).",
        "«Enviar a la Papelera» usa una papelera propia de la app: puedes recuperar lo último con Ctrl+Z al instante. Al acumularse o cerrar la app, esos ficheros pasan a la Papelera de Windows (siguen recuperables ahí).");

    public string AyudaComprimirQuitarPistas => Idioma.Elegir(
        "Removing a dub or some subtitles you are not going to use does NOT need compressing again: with the right button on a video, «Remove audio or subtitle tracks…» repackages the file copying the data as it is. It takes a second, the picture is identical bit for bit, and a file with several dubs can slim down by tens of megabytes. The original goes to the Recycle Bin, so Ctrl+Z brings it back.",
        "Quitar un doblaje o unos subtítulos que no vas a usar NO necesita comprimir de nuevo: con el botón derecho sobre un vídeo, «Quitar pistas de audio o subtítulos…» reempaqueta el fichero copiando los datos tal cual. Tarda un segundo, la imagen queda idéntica bit a bit, y un fichero con varios doblajes puede adelgazar decenas de megas. El original va a la Papelera, así que Ctrl+Z lo recupera.");

    public string AyudaComprimirDiscoLleno => Idioma.Elegir(
        "If the disk fills up, the batch pauses by itself and carries on when you free up space. You can follow the progress from any tab with the pill in the header.",
        "Si el disco se llena, la tanda se pausa sola y sigue cuando liberas espacio. Puedes seguir el progreso desde cualquier pestaña con la píldora de la cabecera.");

    // ── Recortes ────────────────────────────────────────────────────────────
    public string AyudaRecortesTitulo => Idioma.Elegir(
        "Trim: splitting a video or cutting a piece out of it",
        "Recortes: partir un vídeo o quitarle un trozo");

    public string AyudaRecortesPaso1 => Idioma.Elegir(
        "1.  Load a video (or get here from Organise with «Send to Trim…»).",
        "1.  Carga un vídeo (o llega aquí desde Organizar con «Enviar a Recortes…»).");

    // «tramos» son los trozos que se conservan, no los cortes: en inglés
    // «stretches», para no confundirlos con «cuts» dos líneas antes.
    public string AyudaRecortesPaso2 => Idioma.Elegir(
        "2.  Mark the cuts on the timeline to separate the stretches you want to keep (handy for splitting a file that brings two chapters, or for cutting out an intro).",
        "2.  Marca los cortes en la línea de tiempo para separar los tramos que quieres conservar (útil para partir un fichero que trae dos capítulos, o quitar una intro).");

    public string AyudaRecortesPaso3 => Idioma.Elegir(
        "3.  Press «Export»: it generates one file per stretch. The original is not touched.",
        "3.  Pulsa «Exportar»: genera un fichero por tramo. El original no se toca.");

    public string AyudaRecortesPaso4 => Idioma.Elegir(
        "4.  «Clear» leaves the page as if it had just been opened and frees the memory of the video and the thumbnails, without closing the app.",
        "4.  «Vaciar» deja la página como recién abierta y libera la memoria del vídeo y las miniaturas, sin cerrar la app.");

    public string AyudaRecortesPapelera => Idioma.Elegir(
        "With the stretches already on disk, the app offers to send the original to the Recycle Bin (never without asking). It goes to its own bin: if something went wrong, Ctrl+Z brings it back straight away.",
        "Con los tramos ya en disco, la app ofrece mandar el original a la Papelera (nunca sin preguntar). Va a la papelera propia: si algo salió mal, Ctrl+Z lo recupera al instante.");

    public string AyudaRecortesConOrganizarTitulo => Idioma.Elegir(
        "COMBINING IT WITH ORGANISE",
        "COMBINAR CON ORGANIZAR");

    public string AyudaRecortesConOrganizar => Idioma.Elegir(
        "When Organise finds that a file brings TWO episodes, it offers «Split it in two» → it opens in Trim so that you can separate the two chapters. Afterwards each piece is identified and renamed separately in Organise.",
        "Cuando Organizar detecta que un fichero trae DOS episodios, ofrece «Partirlo en dos» → lo abre en Recortes para que separes los dos capítulos. Después, cada trozo se identifica y renombra por separado en Organizar.");
}
