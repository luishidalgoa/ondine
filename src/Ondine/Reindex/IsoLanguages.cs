using System.Globalization;
using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>
/// Un idioma de la lista: el código que va en el JSON y cómo se llama en el idioma de la
/// interfaz.
///
/// <para>
/// Dos nombres y no uno. En el desplegable hay que poder distinguir el español de España
/// del de Hispanoamérica, así que ahí el nombre lleva la variante entre paréntesis; pero
/// una pista de vídeo etiquetada «spa» no dice de qué lado del charco viene el doblaje, y
/// escribir «(España)» ahí sería afirmar algo que el fichero no dice. Por eso
/// <see cref="Nombre"/> y <see cref="NombreLlano"/>.
/// </para>
/// </summary>
/// <param name="Ingles">Nombre a secas en inglés, tal cual lo da .NET.</param>
/// <param name="Castellano">Nombre a secas en castellano, de la tabla de aquí abajo.</param>
public sealed record IsoLanguage(string Codigo, string Ingles, string Castellano)
{
    /// <summary>El nombre a secas: «Spanish» · «Español».</summary>
    public string NombreLlano => Idioma.Elegir(Ingles, Castellano);

    /// <summary>
    /// El nombre con su variante cuando la tiene: «Spanish (Spain)» frente a «Spanish
    /// (Latin America)». Son los dos únicos casos, porque son los dos únicos idiomas de la
    /// lista que conviven partidos por región.
    /// </summary>
    public string Nombre => Codigo switch
    {
        "es" => Textos.Instancia.IdiomaEspanolDeEspana,
        "es-419" => Textos.Instancia.IdiomaEspanolDeHispanoamerica,
        _ => NombreLlano,
    };
}

/// <summary>
/// La norma ISO 639-1 entera, con los nombres en los dos idiomas de la interfaz.
///
/// <para>
/// Antes había siete idiomas fijos elegidos a ojo, y dos de ellos ni siquiera eran ISO:
/// «jp» (el código de Japón, no del japonés — el idioma es «ja») y «lat». Un catálogo que
/// los use sigue leyéndose: <see cref="Normalizar"/> los traduce en vez de dejarlos tirados.
/// </para>
/// <para>
/// El castellano se escribe aquí; el inglés lo pone <c>CultureInfo.EnglishName</c>, que
/// cubre los 183 códigos sin depender de la cultura del sistema. Escribir 183 nombres a
/// mano por idioma sería, además de un día de trabajo, una lista que se queda coja en
/// cuanto alguien añade un código y solo rellena la mitad.
/// </para>
/// <para>
/// Y es la ÚNICA lista de nombres de idioma del programa: el selector de pistas también
/// lee de aquí (<see cref="Ondine.Idiomas"/>). Antes había dos, y dos listas se
/// desincronizan; dos listas por dos idiomas serían cuatro.
/// </para>
/// </summary>
public static class IsoLanguages
{
    /// <summary>
    /// Los que salen primero antes de escribir nada. No es un ranking mundial: son los
    /// idiomas en los que llegan los ficheros de una biblioteca de anime en España.
    /// </summary>
    public static readonly string[] Frecuentes =
        { "es", "es-419", "en", "ja", "ca", "gl", "eu", "fr", "it", "de", "pt", "ko", "zh" };

