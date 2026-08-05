// Todas las cadenas visibles del sitio, en los dos idiomas.
//
// Están juntas y no repartidas por los componentes a propósito: repartidas, la
// mitad se queda sin traducir y nadie se entera hasta que alguien lo ve en
// producción. Aquí, si falta una, TypeScript no compila.
//
// Cuidado con los números: en español el decimal es coma y el porcentaje va
// separado («0,6 s», «80-90 %»), y en inglés es punto y pegado («0.6 s»,
// «80-90%»). Por eso las cifras también viven aquí y no en el componente.

export const IDIOMAS = ["es", "en"] as const;
export type Idioma = (typeof IDIOMAS)[number];

export const NOMBRE_IDIOMA: Record<Idioma, string> = {
  es: "Español",
  en: "English",
};

const es = {
  meta: {
    titulo: "Ondine · Ordena bibliotecas de series y películas",
    descripcion:
      "Identifica cada episodio contra un catálogo, parte los ficheros que traen dos capítulos pegados y quita las pistas que no usas sin recomprimir. Los nombres quedan escritos como los metadatos esperan encontrarlos.",
    tituloSobreMi: "Sobre mí · Ondine",
    altVistaPrevia:
      "La misma biblioteca antes y después de pasarla por Ondine, con el nombre de Ondine encima.",
    descripcionSobreMi: "Quién hay detrás de Ondine y con qué está construido.",
  },

  armazon: {
    codigo: "Código",
    descargar: "Descargar",
    idioma: "Idioma",
    licencia: "Ondine. Licencia MIT.",
    sobreMi: "Sobre mí",
    repositorio: "Repositorio",
    versiones: "Versiones",
    cambios: "Cambios",
    hojaDeRuta: "Hoja de ruta",
  },

  hero: {
    titular: "Ordena bibliotecas de series y películas",
    bajada:
      "Cada capítulo con su título, su número y su temporada. Escrito como los metadatos esperan encontrarlo.",
    descargar: "Descargar para Windows",
    terminal: "Instalar por terminal",
  },

  // El cuadro que abre ese botón. Antes copiaba la orden directamente: había
  // acuse de recibo, pero seguía siendo una línea a ciegas, sin decir para qué
  // sistema era ni qué hacía. Y en Windows copiaba algo que no funciona.
  terminalModal: {
    titulo: "Instalar por terminal",
    detectado: "el tuyo",
    copiar: "Copiar la orden",
    copiado: "Copiado al portapapeles",
    notaUnix:
      "Detecta tu procesador, baja el binario que le toca y lo deja en ~/.local/bin. Sin sudo y sin tocar nada fuera de tu carpeta personal. Necesita ffmpeg instalado; si falta, te lo dice.",
    notaWindows:
      "En Windows no hay guion de instalación: el de arriba es un script de shell y en Windows se para solo. Esto se baja la herramienta de terminal suelta con PowerShell. Si lo que quieres es la aplicación de escritorio, está en la descarga normal.",
    pieA: "Todos los paquetes, y sus sumas de verificación, están en ",
    pieEnlace: "la última versión",
    pieB: ".",
  },

  compatibilidad: {
    entradilla:
      "Ondine no se conecta a tu servidor ni le pide nada. Deja los ficheros escritos como el catálogo de cada uno espera encontrarlos, y el escaneo hace el resto.",
  },

  problema: {
    rotulo: "EL PROBLEMA",
    titulo: "El servidor no es tonto. Le has dado datos malos.",
    cuerpo:
      "Plex, Jellyfin y Kodi enseñan una biblioteca preciosa, pero solo si los ficheros ya vienen bien nombrados. Cuando no, se rinden.",
    crudos: [
      "Cap 12 (2).mkv",
      "episodio final BUENO.mkv",
      "Temporada2_04-05.mkv",
      "Sin titulo (copia).mkv",
      "los simpson 3x07.avi",
      "capitulo nuevo.mkv",
      "descarga final.mp4",
      "video_2019.avi",
      "sin nombre 3.mkv",
      "grabacion (copia 2).mkv",
    ],
    desconocido: "Desconocido",
    pie: "Sin título, sin número, sin sinopsis y sin portada. Lo único que el servidor sabe leer es cuánto dura.",
  },

  partir: {
    rotulo: "LO QUE NO HACE NINGUNA OTRA",
    titulo: "Un fichero. Dos capítulos dentro.",
    cuerpo:
      "Ondine entiende que ese vídeo de 44 minutos son en realidad dos episodios pegados, encuentra la junta y lo parte. Cada mitad sale con su nombre.",
    original: "los simpson 7x21-22.avi",
    meta: "44:28 · 1 fichero",
    piezas: [
      { episodio: "E21", titulo: "22 historias cortas sobre Springfield", tramo: "0:00 - 21:47" },
      { episodio: "E22", titulo: "Mucho Apu y pocas nueces", tramo: "21:47 - 44:28" },
    ],
  },

  ordenar: {
    titulo: "El catálogo no lo tecleas tú.",
    cuerpoA:
      "Ondine te escribe el encargo para que cualquier IA convierta un anexo de episodios en su catálogo. Lo ejecutas donde quieras y pegas el resultado: no hay clave ni servicio de por medio. Después coteja cada vídeo contra ese catálogo y ",
    cuerpoDestacado: "propone",
    cuerpoB: ". No renombra nada sin que se lo digas.",
    json: '{ "num": 12, "temp": 4, "titulo": "Marge contra el monorraíl" }',
    correctos: "CORRECTOS",
    conflictos: "CONFLICTOS",
    tabla: "Nombres propuestos",
    colOriginal: "Fichero original",
    colPropuesta: "Propuesta",
    colEstado: "Estado",
    correcto: "Correcto",
  },

  tamano: {
    rotulo: "EL TAMAÑO",
    titulo: "Adelgaza sin recomprimir.",
    cuerpoA:
      "Un vídeo trae doblajes y subtítulos que nunca vas a usar. Quitarlos no es recodificar: es descartar pistas. El vídeo que queda es ",
    cuerpoDestacado: "idéntico bit a bit",
    cuerpoB: " al de partida.",
    pesoAntes: "1 GB",
    pesoDespues: "133 MB",
    unidad: "MB",
    en: "en",
    crono: "0,6 s",
    pistas: [
      { lengua: "spa", texto: "audio · se queda", queda: true },
      { lengua: "eng", texto: "audio · se va", queda: false },
      { lengua: "eng", texto: "subtítulos · se va", queda: false },
    ],
    sello: "vídeo idéntico",
    subtitulo: "Y si sí quieres recomprimir.",
    cuerpo2A: "Reduce entre un ",
    cuerpo2Destacado: "80 y un 90 %",
    cuerpo2B:
      " con aceleración por hardware. Pero antes de empezar te enseña un pronóstico del tamaño final, y si quieres afinarlo lo mide de verdad codificando muestras cortas. Ese «te lo digo antes de hacerlo» es el argumento, no el porcentaje.",
  },

  comparativa: {
    titulo: "La misma carpeta, antes y después.",
    antes: "ANTES",
    despues: "DESPUÉS",
  },

  spot: {
    activarSonido: "Activar sonido",
    silenciar: "Silenciar",
    sinVideo: "Tu navegador no reproduce vídeo.",
    descargarSpot: "Descarga el spot",
  },

  ficha: {
    fotogramaDe: "Fotograma de",
  },

  descarga: {
    titulo: "Ordena tu biblioteca.",
    apoyo: "Escritorio para Windows, y binarios para macOS y Linux.",
    boton: "Descargar",
    modalTitulo: "Elige tu sistema",
    cerrar: "Cerrar",
    terminalTitulo: "DESDE EL TERMINAL",
    terminalNota:
      "Detecta tu sistema y tu procesador, baja el binario que toca y lo deja en ~/.local/bin. Sin sudo y sin tocar nada fuera de tu carpeta personal.",
    terminalOrden: "curl -fsSL https://ondine.hdglabs.com/install.sh | sh",
    terminalManual: "O a mano, con el paquete de arriba:",
    terminalComandos: "tar xzf ondine-linux-x64.tar.gz\n./ondine --help",
    pieA: "Todas las versiones y sus notas están en ",
    pieEnlace: "la página de versiones",
    pieB: ".",
    grupos: [
      {
        sistema: "Windows",
        opciones: [
          { etiqueta: "Instalador", detalle: "Lo habitual. Crea accesos y se actualiza solo.", peso: "55 MB" },
          { etiqueta: "Portable", detalle: "Un ejecutable suelto. No instala nada.", peso: "13 MB" },
        ],
      },
      {
        sistema: "macOS",
        opciones: [
          { etiqueta: "Apple Silicon", detalle: "M1 en adelante.", peso: "5 MB" },
          { etiqueta: "Intel", detalle: "Mac anteriores a 2020.", peso: "6 MB" },
        ],
      },
      {
        sistema: "Linux",
        opciones: [
          { etiqueta: "x64", detalle: "La mayoría de equipos y servidores.", peso: "6 MB" },
          { etiqueta: "ARM64", detalle: "Raspberry Pi y equipos ARM.", peso: "5 MB" },
        ],
      },
    ],
    promesas: [
      "Nunca toca los originales salvo que se lo pidas, y entonces van a la papelera, no a borrado.",
      "Todo ocurre en tu máquina. No manda tus ficheros a ninguna parte.",
      "Código abierto, licencia MIT, en GitHub.",
    ],
  },

  sobreMi: {
    volver: "Volver a la portada",
    rotulo: "SOBRE MÍ",
    nombre: "Luis Hidalgo",
    bio: "Desarrollador full-stack. Diseño y construyo productos de principio a fin: de la base de datos a la interfaz, pasando por infraestructura, integraciones y diseño. Ondine es uno de esos productos, y este sitio también.",
    verPortfolio: "Ver mi portfolio",
    codigoDeOndine: "Código de Ondine",
    queEsTitulo: "Qué es Ondine",
    queEsCuerpo:
      "Una aplicación de escritorio para Windows, y una herramienta de terminal para Linux, macOS y Windows, que prepara una biblioteca de series y películas antes de que Plex, Jellyfin o Kodi la escaneen. Esos servidores enseñan una biblioteca preciosa, pero solo si los ficheros ya vienen bien nombrados. Ondine es el paso intermedio.",
    queHace: [
      "Identifica cada episodio contra un catálogo y propone su nombre",
      "Parte los ficheros que traen dos capítulos pegados",
      "Quita doblajes y subtítulos sin recomprimir el vídeo",
      "Comprime con aceleración por hardware y pronostica el tamaño antes",
      "Nunca toca los originales salvo que se lo pidas",
    ],
    hablamosTitulo: "¿Hablamos?",
    hablamosCuerpo:
      "Si quieres colaborar, contratarme o preguntarme cómo está hecho algo de esto, en mi portfolio están los enlaces, la experiencia, el resto de proyectos y las formas de contacto.",
    abrirPortfolio: "Abrir portfolio.hdglabs.com",
    pilaTitulo: "Con qué está construido",
    pila: [
      { area: "Escritorio", cosas: ["C# 13 sobre .NET 9", "WPF, con tema propio", "Cero dependencias de NuGet"] },
      { area: "Terminal", cosas: [".NET 9 multiplataforma", "Fichero único y autocontenido", "Recortado: de 68 MB a 13"] },
      { area: "Motor de vídeo", cosas: ["ffmpeg y ffprobe", "HEVC, H.264 y AV1", "MKV, MP4 y WebM"] },
      { area: "Aceleración", cosas: ["Intel Quick Sync", "NVIDIA NVENC", "AMD AMF", "Respaldo por software si no hay"] },
      { area: "Catálogo", cosas: ["Encargo escrito para la IA que tú uses", "Sin clave ni servicio propio", "Esquema reindex/1.0"] },
      { area: "Distribución", cosas: ["GitHub Actions", "Inno Setup para el instalador", "Autoactualización desde Releases", "96 pruebas en cada cambio"] },
    ],
  },
};

