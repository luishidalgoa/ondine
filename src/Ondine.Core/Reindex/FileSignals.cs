using System.Text.RegularExpressions;

namespace Ondine.Reindex;

/// <summary>
/// Lo que se puede deducir de UN fichero antes de mirar el catálogo. Todo es opcional:
/// un fichero puede no traer ni fecha, ni número, ni título reconocible.
/// </summary>
public sealed class FileSignals
{
    /// <summary>Ruta completa; también hace de clave del fichero en el lote.</summary>
    public string Path { get; init; } = "";
    /// <summary>Nombre con extensión, tal cual se ve.</summary>
    public string NombreArchivo { get; init; } = "";
    public string Extension { get; init; } = "";
    /// <summary>Carpeta contenedora (solo el nombre), de donde sale la temporada.</summary>
    public string Carpeta { get; init; } = "";

    public DateOnly? Fecha { get; init; }
    /// <summary>Número que YA trae el fichero (puede estar mal: eso es lo que venimos a arreglar).</summary>
    public int? Indice { get; init; }
    /// <summary>Sufijo de sub-segmento de «[438a]»: distingue mitades del mismo episodio.</summary>
    public string? SubSegmento { get; init; }

    /// <summary>Un episodio añadido en un nombre compuesto: el «+1264b» de «[1262+1264b]».</summary>
    public sealed record Anadido(int Num, string Segmento);

    /// <summary>
    /// Los OTROS episodios cuyas historias trae este mismo fichero.
    ///
    /// <para>
    /// Ondine escribe «[1262+1264]» cuando un fichero junta historias de
    /// episodios distintos, y durante un tiempo no supo leer lo que ella misma
    /// escribía: solo veía el 1262. La consecuencia era que la historia del 1264
    /// no contaba como cubierta, y la app decía «te falta» de un episodio que el
    /// usuario tenía delante — en el mismo fichero cuyo nombre lo nombraba.
    /// </para>
    /// </summary>
    public IReadOnlyList<Anadido> TambienEpisodios { get; init; } = Array.Empty<Anadido>();
    public bool Especial { get; init; }
    public int? IndiceEspecial { get; init; }
    public int? Temporada { get; init; }

    /// <summary>Lo que queda del nombre tras quitar fecha, índice y marcas.</summary>
    public string TituloNombre { get; init; } = "";
    /// <summary>Título del metadato del contenedor (MKV/MP4). Novedad frente a los scripts.</summary>
    public string? TituloMeta { get; init; }

    /// <summary>
    /// Trozos del título si el nombre venía multi-segmento (separador ┃ o |). Si solo hay
    /// uno, la lista está vacía: no hay nada que partir.
    /// </summary>
    public IReadOnlyList<string> Segmentos { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Lo que dura el vídeo, si se ha podido saber.
    ///
    /// <para>
    /// No sale de abrir el fichero: la aporta quien escanea, leyéndola de la ficha que
    /// Windows guarda aparte del contenido. Por eso vale también para los que están en
    /// la nube, que es donde más falta hace -ahí abrirlos los descargaría enteros-.
    /// </para>
    /// <para>
    /// <c>null</c> cuando no se sabe, y eso NO es lo mismo que cero: quien la use tiene
    /// que callarse, no concluir.
    /// </para>
    /// </summary>
    public TimeSpan? Duracion { get; init; }

    /// <summary>Identidad estable del fichero para recordar decisiones (§4 de la epic).</summary>
    public string Fingerprint { get; init; } = "";

    /// <summary>Si el fichero no se pudo leer o el nombre no da nada, el motivo.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// Una copia con otro sub-segmento. Existe porque decidir «este fichero es solo la
    /// historia b» ocurre DESPUÉS de extraer las señales, y las señales son inmutables a
    /// propósito: la decisión crea una variante, no reescribe la evidencia.
    /// </summary>
    public FileSignals ConSegmento(string? seg) => new()
    {
        Path = Path, NombreArchivo = NombreArchivo, Extension = Extension, Carpeta = Carpeta,
        Fecha = Fecha, Indice = Indice, SubSegmento = seg, Especial = Especial,
        IndiceEspecial = IndiceEspecial, Temporada = Temporada, TituloNombre = TituloNombre,
        TituloMeta = TituloMeta, Segmentos = Segmentos, Duracion = Duracion, Fingerprint = Fingerprint, Error = Error,
        TambienEpisodios = TambienEpisodios,
    };

    /// <summary>¿Hay algo con lo que identificar? Si no, es ERROR de entrada.</summary>
    public bool TieneSeñales => Indice.HasValue || Fecha.HasValue
                                || !string.IsNullOrWhiteSpace(TituloNombre)
                                || !string.IsNullOrWhiteSpace(TituloMeta);
}

/// <summary>Lee las señales del nombre del fichero. Función pura: mismo nombre ⇒ mismas señales.</summary>
public static partial class SignalExtractor
{
    // «2005-04-22 …» al inicio
    [GeneratedRegex(@"^\s*(\d{4})-(\d{2})-(\d{2})")]
    private static partial Regex RxFecha();

