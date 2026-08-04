using System.Text;
using Ondine.Localizacion;

namespace Ondine.Reindex;

/// <summary>
/// Construye el encargo que se le pasa a una IA para que convierta un anexo de episodios
/// (Wikipedia, Fandom, lo que sea) en un catálogo <c>reindex/1.0</c>.
///
/// Existe porque cada anexo está montado a su manera: unos numeran por temporada y otros
/// en continuo, unos llaman a la columna «N.º» y otros «Episodio», unos separan «Título en
/// España» de «Título en Hispanoamérica» y otros solo traen el original. Pedirle a la IA
/// «hazme el JSON» sin más produce catálogos distintos cada vez; el valor está en fijar
/// por escrito las decisiones que si no se toman al azar.
///
/// El esquema va INCRUSTADO en el prompt a propósito: así funciona aunque la IA no pueda
/// abrir la documentación del repositorio.
/// </summary>
public static class CatalogPrompt
{
    /// <summary>Idiomas que se ofrecen, con su etiqueta para la interfaz.</summary>
    /// <summary>
    /// La lista de idiomas vive en <see cref="IsoLanguages"/>, que es la norma ISO entera.
    /// Aquí había siete a mano, y dos ni siquiera eran códigos de idioma.
    /// </summary>
    public static string Nombre(string codigo) => IsoLanguages.Nombre(codigo);

