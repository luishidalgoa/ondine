namespace Ondine.Localizacion;

/// <summary>
/// El encargo que se le pasa a una IA para que convierta un anexo de episodios en
/// un catálogo (<c>Reindex/CatalogPrompt.cs</c>).
///
/// <para>
/// Aquí no hay rótulos: es prosa larga que se pinta entera en el cuadro de texto
/// de <c>PromptWindow</c>. Los rótulos de esa ventana viven en
/// <c>Textos.Dialogos.cs</c>; están separados a propósito, porque esto es un
/// documento y aquello es una pantalla.
/// </para>
/// <para>
/// Dos cosas NO se traducen y no es un descuido: los <b>nombres de campo</b> del
/// formato (<c>num</c>, <c>temp</c>, <c>titulos</c>, <c>esquema</c>,
/// <c>episodios</c>…) y los <b>valores</b> de <c>clave</c> (<c>transmision</c>,
/// <c>oficial</c>, <c>continuo</c>). Son claves del JSON: traducirlas produciría
/// un catálogo que el programa rechaza al importar.
/// </para>
/// <para>
/// Los saltos de línea van escritos a mano dentro de cada texto porque el encargo
/// se lee en un cuadro sin ajuste automático: las dos versiones tienen que quedar
/// con el mismo aspecto de bloque, no una en columna estrecha y otra en una línea
/// interminable.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── Huecos cuando el usuario aún no ha escrito nada ──────────────────────
    // Van entre paréntesis y en imperativo: se ven dentro del encargo ya
    // redactado, así que tienen que leerse como una instrucción para quien copia.
    public string EncargoSerieHueco => Idioma.Elegir(
        "(type the series name here)",
        "(escribe aquí el nombre de la serie)");

    public string EncargoFuenteHueco => Idioma.Elegir(
        "(paste the source page address here)",
        "(pega aquí la dirección del anexo)");

    // ── Apertura ────────────────────────────────────────────────────────────
    public string EncargoIntro => Idioma.Elegir(
        "I need you to turn a list of episodes into a JSON catalogue. Follow the\n" +
        "instructions to the letter: the file is read by a program, not by a person.",
        "Necesito que conviertas un anexo de episodios en un catálogo JSON. Sigue las\n" +
        "instrucciones al pie de la letra: el archivo lo va a leer un programa, no una persona.");

    // {0} = el nombre de la serie. Los espacios de sobra alinean las dos
    // etiquetas en columna, y por eso el relleno va DENTRO de cada idioma:
    // «SERIE» y «SERIES» no miden lo mismo.
    public string EncargoSerieLinea => Idioma.Elegir("SERIES: {0}", "SERIE:  {0}");

    // {0} = la dirección del anexo.
    public string EncargoFuenteLinea => Idioma.Elegir("SOURCE: {0}", "FUENTE: {0}");

    public string EncargoLeeEntera => Idioma.Elegir(
        "Read that whole page, including ALL the seasons and their tables.",
        "Lee esa página entera, incluidas TODAS las temporadas y sus tablas.");

    // ── Idiomas ─────────────────────────────────────────────────────────────
    public string EncargoIdiomasTitulo => Idioma.Elegir("## Languages", "## Idiomas");

    // {0} = la lista de idiomas, ya escrita como `es` (Español), `en` (Inglés)…
    public string EncargoIdiomasIncluye => Idioma.Elegir(
        "Include these languages in every episode, whenever the source has them: {0}.",
        "Incluye estos idiomas en cada episodio, siempre que la fuente los tenga: {0}.");

    // {0} = el idioma de salida, dos veces y en el mismo orden en los dos idiomas.
    public string EncargoIdiomasSalida => Idioma.Elegir(
        "The `{0}` language is the one that will be written in the file name. The others are\n" +
        "NOT written, but they are needed all the same: they are what lets the program\n" +
        "recognise files whose name is in another language. That is, a file titled in English\n" +
        "is identified thanks to its English title and renamed with the `{0}` title. So it is\n" +
        "worth not skimping on languages.",
        "El idioma `{0}` es el que se escribirá en el nombre del fichero. Los demás NO se\n" +
        "escriben, pero hacen falta igual: sirven para reconocer ficheros cuyo nombre está en\n" +
        "otro idioma. Es decir, un fichero titulado en inglés se identifica gracias al título\n" +
        "inglés y se renombra con el título en `{0}`. Por eso conviene no escatimar idiomas.");

    public string EncargoIdiomasNoInventes => Idioma.Elegir(
        "If the source does not have one of them, leave that key out of that episode. Do not\n" +
        "invent translations and do not fill the gap with the title from another language: an\n" +
        "invented title causes wrong renames, which is the worst possible outcome.",
        "Si la fuente no trae alguno, omite esa clave en ese episodio. No inventes traducciones\n" +
        "ni rellenes con el título de otro idioma: un título inventado provoca renombrados\n" +
        "equivocados, que es el peor resultado posible.");

    // ── Cómo interpretar la fuente ──────────────────────────────────────────
    public string EncargoFuenteTitulo => Idioma.Elegir(
        "## How to read the source page",
        "## Cómo interpretar la fuente");

    public string EncargoFuenteIntro => Idioma.Elegir(
        "Every source page is put together its own way. Before you write anything, decide:",
        "Cada anexo está montado a su manera. Antes de escribir nada, decide:");

    // Los cuatro puntos numerados. La sangría de tres espacios es la del bloque:
    // si se pierde, la lista deja de leerse como lista.
    public string EncargoDecisionNumero => Idioma.Elegir(
        "1. **Which column is the number. This is the decision you are most likely to get wrong.**\n" +
        "   Many source pages carry SEVERAL numberings at once, and they do not give the same\n" +
        "   result:",
        "1. **Qué columna es el número. Esta es la decisión que más te puedes equivocar.**\n" +
        "   Muchos anexos traen VARIAS numeraciones a la vez, y no dan el mismo resultado:");

    public string EncargoDecisionNumeroTransmision => Idioma.Elegir(
        "   - **Broadcast number** (or \"air order\"): counts the airings in the order they went\n" +
        "     out, with the specials taking their place in the sequence.",
        "   - **Número de transmisión** (u «orden de emisión»): cuenta los pases en el orden\n" +
        "     en que se emitieron, con los especiales ocupando su sitio en la secuencia.");

    public string EncargoDecisionNumeroOficial => Idioma.Elegir(
        "   - **Episode number** (or \"official\"): the canonical numbering of the series, which\n" +
        "     usually leaves the specials out, skips numbers and does not match the real order.",
        "   - **Número de episodio** (u «oficial»): la numeración canónica de la serie, que\n" +
        "     suele dejar los especiales fuera, salta números y no cuadra con el orden real.");

    public string EncargoDecisionNumeroUsa => Idioma.Elegir(
        "   Use the **BROADCAST number** unless you are told otherwise: it is the one that\n" +
        "   usually matches how the files in a collection are numbered, because they are\n" +
        "   downloaded and kept in the order they were broadcast.",
        "   Usa el **número de TRANSMISIÓN** salvo que se te diga otra cosa: es el que suele\n" +
        "   coincidir con cómo están numerados los ficheros de una colección, porque se\n" +
        "   descargan y se guardan en el orden en que se emitieron.");

    // El ejemplo es real y sirve de prueba: si la IA lo entiende, ha entendido la
    // diferencia entre las dos numeraciones.
    public string EncargoDecisionNumeroEjemplo => Idioma.Elegir(
        "   A real example from Doraemon (2005): the premiere of 15-04-2005 is **broadcast 1**,\n" +
        "   but in the official numbering it is a SPECIAL, and the official \"episode 1\" is\n" +
        "   really broadcast 2. Picking the wrong numbering shifts the whole series along and\n" +
        "   makes almost nothing line up.",
        "   Ejemplo real de Doraemon (2005): el estreno del 15-04-2005 es la **transmisión 1**,\n" +
        "   pero en la numeración oficial es un ESPECIAL, y el «episodio 1» oficial es en\n" +
        "   realidad la transmisión 2. Elegir la numeración equivocada desplaza la serie\n" +
        "   entera y hace que casi nada encaje.");

    public string EncargoDecisionNumeroContinuo => Idioma.Elegir(
        "   If the source page only numbers within each season, number continuously yourself\n" +
        "   from 1, following the broadcast order. ALWAYS write down in `clave` which one you\n" +
        "   used.",
        "   Si el anexo solo numera por temporada, numera tú en continuo desde 1 siguiendo\n" +
        "   el orden de emisión. Escribe SIEMPRE en `clave` cuál has usado.");

    public string EncargoDecisionTitulos => Idioma.Elegir(
        "2. **Which columns are titles, and in which language.** \"Title in Spain\" → `es`;\n" +
        "   \"Title in Latin America\" or \"Hispanic America\" → `lat`; \"Original title\" is\n" +
        "   usually `jp` in anime and `en` in American series; go by the alphabet, not by the\n" +
        "   name of the column. \"Literal translation\" is NOT a broadcast title: discard it.",
        "2. **Qué columnas son títulos y de qué idioma.** «Título en España» → `es`;\n" +
        "   «Título en Hispanoamérica» o «Latinoamérica» → `lat`; «Título original» suele ser\n" +
        "   `jp` en anime y `en` en series estadounidenses; fíjate en el alfabeto, no en el\n" +
        "   nombre de la columna. «Traducción literal» NO es un título de emisión: descártala.");

    public string EncargoDecisionFecha => Idioma.Elegir(
        "3. **Which column is the date.** If there are several (original broadcast and local\n" +
        "   premiere), use the ORIGINAL BROADCAST one and write that down in `notas`. Convert\n" +
        "   it to YYYY-MM-DD. If there is no date, leave the field out: better that than an\n" +
        "   invented date.",
        "3. **Qué columna es la fecha.** Si hay varias (emisión original y estreno en España),\n" +
        "   usa la de EMISIÓN ORIGINAL y déjalo escrito en `notas`. Conviértela a AAAA-MM-DD.\n" +
        "   Si no hay fecha, omite el campo: es preferible a una fecha inventada.");

    public string EncargoDecisionSegmentos => Idioma.Elegir(
        "4. **If one episode holds several stories.** It is common in anime: one airing with\n" +
        "   two or three segments, sometimes separated by line breaks or by \"/\" inside the\n" +
        "   same cell. They go as SEVERAL items of that language's array, within the same\n" +
        "   episode. Do not create one episode per segment.",
        "4. **Si un episodio contiene varias historias.** Es corriente en anime: una emisión\n" +
        "   con dos o tres segmentos, a veces separados por saltos de línea o por «/» dentro\n" +
        "   de la misma celda. Van como VARIOS elementos del array de ese idioma, en el mismo\n" +
        "   episodio. No crees un episodio por segmento.");

    // ── Formato ─────────────────────────────────────────────────────────────
    public string EncargoFormatoTitulo => Idioma.Elegir(
        "## Format: these fields and only these",
        "## Formato: estos campos y solo estos");

    public string EncargoFormatoIntro => Idioma.Elegir(
        "This is the COMPLETE list of fields the program understands. Not every source page\n" +
        "carries the same information, so you are expected to use **only the ones you can get**\n" +
        "from the source. A catalogue with fewer fields is perfectly valid.",
        "Esta es la lista COMPLETA de campos que el programa entiende. No todos los anexos\n" +
        "traen la misma información, así que se espera que uses **solo los que puedas sacar**\n" +
        "de la fuente. Un catálogo con menos campos es perfectamente válido.");

    public string EncargoFormatoRegla => Idioma.Elegir(
        "**The rule that is never bent:** you may OMIT fields, never INVENT them. Do not invent\n" +
        "new field names, do not nest things some other way, and do not fill a gap with a\n" +
        "plausible value. An invented value causes wrong renames, which is the worst possible\n" +
        "outcome; a missing one only means that file gets checked by hand.",
        "**La regla que no se salta:** puedes OMITIR campos, nunca INVENTARLOS. Ni inventar\n" +
        "nombres de campo nuevos, ni anidar cosas de otra manera, ni rellenar un hueco con un\n" +
        "valor plausible. Un dato inventado provoca renombrados equivocados, que es el peor\n" +
        "resultado posible; un dato ausente solo hace que ese fichero se revise a mano.");

    // ── Tabla de la raíz ────────────────────────────────────────────────────
    // Los nombres de campo entre acentos graves NO se traducen: son claves del
    // JSON. Lo que se traduce es la columna de la derecha y el «sí / recomendado
    // / opcional» del medio.
    public string EncargoRaizTitulo => Idioma.Elegir("### At the root", "### En la raíz");

    public string EncargoRaizCabecera => Idioma.Elegir(
        "| Field | Always? | What it is |",
        "| Campo | ¿Va siempre? | Qué es |");

    public string EncargoRaizEsquema => Idioma.Elegir(
        "| `esquema` | **yes** | Literally `\"reindex/1.0\"`. |",
        "| `esquema` | **sí** | Literalmente `\"reindex/1.0\"`. |");

    public string EncargoRaizSerie => Idioma.Elegir(
        "| `serie` | **yes** | Name of the series; it gets written into the files. |",
        "| `serie` | **sí** | Nombre de la serie; se escribirá en los ficheros. |");

    public string EncargoRaizEpisodios => Idioma.Elegir(
        "| `episodios` | **yes** | The list. It cannot be empty. |",
        "| `episodios` | **sí** | La lista. No puede ir vacía. |");

    public string EncargoRaizClave => Idioma.Elegir(
        "| `clave` | recommended | Which numbering you used: `transmision`, `oficial`, `continuo`… |",
        "| `clave` | recomendado | Qué numeración usaste: `transmision`, `oficial`, `continuo`… |");

    public string EncargoRaizNotas => Idioma.Elegir(
        "| `notas` | recommended | Oddities of the series and decisions you made. |",
        "| `notas` | recomendado | Rarezas de la serie y decisiones que tomaste. |");

    public string EncargoRaizIdiomas => Idioma.Elegir(
        "| `idiomas` | recommended | `{ \"salida\": \"…\", \"comparar\": [\"…\"] }`. |",
        "| `idiomas` | recomendado | `{ \"salida\": \"…\", \"comparar\": [\"…\"] }`. |");

    public string EncargoRaizTotal => Idioma.Elegir(
        "| `total` | optional | How many episodes there are. Information only. |",
        "| `total` | opcional | Cuántos episodios hay. Solo informativo. |");

    // ── Tabla de cada episodio ──────────────────────────────────────────────
    public string EncargoEpisodioTitulo => Idioma.Elegir(
        "### In each episode",
        "### En cada episodio");

    public string EncargoEpisodioCabecera => Idioma.Elegir(
        "| Field | Always? | If the source does not have it |",
        "| Campo | ¿Va siempre? | Si la fuente no lo trae |");

    public string EncargoEpisodioNum => Idioma.Elegir(
        "| `num` | **yes** | No exceptions: without a number the catalogue cannot be built. |",
        "| `num` | **sí** | No hay excepción: sin número no se puede construir el catálogo. |");

    public string EncargoEpisodioTitulos => Idioma.Elegir(
        "| `titulos` | almost always | Leave it out only if that episode has no title at all. |",
        "| `titulos` | casi siempre | Omítelo solo si ese episodio no tiene ningún título. |");

    public string EncargoEpisodioTemporada => Idioma.Elegir(
        "| `temporada` | if it exists | **Omit the field.** Do not work it out from the year of the date. |",
        "| `temporada` | si existe | **Omite el campo.** No lo deduzcas del año de la fecha. |");

    public string EncargoEpisodioFecha => Idioma.Elegir(
        "| `fecha` | if it exists | **Omit the field.** Never put an approximate date. |",
        "| `fecha` | si existe | **Omite el campo.** Nunca pongas una fecha aproximada. |");

    public string EncargoEpisodioEspecial => Idioma.Elegir(
        "| `especial` | if it applies | Leave it out or put `false`; they are equivalent. |",
        "| `especial` | si aplica | Omítelo o pon `false`; son equivalentes. |");

    public string EncargoEpisodioAliases => Idioma.Elegir(
        "| `aliases` | if they exist | Leave it out or put `[]`; they are equivalent. |",
        "| `aliases` | si existen | Omítelo o pon `[]`; son equivalentes. |");

    // ── Los dos ejemplos ────────────────────────────────────────────────────
    // Del JSON solo se traduce el CONTENIDO de `notas`, que es prosa de ejemplo.
    // Los nombres de campo y el valor de `clave` van tal cual.
    public string EncargoEjemploCompletoTitulo => Idioma.Elegir(
        "### With everything you can possibly have",
        "### Con todo lo que se puede tener");

    public string EncargoEjemploNotas => Idioma.Elegir(
        "which numbering you used, which date, and any oddity of the series",
        "qué numeración usaste, qué fecha, y cualquier rareza de la serie");

    public string EncargoEjemploPobreTitulo => Idioma.Elegir(
        "### With a poor source page (numbers and titles only)",
        "### Con un anexo pobre (solo número y título)");

    public string EncargoEjemploPobreIntro => Idioma.Elegir(
        "Just as valid. What is not there is left out instead of being filled in:",
        "Igual de válido. Se omite lo que no hay, en vez de rellenarlo:");

    public string EncargoEjemploPobreNotas => Idioma.Elegir(
        "the source page has no dates and no seasons",
        "el anexo no trae fechas ni temporadas");

    public string EncargoFaltanFechas => Idioma.Elegir(
        "If the catalogue is missing dates, the program spots it on import and warns that there\n" +
        "will be more doubts to settle by hand. That is right and expected: it beats\n" +
        "identifying the wrong episode because of a date you made up.",
        "Si al catálogo le faltan fechas, el programa lo detecta al importarlo y avisa de que\n" +
        "habrá más dudas que resolver a mano. Eso es correcto y esperable: es mejor que\n" +
        "identificar mal por una fecha que te has inventado.");

    // ── Reglas que valida el importador ─────────────────────────────────────
    public string EncargoReglasTitulo => Idioma.Elegir(
        "## Rules the program checks (if they fail, it rejects the file)",
        "## Reglas que el programa comprueba (si fallan, rechaza el archivo)");

    public string EncargoReglaNum => Idioma.Elegir(
        "- `num` is required, a whole number ≥ 0 and **unique across the whole catalogue**. If\n" +
        "  the source repeats a number, do not duplicate it: decide which one goes in and note\n" +
        "  the clash in `notas`.",
        "- `num` es obligatorio, entero ≥ 0 y **único en todo el catálogo**. Si la fuente\n" +
        "  repite un número, no lo dupliques: decide cuál va y anota el conflicto en `notas`.");

    public string EncargoReglaHuecos => Idioma.Elegir(
        "- **Do not fill in the gaps in the numbering.** Plenty of series officially skip\n" +
        "  numbers. If the source page jumps from 55 to 57, so does your catalogue: do not\n" +
        "  invent a 56.",
        "- **No rellenes los huecos de numeración.** Muchas series saltan números de forma\n" +
        "  oficial. Si el anexo pasa del 55 al 57, tu catálogo también: no inventes un 56.");

    public string EncargoReglaFecha => Idioma.Elegir(
        "- `fecha`, if present, has to be a real date in `YYYY-MM-DD`.",
        "- `fecha`, si está, debe ser una fecha real en `AAAA-MM-DD`.");

    public string EncargoReglaTitulosArray => Idioma.Elegir(
        "- `titulos` are ALWAYS arrays, even when there is only one title.",
        "- `titulos` son SIEMPRE arrays, aunque solo haya un título.");

    public string EncargoReglaEspeciales => Idioma.Elegir(
        "- **Specials only go apart if the numbering you used keeps them apart.** If you number\n" +
        "  by broadcast, a special is one more airing: it takes its number in the sequence and\n" +
        "  goes with `\"especial\": false`. If you number by official episode and the specials\n" +
        "  fall outside that count, then yes: `\"especial\": true` and a range of their own (by\n" +
        "  convention, from 900 upwards).",
        "- **Los especiales solo van aparte si la numeración que has usado los deja aparte.**\n" +
        "  Si numeras por transmisión, un especial es una emisión más: le toca su número en\n" +
        "  la secuencia y va con `\"especial\": false`. Si numeras por episodio oficial y los\n" +
        "  especiales quedan fuera de esa cuenta, entonces sí: `\"especial\": true` y un rango\n" +
        "  propio (por convenio, a partir de 900).");

    public string EncargoReglaCopiaTitulos => Idioma.Elegir(
        "- Copy the titles EXACTLY as they are, with their accents and their punctuation. Do\n" +
        "  not normalise them, do not put them in capitals and do not strip their question\n" +
        "  marks.",
        "- Copia los títulos TAL CUAL, con sus tildes y su puntuación. No los normalices,\n" +
        "  no los pongas en mayúsculas y no les quites los signos de interrogación.");

    public string EncargoReglaReferencias => Idioma.Elegir(
        "- Take the encyclopedia references (\"[1]\", \"[note 2]\") out of the title.",
        "- Quita las referencias de la enciclopedia («[1]», «[nota 2]») de dentro del título.");

    // ── Repaso final ────────────────────────────────────────────────────────
    public string EncargoRepasoTitulo => Idioma.Elegir(
        "## Before you answer, check it yourself",
        "## Antes de responder, comprueba tú mismo");

    public string EncargoRepasoNum => Idioma.Elegir(
        "1. Is any `num` repeated? (this is the most frequent slip)",
        "1. ¿Hay algún `num` repetido? (es el fallo más frecuente)");

    public string EncargoRepasoFechas => Idioma.Elegir(
        "2. Are all the dates in YYYY-MM-DD, and do they really exist?",
        "2. ¿Todas las fechas son AAAA-MM-DD y existen de verdad?");

    public string EncargoRepasoTotal => Idioma.Elegir(
        "3. Does `total` match how many episodes are in the list?",
        "3. ¿`total` coincide con la cantidad de episodios de la lista?");

    public string EncargoRepasoTemporadas => Idioma.Elegir(
        "4. Are ALL the seasons on the page there, not just the first one?",
        "4. ¿Están TODAS las temporadas de la página, no solo la primera?");

    public string EncargoRepasoCampos => Idioma.Elegir(
        "5. Is there any field that is not in the tables above? Take it out.",
        "5. ¿Hay algún campo que no esté en las tablas de arriba? Quítalo.");

    public string EncargoRepasoInventado => Idioma.Elegir(
        "6. Have you filled in a date, a season or a title the source did not give? Take that\n" +
        "   out as well: omitting is right, inventing is not.",
        "6. ¿Has rellenado alguna fecha, temporada o título que la fuente no daba? Quítalo\n" +
        "   también: omitir es correcto, inventar no.");

    // ── Cierre ──────────────────────────────────────────────────────────────
    public string EncargoCierreValida => Idioma.Elegir(
        "You do not have to get the doubtful bits right first time: on import, the program\n" +
        "validates the file and, if something does not fit, says **exactly** what to correct\n" +
        "and in which episode. What it cannot spot is an invented value that looks right, and\n" +
        "that is why this is the one rule with no exceptions.",
        "No hace falta que aciertes a la primera con lo dudoso: al importar, el programa\n" +
        "valida el archivo y, si algo no encaja, dice **exactamente** qué corregir y en qué\n" +
        "episodio. Lo que no puede detectar es un dato inventado que parezca correcto, y por\n" +
        "eso esa es la única regla que no admite excepción.");

    public string EncargoCierreSoloJson => Idioma.Elegir(
        "Answer ONLY with the JSON, with no explanations and no text around it. If the series\n" +
        "is long and does not fit in one go, say so and hand it over in parts, but without\n" +
        "summarising or skipping episodes: an incomplete catalogue leaves files unidentified.",
        "Responde ÚNICAMENTE con el JSON, sin explicaciones ni texto alrededor. Si la serie es\n" +
        "larga y no cabe de una vez, dilo y entrégalo por partes, pero sin resumir ni saltarte\n" +
        "episodios: un catálogo incompleto deja ficheros sin identificar.");
}
