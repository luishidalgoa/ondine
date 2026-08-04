# Ondine · guía de animación del spot

Referencia visual: `disenio/planos.html` (tres a cinco fotogramas por plano, 1920×1080).
Música: **93 BPM**, negra = **0,645 s**, compás de 2/4 = **1,29 s**, compás doble (2,58 s) = unidad de duración de plano.
Salida: 1920×1080, **60 fps**. Todo el movimiento es de opacidad, `transform` y `filter: blur()` — nada de animar `width`, `top` ni `left`.

---

## 0 · Reglas globales

| Regla | Detalle |
|---|---|
| Ningún easing por defecto | Prohibido `ease`, `linear` salvo donde se pide explícitamente. Curvas nombradas abajo. |
| Un solo suceso por golpe | Si dos cosas caen en el mismo golpe, una es principal y la otra la acompaña a ≤40 % de su amplitud. |
| Duración máxima de entrada de texto | 420 ms. Más largo se percibe como “pesado”. |
| Amplitud máxima de desplazamiento | 28 px sobre 1080 de alto. Este spot es sobrio: el movimiento es corto y decidido. |
| Sin rebote visible | Los muelles no pasan de 1,02 de escala. Nada de overshoot elástico. |
| Salidas siempre más rápidas que entradas | Salida ≈ 0,7 × la duración de la entrada correspondiente. |
| Quietud final | Cada plano acaba con ≥0,4 s sin ningún píxel en movimiento antes del corte. |

### Curvas

```
EASE_OUT      cubic-bezier(0.16, 1, 0.3, 1)      entradas
EASE_IN       cubic-bezier(0.7, 0, 0.84, 0)      salidas
EASE_IN_OUT   cubic-bezier(0.65, 0, 0.35, 1)     relevos y cámara
SNAP          cubic-bezier(0.34, 1.2, 0.64, 1)   golpes secos (✕, píldoras, sello)
LINEAR        linear                              SOLO: contadores del plano 4, scroll del plano 7, cronómetro del plano 5
SPRING        stiffness 210 · damping 22 · mass 1  (≈380 ms de asentamiento)
```

---

## 1 · Catálogo de animaciones de texto

Los ocho presets. Cada uno se define una vez aquí y luego se **referencia por nombre** en los beats.
`ENTRADA` = el elemento no existía. `SALIDA` = el elemento desaparece. `RELEVO` = un texto sustituye a otro en el mismo sitio.

### `per-word-crossfade` — ENTRADA
Palabra a palabra, sin desplazamiento. Para frases largas que deben leerse mientras entran.

```
por palabra:  opacity 0 → 1
duración:     260 ms · EASE_OUT
stagger:      60 ms entre palabras (orden de lectura)
transform:    ninguno
total frase de N palabras ≈ 260 + 60·(N−1) ms
```

Nota: si la frase pasa de 8 palabras, sube el stagger a 45 ms para no exceder 700 ms de total.

### `spring-scale-in` — ENTRADA
Para marcas, sellos, píldoras y cifras: el elemento “aterriza”.

```
scale     0,94 → 1        (SPRING, tope 1,02)
opacity   0 → 1           260 ms EASE_OUT (empieza junto a la escala)
origen    center center
```

### `shared-axis-y` — RELEVO
Dos textos que ocupan la misma posición se turnan por el eje vertical.

```
saliente:  y 0 → −24 px · opacity 1 → 0 · blur 0 → 4 px · 200 ms EASE_IN
entrante:  y +24 px → 0 · opacity 0 → 1 · 280 ms EASE_OUT
solape:    el entrante arranca 80 ms antes de que acabe el saliente
```

### `blur-out-up` — SALIDA
La salida “limpia” por defecto de este spot.

```
y       0 → −28 px
blur    0 → 10 px
opacity 1 → 0
320 ms · EASE_IN
```

### `kinetic-center-build` — ENTRADA
Reservado para los dos momentos de mayor peso tipográfico (marca + frase de cierre). El texto se compacta hacia su forma final.

