namespace Ondine.Localizacion;

/// <summary>
/// Textos del grupo «Reindex»: el motor de identificación
/// (<c>ReindexEngine</c>), la carga y validación del catálogo
/// (<c>ReindexCatalog</c>), la plantilla de nombres
/// (<c>LibraryTemplate</c>), el recuento de lo que falta
/// (<c>CoberturaCatalogo</c>), la tarjeta de un catálogo ya importado
/// (<c>CatalogoGuardado</c>), el botón de deshacer un lote
/// (<c>LoteJournal</c>) y el reparto en segmentos
/// (<c>SegmentSplitter</c>).
///
/// <para>
/// Casi todo lo de aquí acaba en la columna «POR QUÉ» de la tabla de Organizar
/// o en el globo de una fila, así que se traduce con el mismo vocabulario que
/// <c>Textos.Organizar.cs</c>: episode, season, story, catalogue, Recycle Bin.
/// Dos palabras distintas para la misma cosa en dos sitios que se leen juntos
/// se notan más que un error de bulto.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ══ ReindexEngine · motivos de la columna «POR QUÉ» ══════════════════════

    public string ReindexMotivoDejarComoEsta => Idioma.Elegir(
        "You told me to leave it as it is",
        "Dijiste que lo dejara como está");

    public string ReindexMotivoSinPistas => Idioma.Elegir(
        "The name gives no clue at all (no number, no date, no title).",
        "El nombre no da ninguna pista (ni número, ni fecha, ni título).");

    public string ReindexMotivoDecidisteTu => Idioma.Elegir(
        "You decided this one earlier",
        "Lo decidiste tú antes");

    // {0} = el número dentro de la temporada, {1} = la temporada.
    // «el número manda» es el nombre del modo: tiene que decirse igual que en el
    // selector de la pantalla (OrganizarModoNumero → «the number wins»).
    public string ReindexMotivoNumeroManda => Idioma.Elegir(
        "Episode {0} of season {1} (\"the number wins\" mode)",
        "El episodio {0} de la temporada {1} (modo «el número manda»)");

    public string ReindexMotivoChoqueMetaNombre => Idioma.Elegir(
        "The title in the name and the one in the metadata point to different episodes",
        "El título del fichero y el del metadato apuntan a episodios distintos");

    public string ReindexMotivoNumeroYFecha => Idioma.Elegir(
        "The number and the air date match exactly",
        "El número y la fecha de emisión cuadran exactos");

    // {0} = el porcentaje ya montado con su signo (ReindexPorcentaje).
    public string ReindexMotivoTituloCoincide => Idioma.Elegir(
        "The title matches {0}",
        "El título coincide al {0}");

    // {0} y {1} = los dos números de episodio que empatan.
    public string ReindexMotivoEmpateTitulo => Idioma.Elegir(
        "The title fits episode {0} and episode {1} equally well - you choose",
        "El título encaja igual de bien con el episodio {0} y con el {1} - elige tú");

    // {0} = cuántos días de diferencia. Dos claves y no un «día(s)» pegado: el
    // plural inglés cae en otro sitio de la frase que el castellano.
    public string ReindexMotivoFechaBailaUnDia => Idioma.Elegir(
        "The number matches and the date is {0} day out",
        "El número cuadra y la fecha baila {0} día");

    public string ReindexMotivoFechaBailaDias => Idioma.Elegir(
        "The number matches and the date is {0} days out",
        "El número cuadra y la fecha baila {0} días");

    // {0} = el número que trae el fichero, {1} = la temporada de su carpeta,
    // {2} = la temporada que el catálogo le da a ese número.
    // El «.º» del castellano es el ordinal abreviado; en inglés no hay ordinal
    // que valga para cualquier número sin escribirlo a mano (1st/2nd/3rd), así
    // que la frase se reordena a «Number {0} within season {1}».
    public string ReindexMotivoOrdinalContradice => Idioma.Elegir(
        "Number {0} within season {1} - catalogue episode {0} is from {2} and does not match the folder",
        "El {0}.º de la temporada {1} - el episodio {0} del catálogo es de {2} y no cuadra con la carpeta");

    public string ReindexMotivoOrdinalNoExiste => Idioma.Elegir(
        "Number {0} within season {1} - episode {0} does not exist in the overall numbering",
        "El {0}.º de la temporada {1} - el episodio {0} no existe en la numeración global");

    public string ReindexMotivoNumeroSinFecha => Idioma.Elegir(
        "The number exists in the series, but there is no date to confirm it",
        "El número existe en la serie, pero no hay fecha que lo confirme");

    // {0} = el porcentaje ya montado.
    public string ReindexMotivoTituloDebil => Idioma.Elegir(
        "Only {0} alike, below what counts as reliable",
        "Se parece al {0}, por debajo de lo fiable");

    public string ReindexMotivoNada => Idioma.Elegir(
        "It could not be identified from any clue",
        "No se ha podido identificar con ninguna pista");

    // {0} = el motivo de antes, entero: esta frase se COMPONE sobre otra de las
    // de arriba, así que ninguna de ellas puede terminar en punto.
    // {1} = el porcentaje, {2} = el otro número de episodio.
    public string ReindexMotivoPeroElTitulo => Idioma.Elegir(
        "{0}, but the title matches {1} with episode {2}",
        "{0}, pero el título encaja al {1} con el episodio {2}");

    public string ReindexMotivoEspecialSinEspeciales => Idioma.Elegir(
        "It comes marked as a special, but the catalogue has no specials",
        "Viene marcado como especial, pero el catálogo no tiene especiales");

    // {0} = el porcentaje ya montado.
    public string ReindexMotivoEspecialTitulo => Idioma.Elegir(
        "Special: the title fits {0} - confirm it",
        "Especial: el título encaja al {0} - confírmalo");

    // {0} = el número del especial. «nº» / «no.» son la abreviatura de número,
    // la misma que usan las etiquetas de la columna.
    public string ReindexMotivoEspecialNumero => Idioma.Elegir(
        "Special no. {0} - confirm it",
        "Especial nº {0} - confírmalo");

    public string ReindexMotivoEspecialSinTitulo => Idioma.Elegir(
        "Special with no recognisable title - choose which one it is",
        "Especial sin título reconocible - elige a cuál corresponde");

    // {0} y {1} = los dos números de episodio, {2} = el trozo del nombre que
    // delata al segundo.
    public string ReindexMotivoDosEpisodios => Idioma.Elegir(
        "This file brings two catalogue episodes: {0} and {1} (\"{2}\"). Giving it the number of one would lose the other",
        "Este fichero trae dos episodios del catálogo: el {0} y el {1} («{2}»). Ponerle el número de uno perdería el otro");

    // {0} = número del episodio, {1} = cuántas historias tiene según el catálogo.
    // Al revés que el de arriba: allí el fichero trae DOS y se le quiere dar UN
    // número; aquí trae UNA y se le quiere dar el nombre de un episodio de dos.
    public string ReindexMotivoNombreDeMas => Idioma.Elegir(
        "The name declares one story, but episode {0} has {1} in the catalogue. Renaming it as the whole episode would claim it holds stories it does not - say which ones it brings",
        "El nombre declara una historia, pero el episodio {0} tiene {1} en el catálogo. Renombrarlo como el episodio entero afirmaría que trae historias que no trae - di cuáles trae");

    // {0} = número del episodio, {1} = la letra de la historia, {2} = lo que dura.
    // Da los tres datos por lo mismo que el de abajo: quien lee tiene que poder
    // juzgar sin ir a comprobarlo a mano.
    public string ReindexMotivoHistoriaSuelta => Idioma.Elegir(
        "It only holds one story of episode {0}, the \"{1}\" one: it runs {2}, about what a single story runs here. Confirm before applying",
        "Solo trae una historia del episodio {0}, la «{1}»: dura {2}, que es lo que dura una historia aquí. Confírmalo antes de aplicar");

    // {0} = lo que dura el fichero, {1} = número del episodio, {2} = cuántas
    // historias tiene, {3} = lo que suele durar ese número de historias aquí.
    // El aviso da los cuatro datos porque sin ellos no se puede juzgar: «no cuadra»
    // a secas obliga a ir a comprobarlo a mano, que es el trabajo que sobra.
    public string ReindexMotivoRelojNoCuadra => Idioma.Elegir(
        "This file runs {0}, but episode {1} has {2} stories, which in this folder run about {3}. Check whether it really holds the whole episode",
        "Este fichero dura {0}, pero el episodio {1} tiene {2} historias, que en esta carpeta duran unos {3}. Comprueba si de verdad trae el episodio entero");

    // {0} = número del episodio, {1} = su título, {2} = el nombre del otro
    // fichero. «Papelera» es la del sistema operativo: en inglés, «Recycle Bin».
    public string ReindexMotivoRepetido => Idioma.Elegir(
        "Duplicate file: another file covers this same episode {0} \"{1}\" - \"{2}\". Decide which one to keep and send the spare to the Recycle Bin.",
        "Fichero repetido: otro fichero cubre el mismo episodio {0} «{1}» - «{2}». Decide cuál conservar y envía el sobrante a la Papelera.");

    // {0} = el nombre del otro fichero, {1} = número del episodio, {2} = su título.
    public string ReindexMotivoConflicto => Idioma.Elegir(
        "Conflict: this one and \"{0}\" both claim episode {1} \"{2}\". Decide which is the right one.",
        "Conflicto: este y «{0}» reclaman el episodio {1} «{2}». Decide cuál es el correcto.");

    // Se AÑADE detrás del motivo que ya tenía el ganador: el « · » de delante es
    // el pegamento entre las dos frases, no un descuido.
    public string ReindexMotivoTambienReclamado => Idioma.Elegir(
        " · another file was claiming this same episode",
        " · otro fichero reclamaba este mismo episodio");

    // ══ ReindexEngine · evidencia de las tarjetas de candidato ═══════════════
    // Los signos ＋ y － son de ancho completo a propósito: alinean las líneas
    // de la tarjeta. No se tocan al traducir.

    // {0} = el porcentaje ya montado.
    public string ReindexEvidenciaTituloCoincide => Idioma.Elegir(
        "＋ the title matches {0}",
        "＋ el título coincide al {0}");

    public string ReindexEvidenciaNumeroApuntaAOtro => Idioma.Elegir(
        "－ the number in the file points to another episode",
        "－ el número del fichero apunta a otro episodio");

    // {0} = el número que trae escrito el fichero.
    public string ReindexEvidenciaFicheroDice => Idioma.Elegir(
        "＋ the file literally says {0}",
        "＋ el fichero dice literalmente {0}");

    // {0} = la temporada de ese episodio, {1} = la que dice la carpeta.
    public string ReindexEvidenciaOtraTemporada => Idioma.Elegir(
        "－ but that episode is from {0} and the folder says {1}",
        "－ pero ese episodio es de {0} y la carpeta dice {1}");

    // {0} = el porcentaje ya montado.
    public string ReindexEvidenciaNombreCoincide => Idioma.Elegir(
        "＋ the file name matches {0}",
        "＋ el nombre del fichero coincide al {0}");

    public string ReindexEvidenciaMetadatoDiscrepa => Idioma.Elegir(
        "－ the metadata inside the video says otherwise",
        "－ el metadato del vídeo dice otra cosa");

    // {0} = el porcentaje ya montado.
    public string ReindexEvidenciaMetadatoCoincide => Idioma.Elegir(
        "＋ the title inside the video matches {0}",
        "＋ el título interno del vídeo coincide al {0}");

    public string ReindexEvidenciaNombreDiscrepa => Idioma.Elegir(
        "－ the file name says otherwise",
        "－ el nombre del fichero dice otra cosa");

    // {0} = el número ya redondeado, sin signo. El espacio antes del «%» es
    // convención tipográfica española; en inglés va pegado.
    public string ReindexPorcentaje => Idioma.Elegir("{0}%", "{0} %");

    // ══ CatalogEpisode ══════════════════════════════════════════════════════

    // {0} = el número del episodio. OJO: esto no solo se lee en la propuesta,
    // se ESCRIBE dentro del nombre del fichero cuando el catálogo no trae
    // ningún título, así que la misma biblioteca organizada en un idioma o en
    // otro produce nombres distintos. Es el precio de no dejar un «Episodio 437»
    // en castellano colgando en una app en inglés.
    public string ReindexEpisodioSinTitulo => Idioma.Elegir("Episode {0}", "Episodio {0}");

    // ══ ReindexCatalog · por qué no se puede usar el catálogo ════════════════
    // Los nombres de campo entrecomillados («num», «serie», «temporada») son
    // literales del JSON: se traduce la frase, nunca el campo.

    public string ReindexCatalogoNoEsObjeto => Idioma.Elegir(
        "The catalogue is not a JSON object.",
        "El catálogo no es un objeto JSON.");

    // {0} = el mensaje de System.Text.Json, que viene en el idioma del runtime.
    public string ReindexCatalogoJsonInvalido => Idioma.Elegir(
        "The file is not valid JSON: {0}",
        "El archivo no es un JSON válido: {0}");

    public string ReindexCatalogoVacio => Idioma.Elegir(
        "The file is empty.",
        "El archivo está vacío.");

    // {0} = el esquema que trae el fichero.
    public string ReindexCatalogoNoEsCatalogo => Idioma.Elegir(
        "This does not look like a reindex catalogue (schema \"{0}\").",
        "No parece un catálogo de reindexado (esquema «{0}»).");

    public string ReindexCatalogoEsquemaIrreconocible => Idioma.Elegir(
        "Schema version not recognised: \"{0}\".",
        "Versión de esquema irreconocible: «{0}».");

    // {0} = el esquema del fichero, {1} = la versión mayor que sí se entiende.
    // «reindex/» es el identificador del esquema y «Ondine» el producto: ni uno
    // ni otro se traducen dentro de la frase.
    public string ReindexCatalogoEsquemaMuyNuevo => Idioma.Elegir(
        "The catalogue uses schema {0} and this version of Ondine only understands up to reindex/{1}.x. Update the app.",
        "El catálogo usa el esquema {0} y esta versión de Ondine solo entiende hasta reindex/{1}.x. Actualiza la app.");

    public string ReindexCatalogoFaltaSerie => Idioma.Elegir(
        "The \"serie\" field is missing. It is the name that gets written into the files, so it cannot be empty.",
        "Falta el campo «serie». Es el nombre que se escribirá en los ficheros, así que no puede ir vacío.");

    public string ReindexCatalogoSinEpisodios => Idioma.Elegir(
        "The catalogue has no episodes.",
        "El catálogo no tiene episodios.");

    // ── Validación: fallos que se acumulan y se enseñan juntos ───────────────

    // {0} = la posición dentro de la lista. Este trozo se cose DELANTE de los
    // cuatro fallos de abajo, que empiezan por dos puntos o por paréntesis: por
    // eso va en minúscula y sin punto final en los dos idiomas.
    public string ReindexCatalogoDonde => Idioma.Elegir(
        "episode at position {0}",
        "episodio en la posición {0}");

    // {0} = el trozo de arriba, {1} = el valor que trae «num».
    public string ReindexCatalogoNumInvalido => Idioma.Elegir(
        "{0}: \"num\" is {1}; it has to be an integer greater than or equal to 0.",
        "{0}: «num» es {1}; tiene que ser un entero mayor o igual que 0.");

    // {0} = el trozo de arriba, {1} = el número repetido, {2} = dónde salió antes.
    public string ReindexCatalogoNumRepetido => Idioma.Elegir(
        "{0}: number {1} is already used by the one at position {2}. Every \"num\" has to be unique: if it repeats, one episode overwrites the other.",
        "{0}: el número {1} ya lo usa el de la posición {2}. Cada «num» debe ser único: si se repite, un episodio pisa al otro.");

    // {0} = el trozo de arriba, {1} = el número del episodio, {2} = la fecha mala.
    // El patrón SÍ se localiza porque se le está EXPLICANDO al usuario; lo que
    // no cambia es el formato real que se parsea, que sigue siendo yyyy-MM-dd.
    public string ReindexCatalogoFechaInvalida => Idioma.Elegir(
        "{0} (no. {1}): the date \"{2}\" is not valid; it has to be a real date in YYYY-MM-DD format.",
        "{0} (nº {1}): la fecha «{2}» no vale; tiene que ser una fecha real en formato AAAA-MM-DD.");

    // {0} = el trozo de arriba, {1} = el número del episodio, {2} = la temporada.
    public string ReindexCatalogoTemporadaNegativa => Idioma.Elegir(
        "{0} (no. {1}): \"temporada\" is {2}; it cannot be negative.",
        "{0} (nº {1}): «temporada» es {2}; no puede ser negativa.");

    public string ReindexCatalogoDemasiadosFallos => Idioma.Elegir(
        "…and there may be more: checking stopped at fault 20.",
        "…y puede que haya más: se ha parado de comprobar en el fallo 20.");

    public string ReindexCatalogoUnProblema => Idioma.Elegir(
        "The catalogue has one problem:",
        "El catálogo tiene un problema:");

    // {0} = cuántos problemas (siempre > 1).
    public string ReindexCatalogoVariosProblemas => Idioma.Elegir(
        "The catalogue has {0} problems:",
        "El catálogo tiene {0} problemas:");

    // ══ ReindexCatalog · avisos que la UI enseña antes de identificar ════════
    // Cada recuento va en dos claves porque con uno solo el plural canta en los
    // dos idiomas, y estos avisos se leen antes de analizar nada.

    // {0} = la lista de números que faltan, ya separada por comas.
    public string ReindexAvisoHuecos => Idioma.Elegir(
        "The numbering skips {0} - the neighbouring numbers are not consecutive.",
        "La numeración salta el {0} - los números vecinos no son consecutivos.");

    public string ReindexAvisoSinNingunaFecha => Idioma.Elegir(
        "No air dates at all: identification will rest on the title alone - expect more doubts.",
        "Sin fechas de emisión: la identificación dependerá solo del título - espera más dudas.");

    // {0} = cuántos episodios.
    public string ReindexAvisoSinFecha => Idioma.Elegir(
        "{0} episodes have no date: they will be identified by title alone.",
        "{0} episodios no tienen fecha: se identificarán solo por título.");

    public string ReindexAvisoSinFechaUno => Idioma.Elegir(
        "{0} episode has no date: it will be identified by title alone.",
        "{0} episodio no tiene fecha: se identificará solo por título.");

    public string ReindexAvisoSinTemporada => Idioma.Elegir(
        "{0} episodes have no season assigned.",
        "{0} episodios no tienen temporada asignada.");

    public string ReindexAvisoSinTemporadaUno => Idioma.Elegir(
        "{0} episode has no season assigned.",
        "{0} episodio no tiene temporada asignada.");

    // «japonés» es el idioma de los títulos que trae el catálogo, no el de la
    // aplicación: por eso se traduce como idioma y no como «your language».
    public string ReindexAvisoSoloJapones => Idioma.Elegir(
        "{0} episodes only have a Japanese title: those cannot be reached by title, only by number or date.",
        "{0} episodios solo tienen título en japonés: a esos no se llega por título, solo por número o fecha.");

    public string ReindexAvisoSoloJaponesUno => Idioma.Elegir(
        "{0} episode only has a Japanese title: that one cannot be reached by title, only by number or date.",
        "{0} episodio solo tiene título en japonés: a ese no se llega por título, solo por número o fecha.");

    public string ReindexAvisoRemakes => Idioma.Elegir(
        "There are remakes: different episodes with the same title, years apart.",
        "Hay remakes: episodios distintos con el mismo título años después.");

    public string ReindexAvisoEspeciales => Idioma.Elegir(
        "{0} specials: they are numbered apart and need confirming by hand.",
        "{0} especiales: se numeran aparte y necesitan confirmación manual.");

    public string ReindexAvisoEspecialesUno => Idioma.Elegir(
        "{0} special: they are numbered apart and need confirming by hand.",
        "{0} especial: se numeran aparte y necesitan confirmación manual.");

    // ══ LibraryTemplate · las marcas que se ofrecen en la plantilla ══════════
    // La marca en sí («<serie>», «<num:000>») es sintaxis que lee el motor: no
    // se traduce NUNCA, ni suelta ni citada dentro de una explicación. Aquí solo
    // se traducen el rótulo y lo que hace.

    public string ReindexMarcaSerie => Idioma.Elegir("Series", "Serie");
    public string ReindexMarcaSerieAyuda => Idioma.Elegir(
        "The name of the series, exactly as you wrote it in the catalogue.",
        "El nombre de la serie, tal cual lo escribiste en el catálogo.");

    public string ReindexMarcaTemporada => Idioma.Elegir("Season", "Temporada");
    public string ReindexMarcaTemporadaAyuda => Idioma.Elegir(
        "The year or season number of the episode. If the catalogue does not bring it, the one from the folder is used.",
        "El año o número de temporada del episodio. Si el catálogo no lo trae, se usa el de la carpeta.");

    public string ReindexMarcaNumero => Idioma.Elegir("Number", "Número");
    public string ReindexMarcaNumeroAyuda => Idioma.Elegir(
        "The episode number according to the catalogue: the one that gets corrected when the file carried a different one.",
        "El número del episodio según el catálogo: el que se corrige si el fichero traía otro.");

    public string ReindexMarcaTitulo => Idioma.Elegir("Title", "Título");
    public string ReindexMarcaTituloAyuda => Idioma.Elegir(
        "The title of the episode. If it has several stories they are joined with \"+\".",
        "El título del episodio. Si tiene varias historias se unen con «+».");

    public string ReindexMarcaSegmento => Idioma.Elegir("Sub-segment", "Sub-segmento");
    public string ReindexMarcaSegmentoAyuda => Idioma.Elegir(
        "The letter in \"[438a]\", to tell halves of the same episode apart. Empty when there is none.",
        "La letra de «[438a]», para distinguir mitades de un mismo episodio. Vacío si no la hay.");

    public string ReindexMarcaNumeroCeros => Idioma.Elegir("Number padded with zeros", "Número con ceros");
    public string ReindexMarcaNumeroCerosAyuda => Idioma.Elegir(
        "The number padded to that many digits: \"<num:000>\" gives 001, 012, 278. If the number is already longer, nothing is cut.",
        "El número relleno hasta esas cifras: «<num:000>» da 001, 012, 278. Si el número ya es más largo, no se recorta.");

    public string ReindexMarcaTituloSeparador => Idioma.Elegir(
        "Title with another separator",
        "Título con otro separador");

    public string ReindexMarcaTituloSeparadorAyuda => Idioma.Elegir(
        "Like \"<título>\" but joining the stories with whatever you put after the colon, spaces included.",
        "Como «<título>» pero uniendo las historias con lo que pongas tras los dos puntos, espacios incluidos.");

    // ══ CoberturaCatalogo · el resumen de lo que falta ═══════════════════════
    // «historias» son los segmentos de un episodio, no los episodios: la cuenta
    // va por historia a propósito y el resumen tiene que decirlo con esa palabra
    // (la misma que usa FaltantesPie).

    // {0} = cuántas historias tiene el catálogo.
    public string ReindexCoberturaCompleta => Idioma.Elegir(
        "Nothing is missing: all {0} stories are here.",
        "No falta nada: están las {0} historias.");

    // {0} = total, {1} = las que tienes, {2} = las que faltan.
    public string ReindexCoberturaResumen => Idioma.Elegir(
        "Of {0} stories you have {1}; {2} missing.",
        "De {0} historias tienes {1}; faltan {2}.");

    // ══ CatalogoGuardado · la tarjeta de un catálogo importado ══════════════
    // El recuento de episodios NO se repite aquí: es el mismo dato que enseña
    // el explorador, así que se reutilizan CatalogoCuentaEpisodios y
    // CatalogoCuentaEpisodioUno. Dos claves para lo mismo acabarían diciendo
    // cosas distintas en dos tarjetas que se leen a la vez.

    // {0} = la carpeta donde vive el JSON. Es la primera mitad de una línea que
    // puede seguir con ReindexTarjetaAnadido, así que va sin punto final.
    public string ReindexTarjetaEn => Idioma.Elegir("in {0}", "en {0}");

    // {0} = la fecha de alta. Se COSE detrás de la anterior: el « · » de delante
    // es el pegamento entre las dos mitades, no un descuido.
    public string ReindexTarjetaAnadido => Idioma.Elegir(
        " · added on {0}",
        " · añadido el {0}");

    // {0} = la ruta completa del JSON, que en la línea de la tarjeta va
    // recortada. El globo existe para enseñarla entera, así que la ruta no se
    // toca; lo que se traduce es lo de alrededor.
    public string ReindexTarjetaProcedenciaDetalle => Idioma.Elegir(
        "The app reads this file directly (no copy):\n{0}\n\nRight-click to open its location.",
        "La app lee este fichero directamente (sin copia):\n{0}\n\nClic derecho para abrir su ubicación.");

    // {0} = cuántos especiales trae el catálogo. Dos claves porque con una sola
    // el plural canta en los dos idiomas, como en el resto del fichero.
    public string ReindexTarjetaEspeciales => Idioma.Elegir("{0} specials", "{0} especiales");
    public string ReindexTarjetaEspecialUno => Idioma.Elegir("{0} special", "{0} especial");

    // Ninguno: se dice que el catálogo NO los lista, que no es lo mismo que que
    // la serie no los tenga. Sin ese matiz la tarjeta afirmaría algo que no sabe.
    public string ReindexTarjetaSinEspeciales => Idioma.Elegir(
        "no specials in the catalogue",
        "sin especiales catalogados");

    // {0} = cuántos episodios traen más de una historia dentro. Solo se pinta
    // con más de cero; el nombre de la cosa contada va elidido en los dos
    // idiomas porque el trozo de delante ya dijo «episodios».
    public string ReindexTarjetaVariosSegmentos => Idioma.Elegir(
        "{0} with several segments",
        "{0} con varios segmentos");

    // ══ LoteJournal ═════════════════════════════════════════════════════════

    // {0} = la hora del lote, {1} = cuántos ficheros movió. Es el rótulo del
    // botón cuando HAY lote; sin lote, el botón enseña OrganizarDeshacerLote,
    // así que las dos claves tienen que empezar igual («Undo batch…»).
    public string ReindexLoteDeshacer => Idioma.Elegir(
        "Undo batch {0} ({1})",
        "Deshacer lote {0} ({1})");

    // ══ SegmentSplitter ═════════════════════════════════════════════════════

    // {0} = el minuto donde se esperaba el corte, ya redondeado a un decimal.
    // {1} = el nombre de la página de recortes: llega por parámetro (y no
    // escrito aquí) para que siga siendo el mismo rótulo que el usuario ve en
    // la pestaña incluso si allí se retoca.
    public string ReindexSinCorteClaro => Idioma.Elegir(
        "No clear cut was found near minute {0}. Open it in {1} and mark the cut by hand.",
        "No se ha encontrado un corte claro cerca del minuto {0}. Ábrelo en {1} y marca el corte a mano.");

    // {0} = el parecido del título, en porcentaje.
    public string ReindexMotivoLoteLoSostiene => Idioma.Elegir(
        "The title matches {0}, and the rest of the folder backs it: the files around it point to consecutive episodes in the same order",
        "El título coincide al {0}, y el resto de la carpeta lo respalda: los ficheros de alrededor apuntan a episodios consecutivos en el mismo orden");
}
