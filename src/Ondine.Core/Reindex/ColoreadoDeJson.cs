namespace Ondine;

/// <summary>
/// Parte un JSON en trozos y le pone a cada uno el nombre del color que le toca.
///
/// <para>
/// Es un <b>autómata mínimo, no un parser</b>: el JSON que llega aquí lo acaba de producir
/// el serializador, así que siempre está bien formado y basta con distinguir cadena, número,
/// palabra y signo. Escribirlo así es deliberado — un parser de verdad para pintar cuatro
/// colores sería mucho más código con más sitios donde fallar.
/// </para>
/// <para>
/// Está en el motor y no en la ventana porque, si no, cada interfaz tendría el suyo. Un
/// autómata reescrito a ojo es un autómata con otro fallo, y este además <b>falla en
/// silencio</b>: colorear del revés deja un JSON perfectamente legible que simplemente ha
/// dejado de ayudar.
/// </para>
/// <para>
/// Devuelve el <b>nombre</b> del color, no el color: quién es «OrgOk» lo decide el tema de
/// cada interfaz, que es lo único que cambia entre WPF y Avalonia.
/// </para>
/// </summary>
public static class ColoreadoDeJson
{
    /// <summary>Un trozo del JSON y el nombre del color con el que se pinta.</summary>
    public readonly record struct Trozo(string Texto, string Color);

    // Los colores son los del tema, no los de un editor: claves en el acento, cadenas en el
    // verde del semáforo, números en su ámbar y la puntuación apagada. Así el panel es de
    // esta app y no un pegote de otro sitio.
    private const string Clave = "Accent300";
    private const string Cadena = "OrgOk";
    private const string Numero = "OrgWarn";
    private const string Palabra = "OrgDanger";
    private const string Signo = "Neutral500";

    /// <summary>
    /// Los trozos, en orden. Pegándolos otra vez sale el JSON de entrada exactamente: si se
    /// perdiera un carácter, el panel enseñaría algo distinto de lo que copia el botón de al
    /// lado, y eso es peor que no colorear.
    /// </summary>
    public static IReadOnlyList<Trozo> Partir(string json)
    {
        var trozos = new List<Trozo>();
        int i = 0;

        while (i < json.Length)
        {
            char c = json[i];

            if (c == '"')
            {
                // Hasta la comilla de cierre, saltándose las escapadas: si no, una cadena
                // con comillas dentro se parte por la mitad y todo lo de después se colorea
                // corrido.
                int j = i + 1;
                while (j < json.Length && (json[j] != '"' || json[j - 1] == '\\')) j++;
                var cadena = json[i..Math.Min(j + 1, json.Length)];

                // La única decisión con miga: una cadena es CLAVE si lo siguiente —saltando
                // espacios— son los dos puntos. Por eso una cadena dentro de una lista no
                // lo es, aunque le siga otra.
                int k = j + 1;
                while (k < json.Length && char.IsWhiteSpace(json[k])) k++;
                bool esClave = k < json.Length && json[k] == ':';

                trozos.Add(new(cadena, esClave ? Clave : Cadena));
                i = j + 1;
            }
            else if (char.IsDigit(c) || c == '-')
            {
                int j = i;
                while (j < json.Length && (char.IsDigit(json[j]) || json[j] is '-' or '+' or '.' or 'e' or 'E')) j++;
                trozos.Add(new(json[i..j], Numero));
                i = j;
            }
            else if (char.IsLetter(c))   // true / false (null no llega: se omite al serializar)
            {
                int j = i;
                while (j < json.Length && char.IsLetter(json[j])) j++;
                trozos.Add(new(json[i..j], Palabra));
                i = j;
            }
            else
            {
                int j = i;
                while (j < json.Length && json[j] is not ('"' or '-') &&
                       !char.IsLetterOrDigit(json[j])) j++;
                trozos.Add(new(json[i..j], Signo));
                i = j;
            }
        }

        return trozos;
    }
}
