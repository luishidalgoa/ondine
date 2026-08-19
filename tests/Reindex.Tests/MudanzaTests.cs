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
        }
        finally
        {
            try { Directory.Delete(raiz, true); } catch { }
        }
    }
}