```
por palabra, desde el centro hacia fuera (centro primero, pares a izq/der):
  opacity        0 → 1
  scale          1,05 → 1
  letter-spacing +0,05em → valor final
  duración       320 ms · EASE_OUT
  stagger        70 ms por anillo (no por palabra)
el bloque completo: sin desplazamiento vertical
```

### `short-slide-right` — ENTRADA
Datos y etiquetas que entran “desde el origen” (izquierda). Rótulos, líneas de log, filas.

```
x       −18 px → 0
opacity 0 → 1
220 ms · EASE_OUT
```

### `short-slide-down` — ENTRADA
Datos que caen: nombres de fichero, líneas de catálogo, filas de tabla.

```
y       −16 px → 0
opacity 0 → 1
220 ms · EASE_OUT
```

### `depth-parallax-words` — ENTRADA
Da profundidad a una frase de cierre sin moverla: cada palabra viene de una “capa” distinta.

```
palabra i (i = 0…N−1):
  scale    1 + 0,06·(1 − i/(N−1))  → 1     capas delanteras entran más grandes
  blur     3 px → 0
  opacity  0 → 1
  duración 340 ms · EASE_OUT
  stagger  90 ms
```

---

## 2 · Plano 2 · El desorden — 5,16 s (2 compases)

Golpes: `0,18 · 0,83 · 1,19 · 1,79 · 2,59 · 3,05 · 3,57 · 4,05 · 4,53 · 5,02`

**Componentes:** `parrilla-fondo` · `rotulo-kicker` · `nombre[0..4]` · `equis[0..2]` · `aro-golpe`

| t | Componente | Animación | Detalle |
|---|---|---|---|
| −0,30 | `parrilla-fondo` | fade | opacity 0 → 0,55 en 300 ms EASE_OUT. Ya está puesta cuando entra el primer golpe. |
| 0,00 | `rotulo-kicker` | `short-slide-right` | El asterisco de acento entra 40 ms antes que el texto. |
| 0,18 | `nombre[0]` | `short-slide-down` | + blur 6 → 0 px en los mismos 220 ms. |
| 0,83 | `nombre[1]` | `short-slide-down` | idéntico |
| 1,19 | `nombre[2]` | `short-slide-down` | `Temporada2_04-05.mkv` — sin tratamiento especial todavía |
| 1,79 | `nombre[3]` | `short-slide-down` | idéntico |
| 2,59 | `nombre[4]` | `short-slide-down` | idéntico |
| 2,59–3,05 | — | **quietud** | 0,46 s sin movimiento. Es el hueco que hace que las ✕ suenen. |
| 3,05 | `equis[0]` sobre `nombre[0]` | `spring-scale-in` acortado | scale 1,4 → 1 en **90 ms** SNAP, sin fade. En paralelo el nombre baja a `#6C7080` (color, 180 ms EASE_OUT). |
| 3,57 | `equis[1]` sobre `nombre[4]` | igual que 3,05 | |
| **4,05** | `equis[2]` sobre `nombre[2]` | igual, **amplificado** | scale 1,6 → 1 en 90 ms SNAP; glifo un 20 % mayor que los otros dos. |
| 4,05 | `aro-golpe` | expansión | scale 0,6 → 1,15 · opacity 0,45 → 0 · 500 ms EASE_OUT. Centrado en `equis[2]`. |
| 4,05 | `parrilla-fondo` | acompañamiento | scale 1 → 1,012 en 400 ms EASE_OUT (imperceptible, sostiene el golpe). |
| 4,53 · 5,02 | — | **nada** | Los dos últimos golpes quedan vacíos a propósito: el plano ya dijo lo que tenía que decir. |

---

## 3 · Plano 3 · Se lo pides a la IA — 6,45 s (2,5 compases)

Golpes: `0,50 · 1,13 · 1,52 · 2,11 · 2,76 · 3,40 · 4,05 · 4,52`

