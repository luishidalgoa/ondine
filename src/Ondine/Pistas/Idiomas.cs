namespace Ondine;

/// <summary>
/// Nombres de idioma a partir del código que traen los ficheros.
///
/// Existe porque «spa», «eng», «por» no le dicen nada a casi nadie: mirando una lista de pistas
/// así no puedes saber qué doblaje es cuál, que es justo lo que necesitas para elegir. Se usa la
/// lista propia y no la del sistema porque los ficheros mezclan ISO 639-1 y 639-2 («es» y «spa»,
/// «pt» y «por»), y hay que entender los dos.
/// </summary>
public static class Idiomas
{
    private static readonly Dictionary<string, string> Nombres = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = "Español",    ["spa"] = "Español",
        ["en"] = "Inglés",     ["eng"] = "Inglés",
        ["pt"] = "Portugués",  ["por"] = "Portugués",
        ["fr"] = "Francés",    ["fre"] = "Francés",  ["fra"] = "Francés",
        ["de"] = "Alemán",     ["ger"] = "Alemán",   ["deu"] = "Alemán",
        ["it"] = "Italiano",   ["ita"] = "Italiano",
        ["ca"] = "Catalán",    ["cat"] = "Catalán",
        ["gl"] = "Gallego",    ["glg"] = "Gallego",
        ["eu"] = "Euskera",    ["baq"] = "Euskera",  ["eus"] = "Euskera",
        ["ja"] = "Japonés",    ["jpn"] = "Japonés",
        ["ko"] = "Coreano",    ["kor"] = "Coreano",
        ["zh"] = "Chino",      ["chi"] = "Chino",    ["zho"] = "Chino",
        ["ru"] = "Ruso",       ["rus"] = "Ruso",
        ["ar"] = "Árabe",      ["ara"] = "Árabe",
        ["nl"] = "Neerlandés", ["dut"] = "Neerlandés", ["nld"] = "Neerlandés",
        ["pl"] = "Polaco",     ["pol"] = "Polaco",
        ["sv"] = "Sueco",      ["swe"] = "Sueco",
        ["da"] = "Danés",      ["dan"] = "Danés",
        ["no"] = "Noruego",    ["nor"] = "Noruego",
        ["fi"] = "Finés",      ["fin"] = "Finés",
        ["tr"] = "Turco",      ["tur"] = "Turco",
        ["cs"] = "Checo",      ["cze"] = "Checo",    ["ces"] = "Checo",
        ["el"] = "Griego",     ["gre"] = "Griego",   ["ell"] = "Griego",
        ["he"] = "Hebreo",     ["heb"] = "Hebreo",
        ["hi"] = "Hindi",      ["hin"] = "Hindi",
        ["th"] = "Tailandés",  ["tha"] = "Tailandés",
        ["hu"] = "Húngaro",    ["hun"] = "Húngaro",
        ["ro"] = "Rumano",     ["rum"] = "Rumano",   ["ron"] = "Rumano",
        ["uk"] = "Ucraniano",  ["ukr"] = "Ucraniano",
        ["lat"] = "Latín",
    };

    /// <summary>
    /// El nombre del idioma, o el código tal cual si no lo conocemos — vale más un «zzz» raro
    /// que perder el dato. Devuelve vacío para «und» y para los que no declaran idioma: no es un
    /// idioma, es la ausencia de uno, y enseñarlo solo mete ruido.
    /// </summary>
    public static string Nombre(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "";
        var c = codigo.Trim();
        if (c.Equals("und", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("unknown", StringComparison.OrdinalIgnoreCase)) return "";
        return Nombres.TryGetValue(c, out var n) ? n : c;
    }
}
