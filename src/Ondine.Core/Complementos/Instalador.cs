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
    /// Hasta dónde se le permite crecer a un paquete al descomprimirse.
    ///
    /// <para>
    /// <b>Bajarlo topado no basta.</b> La descarga está limitada a 80 MB, pero un zip se
    /// descomprime: 80 MB de ceros bien empaquetados son gigabytes en el disco de quien lo
    /// instala. Y no hace falta mala idea para llegar aquí — también lo dispara quien empaquetó
    /// sin querer una carpeta que no tocaba.
    /// </para>
    /// <para>
    /// El cupo se pasa como parámetro, con estos valores por defecto, para poder comprobarlo con
    /// topes pequeños en vez de escribir 250 MB en cada tanda de pruebas.
    /// </para>
    /// </summary>
    /// <param name="MaxFicheros">Cuántos ficheros como mucho. Un complemento no trae miles.</param>
    /// <param name="MaxBytes">Cuánto puede ocupar ya descomprimido, en total.</param>
    public sealed record Cupo(int MaxFicheros = 5_000, long MaxBytes = 250L * 1024 * 1024)
    {
        public static readonly Cupo Normal = new();
    }

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
    public static Resultado Instalar(
        Indice.Entrada entrada, byte[] paquete, string carpetaBase, Cupo? cupo = null)
    {
        if (entrada.Reparo() is { } malo) return new(false, malo);
        cupo ??= Cupo.Normal;

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
                var puestos = 0;
                var escritos = 0L;

                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue;   // una carpeta

                    // NINGUNA entrada puede declararse enlace. Hoy el extractor de .NET la
                    // escribiría como un fichero normal con la ruta dentro —está medido—, así que
                    // esto no tapa un agujero abierto: quita la dependencia de que eso siga siendo
                    // verdad. Un complemento no necesita traer enlaces, y uno que lo intenta está
                    // pidiendo escribir donde no le toca.
                    if (SeDiceEnlace(e))
                        return Fallar(temporal, string.Format(
                            Textos.Instancia.InstaladorEntradaEnlace, e.FullName));

                    if (++puestos > cupo.MaxFicheros)
                        return Fallar(temporal, string.Format(
                            Textos.Instancia.InstaladorDemasiadosFicheros, cupo.MaxFicheros));

                    // La entrada de un zip puede decir «..\..\algo». Combinarla sin
                    // mirar escribe fuera de la carpeta de destino: es la forma
                    // clásica de que un paquete deje cosas donde no debe, y hay que
                    // comprobarlo sobre la ruta YA resuelta, que es donde se ve.
                    var salida = Path.GetFullPath(Path.Combine(temporal, e.FullName));
                    if (!salida.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
                        return Fallar(temporal, string.Format(
                            Textos.Instancia.InstaladorSaleDeLaCarpeta, e.FullName));

                    Directory.CreateDirectory(Path.GetDirectoryName(salida)!);

                    // Se copia a mano contando lo que SE ESCRIBE, y no se suma «e.Length». Esa
                    // cifra la escribe quien hizo el zip: un paquete puede declarar treinta bytes
                    // y traer un gigabyte. Lo único que no se puede falsear es lo que cae al disco.
                    if (!Copiar(e, salida, cupo.MaxBytes - escritos, out var estos))
                        return Fallar(temporal, string.Format(
                            Textos.Instancia.InstaladorDemasiadoAlDescomprimir,
                            cupo.MaxBytes / (1024 * 1024)));

                    escritos += estos;
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
    /// ¿Esta entrada del zip se declara enlace? Se mira el modo de Unix que va en los 16 bits
    /// altos —<c>S_IFLNK</c>— y el atributo de Windows, porque un zip puede venir de cualquiera
    /// de los dos.
    /// </summary>
    private static bool SeDiceEnlace(ZipArchiveEntry e)
    {
        const int TipoUnix = 0xF000;    // la máscara del tipo de fichero
        const int EsEnlace = 0xA000;    // S_IFLNK

        var modoUnix = (e.ExternalAttributes >> 16) & TipoUnix;
        if (modoUnix == EsEnlace) return true;

        return ((FileAttributes)(e.ExternalAttributes & 0xFFFF)).HasFlag(FileAttributes.ReparsePoint);
    }

    /// <summary>
    /// Vuelca una entrada a disco sin pasarse del cupo que queda. Devuelve <c>false</c> —y no deja
    /// el fichero a medias— si se pasa.
    /// </summary>
    private static bool Copiar(ZipArchiveEntry e, string salida, long queda, out long escritos)
    {
        escritos = 0;
        if (queda <= 0) return false;

        var trozo = new byte[81920];
        using (var dentro = e.Open())
        using (var fuera = new FileStream(salida, FileMode.Create, FileAccess.Write))
        {
            int leidos;
            while ((leidos = dentro.Read(trozo, 0, trozo.Length)) > 0)
            {
                escritos += leidos;
                if (escritos > queda) { fuera.Dispose(); return false; }
                fuera.Write(trozo, 0, leidos);
            }
        }
        return true;
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