**Componentes:** `pagina-clara` · `panel-app` · `cursor` · `campo-url` · `boton-ia` · `halo-boton` · `codigo-linea[0..3]` · `contador-24` · `rotulo-kicker`

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 0,00 | `pagina-clara` | ya presente | opacity 1, sin desenfoque. `panel-app` a opacity 0,28 al fondo. |
| 0,00 | `rotulo-kicker` | `short-slide-right` | a 55 % de opacidad final (sube a 1 en 2,76) |
| 0,50 | `cursor` | desplazamiento | de (1180, 880) a (760, 620) en 420 ms EASE_IN_OUT. |
| 1,13 | `cursor` | clic | scale 1 → 0,88 → 1 en 120 ms SNAP. |
| 1,52 | selección de la tabla | barrido | banda de selección crece de 0 a 100 % de ancho, 240 ms EASE_OUT, color `rgba(150,138,224,0.22)`. |
| 2,11 | `pagina-clara` | inicio de salida | x 0 → −276 px · blur 0 → 8 px · opacity 1 → 0,4 · 490 ms EASE_IN (acaba en 2,60). |
| 2,11 | `panel-app` | subida | opacity 0,28 → 1 · x +90 → 0 px · 490 ms EASE_OUT (relevo cruzado con la página). |
| 2,11 | `campo-url` | `short-slide-right` | el texto de la URL aparece dentro del campo; borde `#33364A` → `#968AE0` en 200 ms. |
| **2,76** | `boton-ia` | pulsación | scale 1 → 0,97 → 1 en 120 ms SNAP; relleno pasa a `#968AE0` en 90 ms. |
| 2,76 | `halo-boton` | destello | box-shadow spread 0 → 6 px, opacity 0,18 → 0 en 300 ms EASE_OUT. |
| 2,76 | `pagina-clara` | remate de salida | continúa a x −1200 px · opacity 0 · blur 14 px · 400 ms EASE_IN. |
| 3,00 | `panel-app` | recolocación | x → 136 px (borde izquierdo del lienzo), 520 ms EASE_IN_OUT. Único movimiento de “cámara” del spot. |
| 3,40 | `codigo-linea[0]` | `short-slide-down` + máquina de escribir | el bloque cae y el texto se revela por caracteres a 22 ms/carácter; cursor `▌` de acento parpadeando a 2 Hz. |
| 4,05 | `codigo-linea[1]` | igual | el cursor salta a esta línea |
| 4,52 | `codigo-linea[2]` | igual | |
| 5,00 | `codigo-linea[3]` | igual | fuera de golpe a propósito: cierra el bloque sin remarcarlo |
| 5,30 | `contador-24` | `spring-scale-in` | la cifra cuenta 0 → 24 en 600 ms LINEAR mientras la caja hace el muelle. |
| 5,90–6,45 | — | **quietud** | |

---

## 4 · Plano 4 · Queda ordenado — 5,16 s · **sin golpes**

Bajón musical. **Prohibido**: SNAP, muelles, escalas y destellos. Una sola curva LINEAR gobierna el plano.

**Componentes:** `rotulo-kicker` · `grupo[0..1]` · `fila[0..7]` · `pildora[0..7]` · `contador-correctos` · `contador-conflictos`

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 0,00 | `rotulo-kicker`, `grupo[*]`, `fila[*]` | ya presentes | El plano abre con la propuesta ya escrita. Nada entra. |
| 0,00 → 1,52 | `pildora[0..7]` | fade en cascada | opacity 0 → 1 · y +6 px → 0 · **190 ms LINEAR** por píldora, stagger 190 ms. Sin escala. |
| 0,00 → 4,30 | `contador-correctos` | conteo | 0 → 246 en **4,30 s LINEAR**, sin aceleración final. Color `#4A4F63` → `#8A8FA3` → `#5DB872` interpolado a lo largo del conteo. |
| — | `contador-conflictos` | estático | El `0` no se anima nunca. Es el dato tranquilizador: si se moviera, pediría atención. |
| 4,30 → 5,16 | todo | **quietud total** | 0,86 s. Es el único plano que acaba en silencio completo; no lo rompas con un fade. |

> Si el montaje pide algo más en este plano, la respuesta es no. El plano existe para no tener nada.

---

