---
workflow: product-launch-video
flow: automation
storyboard: no
message: "Lo que pasas antes de que Plex o Jellyfin escaneen tu biblioteca"
destination: embed
aspect: 1920x1080
language: es
audience: "gente que se monta su propio servidor de medios en casa — self-hosting y homelab; técnicos o semitécnicos, alérgicos al software cerrado"
length: 30s
angle: "el paso que falta antes del servidor"
---

## Intent

Spot de lanzamiento de **Ondine**, una app de escritorio para Windows (con CLI multiplataforma)
que prepara una biblioteca de series y películas **antes** de que Plex o Jellyfin la escaneen.
Esos servidores enseñan una biblioteca preciosa, pero solo si los ficheros ya están bien
nombrados y colocados; cuando no lo están, se rinden — el episodio sale como «Desconocido».
Ondine es el paso intermedio: comprime para que quepa, ordena contra un catálogo para que el
servidor lo reconozca, y parte los ficheros que traen varios capítulos pegados.

Tono: sobrio y de herramienta de precisión, no de app de consumo. El público premia la sobriedad
y detesta lo pomposo. Nada de brillos ni de música épica.

La pieza va en el **hero de la landing, en bucle y muda**. De ella se derivará después un corte
más corto con música para redes.

## Assets

- ../../docs/marca/ondine-marca-oscuro.svg — la marca: tres trazos que van de onda a recta. El hook la ensambla desde partículas.
- ../../docs/marca/ondine-marca-16px.svg — versión de píxel entero, por si hace falta a tamaño diminuto.
- ../../docs/img/organizar.png — captura real de Organizar: 246 ficheros, 246 correctos, 0 conflictos. Base del beat 4.
- ../../docs/img/comprimir.png — captura real de Comprimir con una temporada analizada. Base del beat 5.
- ../../docs/img/recortes.png — captura real de Recortes con un fichero partido en dos tramos. Base del beat 6.

## Customizations

- **Hook con partículas**: la marca se ensambla desde una nube de puntos que se posan sobre el
  trazo del SVG, se sostiene, y arranca el spot. Es la tesis del producto contada sin palabras —
  caos que se ordena— así que no es un adorno: es el argumento.
- **Sin voz en off y sin música.** El hero se reproduce mudo.
- Cifras reales, medidas, nunca inventadas: 155 MB → 134 MB en 0,6 s con el vídeo idéntico bit a
  bit; 246 ficheros con 0 conflictos.

## Notes

- **Serie de ejemplo: «Aquí no hay quien viva»** (decisión del usuario). Sus capítulos empiezan
  todos por «Érase...», lo que luce muy bien en el beat del catálogo.
- **Prohibido el estilo scene-release** en los nombres de fichero (nada de `AMZN.WEB-DL.x265-GRUPO`).
  Las normas de marca de Jellyfin prohíben asociar su nombre a la piratería, y el cierre lleva
  Jellyfin. El desorden que mostramos es el de nombres puestos a mano y mal: `Cap 12 (2).mkv`,
  `episodio final BUENO.mkv`, `Temporada2_04-05.mkv`, `Sin titulo (copia).mkv`.
- Jellyfin aparece como compatibilidad, nunca insinuando respaldo, y su logo no se usa como parte
  del nuestro.
- El beat del corte **no** parte un capítulo en tres historias (esa serie no las tiene): parte un
  fichero que trae **dos capítulos pegados** en E04 y E05. Es el caso más común de verdad.
- Paleta exacta de la app, no aproximada: fondo `#161826`, superficie `#232532`, campos `#292B31`,
  texto `#E9E9ED`, acento `#968AE0`, claros `#B5ABFC` `#D2CEFD` `#E7E5FE`, oscuros `#796CBF`
  `#5D5294`. Tipografía Inter.
- Empezar construyendo y renderizando **solo el beat 1** para validar el look antes de seguir.
