using System.IO.Pipes;
using System.Text;

namespace Ondine;

/// <summary>
/// Que solo haya UNA Ondine abierta, y que la segunda le pase lo suyo a la primera.
///
/// <para>
/// En Windows lo hace un <c>Mutex</c> con nombre. Aquí se hace con un <b>fichero bloqueado</b>
/// por dos motivos: funciona igual en Linux, macOS y Windows, y <b>el sistema lo suelta solo
/// cuando el proceso acaba</b>, también si se cuelga o lo matan. Un «fichero de bloqueo» que
/// se borra al salir deja la app sin poder abrirse tras un fallo; uno bloqueado no.
/// </para>
/// <para>
/// El bloqueo decide quién manda; lo que se cuentan es otra cosa. La que manda escucha en una
/// tubería con nombre, y la que llega después le manda los ficheros con los que la abrieron
/// («Abrir con» en el gestor de archivos) y se cierra. Si no hay ficheros, el aviso va vacío y
/// sirve solo para que la primera salga al frente.
/// </para>
/// <para>
/// La tubería es solo del usuario que la abre (<c>CurrentUserOnly</c>): en un equipo con varias
/// cuentas, otra no puede mandarle rutas a tu Ondine.
/// </para>
/// </summary>
public sealed class InstanciaUnica : IDisposable
{
    private readonly FileStream _candado;
    private readonly string _tuberia;
    private volatile bool _cerrada;

    private InstanciaUnica(FileStream candado, string tuberia)
    {
        _candado = candado;
        _tuberia = tuberia;
    }

    /// <summary>El nombre de la tubería de este usuario.</summary>
    public static string TuberiaDelUsuario => "Ondine.Abrir." + Environment.UserName;

    /// <summary>
    /// Intenta ser la única. Devuelve la instancia que hay que conservar mientras dure la
    /// aplicación, o <c>null</c> si ya hay otra corriendo.
    /// </summary>
    public static InstanciaUnica? Tomar(string rutaDelCandado, string? tuberia = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(rutaDelCandado)!);
            var f = new FileStream(rutaDelCandado, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new InstanciaUnica(f, tuberia ?? TuberiaDelUsuario);
        }
        catch (IOException) { return null; }   // la tiene otra
        // Cualquier otra cosa (sin permisos, carpeta de solo lectura): no se puede garantizar
        // nada, y quedarse sin abrir la app por no poder escribir un candado sería peor que
        // tener dos. Se deja pasar.
        catch { return new InstanciaUnica(null!, tuberia ?? TuberiaDelUsuario); }
    }

    /// <summary>
    /// Le manda esto a la instancia que ya corre. Espera hasta <paramref name="espera"/>: la
    /// primera puede haber tomado el candado y no haber abierto aún la tubería. Devuelve si
    /// llegó.
    /// </summary>
    public static bool Avisar(IReadOnlyList<string> ficheros, TimeSpan espera, string? tuberia = null)
    {
        try
        {
            using var cliente = new NamedPipeClientStream(
                ".", tuberia ?? TuberiaDelUsuario, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            cliente.Connect((int)espera.TotalMilliseconds);
            var datos = Encoding.UTF8.GetBytes(string.Join("\n", ficheros));
            cliente.Write(datos, 0, datos.Length);
            cliente.Flush();
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Empieza a atender a las instancias que lleguen después. <paramref name="alRecibir"/> se
    /// llama en un hilo de fondo con los ficheros que traen —puede ser una lista vacía—, así
    /// que quien lo use tiene que devolverlo a su hilo de interfaz.
    /// </summary>
    public void Escuchar(Action<IReadOnlyList<string>> alRecibir)
    {
        var hilo = new Thread(() =>
        {
            while (!_cerrada)
            {
                try
                {
                    using var servidor = new NamedPipeServerStream(
                        _tuberia, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
                    servidor.WaitForConnection();
                    if (_cerrada) return;

                    using var lector = new StreamReader(servidor, Encoding.UTF8);
                    var ficheros = lector.ReadToEnd()
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .ToList();
                    alRecibir(ficheros);
                }
                catch { if (!_cerrada) Thread.Sleep(200); }   // cliente cortado o app cerrándose
            }
        })
        { IsBackground = true, Name = "instancia-unica" };
        hilo.Start();
    }

    public void Dispose()
    {
        _cerrada = true;
        // Despierta al hilo que espera una conexión para que vea que se cerró.
        try { Avisar([], TimeSpan.FromMilliseconds(200), _tuberia); } catch { }
        _candado?.Dispose();
    }
}
