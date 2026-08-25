using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ondine.Reindex;

/// <summary>
/// Papelera PROPIA de la app. Enviar un fichero aquí lo mueve a un almacén gestionado (NO a la
/// Papelera de Windows) y deja DESHACERLO —restaurarlo a su sitio exacto— de forma fiable, sin
/// depender de la Shell ni del idioma del sistema, y sin los problemas de restaurar ficheros de
/// OneDrive de la papelera de la nube. Cuando se acumulan, los más viejos salen a la Papelera del
/// SISTEMA (siguen recuperables ahí) para no llenar el disco. Es la red de seguridad de todos los
/// «Enviar a la Papelera» de la app.
/// </summary>
public static class PapeleraApp
{
    public sealed class Entrada
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("origen")] public string Origen { get; set; } = "";      // dónde estaba
        [JsonPropertyName("guardado")] public string Guardado { get; set; } = "";  // dónde está ahora
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
        [JsonPropertyName("cuando")] public string Cuando { get; set; } = "";
    }

    private static string DirPapelera => Path.Combine(ReindexStore.Raiz, "papelera");
    private static string RutaIndice => Path.Combine(ReindexStore.Raiz, "papelera.json");
    // Disparadores del finalizado a la Papelera del SISTEMA: por cantidad (más de 30 pendientes)
    // y por antigüedad (más de 2 h en la papelera de la app). El tercero, al cerrar, lo hace
    // VaciarAlCerrar. Los tres mandan a la Papelera de Windows, donde siguen recuperables.
    private const int MaxEntradas = 30;
    private static readonly TimeSpan MaxEdad = TimeSpan.FromMinutes(120);
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    /// <summary>
    /// Cómo se mandan los viejos a la papelera del SISTEMA al purgar.
    ///
    /// <para>
    /// <b>Viene puesto de fábrica, y ese es el arreglo.</b> Antes era un hueco que la ventana
    /// principal de WPF rellenaba al arrancar; si nadie lo rellenaba —y la de Avalonia no lo
    /// hacía, porque no existía nada que rellenar— este servicio <b>borraba sin más</b>. Lo
    /// que la app promete que va a la papelera se perdía, en silencio y fuera de Windows.
    /// </para>
    /// <para>
    /// Ahora el valor por defecto es la papelera de verdad, y lo que la app conecta al
    /// arrancar es la variante de Windows dentro de
    /// <see cref="PapeleraDelSistema.EnWindows"/>. Olvidarse de conectar algo ya no puede
    /// costar ficheros: el peor caso es la papelera de freedesktop en un sistema que no la
    /// mira, que es feo pero no borra nada.
    /// </para>
    /// </summary>
    public static Func<string, bool>? EnviarASistema { get; set; } =
        ruta => PapeleraDelSistema.Mandar(ruta);

    private static List<Entrada> LeerIndice()
    {
        try
        {
            return File.Exists(RutaIndice)
                ? JsonSerializer.Deserialize<List<Entrada>>(File.ReadAllText(RutaIndice)) ?? new()
                : new();
        }
        catch { return new(); }
    }

    private static void EscribirIndice(List<Entrada> e)
    {
        Directory.CreateDirectory(ReindexStore.Raiz);
        File.WriteAllText(RutaIndice, JsonSerializer.Serialize(e, Opts));
    }

    /// <summary>¿Hay algo que deshacer?</summary>
    public static bool PuedeDeshacer => LeerIndice().Count > 0;

    /// <summary>Nombre del último fichero enviado, para el aviso de «Deshacer».</summary>
    public static string? UltimoNombre => LeerIndice() is { Count: > 0 } l ? l[^1].Nombre : null;

    /// <summary>Envía un fichero a la papelera de la app. Devuelve su id, o null si no se pudo.</summary>
    public static string? Enviar(string origen)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(origen) || !File.Exists(origen)) return null;
            var id = Guid.NewGuid().ToString("N");
            var carpeta = Path.Combine(DirPapelera, id);
            Directory.CreateDirectory(carpeta);
            var nombre = Path.GetFileName(origen);
            var destino = Path.Combine(carpeta, nombre);
            File.Move(origen, destino);   // mismo volumen = instantáneo; entre volúmenes copia y borra

            var indice = LeerIndice();
            indice.Add(new Entrada { Id = id, Origen = origen, Guardado = destino, Nombre = nombre,
                                     Cuando = DateTime.Now.ToString("o") });
            FinalizarViejosEn(indice);   // trigger por antigüedad
            Purgar(indice);              // trigger por cantidad
            EscribirIndice(indice);
            return id;
        }
        catch { return null; }
    }

    /// <summary>Restaura el ÚLTIMO fichero enviado a su sitio. Devuelve su nombre, o null si no pudo.</summary>
    public static string? DeshacerUltimo()
    {
        var indice = LeerIndice();
        if (indice.Count == 0) return null;
        var e = indice[^1];
        try
        {
            // Si su sitio ya está ocupado, NO se pisa nada: se deja como está y no se deshace.
            if (File.Exists(e.Origen) || !File.Exists(e.Guardado)) return null;
            Directory.CreateDirectory(Path.GetDirectoryName(e.Origen)!);
            File.Move(e.Guardado, e.Origen);
            try { Directory.Delete(Path.GetDirectoryName(e.Guardado)!, false); } catch { }
            indice.RemoveAt(indice.Count - 1);
            EscribirIndice(indice);
            return e.Nombre;
        }
        catch { return null; }
    }

    /// <summary>
    /// Al CERRAR la app: manda todo lo que quede en la papelera propia a la Papelera del SISTEMA
    /// (siguen recuperables ahí) y vacía el almacén. Así no se acumulan ficheros de la app entre
    /// sesiones — el deshacer con Ctrl+Z es de la sesión, y al salir los borrados «se finalizan»
    /// en la Papelera de Windows.
    /// </summary>
    public static void VaciarAlCerrar()
    {
        try
        {
            foreach (var e in LeerIndice()) Finalizar(e);   // trigger por cierre
            if (File.Exists(RutaIndice)) File.Delete(RutaIndice);
            if (Directory.Exists(DirPapelera)) Directory.Delete(DirPapelera, true);
        }
        catch { /* al cerrar, un fallo de limpieza no debe impedir salir */ }
    }

    /// <summary>
    /// Trigger por ANTIGÜEDAD, para llamar periódicamente (p. ej. cada pocos minutos): finaliza a
    /// la Papelera del sistema lo que lleve demasiado tiempo en la papelera de la app, aunque la
    /// app siga abierta. Así no se acumula si te dejas la app puesta días.
    /// </summary>
    public static void FinalizarViejos()
    {
        var indice = LeerIndice();
        int antes = indice.Count;
        FinalizarViejosEn(indice);
        if (indice.Count != antes) EscribirIndice(indice);
    }

    private static void FinalizarViejosEn(List<Entrada> indice)
    {
        for (int i = indice.Count - 1; i >= 0; i--)
        {
            if (!DateTime.TryParse(indice[i].Cuando, out var t) || DateTime.Now - t <= MaxEdad) continue;
            Finalizar(indice[i]);
            indice.RemoveAt(i);
        }
    }

    // Los más viejos salen a la Papelera del sistema (si está conectada) o se borran, para no
    // llenar el disco. Los recientes siguen deshaciéndose al instante.
    private static void Purgar(List<Entrada> indice)
    {
        while (indice.Count > MaxEntradas)
        {
            Finalizar(indice[0]);
            indice.RemoveAt(0);
        }
    }

    /// <summary>
    /// Manda una entrada a la papelera del sistema, o la borra si no se puede. Es lo que hacen
    /// los tres disparadores: cantidad, antigüedad y cierre.
    ///
    /// <para>
    /// <b>EL FICHERO VUELVE A SU SITIO ANTES DE IRSE.</b> Esto mandaba a la papelera del
    /// sistema <c>e.Guardado</c>, que es la ruta DENTRO del almacén de la app
    /// —<c>…/Ondine/papelera/&lt;guid&gt;/x.mkv</c>—, teniendo al lado <c>e.Origen</c>, que es
    /// de donde salió el fichero de verdad.
    /// </para>
    /// <para>
    /// Y la papelera del sistema apunta la procedencia de lo que le dan: en Linux la escribe
    /// en el <c>.trashinfo</c> y en Windows y macOS la guardan la Shell y el Finder. Así que
    /// «Restaurar» devolvía el vídeo <b>a la carpeta interna de la aplicación</b>, que es la
    /// que se vacía al cerrar — recursivamente y sin pasar por ninguna papelera. Recuperar el
    /// fichero era la forma de perderlo.
    /// </para>
    /// <para>
    /// Si el fichero no se puede devolver a su sitio —la carpeta ya no existe, no hay
    /// permiso—, se manda desde donde está: peor procedencia, pero recuperable. Lo que no se
    /// hace nunca es borrar sin haber pasado por la papelera.
    /// </para>
    /// </summary>
    private static void Finalizar(Entrada e)
    {
        var aMandar = DevolverASuSitio(e) ?? e.Guardado;

        if (EnviarASistema?.Invoke(aMandar) != true)
            try { File.Delete(aMandar); } catch { }

        try { Directory.Delete(Path.GetDirectoryName(e.Guardado)!, false); } catch { }
    }

    /// <summary>
    /// Devuelve el fichero a donde estaba, para que la papelera del sistema apunte la
    /// procedencia buena. Devuelve la ruta nueva, o <c>null</c> si no se pudo.
    ///
    /// <para>
    /// Con nombre libre si el sitio está ocupado: entre que el fichero entró en la papelera y
    /// ahora, puede haber otro con su nombre — y pisarlo sería perder uno de verdad, no
    /// mandarlo a la papelera.
    /// </para>
    /// </summary>
    private static string? DevolverASuSitio(Entrada e)
    {
        try
        {
            if (!File.Exists(e.Guardado) || string.IsNullOrWhiteSpace(e.Origen)) return null;

            var carpeta = Path.GetDirectoryName(e.Origen);
            if (string.IsNullOrEmpty(carpeta) || !Directory.Exists(carpeta)) return null;

            var destino = e.Origen;
            if (File.Exists(destino) || Directory.Exists(destino))
            {
                var sinExt = Path.GetFileNameWithoutExtension(e.Origen);
                var ext = Path.GetExtension(e.Origen);
                for (int i = 2; i < 1000; i++)
                {
                    var probar = Path.Combine(carpeta, $"{sinExt} ({i}){ext}");
                    if (!File.Exists(probar) && !Directory.Exists(probar)) { destino = probar; break; }
                }
                if (destino == e.Origen) return null;   // mil con el mismo nombre: no se toca
            }

            File.Move(e.Guardado, destino);
            return destino;
        }
        catch { return null; }
    }
}
