// @ts-check
import { defineConfig } from "astro/config";
import tailwindcss from "@tailwindcss/vite";
import sitemap from "@astrojs/sitemap";

// El sitio vive en su propio dominio, así que se sirve desde la raíz y no
// cuelga de ninguna subcarpeta. Todo lo demás se compone a partir de estas dos
// constantes; si algún día cambia el dominio, se tocan aquí y ya está.
//
// El dominio también está en `public/CNAME`, que es de donde lo lee GitHub
// Pages en cada despliegue. Si se quita ese fichero, Pages vuelve solo a
// luishidalgoa.github.io y el sitio se cae, porque la base ya no coincide.
const BASE = "";
const SITIO = "https://ondine.hdglabs.com";

export default defineConfig({
  site: SITIO,
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

  integrations: [
    sitemap({
      // La página de comprobación de fotogramas no es contenido del sitio.
      filter: (pagina) => !pagina.includes("/caps"),
      // Con esto el sitemap declara que cada página tiene su equivalente en el
      // otro idioma. Es lo que evita que el buscador elija una y entierre la
      // otra por parecerle contenido duplicado.
      i18n: {
        defaultLocale: "en",
        locales: { en: "en", es: "es" },
      },
    }),
  ],

  vite: {
    plugins: [tailwindcss()],
  },
});
