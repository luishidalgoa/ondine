using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Apuntar varios ficheros de una sola vez en «déjalo como está».
///
/// <para>
/// Existía la versión de uno en uno, y el botón de grupo la llamaba en bucle:
/// dieciséis filas eran dieciséis ciclos de leer, parsear y reescribir el
/// catálogo entero —medido, 37 ms de mediana cada uno— con la ventana muerta
/// mientras tanto.
/// </para>
/// </summary>
public static class DejarComoEstaLoteTests
{
    public static void Todas()
    {
        Program.Seccion("Dejar como están, en lote");

        var tmp = Path.Combine(Path.GetTempPath(), "ondine-lote-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            // Un catálogo con un campo que la app NO conoce: el formato promete
            // respetarlo, y un lote no puede ser la excepción a esa promesa.
            File.WriteAllText(tmp, """
            {
              "esquema": "reindex/1.0",
              "serie": "Serie",
              "mis_notas": "esto es mío y tiene que seguir aquí",
              "episodios": [ { "num": 1, "titulos": { "es": ["Uno"] } } ]
            }
            """);

            int n = ReindexCatalog.AnadirADejarComoEsta(tmp, new[] { "b.avi", "a.avi", "c.avi" });
            Program.Eq(3, n, "apunta los tres de una vez");

            var cat = ReindexCatalog.Load(tmp);
            Program.Eq(3, cat.DejarComoEsta.Count, "y quedan los tres en el catálogo");

            Program.Assert(File.ReadAllText(tmp).Contains("esto es mío"),
                "el campo que la app no conoce sigue ahí");

            // Repetir no duplica, y lo dice devolviendo cuántos entraron DE VERDAD.
            Program.Eq(0, ReindexCatalog.AnadirADejarComoEsta(tmp, new[] { "a.avi", "c.avi" }),
                "los que ya estaban no cuentan");
            Program.Eq(1, ReindexCatalog.AnadirADejarComoEsta(tmp, new[] { "a.avi", "d.avi" }),
                "de una mezcla, solo el nuevo");
            Program.Eq(4, ReindexCatalog.Load(tmp).DejarComoEsta.Count, "y no se duplica ninguno");

            // Sin nada que apuntar NO se toca el fichero. Escribirlo igualmente
            // reindentaría 329 KB del catálogo del usuario para nada.
            var antes = File.GetLastWriteTimeUtc(tmp);
            Thread.Sleep(20);
            Program.Eq(0, ReindexCatalog.AnadirADejarComoEsta(tmp, Array.Empty<string>()),
                "una lista vacía no apunta nada");
            Program.Assert(File.GetLastWriteTimeUtc(tmp) == antes,
                "y no reescribe el fichero");

            Program.Eq(0, ReindexCatalog.AnadirADejarComoEsta(tmp, new[] { "  ", "" }),
                "los nombres en blanco se ignoran");

            // La ruta entera se guarda como solo el nombre, igual que la de uno en uno.
            ReindexCatalog.AnadirADejarComoEsta(tmp, new[] { Path.Combine("C:", "tv", "e.avi") });
            Program.Assert(ReindexCatalog.Load(tmp).DejarComoEsta
                    .Any(x => string.Equals(x, "e.avi", StringComparison.OrdinalIgnoreCase)),
                "de una ruta se guarda solo el nombre");
        }
        finally { try { File.Delete(tmp); } catch { } }
    }
}
