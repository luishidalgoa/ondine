using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ondine.Reindex;

/// <summary>
/// La ficha que Windows guarda de un vídeo, al margen de su contenido.
///
/// <para>
/// Existe por una razón concreta: <see cref="Nube"/> explica que abrir un marcador
/// de «archivos a petición» se lo descarga entero, y por eso el análisis se para
/// ante ellos. Pero la duración no hace falta sacarla del fichero. Cuando el
/// proveedor de sincronización crea el marcador deja también su ficha -duración,
/// resolución, tasa de bits-, y esa ficha se lee SIN tocar el contenido. Es lo
/// mismo que hace el Explorador cuando te enseña la duración de un vídeo que está
/// en la nube.
/// </para>
/// <para>
/// Medido sobre la biblioteca real: 50 marcadores de 50 devolvieron duración, a
/// unos 50 ms cada uno, y los 50 seguían siendo marcadores después. Cero bytes
/// descargados. Funciona igual con <c>.mp4</c> y con <c>.avi</c>.
/// </para>
/// <para>
/// <b>Es de Windows.</b> En Linux y macOS no hay ficha equivalente, así que allí
/// devuelve siempre <c>null</c> y quien llame se las apaña. Nunca cae en abrir el
/// fichero como respaldo: eso es justo lo que se está evitando.
/// </para>
/// </summary>
public static class FichaDeWindows
{
    /// <summary>
    /// Convierte lo que devuelve Windows -unidades de 100 nanosegundos- en una
    /// duración. Cero significa «no lo sé», no «dura nada»: es lo que devuelve un
    /// fichero cuyo tipo el sistema no sabe interpretar.
    /// </summary>
    public static TimeSpan? DeUnidadesDe100ns(ulong unidades)
    {
        if (unidades == 0) return null;
        // Un valor absurdo es un dato corrupto, no una película de mil horas.
        // Se descarta antes de que llegue a la barra del reproductor y la deje
        // con un máximo imposible.
        if (unidades > (ulong)TimeSpan.FromHours(24).Ticks) return null;
        return TimeSpan.FromTicks((long)unidades);
    }

    /// <summary>
    /// La duración del vídeo, leída de la ficha. <c>null</c> si no está: ni el
    /// sistema la tiene, ni el formato la declara, ni estamos en Windows.
    /// </summary>
    public static TimeSpan? Duracion(string ruta)
    {
        if (!OperatingSystem.IsWindows()) return null;
        return DuracionEnWindows(ruta);
    }

    [SupportedOSPlatform("windows")]
    private static TimeSpan? DuracionEnWindows(string ruta)
    {
        try
        {
            var iid = new Guid(IidShellItem2);
            if (SHCreateItemFromParsingName(ruta, IntPtr.Zero, ref iid, out var item) != 0 || item is null)
                return null;

            try
            {
                var clave = ClaveDuracion;
                // `GetUInt64` frente a leer el almacén de propiedades a pelo: evita
                // tener que ordenar a mano un PROPVARIANT, que es donde se fugan los
                // punteros si algo sale mal a mitad.
                return item.GetUInt64(ref clave, out ulong cien) == 0
                    ? DeUnidadesDe100ns(cien)
                    : null;
            }
            finally { Marshal.ReleaseComObject(item); }
        }
        catch
        {
            // Un fichero que ya no está, una ruta que el shell no entiende o un
            // proveedor que contesta raro. No saber la duración no es un error que
            // deba subir: se queda en blanco y ya.
            return null;
        }
    }

    // ── la parte de Windows ──────────────────────────────────────────────────

    private const string IidShellItem2 = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

    /// <summary>
    /// PKEY_Media_Duration. Se pide por identificador y no por el nombre de la
    /// columna del Explorador: el nombre cambia con el idioma de Windows y su
    /// posición con la versión, y el identificador no cambia con ninguno.
    /// </summary>
    private static PROPERTYKEY ClaveDuracion => new()
    {
        fmtid = new Guid("64440490-4C8B-11D1-8B70-080036B11A03"),
        pid = 3,
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [ComImport]
    [Guid(IidShellItem2)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem2
    {
        // Los seis primeros huecos son de IShellItem y no se usan, pero tienen que
        // estar declarados: la tabla de métodos va por posición, y saltárselos
        // llamaría al método equivocado.
        void _BindToHandler();
        void _GetParent();
        void _GetDisplayName();
        void _GetAttributes();
        void _Compare();
        void _GetPropertyStore();
        void _GetPropertyStoreWithCreateObject();
        void _GetPropertyStoreForKeys();
        void _GetPropertyDescriptionList();
        void _Update();
        void _GetProperty();
        void _GetCLSID();
        void _GetFileTime();
        void _GetInt32();
        void _GetString();
        [PreserveSig] int GetUInt32(ref PROPERTYKEY key, out uint pui);
        [PreserveSig] int GetUInt64(ref PROPERTYKEY key, out ulong pull);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem2? ppv);
}