## 5 · Plano 5 · Adelgaza sin tocarse — 3,87 s · un golpe en 3,40

**REGLA DURA:** `fotograma-video` no se anima **en absoluto** — ni escala, ni opacidad, ni desenfoque, ni sombra. Congélalo con un `will-change: auto` y no lo metas en ninguna capa animada. Si algo lo roza, el plano pierde su argumento.

**Componentes:** `fotograma-video` (INMÓVIL) · `rotulo-kicker` · `cifra-antes` · `cifra-despues` · `unidad` · `cronometro` · `pista[0..3]` · `sello-identico`

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 0,00 | `rotulo-kicker`, `fotograma-video`, `pista[0..3]` | ya presentes | La cifra “después” arranca como `—`. |
| 0,00 → 0,60 | `cronometro` | conteo | 0,0 → 0,6 s en **tiempo real, LINEAR**. Que coincida con el reloj de verdad es parte del argumento. |
| 0,35 | `pista[1]` (`eng` audio) | salida | x 0 → +120 px · opacity 1 → 0 · **280 ms EASE_IN**. |
| 0,62 | `pista[3]` (`eng` subs) | salida | igual, escalonada. Las pistas `spa` no se mueven ni un píxel. |
| 0,35 | `cifra-antes` | tachado | la línea de `line-through` se dibuja de izquierda a derecha en 260 ms EASE_OUT (usa un pseudo-elemento con `scaleX`, no `text-decoration` animado). |
| 0,50 → 1,40 | `cifra-despues` | conteo | de `—` a 1024 → 133 (mostrado en MB) en 900 ms EASE_OUT; la unidad cambia `GB` → `MB` con `per-word-crossfade` de una sola palabra. |
| 1,40 → 3,40 | — | **quietud** | 2 s de nada. El vacío es el que hace que el sello golpee. |
| **3,40** | `sello-identico` | `spring-scale-in` | scale 1,12 → 1, SPRING; el halo verde (`0 0 0 10px rgba(93,184,114,0.18)` → 0) se apaga en 240 ms. |
| 3,40 | `sello-identico` texto | `per-word-crossfade` | «vídeo idéntico» — 2 palabras, stagger 60 ms, arranca 80 ms después de la escala. |
| 3,66 → 3,87 | — | **quietud** | |

---

## 6 · Plano 6 · El corte — 5,16 s (2 compases)

Golpes: `0,01 · 0,50 · 1,14 · 1,79 · 2,60 · 3,08 · 3,73 · 4,37`

**El tajo no se anima: se hereda.** El primer fotograma renderizado del plano ya tiene el corte hecho y el destello a plena opacidad. No hay fotograma “antes”; si animas la partición, el corte deja de coincidir con la entrada de la música.

**Componentes:** `rotulo-kicker` · `duracion-total` · `nombre-origen` · `pieza[0..1]` · `destello-tajo` · `pildora[0..1]` · `tiempo-ini[0..1]` · `tiempo-fin[0..1]` · `rango[0..1]` · `nombre-final[0..1]`

| t | Componente | Animación | Detalle |
|---|---|---|---|
| **0,01** | `destello-tajo` | ya a plena opacidad | Línea de 2 px `#E7E5FE` + halo elíptico. Se apaga: opacity 1 → 0 en **200 ms EASE_IN**. |
| 0,01 | `pieza[0..1]` | ya separadas 8 px | Estado de partida, no una animación. |
| 0,01 → 0,50 | `pieza[0]` / `pieza[1]` | apertura | x −4 → −44 px / +4 → +44 px (hueco final 88 px) en **490 ms EASE_OUT**. |
| 0,50 | `nombre-origen` | tachado + apagado | línea dibujada en 240 ms EASE_OUT; color → `#4A4F63` en 300 ms. |
| 0,50 | `tiempo-ini/fin[*]` | fade | opacity 0,25 → 1 en 240 ms EASE_OUT. |
| 1,14 | `pildora[0]` (`E04`) | `spring-scale-in` | scale 1,15 → 1. |
| 1,79 | `pildora[1]` (`E05`) | igual | |
| 2,60 | `rango[0]` | `short-slide-right` | `0:00 – 34:12` |
| 3,08 | `rango[1]` | `short-slide-right` | `34:12 – 68:40` |
| 3,73 | `nombre-final[0]` | `short-slide-down` + `depth-parallax-words` | La línea cae 16 px y sus palabras entran con profundidad (stagger 90 ms). Es la carga útil del plano: merece los dos presets. |
| 4,37 | `nombre-final[1]` | igual | |
| 4,70 → 5,16 | — | **quietud** | |

