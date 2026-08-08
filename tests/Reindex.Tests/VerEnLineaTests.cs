using Ondine.Rutas;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Reconocer, entre todo lo que el menú de Windows ofrece para un fichero, el
/// que lo abre en la web de su nube.
///
/// <para>
/// Medido: el menú contextual expone «&amp;Ver en línea» para un fichero de
/// OneDrive, y se puede invocar. Así que Ondine puede llevarte a la web sin
/// saber nada del proveedor ni pedirte credenciales — invoca lo mismo que
/// pulsarías tú.
/// </para>
/// <para>
/// El nombre viene <b>traducido al idioma de Windows</b> y con el «&amp;» del
/// atajo de teclado metido en cualquier sitio. Reconocerlo es lo único delicado
/// de todo esto, y es lo que se prueba aquí.
/// </para>
/// </summary>
public static class VerEnLineaTests
{
    public static void Todas()
    {
        Program.Seccion("Reconocer «ver en línea»");

        // Tal cual lo devuelve el shell, con el acelerador dentro.
        Program.Assert(VerEnLaNube.EsElVerbo("&Ver en línea"), "OneDrive en castellano");
        Program.Assert(VerEnLaNube.EsElVerbo("View &online"), "y en inglés");
        Program.Assert(VerEnLaNube.EsElVerbo("Abrir en navegador"), "Nextcloud en castellano");
        Program.Assert(VerEnLaNube.EsElVerbo("Open in browser"), "y en inglés");

        // Los acentos no pueden decidirlo: el mismo Windows los escribe distinto
        // según la versión, y un fichero no debe dejar de abrirse por una tilde.
        Program.Assert(VerEnLaNube.EsElVerbo("Ver en linea"), "sin tilde también");
        Program.Assert(VerEnLaNube.EsElVerbo("VER EN LÍNEA"), "y en mayúsculas");

        // ── Lo que NO puede confundirse ──
        // Estos salen en el MISMO menú y hacen cosas muy distintas. «Compartir»
        // publica el fichero; «liberar espacio» lo borra del disco. Invocar
        // cualquiera de los dos creyendo que abre la web sería grave.
        foreach (var otro in new[]
        {
            "&Compartir", "Copiar vínculo", "Administrar acceso", "Historial de &versiones",
            "Liberar espacio", "Mantener siempre en este dispositivo", "E&liminar",
            "Share", "Copy link", "Free up space", "Always keep on this device", "Delete",
            "&Abrir", "Abrir con", "Open", "Open with", "&Propiedades",
        })
            Program.Assert(!VerEnLaNube.EsElVerbo(otro), $"«{otro}» NO es «ver en línea»");

        Program.Assert(!VerEnLaNube.EsElVerbo(""), "y una entrada vacía tampoco");
        Program.Assert(!VerEnLaNube.EsElVerbo(null), "ni null");

        Nextcloud();
    }

    /// <summary>
    /// Nextcloud no sale en el menú que <c>Verbs()</c> sabe leer —comprobado en el
    /// equipo: sus opciones no aparecen ni como submenú—, así que hay que
    /// preguntárselo a su cliente por el canal que usa su propia integración con
    /// el explorador. Contesta con una lista como esta:
    ///
    /// <code>
    /// MENU_ITEM:EDIT::Abrir en navegador
    /// MENU_ITEM:SHARE::Opciones de compartir
    /// MENU_ITEM:CURRENT_PIN:d:Algunos solo disponibles en línea
    /// MENU_ITEM:MAKE_ONLINE_ONLY::Liberar espacio local
    /// </code>
    ///
    /// <para>
    /// Aquí no se recorre un menú: se manda una <b>orden</b>. Elegir mal no abre
    /// otra ventana, ejecuta otra cosa — y en esa misma lista están «compartir» y
    /// «liberar espacio local». Por eso se exige que coincidan <b>las dos</b>
    /// cosas: la etiqueta y una orden de una lista corta de las que solo abren el
    /// navegador.
    /// </para>
    /// </summary>
    private static void Nextcloud()
    {
        Program.Seccion("El menú que Nextcloud contesta por su canal");

        // Tal cual lo devuelve el cliente, medido en el equipo.
        var real = new[]
        {
            "REGISTER_PATH:C:\\Users\\luish\\Nextcloud",
            "GET_MENU_ITEMS:BEGIN",
            "MENU_ITEM:ACTIVITY::Actividad",
            "MENU_ITEM:EDIT::Abrir en navegador",
            "MENU_ITEM:SHARE::Opciones de compartir",
            "MENU_ITEM:COPY_PRIVATE_LINK::Copiar enlace interno",
            "MENU_ITEM:FILE_ACTIONS::Acciones de archivo",
            "MENU_ITEM:CURRENT_PIN:d:Algunos solo disponibles en línea",
            "MENU_ITEM:MAKE_AVAILABLE_LOCALLY::Hacer que esté siempre localmente disponible",
            "MENU_ITEM:MAKE_ONLINE_ONLY::Liberar espacio local",
            "GET_MENU_ITEMS:END",
        };
        Program.Assert(VerEnLaNube.OrdenDelMenu(real) == "EDIT",
            "de la lista real sale «EDIT», que es «Abrir en navegador»");

        // ── Lo que NO puede elegir ──
        // Sin una entrada que abra el navegador, la respuesta es «no puedo», no
        // «pues mando la que más se le parezca».
        Program.Assert(VerEnLaNube.OrdenDelMenu(new[]
        {
            "MENU_ITEM:SHARE::Opciones de compartir",
            "MENU_ITEM:MAKE_ONLINE_ONLY::Liberar espacio local",
            "MENU_ITEM:EMAIL_PRIVATE_LINK::Enviar enlace privado por correo electrónico ...",
        }) == null, "sin «abrir en navegador» no se manda nada");

        // Deshabilitada (la «d» del segundo campo) es una entrada que el propio
        // cliente pinta en gris: mandarla es pedir algo que él mismo no ofrece.
        Program.Assert(VerEnLaNube.OrdenDelMenu(new[]
        {
            "MENU_ITEM:EDIT:d:Abrir en navegador",
        }) == null, "una entrada deshabilitada no se manda");

        // Las DOS cerraduras, cada una probada sola:
        // etiqueta buena + orden desconocida -> no. Si mañana el cliente llama
        // «Abrir en navegador» a otra cosa, esto lo para.
        Program.Assert(VerEnLaNube.OrdenDelMenu(new[]
        {
            "MENU_ITEM:DELETE::Abrir en navegador",
        }) == null, "etiqueta buena con orden desconocida: no se manda");

        // orden buena + etiqueta de otra cosa -> tampoco.
        Program.Assert(VerEnLaNube.OrdenDelMenu(new[]
        {
            "MENU_ITEM:EDIT::Liberar espacio local",
        }) == null, "orden conocida con etiqueta de otra cosa: no se manda");

        // Basura sin reventar: el canal es de otro programa y puede contestar
        // cualquier cosa, incluida nada.
        Program.Assert(VerEnLaNube.OrdenDelMenu(new[] { "MENU_ITEM:", "MENU_ITEM", "", "otra cosa" }) == null,
            "líneas rotas no eligen nada");
        Program.Assert(VerEnLaNube.OrdenDelMenu(System.Array.Empty<string>()) == null, "ni una lista vacía");
    }
}
