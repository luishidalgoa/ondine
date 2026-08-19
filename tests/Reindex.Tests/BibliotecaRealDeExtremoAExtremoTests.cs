using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// La prueba que de verdad garantiza: se replica una biblioteca REAL de 75
/// películas en un temporal, se aplica el plan <b>de verdad sobre el disco</b>, y
/// se comprueba que no se pierde ni un fichero y que deshacer lo devuelve todo
/// exactamente a donde estaba.
///
/// <para>
/// Los nombres son los de una biblioteca de verdad —con sus dobles espacios, sus
/// paréntesis pegados, sus acentos y sus carpetas de colección—, copiados tal
/// cual. Los ficheros son de un byte: lo que se prueba son los NOMBRES, y copiar
/// gigas no probaría nada más.
/// </para>
/// <para>
/// Encima se añaden a mano los casos que la biblioteca real no tiene y que sí
/// hacen daño: subtítulos, extras, una película partida en dos, y dos vídeos
/// donde el nombre de uno es prefijo del otro.
/// </para>
/// </summary>
public static class BibliotecaRealDeExtremoAExtremoTests
{
    /// <summary>Los 75 ficheros de la biblioteca real, tal y como se llaman.</summary>
    private static readonly string[] Reales =
    {
        @"Alfredo Landa/Vente a Alemania Pepe (1971).mp4",
        @"Bob Esponja/Bob Esponja Al rescate de Fondo de Bikini La película de Arenita Mejillas.mkv",
        @"Bob Esponja/Bob Esponja El Mundo Guante Por Siempre.avi",
        @"Bob Esponja/Bob Esponja Historia Marina.avi",
        @"Bob Esponja/Bob Esponja Navidad (2003).avi",
        @"Bob Esponja/Bob Esponja Plankton La Película 2025.mkv",
        @"Bob Esponja/Bob Esponja Un Heroe Al Rescate (2020).mkv",
        @"Bob Esponja/Bob Esponja Un Héroe Fuera Del Agua (2015).mp4",
        @"Bob Esponja/Bob Esponja Una Aventura Pirata (2025).mp4",
        @"Bob Esponja/Bob Esponja y La Gran Ola.avi",
        @"Bob Esponja/Bob esponja Patricio esponja.avi",
        @"Cadena perpetua (1994)/Cadena Perpetua (Frank Darabont, 1994).mkv",
        @"Charlie y la Fabrica de Chocolate/Charlie y la Fabrica de Chocolate.avi",
        @"Disney/101 Dalmatas (1961).avi",
        @"Disney/101 Dalmatas 2.mp4",
        @"Disney/102 Dalmatas.avi",
        @"Disney/Bambi (1942).mp4",
        @"Disney/Blancanieves y los siete enanitos (1937).mp4",
        @"Disney/Buscando A Dory.mp4",
        @"Disney/Buscando a Nemo.avi",
        @"Disney/Cars 2.mp4",
        @"Disney/Cars 3.mp4",
        @"Disney/Chicken Little.mkv",
        @"Disney/El Rey Leon 2 1080P.mp4",
        @"Disney/El Rey Leon 3.mp4",
        @"Disney/El Rey leon.mp4",
        @"Disney/Frozen El reino del hielo.avi",
        @"Disney/Frozen II.mkv",
        @"Disney/High School Musical 2 (2007).mp4",
        @"Disney/La Bella y la Bestia (1991).mp4",
        @"Disney/Los Increibles 2.avi",
        @"Disney/Monstruos University.avi",
        @"Disney/Robin Hood (1973).avi",
        @"Disney/Sirenita 2.mkv",
        @"Disney/Sirenita 3.mkv",
        @"Disney/Toy Story 1.mp4",
        @"Disney/Toy Story 3 (2010).avi",
        @"Disney/Toy Story 4.mp4",
        @"Disney/Up.mp4",
        @"Dream Works Animation/Como entrenar a tu dragon 2.mp4",
        @"Duplex (2003)/Duplex (2003).avi",
        @"El lobo de wall street/El lobo de wall street.mp4",
        @"El pasajero/The commuter.mp4",
        @"En busca de la felicidad/En busca de la felicidad.mp4",
        @"Fatima, La Pelicula (2020)/Fatima, La Pelicula (2020).avi",
        @"Ghost Mas Alla Del Amor (1990)/Ghost Mas Alla Del Amor (1990).mp4",
        @"Gladiator/Gladiator Ii (2024).mp4",
        @"Gladiator/Gladiator.mp4",
        @"Grease (1978)/Grease.mp4",
        @"IT/It (2017).mp4",
        @"La Niñera Magica Y El Big Bang/La Niñera Magica Y El Big Bang.avi",
        @"La pasión de cristo/La pasion de cristo.mp4",
        @"Los miserables/Los Miserables.mp4",
        @"Non-Stop/Non-Stop.mp4",
        @"Orgullo y prejuicio (2005)/Orgullo y prejuicio (2005).mp4",
        @"Paco martinez soria/El turismo es un gran invento.avi",
        @"Paco martinez soria/Es peligroso casarse a los 60.avi",
        @"Paco martinez soria/Estoy hecho un chaval.avi",
        @"Paco martinez soria/Hay que educar a papa.avi",
        @"Paco martinez soria/La ciudad no es para mí.avi",
        @"Paco martinez soria/La tía de Carlos.avi",
        @"Paco martinez soria/Qué hacemos con los hijos.avi",
        @"Paco martinez soria/Se armó el Belén.avi",
        @"Piratas del caribe/Piratas Del Caribe  La Venganza De Salazar.mp4",
        @"Piratas del caribe/Piratas Del Caribe La Maldicion De La Perla Negra.mp4",
        @"Polar Express/Polar Express.mkv",
        @"Titanic/Titanic.mp4",
        @"Torrente/Torrente 2.mp4",
        @"Torrente/Torrente 4 Lethal Crisis.avi",
        @"Torrente/Torrente 5.mp4",
        @"Torrente/Torrente El Brazo Armado De La Ley.mp4",
        @"Torrente/Torrente Presidente.mkv",
        @"Una cuestion de tiempo(2013)/Una cuestion de tiempo(2013).mkv",
        @"Yo creo en ti (1948)/Yo creo en ti (1948).mp4",
        @"¡Qué bello es vivir! (1946)/¡Qué bello es vivir! (1946).mkv"
    };

