using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>Un episodio del catálogo de referencia.</summary>
public sealed class CatalogEpisode
{
    /// <summary>Número DESTINO: el que irá en el nombre final.</summary>
    [JsonPropertyName("num")] public int Num { get; set; }
    [JsonPropertyName("temporada")] public int? Temporada { get; set; }
    /// <summary>Fecha de emisión ISO, o null (Shin-chan no trae fechas).</summary>
    [JsonPropertyName("fecha")] public string? Fecha { get; set; }
    [JsonPropertyName("especial")] public bool Especial { get; set; }
    [JsonPropertyName("emitido_es")] public bool? EmitidoEs { get; set; }
    /// <summary>
    /// Títulos por idioma. SIEMPRE arrays: un episodio puede tener 2-3 mini-historias
    /// («segmentos»), y cualquiera de ellas identifica al episodio.
    /// </summary>
    [JsonPropertyName("titulos")] public Dictionary<string, List<string>> Titulos { get; set; } = new();
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = new();

    // ---- calculado al cargar, no viene del JSON ----
    [JsonIgnore] public DateOnly? FechaParsed { get; private set; }

    /// <summary>
    /// Títulos del idioma de SALIDA: los que se escriben en el nombre del fichero.
    /// Se separan de los comparables a propósito — puedes querer el nombre en español
    /// aunque el fichero venga titulado en inglés.
    /// </summary>
    [JsonIgnore] public IReadOnlyList<string> TitulosSalida { get; private set; } = Array.Empty<string>();

    /// <summary>Todos los títulos con los que se intenta emparejar, sin normalizar.</summary>
    [JsonIgnore] public IReadOnlyList<string> TitulosComparables { get; private set; } = Array.Empty<string>();

    /// <summary>Los comparables ya normalizados, que es contra lo que mide el motor.</summary>
    [JsonIgnore] public IReadOnlyList<string> TitulosNorm { get; private set; } = Array.Empty<string>();

    internal void Precompute(IdiomasCatalogo idiomas)
    {
        // Mismo formato exacto que exige la validación: si aceptara más de lo que se valida,
        // las dos reglas podrían separarse con el tiempo y el catálogo diría una cosa y el
        // motor entendería otra.
        FechaParsed = DateOnly.TryParseExact(Fecha, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : null;

        static IEnumerable<string> Limpios(IEnumerable<string>? xs) =>
            (xs ?? Enumerable.Empty<string>()).Where(t => !string.IsNullOrWhiteSpace(t));

        // ── el que se escribe ──
        var salida = new List<string>();
        if (Titulos.TryGetValue(idiomas.Salida, out var deSalida)) salida.AddRange(Limpios(deSalida));
        // Si ese idioma falta en este episodio, se tira del primero que haya: mejor un
        // nombre en otro idioma que un «Episodio 437» sin más.
        if (salida.Count == 0)
            foreach (var lista in Titulos.Values) { salida.AddRange(Limpios(lista)); if (salida.Count > 0) break; }
        TitulosSalida = salida;

        // ── los que se comparan ──
        // Por defecto TODOS los idiomas del catálogo. No es temerario: norm() reduce lo que
        // no sea [a-z0-9] a espacios, así que un título en japonés queda en cadena vacía y
        // se descarta solo. El inglés, en cambio, sobrevive — y es justo lo que hace falta
        // cuando el fichero viene titulado en un idioma y lo quieres nombrado en otro.
        var comparables = new List<string>();
        foreach (var (idioma, lista) in Titulos)
            if (idiomas.SeCompara(idioma)) comparables.AddRange(Limpios(lista));
        comparables.AddRange(Limpios(Aliases));

        TitulosComparables = comparables;
        TitulosNorm = comparables.Select(TitleMatch.Norm).Where(s => s.Length > 0).Distinct().ToList();
    }

    /// <summary>
    /// Título preferente para el nombre final. El apaño de «Episodio 437» sigue
    /// al idioma de la app: un episodio sin ningún título se renombra en el idioma
    /// en que estés organizando, así que la misma biblioteca ordenada en inglés y en
    /// castellano no produce el mismo nombre en esos casos. Es el mal menor: dejarlo
    /// fijo en castellano sale por la pantalla de un usuario que no lo lee.
    /// </summary>
    public string TituloPrincipal => TitulosSalida.Count > 0
        ? TitulosSalida[0]
        : string.Format(Textos.Instancia.ReindexEpisodioSinTitulo, Num);

    /// <summary>Todos los segmentos unidos, para mostrar en la propuesta.</summary>
    public string TituloCompleto => TitulosSalida.Count > 1
        ? string.Join(" + ", TitulosSalida.Take(3))
        : TituloPrincipal;
}

/// <summary>
/// Qué idioma se escribe y con cuáles se compara. Son cosas distintas: puedes querer los
/// ficheros nombrados en español aunque te lleguen titulados en inglés, y entonces el
/// inglés hace falta para reconocerlos aunque no se escriba nunca.
/// </summary>
public sealed class IdiomasCatalogo
{
    /// <summary>Idioma del título que acaba en el nombre del fichero.</summary>
    [JsonPropertyName("salida")] public string Salida { get; set; } = "es";