    /// <summary>
    /// Códigos que la app usó antes de pasarse a ISO, los que la gente escribe de memoria y
    /// los de tres letras (ISO 639-2) con los que vienen etiquetadas las pistas de vídeo.
    /// Sin esto, un catálogo con «jp» dejaría de reconocer sus propios títulos.
    /// </summary>
    private static readonly Dictionary<string, string> Alias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jp"] = "ja",        // lo que usaba la app: es el código del país, no del idioma
        ["lat"] = "es-419",   // «español latino»
        ["cast"] = "es",
        ["esp"] = "es",
        ["ing"] = "en",
        ["jpn"] = "ja",       // ISO 639-2, por si alguien lo escribe
        ["spa"] = "es",
        ["eng"] = "en",
        ["cat"] = "ca",
        ["glg"] = "gl",
        ["baq"] = "eu",
        ["eus"] = "eu",
        ["por"] = "pt",
        ["fra"] = "fr",
        ["fre"] = "fr",
        ["deu"] = "de",
        ["ger"] = "de",
        ["ita"] = "it",
        ["kor"] = "ko",
        ["chi"] = "zh",
        ["zho"] = "zh",
        ["rus"] = "ru",
        ["ara"] = "ar",
        // Los que traen las pistas de los ficheros y no estaban aquí. Vinieron del selector
        // de pistas, que tenía su propia tabla: ahora hay una sola.
        ["dut"] = "nl",
        ["nld"] = "nl",
        ["pol"] = "pl",
        ["swe"] = "sv",
        ["dan"] = "da",
        ["nor"] = "no",
        ["fin"] = "fi",
        ["tur"] = "tr",
        ["cze"] = "cs",
        ["ces"] = "cs",
        ["gre"] = "el",
        ["ell"] = "el",
        ["heb"] = "he",
        ["hin"] = "hi",
        ["tha"] = "th",
        ["hun"] = "hu",
        ["rum"] = "ro",
        ["ron"] = "ro",
        ["ukr"] = "uk",
    };

    /// <summary>
    /// ISO 639-1 completa en castellano, más el hispanoamericano, que no está en la norma
    /// pero sí en las bibliotecas. Los nombres van A SECAS, sin región: la variante de los
    /// dos españoles se pone aparte, en <see cref="IsoLanguage.Nombre"/>.
    /// </summary>
    private static readonly (string Codigo, string Castellano)[] EnCastellano =
    {
        new("es",     "Español"),
        new("es-419", "Español"),
        new("en",     "Inglés"),
        new("ja",     "Japonés"),
        new("ca",     "Catalán"),
        new("gl",     "Gallego"),
        new("eu",     "Euskera"),
        new("fr",     "Francés"),
        new("it",     "Italiano"),
        new("de",     "Alemán"),
        new("pt",     "Portugués"),
        new("ko",     "Coreano"),
        new("zh",     "Chino"),
        new("ru",     "Ruso"),
        new("ar",     "Árabe"),
        new("ab",     "Abjasio"),
        new("aa",     "Afar"),
        new("af",     "Afrikáans"),
        new("ak",     "Akan"),
        new("sq",     "Albanés"),
        new("am",     "Amárico"),
        new("an",     "Aragonés"),
        new("hy",     "Armenio"),
        new("as",     "Asamés"),
        new("av",     "Avar"),
        new("ae",     "Avéstico"),
        new("ay",     "Aimara"),
        new("az",     "Azerí"),
        new("bm",     "Bambara"),
        new("ba",     "Baskir"),
        new("be",     "Bielorruso"),
        new("bn",     "Bengalí"),
        new("bh",     "Bhojpuri"),
        new("bi",     "Bislama"),
        new("bs",     "Bosnio"),
        new("br",     "Bretón"),
        new("bg",     "Búlgaro"),
        new("my",     "Birmano"),
        new("ch",     "Chamorro"),
        new("ce",     "Checheno"),
        new("ny",     "Chichewa"),
        new("cu",     "Eslavo eclesiástico"),
        new("cv",     "Chuvasio"),
        new("kw",     "Córnico"),
        new("co",     "Corso"),
        new("cr",     "Cree"),
        new("hr",     "Croata"),
        new("cs",     "Checo"),
        new("da",     "Danés"),
        new("dv",     "Maldivo"),
        new("nl",     "Neerlandés"),
        new("dz",     "Dzongkha"),
        new("eo",     "Esperanto"),
        new("et",     "Estonio"),
        new("ee",     "Ewé"),
        new("fo",     "Feroés"),
        new("fj",     "Fiyiano"),
        new("fi",     "Finés"),
        new("ff",     "Fula"),
        new("ka",     "Georgiano"),
        new("el",     "Griego"),
        new("gn",     "Guaraní"),
        new("gu",     "Guyaratí"),
        new("ht",     "Criollo haitiano"),
        new("ha",     "Hausa"),
        new("he",     "Hebreo"),
        new("hz",     "Herero"),
        new("hi",     "Hindi"),
        new("ho",     "Hiri motu"),
        new("hu",     "Húngaro"),
        new("ia",     "Interlingua"),
        new("id",     "Indonesio"),
        new("ie",     "Interlingue"),
        new("ga",     "Irlandés"),
        new("ig",     "Igbo"),
        new("ik",     "Inupiaq"),
        new("io",     "Ido"),
        new("is",     "Islandés"),
        new("iu",     "Inuktitut"),
        new("jv",     "Javanés"),
        new("kl",     "Groenlandés"),
        new("kn",     "Canarés"),
        new("kr",     "Kanuri"),
        new("ks",     "Cachemiro"),
        new("kk",     "Kazajo"),
        new("km",     "Jemer"),
        new("ki",     "Kikuyu"),
        new("rw",     "Kinyarwanda"),
        new("ky",     "Kirguís"),
        new("kv",     "Komi"),
        new("kg",     "Kongo"),
        new("kj",     "Kuanyama"),
        new("ku",     "Kurdo"),
        new("lo",     "Lao"),
        new("la",     "Latín"),
        new("lv",     "Letón"),
        new("li",     "Limburgués"),
        new("ln",     "Lingala"),
        new("lt",     "Lituano"),
        new("lu",     "Luba-katanga"),
        new("lb",     "Luxemburgués"),
        new("mk",     "Macedonio"),
        new("mg",     "Malgache"),
        new("ms",     "Malayo"),
        new("ml",     "Malayalam"),
        new("mt",     "Maltés"),
        new("gv",     "Manés"),
        new("mi",     "Maorí"),
        new("mr",     "Maratí"),
        new("mh",     "Marshalés"),
        new("mn",     "Mongol"),
        new("na",     "Nauruano"),
        new("nv",     "Navajo"),
        new("nd",     "Ndebele del norte"),
        new("nr",     "Ndebele del sur"),
        new("ng",     "Ndonga"),
        new("ne",     "Nepalí"),
        new("no",     "Noruego"),
        new("nb",     "Noruego bokmål"),
        new("nn",     "Noruego nynorsk"),
        new("ii",     "Yi de Sichuán"),
        new("oc",     "Occitano"),
        new("oj",     "Ojibwa"),
        new("or",     "Oriya"),
        new("om",     "Oromo"),
        new("os",     "Osetio"),
        new("pi",     "Pali"),
        new("pa",     "Panyabí"),
        new("fa",     "Persa"),
        new("pl",     "Polaco"),
        new("ps",     "Pastún"),
        new("qu",     "Quechua"),
        new("rm",     "Romanche"),
        new("ro",     "Rumano"),
        new("rn",     "Kirundi"),
        new("se",     "Sami septentrional"),
        new("sm",     "Samoano"),
        new("sg",     "Sango"),
        new("sa",     "Sánscrito"),
        new("sc",     "Sardo"),
        new("sr",     "Serbio"),
        new("sn",     "Shona"),
        new("sd",     "Sindi"),
        new("si",     "Cingalés"),
        new("sk",     "Eslovaco"),
        new("sl",     "Esloveno"),
        new("so",     "Somalí"),
        new("st",     "Sesotho"),
        new("su",     "Sundanés"),
        new("sw",     "Suajili"),
        new("ss",     "Suazi"),
        new("sv",     "Sueco"),
        new("ta",     "Tamil"),
        new("te",     "Telugu"),
        new("tg",     "Tayiko"),
        new("th",     "Tailandés"),
        new("ti",     "Tigriña"),
        new("bo",     "Tibetano"),
        new("tk",     "Turcomano"),
        new("tl",     "Tagalo"),
        new("tn",     "Setsuana"),
        new("to",     "Tongano"),
        new("tr",     "Turco"),
        new("ts",     "Tsonga"),
        new("tt",     "Tártaro"),
        new("tw",     "Twi"),
        new("ty",     "Tahitiano"),
        new("ug",     "Uigur"),
        new("uk",     "Ucraniano"),
        new("ur",     "Urdu"),
        new("uz",     "Uzbeko"),
        new("ve",     "Venda"),
        new("vi",     "Vietnamita"),
        new("vo",     "Volapük"),
        new("wa",     "Valón"),
        new("cy",     "Galés"),
        new("wo",     "Wólof"),
        new("fy",     "Frisón occidental"),
        new("xh",     "Xhosa"),
        new("yi",     "Yidis"),
        new("yo",     "Yoruba"),
        new("za",     "Zhuang"),
        new("zu",     "Zulú"),
    };

    /// <summary>
    /// Lo poco que .NET no puede dar bien. «es-419» no está en la norma y su nombre inglés
    /// viene ya con la variante dentro («Spanish (Latin America)»), y aquí los nombres van a
    /// secas para poder enseñarlos también en una pista, donde la variante no se sabe.
    /// </summary>
    private static readonly Dictionary<string, string> InglesAMano = new(StringComparer.OrdinalIgnoreCase)
    {
        ["es-419"] = "Spanish",
    };

    /// <summary>
    /// El nombre en inglés, de <c>CultureInfo</c>. <paramref name="respaldo"/> es el nombre
    /// en castellano: si el sistema no conoce el código —o corre sin datos de idiomas, que
    /// es lo que pasa en un contenedor con globalización invariante— vale más enseñar el
    /// nombre en el otro idioma que dejar el hueco o soltar «Invariant Language».
    /// </summary>
    private static string EnIngles(string codigo, string respaldo)
    {
        if (InglesAMano.TryGetValue(codigo, out var aMano)) return aMano;
        try
        {
            var n = CultureInfo.GetCultureInfo(codigo).EnglishName;
            if (n.Length > 0 &&
                !n.Contains("Invariant", StringComparison.OrdinalIgnoreCase) &&
                !n.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
                return n;
        }
        catch (CultureNotFoundException) { }
        return respaldo;
    }

    /// <summary>La lista tal y como se usa: cada código con sus dos nombres.</summary>
    public static readonly IsoLanguage[] Todos =
        EnCastellano.Select(x => new IsoLanguage(x.Codigo, EnIngles(x.Codigo, x.Castellano), x.Castellano))
                    .ToArray();

    private static readonly Dictionary<string, IsoLanguage> PorCodigo =
        Todos.ToDictionary(i => i.Codigo, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Deja un código en su forma ISO: minúsculas y sin los códigos viejos de la app. Uno
    /// que no conocemos se devuelve tal cual — inventarse una traducción sería peor que
    /// enseñar lo que el catálogo trae de verdad.
    /// </summary>
    public static string Normalizar(string? codigo)
    {
        var c = (codigo ?? "").Trim();
        if (c.Length == 0) return "";
        if (Alias.TryGetValue(c, out var ali)) return ali;
        return PorCodigo.TryGetValue(c, out var l) ? l.Codigo : c;
    }

    /// <summary>
    /// Cómo se llama ese idioma, con su variante si la tiene. De uno desconocido, su propio
    /// código.
    /// </summary>
    public static string Nombre(string? codigo)
    {
        var c = Normalizar(codigo);
        return PorCodigo.TryGetValue(c, out var l) ? l.Nombre : c;
    }

    /// <summary>
    /// El nombre a secas, sin región. Es el que va donde el código no dice de qué variante
    /// se trata, como en la etiqueta de una pista de vídeo.
    /// </summary>
    public static string NombreLlano(string? codigo)
    {
        var c = Normalizar(codigo);
        return PorCodigo.TryGetValue(c, out var l) ? l.NombreLlano : c;
    }

    /// <summary>
    /// Busca por código o por nombre, tolerando lo que se escribe de verdad: sin tildes, a
    /// medias y en cualquier caja. Se apoya en la misma normalización que compara títulos,
    /// así que «japones» encuentra «Japonés» igual que allí.
    ///
    /// <para>
    /// Se busca por el nombre en LOS DOS idiomas, no solo en el de la interfaz. Quien tiene
    /// la app en inglés y escribe «japones» está buscando el japonés igual, y no encontrarlo
    /// por eso sería una tontería; al revés, con «japanese» en la app en castellano, lo
    /// mismo.
    /// </para>
    /// <para>
    /// El orden importa más que el filtro: primero el código exacto, luego lo que empieza
    /// por lo escrito y por último lo que lo contiene. Sin eso, escribir «de» sepultaría el
    /// alemán bajo cada nombre que lleve «de» dentro. A igualdad, mandan los de andar por
    /// casa: escribir «españ» tiene que sacar primero el de España, y no el que quede antes
    /// por orden alfabético, que además cambia según el idioma.
    /// </para>
    /// </summary>
    public static IReadOnlyList<IsoLanguage> Buscar(string? consulta, int cuantos = 0)
    {
        var q = (consulta ?? "").Trim();

        if (q.Length == 0)
        {
            var frecuentes = Frecuentes.Where(PorCodigo.ContainsKey).Select(c => PorCodigo[c]).ToList();
            var resto = Todos.Except(frecuentes).OrderBy(i => i.Nombre, StringComparer.CurrentCulture);
            var todo = frecuentes.Concat(resto).ToList();
            return cuantos > 0 ? todo.Take(cuantos).ToList() : todo;
        }

        var qNorm = TitleMatch.Norm(q);
        var qCod = Normalizar(q).ToLowerInvariant();

        int PorNombre(string nombre)
        {
            var nom = TitleMatch.Norm(nombre);
            return nom.StartsWith(qNorm, StringComparison.Ordinal) ? 2
                : qNorm.Length > 0 && nom.Contains(qNorm, StringComparison.Ordinal) ? 3
                : -1;
        }

        var puntuados = new List<(int Nivel, IsoLanguage Idioma)>();
        foreach (var i in Todos)
        {
            var cod = i.Codigo.ToLowerInvariant();

            int nivel =
                cod == qCod || cod == q.ToLowerInvariant() ? 0
                : cod.StartsWith(qCod, StringComparison.Ordinal) ? 1
                : Mejor(PorNombre(i.Castellano), PorNombre(i.Ingles));

            if (nivel >= 0) puntuados.Add((nivel, i));
        }

        var orden = puntuados
            .OrderBy(x => x.Nivel)
            .ThenBy(x => Prioridad(x.Idioma.Codigo))
            .ThenBy(x => x.Idioma.Nombre, StringComparer.CurrentCulture)
            .Select(x => x.Idioma)
            .ToList();

        return cuantos > 0 ? orden.Take(cuantos).ToList() : orden;
    }

    /// <summary>El mejor de dos niveles, contando que -1 es «no encaja».</summary>
    private static int Mejor(int a, int b) =>
        a < 0 ? b : b < 0 ? a : Math.Min(a, b);

    /// <summary>Su sitio entre los frecuentes; el último de todos si no está.</summary>
    private static int Prioridad(string codigo)
    {
        var i = Array.IndexOf(Frecuentes, codigo);
        return i >= 0 ? i : int.MaxValue;
    }
}
