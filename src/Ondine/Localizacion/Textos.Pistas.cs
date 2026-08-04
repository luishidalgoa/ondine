namespace Ondine.Localizacion;

/// <summary>
/// Textos de «Quitar pistas» (<c>PistasWindow</c>) y del resumen de una pista
/// (<c>Pistas/SelectorDePistas.cs</c>), que es lo que se lee en cada fila de la
/// lista.
/// </summary>
public sealed partial class Textos
{
    // ═══ Ventana ════════════════════════════════════════════════════════════

    // Vale para el Title de la ventana, para el rótulo de la barra dibujada a
    // mano y para el título del diálogo de confirmación: es el mismo texto, así
    // que una sola clave.
    public string PistasTitulo => Idioma.Elegir("Remove tracks", "Quitar pistas");

    // «Marca» es «tick» y no «check»: inglés británico.
    public string PistasAyuda => Idioma.Elegir(
        "Tick the tracks you do NOT want. Nothing is re-encoded: the file is repackaged by copying the data as is, so it takes seconds and the picture stays identical.",
        "Marca las pistas que NO quieras. No se recomprime nada: se reempaqueta copiando los datos tal cual, así que tarda segundos y la imagen queda idéntica.");

    // El castellano elide el sustantivo («las marcadas»); en inglés «the ticked
    // ones» suena a nada, así que se repite «tracks». Sigue cabiendo en el botón.
    public string PistasQuitarMarcadas => Idioma.Elegir("Remove ticked tracks", "Quitar las marcadas");

    // ═══ Lista ══════════════════════════════════════════════════════════════

    /// <summary>Columna derecha de la fila, solo en la pista de vídeo.</summary>
    public string PistasNotaSeConserva => Idioma.Elegir("always kept", "se conserva siempre");

    // ═══ Pie: cuánto se ahorra ══════════════════════════════════════════════

    public string PistasNadaMarcado => Idioma.Elegir(
        "Tick a track to remove it.",
        "Marca alguna pista para quitarla.");

    // {0} = cuántas pistas, {1} = megas que se ahorran. El «(s)» del plural se
    // hereda del original y se mantiene en inglés: ninguno de los dos idiomas
    // resuelve bien el singular, y arreglarlo es cambiar la cadena, no traducirla.
    public string PistasAhorro => Idioma.Elegir(
        "Removing {0} track(s) · about {1} MB less",
        "Se quitan {0} pista(s) · unos {1} MB menos");

    // {0} = cuántas pistas. «Caudal» es el bitrate declarado de la pista.
    public string PistasAhorroDesconocido => Idioma.Elegir(
        "Removing {0} track(s) · the saving will be small (they do not declare their bitrate)",
        "Se quitan {0} pista(s) · el ahorro será pequeño (no declaran su caudal)");

    public string PistasAvisoSinAudio => Idioma.Elegir(
        "Careful: this leaves the video with NO audio.",
        "Ojo: así el vídeo se queda SIN audio.");

    // ═══ Quitar: proceso y resultado ════════════════════════════════════════

    // {0} = nombre del fichero, {1} = la lista de pistas que se van, una por línea.
    // Las comillas cambian de convención: « » en castellano, “ ” en inglés.
    // «Papelera» es la del sistema, que en inglés se llama Recycle Bin.
    public string PistasConfirmarCuerpo => Idioma.Elegir(
        "These will be removed from “{0}”:\n\n{1}\n\nThe video is NOT re-encoded: it is copied as is, so the picture stays identical.\nThe original file goes to the Recycle Bin (recoverable with Ctrl+Z).",
        "Se van a quitar de «{0}»:\n\n{1}\n\nEl vídeo NO se recomprime: se copia tal cual, así que la imagen queda idéntica.\nEl fichero original va a la Papelera (recuperable con Ctrl+Z).");

    public string PistasReempaquetando => Idioma.Elegir("Repackaging…", "Reempaquetando…");

    // {0} = megas antes, {1} = megas después, {2} = megas ahorrados.
    public string PistasHecho => Idioma.Elegir(
        "Done · {0} MB → {1} MB ({2} MB less)",
        "Listo · {0} MB → {1} MB ({2} MB menos)");

    public string PistasHechoCuerpo => Idioma.Elegir(
        "Tracks removed.\n\n{0} MB → {1} MB ({2} MB less, and the picture untouched).",
        "Pistas quitadas.\n\n{0} MB → {1} MB ({2} MB menos, sin tocar la imagen).");

    public string PistasNoSePudo => Idioma.Elegir("Could not do it.", "No se pudo.");

    public string PistasFalloTitulo => Idioma.Elegir("Could not remove", "No se pudo quitar");

    // ═══ Resumen de una pista ═══════════════════════════════════════════════
    //
    // Los trozos con los que se arma cada fila: «Audio · Español · «Castellano
    // AMZN» · 2 canales · 128 kbps · aac · predeterminada».

    public string PistasTipoVideo => Idioma.Elegir("Video", "Vídeo");
    public string PistasTipoAudio => Idioma.Elegir("Audio", "Audio");
    public string PistasTipoSubtitulo => Idioma.Elegir("Subtitle", "Subtítulo");
    public string PistasTipoOtra => Idioma.Elegir("Other", "Otra");

    /// <summary>{0} = el título que trae la pista.</summary>
    public string PistasTituloEntrecomillado => Idioma.Elegir("“{0}”", "«{0}»");

    // Solo sale en subtítulos, y en castellano concuerda con ellos: masculino
    // plural aunque la palabra «subtítulos» no esté en la cadena.
    public string PistasForzados => Idioma.Elegir("forced", "forzados");

    /// <summary>{0} = número de canales.</summary>
    public string PistasCanales => Idioma.Elegir("{0} channels", "{0} canales");

    // Unidad técnica: se escribe igual en los dos idiomas. Pasa por el catálogo
    // igualmente para no dejar texto suelto en el código.
    public string PistasKbps => Idioma.Elegir("kbps", "kbps");

    // Concuerda con «pista», que va implícita.
    public string PistasPredeterminada => Idioma.Elegir("default", "predeterminada");
}
