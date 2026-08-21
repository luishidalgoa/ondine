using Ondine.Trabajos;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La cola de trabajos.
///
/// <para>
/// Hoy todos los ficheros de una tanda comparten las MISMAS opciones, así que para comprimir
/// dos cosas con ajustes distintos hay que esperar a que acabe la primera. La cola lo
/// arregla: cada trabajo se lleva sus opciones puestas y se van despachando en orden.
/// </para>
/// <para>
/// <b>Y ahí está la regla de la que depende todo lo demás:</b> un trabajo guarda una COPIA de
/// las opciones, no una referencia a las de la pantalla. Guardando la referencia, cambiar los
/// ajustes para el siguiente trabajo cambiaría también el que ya está en cola — y eso es
/// justo la sorpresa que esta función viene a evitar. Sin esa copia, la cola es peor que no
/// tenerla.
/// </para>
/// </summary>
public static class ColaDeTrabajosTests
{
    private static EncodeOptions Opciones(string codec = "hevc", int calidad = 24) =>
        new() { VideoCodec = codec, Quality = calidad };

    public static void Todas()
    {
        Program.Seccion("La cola de trabajos");

        // ══ LA REGLA: las opciones se copian, no se apuntan ═══════════════════════
        var deLaPantalla = Opciones("hevc", 24);
        var cola = new ColaDeTrabajos();
        var uno = cola.Encolar(["a.mkv"], deLaPantalla, "C:/salida")!;

        // El usuario cambia los ajustes para el trabajo siguiente. El que ya encoló
        // NO puede enterarse.
        deLaPantalla.VideoCodec = "av1";
        deLaPantalla.Quality = 40;

        Program.Assert(uno.Opciones.VideoCodec == "hevc" && uno.Opciones.Quality == 24,
            "un trabajo ya encolado conserva las opciones con las que se encoló");
        Program.Assert(!ReferenceEquals(uno.Opciones, deLaPantalla),
            "y las guarda aparte: compartir el objeto es lo que dejaría que cambiaran solas");

        var dos = cola.Encolar(["b.mkv"], deLaPantalla, "C:/salida")!;
        Program.Assert(dos.Opciones.VideoCodec == "av1",
            "y el siguiente se lleva las nuevas, que es para lo que existe la cola");

        // Las listas de dentro también, o la copia es de mentira: cambiar los idiomas
        // después mutaría la MISMA lista que se llevó el trabajo.
        var conIdiomas = new EncodeOptions { KeepLangs = { "spa" } };
        var tres = cola.Encolar(["c.mkv"], conIdiomas, "C:/salida")!;
        conIdiomas.KeepLangs.Add("eng");
        Program.Assert(tres.Opciones.KeepLangs.Count == 1,
            "la copia llega hasta las listas de dentro, no solo a la superficie");

        // ── El orden y qué toca ahora ─────────────────────────────────────────────
        var c2 = new ColaDeTrabajos();
        var t1 = c2.Encolar(["1.mkv"], Opciones(), "C:/s")!;
        var t2 = c2.Encolar(["2.mkv"], Opciones(), "C:/s")!;

        Program.Assert(c2.Siguiente()?.Id == t1.Id, "se despachan en el orden en que se pusieron");

        c2.Empezar(t1.Id);
        Program.Assert(t1.Estado == EstadoDelTrabajo.EnCurso, "el que se empieza queda en curso");
        Program.Assert(c2.Siguiente()?.Id == t1.Id,
            "y sigue siendo «el de ahora»: no se salta al siguiente teniendo uno a medias");

        c2.Terminar(t1.Id, salieron: 1, fallaron: 0);
        Program.Assert(t1.Estado == EstadoDelTrabajo.Hecho, "al terminar sin fallos queda hecho");
        Program.Assert(c2.Siguiente()?.Id == t2.Id, "y entonces sí pasa al siguiente");

        c2.Terminar(t2.Id, salieron: 0, fallaron: 1);
        Program.Assert(t2.Estado == EstadoDelTrabajo.Fallido,
            "si no salió ninguno, el trabajo es fallido y se ve: no se cuenta como hecho");
        Program.Assert(c2.Siguiente() is null, "con todo despachado no queda nada que hacer");

        // A medias es su propio estado. Decir «hecho» con la mitad fuera es mentir, y
        // decir «fallido» con la mitad dentro manda a repetir lo que ya está.
        var c3 = new ColaDeTrabajos();
        var aMedias = c3.Encolar(["x.mkv", "y.mkv"], Opciones(), "C:/s")!;
        c3.Terminar(aMedias.Id, salieron: 1, fallaron: 1);
        Program.Assert(aMedias.Estado == EstadoDelTrabajo.AMedias,
            "con unos dentro y otros fuera, ni hecho ni fallido: a medias");

        // ── Reordenar ─────────────────────────────────────────────────────────────
        var c4 = new ColaDeTrabajos();
        var a = c4.Encolar(["a"], Opciones(), "C:/s")!;
        var b = c4.Encolar(["b"], Opciones(), "C:/s")!;
        var c = c4.Encolar(["c"], Opciones(), "C:/s")!;

        Program.Assert(c4.Subir(c.Id) && c4.Trabajos[1].Id == c.Id,
            "un trabajo pendiente se puede adelantar");
        Program.Assert(c4.Bajar(a.Id) && c4.Trabajos[1].Id == a.Id,
            "y retrasar");

        c4.Empezar(c4.Trabajos[0].Id);
        var enCurso = c4.Trabajos[0];
        Program.Assert(!c4.Bajar(enCurso.Id),
            "el que está en curso NO se mueve: ya se está escribiendo en el disco");
        Program.Assert(!c4.Subir(c4.Trabajos[1].Id),
            "ni se adelanta nada por encima de él, que sería colarse delante de lo que ya corre");

        // ── Quitar ────────────────────────────────────────────────────────────────
        Program.Assert(c4.Quitar(b.Id), "un pendiente se puede sacar de la cola");
        Program.Assert(!c4.Quitar(enCurso.Id),
            "el que está en curso no: para eso se cancela, que es otra cosa y se ve distinto");

        // ── El mismo fichero en dos trabajos ──────────────────────────────────────
        // No se prohíbe -puede ser a propósito: dos formatos del mismo original- pero se
        // avisa, porque el segundo trabajo leería un fichero que el primero puede haber
        // mandado a la papelera. Descubrirlo a mitad de cola es tardísimo.
        var c5 = new ColaDeTrabajos();
        c5.Encolar(["peli.mkv", "otra.mkv"], Opciones(), "C:/s");
        var repes = c5.YaEnCola(["peli.mkv", "nueva.mkv"]);
        Program.Assert(repes.Count == 1 && repes[0] == "peli.mkv",
            "se dice qué ficheros ya están en la cola, para poder avisar antes de encolar");

        var sinRepes = c5.YaEnCola(["nueva.mkv"]);
        Program.Assert(sinRepes.Count == 0, "y no se avisa cuando no hay de qué");

        // ── Lo que queda por hacer ────────────────────────────────────────────────
        var c6 = new ColaDeTrabajos();
        var p1 = c6.Encolar(["1", "2"], Opciones(), "C:/s")!;
        c6.Encolar(["3"], Opciones(), "C:/s");
        c6.Empezar(p1.Id);
        Program.Assert(c6.Pendientes == 1 && c6.FicherosPorHacer == 3,
            "queda 1 trabajo por empezar, y 3 ficheros contando el que corre");

        c6.Terminar(p1.Id, salieron: 2, fallaron: 0);
        Program.Assert(c6.FicherosPorHacer == 1, "lo despachado deja de contar");

        var vacia = new ColaDeTrabajos();
        Program.Assert(vacia.Siguiente() is null && vacia.FicherosPorHacer == 0,
            "una cola vacía no inventa trabajo");

        // Encolar sin ficheros no es un trabajo: es una fila que no hace nada y estorba.
        Program.Assert(new ColaDeTrabajos().Encolar([], Opciones(), "C:/s") is null,
            "no se encola un trabajo sin ficheros");

        LaCopiaNoSeDejaNingunaOpcion();
    }

