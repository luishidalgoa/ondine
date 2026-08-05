namespace Ondine.Localizacion;

/// <summary>
/// Textos de los complementos: lo que se enseña al descubrirlos, y sobre todo
/// los motivos por los que uno queda descartado.
///
/// <para>
/// Esos motivos importan más de lo que parece. Un complemento que no aparece y
/// no dice por qué es peor que uno que no está: quien lo instaló se queda
/// mirando una lista vacía sin nada que corregir.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ---- la tienda ----

    public string ComplementosListoTitulo => Idioma.Elegir("Ready", "Todo listo");
    public string ComplementosNingunoTitulo => Idioma.Elegir("Nothing installed", "No hay nada instalado");
    public string ComplementosNadaTitulo => Idioma.Elegir("Nothing there", "Ahí no hay nada");
    public string ComplementosFalloTitulo => Idioma.Elegir("It did not work", "No ha salido");
    public string ComplementosTiendaVaciaTitulo => Idioma.Elegir("Nothing published", "Nada publicado");
    public string ComplementosVolver => Idioma.Elegir("Back", "Volver");
    public string ComplementosActivar => Idioma.Elegir(
        "Turn it off without uninstalling", "Apágalo sin desinstalarlo");

    public string ComplementosInstalados => Idioma.Elegir("Installed", "Instalados");
    public string ComplementosDisponibles => Idioma.Elegir("Available", "Disponibles");

    public string ComplementosInstalar => Idioma.Elegir("Install", "Instalar");
    public string ComplementosYaInstalado => Idioma.Elegir("Installed", "Ya instalado");
    public string ComplementosInstalando => Idioma.Elegir("Installing...", "Instalando...");

    public string ComplementosTiendaVacia => Idioma.Elegir(
        "There is nothing published yet",
        "Todavía no hay nada publicado");

    // {0} = el nombre del complemento.
    public string ComplementosInstalado => Idioma.Elegir(
        "\"{0}\" installed", "«{0}» instalado");

    // {0} = los modos donde sale, ya montados.
    public string ComplementosSaleEn => Idioma.Elegir("Shows up in: {0}", "Sale en: {0}");
    public string ComplementosSaleEnTodo => Idioma.Elegir("Shows up everywhere", "Sale en toda la aplicación");


    public string TiendaIndiceIlegible => Idioma.Elegir(
        "The index could be reached but not understood: either it is not valid or it speaks another contract",
        "El índice se ha podido traer pero no entender: o no es válido o habla otro contrato");

    // {0} = lo que dijo el sistema.
    public string TiendaSinRed => Idioma.Elegir(
        "Could not be downloaded: {0}", "No se ha podido descargar: {0}");

    // {0} = el limite en megas.
    public string TiendaDemasiadoGrande => Idioma.Elegir(
        "The package is over {0} MB and has not been downloaded",
        "El paquete pasa de {0} MB y no se ha descargado");

    // ---- el indice y la instalacion ----

    public string IndiceSinId => Idioma.Elegir(
        "An entry with no id", "Una entrada sin identificador");

    public string IndiceSinPaquete => Idioma.Elegir(
        "An entry with nothing to download", "Una entrada sin nada que descargar");

    // {0} = el identificador tal cual venia.
    public string IndiceIdRaro => Idioma.Elegir(
        "\"{0}\" is not an id, it is a path: it would install outside where it should",
        "«{0}» no es un identificador, es una ruta: instalaría fuera de donde debe");

    public string IndiceSoloHttps => Idioma.Elegir(
        "Packages are only downloaded over HTTPS",
        "Los paquetes solo se descargan por HTTPS");

    public string IndiceSinChecksum => Idioma.Elegir(
        "It brings no sha256, so there is no way to tell the package is the one promised",
        "No trae sha256, así que no hay forma de saber que el paquete es el prometido");

    // {0} = el identificador del complemento.
    public string InstaladorChecksumNoCuadra => Idioma.Elegir(
        "The downloaded package for \"{0}\" is not the one the index promised. Nothing has been installed",
        "El paquete descargado de «{0}» no es el que prometía el índice. No se ha instalado nada");

    // {0} = la entrada del paquete que se salia.
    public string InstaladorSaleDeLaCarpeta => Idioma.Elegir(
        "The package tries to write outside its own folder (\"{0}\"). Nothing has been installed",
        "El paquete intenta escribir fuera de su carpeta («{0}»). No se ha instalado nada");

    public string InstaladorSinManifiesto => Idioma.Elegir(
        "The package brings no plugin.json",
        "El paquete no trae ningún plugin.json");

    // ── la pantalla ─────────────────────────────────────────────────────────

    public string ComplementosTitulo => Idioma.Elegir("Plugins", "Complementos");

    public string ComplementosCual => Idioma.Elegir("Plugin and source", "Complemento y fuente");

    public string ComplementosFuenteAyuda => Idioma.Elegir(
        "What goes in the source box depends on the plugin: a link, a folder, a name. Each one says so in its description.",
        "Lo que va en la casilla de fuente depende del complemento: un enlace, una carpeta, un nombre. Cada uno lo dice en su descripción.");

    public string ComplementosListar => Idioma.Elegir("List", "Listar");
    public string ComplementosListando => Idioma.Elegir("Asking the plugin...", "Preguntándole al complemento...");

    public string ComplementosVacio => Idioma.Elegir(
        "Pick a plugin, put in a source and press List. What comes back gets checked against the catalogue you have open in Organise, so you can see what you are missing.",
        "Elige un complemento, pon una fuente y pulsa Listar. Lo que venga se coteja con el catálogo que tengas abierto en Organizar, para que veas qué te falta.");

    // {0} = la carpeta donde se buscan.
    public string ComplementosNinguno => Idioma.Elegir(
        "No plugins installed. A plugin is a folder with a plugin.json inside, dropped in: {0}",
        "No hay complementos instalados. Un complemento es una carpeta con un plugin.json dentro, puesta en: {0}");

    public string ComplementosNadaEnLaFuente => Idioma.Elegir(
        "The plugin returned nothing for that source",
        "El complemento no ha devuelto nada para esa fuente");

    public string ComplementosDescartados => Idioma.Elegir(
        "INSTALLED BUT NOT USABLE",
        "INSTALADOS PERO SIN PODER USARSE");

    public string ComplementosYaEsta => Idioma.Elegir("you have it", "ya lo tienes");
    // {0} = las letras de las historias que faltan.
    public string ComplementosAMedias => Idioma.Elegir("missing {0}", "te falta {0}");
    public string ComplementosFalta => Idioma.Elegir("missing", "te falta");
    public string ComplementosDesconocido => Idioma.Elegir("not sure", "no se sabe");
    public string ComplementosSinCatalogo => Idioma.Elegir("no catalogue", "sin catálogo");

    public string ComplementosNoEnCatalogo =>
        Idioma.Elegir("not in the catalogue", "no está en el catálogo");

    public string ComplementosHaceFaltaCatalogo => Idioma.Elegir(
        "Open a series catalogue in Organise first. Without it a list is just a list: there is nothing to compare it against.",
        "Abre antes un catálogo de serie en Organizar. Sin él una lista es solo una lista: no hay contra qué compararla.");
    public string ComplementosEpisodio => Idioma.Elegir("episode", "episodio");

    public string ComplementosMarcarLosQueFaltan => Idioma.Elegir(
        "Tick the ones you are missing", "Marcar los que faltan");
    public string ComplementosDesmarcar => Idioma.Elegir("Clear", "Desmarcar");

    // {0} = marcados, {1} = total, {2} = cuántos faltan.
    public string ComplementosResumen => Idioma.Elegir(
        "{0} ticked of {1}  ·  {2} missing",
        "{0} marcados de {1}  ·  te faltan {2}");

    public string ComplementosTraer => Idioma.Elegir("Fetch the ticked ones", "Traer los marcados");

    public string ComplementosDondeDejarlos => Idioma.Elegir(
        "Where should the files go?", "¿Dónde dejo los ficheros?");

    // {0} = cuantos se trajeron.
    public string ComplementosTraidos => Idioma.Elegir(
        "{0} brought over", "{0} traídos");

    // {0} = cuantos, {1} = la carpeta.
    public string ComplementosLlevarAOrganizar => Idioma.Elegir(
        "{0} files are now in:\n{1}\n\nOpen that folder in Organise to identify them against the catalogue?",
        "{0} ficheros están ya en:\n{1}\n\n¿Abro esa carpeta en Organizar para identificarlos contra el catálogo?");

    public string ComplementosTraerPendiente => Idioma.Elegir(
        "Fetching is not wired up yet: where the files land and how they are handed over to Organise is still to be decided. Listing and checking against the catalogue do work.",
        "Traer todavía no está enganchado: falta decidir dónde caen los ficheros y cómo se entregan a Organizar. Listar y cotejar con el catálogo sí funcionan.");

    public string ComplementoSinNombre => Idioma.Elegir(
        "The manifest has no name",
        "El manifiesto no tiene nombre");

    public string ComplementoSinEjecutable => Idioma.Elegir(
        "The manifest does not say which program to run",
        "El manifiesto no dice qué programa hay que ejecutar");

    // {0} = la versión que declara, {1} = la que esta aplicación entiende.
    public string ComplementoContratoAjeno => Idioma.Elegir(
        "It speaks contract version {0} and this version of Ondine speaks {1}",
        "Habla la versión {0} del contrato y esta versión de Ondine habla la {1}");

    // {0} = lo que puso, {1} = los modos que la app conoce.
    public string ComplementoAmbitoDesconocido => Idioma.Elegir(
        "It applies to \"{0}\", which this version does not know. The modes are: {1}",
        "Dice que aplica a «{0}», que esta versión no conoce. Los modos son: {1}");

    // {0} = lo que puso.
    public string ComplementoIntegracionDesconocida => Idioma.Elegir(
        "Its integration is \"{0}\": it has to be \"propia\" or \"nativa\"",
        "Su integración es «{0}»: tiene que ser «propia» o «nativa»");

    public string ComplementoSinCapacidades => Idioma.Elegir(
        "It declares nothing it can do",
        "No declara nada que sepa hacer");

    public string ComplementoEjecutableFuera => Idioma.Elegir(
        "Its program points outside its own folder. A plugin only runs what it brings with it",
        "Su programa apunta fuera de su propia carpeta. Un complemento solo ejecuta lo que trae dentro");

    public string ComplementoManifiestoIlegible => Idioma.Elegir(
        "Its plugin.json cannot be read: it is not valid JSON",
        "Su plugin.json no se puede leer: no es JSON válido");

    // {0} = el nombre del complemento, {1} = lo que dijo el sistema.
    public string ComplementoNoArranca => Idioma.Elegir(
        "\"{0}\" could not be started: {1}",
        "No se ha podido arrancar «{0}»: {1}");

    // {0} = el nombre, {1} = el código de salida, {2} = lo que escribió por su
    // salida de errores, recortado.
    public string ComplementoSalidaMala => Idioma.Elegir(
        "\"{0}\" stopped with code {1} without explaining itself. {2}",
        "«{0}» ha terminado con el código {1} sin explicarse. {2}");

    // {0} = la ruta declarada, tal cual la escribió el manifiesto.
    public string ComplementoEjecutableNoEsta => Idioma.Elegir(
        "Its program is not there: \"{0}\"",
        "Su programa no está: «{0}»");
}
