using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Qué hacer con cada fichero de una carpeta de películas, y qué dejar quieto.
///
/// <para>
/// Es una <b>simulación</b>, como <see cref="PlanDeReordenado"/>: se calcula
/// entero, se enseña, y solo después se aplica.
/// </para>
/// <para>
/// La regla que manda aquí salió de medir una biblioteca real de 75 películas:
/// <b>53 de ellas vivían en carpetas de colección</b> —«Disney» con 26, «Bob
/// Esponja» con 10, «Paco Martínez Soria» con 8, «Torrente» con 5—. La
/// convención de Plex y Jellyfin es una carpeta por película, así que
/// desmontarlas sería «lo correcto» para el escáner… y destruiría la forma en
/// que su dueño mira su biblioteca, que es por estudio y por actor.
/// </para>
/// <para>
/// Así que <b>una carpeta con más de una película no se desmonta</b>. Dentro se
/// limpia el nombre de cada fichero, que es lo que el escáner lee de todas
/// formas, y nada sale de su sitio. Poder marcar a mano qué es una colección y
/// qué no llegará después; mientras tanto, «hay más de una dentro» es la señal
/// disponible y no se equivoca en ninguno de los 75.
/// </para>
/// </summary>
public static class PlanDePeliculas
{
    public enum Porque
    {
        /// <summary>Se coloca en su carpeta canónica.</summary>
        Va,

        /// <summary>Está en una colección: se renombra dentro, sin salir.</summary>
        EnColeccion,

        /// <summary>Ya cumple la convención.</summary>
        YaEsta,

        /// <summary>De su nombre no sale ningún título.</summary>
        SinTitulo,

        /// <summary>En el destino ya hay algo con ese nombre.</summary>
        Ocupado,

        /// <summary>Un tráiler, una escena eliminada… no es la película.</summary>
        EsExtra,
    }

    public sealed record Paso(string Origen, string? Destino, Porque Motivo);

    /// <summary>
    /// Monta el plan. <paramref name="existe"/> se inyecta para poder probar la
    /// decisión sin disco; en la aplicación es <see cref="File.Exists"/>.
    /// </summary>
    public static List<Paso> Montar(
        IEnumerable<string> ficheros,
        string raiz,
        Func<string, bool>? existe = null)
    {
        existe ??= File.Exists;
        var todos = ficheros as IReadOnlyList<string> ?? ficheros.ToList();

        // Cuántas películas hay en cada carpeta. Es lo que distingue una colección
        // de una película que ya tiene su carpeta propia.
        // Los extras NO cuentan: si contaran, una carpeta con una película y tres
        // tráileres pasaría por colección y la película dejaría de colocarse.
        var cuantasPorCarpeta = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in todos)
        {
            if (TituloDePelicula.EsExtra(Path.GetFileName(f))) continue;
            var dir = Path.GetDirectoryName(Path.GetFullPath(f)) ?? "";
            cuantasPorCarpeta[dir] = cuantasPorCarpeta.GetValueOrDefault(dir) + 1;
        }

        var raizPlena = Path.GetFullPath(raiz);
        var plan = new List<Paso>();
        var pedidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var origen in todos)
        {
            // Un extra se queda donde está. Renombrarlo a «Título (Año).ext» lo
            // convierte, a ojos del servidor, en otra versión de la película.
            if (TituloDePelicula.EsExtra(Path.GetFileName(origen)))
            {
                plan.Add(new(origen, null, Porque.EsExtra));
                continue;
            }

            // Para AGRUPAR y COMPARAR se usa la ruta expandida; para CONSTRUIR el
            // destino, la que entró tal cual. Mezclarlas cambia la forma de la
            // ruta —fuera de Windows, «C:lgo» se expande contra el directorio
            // de trabajo— y el destino sale con una raíz que nadie pidió.
            var carpetaPlena = Path.GetDirectoryName(Path.GetFullPath(origen)) ?? raizPlena;
            var carpeta = Path.GetDirectoryName(origen) ?? raiz;
            var enLaRaiz = string.Equals(carpetaPlena, raizPlena, StringComparison.OrdinalIgnoreCase);
            var esColeccion = !enLaRaiz && cuantasPorCarpeta.GetValueOrDefault(carpetaPlena) > 1;

            // La carpeta solo aporta año cuando es de esta película. En una
            // colección el nombre de la carpeta es «Disney», que no dice nada.
            var ficha = TituloDePelicula.Leer(
                Path.GetFileName(origen),
                enLaRaiz || esColeccion ? null : Path.GetFileName(carpeta));

            if (string.IsNullOrWhiteSpace(ficha.Titulo))
            {
                plan.Add(new(origen, null, Porque.SinTitulo));
                continue;
            }

            // Una película partida conserva su parte en el nombre: sin eso las
            // dos mitades dan el mismo canónico y la segunda se queda sin sitio.
            var parte = TituloDePelicula.Parte(Path.GetFileName(origen));
            var nombre = DestinoDePelicula.Nombre(ficha, Path.GetExtension(origen), parte);

            // En una colección el destino es la MISMA carpeta: solo cambia el
            // nombre. Fuera de ella, la carpeta canónica bajo la raíz.
            var destino = esColeccion
                ? Path.Combine(carpeta, nombre)
                : Path.Combine(DestinoDePelicula.Carpeta(raiz, ficha), nombre);

            if (string.Equals(Path.GetFullPath(destino), Path.GetFullPath(origen),
                              StringComparison.OrdinalIgnoreCase))
            {
                plan.Add(new(origen, null, Porque.YaEsta));
                continue;
            }

            // Ni se sobrescribe lo que hay, ni se mandan dos ficheros al mismo
            // sitio dentro del propio plan: el segundo se comería al primero.
            if (existe(destino) || !pedidos.Add(destino))
            {
                plan.Add(new(origen, destino, Porque.Ocupado));
                continue;
            }

            plan.Add(new(origen, destino, esColeccion ? Porque.EnColeccion : Porque.Va));
        }

        return plan;
    }

    /// <summary>Cuántos tienen trabajo de verdad, se muevan o solo se renombren.</summary>
    public static int Cuantos(IEnumerable<Paso> plan)
        => plan.Count(p => p.Motivo is Porque.Va or Porque.EnColeccion);
}
