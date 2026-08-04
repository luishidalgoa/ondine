namespace Ondine.Localizacion;

/// <summary>
/// Textos del grupo «Catálogo»: el explorador de solo lectura
/// (<c>CatalogoWindow</c>) y la lista de lo que falta
/// (<c>FaltantesWindow</c>).
/// </summary>
public sealed partial class Textos
{
    // ══ CatalogoWindow ══════════════════════════════════════════════════════

    // ── Cabecera ────────────────────────────────────────────────────────────
    // Vale para el Title de la ventana y para el rótulo de la barra: el mismo
    // texto, una sola clave.
    public string CatalogoTitulo => Idioma.Elegir("Browse the catalogue", "Explorar el catálogo");

    // {0} = nombre de la serie, que viene del catálogo y NO se traduce.
    public string CatalogoTituloSerie => Idioma.Elegir(
        "Browse the catalogue - {0}",
        "Explorar el catálogo - {0}");

    // ── Buscador ────────────────────────────────────────────────────────────
    // El ejemplo se queda en castellano a propósito: describe la normalización
    // del buscador (quita tildes y escribe los ordinales abreviados) sobre los
    // títulos del catálogo, que son los que son. Traducir «planeta espejo» o
    // «segunda» dejaría el ejemplo sin correspondencia con nada real.
    public string CatalogoBuscarAyuda => Idioma.Elegir(
        "Type a number (\"175\") or part of a title (\"planeta espejo\"). It searches the way the identifier does: accents ignored, and \"2.ª\" counts as \"segunda\".",
        "Escribe un número («175») o parte de un título («planeta espejo»). Busca igual que el identificador: sin tildes y con «2.ª» = «segunda».");

    // {0} = cuántos episodios hay. Dos claves porque con uno solo el plural
    // canta en los dos idiomas.
    public string CatalogoCuentaEpisodios => Idioma.Elegir("{0} episodes", "{0} episodios");
    public string CatalogoCuentaEpisodioUno => Idioma.Elegir("{0} episode", "{0} episodio");

    // {0} = encontrados, {1} = total del catálogo.
    public string CatalogoCuentaFiltrada => Idioma.Elegir("{0} of {1}", "{0} de {1}");

    // ── Filas de la lista ───────────────────────────────────────────────────
    // «en español» NO es el idioma de la aplicación: es el idioma de salida del
    // catálogo (<c>TitulosSalida</c>), el que se escribe en el nombre del
    // fichero. Por eso no se traduce por «in your language».
    public string CatalogoSinTitulo => Idioma.Elegir(
        "(no title in Spanish: identified by number or date)",
        "(sin título en español: se identifica por número o fecha)");

    // {0} = cuántas historias. Solo se pinta con más de una, así que el plural
    // vale siempre.
    public string CatalogoCuantasHistorias => Idioma.Elegir("{0} stories", "{0} historias");

    // Distintivo de una fila: tiene que caber en la esquina, sin artículo.
    public string CatalogoEspecial => Idioma.Elegir("special", "especial");

    // ── Panel del JSON ──────────────────────────────────────────────────────
    // {0} = número del episodio. «E» es el código, no una palabra.
    public string CatalogoJsonTitulo => Idioma.Elegir("E{0} · Catalogue JSON", "E{0} · JSON del catálogo");

    public string CatalogoCopiar => Idioma.Elegir("Copy", "Copiar");

    // Se AÑADE al título del panel al copiar: los dos espacios de delante son
    // la separación, no un descuido.
    public string CatalogoJsonCopiado => Idioma.Elegir("  · copied", "  · copiado");

    public string CatalogoCerrarJson => Idioma.Elegir("Close the JSON panel", "Cerrar el panel del JSON");

    // ── Pie ─────────────────────────────────────────────────────────────────
    public string CatalogoPie => Idioma.Elegir(
        "Read only: this is the reference files are identified against. To change a file's episode, use its row in the table.",
        "Solo lectura: es la referencia contra la que se identifica. Para cambiar un fichero de episodio, usa su fila en la tabla.");

    public string CatalogoPieElegir => Idioma.Elegir(
        "Double-click to choose this file's episode. If the episode has several stories, you will be able to say whether the file is only one of them.",
        "Doble clic para elegir el episodio de este fichero. Si el episodio tiene varias historias, podrás decir si el fichero es solo una de ellas.");

    // ── Pregunta de historia ────────────────────────────────────────────────
    // Ojo: esta cita literalmente el rótulo de CatalogoHistoriaCompleto. Si
    // cambia el botón, cambia también la cita.
    public string CatalogoHistoriaAyuda => Idioma.Elegir(
        "If it has the whole episode, choose \"The whole episode\". If it only has some of its stories, tick them: the letters go right after the number (E413b, or E413ac for several) and the title will be the one of those stories.",
        "Si trae el episodio entero, elige «El episodio completo». Si trae solo algunas de sus historias, márcalas: las letras van pegadas al número (E413b, o E413ac si son varias) y el título será el de esas historias.");

    // {0} = número del episodio, {1} = cuántas historias tiene (siempre > 1).
    public string CatalogoHistoriaPregunta => Idioma.Elegir(
        "Episode {0} has {1} stories. Which ones does this file have?",
        "El episodio {0} tiene {1} historias. ¿Cuáles trae este fichero?");

