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
    }
}
