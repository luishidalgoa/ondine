// @ts-check
import { defineConfig } from "astro/config";
import tailwindcss from "@tailwindcss/vite";

// El sitio se publica en GitHub Pages desde el mismo repo, así que cuelga de
// /Ondine. Si algún día hay dominio propio, `base` se queda en "/" y ya está.
export default defineConfig({
  site: "https://luishidalgoa.github.io",
  base: "/ondine",
  vite: {
    plugins: [tailwindcss()],
  },
});