    /// <summary>Lo que la biblioteca real NO tiene y hay que probar igual.</summary>
    private static readonly string[] Anadidos =
    {
        // Un vídeo cuyo nombre base es prefijo del de otro: el caso que movía
        // uno dentro de la carpeta del otro.
        @"Trampas/Up.mkv",
        @"Trampas/Up.2009.mkv",
        // Con sus compañeros, que sí deben viajar.
        @"Trampas/Up.srt",
        @"Trampas/Up.es.srt",
        @"Trampas/Up.nfo",
        // Extras junto a una película ya bien puesta.
        @"Gladiator (2000)/Gladiator (2000).mkv",
        @"Gladiator (2000)/Gladiator (2000)-trailer.mp4",
        @"Gladiator (2000)/Gladiator (2000)-behindthescenes.mp4",
        // Una película partida en dos.
        @"Lo que el viento se llevo (1939)/Lo que el viento se llevo (1939) cd1.avi",
        @"Lo que el viento se llevo (1939)/Lo que el viento se llevo (1939) cd2.avi",
    };

    private static readonly string[] Videos = { ".mkv", ".mp4", ".avi", ".m4v" };

    public static void Todas()
    {
        Program.Seccion("Una biblioteca real, de extremo a extremo");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-e2e-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            // ── Se replica la biblioteca ──────────────────────────────────────
            var todos = Reales.Concat(Anadidos).ToArray();
            foreach (var rel in todos)
            {
                var p = Path.Combine(raiz, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(p)!);
                File.WriteAllText(p, rel);   // el contenido ES su ruta: así se sabe quién es quién
            }

            var antes = Huella(raiz);
            Program.Assert(antes.Count == todos.Length,
                $"se replican los {todos.Length} ficheros");

            // ── Se monta el plan y se aplica DE VERDAD ────────────────────────
            var videos = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
                .Where(f => Videos.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var plan = PlanDePeliculas.Montar(videos, raiz);
            var pares = plan
                .Where(p => p.Motivo is PlanDePeliculas.Porque.Va or PlanDePeliculas.Porque.EnColeccion
                            && p.Destino is not null)
                .Select(p => (p.Origen, p.Destino!))
                .ToList();

            var parte = Mudanza.Aplicar(pares);

            // ── Lo que NO se puede haber roto ─────────────────────────────────
            var despues = Huella(raiz);

            Program.Assert(despues.Count == antes.Count,
                $"no se pierde ni un fichero: había {antes.Count}, quedan {despues.Count}");

            // El contenido de cada fichero dice quién era. Si alguno cambió de
            // identidad, es que se sobrescribió algo.
            Program.Assert(despues.Values.OrderBy(v => v, StringComparer.Ordinal)
                             .SequenceEqual(antes.Values.OrderBy(v => v, StringComparer.Ordinal)),
                "y son exactamente los mismos: nada se ha sobrescrito");

            Program.Assert(parte.Fallidos.Count == 0,
                $"ninguno falla al moverse ({parte.Fallidos.Count} fallidos)");

            // Ningún vídeo puede haber acabado en la carpeta de OTRA película. Es
            // la comprobación por la que existe todo esto: el fallo que se
            // arregló hacía exactamente eso.
            var intrusos = despues.Keys
                .Where(r => Videos.Contains(Path.GetExtension(r).ToLowerInvariant()))
                .Select(r => (Ruta: r, Carpeta: Path.GetFileName(Path.GetDirectoryName(r)) ?? ""))
                .Where(x => x.Carpeta.EndsWith(")", StringComparison.Ordinal))   // solo las canónicas
                .Where(x => !Path.GetFileNameWithoutExtension(x.Ruta)
                                 .StartsWith(x.Carpeta, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Program.Assert(intrusos.Count == 0,
                intrusos.Count == 0
                    ? "ningún vídeo acaba en la carpeta de otra película"
                    : "hay vídeos en la carpeta equivocada: "
                      + string.Join(" · ", intrusos.Take(3).Select(x => $"{Path.GetFileName(x.Ruta)} en {x.Carpeta}")));

            // Los extras no se han tocado.
            Program.Assert(despues.Keys.Any(k => k.EndsWith("-trailer.mp4", StringComparison.Ordinal)),
                "el tráiler sigue llamándose tráiler");

            // ── Deshacer devuelve TODO a su sitio exacto ──────────────────────
            Mudanza.Deshacer(parte);
            var vuelta = Huella(raiz);

            Program.Assert(vuelta.Count == antes.Count, "tras deshacer siguen estando todos");

            var faltan = antes.Keys.Where(k => !vuelta.ContainsKey(k)).Take(3).ToList();
            Program.Assert(faltan.Count == 0,
                faltan.Count == 0
                    ? "y cada uno está exactamente donde estaba"
                    : "no volvieron a su sitio: " + string.Join(" · ", faltan));
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch { }
        }
    }

    /// <summary>Ruta relativa → quién es (su ruta de partida, escrita dentro).</summary>
    private static Dictionary<string, string> Huella(string raiz) =>
        Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(raiz, f), File.ReadAllText,
                          StringComparer.OrdinalIgnoreCase);
}
