namespace Ondine.Localizacion;

/// <summary>
/// Textos del diálogo de renombrado (<c>RenameWindow</c>): buscar/reemplazar,
/// opciones, ayuda de variables y vista previa.
/// </summary>
public sealed partial class Textos
{
    // ── Cabecera ────────────────────────────────────────────────────────────
    // Sirve para el Title de la ventana y para el rótulo del header: es el mismo
    // texto, así que una sola clave.
    public string RenameTitulo => Idioma.Elegir("Rename output", "Renombrar salida");

    public string RenameSubtitulo => Idioma.Elegir(
        "applies to each video's compressed file as it is processed",
        "se aplica al archivo comprimido de cada vídeo, según se procesa");

    // ── Buscar / Reemplazar ─────────────────────────────────────────────────
    // «Search for» y no «Search»: son dos campos emparejados y en inglés la
    // preposición es la que deja claro que uno es el patrón y el otro el destino.
    public string RenameBuscar => Idioma.Elegir("Search for", "Buscar");
    public string RenameReemplazarPor => Idioma.Elegir("Replace with", "Reemplazar por");

    // ── Opciones ────────────────────────────────────────────────────────────
    public string RenameUsarRegex => Idioma.Elegir("Use regular expressions", "Usar expresiones regulares");

    // El inglés tiene término propio y corto; el castellano necesita la frase larga.
    public string RenameDistinguirMayusculas => Idioma.Elegir("Case sensitive", "Distinguir mayúsculas y minúsculas");

    public string RenameTodasCoincidencias => Idioma.Elegir("Match all occurrences", "Todas las coincidencias");
    public string RenameEnumerar => Idioma.Elegir("Enumerate items", "Enumerar elementos");
    public string RenameAleatorias => Idioma.Elegir("Random strings", "Cadenas aleatorias");

    // ── Aplicar a ───────────────────────────────────────────────────────────
    public string RenameAplicarA => Idioma.Elegir("Apply to", "Aplicar a");
    public string RenameSoloNombre => Idioma.Elegir("Name only", "Solo el nombre");
    public string RenameSoloExtension => Idioma.Elegir("Extension only", "Solo la extensión");
    public string RenameNombreYExtension => Idioma.Elegir("Name and extension", "Nombre y extensión");

    // ── Formato del texto ───────────────────────────────────────────────────
    public string RenameFormatoTexto => Idioma.Elegir("Text formatting", "Formato del texto");
    public string RenameFormatoNinguno => Idioma.Elegir("No change", "Sin cambio");

    // Estas tres se escriben a sí mismas: el texto de la opción ES un ejemplo de
    // lo que hace. La caja de las letras es parte de la traducción, no un descuido.
    public string RenameFormatoMinusculas => Idioma.Elegir("lowercase", "minúsculas");
    public string RenameFormatoMayusculas => Idioma.Elegir("UPPERCASE", "MAYÚSCULAS");
    public string RenameFormatoPrimeraLetra => Idioma.Elegir("First letter of the name", "Primera letra del nombre");
    // «Capitalise» con s: inglés británico.
    public string RenameFormatoCadaPalabra => Idioma.Elegir("Capitalise Each Word", "Mayúscula En Cada Palabra");

    public string RenameAvisoExtension => Idioma.Elegir(
        "Careful: the extension determines the file's real format; changing it does not reconvert the video.",
        "Ojo: la extensión determina el formato real del archivo; cambiarla no reconvierte el vídeo.");

    // ── Ayuda de variables ──────────────────────────────────────────────────
    // Ojo: estas dos citan literalmente el rótulo de su casilla («Enumerate
    // items» / «Random strings»). Si cambia la casilla, cambia también la cita.
    public string RenameVariablesTitulo => Idioma.Elegir("Available variables", "Variables disponibles");

    public string RenameVariablesContadores => Idioma.Elegir(
        "Counters (with \"Enumerate items\"):  ${}   ${start=N}   ${increment=N}   ${padding=N}   ·  combinable: ${padding=4;increment=2;start=10}",
        "Contadores (con «Enumerar elementos»):  ${}   ${start=N}   ${increment=N}   ${padding=N}   ·  combinables: ${padding=4;increment=2;start=10}");

    // Los tokens ($YYYY, $MM…) los lee RenameRule: no se traducen ni se reordenan.
    public string RenameVariablesFecha => Idioma.Elegir(
        "Original file date:  $YYYY $YY $Y  $MMMM $MMM $MM $M  $DDDD $DDD $DD $D  $hh $h  $mm $m  $ss $s  $fff $ff $f",
        "Fecha del original:  $YYYY $YY $Y  $MMMM $MMM $MM $M  $DDDD $DDD $DD $D  $hh $h  $mm $m  $ss $s  $fff $ff $f");

    public string RenameVariablesRegex => Idioma.Elegir(
        "Regex:  capture groups $1 … $9   ·   $$ = a literal dollar sign",
        "Regex:  grupos de captura $1 … $9   ·   $$ = un dólar literal");

    public string RenameVariablesAleatorio => Idioma.Elegir(
        "Random (with \"Random strings\"):  ${rstringalnum=N}  ${rstringalpha=N}  ${rstringdigit=N}  ${ruuidv4}",
        "Aleatorio (con «Cadenas aleatorias»):  ${rstringalnum=N}  ${rstringalpha=N}  ${rstringdigit=N}  ${ruuidv4}");

    // ── Vista previa ────────────────────────────────────────────────────────
    public string RenameVistaPrevia => Idioma.Elegir("PREVIEW", "VISTA PREVIA");
    public string RenameColumnaOriginal => Idioma.Elegir("ORIGINAL", "ORIGINAL");
    public string RenameColumnaNuevo => Idioma.Elegir("NEW NAME", "NUEVO NOMBRE");

    public string RenameSinCambio => Idioma.Elegir("- no change -", "- sin cambio -");
    public string RenameSinVideos => Idioma.Elegir("no videos selected", "no hay vídeos seleccionados");

    // {0} = cuántos se renombran, {1} = cuántos hay. Dos claves porque el
    // castellano concuerda el verbo con {0} («se renombra» / «se renombran») y el
    // inglés no distingue: ahí las dos versiones salen iguales a propósito.
    public string RenameCuenta => Idioma.Elegir("{0} of {1} will be renamed", "{0} de {1} se renombran");
    public string RenameCuentaUno => Idioma.Elegir("{0} of {1} will be renamed", "{0} de {1} se renombra");

    // ── Pie ─────────────────────────────────────────────────────────────────
    public string RenameQuitarRegla => Idioma.Elegir("Remove rule", "Quitar regla");

    public string RenameQuitarReglaAyuda => Idioma.Elegir(
        "Turns renaming off: the output keeps its original name",
        "Desactiva el renombrado: la salida conservará el nombre original");
}
