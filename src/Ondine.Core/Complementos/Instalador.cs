using System.IO;
using System.IO.Compression;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// Deja un paquete descargado convertido en un complemento instalado.
///
/// <para>
/// La descarga se hace fuera: aquí entran los BYTES ya en memoria. Así todo lo
/// que decide -si el paquete es el prometido, qué se extrae y dónde- se puede
/// probar sin red, que es justo la parte que no puede fallar.
/// </para>
/// </summary>
public static class Instalador
{
    /// <param name="Ok">Si quedó instalado.</param>
    /// <param name="Motivo">Por qué no, si no.</param>
    public sealed record Resultado(bool Ok, string? Motivo);

    /// <summary>
    /// Quita un complemento instalado: borra <c>{carpetaBase}/{id}</c> entera.
    ///
    /// <para>
    /// El <paramref name="id"/> se valida con las MISMAS reglas que al instalar, y
    /// se comprueba sobre la ruta ya resuelta. Es el mismo dato viniendo del mismo
    /// sitio, y aquí lo que está en juego es un borrado recursivo: un id que en
    /// realidad sea <c>..\..lgo</c> no borraría un complemento, borraría lo que
    /// hubiera ahí.
    /// </para>
    /// <para>
    /// Desinstalar lo que no está devuelve <c>false</c>. No es un error que haya
    /// que gritar, pero tampoco un éxito: quien lo pidió esperaba que hubiera algo.
    /// </para>
    /// </summary>
    public static Resultado Desinstalar(string id, string carpetaBase)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            id.Any(c => c is '/' or '\\' or ':') || id.Contains(".."))
            return new(false, string.Format(Textos.Instancia.IndiceIdRaro, id));

        var destino = Path.GetFullPath(Path.Combine(carpetaBase, id));
        var raiz = Path.GetFullPath(carpetaBase) + Path.DirectorySeparatorChar;
        if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
            return new(false, string.Format(Textos.Instancia.IndiceIdRaro, id));

        if (!Directory.Exists(destino))
            return new(false, string.Format(Textos.Instancia.InstaladorNoEstaba, id));

        try
        {
            Directory.Delete(destino, recursive: true);
            return new(true, null);
        }
        catch (Exception ex)
        {
            return new(false, string.Format(Textos.Instancia.InstaladorNoSePudoQuitar, id, ex.Message));
        }
    }

    /// <summary>
    /// Instala en <c>{carpetaBase}/{entrada.Id}</c>.
    /// </summary>
    public static Resultado Instalar(Indice.Entrada entrada, byte[] paquete, string carpetaBase)
    {
        if (entrada.Reparo() is { } malo) return new(false, malo);

        // Lo primero, antes de tocar el disco: ¿es el paquete que el índice
        // prometía? Bajar un ejecutable y correrlo sin comprobarlo es lo que
        // convierte un gestor de complementos en un problema.
        if (!Indice.Cuadra(paquete, entrada.Sha256))
            return new(false, string.Format(Textos.Instancia.InstaladorChecksumNoCuadra, entrada.Id));

        var destino = Path.Combine(carpetaBase, entrada.Id);
        var temporal = destino + ".instalando";

        try
        {
            // Se extrae a un lado y se mueve al final. Si algo falla a mitad, lo
            // que había sigue funcionando: media instalación encima de un
            // complemento que iba bien es peor que no haber intentado nada.
            if (Directory.Exists(temporal)) Directory.Delete(temporal, recursive: true);
            Directory.CreateDirectory(temporal);

            using (var ms = new MemoryStream(paquete))
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                var raiz = Path.GetFullPath(temporal) + Path.DirectorySeparatorChar;

                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue;   // una carpeta

                    // La entrada de un zip puede decir «..\..\algo». Combinarla sin
                    // mirar escribe fuera de la carpeta de destino: es la forma
                    // clásica de que un paquete deje cosas donde no debe, y hay que
                    // comprobarlo sobre la ruta YA resuelta, que es donde se ve.
                    var salida = Path.GetFullPath(Path.Combine(temporal, e.FullName));
                    if (!salida.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
                        return Fallar(temporal, string.Format(
                            Textos.Instancia.InstaladorSaleDeLaCarpeta, e.FullName));

                    Directory.CreateDirectory(Path.GetDirectoryName(salida)!);
                    e.ExtractToFile(salida, overwrite: true);
                }
            }

            // Y el manifiesto se valida DESPUÉS de extraer, con las mismas reglas
            // que uno puesto a mano. Venir de un índice no le da permisos extra.
            var manifiesto = Path.Combine(temporal, "plugin.json");
            if (!File.Exists(manifiesto))
                return Fallar(temporal, Textos.Instancia.InstaladorSinManifiesto);

            var c = Complemento.Leer(manifiesto);
            if (c is null) return Fallar(temporal, Textos.Instancia.ComplementoManifiestoIlegible);

            // Los permisos, ANTES de validar. Un .zip no guarda el bit de ejecución de Unix -y
            // menos uno hecho en Windows-, así que los scripts salen de aquí sin poder
            // ejecutarse: en su sitio, con el contenido bueno, y el sistema contestando
            // «permission denied» al pulsar. Es un fallo que no se parece nada a su causa.
            DarPermisos(temporal, c);

            if (c.Reparo() is { } reparo) return Fallar(temporal, reparo);

            if (Directory.Exists(destino)) Directory.Delete(destino, recursive: true);
            Directory.Move(temporal, destino);
            return new(true, null);
        }
        catch (Exception ex)
        {
            return Fallar(temporal, ex.Message);
        }
    }

    /// <summary>
    /// Deja ejecutables los ficheros del complemento que lo necesitan: su propio programa y
    /// cualquier <c>.sh</c> que traiga. En Windows no hace nada.
    ///
    /// <para>
    /// <b>No se le da el bit a todo lo extraído.</b> Un paquete trae también datos —un .json, un
    /// icono, un README—, y volverlos ejecutables «por si acaso» reparte permiso donde no hace
    /// falta. Los <c>.sh</c> entran enteros porque un script de shell no sirve para otra cosa; los
    /// <c>.py</c> no, porque se abren con el intérprete y ahí el permiso no pinta nada.
    /// </para>
    /// </summary>
    private static void DarPermisos(string carpeta, Complemento c)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            foreach (var f in AQuienDarPermiso(carpeta, c)) Permisos.AsegurarEjecutable(f);
        }
        catch { /* lo que no se pueda tocar se dirá al arrancar, con su mensaje */ }
    }

    /// <summary>
    /// A QUÉ ficheros hay que darles permiso de ejecución. Es la parte que tiene criterio dentro,
    /// así que se deja aparte de la llamada al sistema para poder comprobarla en cualquier sitio:
    /// el reparto en sí solo existe en Unix, y si la decisión viviera ahí dentro solo se
    /// comprobaría corriendo en Linux.
    /// </summary>
    public static IEnumerable<string> AQuienDarPermiso(string carpeta, Complemento c)
    {
        foreach (var sh in Guiones(carpeta))
            yield return sh;

        // Y el suyo, se llame como se llame: un binario compilado sin extensión no lo caza
        // ninguna búsqueda por extensión. Solo si se ejecuta ÉL — si va por intérprete, el
        // programa está fuera de esta carpeta y ahí no se le tocan los permisos a nadie.
        var arranque = c.ComoArrancar();
        if (arranque.Reparo is null && arranque.Antes.Count == 0)
            yield return arranque.Programa;
    }

    /// <summary>
    /// Los <c>.sh</c> de la carpeta, <b>sin entrar por enlaces</b>.
    ///
    /// <para>
    /// <c>Directory.EnumerateFiles</c> con <c>AllDirectories</c> entra por las carpetas
    /// enlazadas. Un complemento que trajera dentro un enlace a otro sitio del disco haría que
    /// esta lista devolviera ficheros de fuera de su carpeta — y a esos se les iba a dar permiso
    /// de ejecución. Que hoy el extractor no cree enlaces al descomprimir no cierra el hueco: un
    /// complemento también se copia a mano, y la carpeta es de quien la tenga.
    /// </para>
    /// <para>
    /// Se recorre a mano por eso: para poder <b>no</b> entrar.
    /// </para>
    /// </summary>
    private static IEnumerable<string> Guiones(string carpeta)
    {
        IEnumerable<string> hijos;
        try { hijos = Directory.EnumerateFileSystemEntries(carpeta); }
        catch { yield break; }

        foreach (var hijo in hijos)
        {
            if (Permisos.EsEnlace(hijo)) continue;

            if (Directory.Exists(hijo))
            {
                foreach (var dentro in Guiones(hijo)) yield return dentro;
            }
            else if (hijo.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
            {
                yield return hijo;
            }
        }
    }

    private static Resultado Fallar(string temporal, string motivo)
    {
        try { if (Directory.Exists(temporal)) Directory.Delete(temporal, recursive: true); } catch { }
        return new(false, motivo);
    }
}
