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

    private static Resultado Fallar(string temporal, string motivo)
    {
        try { if (Directory.Exists(temporal)) Directory.Delete(temporal, recursive: true); } catch { }
        return new(false, motivo);
    }
}