    /// <summary>
    /// Idiomas con los que se intenta emparejar. Vacío o ausente = todos los del catálogo,
    /// que es lo razonable: comparar de más no hace daño (los que no comparten alfabeto se
    /// descartan solos al normalizar) y comparar de menos deja ficheros sin identificar.
    /// </summary>
    [JsonPropertyName("comparar")] public List<string>? Comparar { get; set; }

    public bool SeCompara(string idioma) =>
        Comparar == null || Comparar.Count == 0 ||
        Comparar.Contains(idioma, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Catálogo de referencia de una serie (esquema reindex/1.0).</summary>
public sealed class ReindexCatalog
{
    [JsonPropertyName("esquema")] public string Esquema { get; set; } = "";
    [JsonPropertyName("serie")] public string Serie { get; set; } = "";
    /// <summary>Qué significa «num» en esta serie (oficial, segmento, continuo…).</summary>
    [JsonPropertyName("clave")] public string Clave { get; set; } = "";
    [JsonPropertyName("notas")] public string Notas { get; set; } = "";
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("episodios")] public List<CatalogEpisode> Episodios { get; set; } = new();

    /// <summary>Qué idioma se escribe y con cuáles se compara. Ausente = español de salida
    /// y comparación contra todos los idiomas que traiga el catálogo.</summary>
    [JsonPropertyName("idiomas")] public IdiomasCatalogo? Idiomas { get; set; }

    /// <summary>
    /// Ficheros que ya están bien con el nombre que tienen y no hay que tocar. El caso típico son
    /// los capítulos especiales que SÍ son de la serie pero no salen en ningún anexo —así que no
    /// están en la lista de episodios— y sin esto vuelven a salir como conflicto en cada análisis.
    /// Decidir cien veces lo mismo no es decidir.
    ///
    /// Va en el CATÁLOGO y no en los ajustes de la app a propósito: es una decisión sobre ESTA
    /// serie, así que viaja con ella si te llevas el JSON a otro equipo o se lo pasas a alguien.
    /// </summary>
    [JsonPropertyName("dejar_como_esta")] public List<string> DejarComoEsta { get; set; } = new();

    /// <summary>
    /// ¿Este fichero está en la lista de los que hay que dejar en paz? Acepta la ruta entera —se
    /// compara solo el nombre— y no distingue mayúsculas, porque Windows tampoco.
    /// </summary>
    public bool SeDejaComoEsta(string rutaONombre)
    {
        if (DejarComoEsta.Count == 0 || string.IsNullOrWhiteSpace(rutaONombre)) return false;
        var nombre = SoloNombre(rutaONombre);
        return DejarComoEsta.Any(i => string.Equals(SoloNombre(i), nombre, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// El nombre del fichero, cortando por los DOS separadores.
    ///
    /// No vale <c>Path.GetFileName</c>: en Linux la barra invertida no es un separador, así que
    /// una ruta de Windows se devolvería entera. Y este código corre en Linux y macOS —la CLI es
    /// multiplataforma— con catálogos escritos en Windows.
    /// </summary>
    private static string SoloNombre(string s)
    {
        var t = s.Trim();
        int corte = t.LastIndexOfAny(new[] { '/', '\\' });
        return corte >= 0 ? t[(corte + 1)..] : t;
    }

    /// <summary>
    /// Apunta un fichero en la lista de «déjalo como está» DEL JSON del usuario. Devuelve false si
    /// ya estaba.
    ///
    /// Se edita el árbol JSON en vez de volver a serializar desde el modelo: el catálogo es el
    /// fichero del usuario y el formato promete que los campos que la app no conoce se respetan.
    /// Reescribirlo desde el modelo se llevaría por delante sus notas y sus añadidos sin avisar.
    /// </summary>
    public static bool AnadirADejarComoEsta(string rutaCatalogo, string nombreFichero) =>
        AnadirADejarComoEsta(rutaCatalogo, new[] { nombreFichero }) > 0;

    /// <summary>
    /// Los apunta TODOS con una sola pasada de leer, parsear y escribir. Devuelve
    /// cuántos entraron de verdad (los que ya estaban no cuentan).
    ///
    /// <para>
    /// Existe porque llamar a la versión de uno en uno dentro de un bucle salía
    /// carísimo: medido sobre un catálogo real de 329 KB, cada vuelta son unos
    /// 37 ms de leer + parsear + reescribir, y un lote de dieciséis dejaba la
    /// ventana muerta más de medio segundo.
    /// </para>
    /// <para>
    /// Si no hay nada que apuntar, <b>no se toca el fichero</b>. Escribirlo
    /// igualmente reindentaría el catálogo entero del usuario para nada.
    /// </para>
    /// </summary>
    public static int AnadirADejarComoEsta(string rutaCatalogo, IEnumerable<string> nombres)
    {
        var pedidos = nombres
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(SoloNombre)
            .ToList();
        if (pedidos.Count == 0) return 0;

        var raiz = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(rutaCatalogo))
                   as System.Text.Json.Nodes.JsonObject
                   ?? throw new ReindexCatalogException(Textos.Instancia.ReindexCatalogoNoEsObjeto);

        var lista = raiz["dejar_como_esta"] as System.Text.Json.Nodes.JsonArray;
        var actuales = lista?.Select(n => n?.GetValue<string>() ?? "").ToList() ?? new List<string>();
        var yaEstan = actuales.Select(SoloNombre).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int entraron = 0;
        foreach (var n in pedidos)
            if (yaEstan.Add(n)) { actuales.Add(n); entraron++; }

        // Ni una escritura si no cambió nada: la pasada de dieciséis en la que
        // quince ya estaban apuntados no tiene por qué tocar el disco.
        if (entraron == 0) return 0;

        GuardarDejarComoEsta(rutaCatalogo, raiz, actuales);
        return entraron;
    }

    private static void GuardarDejarComoEsta(
        string rutaCatalogo, System.Text.Json.Nodes.JsonObject raiz, List<string> actuales)
    {
        var nueva = new System.Text.Json.Nodes.JsonArray();
        foreach (var x in actuales.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) nueva.Add(x);
        raiz["dejar_como_esta"] = nueva;

        System.IO.File.WriteAllText(rutaCatalogo,
            raiz.ToJsonString(EscrituraDelCatalogo), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Cómo se reescribe el catálogo del usuario. Sin escapar los no-ASCII: por
    /// omisión el serializador convierte «Adiós» en «Adiós», y eso deja el
    /// fichero del usuario ilegible para él con solo haber pulsado un botón.
    /// </summary>
    private static readonly System.Text.Json.JsonSerializerOptions EscrituraDelCatalogo = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Lo saca de la lista, por si te arrepientes. Devuelve false si no estaba.</summary>
    public static bool QuitarDeDejarComoEsta(string rutaCatalogo, string nombreFichero) =>
        TocarDejarComoEsta(rutaCatalogo, nombreFichero, quitar: true);

    private static bool TocarDejarComoEsta(string rutaCatalogo, string nombreFichero, bool quitar)
    {
        if (string.IsNullOrWhiteSpace(nombreFichero)) return false;
        var nombre = SoloNombre(nombreFichero);

        var raiz = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(rutaCatalogo))
                   as System.Text.Json.Nodes.JsonObject
                   ?? throw new ReindexCatalogException(Textos.Instancia.ReindexCatalogoNoEsObjeto);

        var lista = raiz["dejar_como_esta"] as System.Text.Json.Nodes.JsonArray;
        var actuales = lista?.Select(n => n?.GetValue<string>() ?? "").ToList() ?? new List<string>();
        bool estaba = actuales.Any(x => string.Equals(SoloNombre(x), nombre, StringComparison.OrdinalIgnoreCase));
        if (quitar == !estaba) return false;   // quitar lo que no está, o añadir lo que ya está

        if (quitar)
            actuales.RemoveAll(x => string.Equals(SoloNombre(x), nombre, StringComparison.OrdinalIgnoreCase));
        else
            actuales.Add(nombre);

        GuardarDejarComoEsta(rutaCatalogo, raiz, actuales);
        return true;
    }

    /// <summary>La configuración efectiva, con los valores por defecto ya aplicados.</summary>
    [JsonIgnore] public IdiomasCatalogo IdiomasEfectivos => Idiomas ?? new IdiomasCatalogo();

    // ---- índices calculados ----
    [JsonIgnore] private Dictionary<int, CatalogEpisode> _porNum = new();
    [JsonIgnore] public IReadOnlyList<CatalogEpisode> Regulares { get; private set; } = Array.Empty<CatalogEpisode>();
    [JsonIgnore] public IReadOnlyList<CatalogEpisode> Especiales { get; private set; } = Array.Empty<CatalogEpisode>();

    /// <summary>Versión mayor del esquema que esta app sabe leer.</summary>
    public const int EsquemaMayorSoportado = 1;

    /// <summary>Avisos de esta serie que la UI DEBE enseñar antes de identificar nada.</summary>
    [JsonIgnore] public IReadOnlyList<string> Advertencias { get; private set; } = Array.Empty<string>();

    public CatalogEpisode? PorNum(int num) => _porNum.TryGetValue(num, out var e) ? e : null;
    public bool ExisteNum(int num) => _porNum.ContainsKey(num);

    public static ReindexCatalog Load(string path) => Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));

    /// <summary>
    /// Catálogo de ejemplo que la app ofrece como punto de partida. Vive aquí, junto a las
    /// reglas que debe cumplir, y no en la vista: así un test puede comprobar que sigue
    /// siendo válido. Entregar un ejemplo que no importa sería el peor recibimiento posible.
    ///
    /// Cubre las tres formas que más confunden al escribir el primero: un episodio con
    /// varios segmentos, uno simple y un especial.
    /// </summary>
    public const string Ejemplo = """
    {
      "esquema": "reindex/1.0",
      "serie": "Mi serie (2005)",
      "clave": "oficial",
      "notas": "Cambia «serie» por el nombre que quieres que aparezca en los ficheros.",
      "episodios": [
        {
          "num": 1,
          "temporada": 2005,
          "fecha": "2005-04-22",
          "especial": false,
          "titulos": {
            "es": ["Primer episodio", "Segunda historia del mismo episodio"]
          },
          "aliases": []
        },
        {
          "num": 2,
          "temporada": 2005,
          "fecha": "2005-04-29",
          "titulos": { "es": ["Segundo episodio"] }
        },
        {
          "num": 901,
          "temporada": 2005,
          "especial": true,
          "titulos": { "es": ["Especial de Navidad"] }
        }
      ]
    }
    """;

    /// <summary>
    /// Lee un catálogo. Lanza <see cref="ReindexCatalogException"/> si el esquema es de
    /// una versión mayor que no entendemos; los campos desconocidos se ignoran, para que
    /// un catálogo más nuevo con extras siga funcionando.
    /// </summary>
    public static ReindexCatalog Parse(string json)
    {
        ReindexCatalog? cat;
        try
        {
            cat = JsonSerializer.Deserialize(json, ReindexJsonContext.Default.ReindexCatalog);
        }
        catch (JsonException ex)
        {
            throw new ReindexCatalogException(
                string.Format(Textos.Instancia.ReindexCatalogoJsonInvalido, ex.Message));
        }
        if (cat == null) throw new ReindexCatalogException(Textos.Instancia.ReindexCatalogoVacio);

        // «reindex/1.0» → mayor = 1. Una mayor superior traería reglas que no conocemos.
        var esquema = cat.Esquema ?? "";
        if (!esquema.StartsWith("reindex/", StringComparison.OrdinalIgnoreCase))
            throw new ReindexCatalogException(
                string.Format(Textos.Instancia.ReindexCatalogoNoEsCatalogo, esquema));
        var version = esquema["reindex/".Length..];
        int mayor = int.TryParse(version.Split('.')[0], out var m) ? m : -1;
        if (mayor < 0)
            throw new ReindexCatalogException(
                string.Format(Textos.Instancia.ReindexCatalogoEsquemaIrreconocible, esquema));
        if (mayor > EsquemaMayorSoportado)
            throw new ReindexCatalogException(
                string.Format(Textos.Instancia.ReindexCatalogoEsquemaMuyNuevo, esquema, EsquemaMayorSoportado));

        if (string.IsNullOrWhiteSpace(cat.Serie))
            throw new ReindexCatalogException(Textos.Instancia.ReindexCatalogoFaltaSerie);

        if (cat.Episodios.Count == 0)
            throw new ReindexCatalogException(Textos.Instancia.ReindexCatalogoSinEpisodios);

        cat.Validar();
        cat.Index();
        return cat;
    }

    /// <summary>
    /// Comprueba los episodios uno a uno y junta TODOS los fallos antes de rendirse. Un
    /// catálogo escrito a mano suele traer varios errores del mismo tipo; enseñarlos de uno
    /// en uno obliga a importar, corregir y reimportar sin final.
    /// </summary>
    private void Validar()
    {
        var fallos = new List<string>();
        var numerosVistos = new Dictionary<int, int>();   // num → posición donde salió primero

        for (int i = 0; i < Episodios.Count; i++)
        {
            var e = Episodios[i];
            string donde = string.Format(Textos.Instancia.ReindexCatalogoDonde, i + 1);

            if (e.Num < 0)
            {
                fallos.Add(string.Format(Textos.Instancia.ReindexCatalogoNumInvalido, donde, e.Num));
            }
            else if (numerosVistos.TryGetValue(e.Num, out var primera))
            {
                // El índice se construye con «por número», así que un repetido borraría al
                // anterior sin decir nada y perderías un episodio entero.
                fallos.Add(string.Format(Textos.Instancia.ReindexCatalogoNumRepetido, donde, e.Num, primera));
            }
            else numerosVistos[e.Num] = i + 1;

            // El formato que se PARSEA es siempre yyyy-MM-dd; lo que se traduce es
            // cómo se le explica al usuario (AAAA-MM-DD / YYYY-MM-DD).
            if (e.Fecha != null && !DateOnly.TryParseExact(e.Fecha, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
                fallos.Add(string.Format(Textos.Instancia.ReindexCatalogoFechaInvalida, donde, e.Num, e.Fecha));

            if (e.Temporada is < 0)
                fallos.Add(string.Format(Textos.Instancia.ReindexCatalogoTemporadaNegativa,
                    donde, e.Num, e.Temporada));

            if (fallos.Count >= 20)
            {
                fallos.Add(Textos.Instancia.ReindexCatalogoDemasiadosFallos);
                break;
            }
        }

        if (fallos.Count > 0)
            throw new ReindexCatalogException(
                (fallos.Count == 1
                    ? Textos.Instancia.ReindexCatalogoUnProblema
                    : string.Format(Textos.Instancia.ReindexCatalogoVariosProblemas, fallos.Count))
                + "\n\n• " + string.Join("\n• ", fallos));
    }

    private void Index()
    {
        foreach (var e in Episodios) e.Precompute(IdiomasEfectivos);

        // NUNCA iterar 1..Total: la numeración salta valores (56/138/173 en Doraemon 2005)
        _porNum = new Dictionary<int, CatalogEpisode>();
        foreach (var e in Episodios) _porNum[e.Num] = e;

        Regulares = Episodios.Where(e => !e.Especial).ToList();
        Especiales = Episodios.Where(e => e.Especial).ToList();
        Advertencias = ConstruirAdvertencias();
    }

    /// <summary>
    /// Avisos derivados de los datos reales, no solo del texto de «notas»: así el usuario
    /// ve el peligro concreto de SU catálogo aunque las notas se queden cortas.
    /// </summary>
    private List<string> ConstruirAdvertencias()
    {
        var avisos = new List<string>();

        var huecos = HuecosDeNumeracion(8);
        if (huecos.Count > 0)
            avisos.Add(string.Format(Textos.Instancia.ReindexAvisoHuecos, string.Join(", ", huecos)));

        int sinFecha = Episodios.Count(e => e.FechaParsed == null);
        if (sinFecha == Episodios.Count)
            avisos.Add(Textos.Instancia.ReindexAvisoSinNingunaFecha);
        else if (sinFecha > 0)
            avisos.Add(string.Format(sinFecha == 1
                ? Textos.Instancia.ReindexAvisoSinFechaUno
                : Textos.Instancia.ReindexAvisoSinFecha, sinFecha));

        int sinTemporada = Episodios.Count(e => e.Temporada == null);
        if (sinTemporada > 0)
            avisos.Add(string.Format(sinTemporada == 1
                ? Textos.Instancia.ReindexAvisoSinTemporadaUno
                : Textos.Instancia.ReindexAvisoSinTemporada, sinTemporada));

        // Episodios que solo existen en japonés (nunca doblados): el emparejamiento por
        // título no puede alcanzarlos, así que si el fichero tampoco trae número o fecha
        // no hay por dónde cogerlo. Conviene saberlo ANTES, no descubrirlo fila a fila.
        int sinTitulo = Episodios.Count(e => e.TitulosNorm.Count == 0);
        if (sinTitulo > 0)
            avisos.Add(string.Format(sinTitulo == 1
                ? Textos.Instancia.ReindexAvisoSoloJaponesUno
                : Textos.Instancia.ReindexAvisoSoloJapones, sinTitulo));

        // Remakes: mismo título normalizado en episodios distintos y lejanos en numeración
        if (TieneRemakes())
            avisos.Add(Textos.Instancia.ReindexAvisoRemakes);

        if (Especiales.Count > 0)
            avisos.Add(string.Format(Especiales.Count == 1
                ? Textos.Instancia.ReindexAvisoEspecialesUno
                : Textos.Instancia.ReindexAvisoEspeciales, Especiales.Count));

        return avisos;
    }

    /// <summary>Números que faltan dentro del rango: no son huecos reales, son saltos oficiales.</summary>
    public List<int> HuecosDeNumeracion(int maximoAMostrar = int.MaxValue)
    {
        var regulares = Regulares.Select(e => e.Num).Where(n => n > 0).OrderBy(n => n).ToList();
        if (regulares.Count < 2) return new List<int>();
        var faltan = new List<int>();
        for (int n = regulares[0]; n <= regulares[^1] && faltan.Count < maximoAMostrar; n++)
            if (!_porNum.ContainsKey(n)) faltan.Add(n);
        return faltan;
    }

    /// <summary>¿Hay títulos repetidos en episodios distintos? (la trampa del remake)</summary>
    public bool TieneRemakes()
    {
        var vistos = new Dictionary<string, int>();
        foreach (var e in Episodios)
            foreach (var t in e.TitulosNorm)
            {
                if (t.Length < 8) continue;             // títulos muy cortos coinciden por azar
                if (vistos.TryGetValue(t, out var otro) && Math.Abs(otro - e.Num) > 50) return true;
                vistos[t] = e.Num;
            }
        return false;
    }
}

/// <summary>El catálogo no se puede usar, con el motivo en un lenguaje que la UI puede enseñar.</summary>
public sealed class ReindexCatalogException : Exception
{
    public ReindexCatalogException(string mensaje) : base(mensaje) { }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ReindexCatalog))]
internal partial class ReindexJsonContext : JsonSerializerContext { }