    /// <summary>
    /// Que la copia no se deje ninguna opción por el camino.
    ///
    /// <para>
    /// La copia es una lista de propiedades escrita a mano, y eso ya ha mordido en este
    /// repositorio: la CLI dejó de compilar en cinco plataformas porque una lista a mano se
    /// quedó corta. Aquí sería peor, porque <b>no daría error</b>: se añade una opción nueva
    /// a <c>EncodeOptions</c>, se olvida en <c>Copiar</c>, y los trabajos de la cola se
    /// comprimen ignorándola en silencio.
    /// </para>
    /// <para>
    /// Se recorre por reflexión, así que la comprobación no hay que mantenerla: una
    /// propiedad nueva entra sola y la cubre desde el primer día.
    /// </para>
    /// </summary>
    private static void LaCopiaNoSeDejaNingunaOpcion()
    {
        var props = typeof(EncodeOptions).GetProperties()
            .Where(p => p.CanRead && p.CanWrite).ToList();

        var original = new EncodeOptions();
        var puestas = new List<string>();

        // A cada propiedad se le pone un valor DISTINTO del que trae de fábrica: si se le
        // pusiera el de por defecto, una propiedad olvidada en la copia seguiría cuadrando.
        var sinCubrir = new List<string>();

        foreach (var p in props)
        {
            var valor = ValorDistintoDelPorDefecto(p.PropertyType);
            if (valor is null) { sinCubrir.Add($"{p.Name} ({Nombre(p.PropertyType)})"); continue; }

            p.SetValue(original, valor);
            puestas.Add(p.Name);
        }

        // Y se dice lo que NO se pudo cubrir, en vez de callarlo. La lista de tipos era a
        // mano y tenia dos huecos -los enum y los long-, asi que Velocidad y
        // TamanoObjetivoBytes entraron sin que esto se enterara. Un guardian con un hueco
        // silencioso es peor que ninguno: da la tranquilidad sin dar la cobertura.
        Program.Assert(sinCubrir.Count == 0,
            sinCubrir.Count == 0
                ? "esta comprobacion sabe fabricar un valor para TODOS los tipos de EncodeOptions"
                : $"no se sabe probar {sinCubrir.Count} opciones: {string.Join(", ", sinCubrir)}. " +
                  "Ensena a ValorDistintoDelPorDefecto a fabricar ese tipo, o quedan sin vigilar.");

        Program.Assert(puestas.Count >= 10,
            $"la comprobación toca {puestas.Count} opciones: si fueran cuatro, no estaría midiendo nada");

        var cola = new ColaDeTrabajos();
        var encolado = cola.Encolar(["f.mkv"], original, "C:/s")!;

        var olvidadas = new List<string>();
        foreach (var nombre in puestas)
        {
            var p = props.First(x => x.Name == nombre);
            var a = p.GetValue(original);
            var b = p.GetValue(encolado.Opciones);

            bool igual = (a, b) switch
            {
                (List<string> la, List<string> lb) => la.SequenceEqual(lb),
                _ => Equals(a, b),
            };
            if (!igual) olvidadas.Add(nombre);
        }

        Program.Assert(olvidadas.Count == 0,
            olvidadas.Count == 0
                ? $"la copia se lleva las {puestas.Count} opciones, ninguna se queda atrás"
                : $"la copia se deja {olvidadas.Count}: {string.Join(", ", olvidadas)}. " +
                  "Añádelas a ColaDeTrabajos.Copiar o el trabajo encolado las ignorará en silencio.");
    }

