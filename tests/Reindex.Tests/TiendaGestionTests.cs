using Ondine.Complementos;
using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Gestionar lo instalado: desinstalar, saber si hay versión nueva, y recordar
/// de qué lista viene cada catálogo.
///
/// <para>
/// La tienda sabía instalar y nada más. Instalar sin desinstalar deja al usuario
/// borrando carpetas a mano —que es justo lo que la tienda venía a evitar— y sin
/// saber si hay versión nueva, lo instalado se queda viejo para siempre sin que
/// nada lo diga.
/// </para>
/// </summary>
public static class TiendaGestionTests
{
    private static string Nueva()
    {
        var t = Path.Combine(Path.GetTempPath(), "ondine-tienda-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(t);
        return t;
    }

    public static void Todas()
    {
        Program.Seccion("Gestionar los complementos instalados");

        Desinstalar();
        HayVersionNueva();
        LaListaDeCadaCatalogo();
    }

    // ── Desinstalar ──
    private static void Desinstalar()
    {
        var baseDir = Nueva();
        try
        {
            var suya = Path.Combine(baseDir, "youtube");
            Directory.CreateDirectory(Path.Combine(suya, "dentro"));
            File.WriteAllText(Path.Combine(suya, "plugin.json"), "{}");
            File.WriteAllText(Path.Combine(suya, "dentro", "algo.py"), "x");

            // Un vecino que NO se debe tocar.
            var vecino = Path.Combine(baseDir, "otro");
            Directory.CreateDirectory(vecino);
            File.WriteAllText(Path.Combine(vecino, "plugin.json"), "{}");

            Program.Assert(Instalador.Desinstalar("youtube", baseDir).Ok, "desinstala");
            Program.Assert(!Directory.Exists(suya), "y la carpeta ya no está");
            Program.Assert(Directory.Exists(vecino), "el de al lado sigue entero");

            // Desinstalar lo que no está no es un error que haya que gritar, pero
            // tampoco un éxito: se dice que no estaba.
            Program.Assert(!Instalador.Desinstalar("youtube", baseDir).Ok,
                "desinstalar lo que ya no está no cuela como hecho");

            // ── Lo que de verdad importa aquí ──
            // Un id que en realidad es una ruta borraría lo que le diera la gana.
            // Se comprueba con las MISMAS reglas que al instalar, porque es el
            // mismo dato viniendo del mismo sitio.
            foreach (var malo in new[] { "..", "../otro", @"..\otro", "C:/Windows", "a/b", "" })
                Program.Assert(!Instalador.Desinstalar(malo, baseDir).Ok,
                    $"un id que es una ruta no borra nada: «{malo}»");

            Program.Assert(Directory.Exists(vecino), "y después de todo eso, el vecino sigue ahí");
            Program.Assert(Directory.Exists(baseDir), "y la carpeta de complementos también");
        }
        finally { try { Directory.Delete(baseDir, true); } catch { } }
    }

    // ── ¿Hay versión nueva? ──
    private static void HayVersionNueva()
    {
        Program.Assert(Indice.EsMasNueva("1.0.0", "1.0.1"), "1.0.1 es más nueva que 1.0.0");
        Program.Assert(Indice.EsMasNueva("1.0.9", "1.1.0"), "y 1.1.0 más que 1.0.9");
        Program.Assert(Indice.EsMasNueva("1.9.0", "2.0.0"), "y 2.0.0 más que 1.9.0");

        // Por número, no por texto: en orden alfabético «1.10.0» va ANTES que
        // «1.9.0», y entonces una actualización de verdad no se ofrecería.
        Program.Assert(Indice.EsMasNueva("1.9.0", "1.10.0"), "1.10.0 es más nueva que 1.9.0");

        Program.Assert(!Indice.EsMasNueva("1.0.1", "1.0.1"), "la misma no es más nueva");
        Program.Assert(!Indice.EsMasNueva("1.0.2", "1.0.1"), "ni una anterior");

        // Faltar trozos no es raro: hay quien versiona «1.2».
        Program.Assert(Indice.EsMasNueva("1.2", "1.2.1"), "«1.2» contra «1.2.1»");
        Program.Assert(!Indice.EsMasNueva("1.2.1", "1.2"), "y al revés, no");

        // Ante algo que no se puede comparar NO se ofrece actualizar. Empujar una
        // reinstalación por no saber leer un número es peor que no decir nada.
        Program.Assert(!Indice.EsMasNueva("", "1.0.0"), "sin versión instalada no se afirma nada");
        Program.Assert(!Indice.EsMasNueva("1.0.0", ""), "ni sin versión en el índice");
        Program.Assert(!Indice.EsMasNueva("una cosa", "otra"), "ni con texto que no es una versión");
    }

    // ── La lista de reproducción de cada catálogo ──
    private static void LaListaDeCadaCatalogo()
    {
        var tmp = Nueva();
        var antes = ReindexStore.RaizOverride;
        ReindexStore.RaizOverride = tmp;
        try
        {
            var doraemon = Path.Combine("C:", "cat", "doraemon.json");
            var shin = Path.Combine("C:", "cat", "shinchan.json");

            Program.Eq("", ReindexStore.CargarFuente(doraemon, "youtube"),
                "sin nada guardado, no se propone nada");

            ReindexStore.GuardarFuente(doraemon, "youtube", "https://youtube.com/playlist?list=AAA");
            Program.Eq("https://youtube.com/playlist?list=AAA",
                ReindexStore.CargarFuente(doraemon, "youtube"),
                "vuelve la lista de ese catálogo");

            // Cada catálogo la suya: la lista de Doraemon no vale para Shin-chan,
            // y proponerla ahí haría cotejar una serie contra la lista de otra.
            Program.Eq("", ReindexStore.CargarFuente(shin, "youtube"),
                "otro catálogo no hereda la lista");

            ReindexStore.GuardarFuente(shin, "youtube", "https://youtube.com/playlist?list=BBB");
            Program.Eq("https://youtube.com/playlist?list=AAA",
                ReindexStore.CargarFuente(doraemon, "youtube"),
                "y guardar la de uno no pisa la del otro");

            // Y cada COMPLEMENTO la suya: la lista de YouTube no le sirve a otro.
            Program.Eq("", ReindexStore.CargarFuente(doraemon, "otrocomplemento"),
                "otro complemento tampoco la hereda");

            // Vaciarla la borra, en vez de guardar una cadena vacía que luego se
            // proponga como si fuera una dirección.
            ReindexStore.GuardarFuente(doraemon, "youtube", "   ");
            Program.Eq("", ReindexStore.CargarFuente(doraemon, "youtube"), "vaciarla la olvida");

            // Sin catálogo abierto no hay dónde apuntarla, y no debe reventar.
            ReindexStore.GuardarFuente("", "youtube", "https://algo");
            Program.Eq("", ReindexStore.CargarFuente("", "youtube"), "sin catálogo, nada");
        }
        finally
        {
            ReindexStore.RaizOverride = antes;
            try { Directory.Delete(tmp, true); } catch { }
        }
    }
}
