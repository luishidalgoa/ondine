using Ondine.Trabajos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La tabla de Comprimir como vista de la cola.
///
/// <para>
/// Lo que se fija aquí es la regla de la que depende la pantalla: lo que está en la cola va
/// arriba, en el orden en que se comprimirá, y lo demás debajo. Una fila que ya no está en la
/// cola no puede seguir contándose como «en cola»: fue exactamente el fallo de quitar de la
/// tabla sin tocar la cola.
/// </para>
/// </summary>
public static class VistaDeColaTests
{
    private static EncodeOptions Opciones() => new() { VideoCodec = "hevc", Quality = 24 };

    private static VideoRow Fila(string ruta) => new() { Path = ruta, Name = Path.GetFileName(ruta) };

    public static void Todas()
    {
        Program.Seccion("La tabla como vista de la cola");

        LoEncoladoVaArribaEnSuOrden();
        UnFicheroEnDosTrabajosCuentaEnElPrimero();
        LosTrabajosTerminadosNoCuentan();
        CadaGrupoAbreConSuCabecera();
        SacarDeLaColaLoDevuelveALosAnalizados();
        LoQueSeComprimeVaArribaComoProcesando();
    }

    private static void LoEncoladoVaArribaEnSuOrden()
    {
        var cola = new ColaDeTrabajos();
        cola.Encolar(["c.mkv"], Opciones(), "C:/s");
        cola.Encolar(["a.mkv", "b.mkv"], Opciones(), "C:/s");
        // La lista está en otro orden: el analizado va primero por llegar primero.
        var filas = new[] { Fila("x.mkv"), Fila("a.mkv"), Fila("c.mkv"), Fila("b.mkv"), Fila("y.mkv") };

        var vista = VistaDeCola.Colocar(filas, cola.Trabajos);
        var rutas = vista.Select(e => e.Fila.Path).ToList();

        Program.Assert(string.Join(",", rutas) == "c.mkv,a.mkv,b.mkv,x.mkv,y.mkv",
            $"lo encolado va arriba en el orden de la cola, y lo demás debajo ({string.Join(",", rutas)})");
        Program.Assert(vista.Take(3).All(e => e.Grupo == GrupoDeVideo.EnCola)
                    && vista.Skip(3).All(e => e.Grupo == GrupoDeVideo.Analizado),
            "cada fila sabe en qué grupo está");
        Program.Assert(vista[0].PuestoEnCola == 1 && vista[2].PuestoEnCola == 3 && vista[3].PuestoEnCola == 0,
            "el puesto en la cola se cuenta por fichero, y los analizados no tienen puesto");
    }

    private static void UnFicheroEnDosTrabajosCuentaEnElPrimero()
    {
        var cola = new ColaDeTrabajos();
        cola.Encolar(["a.mkv"], Opciones(), "C:/s");
        cola.Encolar(["b.mkv", "a.mkv"], Opciones(), "C:/s");

        var vista = VistaDeCola.Colocar([Fila("a.mkv"), Fila("b.mkv")], cola.Trabajos);

        Program.Assert(vista[0].Fila.Path == "a.mkv" && vista[0].PuestoEnCola == 1,
            "un fichero repetido en dos trabajos ocupa el puesto del primero que lo comprime");
        Program.Assert(vista.Count == 2 && vista.All(e => e.Grupo == GrupoDeVideo.EnCola),
            "y no aparece dos veces en la tabla");
    }

    private static void LosTrabajosTerminadosNoCuentan()
    {
        var cola = new ColaDeTrabajos();
        var hecho = cola.Encolar(["a.mkv"], Opciones(), "C:/s")!;
        cola.Empezar(hecho.Id);
        cola.Terminar(hecho.Id, 1, 0);
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");

        var vista = VistaDeCola.Colocar([Fila("a.mkv"), Fila("b.mkv")], cola.Trabajos);

        Program.Assert(vista[0].Fila.Path == "b.mkv" && vista[1].Fila.Path == "a.mkv"
                    && vista[1].Grupo == GrupoDeVideo.Analizado,
            "un fichero ya comprimido sale de la cola y vuelve a los analizados");
    }

    private static void CadaGrupoAbreConSuCabecera()
    {
        var cola = new ColaDeTrabajos();
        cola.Encolar(["a.mkv", "b.mkv"], Opciones(), "C:/s");
        var vista = VistaDeCola.Colocar([Fila("a.mkv"), Fila("b.mkv"), Fila("c.mkv")], cola.Trabajos);

        Program.Assert(vista[0].PrimeraDeGrupo && vista[0].Cuantos == 2,
            "el primero de la cola abre el grupo, y el grupo sabe que tiene dos");
        Program.Assert(!vista[1].PrimeraDeGrupo,
            "el segundo de la cola no vuelve a abrir grupo");
        Program.Assert(vista[2].PrimeraDeGrupo && vista[2].Cuantos == 1,
            "el primer analizado abre su grupo, con uno");

        var sinCola = VistaDeCola.Colocar([Fila("c.mkv")], new ColaDeTrabajos().Trabajos);
        Program.Assert(sinCola.Count == 1 && sinCola[0].PrimeraDeGrupo && sinCola[0].Grupo == GrupoDeVideo.Analizado,
            "sin cola, todo es analizado y el grupo abre sin nada encima");
    }

    /// <summary>
    /// Lo que se comprime ahora (lo lanzó el botón Comprimir directo, sin cola) no puede verse como analizado:
    /// va arriba, en el grupo de cola, marcado como procesando.
    /// </summary>
    private static void LoQueSeComprimeVaArribaComoProcesando()
    {
        var cola = new ColaDeTrabajos();
        cola.Encolar(["b.mkv"], Opciones(), "C:/s");
        var filas = new[] { Fila("a.mkv"), Fila("b.mkv"), Fila("c.mkv") };

        var vista = VistaDeCola.Colocar(filas, cola.Trabajos, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a.mkv" });

        Program.Assert(vista[0].Fila.Path == "a.mkv" && vista[0].Procesando && vista[0].Grupo == GrupoDeVideo.EnCola,
            "lo que se comprime ahora va arriba, en el grupo de cola, como procesando");
        Program.Assert(vista[1].Fila.Path == "b.mkv" && !vista[1].Procesando && vista[1].PuestoEnCola == 1,
            "lo que espera sigue en la cola, con su puesto y sin estar procesando");
        Program.Assert(vista[2].Fila.Path == "c.mkv" && vista[2].Grupo == GrupoDeVideo.Analizado && vista[2].TrabajoId == 0,
            "lo que no está en la cola ni se comprime es analizado, sin trabajo");
        Program.Assert(vista[1].TrabajoId > 0,
            "cada fila de la cola sabe a qué trabajo pertenece, para poder ordenarla");
    }

    private static void SacarDeLaColaLoDevuelveALosAnalizados()
    {
        // Es lo que hace «quitar de la cola» en la tabla: la fila no desaparece, cambia de grupo.
        var cola = new ColaDeTrabajos();
        cola.Encolar(["a.mkv", "b.mkv"], Opciones(), "C:/s");
        var filas = new[] { Fila("a.mkv"), Fila("b.mkv") };
        cola.QuitarFicheros(["a.mkv"]);

        var vista = VistaDeCola.Colocar(filas, cola.Trabajos);

        Program.Assert(vista[0].Fila.Path == "b.mkv" && vista[0].Grupo == GrupoDeVideo.EnCola
                    && vista[1].Fila.Path == "a.mkv" && vista[1].Grupo == GrupoDeVideo.Analizado,
            "quitar de la cola deja la fila en la tabla, debajo, como analizada");
    }
}
