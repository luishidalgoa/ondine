using Ondine.Trabajos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El despachador de la cola: qué se lanza, en qué orden, y cuándo.
///
/// <para>
/// Estas reglas viven aquí, en el motor, y no en la ventana, porque en la ventana no hay forma
/// de comprobarlas. El fallo que motivó esto fue que lo añadido mientras corría una compresión
/// no llegaba a lanzarse: se puede reproducir sin ventana, con una compresión falsa.
/// </para>
/// </summary>
public static class DespachoDeColaTests
{
    private static EncodeOptions Opciones() => new() { VideoCodec = "hevc", Quality = 24 };

    public static void Todas()
    {
        Program.Seccion("El despachador de la cola");

        LoQueSeAnadeMientrasCorreSeLanza();
        UnaCompresionDirectaEnMarchaNoArrancaNada();
        UnSoloBucleALaVez();
        CancelarDetieneLaColaEntera();
        UnFalloNoParaLaCola();
    }

    /// <summary>Lo que se mete con la cola en marcha sale después de lo que ya estaba.</summary>
    private static void LoQueSeAnadeMientrasCorreSeLanza()
    {
        var cola = new ColaDeTrabajos();
        var despacho = new DespachoDeCola(cola);
        var orden = new List<string>();
        var a = cola.Encolar(["a.mkv"], Opciones(), "C:/s")!;
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");

        despacho.DrenarAsync(
            () => false,
            async t =>
            {
                orden.Add(t.Ficheros[0]);
                if (t.Id == a.Id) cola.Encolar(["c.mkv"], Opciones(), "C:/s");   // llega mientras corre A
                await Task.Yield();
                return (false, 1, 0);
            },
            () => { }).GetAwaiter().GetResult();

        Program.Assert(string.Join(",", orden) == "a.mkv,b.mkv,c.mkv",
            $"lo añadido a mitad de la tanda se lanza después de lo que ya estaba ({string.Join(",", orden)})");
        Program.Assert(cola.Trabajos.All(t => t.Estado == EstadoDelTrabajo.Hecho),
            "y todo queda hecho, sin nada colgado");
    }

    /// <summary>Con una compresión directa en marcha, la cola no lanza nada; luego sí.</summary>
    private static void UnaCompresionDirectaEnMarchaNoArrancaNada()
    {
        var cola = new ColaDeTrabajos();
        var despacho = new DespachoDeCola(cola);
        cola.Encolar(["a.mkv"], Opciones(), "C:/s");
        bool directa = true;
        int lanzados = 0;
        Func<Trabajo, Task<(bool, int, int)>> correr = async _ => { lanzados++; await Task.Yield(); return (false, 1, 0); };

        despacho.DrenarAsync(() => directa, correr, () => { }).GetAwaiter().GetResult();
        Program.Assert(lanzados == 0, "con otra compresión en marcha, la cola no lanza nada");
        Program.Assert(cola.Trabajos[0].Estado == EstadoDelTrabajo.Pendiente,
            "y el trabajo sigue esperando, sin darlo por empezado");

        directa = false;   // la directa termina y vuelve a llamar: ahora sí
        despacho.DrenarAsync(() => directa, correr, () => { }).GetAwaiter().GetResult();
        Program.Assert(lanzados == 1 && cola.Trabajos[0].Estado == EstadoDelTrabajo.Hecho,
            "cuando la directa termina y vuelve a llamar, el trabajo sale");
    }

    /// <summary>Un segundo drenaje mientras hay uno no lanza otra vez lo que ya corre.</summary>
    private static void UnSoloBucleALaVez()
    {
        var cola = new ColaDeTrabajos();
        var despacho = new DespachoDeCola(cola);
        var a = cola.Encolar(["a.mkv"], Opciones(), "C:/s")!;
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");
        var veces = new Dictionary<int, int>();

        despacho.DrenarAsync(
            () => false,
            async t =>
            {
                veces[t.Id] = veces.GetValueOrDefault(t.Id) + 1;
                if (t.Id == a.Id) await despacho.DrenarAsync(() => false, _ => Task.FromResult((false, 1, 0)), () => { });
                await Task.Yield();
                return (false, 1, 0);
            },
            () => { }).GetAwaiter().GetResult();

        Program.Assert(veces.Values.All(n => n == 1) && veces.Count == 2,
            "un segundo drenaje mientras hay uno no vuelve a lanzar lo que ya corre");
    }

    /// <summary>Cancelar para la cola entera: lo que queda sigue pendiente.</summary>
    private static void CancelarDetieneLaColaEntera()
    {
        var cola = new ColaDeTrabajos();
        var despacho = new DespachoDeCola(cola);
        cola.Encolar(["a.mkv"], Opciones(), "C:/s");
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");

        despacho.DrenarAsync(
            () => false,
            async _ => { await Task.Yield(); return (true, 0, 0); },
            () => { }).GetAwaiter().GetResult();

        Program.Assert(cola.Trabajos[0].Estado == EstadoDelTrabajo.Cancelado,
            "el trabajo cancelado queda marcado como cancelado");
        Program.Assert(cola.Trabajos[1].Estado == EstadoDelTrabajo.Pendiente,
            "y el siguiente no arranca: cancelar detiene la cola entera");
    }

    /// <summary>Un trabajo que revienta cuenta como fallido, y la cola sigue.</summary>
    private static void UnFalloNoParaLaCola()
    {
        var cola = new ColaDeTrabajos();
        var despacho = new DespachoDeCola(cola);
        var a = cola.Encolar(["a.mkv"], Opciones(), "C:/s")!;
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");

        despacho.DrenarAsync(
            () => false,
            async t =>
            {
                await Task.Yield();
                if (t.Id == a.Id) throw new InvalidOperationException("ffmpeg no arranca");
                return (false, 1, 0);
            },
            () => { }).GetAwaiter().GetResult();

        Program.Assert(cola.Trabajos[0].Estado == EstadoDelTrabajo.Fallido,
            "el que revienta queda fallido, no en curso para siempre");
        Program.Assert(cola.Trabajos[1].Estado == EstadoDelTrabajo.Hecho,
            "y el siguiente se lanza igualmente");
    }
}
