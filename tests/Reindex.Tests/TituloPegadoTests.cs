using System.Linq;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Un título de metadato que trae varias cosas pegadas se compara también por
/// trozos — y el separador puede ser una tanda de espacios, no solo una barra.
///
/// <para>
/// Medido en dos <c>.nfo</c> de la misma carpeta, del mismo sitio y con la misma
/// forma:
/// </para>
/// <code>
/// Shin chan | ¡Kasukabetti Western! (I) | Episodio 534 en español   -> acertaba
/// Shin chan   ¡Eh, que me voy a la playa...!   Episodio 509 ...     -> NO
/// </code>
/// <para>
/// El primero se partía por la barra y el trozo de en medio casaba al 0,95. El
/// segundo venía con <b>tres espacios</b> en vez de barras, así que se comparaba
/// el churro entero contra «Me voy a la playa con Nanako» — que no se parece— y
/// el motor se caía al número del nombre. El nombre decía 509 y el episodio de
/// verdad era el 477.
/// </para>
/// <para>
/// Lo que se ve desde fuera es peor que un fallo de parecido: el título correcto
/// estaba en el disco, leído y en memoria, y aun así la app proponía otro
/// episodio <b>como si el nombre mandara</b>.
/// </para>
/// </summary>
public static class TituloPegadoTests
{
    public static void Todas()
    {
        Program.Seccion("Un metadato con varias cosas pegadas");

        // Un catálogo mínimo con los episodios de verdad, sin fechas -como el real-.
        var cat = Catalogo(
            (477, "Me voy a la playa con Nanako"),
            (497, "Kasukabetti Western"),
            (509, "Nos regalan unas figuritas"));

        // Con barras ya funcionaba: esto es la red que impide romperlo.
        Program.Assert(
            Resuelve(cat, "Serie S01E534.mp4", "Shin chan | ¡Kasukabetti Western! (I) | Episodio 534 en español") == 497,
            "separado por barras: sale el 497, no el número del nombre");

        // Y esto es lo que fallaba.
        Program.Assert(
            Resuelve(cat, "Serie S01E509.mp4", "Shin chan   ¡Eh, que me voy a la playa con Nanako!   Episodio 509 en español") == 477,
            "separado por espacios: sale el 477, no el 509 del nombre");

        // Un espacio suelto NO separa: partiría cualquier título normal en palabras
        // y entonces «Me voy» casaría con medio catálogo.
        Program.Assert(
            Resuelve(cat, "Serie S01E001.mp4", "Kasukabetti Western") == 497,
            "un título normal con espacios simples sigue comparándose entero");
    }

    /// <summary>
    /// Por el camino de verdad —el JSON— y no montando los objetos a mano: el
    /// catálogo calcula al cargarse las listas contra las que compara, y un
    /// catálogo armado a mano se salta ese paso y probaría otra cosa.
    /// </summary>
    private static ReindexCatalog Catalogo(params (int num, string titulo)[] eps)
    {
        var filas = string.Join(",", eps.Select(e =>
            $"{{\"num\":{e.num},\"titulos\":{{\"es\":[\"{e.titulo}\"]}}}}"));
        return ReindexCatalog.Parse(
            $"{{\"esquema\":\"reindex/1.0\",\"serie\":\"Crayon Shin-Chan\",\"salida\":\"es\",\"episodios\":[{filas}]}}");
    }

    /// <summary>El número de episodio que el motor propone, o -1 si no propone ninguno.</summary>
    private static int Resuelve(ReindexCatalog cat, string nombre, string tituloMeta)
    {
        var señal = SignalExtractor.Extract(@"C:\x\Season 01\" + nombre, "Season 01", tituloMeta);
        var r = ReindexEngine.Resolve(new[] { señal }, cat).Single();
        return r.Episodio?.Num ?? -1;
    }
}
