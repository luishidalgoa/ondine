import { readdirSync } from "node:fs";
import { join } from "node:path";
import { ruta } from "../lib/rutas";

// Resuelve el fotograma de cada episodio leyendo la carpeta EN TIEMPO DE
// COMPILACIÓN, en vez de dar por hecho un nombre exacto.
//
// La primera versión pedía `s01e01.jpg` clavado. Eso obliga a acertar con el
// cero de la temporada y con la extensión cada vez que se añade un capítulo, y
// cuando falla no avisa: la ficha se queda en su rectángulo de color y parece
// que la imagen no está puesta. Es un mal reparto del trabajo, porque el que
// tiene que ser exacto es el programa, no la persona que suelta ficheros en
// una carpeta.
//
// Ahora vale cualquiera de estos, y en jpg, jpeg, png, webp o avif:
//     s01e01.jpg   s1e01.webp   S1E1.png   los simpson 1x01.jpg   s2e01..jpg
//
// Lo único que hace falta es que en el nombre aparezca la temporada y el
// episodio de alguna de las formas habituales.

const CARPETA = "public/img/caps";
const EXTENSIONES = new Set([".jpg", ".jpeg", ".png", ".webp", ".avif"]);

/** `1` y `01` son la misma temporada. */
const clave = (temporada: number, episodio: number) =>
  `s${String(temporada).padStart(2, "0")}e${String(episodio).padStart(2, "0")}`;

/**
 * Saca temporada y episodio del nombre del fichero. Reconoce las dos formas
 * que usa la gente: `s01e02` y `1x02`.
 */
function leerCodigo(nombre: string): string | null {
  const n = nombre.toLowerCase();
  const sxxexx = n.match(/s(\d{1,2})\s*[e_. -]\s*(\d{1,3})/);
  if (sxxexx) return clave(Number(sxxexx[1]), Number(sxxexx[2]));
  const nxnn = n.match(/(\d{1,2})\s*x\s*(\d{1,3})/);
  if (nxnn) return clave(Number(nxnn[1]), Number(nxnn[2]));
  return null;
}

function mapear(): Map<string, string> {
  const encontrados = new Map<string, string>();
  let ficheros: string[];
  try {
    ficheros = readdirSync(join(process.cwd(), CARPETA));
  } catch {
    // La carpeta puede no existir todavía. No es un error: significa que aún
    // no hay fotogramas y las fichas se quedan en su rectángulo de color.
    return encontrados;
  }

  for (const f of ficheros) {
    const punto = f.lastIndexOf(".");
    if (punto < 0) continue;
    if (!EXTENSIONES.has(f.slice(punto).toLowerCase())) continue;

    const codigo = leerCodigo(f.slice(0, punto));
    if (codigo && !encontrados.has(codigo)) encontrados.set(codigo, f);
  }
  return encontrados;
}

const CAPS = mapear();

/** Devuelve la ruta pública del fotograma, o null si todavía no está puesto. */
export function fotograma(episodio: string): string | null {
  const codigo = leerCodigo(episodio);
  const fichero = codigo ? CAPS.get(codigo) : undefined;
  if (!fichero) return null;
  // El nombre puede traer espacios o acentos, así que se codifica.
  return ruta(`img/caps/${encodeURIComponent(fichero)}`);
}

/** Para poder avisar por consola de cuántos faltan al compilar. */
export const CAPS_ENCONTRADOS = CAPS;