    // «[S]» o «[S12]» — desvía a la rama de especiales
    [GeneratedRegex(@"\[\s*S\s*(\d*)\s*\]", RegexOptions.IgnoreCase)]
    private static partial Regex RxEspecial();

    // «[438]», «[438a]» (una historia) o «[438ac]» (varias): las letras son el sub-segmento.
    // Hasta 3 letras: un episodio no trae tantas historias, y limitarlo evita leer una palabra
    // pegada al número («E02best») como si fueran segmentos.
    // El «(?:\+\d{1,4}[a-z]{0,3})*» es lo que deja pasar «[1262+1264]», que es como
    // la propia app escribe un fichero con historias de episodios distintos. Sin
    // eso el corchete entero no casaba y el numero se leia de otro sitio.
    [GeneratedRegex(@"\[\s*(\d{1,4})\s*([a-z]{0,3})((?:\s*\+\s*\d{1,4}[a-z]{0,3})*)\s*\]",
                    RegexOptions.IgnoreCase)]
    private static partial Regex RxIndiceCorchetes();

    /// <summary>Los «+1264b» de dentro del corchete.</summary>
    [GeneratedRegex(@"\+\s*(\d{1,4})([a-z]{0,3})", RegexOptions.IgnoreCase)]
    private static partial Regex RxAnadido();

    // «S03E12» / «s03e12», «S2017E487b» (una historia) y «S2017E487ac» (varias). Las letras
    // pegadas al número son las historias que trae. Es el formato que ESCRIBE la propia app al
    // marcar «esto es solo la historia b» (o «la a y la c»), así que tiene que saber releerlo:
    // si no, cada pasada deshace la decisión de la anterior. Máximo 3 letras y el \b de después
    // las cierra: así una palabra como «best» pegada al número no se confunde con segmentos.
    // El «(?:\+\d{1,4}[a-z]{0,3})*» del final es la OTRA forma en que la propia app
    // escribe un fichero con historias de episodios distintos: la plantilla con <num>
    // pega el añadido AL NÚMERO, «S2004E9042f+9044», mientras que la de <índice> lo
    // pone entre corchetes, «[1262+1264]». Se escribían las dos y solo se sabía leer
    // una, así que de un fichero como ese solo se veía el 9042 y la app pedía un
    // episodio que estaba dentro de ese mismo fichero.
    [GeneratedRegex(@"\bS(\d{1,4})E(\d{1,4})([a-z]{0,3})((?:\+\d{1,4}[a-z]{0,3})*)\b",
                    RegexOptions.IgnoreCase)]
    private static partial Regex RxSxxExx();

    // «E72» suelto, con sus letras opcionales igual que arriba
    [GeneratedRegex(@"\bE(\d{1,4})([a-z]{0,3})\b", RegexOptions.IgnoreCase)]
    private static partial Regex RxEpisodioE();

