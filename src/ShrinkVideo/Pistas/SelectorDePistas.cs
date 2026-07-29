namespace ShrinkVideo;

public enum TipoPista { Video, Audio, Subtitulo, Otro }

/// <summary>
/// Una pista del fichero, tal como la ve ffprobe. <see cref="Indice"/> es el índice ABSOLUTO
/// dentro del contenedor, que es con lo que se mapea sin ambigüedad.
/// </summary>
public sealed record Pista(
    int Indice,
    TipoPista Tipo,
    string Codec,
    string Idioma,
    int? Canales,
    long? BitsPorSegundo)
{
    /// <summary>«Audio · aac · spa · 2 canales · 128 kbps» — lo que se lee para decidir.</summary>
    public string Resumen
    {
        get
        {
            var partes = new List<string>
            {
                Tipo switch
                {
                    TipoPista.Video => "Vídeo",
                    TipoPista.Audio => "Audio",
                    TipoPista.Subtitulo => "Subtítulo",
                    _ => "Otra",
                },
            };
            if (Codec.Length > 0) partes.Add(Codec);
            if (Idioma.Length > 0) partes.Add(Idioma);
            if (Canales is > 0) partes.Add($"{Canales} canales");
            if (BitsPorSegundo is > 0) partes.Add($"{BitsPorSegundo / 1000} kbps");
            return string.Join(" · ", partes);
        }
    }
}

/// <summary>
/// Quitar pistas de audio o subtítulos SIN recomprimir.
///
/// Es un «remux»: se copian los paquetes tal cual a un contenedor nuevo dejando fuera las pistas
/// que no quieres. Tarda segundos en vez de minutos y **no pierde un bit de calidad**, porque el
/// vídeo no se vuelve a codificar — solo se reempaqueta.
///
/// De paso ahorra espacio de verdad: un fichero con cinco doblajes puede llevar cientos de megas
/// de audio que no vas a escuchar nunca, y tirarlos no le hace nada a la imagen.
/// </summary>
public static class SelectorDePistas
{
    public sealed record Plan(
        IReadOnlyList<Pista> Conservadas,
        IReadOnlyList<Pista> Quitadas)
    {
        public bool HayCambios => Quitadas.Count > 0;

        /// <summary>
        /// Se queda sin ninguna pista de audio. Es legítimo —un vídeo mudo se puede querer— pero
        /// hay que decirlo antes, porque no es lo que espera casi nadie.
        /// </summary>
        public bool QuedaSinAudio => Conservadas.All(p => p.Tipo != TipoPista.Audio);

        /// <summary>
        /// Cuánto se ahorra, en bytes, según lo que dure el vídeo. Es EL argumento para hacerlo,
        /// así que hay que poder decirlo antes de tocar nada.
        ///
        /// Solo cuenta lo que tiene un caudal declarado: un subtítulo de texto pesa kilobytes, y
        /// prometer un ahorro que no existe es peor que no prometer nada.
        /// </summary>
        public long BytesQueSeAhorran(int duracionSegundos)
        {
            if (duracionSegundos <= 0) return 0;
            long bits = Quitadas.Where(p => p.BitsPorSegundo is > 0)
                                .Sum(p => p.BitsPorSegundo!.Value);
            return bits / 8 * duracionSegundos;
        }

        /// <summary>
        /// Los argumentos de ffmpeg. Se mapea por índice ABSOLUTO lo que se CONSERVA, en vez de
        /// excluir con «-map -0:a:1»: excluir obliga a contar pistas del mismo tipo y se lía en
        /// cuanto hay huecos, mientras que decir exactamente qué entra no admite dudas.
        /// </summary>
        public IReadOnlyList<string> Argumentos(string entrada, string salida)
        {
            var args = new List<string> { "-hide_banner", "-loglevel", "error", "-i", entrada };
            foreach (var p in Conservadas)
            {
                args.Add("-map");
                args.Add($"0:{p.Indice}");
            }
            // «copy» para TODO: ni vídeo, ni audio, ni subtítulos se recodifican.
            args.AddRange(new[] { "-c", "copy" });
            args.Add(salida);
            args.Add("-y");
            return args;
        }
    }

    /// <summary>
    /// Decide qué queda y qué se va. La pista de VÍDEO nunca se quita, aunque se pida: sin ella
    /// el resultado ya no es el vídeo del que se partió, y es un error fácil de cometer marcando
    /// casillas a lo loco.
    /// </summary>
    public static Plan Planificar(IReadOnlyList<Pista> pistas, IReadOnlyCollection<int> indicesAQuitar)
    {
        var quitar = indicesAQuitar.ToHashSet();
        var conservadas = pistas.Where(p => p.Tipo == TipoPista.Video || !quitar.Contains(p.Indice))
                                .OrderBy(p => p.Indice).ToList();
        var quitadas = pistas.Where(p => p.Tipo != TipoPista.Video && quitar.Contains(p.Indice))
                             .OrderBy(p => p.Indice).ToList();
        return new Plan(conservadas, quitadas);
    }
}
