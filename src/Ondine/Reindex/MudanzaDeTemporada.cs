using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Ejecuta el plan de <see cref="PlanDeReordenado"/>: mueve los ficheros a su
/// carpeta de temporada, y guarda lo justo para poder volver atrás.
///
/// <para>
/// Dos reglas que no se negocian: <b>nunca sobrescribe</b> —ni al mover ni al
/// deshacer— y <b>los compañeros viajan con el vídeo</b>. Lo primero porque el
/// plan se calculó antes y el disco pudo cambiar entre medias; lo segundo
/// porque un .mkv que llega a su carpeta sin su .srt es un capítulo que ha
/// perdido los subtítulos sin que nadie lo haya pedido.
/// </para>
/// </summary>
public static class MudanzaDeTemporada
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
    /// Aplica los pasos marcados <see cref="PlanDeReordenado.Porque.Va"/>. El resto
    /// del plan viaja en la lista solo para poder enseñarlo; aquí se ignora.
    /// </summary>
    public static Parte Aplicar(
        IEnumerable<PlanDeReordenado.Paso> plan,
        Action<string>? avanza = null,
        CancellationToken corte = default)
    {
        var parte = new Parte();

        foreach (var paso in plan)
        {
            if (corte.IsCancellationRequested) break;
            if (paso.Motivo != PlanDeReordenado.Porque.Va || paso.Destino is not { } destino) continue;

            var nombre = Path.GetFileName(paso.Origen);
            avanza?.Invoke(nombre);

            // Se vuelve a preguntar por el destino AUNQUE el plan ya lo comprobó:
            // entre simular y aplicar puede haber pasado cualquier cosa, y el
            // error de pisar un fichero no tiene arreglo posterior.
            if (File.Exists(destino) || !File.Exists(paso.Origen))
            {
                parte.Fallidos.Add(paso.Origen);
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
                    paso.Origen, destino, Directory.EnumerateFiles(Path.GetDirectoryName(paso.Origen)!));

                File.Move(paso.Origen, destino);

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

                parte.Movidos.Add(new(paso.Origen, destino, hechos));
            }
            catch
            {
                parte.Fallidos.Add(paso.Origen);
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
