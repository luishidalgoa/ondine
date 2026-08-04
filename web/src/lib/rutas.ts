// Un solo sitio donde se compone una ruta del sitio.
//
// Hace falta porque `import.meta.env.BASE_URL` NO es homogéneo: vale
// «/ondine/» cuando el sitio cuelga de una subcarpeta y «/» cuando se sirve
// desde la raíz de un dominio propio. Pegarle un trozo detrás con una plantilla
// da «/ondine//img/x» en un caso y «//img/x» en el otro, y ese segundo es peor
// de lo que parece: «//img/x» no es una ruta, es una URL de protocolo relativo,
// o sea que el navegador se va a buscar un servidor llamado «img».
//
// Pasó de verdad al mover el sitio a ondine.hdglabs.com: la compilación se cayó
// con «Invalid URL» y todas las imágenes habrían apuntado fuera.

/** La base sin barra final. Cadena vacía cuando el sitio va en la raíz. */
export const BASE = import.meta.env.BASE_URL.replace(/\/+$/, "");

/** Compone una ruta del sitio. `ruta("img/x.jpg")` y `ruta("/img/x.jpg")` valen igual. */
export function ruta(trozo = ""): string {
  const limpio = trozo.replace(/^\/+/, "");
  return `${BASE}/${limpio}`;
}

/** La misma ruta, absoluta. Para `hreflang` y las etiquetas de compartir. */
export function rutaAbsoluta(trozo: string, sitio: URL | undefined): string {
  return sitio ? new URL(ruta(trozo), sitio).href : ruta(trozo);
}
