using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Lo que la pantalla de Organizar DECIDE, separado de lo que pinta.
///
/// <para>
/// Estas reglas llevaban tiempo dentro de <c>ActualizarContadores</c>, mezcladas con las
/// asignaciones a etiquetas y botones. Tenían su porqué escrito en comentarios —«aplicar
/// nunca lleva sorpresa dentro», «partir solo tiene sentido sobre lo ya identificado»— y
/// ninguna prueba. Un motivo escrito que nada comprueba es un motivo que se pierde en el
/// primer refactor.
/// </para>
/// <para>
/// No se prueban textos: el resumen decide <b>cuántos</b> y <b>qué modo</b>, y la pantalla
/// se encarga de redactarlo. Así la regla vale igual en WPF que en cualquier otra cosa.
/// </para>
/// </summary>
public static class ResumenDelLoteTests
{
    private static FilaDelLote Fila(
        ReindexEstado estado = ReindexEstado.Limpio,
        bool listo = false, bool marcado = false, bool duda = false,
        bool sinCambios = false, bool partible = false) =>
        new(estado, listo, marcado, duda, sinCambios, partible);

    public static void Todas()
    {
        Program.Seccion("El resumen del lote: qué se enciende y qué se dice");

        // ── El recuento por estado ────────────────────────────────────────────────
        var mezcla = new[]
        {
            Fila(ReindexEstado.Limpio),
            Fila(ReindexEstado.Limpio),
            Fila(ReindexEstado.Corregido, listo: true, marcado: true),
            Fila(ReindexEstado.Especial),
            Fila(ReindexEstado.Conflicto, duda: true),
            Fila(ReindexEstado.Error, duda: true),
        };

        var r = ResumenDelLote.De(mezcla, hayCatalogo: true);

        Program.Assert(r.Total == 6 && r.Limpios == 2 && r.Corregidos == 1 &&
                       r.Especiales == 1 && r.Conflictos == 1 && r.Errores == 1,
            "cada fila cuenta en su estado, y ninguna se pierde por el camino");

        // ── «Aplicar nunca lleva sorpresa dentro» ─────────────────────────────────
        // Es la regla con más motivo detrás: el botón tiene que decir exactamente
        // cuántos ficheros va a tocar. Tres situaciones y tres respuestas distintas.
        var nadaMarcado = ResumenDelLote.De([Fila(listo: true), Fila(listo: true)], true);
        Program.Assert(nadaMarcado.ModoAplicar == ModoDeAplicar.Nada && !nadaMarcado.PuedeAplicar,
            "sin nada marcado el botón no promete nada y no se puede pulsar");

        var todosMarcados = ResumenDelLote.De(
            [Fila(listo: true, marcado: true), Fila(listo: true, marcado: true)], true);
        Program.Assert(todosMarcados.ModoAplicar == ModoDeAplicar.Todos && todosMarcados.Marcados == 2,
            "con todos los listos marcados basta decir cuántos son");

        var aMedias = ResumenDelLote.De(
            [Fila(listo: true, marcado: true), Fila(listo: true), Fila(listo: true)], true);
        Program.Assert(aMedias.ModoAplicar == ModoDeAplicar.Algunos &&
                       aMedias.Marcados == 1 && aMedias.Listos == 3,
            "con listos sin marcar hay que decir «1 de 3»: es donde cabía la sorpresa");

        // Marcar algo que NO está listo no cuenta: se aplican los listos, no los marcados.
        var marcadoPeroNoListo = ResumenDelLote.De([Fila(marcado: true)], true);
        Program.Assert(marcadoPeroNoListo.Marcados == 0 && !marcadoPeroNoListo.PuedeAplicar,
            "marcar algo que no está listo no lo mete en el lote");

        // ── Lo que solo tiene sentido con catálogo y con análisis ─────────────────
        var sinCatalogo = ResumenDelLote.De([Fila()], hayCatalogo: false);
        Program.Assert(!sinCatalogo.PuedeCompararCatalogo && !sinCatalogo.PuedeReordenar,
            "sin catálogo no se sabe de qué temporada es cada fichero: ni comparar ni reordenar");

        var sinNada = ResumenDelLote.De([], hayCatalogo: true);
        Program.Assert(!sinNada.PuedeCompararCatalogo && !sinNada.PuedeReordenar,
            "y con catálogo pero sin analizar tampoco hay nada que comparar ni que ordenar");

        // ── Los chips solo se encienden si hay algo detrás ────────────────────────
        var soloLimpios = ResumenDelLote.De([Fila(ReindexEstado.Limpio)], true);
        Program.Assert(!soloLimpios.PuedeConfirmarEspeciales,
            "un chip que no filtra nada no se puede pulsar: encenderlo sería mentir");

        // ── Partir ────────────────────────────────────────────────────────────────
        var conPartibles = ResumenDelLote.De([Fila(partible: true), Fila()], true);
        Program.Assert(conPartibles.Partibles == 1 && conPartibles.PuedePartir,
            "partir se ofrece cuando hay algo que partir, y dice cuántos");

        // ── «Si la mayoría son dudas, se dice de frente» ──────────────────────────
        // La mitad justa NO avisa: el aviso es para cuando el lote está mayormente en
        // duda, no para cualquier lote repartido.
        var mitadJusta = ResumenDelLote.De([Fila(duda: true), Fila()], true);
        Program.Assert(!mitadJusta.AvisarDeDudas,
            "la mitad justa no dispara el aviso");

        var mayoriaDudas = ResumenDelLote.De([Fila(duda: true), Fila(duda: true), Fila()], true);
        Program.Assert(mayoriaDudas.AvisarDeDudas && mayoriaDudas.Dudas == 2,
            "con la mayoría en duda se dice de frente, en vez de dejar que se descubra fila a fila");

        var loteVacio = ResumenDelLote.De([], true);
        Program.Assert(!loteVacio.AvisarDeDudas,
            "un lote vacío no avisa de nada: 0 de 0 no es «la mayoría»");

        // ── Los que ya estaban bien se cuentan aparte ─────────────────────────────
        // Sin esto, «383 listos · 165 por despachar» sobre 548 deja huecos sin explicar
        // y parece que se han perdido ficheros por el camino.
        var conHechos = ResumenDelLote.De(
            [Fila(sinCambios: true), Fila(sinCambios: true), Fila(listo: true)], true);
        Program.Assert(conHechos.YaBien == 2,
            "los que ya estaban bien se cuentan aparte para que la suma cuadre a la vista");
    }
}
