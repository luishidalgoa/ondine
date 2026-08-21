namespace Ondine.Localizacion;

/// <summary>
/// La zona de idiomas: el desplegable de salida del encargo, su buscador y el nombre de cada
/// idioma en la etiqueta de una pista.
///
/// <para>
/// Aquí NO están los 183 nombres de idioma, y es a propósito. En inglés los da
/// <c>CultureInfo.EnglishName</c>, que los conoce todos y no depende de la cultura del
/// sistema; en castellano salen de la tabla de <c>IsoLanguages</c>, que ya existía. Copiarlos
/// aquí sería escribir a mano 366 propiedades para no aportar nada, y dejar una lista que se
/// queda coja en cuanto alguien añade un código y solo rellena la mitad.
/// </para>
/// <para>
/// Lo que sí vive aquí es lo poco que ninguna de las dos fuentes puede dar: las dos variantes
/// del español. <c>CultureInfo</c> llama «Spanish» al de España, sin decir de dónde, y eso en
/// una lista donde también está el de Hispanoamérica no distingue nada.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── Las dos variantes del español ───────────────────────────────────────
    // Van con la región dentro y no como «nombre + región» por piezas: en otro idioma el
    // paréntesis podría no ir al final, y armar frases por trozos es justo lo que hace que
    // una traducción suene a máquina.
    public string IdiomaEspanolDeEspana => Idioma.Elegir("Spanish (Spain)", "Español (España)");

    public string IdiomaEspanolDeHispanoamerica =>
        Idioma.Elegir("Spanish (Latin America)", "Español (Hispanoamérica)");
}