// El inglés replica la estructura exacta. TypeScript comprueba que no falte
// ninguna clave: si alguien añade una cadena en español y se olvida aquí, la
// compilación se cae en vez de salir a producción a medio traducir.
const en: typeof es = {
  meta: {
    titulo: "Ondine · Get your TV and film library in order",
    descripcion:
      "Matches every episode against a catalogue, splits the files that carry two episodes stuck together, and drops the tracks you never use without re-encoding. Names end up written the way metadata expects to find them.",
    tituloSobreMi: "About · Ondine",
    altVistaPrevia:
      "The same library before and after running it through Ondine, with the Ondine name over it.",
    descripcionSobreMi: "Who is behind Ondine, and what it is built with.",
  },

  armazon: {
    codigo: "Code",
    descargar: "Download",
    idioma: "Language",
    licencia: "Ondine. MIT licence.",
    sobreMi: "About",
    repositorio: "Repository",
    versiones: "Releases",
    cambios: "Changelog",
    hojaDeRuta: "Roadmap",
  },

  hero: {
    titular: "Get your TV and film library in order",
    bajada:
      "Every episode with its title, its number and its season. Written the way metadata expects to find it.",
    descargar: "Download for Windows",
    terminal: "Install from the terminal",
  },

  terminalModal: {
    titulo: "Install from the terminal",
    detectado: "yours",
    copiar: "Copy the command",
    copiado: "Copied to the clipboard",
    notaUnix:
      "It works out your processor, downloads the right binary and drops it in ~/.local/bin. No sudo, and nothing touched outside your home folder. It needs ffmpeg installed; if it is missing, it tells you.",
    notaWindows:
      "There is no install script on Windows: the one above is a shell script and it stops on its own there. This downloads the standalone terminal tool with PowerShell. If what you want is the desktop app, it is in the normal download.",
    pieA: "Every package, and its checksums, live in ",
    pieEnlace: "the latest release",
    pieB: ".",
  },

  compatibilidad: {
    entradilla:
      "Ondine never connects to your server or asks it for anything. It leaves the files written the way each catalogue expects to find them, and the scan does the rest.",
  },

  problema: {
    rotulo: "THE PROBLEM",
    titulo: "Your server is not the problem. The data you gave it is.",
    cuerpo:
      "Plex, Jellyfin and Kodi show you a beautiful library, but only when the files are already named properly. When they are not, they give up.",
    crudos: [
      "Ep 12 (2).mkv",
      "final episode GOOD.mkv",
      "Season2_04-05.mkv",
      "Untitled (copy).mkv",
      "the simpsons 3x07.avi",
      "new episode.mkv",
      "final download.mp4",
      "video_2019.avi",
      "no name 3.mkv",
      "recording (copy 2).mkv",
    ],
    desconocido: "Unknown",
    pie: "No title, no number, no synopsis and no artwork. The only thing the server can read is how long it runs.",
  },

  partir: {
    rotulo: "WHAT NOTHING ELSE DOES",
    titulo: "One file. Two episodes inside.",
    cuerpo:
      "Ondine works out that a 44-minute video is really two episodes stuck together, finds the seam and splits it. Each half comes out with its own name.",
    original: "the simpsons 7x21-22.avi",
    meta: "44:28 · 1 file",
    piezas: [
      { episodio: "E21", titulo: "22 Short Films About Springfield", tramo: "0:00 - 21:47" },
      { episodio: "E22", titulo: "Much Apu About Nothing", tramo: "21:47 - 44:28" },
    ],
  },

  ordenar: {
    titulo: "You don't type the catalogue yourself.",
    cuerpoA:
      "Ondine writes the brief so that whichever AI you use can turn an episode list into its catalogue. You run it wherever you like and paste the result back: no key and no service in between. Then it matches every video against that catalogue and ",
    cuerpoDestacado: "proposes",
    cuerpoB: ". It renames nothing until you say so.",
    json: '{ "num": 12, "temp": 4, "titulo": "Marge vs. the Monorail" }',
    correctos: "CORRECT",
    conflictos: "CONFLICTS",
    tabla: "Proposed names",
    colOriginal: "Original file",
    colPropuesta: "Proposed",
    colEstado: "Status",
    correcto: "Correct",
  },

  tamano: {
    rotulo: "SIZE",
    titulo: "Slim it down without re-encoding.",
    cuerpoA:
      "A video carries dubs and subtitles you are never going to use. Removing them is not re-encoding: it is dropping tracks. What is left is ",
    cuerpoDestacado: "identical, bit for bit",
    cuerpoB: " to what you started with.",
    pesoAntes: "1 GB",
    pesoDespues: "133 MB",
    unidad: "MB",
    en: "in",
    crono: "0.6 s",
    pistas: [
      { lengua: "spa", texto: "audio · kept", queda: true },
      { lengua: "eng", texto: "audio · dropped", queda: false },
      { lengua: "eng", texto: "subtitles · dropped", queda: false },
    ],
    sello: "identical video",
    subtitulo: "And when you do want to re-encode.",
    cuerpo2A: "It cuts ",
    cuerpo2Destacado: "80 to 90%",
    cuerpo2B:
      " with hardware acceleration. But before it starts it shows you a forecast of the final size, and if you want that sharper it measures for real by encoding short samples. That «I'll tell you before I do it» is the argument, not the percentage.",
  },

  comparativa: {
    titulo: "The same folder, before and after.",
    antes: "BEFORE",
    despues: "AFTER",
  },

  spot: {
    activarSonido: "Turn sound on",
    silenciar: "Mute",
    sinVideo: "Your browser cannot play this video.",
    descargarSpot: "Download the spot",
  },

  ficha: {
    fotogramaDe: "Still from",
  },

  descarga: {
    titulo: "Get your library in order.",
    apoyo: "Desktop app for Windows, and binaries for macOS and Linux.",
    boton: "Download",
    modalTitulo: "Choose your system",
    cerrar: "Close",
    terminalTitulo: "FROM THE TERMINAL",
    terminalNota:
      "It works out your system and CPU, downloads the right binary and drops it in ~/.local/bin. No sudo, and nothing outside your home directory is touched.",
    terminalOrden: "curl -fsSL https://ondine.hdglabs.com/install.sh | sh",
    terminalManual: "Or by hand, with the package above:",
    terminalComandos: "tar xzf ondine-linux-x64.tar.gz\n./ondine --help",
    pieA: "Every release and its notes are on ",
    pieEnlace: "the releases page",
    pieB: ".",
    grupos: [
      {
        sistema: "Windows",
        opciones: [
          { etiqueta: "Installer", detalle: "The usual one. Adds shortcuts and updates itself.", peso: "55 MB" },
          { etiqueta: "Portable", detalle: "A single executable. Installs nothing.", peso: "13 MB" },
        ],
      },
      {
        sistema: "macOS",
        opciones: [
          { etiqueta: "Apple Silicon", detalle: "M1 and later.", peso: "5 MB" },
          { etiqueta: "Intel", detalle: "Macs from before 2020.", peso: "6 MB" },
        ],
      },
      {
        sistema: "Linux",
        opciones: [
          { etiqueta: "x64", detalle: "Most desktops and servers.", peso: "6 MB" },
          { etiqueta: "ARM64", detalle: "Raspberry Pi and ARM machines.", peso: "5 MB" },
        ],
      },
    ],
    promesas: [
      "It never touches the originals unless you ask it to, and then they go to the recycle bin, not to deletion.",
      "Everything happens on your machine. It sends your files nowhere.",
      "Open source, MIT licence, on GitHub.",
    ],
  },

  sobreMi: {
    volver: "Back to the home page",
    rotulo: "ABOUT ME",
    nombre: "Luis Hidalgo",
    bio: "Full-stack developer. I design and build products end to end: from the database to the interface, taking in infrastructure, integrations and design along the way. Ondine is one of those products, and so is this site.",
    verPortfolio: "See my portfolio",
    codigoDeOndine: "Ondine source",
    queEsTitulo: "What Ondine is",
    queEsCuerpo:
      "A desktop app for Windows, and a terminal tool for Linux, macOS and Windows, that gets a TV and film library ready before Plex, Jellyfin or Kodi scan it. Those servers show you a beautiful library, but only when the files are already named properly. Ondine is the step in between.",
    queHace: [
      "Matches every episode against a catalogue and proposes its name",
      "Splits the files that carry two episodes stuck together",
      "Drops dubs and subtitles without re-encoding the video",
      "Compresses with hardware acceleration, and forecasts the size first",
      "Never touches the originals unless you ask it to",
    ],
    hablamosTitulo: "Shall we talk?",
    hablamosCuerpo:
      "If you want to work together, hire me, or ask how any of this is put together, my portfolio has the links, the experience, the other projects and the ways to get in touch.",
    abrirPortfolio: "Open portfolio.hdglabs.com",
    pilaTitulo: "What it is built with",
    pila: [
      { area: "Desktop", cosas: ["C# 13 on .NET 9", "WPF, with its own theme", "Zero NuGet dependencies"] },
      { area: "Terminal", cosas: [".NET 9, cross-platform", "Single self-contained file", "Trimmed: from 68 MB to 13"] },
      { area: "Video engine", cosas: ["ffmpeg and ffprobe", "HEVC, H.264 and AV1", "MKV, MP4 and WebM"] },
      { area: "Acceleration", cosas: ["Intel Quick Sync", "NVIDIA NVENC", "AMD AMF", "Software fallback when there is none"] },
      { area: "Catalogue", cosas: ["A brief written for whichever AI you use", "No key, no service of its own", "reindex/1.0 schema"] },
      { area: "Distribution", cosas: ["GitHub Actions", "Inno Setup for the installer", "Self-updating from Releases", "96 tests on every change"] },
    ],
  },
};

const TODOS = { es, en } satisfies Record<Idioma, typeof es>;

/** Devuelve las cadenas del idioma pedido. Ante la duda, español. */
export function textos(idioma?: string | null) {
  return TODOS[(idioma as Idioma) in TODOS ? (idioma as Idioma) : "es"];
}

/** El idioma en curso, ya saneado. */
export function idiomaDe(idioma?: string | null): Idioma {
  return (idioma as Idioma) in TODOS ? (idioma as Idioma) : "es";
}
