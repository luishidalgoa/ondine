using Ondine.Localizacion;

namespace Ondine;

/// <summary>
/// Catálogos de sugerencias para los campos Buscar y Reemplazar por.
///
/// <para>
/// Son propiedades y no campos <c>static readonly</c>: una lista construida una
/// sola vez se quedaría con las descripciones del idioma que hubiera al arrancar,
/// y al cambiar de idioma el desplegable seguiría en el anterior. Se rehacen en
/// cada acceso, que ocurre al abrir la ventana de renombrado y no en bucle.
/// </para>
/// </summary>
public static class Suggestions
{
    /// <summary>Patrones frecuentes para el campo «Buscar» (todos son regex).</summary>
    public static IReadOnlyList<SuggestionItem> Search
    {
        get
        {
            var t = Textos.Instancia;
            return new List<SuggestionItem>
            {
                new() { Text = "^",                          Desc = t.SugerenciaInicioNombre, Enables = "regex" },
                new() { Text = "$",                          Desc = t.SugerenciaFinNombre, Enables = "regex" },
                new() { Text = ".*",                         Desc = t.SugerenciaTodoElTexto, Enables = "regex" },
                new() { Text = "(.*)",                       Desc = t.SugerenciaCapturaNombre, Enables = "regex" },
                new() { Text = "^.*$",                       Desc = t.SugerenciaNombreCompleto, Enables = "regex" },
                new() { Text = @"\d+",                       Desc = t.SugerenciaDigitos, Enables = "regex" },
                new() { Text = @"\s+",                       Desc = t.SugerenciaEspacios, Enables = "regex" },
                new() { Text = "[._-]+",                     Desc = t.SugerenciaSeparadores, Enables = "regex" },
                new() { Text = @"\[.*?\]",                   Desc = t.SugerenciaCorchetes, Enables = "regex" },
                new() { Text = @"\(.*?\)",                   Desc = t.SugerenciaParentesis, Enables = "regex" },
                new() { Text = "^.{3}",                      Desc = t.SugerenciaTresPrimeros, Enables = "regex" },
                new() { Text = ".{3}$",                      Desc = t.SugerenciaTresUltimos, Enables = "regex" },
                new() { Text = "^foo",                       Desc = t.SugerenciaEmpiezaPor, Enables = "regex" },
                new() { Text = "bar$",                       Desc = t.SugerenciaTerminaEn, Enables = "regex" },
                new() { Text = "^foo.*bar$",                 Desc = t.SugerenciaEmpiezaYAcaba, Enables = "regex" },
                new() { Text = ".+?(?=bar)",                 Desc = t.SugerenciaAnteriorA, Enables = "regex" },
                new() { Text = @"foo[\s\S]*bar",             Desc = t.SugerenciaEntreDos, Enables = "regex" },
                new() { Text = @"(\d{2})-(\d{2})-(\d{4})",   Desc = t.SugerenciaFecha, Enables = "regex" },
                new() { Text = @"[Ss](\d{1,2})[Ee](\d{1,2})", Desc = t.SugerenciaTemporadaEpisodio, Enables = "regex" },
                new() { Text = "1080p|720p|480p|2160p|4K",   Desc = t.SugerenciaResolucion, Enables = "regex" },
                new() { Text = "x264|x265|HEVC|WEB-DL|BluRay", Desc = t.SugerenciaCodecFuente, Enables = "regex" },
            };
        }
    }

    /// <summary>Variables disponibles para el campo «Reemplazar por».</summary>
    public static IReadOnlyList<SuggestionItem> Replace
    {
        get
        {
            var t = Textos.Instancia;
            return new List<SuggestionItem>
            {
                // grupos de captura
                new() { Text = "$1", Desc = t.SugerenciaGrupo1, Enables = "regex" },
                new() { Text = "$2", Desc = t.SugerenciaGrupo2, Enables = "regex" },
                new() { Text = "$3", Desc = t.SugerenciaGrupo3, Enables = "regex" },
                new() { Text = "$$", Desc = t.SugerenciaDolarLiteral },
                // contadores
                new() { Text = "${}",                                  Desc = t.SugerenciaContadorSimple, Enables = "enum" },
                new() { Text = "${start=1}",                           Desc = t.SugerenciaContadorDesdeUno, Enables = "enum" },
                new() { Text = "${padding=2;start=1}",                 Desc = t.SugerenciaContadorDosCifras, Enables = "enum" },
                new() { Text = "${padding=3;start=1}",                 Desc = t.SugerenciaContadorTresCifras, Enables = "enum" },
                new() { Text = "${increment=2}",                       Desc = t.SugerenciaContadorDeDosEnDos, Enables = "enum" },
                new() { Text = "${padding=4;increment=2;start=10}",    Desc = t.SugerenciaContadorCombinado, Enables = "enum" },
                // fecha del archivo original
                new() { Text = "$YYYY", Desc = t.SugerenciaAnio4 },
                new() { Text = "$YY",   Desc = t.SugerenciaAnio2 },
                new() { Text = "$Y",    Desc = t.SugerenciaAnio1 },
                new() { Text = "$MMMM", Desc = t.SugerenciaMesNombre },
                new() { Text = "$MMM",  Desc = t.SugerenciaMesAbreviado },
                new() { Text = "$MM",   Desc = t.SugerenciaMesCero },
                new() { Text = "$M",    Desc = t.SugerenciaMesSinCero },
                new() { Text = "$DDDD", Desc = t.SugerenciaDiaSemana },
                new() { Text = "$DDD",  Desc = t.SugerenciaDiaSemanaAbreviado },
                new() { Text = "$DD",   Desc = t.SugerenciaDiaCero },
                new() { Text = "$D",    Desc = t.SugerenciaDiaSinCero },
                new() { Text = "$hh",   Desc = t.SugerenciaHoraCero },
                new() { Text = "$h",    Desc = t.SugerenciaHoraSinCero },
                new() { Text = "$mm",   Desc = t.SugerenciaMinutosCero },
                new() { Text = "$m",    Desc = t.SugerenciaMinutosSinCero },
                new() { Text = "$ss",   Desc = t.SugerenciaSegundosCero },
                new() { Text = "$s",    Desc = t.SugerenciaSegundosSinCero },
                new() { Text = "$fff",  Desc = t.SugerenciaMilisegundos3 },
                new() { Text = "$ff",   Desc = t.SugerenciaMilisegundos2 },
                new() { Text = "$f",    Desc = t.SugerenciaMilisegundos1 },
                // aleatorios
                new() { Text = "${rstringalnum=8}", Desc = t.SugerenciaAleatorioAlfanumerico, Enables = "rand" },
                new() { Text = "${rstringalpha=8}", Desc = t.SugerenciaAleatorioLetras, Enables = "rand" },
                new() { Text = "${rstringdigit=6}", Desc = t.SugerenciaAleatorioDigitos, Enables = "rand" },
                new() { Text = "${ruuidv4}",        Desc = t.SugerenciaUuid, Enables = "rand" },
            };
        }
    }
}

/// <summary>
/// Desplegable de autocompletado reactivo para un TextBox: se abre al enfocar/pulsar,
/// filtra según se escribe y permite elegir con ratón o teclado (↑ ↓ Entrar Esc).
/// </summary>
