using System.IO;

namespace Ondine;

/// <summary>
/// Dónde vive lo que el usuario ha construido: catálogos importados, memoria de decisiones,
/// presets, ajustes y los diarios de lote que permiten deshacer.
///
/// Existe por el cambio de nombre de la app. La carpeta se llamaba <c>ShrinkStudio</c> y ahora
/// se llama <c>Ondine</c>; sin una mudanza, quien actualiza abre la versión nueva y se encuentra
/// la app como recién instalada, con sus catálogos «perdidos» (siguen en disco, pero en una
/// carpeta que la app ya no mira, que para el caso es lo mismo).
/// </summary>
public static class DatosDeUsuario
{
    /// <summary>Cómo se llamaba la carpeta hasta la v1.4.0. Ver <see cref="NombresHistoricos"/>.</summary>
    public const string NombreAnterior = NombresHistoricos.CarpetaDatos;
    public const string Nombre = "Ondine";

    /// <summary>Se puede redirigir en los tests para no ensuciar el perfil del usuario.</summary>
    public static string? RaizOverride { get; set; }

    private static bool _mudanzaHecha;

    /// <summary>
    /// La carpeta de datos, ya mudada. El primer acceso de cada arranque intenta la mudanza; los
    /// siguientes no tocan disco.
    /// </summary>
    public static string Raiz
    {
        get
        {
            if (RaizOverride != null) return RaizOverride;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var nueva = Path.Combine(appData, Nombre);

            if (!_mudanzaHecha)
            {
                _mudanzaHecha = true;   // se marca ANTES: si falla, no se reintenta en bucle
                try { Mudar(Path.Combine(appData, NombreAnterior), nueva); }
                catch { /* abajo se explica por qué esto no puede tumbar la app */ }
            }

            return nueva;
        }
    }

    /// <summary>
    /// Mueve la carpeta vieja a la nueva. Devuelve <c>true</c> solo si movió algo.
    /// </summary>
    /// <remarks>
    /// Tres decisiones que parecen detalles y no lo son:
    ///
    /// 1. <b>Si la carpeta nueva ya existe CON ALGO DENTRO, no se toca nada.</b> Es el caso de
    ///    quien usó la versión nueva y luego abrió una vieja. Fusionar podría pisar datos buenos
    ///    con datos viejos, y eso no tiene marcha atrás. Se deja la nueva como está y la vieja
    ///    donde estaba: recuperable a mano, que es peor experiencia pero nunca pérdida.
    ///    <br/>
    ///    Pero una carpeta nueva <b>vacía</b> sí se rellena, y esto no es un detalle: si la
    ///    mudanza falla una vez, la propia app crea el destino a los pocos segundos —
    ///    <c>SettingsStore.Save</c> y <c>ReindexStore</c> hacen <c>CreateDirectory</c>— y sin esta
    ///    excepción la regla anterior sería cierta para siempre. Un fallo transitorio (un fichero
    ///    abierto, el antivirus) dejaría los datos varados sin posibilidad de reintento.
    ///
    /// 2. <b>Si <c>Move</c> falla, se copia.</b> <c>Move</c> es atómico dentro del mismo volumen,
    ///    pero falla si algo tiene un fichero abierto o si %AppData% está redirigido a otra
    ///    unidad. La copia deja la carpeta vieja intacta a propósito: si algo va mal a medias,
    ///    los datos originales siguen ahí.
    ///
    /// 3. <b>Nunca lanza hacia arriba.</b> Esto corre al arrancar. Un permiso denegado no puede
    ///    impedir que la app abra; como mucho, que aparezca sin sus ajustes.
    /// </remarks>
    public static bool Mudar(string vieja, string nueva)
    {
        if (!Directory.Exists(vieja)) return false;

        bool destinoExiste = Directory.Exists(nueva);
        if (destinoExiste && !EstaVacia(nueva)) return false;

        try
        {
            // Con el destino vacío no vale Directory.Move —falla si la carpeta ya está—, así que
            // se mueve el contenido y se retira la vieja al terminar.
            if (destinoExiste) MoverContenido(vieja, nueva);
            else Directory.Move(vieja, nueva);
            return true;
        }
        catch
        {
            try
            {
                Copiar(vieja, nueva);
                return true;
            }
            catch
            {
                // Que no quede una carpeta a medias haciéndose pasar por una mudanza completa.
                // Si el destino ya estaba (vacío), se respeta: no es nuestro.
                try { if (!destinoExiste && Directory.Exists(nueva)) Directory.Delete(nueva, true); } catch { }
                return false;
            }
        }
    }

    private static bool EstaVacia(string dir)
    {
        try { return !Directory.EnumerateFileSystemEntries(dir).Any(); }
        catch { return false; }   // si no se puede mirar, se trata como ocupada: nunca destruir
    }

    private static void MoverContenido(string de, string a)
    {
        foreach (var f in Directory.GetFiles(de))
            File.Move(f, Path.Combine(a, Path.GetFileName(f)));
        foreach (var d in Directory.GetDirectories(de))
            Directory.Move(d, Path.Combine(a, Path.GetFileName(d)));
        try { Directory.Delete(de, recursive: false); } catch { }
    }

    private static void Copiar(string de, string a)
    {
        Directory.CreateDirectory(a);
        foreach (var f in Directory.GetFiles(de))
            File.Copy(f, Path.Combine(a, Path.GetFileName(f)), overwrite: false);
        foreach (var d in Directory.GetDirectories(de))
            Copiar(d, Path.Combine(a, Path.GetFileName(d)));
    }
}
