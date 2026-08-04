namespace Ondine.Localizacion;

/// <summary>
/// Textos de los dos diálogos de la aplicación: el generador del encargo para la
/// IA (<c>PromptWindow</c>) y el cuadro de aviso y confirmación
/// (<c>DialogWindow</c>).
/// </summary>
public sealed partial class Textos
{
    // ═══ PromptWindow ═══════════════════════════════════════════════════════
    //
    // «Anexo» es el nombre que le da la Wikipedia en español a los artículos de
    // lista («Anexo:Episodios de…»), y en inglés no existe: allí es un artículo
    // normal. Por eso en inglés se traduce por «source page», que es lo que de
    // verdad se le pide al usuario, y no por el falso amigo «annex».

    // ── Cabecera ────────────────────────────────────────────────────────────
    // Vale para el Title de la ventana y para el rótulo de la barra dibujada a
    // mano: es el mismo texto, así que una sola clave (si fueran dos, se
    // quedarían descuadradas la primera vez que alguien retoque una).
    public string PromptTitulo => Idioma.Elegir(
        "Generate a catalogue with an AI",
        "Generar catálogo con una IA");

    public string PromptExplicacion => Idioma.Elegir(
        "Fill in the details and copy the text. Paste it to an AI that can read web pages (Claude, ChatGPT…) together with the source page address, and it will hand you back the catalogue ready to import. The text already carries the format and the rules inside, so there is no need to explain anything else.",
        "Rellena los datos y copia el texto. Pégaselo a una IA que pueda leer páginas web (Claude, ChatGPT…) junto con la dirección del anexo, y te devolverá el catálogo listo para importar. El texto ya lleva dentro el formato y las reglas, así que no hace falta que le expliques nada más.");

    // ── Campos ──────────────────────────────────────────────────────────────
    public string PromptSerie => Idioma.Elegir("Series", "Serie");

    public string PromptIdiomaSalida => Idioma.Elegir(
        "Language for the file name",
        "Idioma para el nombre del fichero");

    public string PromptIdiomaSalidaAyuda => Idioma.Elegir(
        "The title that will be written when renaming",
        "El título que se escribirá al renombrar");

    public string PromptFuente => Idioma.Elegir("Source page address", "Dirección del anexo");

    public string PromptFuenteAyuda => Idioma.Elegir(
        "The page with the episode list: Wikipedia, Fandom, whichever you use",
        "La página con la lista de episodios: Wikipedia, Fandom, la que uses");

    // ── Idiomas de los ficheros ─────────────────────────────────────────────
    // El rótulo va con TextTrimming: si se alarga, se corta con puntos
    // suspensivos. Las dos versiones tienen que caber en una línea corta.
    public string PromptIdiomasFicheros => Idioma.Elegir(
        "Languages your files are titled in",
        "Idiomas en los que vienen titulados tus ficheros");

    // Tres párrafos separados por línea en blanco, igual que el original. Van en
    // una sola clave porque son un único globo de ayuda, no tres textos.
    public string PromptIdiomasFicherosAyuda => Idioma.Elegir(
        "This is not the language of the final name: that one is the field above.\n\nIt is which languages the title your files already carry today may be written in. The catalogue will store the titles in every one you tick, and identification will compare against all of them.\n\nThe more you tick, the fewer files go unrecognised. What it costs you is a longer catalogue, so tick the ones that really show up in your folder.",
        "No es el idioma del nombre final: ese es el de arriba.\n\nEs en qué idiomas puede estar escrito el título que tus ficheros ya traen hoy. El catálogo guardará los títulos en todos los que marques, y al identificar se comparará contra todos ellos.\n\nCuantos más marques, menos ficheros se quedan sin reconocer. Lo que cuesta es un catálogo más largo, así que marca los que de verdad aparecen en tu carpeta.");

    public string PromptQuitarIdioma => Idioma.Elegir("Remove this language", "Quitar este idioma");

    // El «+» se queda fuera de la traducción: es decoración del botón y va
    // aparte en el XAML. Aquí solo el verbo, que es lo único que se traduce.
    public string PromptAnadirIdioma => Idioma.Elegir("Add", "Añadir");

    public string PromptAnadirIdiomaAyuda => Idioma.Elegir(
        "Search for and add languages",
        "Buscar y añadir idiomas");

    public string PromptBuscarIdiomaAyuda => Idioma.Elegir(
        "Type the name or the code",
        "Escribe el nombre o el código");

    public string PromptSinIdiomas => Idioma.Elegir(
        "No language by that name",
        "Ningún idioma se llama así");

    // ── Acciones ────────────────────────────────────────────────────────────
    public string PromptAbrirFuente => Idioma.Elegir("Open the source page", "Abrir el anexo");

    public string PromptAbrirFuenteAyuda => Idioma.Elegir(
        "Opens the page in your browser so you can check it is the right one",
        "Abre la página en el navegador para comprobar que es la correcta");

    public string PromptCopiar => Idioma.Elegir("Copy the prompt", "Copiar el prompt");

    // ── Avisos del pie ──────────────────────────────────────────────────────
    // {0} = el idioma de salida.
    public string PromptAvisoSinIdiomas => Idioma.Elegir(
        "With none ticked, titles are only compared against {0}, the output language.",
        "Sin ninguno marcado solo se compara contra {0}, el de salida.");

    public string PromptAvisoUnIdioma => Idioma.Elegir(
        "With a single language, only files titled in that language will be recognised.",
        "Con un solo idioma, solo reconocerá los ficheros titulados en ese idioma.");

    // {0} = cuántos idiomas marcados (siempre 2 o más), {1} = el de salida.
    public string PromptAvisoVariosIdiomas => Idioma.Elegir(
        "{0} languages to recognise · the name will be written in {1}",
        "{0} idiomas para reconocer · el nombre se escribirá en {1}");

    public string PromptCopiado => Idioma.Elegir(
        "Copied. Paste it to the AI together with the source page address.",
        "Copiado. Pégaselo a la IA junto con la dirección del anexo.");

    // Estas dos se concatenan con el mensaje de la excepción, que lo escribe
    // .NET en el idioma del sistema y no pasa por aquí. El espacio final es
    // parte del texto: sin él se pega al mensaje.
    public string PromptNoSePudoCopiar => Idioma.Elegir("Could not copy: ", "No se pudo copiar: ");
    public string PromptNoSePudoAbrir => Idioma.Elegir("Could not open: ", "No se pudo abrir: ");

    public string PromptFuenteVacia => Idioma.Elegir(
        "Enter the source page address first.",
        "Escribe primero la dirección del anexo.");

    // ═══ DialogWindow ═══════════════════════════════════════════════════════

    // El Title de la ventana es el nombre de la aplicación: invariante. Va como
    // propiedad y no como literal para que el XAML no tenga texto suelto; el
    // título que se lee en pantalla lo pone el code-behind, no este.
    public string DialogoTituloVentana => Idioma.Elegir("Ondine", "Ondine");

    // Botón único de los avisos. «Got it» y no «OK»: el de Comun ya cubre el OK
    // seco, y aquí el botón es un acuse de recibo, no una aceptación.
    public string DialogoEntendido => Idioma.Elegir("Got it", "Entendido");
}
