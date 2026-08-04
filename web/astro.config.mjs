// @ts-check
import { defineConfig } from "astro/config";
import tailwindcss from "@tailwindcss/vite";

// El sitio se publica en GitHub Pages desde el mismo repo, así que cuelga de
// /ondine.
//
// Cuando `ondine.hdglabs.com` esté apuntando aquí, esto pasa a "" y ya está:
// con dominio propio, Pages sirve desde la raíz. Es la ÚNICA línea que hay que
// tocar, porque todo lo demás lo compone a partir de ella.
const BASE = "/ondine";

export default defineConfig({
  site: "https://luishidalgoa.github.io",
  base: BASE,

  // El inglés es el idioma por defecto y va sin prefijo. Ondine se distribuye
  // por GitHub y su público es internacional; el castellano es el caso
  // particular, no al revés.
  i18n: {
    defaultLocale: "en",
    locales: ["en", "es"],
    routing: {
      prefixDefaultLocale: false,
    },
  },

  // Las rutas de la versión anterior, cuando el castellano no llevaba prefijo.
  // Estuvieron publicadas, así que se redirigen en vez de devolver un 404: una
  // URL que ha existido no se rompe porque hayamos cambiado de idea.
  //
  // El destino lleva `BASE` a mano porque Astro NO se lo añade: sin él, la
  // redirección sale como «/about» y se va a la raíz del dominio, no a la del
  // sitio. La clave de la izquierda sí es relativa al sitio.
  redirects: {
    "/en": `${BASE}/`,
    "/en/about": `${BASE}/about`,
    "/sobre-mi": `${BASE}/es/sobre-mi`,
  },

  vite: {
    plugins: [tailwindcss()],
  },
});
