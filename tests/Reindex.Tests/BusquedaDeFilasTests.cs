using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Buscar en la tabla de Organizar.
///
/// <para>
/// Parece trivial y no lo es: la regla que de verdad importa es que <b>una fila ya
/// aplicada solo se encuentra por su nombre NUEVO</b>. El viejo ya no existe en el disco,
/// y que siguiera apareciendo al buscarlo hacía dudar de si el renombrado había ocurrido
/// de verdad. Eso estaba escrito en un comentario dentro de un delegado dentro de
/// <c>AplicarFiltro</c>, y no lo comprobaba nada.
/// </para>
/// </summary>
public static class BusquedaDeFilasTests
{
    public static void Todas()
    {
        Program.Seccion("Buscar en la tabla de Organizar");

        var sinAplicar = new FilaBuscable(
            Aplicado: false,
            Original: "Shin chan 517.mkv",
            Propuesta: "Crayon Shin-Chan S01E517 - ¡En busca de una sonrisa!.mkv",
            NombreNuevo: null);

        // ── Sin texto pasa todo: el filtro vacío no filtra ────────────────────────
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, ""),
            "sin nada escrito no se esconde ninguna fila");
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, "   "),
            "y unos espacios tampoco son una búsqueda");

        // ── Se busca por lo viejo Y por lo propuesto ──────────────────────────────
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, "517"),
            "se encuentra por el nombre que tiene ahora");
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, "sonrisa"),
            "y por el que va a tener: buscas lo que recuerdas, no lo que hay en disco");

        // ── La normalización del identificador ────────────────────────────────────
        // Es la misma que usa el cotejo, y por eso «sonrisa» encuentra «¡...sonrisa!»
        // aunque lleve signos, y «shin chan» encuentra «Shin-Chan».
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, "SONRISA"),
            "las mayúsculas no cuentan");
        Program.Assert(BusquedaDeFilas.Pasa(sinAplicar, "shin chan"),
            "ni el guion frente al espacio: se compara normalizado, como el cotejo");

        Program.Assert(!BusquedaDeFilas.Pasa(sinAplicar, "doraemon"),
            "y lo que no está, no está");

        // ══ LA REGLA ══════════════════════════════════════════════════════════════
        var aplicada = new FilaBuscable(
            Aplicado: true,
            Original: "Shin chan 517.mkv",
            Propuesta: "Crayon Shin-Chan S01E517 - ¡En busca de una sonrisa!.mkv",
            NombreNuevo: "Crayon Shin-Chan S01E517 - ¡En busca de una sonrisa!.mkv");

        Program.Assert(BusquedaDeFilas.Pasa(aplicada, "sonrisa"),
            "una fila ya aplicada se encuentra por su nombre nuevo, que es el que existe");

        Program.Assert(!BusquedaDeFilas.Pasa(aplicada, "Shin chan 517"),
            "y NO por el viejo: ese nombre ya no está en el disco, y verlo hacía dudar " +
            "de si el renombrado había ocurrido");

        // Si se aplicó pero no se sabe el nombre nuevo, se cae al original en vez de
        // dejar la fila inencontrable. Una fila que no aparece nunca es peor que una
        // que aparece por un nombre viejo.
        var aplicadaSinNombre = aplicada with { NombreNuevo = null };
        Program.Assert(BusquedaDeFilas.Pasa(aplicadaSinNombre, "Shin chan 517"),
            "sin nombre nuevo se busca por el original: mejor eso que una fila que no aparece nunca");

        // ── La consulta preparada da lo mismo que la suelta ───────────────────────
        // La pantalla normaliza una vez y la pasa hecha, porque el filtro corre a cada
        // tecla sobre la tabla entera. Si las dos vías no coincidieran, el atajo estaria
        // cambiando lo que se ve.
        foreach (var texto in new[] { "", "  ", "sonrisa", "SHIN CHAN", "doraemon", "517" })
        {
            var preparada = BusquedaDeFilas.Consulta.De(texto);
            Program.Assert(
                BusquedaDeFilas.Pasa(sinAplicar, preparada) == BusquedaDeFilas.Pasa(sinAplicar, texto) &&
                BusquedaDeFilas.Pasa(aplicada, preparada) == BusquedaDeFilas.Pasa(aplicada, texto),
                $"la consulta preparada decide igual que la suelta para «{texto}»");
        }
    }
}