---

## 7 · Plano 7 · En tu servidor — 10,32 s (4 compases)

Golpes originales: `0,02 · 0,50 · 1,15 · 1,78 · 2,60 · 3,09 · 3,56 · 4,05`
Rejilla propuesta para la extensión (mismo pulso, ajústala a la pista): `4,53 · 5,20 · 5,67 · 6,45 · 7,10 · 7,74 · 8,80 · 9,60`

**Componentes:** `parrilla-fondo` · `ficha[0..17]` · `biblioteca` (`barra-superior`, `telon`, `portada`, `titulo-serie`, `meta-serie`, `botonera`, `capitulo[0..5]`) · `velo-viñeta` · `texto-optimiza` · `marca-ondine` · `frase-cierre` · `compatibilidad` (`jellyfin-icono` = imagen incrustada, `plex-icono`, etiquetas)

### Compás 1 — la parrilla enfoca (0,02 → 2,58)

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 0,02 → 1,15 | `parrilla-fondo` | enfoque | blur **18 → 0 px** y opacity **0,55 → 0,30**, ambos EASE_OUT en 1,13 s. Un solo parámetro conduce el compás. |
| 0,50 · 1,15 · 1,78 | `ficha[*]` | relleno por oleadas | Las placas grises `#2A2E42` se sustituyen por carátula + título en 3 oleadas de 6 fichas; por ficha: `per-word-crossfade` del título (2–4 palabras) y la carátula con opacity 0 → 1 en 240 ms EASE_OUT. Orden aleatorio dentro de cada oleada. |
| 2,58 | corte | duro | Corte seco a la biblioteca. **Sin transición**: el salto de “lo que ve el servidor” a “lo que ves tú” es el efecto. |

### Compás 2 — la biblioteca (2,60 → 5,18)

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 2,60 | `biblioteca` | entrada por corte | Aparece ya montada, sin fade. Scroll en 0. |
| 2,60 → 10,32 | `biblioteca` (contenedor de scroll) | **desplazamiento continuo** | `translateY` de **0 a −1240 px** en **7,72 s LINEAR**. No se detiene nunca, ni bajo el overlay: es lo que impide que el cierre se sienta congelado. |
| 2,60 | `velo-viñeta` | reposo | radial al 0 % en el centro / 28 % en los bordes. |
| 3,09 · 3,56 · 4,05 | `capitulo[*]` | acento de lectura | Cada golpe, el capítulo que cruza el centro óptico sube su título de `#C9CCD6` a `#E9E9ED` durante 400 ms y vuelve. Nada más: el scroll ya es el movimiento. |
| 4,53 | `titulo-serie` | ya fuera de cuadro | El scroll ha dejado atrás la cabecera; a partir de aquí solo se ven capítulos. |

### Compás 3 — «Optimiza tus bibliotecas para tus plataformas favoritas» (5,20 → 7,74)

| t | Componente | Animación | Detalle |
|---|---|---|---|
| 5,20 → 5,90 | `velo-viñeta` | cierre | centro 0 → 0,62 · bordes 0,28 → 0,74 en 700 ms EASE_IN_OUT. La biblioteca se hunde sin dejar de moverse. |
| **5,20** | `texto-optimiza` | `per-word-crossfade` | 7 palabras · stagger 60 ms · 260 ms cada una ≈ 620 ms de total. Sin desplazamiento: el fondo ya se mueve, el texto tiene que estar clavado. |
| 5,67 | `texto-optimiza` | (en curso) | La cuarta palabra cae justo en este golpe; cuadra solo si el arranque es 5,20. |
| 6,45 → 7,10 | — | lectura | El texto se queda quieto un compás entero. No lo toques. |
| 7,10 | `texto-optimiza` | `blur-out-up` | y → −28 px · blur → 10 px · opacity → 0 en 320 ms EASE_IN. |
| 7,10 | `texto-optimiza` ↔ `marca-ondine` | `shared-axis-y` | El relevo es un único gesto: la marca arranca 80 ms antes de que el texto acabe de salir. |

