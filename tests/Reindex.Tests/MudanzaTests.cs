using Ondine.Reindex;

namespace Ondine.Reindex.Tests;

/// <summary>
/// El motor de mudanza, compartido por el reordenado de temporadas y por las
/// películas.
///
/// <para>
/// Se extrae en vez de duplicarse porque lo delicado no es mover un fichero: es
/// no pisar nada, llevarse los compañeros, y poder volver atrás. Dos copias de
/// eso divergen, y la que se quede corta lo hará sobre la biblioteca de alguien.
/// </para>
/// </summary>
public static class MudanzaTests
{
    public static void Todas()
    {
        Program.Seccion("El motor de mudanza, compartido");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-mudanza-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(raiz);

            string Poner(string rel, string contenido = "x")
            {
                var p = Path.Combine(raiz, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(p)!);
                File.WriteAllText(p, contenido);
                return p;
            }

            // Una película con su subtítulo al lado, que es lo normal.
            var video = Poner("Blade Runner 1982.mkv");
            var subtitulo = Poner("Blade Runner 1982.srt");
            var destino = Path.Combine(raiz, "Blade Runner (1982)", "Blade Runner (1982).mkv");

            var parte = Mudanza.Aplicar(new[] { (video, destino) });

            Program.Assert(parte.Movidos.Count == 1 && File.Exists(destino),
                "el vídeo llega a su destino");
            Program.Assert(!File.Exists(video), "y deja de estar donde estaba");
            Program.Assert(File.Exists(Path.Combine(raiz, "Blade Runner (1982)", "Blade Runner (1982).srt")),
                "el subtítulo viaja con él, y con el nombre nuevo: si no, deja de encontrarlo el reproductor");
            Program.Assert(!File.Exists(subtitulo), "y tampoco se queda una copia atrás");

            // ── Deshacer ──────────────────────────────────────────────────────
            var vueltos = Mudanza.Deshacer(parte);
            Program.Assert(vueltos == 1 && File.Exists(video),
                "deshacer devuelve el vídeo a su sitio exacto");
            Program.Assert(File.Exists(subtitulo), "y el subtítulo con él");
            Program.Assert(!Directory.Exists(Path.Combine(raiz, "Blade Runner (1982)")),
                "la carpeta que hubo que crear se retira si queda vacía: no se deja basura de un intento");

            // ── Nunca se pisa nada ────────────────────────────────────────────
            var ocupado = Poner(Path.Combine("Ocupada", "ya estaba.mkv"), "no me toques");
            var otro = Poner("otro.mkv");
            var parte2 = Mudanza.Aplicar(new[] { (otro, ocupado) });

            Program.Assert(parte2.Movidos.Count == 0 && parte2.Fallidos.Count == 1,
                "si el destino ya existe no se mueve, y se cuenta como fallido");
            Program.Assert(File.ReadAllText(ocupado) == "no me toques",
                "y lo que había sigue intacto");
            Program.Assert(File.Exists(otro), "y el de origen tampoco se pierde");

            // ── Una carpeta que YA existía no se borra al deshacer ────────────
            var dentro = Path.Combine(raiz, "Ocupada", "otro.mkv");
            var parte3 = Mudanza.Aplicar(new[] { (otro, dentro) });
            Mudanza.Deshacer(parte3);
            Program.Assert(Directory.Exists(Path.Combine(raiz, "Ocupada")),
                "la carpeta que ya existía se queda: borrar lo que no creamos sería destruir");

            // ── Un compañero que no se pudo mover NO se calla ─────────────────
            // Si el destino del subtítulo ya está ocupado, se salta. Antes eso no
            // se contaba en ningún sitio: la ventana decía «2 hechas, 0 fallos» y
            // el subtítulo se quedaba huérfano en otra carpeta, que para Plex es
            // como si no existiera.
            var v2 = Poner("otra.mkv");
            Poner("otra.srt");
            var destino2 = Path.Combine(raiz, "Otra (2020)", "Otra (2020).mkv");
            Poner(Path.Combine("Otra (2020)", "Otra (2020).srt"), "ya estaba aqui");

            var parte4 = Mudanza.Aplicar(new[] { (v2, destino2) });
            Program.Assert(parte4.Movidos.Count == 1, "el vídeo sí llega");
            Program.Assert(parte4.CompanerosSinMover.Count == 1,
                "y se dice que un compañero se quedó atrás, en vez de callarlo");
            Program.Assert(parte4.CompanerosSinMover[0].EndsWith("otra.srt", StringComparison.Ordinal),
                "y cuál fue");

            // ── Deshacer a medias se puede reintentar ─────────────────────────
            // Un vídeo abierto en el reproductor no puede volver. Antes de esto,
            // la ventana tiraba el registro y ese fichero se quedaba desplazado
            // para siempre, sin forma de recuperarlo desde la app.
            var v3 = Poner("bloqueada.mkv");
            var destino3 = Path.Combine(raiz, "Bloqueada (1999)", "Bloqueada (1999).mkv");
            var parte5 = Mudanza.Aplicar(new[] { (v3, destino3) });
            Program.Assert(parte5.Movidos.Count == 1, "se mueve");

            // Bloquear un fichero abierto solo impide moverlo en Windows: en Linux
            // se puede renombrar un fichero que alguien tiene abierto, así que
            // allí no hay forma de provocar el fallo parcial. Se dice en voz alta
            // en vez de saltarlo callando, que un salto silencioso se lee como
            // cobertura.
            if (OperatingSystem.IsWindows())
            {
                int bloqueado;
                using (File.Open(destino3, FileMode.Open, FileAccess.Read, FileShare.None))
                    bloqueado = Mudanza.Deshacer(parte5);

                Program.Assert(bloqueado == 0, "con el fichero en uso no puede volver");
                Program.Assert(parte5.Movidos.Count == 1,
                    "pero el registro NO se destruye: sin él, ese fichero se queda desplazado para siempre");
            }
            else
            {
                Console.WriteLine("  · saltado: bloquear un fichero solo impide moverlo en Windows");
            }

            // Esto sí vale en las dos: deshacer es idempotente y se puede
            // reintentar, que es lo que hace seguro conservar el registro.
            Program.Assert(parte5.Movidos.Count == 1, "el registro sigue entero para reintentar");
            Program.Assert(Mudanza.Deshacer(parte5) >= 0 && File.Exists(v3),
                "reintentar lo completa y el vídeo está de vuelta");
            Program.Assert(Mudanza.Deshacer(parte5) == 0,
                "y un tercer intento no hace nada ni rompe: lo que ya volvió se salta solo");
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch { }
        }
    }
}
