using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Ondine.Ia;

/// <summary>
/// El modelo de lenguaje que se haya conectado, si es que hay alguno.
///
/// <para>
/// Ondine funciona entera sin esto. Es un <b>apoyo opcional</b> para los casos
/// que las reglas no resuelven, y quien no lo configure no nota que existe.
/// </para>
/// </summary>
public sealed class AjustesDeModelo
{
    /// <summary>Si está apagado, no se pregunta a nadie aunque haya dirección y clave.</summary>
    public bool Activo { get; set; }

    /// <summary>
    /// La base de la API, al estilo OpenAI: <c>https://api.openai.com/v1</c>,
    /// <c>http://localhost:11434/v1</c> (Ollama), la de tu proveedor…
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>El nombre del modelo, tal y como lo llame ese servidor.</summary>
    public string Modelo { get; set; } = "";

    /// <summary>
    /// La clave, <b>cifrada</b> con la protección de datos de Windows y atada a
    /// esta cuenta de usuario.
    ///
    /// <para>
    /// En claro no se guarda nunca. <c>settings.json</c> es un fichero normal
    /// que se copia, se sube a una nube de respaldo y se pega en un informe de
    /// fallo sin pensarlo; una clave de API ahí dentro es una clave filtrada. Y
    /// atada al usuario, no solo a la máquina: en un equipo compartido, la
    /// cuenta de al lado no puede descifrarla.
    /// </para>
    /// <para>
    /// Esto protege del descuido, no de quien ya esté ejecutando código como tú:
    /// si alguien llega a ese punto, la clave es lo de menos.
    /// </para>
    /// </summary>
    public string ClaveCifrada { get; set; } = "";

    /// <summary>¿Hay algo configurado como para poder preguntar?</summary>
    [JsonIgnore]
    public bool Listo => Activo
                         && ModeloConectado.Endpoint(BaseUrl) != null
                         && !string.IsNullOrWhiteSpace(Modelo);

    /// <summary>La clave en claro, solo para el momento de usarla.</summary>
    public string Clave() => Proteccion.Descifrar(ClaveCifrada);

    /// <summary>Guarda la clave cifrada. Con vacío, la borra.</summary>
    public void PonerClave(string? clave) =>
        ClaveCifrada = string.IsNullOrWhiteSpace(clave) ? "" : Proteccion.Cifrar(clave);

    /// <summary>Hay clave guardada (sin descifrarla), para poder enseñar «••••••».</summary>
    [JsonIgnore]
    public bool TieneClave => ClaveCifrada.Length > 0;

    public AjustesDeModelo Clone() => (AjustesDeModelo)MemberwiseClone();
}

/// <summary>
/// Cifrado de la clave con la protección de datos de Windows (DPAPI).
///
/// <para>
/// Se llama a <c>crypt32</c> directamente en vez de traer el paquete
/// <c>System.Security.Cryptography.ProtectedData</c>: <b>este proyecto no tiene
/// ni una dependencia de NuGet</b>, y eso es lo que permite que CI compile y
/// pruebe sin restaurar nada. Dos funciones importadas cuestan menos que romper
/// esa propiedad.
/// </para>
/// </summary>
internal static class Proteccion
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob pIn, string? szDescription,
        IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DataBlob pOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob pIn, IntPtr ppszDescription,
        IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DataBlob pOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string Cifrar(string claro)
    {
        // Fuera de Windows no hay DPAPI. Antes que guardar la clave en claro sin
        // avisar, no se guarda: la app de escritorio es de Windows, y un
        // «funciona igual» que además filtra la clave es peor que no funcionar.
        if (!OperatingSystem.IsWindows()) return "";
        return Convertir(Encoding.UTF8.GetBytes(claro), cifrar: true) is { } b
            ? Convert.ToBase64String(b) : "";
    }

    public static string Descifrar(string cifrado)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(cifrado)) return "";
        try
        {
            return Convertir(Convert.FromBase64String(cifrado), cifrar: false) is { } b
                ? Encoding.UTF8.GetString(b) : "";
        }
        catch { return ""; }
    }

    private static byte[]? Convertir(byte[] datos, bool cifrar)
    {
        var entrada = new DataBlob();
        var salida = new DataBlob();
        try
        {
            entrada.cbData = datos.Length;
            entrada.pbData = Marshal.AllocHGlobal(datos.Length);
            Marshal.Copy(datos, 0, entrada.pbData, datos.Length);

            bool bien = cifrar
                ? CryptProtectData(ref entrada, "Ondine", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out salida)
                : CryptUnprotectData(ref entrada, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out salida);
            if (!bien) return null;

            var r = new byte[salida.cbData];
            Marshal.Copy(salida.pbData, r, 0, salida.cbData);
            return r;
        }
        catch { return null; }
        finally
        {
            if (entrada.pbData != IntPtr.Zero) Marshal.FreeHGlobal(entrada.pbData);
            if (salida.pbData != IntPtr.Zero) LocalFree(salida.pbData);
        }
    }
}
