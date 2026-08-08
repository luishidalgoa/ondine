using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ondine.Rutas;

/// <summary>
/// Abrir un fichero en la web de su nube, invocando lo mismo que pulsarías tú en
/// el menú del explorador.
///
/// <para>
/// Medido: leer <b>un mega</b> de un fichero de 65 MB que solo está en OneDrive
/// bloquea más de cinco minutos sin terminar — Windows lo recupera entero al
/// abrirlo—. Así que para comprobar de qué episodio es, la vía buena es no
/// tocarlo y verlo en su web.
/// </para>
/// <para>
/// <b>Sin API, sin credenciales y sin saber de ningún proveedor.</b> El
/// sincronizador ya pone su «Ver en línea» en el menú contextual, y ese menú se
/// puede recorrer e invocar. Construir la URL a mano sería atarse a OneDrive y
/// pedirle a alguien sus credenciales para algo que su propio programa ya hace.
/// </para>
/// <para>
/// El precio: el nombre del verbo viene <b>traducido</b> al idioma de Windows.
/// Reconocerlo es lo único delicado, y por eso <see cref="EsElVerbo"/> está
/// aparte y probado — confundirse ahí no sería abrir la web, sería invocar otra
/// cosa del mismo menú, y ahí al lado están «Compartir» y «Liberar espacio».
/// </para>
/// </summary>
public static class VerEnLaNube
{
    /// <summary>
    /// Los nombres con los que los sincronizadores llaman a «abre esto en la web».
    ///
    /// <para>
    /// Comparados sin acentos, sin mayúsculas y sin el «&amp;» del atajo, porque el
    /// shell los devuelve como «&amp;Ver en línea» y la tilde no puede decidir si un
    /// fichero se abre o no.
    /// </para>
    /// </summary>
    private static readonly string[] Nombres =
    {
        "ver en linea",       // OneDrive, castellano
        "view online",        // OneDrive, inglés
        "abrir en navegador", // Nextcloud, castellano
        "open in browser",    // Nextcloud, inglés
        "ver en la web",
        "view on web",
    };

    /// <summary>¿Es esta entrada del menú la que abre el fichero en la web?</summary>
    public static bool EsElVerbo(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return false;
        var limpio = Limpiar(nombre);
        return Nombres.Any(n => limpio == n);
    }

    private static string Limpiar(string s)
    {
        var sin = new StringBuilder();
        foreach (var c in s.Replace("&", "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sin.Append(c);
        return sin.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
    }

    /// <summary>¿Ofrece el menú de este fichero la opción de verlo en la web?</summary>
    public static bool SePuede(string? ruta) => Verbo(ruta) is not null;

    /// <summary>
    /// Lo abre en la web de su nube. Devuelve false si no se pudo — y entonces
    /// quien llame decide qué hacer, en vez de quedarse sin nada.
    /// </summary>
    public static bool Abrir(string? ruta)
    {
        if (Verbo(ruta) is not { } v) return false;
        try
        {
            v.GetType().InvokeMember("DoIt", BindingFlags.InvokeMethod, null, v, null);
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Busca en el menú contextual del fichero la entrada que abre la web.
    ///
    /// <para>
    /// Por reflexión y no con <c>dynamic</c> ni una biblioteca de interoperabilidad:
    /// este proyecto no tiene ni una dependencia de NuGet, y el enlace tardío del
    /// compilador arrastra su propio paquete.
    /// </para>
    /// <para>
    /// Solo llega al primer nivel del menú. OneDrive pone ahí sus opciones y
    /// funciona; Nextcloud las mete en un submenú y ahí esto no alcanza — por eso
    /// quien llama tiene que tener una salida cuando devuelve null, y no dar por
    /// hecho que siempre hay web a la que ir.
    /// </para>
    /// </summary>
    private static object? Verbo(string? ruta)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(ruta)) return null;

        try
        {
            var carpeta = Path.GetDirectoryName(ruta);
            var nombre = Path.GetFileName(ruta);
            if (string.IsNullOrEmpty(carpeta) || string.IsNullOrEmpty(nombre)) return null;

            var tipo = Type.GetTypeFromProgID("Shell.Application");
            if (tipo is null) return null;
            var shell = Activator.CreateInstance(tipo);
            if (shell is null) return null;

            var ns = Llamar(shell, "Namespace", carpeta);
            if (ns is null) return null;
            var item = Llamar(ns, "ParseName", nombre);
            if (item is null) return null;
            var verbos = Llamar(item, "Verbs");
            if (verbos is null) return null;

            int cuantos = Convert.ToInt32(Leer(verbos, "Count") ?? 0);
            for (int i = 0; i < cuantos; i++)
            {
                var v = Llamar(verbos, "Item", i);
                if (v is null) continue;
                if (EsElVerbo(Leer(v, "Name") as string)) return v;
            }
        }
        catch { /* sin shell, sin permisos o con el proveedor a medias: no se ofrece */ }

        return null;
    }

    private static object? Llamar(object o, string metodo, params object[] args) =>
        o.GetType().InvokeMember(metodo, BindingFlags.InvokeMethod, null, o, args);

    private static object? Leer(object o, string propiedad) =>
        o.GetType().InvokeMember(propiedad, BindingFlags.GetProperty, null, o, null);
}