    /// <summary>
    /// Redacta el encargo. <paramref name="comparar"/> son los idiomas que el catálogo debe
    /// incluir para PODER reconocer los ficheros; <paramref name="salida"/> es el que se
    /// escribirá en el nombre final.
    /// </summary>
    public static string Build(string serie, string fuente, string salida, IReadOnlyList<string> comparar)
    {
        var t = Textos.Instancia;

        serie = string.IsNullOrWhiteSpace(serie) ? t.EncargoSerieHueco : serie.Trim();
        fuente = string.IsNullOrWhiteSpace(fuente) ? t.EncargoFuenteHueco : fuente.Trim();
        salida = string.IsNullOrWhiteSpace(salida) ? "es" : salida.Trim();

        // El de salida SIEMPRE se incluye entre los comparables: sería absurdo escribir un
        // título que el motor no sabe reconocer.
        // Se normalizan aquí para que el encargo pida siempre códigos ISO: si alguien trae un
        // «jp» de los de antes, la IA no debe aprenderlo y perpetuarlo en el catálogo nuevo.
        var idiomas = new List<string> { IsoLanguages.Normalizar(salida) };
        foreach (var c in comparar)
        {
            var n = IsoLanguages.Normalizar(c);
            if (n.Length > 0 && !idiomas.Contains(n, StringComparer.OrdinalIgnoreCase)) idiomas.Add(n);
        }

        var listaIdiomas = string.Join(", ", idiomas.Select(c => $"`{c}` ({Nombre(c)})"));
        var jsonComparar = string.Join(", ", idiomas.Select(c => $"\"{c}\""));

        var sb = new StringBuilder();

        sb.AppendLine(t.EncargoIntro);
        sb.AppendLine();
        sb.AppendLine(string.Format(t.EncargoSerieLinea, serie));
        sb.AppendLine(string.Format(t.EncargoFuenteLinea, fuente));
        sb.AppendLine();
        sb.AppendLine(t.EncargoLeeEntera);
        sb.AppendLine();

        sb.AppendLine(t.EncargoIdiomasTitulo);
        sb.AppendLine();
        sb.AppendLine(string.Format(t.EncargoIdiomasIncluye, listaIdiomas));
        sb.AppendLine();
        sb.AppendLine(string.Format(t.EncargoIdiomasSalida, salida));
        sb.AppendLine();
        sb.AppendLine(t.EncargoIdiomasNoInventes);
        sb.AppendLine();

        sb.AppendLine(t.EncargoFuenteTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoFuenteIntro);
        sb.AppendLine();
        sb.AppendLine(t.EncargoDecisionNumero);
        sb.AppendLine();
        sb.AppendLine(t.EncargoDecisionNumeroTransmision);
        sb.AppendLine(t.EncargoDecisionNumeroOficial);
        sb.AppendLine();
        sb.AppendLine(t.EncargoDecisionNumeroUsa);
        sb.AppendLine();
        sb.AppendLine(t.EncargoDecisionNumeroEjemplo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoDecisionNumeroContinuo);
        sb.AppendLine(t.EncargoDecisionTitulos);
        sb.AppendLine(t.EncargoDecisionFecha);
        sb.AppendLine(t.EncargoDecisionSegmentos);
        sb.AppendLine();

        sb.AppendLine(t.EncargoFormatoTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoFormatoIntro);
        sb.AppendLine();
        sb.AppendLine(t.EncargoFormatoRegla);
        sb.AppendLine();

        sb.AppendLine(t.EncargoRaizTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoRaizCabecera);
        sb.AppendLine("|---|---|---|");
        sb.AppendLine(t.EncargoRaizEsquema);
        sb.AppendLine(t.EncargoRaizSerie);
        sb.AppendLine(t.EncargoRaizEpisodios);
        sb.AppendLine(t.EncargoRaizClave);
        sb.AppendLine(t.EncargoRaizNotas);
        sb.AppendLine(t.EncargoRaizIdiomas);
        sb.AppendLine(t.EncargoRaizTotal);
        sb.AppendLine();

        sb.AppendLine(t.EncargoEpisodioTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoEpisodioCabecera);
        sb.AppendLine("|---|---|---|");
        sb.AppendLine(t.EncargoEpisodioNum);
        sb.AppendLine(t.EncargoEpisodioTitulos);
        sb.AppendLine(t.EncargoEpisodioTemporada);
        sb.AppendLine(t.EncargoEpisodioFecha);
        sb.AppendLine(t.EncargoEpisodioEspecial);
        sb.AppendLine(t.EncargoEpisodioAliases);
        sb.AppendLine();

        sb.AppendLine(t.EncargoEjemploCompletoTitulo);
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("{");
        sb.AppendLine("  \"esquema\": \"reindex/1.0\",");
        sb.AppendLine($"  \"serie\": \"{serie}\",");
        sb.AppendLine("  \"clave\": \"transmision\",");
        sb.AppendLine($"  \"notas\": \"{t.EncargoEjemploNotas}\",");
        sb.AppendLine($"  \"idiomas\": {{ \"salida\": \"{salida}\", \"comparar\": [{jsonComparar}] }},");
        sb.AppendLine("  \"total\": 768,");
        sb.AppendLine("  \"episodios\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"num\": 1,");
        sb.AppendLine("      \"temporada\": 2005,");
        sb.AppendLine("      \"fecha\": \"2005-04-22\",");
        sb.AppendLine("      \"especial\": false,");
        sb.AppendLine("      \"titulos\": {");
        foreach (var c in idiomas)
            sb.AppendLine($"        \"{c}\": [\"…\"],");
        sb.AppendLine("      },");
        sb.AppendLine("      \"aliases\": []");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine(t.EncargoEjemploPobreTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoEjemploPobreIntro);
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("{");
        sb.AppendLine("  \"esquema\": \"reindex/1.0\",");
        sb.AppendLine($"  \"serie\": \"{serie}\",");
        sb.AppendLine("  \"clave\": \"continuo\",");
        sb.AppendLine($"  \"notas\": \"{t.EncargoEjemploPobreNotas}\",");
        sb.AppendLine($"  \"idiomas\": {{ \"salida\": \"{salida}\" }},");
        sb.AppendLine("  \"episodios\": [");
        sb.AppendLine($"    {{ \"num\": 1, \"titulos\": {{ \"{salida}\": [\"…\"] }} }},");
        sb.AppendLine($"    {{ \"num\": 2, \"titulos\": {{ \"{salida}\": [\"…\"] }} }}");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(t.EncargoFaltanFechas);
        sb.AppendLine();

        sb.AppendLine(t.EncargoReglasTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoReglaNum);
        sb.AppendLine(t.EncargoReglaHuecos);
        sb.AppendLine(t.EncargoReglaFecha);
        sb.AppendLine(t.EncargoReglaTitulosArray);
        sb.AppendLine(t.EncargoReglaEspeciales);
        sb.AppendLine(t.EncargoReglaCopiaTitulos);
        sb.AppendLine(t.EncargoReglaReferencias);
        sb.AppendLine();

        sb.AppendLine(t.EncargoRepasoTitulo);
        sb.AppendLine();
        sb.AppendLine(t.EncargoRepasoNum);
        sb.AppendLine(t.EncargoRepasoFechas);
        sb.AppendLine(t.EncargoRepasoTotal);
        sb.AppendLine(t.EncargoRepasoTemporadas);
        sb.AppendLine(t.EncargoRepasoCampos);
        sb.AppendLine(t.EncargoRepasoInventado);
        sb.AppendLine();
        sb.AppendLine(t.EncargoCierreValida);
        sb.AppendLine();
        sb.AppendLine(t.EncargoCierreSoloJson);

        return sb.ToString();
    }
}
