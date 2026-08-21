using Ondine.Objetivo;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Llegar a un tamaño concreto.
///
/// <para>
/// «Que quepa en un pendrive de 8 GB», «que entre en el límite de subida». Es la petición
/// más común de un compresor y la que Ondine no sabía hacer: hasta ahora se elegía calidad y
/// se veía qué salía.
/// </para>
/// <para>
/// <b>Lo que de verdad hay que acertar no es la cuenta, es decir que NO cabe.</b> Pedir 50 MB
/// para dos horas se puede «cumplir» dando 40 kbps de vídeo, y sale un puré ilegible que
/// técnicamente pesa lo pedido. Eso es peor que negarse: has esperado la codificación entera
/// para tirar el resultado. Así que hay un suelo, y por debajo se dice que no y por qué.
/// </para>
/// </summary>
public static class TamanoObjetivoTests
{
    private const long MB = 1024L * 1024L;

    public static void Todas()
    {
        Program.Seccion("Llegar a un tamaño concreto");

        // ── La cuenta ─────────────────────────────────────────────────────────────
        // 100 MB para 600 s son ~1398 kbps en total. Quitando 128 de audio y el 2% que
        // se lleva el contenedor, quedan ~1242 para el vídeo.
        var normal = TamanoObjetivo.Calcular(100 * MB, duracionSeg: 600, audioKbps: 128);

        Program.Assert(normal.Cabe, "100 MB para 10 minutos caben de sobra");
        Program.Assert(Math.Abs(normal.VideoKbps - 1242) <= 15,
            $"la cuenta sale: {normal.VideoKbps} kbps de vídeo (esperado ~1242)");
        Program.Assert(normal.AudioKbps == 128, "y el audio se respeta tal cual: no se toca");

        // ── El contenedor se DESCUENTA, no se suma ────────────────────────────────
        // Es el error clásico: reservar el margen sumándolo deja el fichero pasándose
        // justo del límite, que es el único sitio donde importa.
        var conMargen = TamanoObjetivo.Calcular(100 * MB, 600, 0);
        var sinContar = (int)(100.0 * MB * 8 / 600 / 1000);
        Program.Assert(conMargen.VideoKbps < sinContar,
            "el margen del contenedor se resta: sumarlo dejaría el fichero pasado de largo");

        // ── Cuando NO cabe ────────────────────────────────────────────────────────
        // 12 MB para 10 minutos: el audio SÍ cabe -son unos 9,5 MB- pero al vídeo le
        // quedan ~36 kbps. La cuenta «funciona»; el resultado sería ilegible.
        //
        // La primera versión de esta comprobación usaba 5 MB para dos horas y fallaba: ahí
        // ni el audio cabe, así que probaba OTRA rama. Un caso mal elegido puede pasar por
        // bueno años enteros comprobando algo distinto de lo que dice comprobar.
        var imposible = TamanoObjetivo.Calcular(12 * MB, duracionSeg: 600, audioKbps: 128);
        Program.Assert(!imposible.Cabe && imposible.Porque == PorQueNoCabe.NoLlegaAlMinimo,
            "un objetivo que dejaría el vídeo por debajo del mínimo se rechaza, no se sirve en puré");

        // Y se dice CUÁNTO haría falta, que es lo único accionable: sin ese número, el
        // usuario solo puede ir probando cifras a ciegas.
        Program.Assert(imposible.MinimoNecesarioBytes > 12 * MB,
            "y se dice cuánto haría falta como mínimo, para no tener que adivinar");

        // El audio solo ya se pasa: ni con vídeo de cero cabría.
        var soloAudioSePasa = TamanoObjetivo.Calcular(1 * MB, duracionSeg: 3600, audioKbps: 192);
        Program.Assert(!soloAudioSePasa.Cabe && soloAudioSePasa.Porque == PorQueNoCabe.NoCabeNiElAudio,
            "si el audio solo ya no cabe, se dice eso y no «baja la calidad»: bajarla no arreglaría nada");

        // ── Sin duración no se promete nada ───────────────────────────────────────
        var sinDuracion = TamanoObjetivo.Calcular(100 * MB, duracionSeg: 0, audioKbps: 128);
        Program.Assert(!sinDuracion.Cabe && sinDuracion.Porque == PorQueNoCabe.SinDuracion,
            "sin saber cuánto dura no hay cuenta que hacer, y se admite en vez de inventar");

        var duracionNegativa = TamanoObjetivo.Calcular(100 * MB, -5, 128);
        Program.Assert(duracionNegativa.Porque == PorQueNoCabe.SinDuracion,
            "una duración imposible es lo mismo que no tenerla");

        // ── El objetivo en el límite ──────────────────────────────────────────────
        // Justo en el mínimo cabe. Es el borde, y es donde una comparación mal puesta
        // deja fuera lo que sí valía.
        var justo = TamanoObjetivo.Calcular(
            TamanoObjetivo.Calcular(1, 600, 128).MinimoNecesarioBytes, 600, 128);
        Program.Assert(justo.Cabe,
            "el tamaño mínimo que se anuncia como suficiente TIENE que caber: si no, la cifra miente");

        // ── Y al revés: un objetivo enorme no se traga el disco ───────────────────
        // Pedir 10 GB para un clip corto no debe dar un bitrate absurdo que tarde horas
        // y no mejore nada: por encima del original no hay calidad que ganar.
        var enorme = TamanoObjetivo.Calcular(10240L * MB, duracionSeg: 20, audioKbps: 128,
                                             bitrateOriginalKbps: 2000);
        Program.Assert(enorme.Cabe && enorme.VideoKbps <= 2000,
            "no se recomprime «hacia arriba»: pasado el bitrate del original no se gana nada");

        // Sin saber el bitrate del original no se recorta nada, que es lo honesto.
        var sinReferencia = TamanoObjetivo.Calcular(10240L * MB, 20, 128);
        Program.Assert(sinReferencia.VideoKbps > 2000,
            "y sin conocer el original no se inventa un tope");
    }
}