### Compás 4 — cierre y bucle (7,74 → 10,32)

| t | Componente | Animación | Detalle |
|---|---|---|---|
| **7,74** | `marca-ondine` | `spring-scale-in` | scale 0,96 → 1 · opacity 0 → 1 · SPRING. Trazo y palabra entran juntos, como un solo objeto. |
| 7,74 → 8,40 | `velo-viñeta` | cierre | centro 0,62 → 0,80 · bordes 0,74 → 0,86, 660 ms EASE_IN_OUT. |
| 8,80 | `frase-cierre` | `kinetic-center-build` | «Organización en un solo click» — 4 anillos desde el centro, stagger 70 ms, letter-spacing +0,05em → −0,015em. El momento tipográfico del cierre. |
| 9,60 | `compatibilidad` | `short-slide-down` escalonado | «funciona con» → `jellyfin-icono` + etiqueta → separador → `plex-icono` + etiqueta. Stagger 70 ms, 220 ms cada uno. El icono de Jellyfin es la imagen incrustada: solo opacidad y `translateY`, nunca escala (se pixela). |
| 9,60 → 10,02 | `velo-viñeta` | cierre final | centro 0,80 → 0,88 · bordes 0,86 → 0,92. |
| 10,02 → 10,32 | todo | **quietud + cierre a negro** | La viñeta puede llevar el centro a `#000` en los últimos 300 ms para encadenar con el arranque en negro del plano 1. El scroll de la biblioteca sigue corriendo por debajo hasta el último fotograma. |

---

## 8 · Tabla resumen de presets por componente

| Componente | Preset | Plano |
|---|---|---|
| `nombre[0..4]` (ficheros sucios) | `short-slide-down` | 2 |
| `rotulo-kicker` (todos los planos) | `short-slide-right` | 2·3·4·5·6 |
| `equis[*]`, `aro-golpe` | SNAP a medida (no es texto) | 2 |
| `campo-url`, `codigo-linea[*]` | `short-slide-down` + máquina de escribir | 3 |
| `contador-24` | `spring-scale-in` + conteo LINEAR | 3 |
| `pildora[*]` «Correcto» | fade LINEAR (sin preset: el plano prohíbe muelles) | 4 |
| `contador-correctos` | conteo LINEAR | 4 |
| `sello-identico` | `spring-scale-in` + `per-word-crossfade` | 5 |
| `pildora[0..1]` `E04`/`E05` | `spring-scale-in` | 6 |
| `rango[0..1]` | `short-slide-right` | 6 |
| `nombre-final[0..1]` | `short-slide-down` + `depth-parallax-words` | 6 |
| `ficha[*]` títulos de carátula | `per-word-crossfade` | 7 |
| `texto-optimiza` | `per-word-crossfade` (entrada) + `blur-out-up` (salida) | 7 |
| `texto-optimiza` → `marca-ondine` | `shared-axis-y` | 7 |
| `marca-ondine` | `spring-scale-in` | 7 |
| `frase-cierre` | `kinetic-center-build` | 7 |
| `compatibilidad` | `short-slide-down` escalonado | 7 |

## 9 · Qué NO animar

- El `fotograma-video` del plano 5. Nada, nunca.
- El contador de `conflictos` del plano 4 (el `0` es estático).
- Las pistas `spa` del plano 5 (que se queden quietas es el mensaje).
- La partición del plano 6 (se hereda del primer fotograma).
- El icono de Jellyfin del plano 7 en escala (es bitmap: solo opacidad y desplazamiento).
- Los golpes 4,53 y 5,02 del plano 2: están vacíos a propósito.
