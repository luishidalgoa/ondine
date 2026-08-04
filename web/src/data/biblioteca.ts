import type { Idioma } from "../i18n/textos";

// La biblioteca de ejemplo. Dos estados del MISMO conjunto: cómo llega y cómo
// queda, ahora en los dos idiomas.
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
// Los nombres de fichero son de los que escribe una persona a mano, no de
// descarga: nada de AMZN, WEB-DL, x265 ni nombres de grupo. Y están traducidos,
// no copiados: un inglés no llama a un fichero «capitulo nuevo.mkv», y si el
// desorden no se reconoce, la sección no funciona.

export const SERIE: Record<Idioma, string> = {
  es: "Los Simpson",
  en: "The Simpsons",
};

export type Variante = "onda" | "arco" | "horizonte" | "columna";

interface Version {
  /** Cómo llega el fichero. */
  crudo: string;
  /** Título del episodio que Ondine resuelve. */
  titulo: string;
}

export interface Ficha {
  /** Temporada y episodio. También da el nombre del fichero de imagen. */
  episodio: string;
  /** Tinte de reserva, DEBAJO del fotograma: si la imagen falta, la ficha se
   *  degrada a un rectángulo de la paleta y no a un icono de imagen rota. */
  tinte: string;
  /** Geometría que se dibuja cuando no hay fotograma. */
  variante: Variante;
  textos: Record<Idioma, Version>;
}

export const BIBLIOTECA: Ficha[] = [
  {
    episodio: "S01E01", tinte: "#2E5B5E", variante: "onda",
    textos: {
      es: { crudo: "capitulo 1 (2).mkv", titulo: "Sin blanca Navidad" },
      en: { crudo: "episode 1 (2).mkv", titulo: "Simpsons Roasting on an Open Fire" },
    },
  },
  {
    episodio: "S02E01", tinte: "#5C4136", variante: "arco",
    textos: {
      es: { crudo: "bart suspende.avi", titulo: "Bart suspende" },
      en: { crudo: "bart fails.avi", titulo: "Bart Gets an F" },
    },
  },
  {
    episodio: "S02E02", tinte: "#31415F", variante: "horizonte",
    textos: {
      es: { crudo: "Sin titulo (copia).mkv", titulo: "Simpson y Dalila" },
      en: { crudo: "Untitled (copy).mkv", titulo: "Simpson and Delilah" },
    },
  },
  {
    episodio: "S04E03", tinte: "#434A3A", variante: "columna",
    textos: {
      es: { crudo: "cap nuevo BUENO.mkv", titulo: "Homer, el hereje" },
      en: { crudo: "new ep GOOD.mkv", titulo: "Homer the Heretic" },
    },
  },
  {
    episodio: "S04E12", tinte: "#4C3659", variante: "arco",
    textos: {
      es: { crudo: "simpsons temporada 4 - 12.mkv", titulo: "Marge contra el monorraíl" },
      en: { crudo: "simpsons season 4 - 12.mkv", titulo: "Marge vs. the Monorail" },
    },
  },
  {
    episodio: "S05E02", tinte: "#2F4A54", variante: "onda",
    textos: {
      es: { crudo: "los simpson 5x02.avi", titulo: "Cabo Miedo" },
      en: { crudo: "the simpsons 5x02.avi", titulo: "Cape Feare" },
    },
  },
  {
    episodio: "S05E17", tinte: "#574538", variante: "columna",
    textos: {
      es: { crudo: "video_2019.avi", titulo: "Bart consigue un elefante" },
      en: { crudo: "video_2019.avi", titulo: "Bart Gets an Elephant" },
    },
  },
  {
    episodio: "S06E24", tinte: "#35496A", variante: "horizonte",
    textos: {
      es: { crudo: "descarga final.mp4", titulo: "El limonero de Troya" },
      en: { crudo: "final download.mp4", titulo: "Lemon of Troy" },
    },
  },
  {
    episodio: "S07E21", tinte: "#3D3A5C", variante: "horizonte",
    textos: {
      es: { crudo: "simpson 7x21.mkv", titulo: "22 historias cortas sobre Springfield" },
      en: { crudo: "simpsons 7x21.mkv", titulo: "22 Short Films About Springfield" },
    },
  },
  {
    episodio: "S07E24", tinte: "#3D5A52", variante: "columna",
    textos: {
      es: { crudo: "cap sin nombre 3.mkv", titulo: "Homerpalooza" },
      en: { crudo: "unnamed ep 3.mkv", titulo: "Homerpalooza" },
    },
  },
  {
    episodio: "S08E23", tinte: "#5A4038", variante: "onda",
    textos: {
      es: { crudo: "temp8_23.mkv", titulo: "El enemigo de Homer" },
      en: { crudo: "season8_23.mkv", titulo: "Homer's Enemy" },
    },
  },
  {
    episodio: "S09E01", tinte: "#384168", variante: "arco",
    textos: {
      es: { crudo: "nuevo_cap (1).mkv", titulo: "La ciudad de Nueva York contra Homer" },
      en: { crudo: "new_ep (1).mkv", titulo: "The City of New York vs. Homer Simpson" },
    },
  },
];
