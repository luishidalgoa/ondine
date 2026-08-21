using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// El índice de complementos que se pueden instalar.
///
/// <para>
/// Hoy lo publica el propio proyecto y no acepta a nadie más. Pero el
/// <c>sha256</c> es <b>obligatorio desde el primer día</b>, aunque con un índice
/// de un solo autor parezca de sobra: el día que se abra a que publique
/// cualquiera, lo único que hará falta cambiar es quién puede escribir en el
/// índice — no el formato, ni el instalador, ni lo que ya haya publicado.
/// Añadirlo después obligaría a tratar «sin checksum» como un caso válido más, y
/// esa rama se arrastra para siempre.
/// </para>
/// </summary>
public sealed class Indice
{
    [JsonPropertyName("contrato")] public int Contrato { get; set; } = 1;
    [JsonPropertyName("complementos")] public List<Entrada> Complementos { get; set; } = new();

    /// <summary>Una entrada del índice: lo que hace falta para decidir e instalar.</summary>
    public sealed class Entrada
    {
        /// <summary>Manda sobre el nombre de la carpeta: instalar crea <c>complementos/{id}</c>.</summary>
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
        [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "";
        [JsonPropertyName("autor")] public string Autor { get; set; } = "";

        /// <summary>De dónde se baja el paquete. Un zip con el contenido de la carpeta.</summary>
        [JsonPropertyName("paquete")] public string Paquete { get; set; } = "";

        /// <summary>
        /// El sha256 del paquete, en hexadecimal. Sin esto no se instala nada:
        /// bajar un ejecutable y correrlo sin comprobar que es el que el índice
        /// dice es lo que convierte un gestor de complementos en un problema.
        /// </summary>
        [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";

        [JsonPropertyName("bytes")] public long Bytes { get; set; }

        // Se repiten del manifiesto a propósito: la pantalla tiene que poder
        // enseñar de qué va cada uno ANTES de bajarlo, y el manifiesto está
        // dentro del paquete.
        [JsonPropertyName("capacidades")] public List<string> Capacidades { get; set; } = new();
        [JsonPropertyName("ambito")] public List<string> Ambito { get; set; } = new();
        [JsonPropertyName("integracion")] public string Integracion { get; set; } = Complemento.IntegracionPropia;

        /// <summary>Qué le pasa a esta entrada, si le pasa algo. Null si se puede instalar.</summary>
        public string? Reparo()
        {
            if (string.IsNullOrWhiteSpace(Id)) return Textos.Instancia.IndiceSinId;
            if (string.IsNullOrWhiteSpace(Paquete)) return Textos.Instancia.IndiceSinPaquete;

            // Un id con separadores no es un id: es una ruta, y de ahí sale una
            // carpeta de instalación fuera de donde debe. Se comprueba aquí, que
            // es donde entra el dato de fuera, y no al usarlo.
            if (Id.Any(c => c is '/' or '\\' or ':') || Id.Contains(".."))
                return string.Format(Textos.Instancia.IndiceIdRaro, Id);

            // Solo HTTPS. Por HTTP, cualquiera en medio puede cambiar el paquete
            // — y aunque el checksum lo cazaría, también puede cambiar el índice.
            if (!Uri.TryCreate(Paquete, UriKind.Absolute, out var u) ||
                !string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return Textos.Instancia.IndiceSoloHttps;

            if (!EsSha256(Sha256)) return Textos.Instancia.IndiceSinChecksum;

            return null;
        }
    }

    /// <summary>Sesenta y cuatro caracteres hexadecimales, ni uno más ni uno menos.</summary>
    public static bool EsSha256(string? s) =>
        s is { Length: 64 } && s.All(Uri.IsHexDigit);

    /// <summary>
    /// El sha256 de unos bytes, en el mismo formato que el índice.
    /// </summary>
    public static string Huella(byte[] datos) =>
        Convert.ToHexString(SHA256.HashData(datos)).ToLowerInvariant();

    /// <summary>
    /// ¿Estos bytes son los que el índice prometía? Comparación insensible a
    /// mayúsculas porque el hexadecimal se escribe de las dos formas y rechazar
    /// un índice correcto por eso sería absurdo.
    /// </summary>
    public static bool Cuadra(byte[] datos, string? esperado) =>
        EsSha256(esperado) && Huella(datos).Equals(esperado, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// ¿La del índice es más nueva que la instalada?
    ///
    /// <para>
    /// Se compara <b>por número y trozo a trozo</b>, no como texto: en orden
    /// alfabético «1.10.0» va antes que «1.9.0», y una actualización de verdad no
    /// se ofrecería nunca.
    /// </para>
    /// <para>
    /// Ante algo que no se puede leer como versión —vacío, texto— NO se ofrece
    /// actualizar. Empujar una reinstalación por no saber leer un número es peor
    /// que quedarse callado: el usuario acabaría bajando lo mismo que ya tiene.
    /// </para>
    /// </summary>
    public static bool EsMasNueva(string? instalada, string? disponible)
    {
        if (Trozos(instalada) is not { } a || Trozos(disponible) is not { } b) return false;

        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            int x = i < a.Length ? a[i] : 0;   // «1.2» es «1.2.0»
            int y = i < b.Length ? b[i] : 0;
            if (y != x) return y > x;
        }
        return false;
    }

    private static int[]? Trozos(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var partes = v.Trim().Split('.');
        var nums = new int[partes.Length];
        for (int i = 0; i < partes.Length; i++)
            if (!int.TryParse(partes[i], out nums[i])) return null;
        return nums;
    }

    /// <summary>Lee un índice. Null si no es JSON válido o habla otro contrato.</summary>
    public static Indice? Leer(string json)
    {
        try
        {
            var i = JsonSerializer.Deserialize<Indice>(json);
            return i is null || i.Contrato != Complemento.ContratoActual ? null : i;
        }
        catch { return null; }
    }
}
