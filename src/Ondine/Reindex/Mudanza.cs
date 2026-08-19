using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Mueve ficheros de sitio y guarda lo justo para poder deshacerlo.
///
/// <para>
/// Lo comparten el reordenado por temporadas y el de películas. Se extrajo en
/// vez de copiarse porque lo delicado aquí no es mover un fichero: es <b>no
/// pisar nada</b>, <b>llevarse los compañeros</b> —el .srt y la ficha viajan con
/// su vídeo— y <b>poder volver atrás</b>. Dos copias de eso divergen, y la que
/// se quede corta lo hará sobre la biblioteca de alguien.
/// </para>
/// <para>
/// No decide nada: recibe pares origen→destino ya pensados. Quién se mueve y a
/// dónde es cosa de <see cref="PlanDeReordenado"/> y <see cref="PlanDePeliculas"/>.
/// </para>
/// </summary>
public static class Mudanza
{
    public sealed record Hecho(string Origen, string Destino, List<(string De, string A)> Companeros);

    /// <summary>Lo que se movió en una pasada. Es lo que hace falta para deshacerla.</summary>
    public sealed record Parte
    {
        public List<Hecho> Movidos { get; init; } = new();
        public List<string> Fallidos { get; init; } = new();
        /// <summary>Carpetas que no existían y hubo que crear: al deshacer se retiran si quedan vacías.</summary>
        public List<string> CarpetasCreadas { get; init; } = new();
    }

    /// <summary>
    /// Mueve cada par origen→destino. Quien llama decide QUÉ se mueve; aquí solo
    /// se mueve, y se guarda lo justo para poder volver atrás.
    /// </summary>
    public static Parte Aplicar(
        IEnumerable<(string Origen, string Destino)> pares,
        Action<string>? avanza = null,
        CancellationToken corte = default)
    {
        var parte = new Parte();

        foreach (var (origen, destino) in pares)
        {
            if (corte.IsCancellationRequested) break;

            var nombre = Path.GetFileName(origen);
            avanza?.Invoke(nombre);

            // Se vuelve a preguntar por el destino AUNQUE el plan ya lo comprobó:
            // entre simular y aplicar puede haber pasado cualquier cosa, y el
            // error de pisar un fichero no tiene arreglo posterior.
            if (File.Exists(destino) || !File.Exists(origen))
            {
                parte.Fallidos.Add(origen);
                continue;
            }

            var carpeta = Path.GetDirectoryName(destino)!;
            var habiaCarpeta = Directory.Exists(carpeta);

            try
            {
                if (!habiaCarpeta)
                {
                    Directory.CreateDirectory(carpeta);
                    parte.CarpetasCreadas.Add(carpeta);
                }

                var companeros = SidecarPlanner.Planear(
                    origen, destino, Directory.EnumerateFiles(Path.GetDirectoryName(origen)!));

                File.Move(origen, destino);

                // Los compañeros van DESPUÉS del vídeo y uno a uno: si alguno no
                // puede (está abierto en el reproductor, p. ej.), se queda atrás
                // pero el capítulo ya está en su sitio. Al revés —fallar el vídeo
                // con los .srt ya movidos— dejaría el estropicio repartido.
                var hechos = new List<(string, string)>();
                foreach (var (de, a) in companeros)
                {
                    if (File.Exists(a) || !File.Exists(de)) continue;
                    try { File.Move(de, a); hechos.Add((de, a)); } catch { }
                }

                parte.Movidos.Add(new(origen, destino, hechos));
            }
            catch
            {
                parte.Fallidos.Add(origen);
            }
        }

        return parte;
    }

    /// <summary>
    /// Devuelve todo a su sitio exacto. Da cuántos vídeos volvieron.
    ///
    /// <para>
    /// Se recorre al revés para que dos ficheros que se cruzaron vuelvan en el
    /// orden inverso al que salieron.
    /// </para>
    /// </summary>
    public static int Deshacer(Parte parte)
    {
        int vueltos = 0;

        for (int i = parte.Movidos.Count - 1; i >= 0; i--)
        {
            var h = parte.Movidos[i];

            // La misma regla que al mover: si alguien ocupó el hueco, se deja
            // como está. Perder el fichero nuevo por recuperar el viejo no es
            // deshacer, es otro estropicio.
            if (File.Exists(h.Origen) || !File.Exists(h.Destino)) continue;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(h.Origen)!);
                File.Move(h.Destino, h.Origen);
                vueltos++;

                foreach (var (de, a) in h.Companeros)
                {
                    if (File.Exists(de) || !File.Exists(a)) continue;
                    try { File.Move(a, de); } catch { }
                }
            }
            catch { }
        }

        // Solo las que creamos nosotros, y solo si quedaron vacías: borrar una
        // carpeta que ya existía —o que tiene algo dentro— sería destruir lo que
        // no habíamos tocado.
        foreach (var c in parte.CarpetasCreadas)
        {
            try
            {
                if (Directory.Exists(c) && !Directory.EnumerateFileSystemEntries(c).Any())
                    Directory.Delete(c, false);
            }
            catch { }
        }

        return vueltos;
    }
}
