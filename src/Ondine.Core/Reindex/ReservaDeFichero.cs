using System.Security.Cryptography;
using System.Text;

namespace Ondine;

/// <summary>
/// Reserva exclusiva de un fichero, válida entre procesos.
///
/// <para>
/// La app, la CLI y el servidor MCP pueden estar abiertos a la vez, y los tres pueden comprimir o
/// mandar a la Papelera el mismo vídeo. Sin esto, dos procesos leían y borraban el mismo fichero a la
/// vez. Una reserva es un cerrojo del sistema sobre un fichero de bloqueo que lleva el nombre de la
/// ruta del vídeo: quien la tiene manda sobre ese fichero, y el sistema la suelta solo cuando el proceso
/// acaba, también si se cuelga o lo matan. Es el mismo mecanismo que la instancia única.
/// </para>
/// <para>
/// Dentro de un mismo proceso también se respeta: dos reservas de la misma ruta no se dan a la vez.
/// </para>
/// </summary>
public sealed class ReservaDeFichero : IDisposable
{
    private readonly FileStream _cerrojo;

    private ReservaDeFichero(FileStream cerrojo) => _cerrojo = cerrojo;

    /// <summary>La reserva de <paramref name="ruta"/>, o <c>null</c> si otra copia de Ondine la tiene.</summary>
    public static ReservaDeFichero? Tomar(string ruta)
    {
        try
        {
            var carpeta = Path.Combine(DatosDeUsuario.Raiz, "reservas");
            Directory.CreateDirectory(carpeta);
            var cerrojo = new FileStream(Path.Combine(carpeta, Nombre(ruta)),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new ReservaDeFichero(cerrojo);
        }
        catch (IOException) { return null; }            // la tiene otra copia (o este mismo proceso)
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>
    /// Como <see cref="Tomar"/>, pero espera hasta <paramref name="espera"/> a que se libere. Sirve para la
    /// Papelera, que puede llegar justo cuando el compresor acaba de soltar el fichero.
    /// </summary>
    public static ReservaDeFichero? Esperar(string ruta, TimeSpan espera)
    {
        var limite = DateTime.UtcNow + espera;
        while (true)
        {
            var r = Tomar(ruta);
            if (r is not null) return r;
            if (DateTime.UtcNow >= limite) return null;
            Thread.Sleep(50);
        }
    }

    public void Dispose() => _cerrojo.Dispose();

    /// <summary>
    /// Nombre del fichero de bloqueo para una ruta. Es un resumen de la ruta completa, para que no haya
    /// caracteres raros ni rutas largas. En Windows las mayúsculas no cuentan, como en el propio sistema.
    /// </summary>
    internal static string Nombre(string ruta)
    {
        var completa = Path.GetFullPath(ruta);
        if (OperatingSystem.IsWindows()) completa = completa.ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(completa));
        return Convert.ToHexString(hash, 0, 16).ToLowerInvariant() + ".lock";
    }
}
