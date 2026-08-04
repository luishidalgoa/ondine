# Análisis de diales y de módulos informativos

Dos cosas distintas que la palabra «dial» puede significar aquí, y las dos hacen
falta. Primero los tres diales de la skill, que gobiernan cómo se ve todo.
Después el inventario de módulos, que es cuánta información del proyecto se
puede llegar a enseñar y con qué forma.

---

## Parte 1 · Los tres diales

**Lectura del encargo:** landing de herramienta de escritorio autoalojada, para
público de *homelab* y *self-hosting*, con lenguaje oscuro y técnico, tipografía
y paleta ya fijadas por una aplicación y un spot que existen.

Hay una tensión real en el encargo y conviene nombrarla en vez de promediarla:

- El público **premia la sobriedad y detesta lo pomposo**. Eso empuja los diales
  hacia abajo.
- La petición explícita es **la página más moderna y animada posible**. Eso los
  empuja hacia arriba.

No se resuelve poniéndolos en el medio, que da una página tibia. Se resuelve
subiendo el movimiento y bajando el adorno: **mucha animación, cero brillos**.
El movimiento sirve para demostrar lo que hace el producto, no para decorar.

| Dial | Valor | Por qué este y no otro |
|---|---|---|
| `DESIGN_VARIANCE` | **7** | La línea base de la skill son 8. Bajo a 7 porque cuatro secciones enseñan datos tabulares reales (nombres de fichero, pistas, tamaños) y esos necesitan retícula legible, no caos. Sigue prohibiendo el hero centrado y obliga a repartos desiguales. |
| `MOTION_INTENSITY` | **8** | Es el dial que fallé. Sube a 8, no a 6, porque **el producto es una transformación**: todo lo que hace Ondine es un estado que se convierte en otro. Esa es la única categoría de animación que la skill considera justificada por sí sola, la de narrar. A 8 entran anclaje con scroll, recorrido horizontal y paralaje. |
| `VISUAL_DENSITY` | **4** | Se queda. Es una landing, no un panel de control. Pero cuatro y no dos: este público lee datos y desconfía de la pantalla vacía con una frase enorme, que le suena a folleto. |

**Lo que estos valores obligan, en concreto:**

1. Todas las secciones se mueven al entrar. Ninguna es una lámina quieta.
2. Al menos dos secciones van **ancladas** con recorrido de scroll, no solo
   apareciendo por opacidad.
3. Hay **paralaje** de verdad: planos a distintas velocidades, no un fundido.
4. Y a cambio, con `VARIANCE 7` y el tono del público: ni un resplandor, ni un
   bisel, ni un degradado de color, ni una sombra de colores. El movimiento es
   el lujo; el acabado es plano.

**Y la contrapartida obligatoria:** con el movimiento a 8, cada sección tiene que
tener su fotograma quieto para `prefers-reduced-motion`. No es un extra de
accesibilidad, es que parte de este público navega así.

---

## Parte 2 · Cuántos módulos informativos aguanta la página

Inventario de todo lo que se puede llegar a contar de Ondine, con el juicio de
si entra o no. La columna que importa es la última.

### Entran, y son el esqueleto

| # | Módulo | Qué dato aporta | Forma |
|---|---|---|---|
| 1 | **Hero: la biblioteca resolviéndose** | La promesa entera en tres segundos | Mural de carátulas construido en HTML que pasa de crudo a resuelto, con paralaje |
| 2 | **El problema** | Por qué el servidor se rinde: nombres escritos a mano | Tira de nombres de fichero reales frente a fichas sin datos |
| 3 | **Partir** | Lo diferencial: un fichero trae dos capítulos | Objeto centrado que se parte, anclado al scroll |
| 4 | **Ordenar** | El catálogo no lo tecleas tú; 246 correctos, 0 conflictos | Tabla que se rellena, con contadores |
| 5 | **El tamaño** | 1 GB a 133 MB en 0,6 s sin tocar el vídeo, y el 80-90 % si recomprimes | Cifra enorme arriba, comparación abajo |
| 6 | **Antes / después** | El argumento cerrado, a sangre | Mural partido, recorrido con el scroll |
| 7 | **Descarga** | Windows, y las tres salidas de terminal | Llamada grande y comandos en mono |
| 8 | **Pie** | Repositorio, versiones, cambios, hoja de ruta, licencia | Plano |

### Se pueden añadir si hacen falta, por orden de valor

| # | Módulo | Qué dato aporta | Veredicto |
|---|---|---|---|
| 9 | **El spot de 44 s** | Es el mejor material que existe y ya está producido | **Vale la pena.** Un módulo de vídeo con la portada de la miniatura. Cuesta poco y convence mucho |
| 10 | **Las tres promesas** | No toca los originales, todo local, código abierto | **Vale la pena**, pero dentro de «Descarga», no como sección |
| 11 | **Compatibilidad** | Plex, Jellyfin, Kodi, y los formatos que traga | Solo si cabe como tira estrecha. Nunca con sus logotipos |
| 12 | **Requisitos** | Windows 10+, aceleración por hardware | A un desplegable en «Descarga». No merece aire propio |
| 13 | **Cómo funciona por dentro** | Cotejo contra catálogo, señales, umbrales | **Fuera.** Es documentación, y este público va al repositorio a leerla |
| 14 | **Comparativa con la competencia** | Frente a FileBot, tinyMediaManager | **Fuera.** Nombrar competidores en tu propia landing les da tráfico |
| 15 | **Testimonios** | No hay usuarios que citar todavía | **Fuera.** Inventarlos es exactamente el tipo de dato falso que se prohíbe |
| 16 | **Estrellas de GitHub** | Prueba social real | **Fuera de momento.** Un número bajo resta; cuando suba, se pone |

**Conclusión: ocho secciones de esqueleto, más el módulo de vídeo (9) que se
gana su sitio, y dos añadidos (10 y 12) que van dentro de otras.** Nueve bloques
visibles. Del 13 al 16 no entran y conviene dejar escrito por qué, para no
volver a discutirlo.
