# Plan del clip publicitario

Vídeo corto en bucle para el *hero* de la landing. **No es una captura de pantalla**: es un render
de motion graphics. La app ya se ve en las capturas del README; el clip está para contar la tesis,
no para enseñar botones.

> **La tesis, en una frase**: esto es lo que pasas **antes** de que Plex escanee.

---

## 1. Qué contar (y qué no)

El análisis competitivo dice algo incómodo que conviene tener presente al escribir el guion:
**el renombrado contra catálogo es la función más saturada del nicho.** FileBot lleva quince años
haciéndolo, y Sonarr y Radarr lo hacen gratis. Un clip que diga «renombro tus episodios» compite
de frente contra eso y pierde.

Lo que **no** hace nadie, y por tanto es lo que el clip debe enseñar:

| Prioridad | Qué | Por qué |
|---|---|---|
| **1** | **Partir un episodio en sus historias** (E1a / E1b / E1c) | El hueco más limpio. Ni FileBot ni tinyMediaManager modelan que dentro de un fichero haya tres capítulos. Las alternativas son scripts de GitHub sin mantener. |
| **2** | **Adelgazar sin recomprimir** | Contraintuitivo y demostrable con una cifra. Nadie lo cuenta. |
| **3** | Comprimir + organizar **en una app de escritorio, sin Docker** | Los que comprimen (Tdarr, Unmanic, FileFlows) son contenedores para quien ya tiene un homelab. |
| — | Recortar clips | **No vender por aquí.** LosslessCut es gratis y muy querido. Es la pata más floja. |

---

## 2. Datos que se pueden enseñar

Solo cifras que están escritas y medidas. Un dato inflado en un anuncio es una mentira.

| Dato | Cifra | Fuente |
|---|---|---|
| Adelgazar sin recomprimir | **155 MB → 134 MB en 0,6 s**, vídeo idéntico bit a bit | `CHANGELOG.md:59-60` |
| Partir un episodio | ~**1 s por episodio**, originales a la Papelera con Ctrl+Z | `CHANGELOG.md:110` |
| Catálogo de referencia | **333 episodios · 636 segmentos** (Bob Esponja castellano, T1–T16) | catálogo del autor |

La cifra de compresión con recodificación **no está medida** en el repo. Si se quiere un titular de
«ahorra X %», hay que medirlo antes sobre un lote real y apuntarlo. **No inventarlo.**

---

## 3. Guion — 18 s, bucle limpio

Tres actos. Sin voz en off, sin texto que haya que leer deprisa. El bucle cierra volviendo al
plano 1, así que el último fotograma debe encajar con el primero.

### Acto 1 — el desorden (0 – 5 s)

Nombres de fichero reales cayendo en una columna, desalineados, en gris apagado:

```
bob.esponja.1x01.AMZN.WEB-DL.x265-GRUPO.mkv
Bob_Esponja_-_Cap01-02-03_[castellano].mkv
BobEsponja.S01.E1.SPANISH.1080p.mkv
```

Al fondo, **muy desenfocada**, una parrilla de carátulas con huecos y placas grises de
«Episodio desconocido». Ese es el problema, y se entiende sin explicarlo.

### Acto 2 — el trabajo (5 – 13 s)

El corazón del clip. Dos gestos, no más:

1. **Se parte un episodio.** Un bloque-fichero se divide en tres, y cada trozo recibe su nombre:
   `E1a — Ayudante de cocina` · `E1b — Globos a reventar` · `E1c — Se busca canguro`.
   Es el momento que ninguna otra herramienta puede enseñar. Que respire: **4 segundos**.
2. **Adelgaza sin tocarse.** Una barra de tamaño se desinfla de **155 MB a 134 MB** mientras un
   contador marca **0,6 s** y un sello dice **«vídeo idéntico»**. La imagen del vídeo no parpadea
   en ningún momento: eso *es* el mensaje.

### Acto 3 — la biblioteca (13 – 18 s)

El fondo desenfocado **enfoca**: la parrilla ya está completa, con sus títulos y sin huecos. La
columna de nombres cutres ya no está. Aparece el logotipo y una línea:

> **Lo que pasas antes de que Plex escanee.**

Fundido al plano 1 para el bucle.

---

## 4. Cómo se hace: Remotion

**[Remotion](https://remotion.dev)** — el vídeo se escribe en React y se renderiza a MP4.

Por qué, y no After Effects:

- El clip **vive en el repo** y se versiona con el código. Un `.aep` es un binario que solo abre
  quien tiene la licencia.
- Se **regenera** cuando cambien las cifras o la marca, en vez de quedarse desactualizado.
- Los datos salen de un `.ts` compartido con la landing: **una sola fuente para las cifras**, así
  que la web y el vídeo no pueden contradecirse.
- Si la SPA es React, es el mismo stack y los mismos componentes.

Contrapartida honesta: el acabado de motion es peor que en After Effects, y las animaciones de
carácter (rebotes, elásticos) hay que escribirlas a mano. Para un clip de formas geométricas y
texto como este, no importa.

### Estructura

```
web/
  remotion/
    Root.tsx              # registro de composiciones
    Clip.tsx              # los tres actos encadenados
    escenas/
      Desorden.tsx
      Partir.tsx          # el plano importante
      Adelgazar.tsx
      Biblioteca.tsx
    datos.ts              # ← las cifras, compartidas con la landing
    marca.ts              # colores y tipografía
```

### Salida

- **1920×1080, 30 fps, 18 s** — master
- **1080×1080** cuadrado, para redes
- **WebM (VP9) + MP4 (H.264)**, con `poster` de respaldo
- Objetivo: **por debajo de 2 MB**. Es un *hero*: si tarda en cargar, no lo ve nadie.
- `autoplay muted loop playsinline` — sin sonido, no hace falta.

Ironía útil: el clip del compresor de vídeo debería comprimirse con la propia app.

---

## 5. Marca visual

Del tema actual (`docs/design-brief.md`):

| | |
|---|---|
| Fondo | `#0F1216` |
| Tarjetas | `#1A1F26` |
| Campos | `#232A33` |
| Texto | `#E8EAED` |
| Atenuado | `#8A93A0` |
| **Acento** | **`#6CE8D0`** (turquesa) |

El turquesa marca **solo** lo que la app resuelve: el nombre correcto, el sello de «vídeo
idéntico», la carátula completa. Todo lo demás va en gris. Si el acento se usa en todas partes,
deja de significar nada.

**Movimiento**: nada de rebotes ni elásticos. Transiciones cortas con `easeOutCubic`; que parezca
una herramienta precisa, no una app de consumo.

---

## 6. Parallax y clip: cómo conviven

El clip es el **hero**, arriba, en bucle. El parallax es lo que pasa **al bajar** — y no deben
competir por la atención al mismo tiempo.

Al empezar a hacer scroll, el clip se atenúa y toma el relevo el parallax con las tres capas:
fondo de carátulas (lento) · nombres reescribiéndose (medio) · la ventana real de la app (rápido).

Ahí sí van capturas de verdad, que para eso están en `docs/img/`.

**Accesibilidad**: respetar `prefers-reduced-motion`. Quien lo tenga activado ve el póster estático
y un parallax sin desplazamiento. No es opcional: el movimiento con scroll marea a bastante gente.

---

## 7. Antes de producirlo

1. **Cerrar el nombre.** El logotipo del acto 3 y la marca dependen de él.
2. Medir el ahorro real de una compresión con recodificación, si se quiere ese titular.
3. Decidir dónde vive la landing (hoy **no hay GitHub Pages** configurado en el repo).
