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
    /// <summary>
    /// Lo más larga que puede ser una línea del complemento. El contrato es «una línea, un
    /// mensaje», y 64 kB de JSON es muchísimo para un mensaje: el título más largo del mundo cabe
    /// mil veces.
    /// </summary>
    public const int TechoDeLinea = 64 * 1024;

    /// <summary>Cuánto se guarda del error estándar. Solo se enseñan 300 caracteres.</summary>
    public const int TechoDelRuido = 8 * 1024;

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
    /// Las líneas de un lector, <b>sin que ninguna pueda crecer sin fin</b>.
    ///
    /// <para>
    /// <c>ReadLineAsync</c> lee hasta el salto de línea, y si no llega ninguno sigue guardando en
    /// memoria. Un complemento que escriba sin parar y sin saltar de línea se lleva por delante la
    /// aplicación sin hacer nada ilegal — ni siquiera hace falta que sea a mala idea.
    /// </para>
    /// <para>
    /// La línea que se pasa <b>se descarta y se sigue leyendo</b>. Cortar ahí convertiría una
    /// línea gorda en el final de la descarga, y lo que viene detrás suele ser justo lo que
    /// interesa: el «hecho» con lo que se trajo.
    /// </para>
    /// </summary>
    public static async IAsyncEnumerable<string> LineasConTecho(
        TextReader lector, int techo,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken corte = default)
    {
        var trozo = new char[8192];
        var linea = new System.Text.StringBuilder();
        var pasada = false;   // esta línea ya se pasó del techo: se tira lo que queda de ella

        int leidos;
        while ((leidos = await lector.ReadAsync(trozo.AsMemory(), corte).ConfigureAwait(false)) > 0)
        {
            for (var i = 0; i < leidos; i++)
            {
                var c = trozo[i];
                if (c == '\n')
                {
                    if (!pasada) yield return Limpia(linea);
                    linea.Clear();
                    pasada = false;
                    continue;
                }

                if (pasada) continue;
                if (linea.Length >= techo) { pasada = true; linea.Clear(); continue; }
                linea.Append(c);
            }
        }

        // Lo último, aunque no traiga salto de línea: un complemento que termina sin saltar sigue
        // habiendo dicho algo.
        if (!pasada && linea.Length > 0) yield return Limpia(linea);
    }

    /// <summary>Sin el retorno de carro de Windows, que si no se queda pegado al mensaje.</summary>
    private static string Limpia(System.Text.StringBuilder sb) =>
        sb.ToString().TrimEnd('\r');

    /// <summary>
    /// Vacía un lector guardando solo el principio. Vaciar hay que vaciarlo entero: si nadie lee,
    /// el complemento se bloquea escribiendo y parece colgado.
    /// </summary>
    private static async Task<string> VaciarConTecho(TextReader lector, int techo)
    {
        var trozo = new char[8192];
        var guardado = new System.Text.StringBuilder();

        int leidos;
        while ((leidos = await lector.ReadAsync(trozo.AsMemory()).ConfigureAwait(false)) > 0)
        {
            var caben = techo - guardado.Length;
            if (caben > 0) guardado.Append(trozo, 0, Math.Min(caben, leidos));
        }
        return guardado.ToString();
    }

    /// <summary>
    /// Corre el complemento y devuelve sus mensajes uno a uno.
    /// </summary>
    /// <param name="quien">El complemento, ya validado.</param>
    /// <param name="comando">«listar» o «traer».</param>
    /// <param name="argumentos">Lo que le toque al comando.</param>
    /// <param name="corte">Para poder parar: una descarga larga tiene que poder cancelarse.</param>
    /// <param name="destino">
    /// La carpeta que eligió el usuario. Lo que el complemento diga haber dejado FUERA de ella no
    /// se le cree. Sin ella no se le cree ninguno.
    /// </param>
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
        PuenteDelModelo? modelo = null,
        string? destino = null)
    {
        // QUÉ se ejecuta lo decide la resolución, que sabe de sistemas: en Windows el .cmd
        // declarado, en Unix el .sh de al lado, o el intérprete con el .py delante. Aquí solo se
        // arranca lo que diga.
        var arranque = quien.ComoArrancar();
        if (arranque.Reparo is not null)
        {
            yield return new Mensaje
            {
                Tipo = Mensaje.TipoError,
                MensajeError = string.Format(Textos.Instancia.ComplementoNoArranca,
                    quien.Nombre, arranque.Reparo),
            };
            yield break;
        }

        // Y el permiso de ejecución, JUSTO ANTES de lanzarlo. Un .sh salido de un .zip hecho en
        // Windows llega sin él: está donde tiene que estar, con el contenido correcto, y el
        // sistema contesta «permission denied». Se pone aquí además de al instalar porque un
        // complemento también se copia a mano, y ahí no pasa por el instalador.
        //
        // Solo cuando se ejecuta ÉL. Si va por intérprete, el programa es el intérprete —fuera de
        // la carpeta del complemento— y ahí no se tocan permisos de nada.
        if (arranque.Antes.Count == 0) Permisos.AsegurarEjecutable(arranque.Programa);

        var psi = new ProcessStartInfo(arranque.Programa)
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

        // «Antes» va primero de todo: es el script que el intérprete tiene que abrir, y detrás
        // van los argumentos fijos del complemento y su subcomando.
        var todos = arranque.Antes.Concat(quien.Argumentos).Append(comando).Concat(argumentos);

        // Un fichero por lotes NO recibe los argumentos como un programa normal:
        // van por cmd.exe, y ahí «&» separa órdenes. La URL de una lista de
        // YouTube lleva «&list=...&index=6», así que llegaba partida y cmd
        // intentaba EJECUTAR «list» y «index».
        //
        // Y ahí está lo serio: si un «&» cuela una orden, la cuela igual una
        // fuente escrita a mala idea. Entrecomillar no es para que se vea bien.
        if (arranque.PorLotes)
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
        // El error estándar se vacía en paralelo, CON TECHO. Antes se guardaba entero: un
        // complemento hablador —o uno que escupe su traza en bucle— llenaba la memoria de la
        // aplicación con un texto del que luego solo se enseñan 300 caracteres. Se queda con el
        // principio y sigue vaciando la tubería, que es lo que no se puede dejar de hacer: si
        // nadie lee, el complemento se bloquea escribiendo y parece colgado.
        var ruido = Task.Run(() => VaciarConTecho(proceso.StandardError, TechoDelRuido),
                             CancellationToken.None);

        var seExplico = false;
        try
        {
            await foreach (var linea in LineasConTecho(proceso.StandardOutput, TechoDeLinea, corte)
                               .WithCancellation(corte).ConfigureAwait(false))
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

                // LO QUE DICE HABER TRAÍDO, filtrado aquí y no en cada pantalla: hay dos, y
                // arreglar una y dejar la otra es la forma habitual de que esto vuelva.
                if (m.Tipo == Mensaje.TipoHecho)
                    m.Ficheros = Mensaje.SoloDentroDe(destino, m.Ficheros);

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