    public string CatalogoHistoriaCompleto => Idioma.Elegir("The whole episode", "El episodio completo");

    public string CatalogoHistoriaMarcar => Idioma.Elegir(
        "Or tick only the stories it has:",
        "O marca solo las historias que trae:");

    // {0} = título de la historia, {1} = su código («E413b»). Lo único que
    // cambia entre idiomas son las comillas; la flecha y el código son formato.
    public string CatalogoHistoriaOpcion => Idioma.Elegir("\"{0}\"  →  {1}", "«{0}»  →  {1}");

    public string CatalogoHistoriaUsar => Idioma.Elegir(
        "Use the ticked stories",
        "Usar las historias marcadas");

    // ══ FaltantesWindow ═════════════════════════════════════════════════════

    // ── Cabecera ────────────────────────────────────────────────────────────
    public string FaltantesTitulo => Idioma.Elegir("What is missing", "Qué falta");

    // {0} = nombre de la serie, dato del catálogo.
    public string FaltantesTituloSerie => Idioma.Elegir("What is missing - {0}", "Qué falta - {0}");

    // ── Alcance ─────────────────────────────────────────────────────────────
    // Hay catálogos que numeran por año de emisión en vez de por temporada, así
    // que cada rótulo del selector viene en las dos formas.
    public string FaltantesTemporada => Idioma.Elegir("Season", "Temporada");
    public string FaltantesAnio => Idioma.Elegir("Year", "Año");

    public string FaltantesTodasLasTemporadas => Idioma.Elegir("All seasons", "Todas las temporadas");
    public string FaltantesTodosLosAnios => Idioma.Elegir("All years", "Todos los años");

    // {0} = número de la temporada.
    public string FaltantesTemporadaN => Idioma.Elegir("Season {0}", "Temporada {0}");

    public string FaltantesIncluirNoEmpezadas => Idioma.Elegir(
        "Include seasons I have not started",
        "Incluir las temporadas que no he empezado");

    // ── Resumen ─────────────────────────────────────────────────────────────
    // {0} = temporada o año, {1} = el resumen que calcula la cobertura (ese
    // texto es de otro grupo y llega ya traducido).
    public string FaltantesResumenTemporada => Idioma.Elegir("Season {0}: {1}", "Temporada {0}: {1}");
    public string FaltantesResumenAnio => Idioma.Elegir("Year {0}: {1}", "Año {0}: {1}");

    // {0} = cuántos especiales faltan (siempre > 0). Dos claves para no arrastrar
    // el «(es)» del original, que era un apaño de plural.
    public string FaltantesEspeciales => Idioma.Elegir(
        "Also missing: {0} specials. They are counted apart because hardly anyone has them all.",
        "Aparte, faltan {0} especiales. Se cuentan por separado porque casi nadie los tiene completos.");

    public string FaltantesEspecialesUno => Idioma.Elegir(
        "Also missing: {0} special. They are counted apart because hardly anyone has them all.",
        "Aparte, falta {0} especial. Se cuentan por separado porque casi nadie los tiene completos.");

    // ── Filas ───────────────────────────────────────────────────────────────
    // Distintivo de cada fila: falta el capítulo entero, o solo parte de sus
    // historias. Va en una etiqueta estrecha, así que una palabra como mucho.
    public string FaltantesEntero => Idioma.Elegir("whole", "entero");
    public string FaltantesAMedias => Idioma.Elegir("partial", "a medias");

    // ── Pie ─────────────────────────────────────────────────────────────────
    public string FaltantesPie => Idioma.Elegir(
        "Counted by stories: an episode with three, and only two on disk, shows up as incomplete.",
        "Se cuenta por historias: un capítulo con tres y solo dos en disco sale como incompleto.");

    public string FaltantesCopiarLista => Idioma.Elegir("Copy the list", "Copiar la lista");

    // {0} = temporada o año. Dos claves porque en castellano el año va sin
    // artículo («de 2005») y la temporada con él («de la temporada 3»).
    public string FaltantesNadaTemporada => Idioma.Elegir(
        "Nothing is missing from season {0}.",
        "No falta nada de la temporada {0}.");

    public string FaltantesNadaAnio => Idioma.Elegir(
        "Nothing is missing from {0}.",
        "No falta nada de {0}.");

    public string FaltantesNadaCatalogo => Idioma.Elegir(
        "Nothing is missing from the whole catalogue.",
        "No falta nada del catálogo entero.");

    public string FaltantesNadaEmpezadas => Idioma.Elegir(
        "Nothing is missing from the seasons you have started.",
        "No falta nada de las temporadas que has empezado.");

    // {0} = cuántas líneas se copiaron (siempre > 0: sin huecos no hay botón).
    public string FaltantesCopiado => Idioma.Elegir("Copied: {0} lines.", "Copiado: {0} líneas.");
    public string FaltantesCopiadoUno => Idioma.Elegir("Copied: {0} line.", "Copiado: {0} línea.");

    // {0} = el mensaje del sistema, que viene en el idioma de Windows.
    public string FaltantesNoSePudoCopiar => Idioma.Elegir("Could not copy: {0}", "No se pudo copiar: {0}");
}
