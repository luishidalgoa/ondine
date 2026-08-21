using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Threading;
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
/// sincronizador ya sabe abrir el fichero en su web; lo que hace Ondine es
/// pedirle que lo haga. Construir la URL a mano sería atarse a un proveedor y
/// pedirle a alguien sus credenciales para algo que su propio programa ya hace.
/// </para>
/// <para>
/// Dos puertas, porque los proveedores no coinciden en dónde ponen la opción:
/// <list type="bullet">
/// <item>el <b>menú del explorador</b>, que es donde la pone OneDrive; y</item>
/// <item>el <b>canal del cliente</b> de Nextcloud, que no sale en ese menú ni
/// como submenú —comprobado— pero contesta por una tubería con nombre.</item>
/// </list>
/// </para>
/// <para>
/// El precio es el mismo en las dos: la opción viene <b>traducida</b>, y
/// reconocerla es lo único delicado. Por eso <see cref="EsElVerbo"/> está aparte
/// y probado — confundirse no sería no abrir la web, sería ejecutar otra cosa de
/// la misma lista, y ahí al lado están «Compartir» y «Liberar espacio local».
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

    /// <summary>¿Ofrece este fichero la opción de verlo en la web?</summary>
    public static bool SePuede(string? ruta) =>
        Verbo(ruta) is not null || Nextcloud.Orden(ruta) is not null;

    /// <summary>
    /// Lo abre en la web de su nube. Devuelve false si no se pudo — y entonces
    /// quien llame decide qué hacer, en vez de quedarse sin nada.
    ///
    /// <para>
    /// Se prueba primero el menú del explorador, que es lo barato y lo que cubre a
    /// OneDrive; y si de ahí no sale, se le pregunta al cliente de Nextcloud por su
    /// canal. En ese orden porque abrir un canal cuesta más que leer un menú.
    /// </para>
    /// </summary>
    public static bool Abrir(string? ruta)
    {
        if (Verbo(ruta) is { } v)
        {
            try
            {
                v.GetType().InvokeMember("DoIt", BindingFlags.InvokeMethod, null, v, null);
                return true;
            }
            catch { /* y se prueba lo siguiente */ }
        }

        return Nextcloud.Orden(ruta) is { } orden && Nextcloud.Mandar(orden, ruta!);
    }

    /// <summary>
    /// De la lista de acciones que contesta el cliente de Nextcloud, la orden que
    /// abre el fichero en el navegador — o null si no la ofrece.
    ///
    /// <para>
    /// Cada línea viene como <c>MENU_ITEM:ORDEN:banderas:Etiqueta</c>. La bandera
    /// <c>d</c> significa que el propio cliente la pinta en gris.
    /// </para>
    /// <para>
    /// Aquí no se recorre un menú: se manda una <b>orden</b>, y en esa misma lista
    /// están «Opciones de compartir» y «Liberar espacio local» —que borra la copia
    /// local—. Por eso hacen falta <b>las dos</b> cerraduras: que la etiqueta sea
    /// la de «abrir en la web» <i>y</i> que la orden esté en una lista corta de las
    /// que solo abren el navegador. Con una sola, un cambio de etiquetas del
    /// cliente bastaría para que Ondine mandara otra cosa.
    /// </para>
    /// </summary>
    public static string? OrdenDelMenu(IEnumerable<string> lineas)
    {
        foreach (var linea in lineas)
        {
            if (linea is null || !linea.StartsWith("MENU_ITEM:", StringComparison.Ordinal)) continue;

            // ORDEN : banderas : etiqueta (que puede traer «:» dentro).
            var trozos = linea["MENU_ITEM:".Length..].Split(':', 3);
            if (trozos.Length < 3) continue;

            var orden = trozos[0].Trim();
            var banderas = trozos[1];
            var etiqueta = trozos[2];

            if (banderas.Contains('d')) continue;                    // el cliente la da por no disponible
            if (!Nextcloud.OrdenesQueAbrenLaWeb.Contains(orden)) continue;
            if (!EsElVerbo(etiqueta)) continue;

            return orden;
        }
        return null;
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

    /// <summary>
    /// El canal del cliente de Nextcloud — el mismo que usa su integración con el
    /// explorador.
    ///
    /// <para>
    /// Hace falta porque Nextcloud <b>no aparece</b> en el menú que
    /// <see cref="Verbo"/> sabe leer: comprobado en un fichero real, sus opciones
    /// no salen ni siquiera como submenú, así que por ahí no hay nada que
    /// encontrar. Pero su cliente contesta por una tubería con nombre, y ahí sí
    /// dice «Abrir en navegador».
    /// </para>
    /// </summary>
    private static class Nextcloud
    {
        /// <summary>
        /// Las únicas órdenes que Ondine se permite mandar. Lista blanca y no
        /// negra: lo que no se conoce no se manda, porque el canal admite también
        /// compartir, mandar por correo y liberar espacio local.
        /// </summary>
        public static readonly HashSet<string> OrdenesQueAbrenLaWeb =
            new(StringComparer.Ordinal) { "EDIT", "OPEN_PRIVATE_LINK" };

        /// <summary>
        /// Presupuesto de espera. Esto se consulta al pulsar «reproducir», con la
        /// persona mirando: si el cliente no está o no contesta, más vale ofrecer
        /// el explorador enseguida que dejar la ventana quieta. Un cliente vivo
        /// contesta en milisegundos.
        /// </summary>
        private const int MsParaConectar = 250;
        private const int MsParaContestar = 750;

        public static string? Orden(string? ruta) =>
            Hablar(ruta, "GET_MENU_ITEMS", lineas => OrdenDelMenu(lineas));

        public static bool Mandar(string orden, string ruta) =>
            Hablar(ruta, orden, _ => "ya") is not null;

        /// <summary>
        /// Abre la tubería, manda <c>ORDEN:ruta</c> y deja que quien llama lea la
        /// respuesta.
        ///
        /// <para>
        /// La respuesta se lee hasta <c>ORDEN:END</c> o hasta agotar el
        /// presupuesto: el cliente manda también avisos suyos —<c>REGISTER_PATH</c>
        /// y demás— cuando le parece, así que esperar «una línea» leería lo que no
        /// es. Para las órdenes que solo se ejecutan no hay END que esperar, y por
        /// eso quien llama decide cuándo ha terminado.
        /// </para>
        /// </summary>
        private static string? Hablar(string? ruta, string orden, Func<List<string>, string?> leer)
        {
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(ruta)) return null;
            if (Tuberia() is not { } tuberia) return null;

            try
            {
                using var canal = new NamedPipeClientStream(".", tuberia, PipeDirection.InOut);
                canal.Connect(MsParaConectar);

                using var pluma = new StreamWriter(canal, new UTF8Encoding(false)) { AutoFlush = true };
                pluma.WriteLine($"{orden}:{ruta}");

                // Mandar y no leer nada dejaría la orden a medio camino: se cierra
                // la tubería antes de que el cliente la haya procesado.
                using var oido = new StreamReader(canal, new UTF8Encoding(false));
                var lineas = new List<string>();
                var hasta = Environment.TickCount64 + MsParaContestar;

                while (Environment.TickCount64 < hasta)
                {
                    if (oido.Peek() < 0) { Thread.Sleep(15); continue; }
                    var linea = oido.ReadLine();
                    if (linea is null) break;
                    lineas.Add(linea);
                    if (linea == $"{orden}:END") break;
                }

                return leer(lineas);
            }
            catch { return null; }   // sin cliente, sin permisos o con otra versión del protocolo
        }

        /// <summary>
        /// La tubería del cliente, buscada por su nombre entre las del sistema.
        ///
        /// <para>
        /// Se busca en vez de componerla como «nextcloud-{usuario}» porque ese
        /// nombre lleva el usuario dentro y ahí caben sorpresas —tildes, dominio,
        /// un nombre que no es el de la sesión—. Preguntar cuesta lo mismo que
        /// suponer y no falla en silencio.
        /// </para>
        /// </summary>
        private static string? Tuberia()
        {
            try
            {
                return Directory.GetFiles(@"\\.\pipe\")
                    .Select(Path.GetFileName)
                    .FirstOrDefault(n => n is not null
                        && (n.StartsWith("nextcloud-", StringComparison.OrdinalIgnoreCase)
                         || n.StartsWith("owncloud-", StringComparison.OrdinalIgnoreCase)));
            }
            catch { return null; }
        }
    }
}
