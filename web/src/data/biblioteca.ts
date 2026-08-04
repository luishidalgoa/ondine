// La biblioteca de ejemplo. Dos estados del MISMO conjunto: cómo llega y cómo
// queda.
//
// Es una serie internacional y conocida a propósito: quien llega a esta página
// tiene que reconocer su propia carpeta en tres segundos, y con títulos
// inventados eso no pasa.
//
// Lo que sí se mantiene es que NO se reproduce ninguna imagen ajena. El nombre
// de un episodio es texto y nombrarlo para explicar qué hace una herramienta es
// legítimo; una carátula o un fotograma son otra cosa. Las portadas de aquí son
// geometría plana de la paleta, y una de las cuatro variantes es la propia
// marca de Ondine.
//
// Y los nombres de fichero son de los que escribe una persona a mano, no de
// descarga: nada de AMZN, WEB-DL, x265 ni nombres de grupo. El desorden de una
// casa se parece a esto.

export const SERIE = "Los Simpson";

export type Variante = "onda" | "arco" | "horizonte" | "columna";

export interface Ficha {
  /** Cómo llega el fichero. */
  crudo: string;
  /** Título del episodio que Ondine resuelve. */
  titulo: string;
  /** Temporada y episodio ya identificados. También da el nombre del fichero
   *  de imagen: S01E01 busca `img/caps/s01e01.jpg`. */
  episodio: string;
  /** Tinte de reserva. Va DEBAJO del fotograma, así que si la imagen falta o
   *  todavía no está puesta, la ficha se degrada a un rectángulo de color de
   *  la paleta en vez de a un icono de imagen rota. */
  tinte: string;
  /** Geometría que se dibuja cuando no hay fotograma. */
  variante: Variante;
}

export const BIBLIOTECA: Ficha[] = [
  { crudo: "capitulo 1 (2).mkv",        titulo: "Sin blanca Navidad",                  episodio: "S01E01", tinte: "#2E5B5E", variante: "onda" },
  { crudo: "bart suspende.avi",         titulo: "Bart suspende",                       episodio: "S02E01", tinte: "#5C4136", variante: "arco" },
  { crudo: "Sin titulo (copia).mkv",    titulo: "Simpson y Dalila",                    episodio: "S02E02", tinte: "#31415F", variante: "horizonte" },
  { crudo: "cap nuevo BUENO.mkv",       titulo: "Homer, el hereje",                    episodio: "S04E03", tinte: "#434A3A", variante: "columna" },

  { crudo: "simpsons temporada 4 - 12.mkv", titulo: "Marge contra el monorraíl",       episodio: "S04E12", tinte: "#4C3659", variante: "arco" },
  { crudo: "los simpson 5x02.avi",      titulo: "Cabo Miedo",                          episodio: "S05E02", tinte: "#2F4A54", variante: "onda" },
  { crudo: "video_2019.avi",            titulo: "Bart consigue un elefante",           episodio: "S05E17", tinte: "#574538", variante: "columna" },
  { crudo: "descarga final.mp4",        titulo: "¿Quién disparó al señor Burns?",      episodio: "S06E25", tinte: "#35496A", variante: "horizonte" },

  { crudo: "simpson 7x21.mkv",          titulo: "22 historias cortas sobre Springfield", episodio: "S07E21", tinte: "#3D3A5C", variante: "horizonte" },
  { crudo: "cap sin nombre 3.mkv",      titulo: "Homerpalooza",                        episodio: "S07E24", tinte: "#3D5A52", variante: "columna" },
  { crudo: "temp8_23.mkv",              titulo: "El limonero de Troya",                episodio: "S08E23", tinte: "#5A4038", variante: "onda" },
  { crudo: "nuevo_cap (1).mkv",         titulo: "La ciudad de Nueva York contra Homer", episodio: "S09E01", tinte: "#384168", variante: "arco" },
];