    // «4x01», «12x05» — temporada×episodio (formato «NxNN», muy común en descargas). El episodio
    // exige 2-3 cifras para no confundir un «4x4» de un título con una numeración, y las letras
    // finales son las historias, igual que en SxxExx. Reconocerlo quita el prefijo de serie
    // («Bob_Esponja_5x01_…») del título, que si no se quedaba dentro y hundía el parecido.
    // OJO con los límites: el «_» es carácter de palabra, así que \b NO salta entre «_» y una
    // cifra/letra. Los nombres de descarga usan «_» de separador («…Esponja_4x01_…»), de modo
    // que se usan lookarounds que excluyen alfanuméricos (y tratan «_», espacio y borde como
    // frontera) en vez de \b.
    [GeneratedRegex(@"(?<![a-z0-9])(\d{1,2})x(\d{2,3})([a-z]{0,3})(?![a-z0-9])", RegexOptions.IgnoreCase)]
    private static partial Regex RxTemporadaEpisodioX();

    // «[Cap.101]» = temporada 1, episodio 01; «[Cap.205]» = temporada 2, episodio 05.
    // Es una convención real de nombres de descarga. Solo se descompone cuando el prefijo
    // coincide con la temporada de la carpeta: fuera de ese contexto, 101 puede ser de verdad
    // el episodio global 101 y partirlo inventaría información.
    [GeneratedRegex(@"\[\s*Cap(?:(?:itulo|\u00edtulo))?\.?\s*(\d{3,4})\s*\]", RegexOptions.IgnoreCase)]
    private static partial Regex RxCapituloCompacto();

