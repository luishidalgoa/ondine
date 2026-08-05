using System.Diagnostics;
using System.IO;
using System.Threading;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// Ejecuta un complemento y va entregando lo que dice, según lo dice.
///
/// <para>
/// Los mensajes se emiten conforme llegan y no al final. Traer cuarenta vídeos
/// tarda minutos: con una sola respuesta al terminar, la aplicación se queda
/// muda todo ese rato y no hay forma de saber si avanza o se ha colgado.
/// </para>
/// </summary>
public static class Invocador
{
    /// <summary>Listar lo que hay en una fuente, sin descargar nada.</summary>
    public const string ComandoListar = "listar";

    /// <summary>Traer los elementos elegidos a una carpeta.</summary>
    public const string ComandoTraer = "traer";

    /// <summary>
    /// Corre el complemento y devuelve sus mensajes uno a uno.
    /// </summary>
    /// <param name="quien">El complemento, ya validado.</param>
    /// <param name="comando">«listar» o «traer».</param>
    /// <param name="argumentos">Lo que le toque al comando.</param>
    /// <param name="corte">Para poder parar: una descarga larga tiene que poder cancelarse.</param>
    public static async IAsyncEnumerable<Mensaje> CorrerAsync(
        Complemento quien,
        string comando,
        IEnumerable<string> argumentos,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken corte = default)
    {
        var psi = new ProcessStartInfo(quien.RutaEjecutable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Se arranca EN su carpeta: un complemento trae sus cosas al lado y
            // las busca por ruta relativa. Sin esto funciona al desarrollarlo y
            // falla al instalarlo, que es la peor forma de fallar.
            WorkingDirectory = quien.Carpeta,
            // UTF-8 EXPLÍCITO. Sin esto se lee con la página de códigos de la
            // consola de Windows y «máquina» llega como «mÃ¡quina»: el
            // complemento escribe bien y quien lee lo estropea. Y un título con
            // los acentos rotos no casa con nada del catálogo, así que el fallo
            // no se queda en lo feo — se lleva por delante el cotejo.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };

        foreach (var a in quien.Argumentos) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add(comando);
        foreach (var a in argumentos) psi.ArgumentList.Add(a);

        using var proceso = new Process { StartInfo = psi };

        bool arrancó;
        string? falloAlArrancar = null;
        try { arrancó = proceso.Start(); }
        catch (Exception ex) { arrancó = false; falloAlArrancar = ex.Message; }

        if (!arrancó)
        {
            yield return new Mensaje
            {
                Tipo = Mensaje.TipoError,
                MensajeError = string.Format(Textos.Instancia.ComplementoNoArranca,
                    quien.Nombre, falloAlArrancar ?? ""),
            };
            yield break;
        }

        // El error estándar se vacía en paralelo y NO se interpreta. Si no se lee,
        // un complemento hablador llena la tubería y se queda bloqueado escribiendo
        // -parece colgado y en realidad está esperando a que alguien lea-.
        var ruido = Task.Run(() => proceso.StandardError.ReadToEndAsync(), CancellationToken.None);

        try
        {
            while (await proceso.StandardOutput.ReadLineAsync(corte).ConfigureAwait(false) is { } linea)
            {
                var m = Mensaje.Interpretar(linea);
                if (m is not null) yield return m;
            }
        }
        finally
        {
            if (corte.IsCancellationRequested && !proceso.HasExited)
                try { proceso.Kill(entireProcessTree: true); } catch { }

            try { await proceso.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
            try { await ruido.ConfigureAwait(false); } catch { }
        }

        // Un código de salida distinto de cero SIN un mensaje de error propio es
        // el caso peor: el complemento se murió sin explicarse. Se dice, porque
        // si no la lista se queda a medias y parece que eso era todo lo que había.
        if (proceso.ExitCode != 0 && !corte.IsCancellationRequested)
        {
            var suyo = "";
            try { suyo = (await ruido.ConfigureAwait(false) ?? "").Trim(); } catch { }
            yield return new Mensaje
            {
                Tipo = Mensaje.TipoError,
                MensajeError = string.Format(Textos.Instancia.ComplementoSalidaMala,
                    quien.Nombre, proceso.ExitCode,
                    suyo.Length > 300 ? suyo[..300] : suyo),
            };
        }
    }
}
