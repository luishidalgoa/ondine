---
format: 1920x1080
duration: 41.29s
message: "Lo que pasas antes de que Plex o Jellyfin escaneen tu biblioteca"
arc: Hook → Problema → Se lo pides a la IA → Queda ordenado → Adelgaza → Parte → En tu servidor
audience: "self-hosting y homelab: gente que se monta su propio servidor de medios en casa"
mode: autonomous
# `music: none` a propósito: la pista NO se busca en la biblioteca de HeyGen (no hay
# sesión). Es un fichero local, assets/musica.m4a, ya recortado al compás y con fundidos.
music: none
tempo: 93 BPM · compás 4/4 de 2,5806 s · 16 compases
---

> **Los planos 2 a 7 ya no se autoritan desde aquí.** La maqueta la firma Claude Design
> (`disenio/planos.html`, cinco fotogramas por plano a 1920×1080) y el movimiento lo manda
> [`GUIA-ANIMACION.md`](GUIA-ANIMACION.md): curvas nombradas, ocho presets de texto y un
> cuadro de tiempos por plano. Este documento se queda como intención narrativa y como
> referencia del plano 1, que es anterior y sigue vigente. Si algo aquí contradice a la
> guía, manda la guía.

## Frame 1 — La marca se ensambla

- scene: Una nube de partículas se posa y forma los tres trazos de Ondine
- duration: 5.161s
- poster: 2.6s
- transition_in: cut
- status: animated
- src: compositions/frames/01-hook.html
- asset_candidates: assets/ondine-marca.svg

