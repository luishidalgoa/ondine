namespace Ondine.Localizacion;

/// <summary>
/// Los textos del reproductor integrado (<c>ReproductorWindow</c>).
///
/// <para>
/// Las letras entre paréntesis de los rótulos de ayuda son TECLAS FÍSICAS
/// (<c>Key.M</c>, <c>Key.F</c>): se traduce el verbo, nunca la letra, porque el
/// teclado no cambia de idioma con la interfaz.
/// </para>
/// </summary>
public sealed partial class Textos
{
    // ── La ventana ──────────────────────────────────────────────────────────
    // No hay barra de título (WindowStyle="None"), pero este texto sale en la
    // barra de tareas y en Alt+Tab, así que se ve igual.
    public string ReproductorTitulo => Idioma.Elegir("Player", "Reproductor");

    // ── Plan B cuando el códec no se puede decodificar ───────────────────────
    public string ReproductorAbrirEnSistema =>
        Idioma.Elegir("Open in the system player", "Abrir en el reproductor del sistema");

    // Los dos literales que estaban concatenados en el código se unifican aquí:
    // partir una frase en trozos hace imposible traducirla bien. {0} es el
    // mensaje del sistema, que llega en el idioma de Windows y no se toca.
    public string ReproductorFalloCodec => Idioma.Elegir(
        "This video cannot be played here (codec not supported by the system decoder): {0}",
        "Este vídeo no se puede reproducir aquí (códec no soportado por el decodificador del sistema): {0}");

    public string ReproductorNoSePudoAbrir => Idioma.Elegir(
        "The video could not be opened: {0}",
        "No se pudo abrir el vídeo: {0}");

    // ── La espera ───────────────────────────────────────────────────────────
    public string ReproductorDescargandoNube =>
        Idioma.Elegir("Downloading the video from the cloud…", "Descargando el vídeo de la nube…");

    public string ReproductorAbriendoVideo =>
        Idioma.Elegir("Opening the video…", "Abriendo el vídeo…");

    public string ReproductorCargando => Idioma.Elegir("Loading…", "Cargando…");

    // ── Aviso de fichero en la nube ─────────────────────────────────────────
    // Cabe en una sola línea del chip: si crece, se sale del ancho del título.
    public string ReproductorChipNube =>
        Idioma.Elegir("In the cloud · downloading to play it", "En la nube · descargando para verlo");

    // La misma idea dentro del globo de la previa, en minúscula y en dos
    // palabras: ahí solo hay 192 px de ancho.
    public string ReproductorPreviaEnNube => Idioma.Elegir("in the cloud", "en la nube");

    // ── Rótulos de ayuda del transporte ─────────────────────────────────────
    // El doble espacio antes del paréntesis es deliberado: separa la acción de
    // su atajo sin meter un signo de puntuación.
    public string ReproductorAtrasTip =>
        Idioma.Elegir("Back 10 s  (left arrow)", "Atrás 10 s  (flecha izquierda)");

    public string ReproductorPlayTip =>
        Idioma.Elegir("Play / pause  (space)", "Reproducir / pausar  (espacio)");

    public string ReproductorAdelanteTip =>
        Idioma.Elegir("Forward 10 s  (right arrow)", "Adelante 10 s  (flecha derecha)");

    public string ReproductorMudoTip => Idioma.Elegir("Mute  (M)", "Silenciar  (M)");

    public string ReproductorPantallaTip =>
        Idioma.Elegir("Full screen  (F)", "Pantalla completa  (F)");

    // Un fichero a medio bajar de la nube falla EXACTAMENTE igual que uno con un
    // códec que el sistema no trae. Decir «códec» ahí manda a buscar un problema
    // que no existe, así que se distingue y se reintenta solo.
    public string ReproductorEsperandoLaNube => Idioma.Elegir(
        "This file is still coming down from the cloud. It will start on its own when it finishes.",
        "Este fichero todavía se está bajando de la nube. Empezará solo en cuanto termine.");

    // Qué códec es y qué hacer. Un código en hexadecimal no le dice a nadie qué
    // hacer; «es AV1, instala su extensión» sí. {0} = el códec · {1} = la extensión.
    public string ReproductorFaltaExtension => Idioma.Elegir(
        "This video is {0}, and Windows does not ship that decoder. Install «{1}» from the Microsoft Store, or open it in the system player.",
        "Este vídeo es {0}, y Windows no trae ese decodificador de fábrica. Instala «{1}» desde la Microsoft Store, o ábrelo en el reproductor del sistema.");

    // {0} = el códec, tal y como lo llama ffprobe.
    public string ReproductorCodecDesconocido => Idioma.Elegir(
        "This video uses the {0} codec, which the Windows decoder does not recognise. Open it in the system player.",
        "Este vídeo usa el códec {0}, que el decodificador de Windows no reconoce. Ábrelo en el reproductor del sistema.");

    public string ReproductorCodecSinSaber => Idioma.Elegir(
        "This video cannot be played here and it was not possible to find out which codec it uses. Open it in the system player.",
        "Este vídeo no se puede reproducir aquí y no se ha podido averiguar qué códec usa. Ábrelo en el reproductor del sistema.");
}
