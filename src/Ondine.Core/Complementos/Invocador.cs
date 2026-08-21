using System.Diagnostics;
using System.IO;
using System.Text.Json;
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

    // Sin escapar los no-ASCII: la respuesta del modelo lleva acentos, y por
    // omisión el serializador los manda como «á». Es JSON válido, pero
    // obliga a que el complemento lo deshaga —y el de Python que lo lea con
    // json.loads lo hace, pero uno escrito a mano no tiene por qué—.
    private static readonly JsonSerializerOptions SinEscapar = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static bool EsPorLotes(string ruta) =>
        ruta.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
        ruta.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Un argumento tal y como hay que dárselo a un fichero por lotes.
    ///
    /// <para>
    /// Entre comillas siempre, aunque no tenga espacios: lo que hay que neutralizar
    /// no son los espacios sino «&amp;», «|», «&gt;» y compañía, que fuera de comillas
    /// cmd toma como sintaxis y no como texto. Las comillas de dentro se doblan,
    /// que es como las escapa cmd.
    /// </para>
    /// <para>
    /// Queda un cabo: dentro de comillas cmd sigue expandiendo «%VAR%». No se
    /// puede escapar de forma fiable, así que un complemento que reciba textos de
    /// fuera hace mejor en no ser un .cmd — el ejemplo de YouTube llama a Python
    /// directamente por eso.
    /// </para>
    /// </summary>
    public static string ParaLote(string a) => "\"" + (a ?? "").Replace("\"", "\"\"") + "\"";

    /// <summary>
    /// Corre el complemento y devuelve sus mensajes uno a uno.
    /// </summary>
    /// <param name="quien">El complemento, ya validado.</param>
    /// <param name="comando">«listar» o «traer».</param>
    /// <param name="argumentos">Lo que le toque al comando.</param>
    /// <param name="corte">Para poder parar: una descarga larga tiene que poder cancelarse.</param>
    /// <param name="modelo">
    /// El puente al modelo de lenguaje, si este complemento lo declara. Con
    /// <c>null</c>, una pregunta suya se contesta con un no y sigue todo igual:
    /// preguntar sin que nadie escuche dejaría al complemento esperando.
    /// </param>
    public static async IAsyncEnumerable<Mensaje> CorrerAsync(
        Complemento quien,
        string comando,
        IEnumerable<string> argumentos,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken corte = default,
        PuenteDelModelo? modelo = null)
    {
        var psi = new ProcessStartInfo(quien.RutaEjecutable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // La ENTRADA se redirige siempre, la use o no. Si solo se redirigiera
            // para los que declaran el modelo, un complemento que lo declarase
            // por error se comportaría distinto en las dos ramas y el fallo solo
            // saldría en una de ellas.
            RedirectStandardInput = true,
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
            // Y la entrada también, por lo mismo pero al revés: la respuesta del
            // modelo lleva acentos, y sin esto llegaría al complemento rota por
            // la página de códigos de la consola.
            StandardInputEncoding = new System.Text.UTF8Encoding(false),
        };

        var todos = quien.Argumentos.Append(comando).Concat(argumentos);

        // Un fichero por lotes NO recibe los argumentos como un programa normal:
        // van por cmd.exe, y ahí «&» separa órdenes. La URL de una lista de
        // YouTube lleva «&list=...&index=6», así que llegaba partida y cmd
        // intentaba EJECUTAR «list» y «index».
        //
        // Y ahí está lo serio: si un «&» cuela una orden, la cuela igual una
        // fuente escrita a mala idea. Entrecomillar no es para que se vea bien.
        if (EsPorLotes(quien.RutaEjecutable))
            psi.Arguments = string.Join(' ', todos.Select(ParaLote));
        else
            foreach (var a in todos) psi.ArgumentList.Add(a);

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

        var seExplico = false;
        try
        {
            while (await proceso.StandardOutput.ReadLineAsync(corte).ConfigureAwait(false) is { } linea)
            {
                var m = Mensaje.Interpretar(linea);
                if (m is null) continue;

                // Una pregunta al modelo no es un suceso que enseñar: se atiende
                // y se le contesta por su entrada. Nunca se deja sin respuesta
                // -ni siquiera si no hay puente-, porque el complemento está
                // esperando una línea y sin ella se queda parado para siempre.
                if (m.Tipo == Mensaje.TipoPreguntar)
                {
                    var r = modelo is not null
                        ? await modelo.ResponderAsync(m, corte).ConfigureAwait(false)
                        : new Mensaje
                        {
                            Tipo = Mensaje.TipoRespuesta, Id = m.Id,
                            MensajeError = Textos.Instancia.IaComplementoSinPermiso,
                        };
                    try
                    {
                        await proceso.StandardInput
                            .WriteLineAsync(JsonSerializer.Serialize(r, SinEscapar))
                            .ConfigureAwait(false);
                        await proceso.StandardInput.FlushAsync(corte).ConfigureAwait(false);
                    }
                    catch { /* se murió mientras esperaba: el bucle lo verá al leer */ }
                    continue;
                }

                if (m.Tipo == Mensaje.TipoError) seExplico = true;
                yield return m;
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
        // ...Y SOLO si no se explicó. Un complemento que dice su motivo y luego
        // sale con código 1 está haciendo lo correcto -«no descargo», y lo dice-.
        // Soltarle encima «se murió sin explicarse» tapa el motivo bueno con uno
        // falso, y quien lo lee se queda pensando que el complemento está roto.
        if (proceso.ExitCode != 0 && !corte.IsCancellationRequested && !seExplico)
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