Arranque en frío, sin logo previo ni cortinilla. Sobre el fondo `cream` (#161826), unas 900
partículas de acento entran desde posiciones aleatorias y se posan sobre el trazo del SVG de la
marca hasta formar las tres líneas: onda, media onda, recta.

**Por qué esto y no un logo que aparece**: la marca *significa* «lo que entra revuelto sale
ordenado». Ensamblarla desde el caos es el argumento del producto dicho sin una sola palabra, y
en cuatro segundos. Un fundido de entrada normal desperdiciaría el único plano en el que el
espectador aún no sabe de qué va esto.

Al asentarse, el nombre **Ondine** aparece a la derecha de la marca (el bloque del manual de
marca: separación de media altura, alineado al eje óptico de la equis). Sale sin fanfarria.

## Frame 2 — El desorden

- scene: Nombres de fichero puestos a mano y mal, cayendo; los que no encajan se marcan
- duration: 5.161s
- poster: 3.5s
- transition_in: cut
- status: animated
- src: compositions/frames/02-desorden.html

Una columna de nombres de fichero reales de una biblioteca casera, en mono y en gris apagado,
entrando escalonados:

```
Cap 12 (2).mkv
episodio final BUENO.mkv
Temporada2_04-05.mkv
Sin titulo (copia).mkv
aqui no hay quien viva 3x07.avi
```

Al fondo, **muy desenfocada**, una parrilla de fichas con huecos y placas que dicen «Desconocido».
Es lo que ve el servidor.

Sobre tres de los nombres cae una **✕** que se enciende brevemente. No es un error de la app: es
lo que el servidor no sabe leer.

**Ojo (regla dura del brief)**: nada de nombres de scene-release. El desorden de una casa son
nombres escritos a mano, no releases. Además el cierre lleva Jellyfin y sus normas de marca
prohíben asociarla a la piratería.

**Secuencia (5,161 s)** — los segundos son acentos de la música, no números redondos.

- **Escena 1 · el fondo del problema (0 – 1,79)** — *Layout*: la parrilla del servidor al fondo,
  muy desenfocada (`blur(14px)`, opacidad 0,35), con placas grises que dicen «Desconocido».
  Columna de nombres en mono, alineada a la izquierda, ocupando el tercio central.
  *Motion*: `waterfall-entry`. Cae un nombre en **0,18**, **0,83**, **1,19** y **1,79**; cada uno
  entra con 30 px de desplazamiento vertical y un desenfoque que resuelve en 0,22 s.
- **Escena 2 · lo que el servidor no sabe leer (1,79 – 4,53)** — el quinto nombre cae en **2,59**.
  Sobre él se enciende la primera **✕** roja (`#C64545`, trazo, no relleno) con `spring-pop-entrance`
  y un halo corto. Segunda ✕ en **3,57**. La tercera en **4,05**, que es **el acento más fuerte de
  todo el spot**: ahí la ✕ entra un 15 % más grande y el bloque entero acusa el golpe con 4 px de
  desplazamiento que vuelve.
- **Escena 3 · se queda así (4,53 – 5,161)** — las tres ✕ mantienen. La parrilla del fondo pierde
  otro 10 % de opacidad: el servidor se rinde. Corte seco.

*handoff_out*: la parrilla desenfocada queda a opacidad 0,25 y `blur(16px)` — el Frame 7 la
recupera exactamente ahí para enfocarla.

## Frame 3 — Se lo pides a la IA

- scene: El cursor copia un anexo de la Wikipedia y la app escribe el catálogo sola
- duration: 6.452s
- poster: 4.5s
- transition_in: crossfade
- status: animated
- src: compositions/frames/03-catalogo.html

El plano de «magia», y el que más gente convierte. Un cursor cruza a una ventana sobria de
navegador con una tabla de episodios (título de sección: *Anexo: Episodios*), **selecciona la
URL** y la arrastra al panel de Ondine.

En el panel, el botón **«Generar con IA…»** se pulsa y el catálogo `reindex/1.0` **se escribe
solo**, línea a línea, en mono sobre la superficie `navy`:

```json
{ "num": 12, "titulos": { "es": ["Érase un ascensor"] } }
```

Que se vea que los títulos empiezan por «Érase…» — es la serie del ejemplo y ancla que esto son
datos de verdad, no relleno.

**Secuencia (6,452 s)** — el plano de «magia», el que más convierte.

- **Escena 1 · la fuente (0 – 2,11)** — *Layout*: ventana de navegador sobria (barra de título
  con tres puntos, sin marca reconocible) en perspectiva ligera (`rotateY(-8deg)`, `perspective:
  1400px`), ocupando 62 % del ancho. Dentro, una tabla de episodios con títulos que empiezan por
  «Érase…». *Motion*: el cursor entra por la derecha y **llega en 0,50**; sube a la barra de
  direcciones en **1,13**; la URL se resalta en **1,52**; en **2,11** destella el copiado.
- **Escena 2 · el encargo (2,11 – 3,40)** — la ventana sale deslizándose a la derecha mientras
  entra el panel de Ondine desde la izquierda (`crossfade` de posición, no de opacidad). En
  **2,76** —«uno» de compás— el botón **«Generar con IA…»** se pulsa: `press-release-spring`.
- **Escena 3 · se escribe solo (3,40 – 6,452)** — el catálogo `reindex/1.0` aparece línea a línea
  en mono sobre la superficie `navy`, **una línea por acento**: 3,40 · 4,05 · 4,52, y después al
  pulso de 0,645 s. Un contador de episodios sube en paralelo. La última línea queda con el cursor
  parpadeando una sola vez.

Que se lea `{ "num": 12, "titulos": { "es": ["Érase un ascensor"] } }`: son datos de verdad.

## Frame 4 — Queda ordenado

- scene: La app entra y las filas se van poniendo verdes
- duration: 5.161s
- poster: 3.8s
- transition_in: cut
- status: animated
- src: compositions/frames/04-organizar.html
- asset_candidates: assets/organizar.png

La ventana de Organizar entra **desde arriba**, en perspectiva ligera, y se asienta. Dentro, las
filas pasan de gris a la píldora verde «Correcto» en cascada rápida, de arriba abajo.

Los contadores suben hasta **246 correctos · 0 conflictos** — cifra real de la captura, no
inventada. El nombre propuesto se lee al lado del original: se ve el antes y el después en la
misma fila.

**Secuencia (5,161 s)** — **este beat cae entero dentro del bajón de la música: no tiene ni un
solo acento.** Es la única parte de la pieza donde el silencio hace el trabajo, así que **nada de
percusión visual** — ni golpes, ni rebotes, ni destellos.

- **Escena 1 · llega la herramienta (0 – 1,29)** — *Layout*: la captura real
  `assets/organizar.png` como ventana, entrando **desde arriba** con perspectiva
  (`rotateX(9deg)` → `0`) y asentándose. *Motion*: `power3.out`, **sin rebote**. Dos pulsos de
  música (1,29 s) para todo el movimiento: entra despacio.
- **Escena 2 · se pone en orden (1,29 – 4,0)** — las filas pasan de gris a la píldora verde
  «Correcto» en **cascada continua a 0,32 s por fila** (media negra). Es un goteo, no una ola.
  Los contadores suben con `counting-dynamic-scale` hasta **246 correctos · 0 conflictos**.
- **Escena 3 · queda quieto (4,0 – 5,161)** — todo estático salvo una respiración imperceptible
  de la ventana (escala 1 → 1,004). El plano descansa antes de que la música vuelva.

## Frame 5 — Adelgaza sin tocarse

- scene: Una barra de tamaño se desinfla; el vídeo no parpadea
- duration: 3.871s
- poster: 2.8s
- transition_in: crossfade
- status: animated
- src: compositions/frames/05-adelgaza.html
- asset_candidates: assets/comprimir.png

Una barra de tamaño se desinfla de **155 MB a 134 MB** mientras un contador marca **0,6 s** y
entra un sello: **«vídeo idéntico»**.

**La imagen del vídeo no parpadea ni un fotograma.** Esa quietud *es* el mensaje: quitar doblajes
y subtítulos que no usas no recomprime nada. Si el plano tiembla, el argumento se cae.

**Secuencia (3,871 s)** — el beat más corto, y el que se sostiene sobre una quietud.

- **Escena 1 · el peso (0 – 1,29)** — *Layout*: un fichero con su fotograma a la izquierda y, a
  la derecha, la barra de tamaño llena marcando **155 MB**. Debajo, las pistas: `spa`, `eng`,
  `eng (subs)`. *Motion*: entra con un `crossfade` desde el beat anterior, sin gesto propio.
- **Escena 2 · adelgaza (1,29 – 3,40)** — dos pistas se tachan y **la barra se desinfla de 155 a
  134 MB** con `stat-bars-and-fills`, mientras un contador va de 0,0 a **0,6 s**.
  **REGLA DURA: el fotograma del vídeo no cambia ni un píxel durante todo el plano.** Esa quietud
  *es* el argumento — no se recomprime nada. Si la imagen tiembla, el beat miente.
- **Escena 3 · la prueba (3,40 – 3,871)** — en el **único acento del beat** entra el sello
  **«vídeo idéntico»** con `spring-pop-entrance`, y a los 0,47 s corta.

## Frame 6 — El corte

- scene: Un fichero con dos capítulos pegados se parte en dos
- duration: 5.161s
- poster: 2.8s
- transition_in: cut
- status: animated
- src: compositions/frames/06-corte.html
- asset_candidates: assets/recortes.png

El diferencial, y el plano que ninguna otra herramienta puede enseñar. Un bloque etiquetado
`Temporada2_04-05.mkv` **se parte por la mitad** con un corte limpio, y las dos piezas se separan
y reciben su nombre:

```
Aqui no hay quien viva - S02E04 - Érase una okupa.mkv
Aqui no hay quien viva - S02E05 - Érase un ascensor.mkv
```

Que respire. Es el único beat que vende algo que no está saturado en el nicho.

**Secuencia (5,161 s)** — el diferencial. **La música vuelve a entrar en el fotograma 1 de este
beat**: el golpe y el corte del fichero son el mismo instante.

- **Escena 1 · el tajo (0 – 0,50)** — *Layout*: un bloque ancho etiquetado `Temporada2_04-05.mkv`
  centrado, con su línea de tiempo y miniaturas. *Motion*: en **0,01** —el drop— el bloque **se
  parte**: un destello del filo de acento recorre la junta en 0,12 s y las dos mitades quedan
  separadas 6 px. Nada más se mueve: el corte es el único suceso.
- **Escena 2 · dos piezas (0,50 – 2,60)** — las mitades se separan hasta 90 px en **0,50** y
  terminan de acomodarse en **1,14**. En **1,79** aparecen sus tiempos (`0:00–34:12` /
  `34:12–68:40`).
- **Escena 3 · cada una con su nombre (2,60 – 5,161)** — en **◆2,60** aterriza el nombre de la
  primera y en **3,08** el de la segunda:
  `Aqui no hay quien viva - S02E04 - Érase una okupa.mkv` ·
  `... - S02E05 - Érase un ascensor.mkv`.
  Las píldoras **E04** y **E05** entran en **3,73** y **4,37**. Hold.

Que respire: es el único plano que vende algo que no tiene nadie más en el nicho.

## Frame 7 — En tu servidor

- scene: La parrilla enfoca y se rellena; marca y frase
- duration: 10.322s
- poster: 3s
- transition_in: crossfade
- status: animated
- src: compositions/frames/07-cierre.html

La parrilla desenfocada del Frame 2 vuelve, ahora **enfocando**: las placas «Desconocido» se
rellenan con carátulas planas y sus títulos. Es el pago de la promesa del principio.

Debajo, discreto: **«funciona con Plex, Jellyfin y Kodi»** — compatibilidad, nunca respaldo. Sin
su logotipo.

Cierra la marca de Ondine con la frase:

> **Lo que pasas antes de que Plex escanee.**

El último fotograma debe casar con el primero: la pieza va en bucle en el hero.

**Secuencia (5,161 s)** — el pago de la promesa del Frame 2.

- **Escena 1 · enfoca (0 – 1,15)** — *Layout*: la misma parrilla del Frame 2, recuperada en su
  estado exacto (opacidad 0,25, `blur(16px)`). *Motion*: en **◆0,02** empieza a enfocar
  (`blur(16px)` → `0`, opacidad → 1) en 0,9 s, y las placas «Desconocido» se rellenan con
  carátulas planas de la paleta y sus títulos, en cascada a **0,32 s** desde el centro hacia
  fuera (`center-outward-expansion`).
- **Escena 2 · la firma (1,15 – 2,60)** — en **1,15**, el acento más fuerte del cierre, la
  parrilla se atenúa al 30 % y entra la **marca de Ondine** con el nombre — el mismo bloque del
  Frame 1, sin volver a ensamblarlo: aquí ya se da por conocido.
- **Escena 3 · la frase (2,60 – 5,161)** — en **◆2,60** entra **«Lo que pasas antes de que Plex
  escanee.»** con cortinilla izquierda→derecha. En **3,09**, más pequeño y atenuado:
  «funciona con Plex, Jellyfin y Kodi». Hold hasta el final.

**Compatibilidad, nunca respaldo**: los nombres van como texto, sin sus logotipos, y sin insinuar
que aprueban nada.

*handoff_in*: la parrilla llega del Frame 2 en opacidad 0,25 y `blur(16px)`, misma posición y
escala. El último fotograma debe poder encadenar con el primero del Frame 1: la pieza va en bucle.

---

## Video direction

**La música manda el montaje, no al revés.** La pista elegida (`assets/musica.m4a`, de Audioknap
vía Pixabay — licencia de uso comercial sin atribución) se midió con análisis de ataques, no de
oído ni fiándose de la etiqueta:

- **93,0 BPM** · compás 4/4 de **2,5806 s** · primer pulso de la pista original en 0,476 s.
- El spot dura **14 compases exactos = 36,13 s**, y cada corte cae en un compás o en su mitad.
- La pista se recorta desde su **compás 12** (segundo 28,863 del original) con entrada de 0,35 s
  y salida de 1,2 s.

**Por qué desde el compás 12 y no desde el principio.** La pista tiene arreglo completo hasta su
compás 17, un **bajón** entre el 18 y el 21, y vuelve a entrar en el 22. Entrando en el 12, esa
estructura cae encima del guion sin forzar nada:

| Segundo del spot | Qué hace la música | Qué hay en pantalla |
|---|---|---|
| 0 – 15,5 | arreglo completo | Hook · El desorden · Se lo pides a la IA |
| **15,5 – 25,8** | **bajón** | Queda ordenado · Adelgaza sin tocarse |
| **25,8** | **vuelve a entrar** | **primer fotograma de «El corte»** |
| 25,8 – 36,1 | arreglo completo | El corte · En tu servidor |

Los dos beats contemplativos —la biblioteca poniéndose en orden y el fichero adelgazando sin
tocarse— caen en el silencio relativo, y **la música vuelve justo en el plano que más pesa**, el
del corte. No estaba planeado: salió de medir la pista y encajó.

**Consecuencia para quien retoque esto**: mover la duración de un beat desplaza todos los cortes
posteriores y rompe la sincronía. Si hay que cambiar uno, hay que compensar en otro para que la
suma siga siendo 14 compases.

**El hero de la landing va MUDO** (los navegadores no reproducen sonido sin permiso). La música
es para el corte de redes. De la misma línea de tiempo salen las dos versiones.

### Mapa de acentos — dónde tiene que caer el movimiento

Medido sobre el recorte final (flujo espectral, 40 acentos por encima del umbral). **El movimiento
grande va sobre un acento, nunca entre dos**: es lo que separa un montaje que va *con* la música de
uno que va al lado.

`◆` = el acento cae en el «uno» de un compás.

| Beat | Acentos (segundo dentro del beat) | Qué poner encima |
|---|---|---|
| **1** La marca se ensambla | ◆2,44 · 3,09 · 3,56 · 4,04 · 4,53 | **Ya sincronizado**: la marca se aparta en 2,44 y la cortinilla del nombre va de 3,09 a 3,56 |
| **2** El desorden | ◆0,18 · 0,83 · 1,19 · 1,79 · ◆2,59 · 3,05 · 3,57 · **4,05** · 4,53 · ◆5,02 | Un nombre de fichero cae en cada uno. Las ✕ rojas en 2,59 y en **4,05, que es el acento más fuerte de todo el spot** |
| **3** Se lo pides a la IA | 0,50 · 1,13 · 1,52 · 2,11 · ◆2,76 · 3,40 · 4,05 · 4,52 | El cursor llega en 0,50, copia en 2,11, «Generar con IA» en ◆2,76, y el catálogo se escribe línea a línea sobre los cuatro últimos |
| **4** Queda ordenado | **ninguno** | Cae entero dentro del bajón. **No metas percusión visual aquí**: cascada lenta y continua de filas poniéndose verdes. El silencio es el plano |
| **5** Adelgaza sin tocarse | 3,40 | Un solo golpe, a 0,47 s del corte. La barra termina de desinflarse y el sello **«vídeo idéntico»** entra justo ahí |
| **6** El corte | **◆0,01** · 0,50 · 1,14 · 1,79 · ◆2,60 · 3,08 · 3,73 · 4,37 | **La música vuelve en el fotograma 1**: el fichero se parte exactamente ahí. Las dos mitades se separan sobre 0,50 y 1,14; sus nombres aterrizan en ◆2,60 y 3,08 |
| **7** En tu servidor | ◆0,02 · 0,50 · **1,15** · 1,78 · ◆2,60 · 3,09 · 3,56 · 4,05 | La parrilla empieza a enfocar en ◆0,02 y las plazas se rellenan en cascada; la marca de Ondine entra en **1,15**, el acento más fuerte del cierre |

### Plano 7 extendido — acentos medidos del tramo nuevo

El plano pasa de 2 a **4 compases (10,322 s)**. Los 5,16 s extra salen de coger dos compases más
de la pista, **sin estirar ni ralentizar nada**: mismo tempo, mismo timbre. Y como se cogen por el
final, **todo lo anterior conserva su sincronía** — la música sigue entrando por el compás 12, el
bajón sigue en el 15,5 y el regreso sigue cayendo en el primer fotograma de «El corte».

Acentos reales dentro del plano 7 (`◆` = «uno» de compás):

`◆0,02 · 0,50 · 1,15 · 1,78 · ◆2,60 · 3,09 · 3,56 · 4,05 · 4,51 · ◆5,34 · 6,31 · 6,95 · ◆7,75 · 8,24 · 8,73 · 9,20`

**Dos correcciones sobre la rejilla que proponía la guía de diseño**, medidas contra la pista:

| Guía | Real | Qué hacer |
|---|---|---|
| 5,67 | **no existe** | Arrancar «Optimiza tus bibliotecas…» en **5,16** en vez de 5,20: con stagger de 60 ms su cuarta palabra cae en 5,40, que sí coincide con el acento real de **5,34**. |
| 9,60 | **9,20** | Bajar la fila de compatibilidad a **9,20**. Además deja 1,1 s de quietud antes del negro en vez de 0,7, que le viene bien al cierre. |

Los dos momentos que la guía marca como principales —la marca en **7,74** y la frase de cierre en
**8,80**— caen clavados sobre acentos reales (7,75 y 8,73). Ahí no se toca nada.

**El pulso base son 0,645 s** (negra a 93 BPM), así que cualquier cascada de elementos debería ir a
ese paso, a su mitad (0,32 s) o a su doble (1,29 s) — nunca a un número redondo tipo 0,5 s, que
suena a que va por libre.
