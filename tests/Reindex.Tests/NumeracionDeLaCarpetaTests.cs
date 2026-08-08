using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Si la carpeta demuestra que <b>su numeración no es la del catálogo</b>, un
/// episodio sacado solo del número del nombre deja de ser una propuesta.
///
/// <para>
/// El motor trata el número del fichero como pista cuando no hay fecha con la
/// que confirmarlo. Eso funciona en carpetas cuya numeración viene del catálogo,
/// y falla entero en las que no: si los ficheros los numeró otro —un canal, una
/// lista de reproducción, una emisión— el número existe en el catálogo pero
/// apunta a otro episodio.
/// </para>
/// <para>
/// Medido en una carpeta real de Crayon Shin-Chan: de los 42 ficheros
/// identificados <b>por su título</b>, 36 caían a desfase −30..−40 y solo 6 a
/// desfase 0 —los ya renombrados—. Y los 17 identificados <b>por el número</b>
/// asumían, los 17, desfase 0. La carpeta decía a gritos que su numeración iba
/// corrida, y esos 17 eran los únicos que no la escuchaban.
/// </para>
/// <para>
/// No se propone el episodio corregido por el desfase: el desfase <b>se desliza</b>
/// —de −30 a −40 en esa misma carpeta— así que corregir sería cambiar un número
/// inventado por otro. Lo que se puede afirmar con lo que hay es que el número no
/// vale, y eso es lo que se dice.
/// </para>
/// </summary>
public static class NumeracionDeLaCarpetaTests
{
    public static void Todas()
    {
        Program.Seccion("Cuando la numeración de la carpeta no es la del catálogo");

        // Reparto real medido: 36 títulos corridos, 6 a cero.
        var corrida = new List<ReindexResolution>();
        for (int i = 0; i < 36; i++) corrida.Add(PorTitulo(indice: 500 + i, ep: 468 + i));   // -32
        for (int i = 0; i < 6; i++) corrida.Add(PorTitulo(indice: 100 + i, ep: 100 + i));    //   0
        Program.Assert(NumeracionDeLaCarpeta.NoCuadra(corrida),
            "36 títulos corridos contra 6 a cero: la numeración no manda");

        // Una carpeta normal: los títulos confirman los números.
        var sana = new List<ReindexResolution>();
        for (int i = 0; i < 20; i++) sana.Add(PorTitulo(indice: 200 + i, ep: 200 + i));
        Program.Assert(!NumeracionDeLaCarpeta.NoCuadra(sana),
            "si los títulos confirman los números, el número sigue valiendo");

        // Una carpeta corrida ENTERA tampoco cuadra... pero eso no es lo mismo:
        // ahí el número sí sirve de pista una vez sabido el desfase. Aun así no se
        // propone a ciegas, por lo mismo: el desfase se desliza.
        var todaCorrida = new List<ReindexResolution>();
        for (int i = 0; i < 20; i++) todaCorrida.Add(PorTitulo(indice: 500 + i, ep: 468 + i));
        Program.Assert(NumeracionDeLaCarpeta.NoCuadra(todaCorrida),
            "una carpeta corrida entera tampoco autoriza el número tal cual");

        // ── Lo que NO puede decidir ──
        // Con cuatro ejemplos no se declara nada: por tres puntos pasa cualquier
        // recta, y aquí el precio de equivocarse es dejar sin propuesta a una
        // carpeta que estaba bien.
        var pocos = new List<ReindexResolution>();
        for (int i = 0; i < 4; i++) pocos.Add(PorTitulo(indice: 500 + i, ep: 468 + i));
        Program.Assert(!NumeracionDeLaCarpeta.NoCuadra(pocos),
            "con cuatro títulos no hay con qué declarar nada");
        Program.Assert(!NumeracionDeLaCarpeta.NoCuadra(new List<ReindexResolution>()),
            "ni con una carpeta vacía");

        // ── La trampa que se comió la regla en una carpeta real ──
        //
        // Un fichero YA RENOMBRADO trae el título del episodio en su propio nombre,
        // y su número lo escribió el renombrador a partir del catálogo. Así que
        // vota «desfase 0» siempre, por construcción — no porque el origen lo
        // numerara así.
        //
        // Medido en la carpeta de Crayon Shin-Chan a mitad de arreglar: 44 votos,
        // 28 desviados y 16 a cero, y los DIECISÉIS a cero eran ficheros ya
        // renombrados. Hacían falta 29,3 para declarar: la regla se apagó por 1,3
        // votos y volvieron las propuestas inventadas.
        //
        // Lo grave es el sentido: cuantos más arreglas, más «pruebas» acumula la
        // app de que la numeración estaba bien. La protección se apagaba sola justo
        // según ibas avanzando.
        var aMedioArreglar = new List<ReindexResolution>();
        for (int i = 0; i < 28; i++) aMedioArreglar.Add(PorTitulo(indice: 500 + i, ep: 468 + i));
        for (int i = 0; i < 16; i++) aMedioArreglar.Add(YaRenombrado(indice: 100 + i, ep: 100 + i));
        Program.Assert(NumeracionDeLaCarpeta.NoCuadra(aMedioArreglar),
            "los ya renombrados no diluyen el voto: la regla sigue puesta");

        // Y los identificados por NÚMERO no votan: son justo los que están en
        // duda. Dejarlos votar seria preguntarle al acusado.
        var soloNumeros = new List<ReindexResolution>();
        for (int i = 0; i < 30; i++) soloNumeros.Add(PorNumero(indice: 500 + i, ep: 500 + i));
        Program.Assert(!NumeracionDeLaCarpeta.NoCuadra(soloNumeros),
            "los identificados por número no votan sobre si el número vale");
    }

    private static ReindexResolution PorTitulo(int indice, int ep) => Fila(indice, ep, ReindexHint.Titulo);
    private static ReindexResolution PorNumero(int indice, int ep) => Fila(indice, ep, ReindexHint.IndiceFechaAprox);

    /// <summary>
    /// Uno ya arreglado: su nombre trae el título del episodio, porque lo escribió
    /// el renombrador a partir del catálogo.
    /// </summary>
    private static ReindexResolution YaRenombrado(int indice, int ep) => new()
    {
        Archivo = SignalExtractor.Extract(
            $@"C:\x\Season 01\Serie - S2004E{indice} - Un titulo cualquiera.mp4", "Season 01"),
        Episodio = Episodio(ep, "Un titulo cualquiera"),
        Hint = ReindexHint.Titulo,
        Score = 1.0,
    };

    private static ReindexResolution Fila(int indice, int ep, ReindexHint hint) => new()
    {
        Archivo = SignalExtractor.Extract($@"C:\x\Season 01\Serie S01E{indice}.mp4", "Season 01"),
        Episodio = Episodio(ep, "Titulo que el nombre no dice"),
        Hint = hint,
        Score = 1.0,
    };

    /// <summary>
    /// Por el JSON, que es como el catálogo calcula sus listas de comparación: un
    /// episodio armado a mano las trae vacías y no se parecería a nada.
    /// </summary>
    private static CatalogEpisode Episodio(int num, string titulo) =>
        ReindexCatalog.Parse(
            $"{{\"esquema\":\"reindex/1.0\",\"serie\":\"Serie\",\"salida\":\"es\"," +
            $"\"episodios\":[{{\"num\":{num},\"titulos\":{{\"es\":[\"{titulo}\"]}}}}]}}")
        .Episodios[0];
}
