using Ondine.Localizacion;

namespace Ondine;

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
    /// <summary>El título que trae la pista («Castellano AMZN», «Forzados»), si lo trae.</summary>
    public string Titulo { get; init; } = "";

    /// <summary>La que el reproductor pone por defecto.</summary>
    public bool EsPredeterminada { get; init; }

    /// <summary>Subtítulos «forzados»: solo traducen lo imprescindible, no todo el diálogo.</summary>
    public bool EsForzada { get; init; }

    /// <summary>
    /// «Audio · Español · «Castellano AMZN» · 2 canales · 128 kbps · aac · predeterminada».
    ///
    /// El orden no es casual: primero lo que te dice QUÉ es (tipo, idioma, título) y al final el
    /// detalle técnico. Un código como «spa» no sirve para elegir, y lo que sobra —un idioma sin
    /// declarar, un caudal de cero— no se enseña: parece un error y no aporta.
    /// </summary>
    public string Resumen
    {
        get
        {
            var t = Textos.Instancia;
            var partes = new List<string>
            {
                Tipo switch
                {
                    TipoPista.Video => t.PistasTipoVideo,
                    TipoPista.Audio => t.PistasTipoAudio,
                    TipoPista.Subtitulo => t.PistasTipoSubtitulo,
                    _ => t.PistasTipoOtra,
                },
            };
            // El nombre del idioma sale de `Idiomas`, que lee de la misma lista que el
            // selector de la interfaz: así se traduce con ella y no hay dos que discrepen.
            var idioma = Idiomas.Nombre(Idioma);
            if (idioma.Length > 0) partes.Add(idioma);
            if (Titulo.Length > 0) partes.Add(string.Format(t.PistasTituloEntrecomillado, Titulo));
            if (EsForzada) partes.Add(t.PistasForzados);
            if (Canales is > 0) partes.Add(string.Format(t.PistasCanales, Canales));
            // A partir de 1 kbps: un subtítulo de texto declara ~76 bps y sacar «0 kbps» es tan
            // confuso como sacar el cero a secas.
            if (BitsPorSegundo is >= 1000) partes.Add($"{BitsPorSegundo / 1000} {t.PistasKbps}");
            if (Codec.Length > 0) partes.Add(Codec);
            if (EsPredeterminada) partes.Add(t.PistasPredeterminada);
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
