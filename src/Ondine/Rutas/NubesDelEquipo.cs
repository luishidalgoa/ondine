using System.IO;
using Microsoft.Win32;
using Ondine.Reindex;

namespace Ondine.Rutas;

/// <summary>
/// Qué nubes hay sincronizadas en este equipo, preguntándoselo a Windows.
///
/// <para>
/// Cualquier proveedor que use la API de nube de Windows —OneDrive, Nextcloud,
/// Dropbox, iCloud— se registra como «raíz de sincronización» y deja ahí su
/// nombre y su carpeta. Leer de ahí es lo que hace que esto valga para todos sin
/// saber nada de ninguno: la alternativa, una lista de proveedores cableada, se
/// queda corta el día que alguien usa el siguiente.
/// </para>
/// <para>
/// Vive en <c>Rutas</c> y no en <c>Reindex</c> porque toca el registro de
/// Windows, y el motor compila y se prueba sin Windows a propósito. Ahí solo está
/// la regla de decidir a quién pertenece una ruta, que es pura.
/// </para>
/// </summary>
public static class NubesDelEquipo
{
    private const string Clave =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager";

    /// <summary>
    /// Las raíces registradas. Lista vacía si no se puede leer: no saber qué nubes
    /// hay no debe impedir nada, solo hace que no se ofrezca la ayuda.
    /// </summary>
    public static IReadOnlyList<Nube.Sincronizacion> Registradas()
    {
        var fuera = new List<Nube.Sincronizacion>();
        if (!OperatingSystem.IsWindows()) return fuera;

        try
        {
            using var raiz = Registry.LocalMachine.OpenSubKey(Clave);
            if (raiz is null) return fuera;

            foreach (var nombre in raiz.GetSubKeyNames())
            {
                using var k = raiz.OpenSubKey(nombre);
                if (k is null) continue;

                // El nombre que se le enseña a una persona. Si no está, se usa la
                // primera parte de la clave -«OneDrive!S-1-5-…»-, que al menos dice
                // de quién es.
                var proveedor = k.GetValue("DisplayNameResource") as string;
                if (string.IsNullOrWhiteSpace(proveedor))
                    proveedor = nombre.Split('!')[0];

                using var suyas = k.OpenSubKey("UserSyncRoots");
                if (suyas is null) continue;

                foreach (var v in suyas.GetValueNames())
                    if (suyas.GetValue(v) is string ruta && !string.IsNullOrWhiteSpace(ruta)
                        && Directory.Exists(ruta))
                        fuera.Add(new Nube.Sincronizacion(proveedor!, ruta));
            }
        }
        catch { /* sin permisos o con el registro raro: se queda sin ofrecer la ayuda */ }

        return fuera;
    }

    /// <summary>De qué nube es este fichero, preguntando al equipo. Null si de ninguna.</summary>
    public static Nube.Sincronizacion? DuenaDe(string? ruta) => Nube.Duena(ruta, Registradas());
}