    // Morralla de descarga: la fuente/release al final del nombre («…AMZN WEB-DL x265 1080p…»).
    // No es parte del título y hundía el parecido contra el catálogo. Se corta desde el PRIMER
    // marcador INEQUÍVOCO —nunca una palabra real de un título—. A propósito NO se incluyen
    // marcas ambiguas sueltas como «web» o «dvd», que sí pueden aparecer en un título de verdad.
    // El «_» es carácter de palabra: no se puede cerrar el marcador con \b (fallaría en «AMZN_»).
    // Se cierra con un lookahead que exige un no-alfanumérico (o el final), así «_AMZN_» sí casa.
    private static readonly Regex RxMorralla = new(
        @"[\s_.\-]+(?:AMZN|WEBRip|WEB[-_.]?DL|HDTV|BluRay|BDRip|BRRip|DVDRip|HDRip|x26[45]|h26[45]|HEVC|XviD|(?:2160|1080|720|480)p)(?![a-z0-9]).*$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // «72 Título» — número al principio seguido de separador
    [GeneratedRegex(@"^\s*(\d{1,4})\s*[-–_.\s]+")]
    private static partial Regex RxNumeroInicial();

    // Carpeta: «Season 2007», «Temporada 3», «2007», «S03»
    [GeneratedRegex(@"^(?:season|temporada|t|s)?\s*_?-?\s*(\d{1,4})\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RxCarpetaTemporada();

    // Con adornos detrás: «Season 3 (2011)», «Temporada 2 [1080p]», «S02 - 720p».
    //
    // EXIGE la palabra delante, y ahí está toda la seguridad de esto. Sin ella, «Los 4
    // Fantásticos» pasaría a ser la temporada 4 — y un falso positivo es peor que no
    // detectar nada: no detectar deja el hueco a la vista, detectar mal manda los capítulos
    // a otra carpeta con toda la confianza.
    //
    // El número va pegado a la palabra, así que «Season Finale» no cuela y «Temporada de
    // caza» tampoco.
    [GeneratedRegex(@"^(?:season|temporada|s|t)\s*_?-?\s*(\d{1,4})\b", RegexOptions.IgnoreCase)]
    private static partial Regex RxCarpetaTemporadaConAdornos();

    // «Specials» / «Especiales» = temporada 0. Es la convención de Plex y Jellyfin, que es
    // justo lo que Ondine viene a servir; antes daban null y esos capítulos se quedaban
    // sin temporada.
    [GeneratedRegex(@"^(?:specials?|especiales?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RxCarpetaEspeciales();

    /// <summary>
    /// Separadores de multi-segmento del nombre. Cada web reparte las dos historias de un
    /// capítulo a su manera: «A ┃ B», «A | B», «A + B» o «A - B».
    ///
    /// El guion EXIGE espacios a los lados a propósito: sin ellos, «El súper-guante» se
    /// partiría en dos historias inventadas.
    /// </summary>
    private static readonly Regex RxSeparadorHistorias = new(@"\s*[┃|+]\s*|\s+[-–—]\s+");

    /// <summary>
    /// Extrae las señales de un nombre de fichero. No toca el disco: <paramref name="tituloMeta"/>
    /// lo aporta quien haya leído el contenedor, para que esta función siga siendo testeable.
    /// </summary>
    public static FileSignals Extract(string rutaCompleta, string? carpeta = null, string? tituloMeta = null,
                                      string? fingerprint = null, TimeSpan? duracion = null)
    {
        var nombreArchivo = System.IO.Path.GetFileName(rutaCompleta);
        var ext = System.IO.Path.GetExtension(rutaCompleta);
        var resto = System.IO.Path.GetFileNameWithoutExtension(rutaCompleta) ?? "";

        // Un nombre suelto sin carpeta deja GetDirectoryName en cadena vacía, y DirectoryInfo
        // lanza con eso. Antes reventaba en vez de limitarse a no saber la temporada.
        if (carpeta == null)
        {
            var dir = System.IO.Path.GetDirectoryName(rutaCompleta);
            carpeta = string.IsNullOrEmpty(dir) ? "" : new System.IO.DirectoryInfo(dir).Name;
        }
        var temporadaCarpeta = TemporadaDeCarpeta(carpeta);

        DateOnly? fecha = null;
        int? indice = null, indiceEspecial = null;
        string? subSegmento = null;
        bool especial = false;

        // 1. fecha al inicio — la primera, para que su «22» no se confunda con un índice
        var mFecha = RxFecha().Match(resto);
        if (mFecha.Success)
        {
            if (DateOnly.TryParse($"{mFecha.Groups[1].Value}-{mFecha.Groups[2].Value}-{mFecha.Groups[3].Value}",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d))
                fecha = d;
            resto = resto[mFecha.Length..];
        }

        // 2. especial antes que índice: «[S12]» no debe leerse como número de episodio regular
        var mEsp = RxEspecial().Match(resto);
        if (mEsp.Success)
        {
            especial = true;
            if (int.TryParse(mEsp.Groups[1].Value, out var ne)) indiceEspecial = ne;
            resto = resto.Remove(mEsp.Index, mEsp.Length);
        }

        // 3. índice: SxxExx → corchetes → E72 → número inicial (en orden de fiabilidad).
        //
        // El SxxExx va PRIMERO porque es el único marcador que no admite otra lectura. Los
        // corchetes iban delante y se los llevaba cualquier número entre corchetes del
        // título: hay catálogos que los usan («Cuido de mamá (LA)[30]»), y entonces el
        // nombre que la propia app escribía se releía como el episodio 30. Renombrar y
        // volver a simular tiene que dar lo mismo, o los datos se degradan solos.
        // 3-a. Antes que nada, el nombre COMPUESTO: «[1262+1264]».
        //
        // Va aquí y no más abajo porque el corchete no sobrevive: la limpieza que
        // se aplica al quedarse con lo de después del marcador borra cualquier
        // grupo entre corchetes. Ondine escribe «[1262+1264]» cuando un fichero
        // junta historias de episodios distintos, y durante un tiempo no supo leer
        // lo que ella misma escribía: solo veía el 1262, así que la historia del
        // otro no contaba como cubierta y la app decía «te falta» de algo que
        // estaba en ese mismo fichero, con su nombre delante.
        var anadidos = new List<FileSignals.Anadido>();
        int? indiceDelCompuesto = null;
        var mComp = RxIndiceCorchetes().Match(resto);
        if (mComp.Success && mComp.Groups[3].Value.Length > 0)
        {
            indiceDelCompuesto = int.Parse(mComp.Groups[1].Value);
            if (mComp.Groups[2].Value.Length > 0) subSegmento = mComp.Groups[2].Value.ToLowerInvariant();

            foreach (Match a in RxAnadido().Matches(mComp.Groups[3].Value))
                anadidos.Add(new FileSignals.Anadido(int.Parse(a.Groups[1].Value),
                                                     a.Groups[2].Value.ToLowerInvariant()));

            // Fuera del texto: es numeración, no título. Y así la cadena de abajo
            // no vuelve a tropezar con él.
            resto = resto.Remove(mComp.Index, mComp.Length);
        }

        int? temporadaNombre = null;   // la que declara el propio nombre en «S2012E455»
        var mSE = RxSxxExx().Match(resto);
        if (mSE.Success)
        {
            indice = int.Parse(mSE.Groups[2].Value);
            if (int.TryParse(mSE.Groups[1].Value, out var tNom)) temporadaNombre = tNom;
            if (mSE.Groups[3].Value.Length > 0) subSegmento = mSE.Groups[3].Value.ToLowerInvariant();

            // Los «+9044» pegados al número, si los hay. Solo cuando el corchete no
            // dijo ya lo suyo: si vinieran los dos, manda el corchete, que es el que
            // se leyó antes.
            if (anadidos.Count == 0 && mSE.Groups[4].Value.Length > 0)
                foreach (Match a in RxAnadido().Matches(mSE.Groups[4].Value))
                    anadidos.Add(new FileSignals.Anadido(int.Parse(a.Groups[1].Value),
                                                         a.Groups[2].Value.ToLowerInvariant()));

            resto = TrasElMarcador(resto, mSE.Index, mSE.Length);
        }
        else if (RxTemporadaEpisodioX().Match(resto) is { Success: true } mX)
        {
            // «4x01»: la temporada y el episodio del propio nombre, y el prefijo de serie fuera.
            indice = int.Parse(mX.Groups[2].Value);
            if (int.TryParse(mX.Groups[1].Value, out var tX)) temporadaNombre = tX;
            if (mX.Groups[3].Value.Length > 0) subSegmento = mX.Groups[3].Value.ToLowerInvariant();
            resto = TrasElMarcador(resto, mX.Index, mX.Length);
        }
        else if (RxCapituloCompacto().Match(resto) is { Success: true } mCap)
        {
            var escrito = mCap.Groups[1].Value;
            var prefijo = temporadaCarpeta?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (prefijo is not null && escrito.StartsWith(prefijo, StringComparison.Ordinal)
                && escrito.Length == prefijo.Length + 2
                && int.TryParse(escrito[prefijo.Length..], out var episodioLocal))
            {
                indice = episodioLocal;
            }
            else
            {
                indice = int.Parse(escrito, System.Globalization.CultureInfo.InvariantCulture);
            }
            resto = resto.Remove(mCap.Index, mCap.Length);
        }
        else
        {
            var mCor = RxIndiceCorchetes().Match(resto);
            if (mCor.Success)
            {
                indice = int.Parse(mCor.Groups[1].Value);
                if (mCor.Groups[2].Value.Length > 0) subSegmento = mCor.Groups[2].Value.ToLowerInvariant();
                resto = resto.Remove(mCor.Index, mCor.Length);
            }
            else
            {
                var mE = RxEpisodioE().Match(resto);
                if (mE.Success)
                {
                    indice = int.Parse(mE.Groups[1].Value);
                    if (mE.Groups[2].Value.Length > 0) subSegmento = mE.Groups[2].Value.ToLowerInvariant();
                    resto = TrasElMarcador(resto, mE.Index, mE.Length);
                }
                else
                {
                    var mNum = RxNumeroInicial().Match(resto);
                    if (mNum.Success)
                    {
                        indice = int.Parse(mNum.Groups[1].Value);
                        resto = resto[mNum.Length..];
                    }
                }
            }
        }

        indice ??= indiceDelCompuesto;

        // 4. temporada: manda la de la carpeta (así está organizada la biblioteca), pero si la
        //    carpeta no la dice —un fichero en una subcarpeta de trabajo tipo «Renombrar»— se
        //    usa la que trae el propio nombre en «S2012E455». Sin esto, un fichero perfecto
        //    perdía su temporada fuera de su carpeta y se confundía con un REMAKE del mismo
        //    título en otro año (caso «El aro de la gratitud»: 574 de 2020 tomado por el 88 de 2007).
        int? temporada = temporadaCarpeta ?? temporadaNombre;

        // 5. lo que queda es el título; los trozos si venía multi-segmento
        var titulo = LimpiarTitulo(resto);
        var segmentos = RxSeparadorHistorias.Split(titulo)
                              .Select(LimpiarTitulo)
                              .Where(s => s.Length > 0)
                              .ToList();
        if (segmentos.Count < 2) segmentos.Clear();   // no había nada que partir

        return new FileSignals
        {
            Path = rutaCompleta,
            NombreArchivo = nombreArchivo,
            Extension = ext,
            Carpeta = carpeta,
            Fecha = fecha,
            Indice = indice,
            SubSegmento = subSegmento,
            Especial = especial,
            IndiceEspecial = indiceEspecial,
            Temporada = temporada,
            TambienEpisodios = anadidos,
            TituloNombre = titulo,
            TituloMeta = string.IsNullOrWhiteSpace(tituloMeta) ? null : tituloMeta.Trim(),
            Segmentos = segmentos,
            Fingerprint = fingerprint ?? rutaCompleta,
            Duracion = duracion,
        };
    }

    /// <summary>
    /// Qué temporada dice el NOMBRE de una carpeta: «Season 2005», «Temporada 3», «S03» o
    /// un año a secas. Null si no dice ninguna («Especiales», «Películas»…).
    ///
    /// Lo usan dos sitios —las señales de un fichero y el orden de la biblioteca— y tienen
    /// que estar de acuerdo: si una reconociera «Season 2005» y la otra no, la tabla
    /// ordenaría por un criterio distinto del que luego identifica.
    /// </summary>
    public static int? TemporadaDeCarpeta(string carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta)) return null;

        var limpio = carpeta.Trim();

        if (RxCarpetaEspeciales().IsMatch(limpio)) return 0;

        // Primero el patrón estricto -el nombre ES la temporada- y solo si no cuadra, el
        // que admite adornos. En ese orden porque «2005» a secas tiene que seguir siendo
        // el año 2005 y no caer en el otro camino.
        var m = RxCarpetaTemporada().Match(limpio);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var t)) return t;

        var mAdornos = RxCarpetaTemporadaConAdornos().Match(limpio);
        if (mAdornos.Success && int.TryParse(mAdornos.Groups[1].Value, out var t2)) return t2;

        var mSE = RxSxxExx().Match(carpeta);
        return mSE.Success ? int.Parse(mSE.Groups[1].Value) : null;
    }

    /// <summary>
    /// El título es lo que va DESPUÉS del marcador de episodio: delante está el nombre de
    /// la serie («Doraemon (2005) - S17E485 - Título»). Quitando solo el marcador quedaba
    /// «Doraemon (2005) - - Título», y ese guion suelto se confundía con el que separa las
    /// dos historias de un capítulo.
    ///
    /// Si detrás no hay nada (el marcador iba al final: «Título S01E02»), se conserva lo de
    /// delante — que entonces sí era el título.
    /// </summary>
    private static string TrasElMarcador(string resto, int indice, int largo)
    {
        var despues = LimpiarTitulo(resto[(indice + largo)..]);
        return despues.Length > 0 ? despues : resto.Remove(indice, largo);
    }

    /// <summary>
    /// Lo mismo que se le hace al título de un episodio: fuera la morralla de
    /// descarga y las etiquetas de fuente. Se abre en público porque las
    /// películas necesitan exactamente esta limpieza, y tener dos listas de
    /// marcadores garantiza que un día una sepa de un formato que la otra no.
    /// </summary>
    public static string SinMorralla(string s) => LimpiarTitulo(s);

    /// <summary>Quita separadores sobrantes de los bordes y espacios repetidos.</summary>
    private static string LimpiarTitulo(string s)
    {
        // La morralla de descarga («…_AMZN_WEB_DLtrialeng…», «…x265_1080p») se corta desde su
        // primer marcador: no es título y restaba parecido contra el catálogo.
        s = RxMorralla.Replace(s, "");
        // Las etiquetas de la fuente («[Boing HD]», «[1080p]») no son parte del título y
        // hundían el parecido: comparadas contra el catálogo restaban puntos por nada.
        s = Regex.Replace(s, @"\[[^\]]*\]", " ");
        s = s.Trim().Trim('-', '–', '_', '.', ' ', '\t');
        return Regex.Replace(s, @"\s{2,}", " ").Trim();
    }
}