    /// <summary>
    /// Un valor DISTINTO del que trae de fabrica, para cualquier tipo. Si se le pusiera el
    /// de por defecto, una propiedad olvidada en la copia seguiria cuadrando y la
    /// comprobacion no probaria nada.
    ///
    /// <para>
    /// Devuelve null cuando no sabe fabricarlo, y quien llama lo DICE. Antes esto era un
    /// switch de tipos escrito a mano —justo el antipatron que esta comprobacion existe para
    /// evitar— y se callaba lo que no reconocia.
    /// </para>
    /// </summary>
    private static object? ValorDistintoDelPorDefecto(Type t)
    {
        var real = Nullable.GetUnderlyingType(t) ?? t;

        if (real == typeof(string)) return "cambiado";
        if (real == typeof(bool)) return true;
        if (real.IsEnum) return Enum.GetValues(real).Cast<object>().Skip(1).FirstOrDefault();
        if (real == typeof(List<string>)) return new List<string> { "zzz" };

        // Cualquier numero: int, long, double, float, decimal, short, byte…
        try
        {
            if (real.IsPrimitive || real == typeof(decimal))
                return Convert.ChangeType(42, real, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch { /* no es convertible desde 42: se dira que no se sabe */ }

        // Una clase suya con constructor vacio: vale una instancia nueva. La copia la pasa
        // por referencia, asi que comparar por Equals distingue «copiada» de «olvidada»
        // -que se quedaria en null-. Asi entro NameRule, que llevaba sin vigilar desde el
        // principio sin que nadie lo supiera.
        if (real.IsClass && real.GetConstructor(Type.EmptyTypes) is not null)
        {
            try { return Activator.CreateInstance(real); } catch { }
        }

        return null;
    }

    private static string Nombre(Type t) =>
        Nullable.GetUnderlyingType(t) is { } u ? $"{u.Name}?" : t.Name;
}