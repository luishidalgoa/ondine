# Encargo para Claude Design — los planos del spot de Ondine

Necesito el **diseño visual** de seis planos de un spot. No hace falta que animes nada: quiero
**fotogramas clave**, y yo me encargo de la animación después.

**Una página por plano.** Cada página es un lienzo de **1920×1080**.

---

## Qué es Ondine

Una app de escritorio para Windows que **prepara una biblioteca de series y películas antes de
que Plex o Jellyfin la escaneen**. Esos servidores enseñan una biblioteca preciosa, pero solo si
los ficheros ya están bien nombrados; cuando no lo están, se rinden y el episodio sale como
«Desconocido». Ondine es el paso intermedio: **comprime** para que quepa, **ordena** contra un
catálogo para que el servidor lo reconozca, y **parte** los ficheros que traen dos capítulos
pegados.

**A quién le habla:** gente que se monta su propio servidor de medios en casa. Mundo del
*self-hosting* y el *homelab*. Técnicos o semitécnicos, alérgicos al software cerrado, que premian
la sobriedad y detestan lo pomposo.

**Tono:** herramienta de precisión, no app de consumo. Nada de brillos, biseles ni degradados de
app de móvil. La sobriedad es el argumento.

---

## Sistema visual — esto no se negocia

Es la paleta real de la aplicación. Úsala exacta, no aproximada.

| Papel | Color |
|---|---|
| Lienzo / fondo | `#161826` |
| Superficie, tarjetas | `#232532` |
| Campos, hundidos | `#292B31` |
| Texto | `#E9E9ED` |
| **Acento** | **`#968AE0`** |
| Acento claro | `#B5ABFC` · `#D2CEFD` · `#E7E5FE` |
| Acento oscuro | `#796CBF` · `#5D5294` |
| Superficie honda (código) | `#101120` |
| Verde de «correcto» | `#5DB872` |
| Rojo de error | `#C64545` |

**Tipografía:** Inter para todo. JetBrains Mono para nombres de fichero, código y datos.

**El acento es escaso.** Un solo momento de voltaje por plano. Si el morado está en todas partes,
deja de significar nada.

**Todo va sobre fondo oscuro**, con una excepción deliberada que verás en el plano 3.

---

## La marca

Tres trazos horizontales que van de **onda a media onda a recta**: lo que entra revuelto sale
ordenado. Retícula de 64, grosor de trazo 6, extremos redondos. Es un **trazo abierto sin
contenedor** — nada de recuadros, fondos ni degradados detrás.

```svg
<svg viewBox="0 0 64 64" fill="none" stroke="#968AE0" stroke-width="6"
     stroke-linecap="round" stroke-linejoin="round">
  <path d="M7 17 C 15.5 9 26.5 25 35 17 C 43.5 9 48.5 25 57 17"/>
  <path d="M7 32 C 16 28 26 36 35 32 C 44 28 48 36 57 32"/>
  <path d="M7 47 L 57 47"/>
</svg>
```

Cuando la marca va con el nombre: separación de media altura de la marca, y la marca se alinea con
la **altura de la equis** del nombre, no con la caja de texto. Inter Medium (500), tracking
+0,01 em, sin mayúsculas y sin negrita.

---

## Por qué te doy segundos, y por qué importan

El spot va **cortado a la música**: 93 BPM, compás de 2,58 s. Cada plano dura un número redondo de
compases y **cada movimiento cae sobre un golpe medido de la pista**.

Eso condiciona tu diseño: si un plano tiene un golpe fuerte en el segundo 4,05, ahí tiene que
**pasar algo grande**, y la composición tiene que dejarle sitio. Un diseño precioso pero cerrado,
sin un hueco donde quepa ese suceso, no me sirve.

Por eso, para cada plano te pido **tres fotogramas**: inicio, medio y final. Entre ellos pongo yo
la animación.

---

# Los seis planos

> El **plano 1 ya está hecho y aprobado**: la marca se ensambla desde una nube de partículas sobre
> negro y el nombre entra con las letras desenfocándose hacia nítido. Es la referencia de tono para
> todo lo demás — sobrio, oscuro, poco elemento y mucho aire. **No lo rediseñes**; úsalo como norte.

---

## Plano 2 · El desorden — 5,16 s

**Qué tiene que entenderse:** una biblioteca casera está llena de ficheros nombrados a mano y mal,
y el servidor no sabe qué son.

**Contenido exacto** (en mono, no cambies los nombres):

```
Cap 12 (2).mkv
episodio final BUENO.mkv
Temporada2_04-05.mkv
Sin titulo (copia).mkv
aqui no hay quien viva 3x07.avi
```

Al fondo, muy desenfocada, una parrilla de fichas con huecos y placas que dicen «Desconocido»: es
lo que ve el servidor.

**Golpes** (segundos): 0,18 · 0,83 · 1,19 · 1,79 · 2,59 · 3,05 · 3,57 · **4,05** · 4,53 · 5,02
Cae un nombre en cada uno de los primeros. En 3,05, 3,57 y **4,05** se marcan tres de ellos con
una **✕** roja. El de 4,05 es **el golpe más fuerte de todo el spot**.

**Prohibido:** nombres de *scene-release* (nada de `AMZN`, `WEB-DL`, `x265`, nombres de grupo). El
desorden de una casa son nombres escritos a mano, no descargas. Es una regla dura.

---

## Plano 3 · Se lo pides a la IA — 6,45 s

**Qué tiene que entenderse:** el catálogo no lo tecleas tú. Coges una lista de episodios de
cualquier web y la app la convierte en el fichero que necesita.

**Este es el único plano CLARO de toda la pieza**, y es deliberado: una página de enciclopedia con
fondo blanco. Ese salto de oscuro a blanco y vuelta a oscuro hace legible de un vistazo que el dato
**viene de fuera**.

Dos elementos: una **página de enciclopedia** (fondo blanco, título en serifa, barra lateral con las
temporadas, tabla de episodios con cabecera y filas alternas) y el **panel de la app** (oscuro), con
un cursor que lleva la URL de una a otro.

Texto del panel: rótulo **Fuente**, debajo la frase *«Pega una lista de episodios de cualquier
web.»*, luego el campo, y un botón **«Generar con IA…»**. Después, un catálogo en mono
escribiéndose línea a línea sobre `#101120`.

Los títulos de los episodios empiezan todos por «Érase…» (*Érase un okupa*, *Érase un ascensor*…).

**Golpes:** 0,50 (el cursor llega) · 1,13 · 1,52 · 2,11 (copia) · **2,76** (se pulsa el botón) ·
3,40 · 4,05 · 4,52 (una línea de catálogo por golpe)

**Ojo:** el logotipo de la esfera de Wikipedia es **marca registrada**. Diséñalo con una marca
neutra de enciclopedia, no con la suya.

---

## Plano 4 · Queda ordenado — 5,16 s

**Qué tiene que entenderse:** la biblioteca entera queda identificada, y no se ha tocado nada sin
permiso.

Una ventana de la app con una tabla de ficheros agrupados por temporada. Cada fila muestra el
**nombre original** y, al lado, el **nombre propuesto**. Las filas van pasando a una píldora verde
que dice «Correcto». Contadores grandes: **246 correctos · 0 conflictos**.

**Golpes: NINGUNO.** Este plano cae entero dentro de un bajón de la música — es el único que se
sostiene sobre el silencio. Diséñalo para que **respire**: composición tranquila, sin elementos que
pidan un golpe, sin nada que grite. Es el momento de calma antes del final.

---

## Plano 5 · Adelgaza sin tocarse — 3,87 s

**Qué tiene que entenderse:** puedes quitarle a un vídeo los doblajes y subtítulos que no usas
**sin recomprimirlo**. El vídeo queda idéntico.

A un lado, el fotograma de un vídeo. Al otro, el tamaño bajando de **155 MB a 134 MB**, un
cronómetro que marca **0,6 s**, y las pistas: se queda `spa`, se van `eng` (audio) y `eng`
(subtítulos). Al final, un sello: **«vídeo idéntico»**.

**REGLA DURA:** el fotograma del vídeo **no puede cambiar ni un píxel**. Esa quietud *es* el
argumento. Diseña el plano de forma que la imagen sea claramente lo único que no se mueve.

**Golpes:** uno solo, en **3,40**, a medio segundo del final. Ahí entra el sello.

---

## Plano 6 · El corte — 5,16 s

**El plano más importante del spot.** Es lo único que hace Ondine y no hace ninguna competidora:
entender que un fichero trae **dos capítulos pegados** y partirlo.

Un bloque ancho etiquetado `Temporada2_04-05.mkv`, con su línea de tiempo y miniaturas, que **se
parte por la mitad**. Las dos piezas se separan y reciben su nombre:

```
Aqui no hay quien viva - S02E04 - Érase una okupa.mkv
Aqui no hay quien viva - S02E05 - Érase un ascensor.mkv
```

Con sus píldoras **E04** y **E05** y sus tiempos: `0:00–34:12` y `34:12–68:40`.

**Golpes:** **0,01** (aquí se parte) · 0,50 · 1,14 · 1,79 · **2,60** · 3,08 · 3,73 · 4,37

El del **0,01** es especial: la música vuelve a entrar exactamente en el primer fotograma de este
plano. El corte y el golpe son el mismo instante. Diseña el plano para que **el tajo sea lo primero
y lo único** que ocurre, y todo lo demás llegue después.

---

## Plano 7 · En tu servidor — 5,16 s

**Qué tiene que entenderse:** el resultado. La biblioteca que antes tenía huecos ahora está
completa en tu servidor.

La misma parrilla desenfocada del plano 2, ahora **enfocando**: las placas «Desconocido» se
rellenan con carátulas y títulos. Después entra la marca de Ondine con el nombre, y la frase:

> **Lo que pasas antes de que Plex escanee.**

Y debajo, pequeño y atenuado: *funciona con Plex, Jellyfin y Kodi*.

**Golpes:** 0,02 (empieza a enfocar) · 0,50 · **1,15** (entra la marca) · 1,78 · 2,60 (entra la
frase) · 3,09 · 3,56 · 4,05

**Reglas duras:** Plex, Jellyfin y Kodi van **solo como texto de compatibilidad**. Nada de sus
logotipos, ni sus colores, ni imitar sus interfaces — insinuaría que respaldan el producto. Las
carátulas de la parrilla son **formas planas** de la paleta de Ondine, con títulos inventados; no
uses portadas reales.

Es el último plano de una pieza que va **en bucle**, así que tiene que quedar tranquilo y poder
encadenar con un arranque en negro.

---

## Qué necesito de vuelta

Para **cada plano**, en su propia página:

1. **Tres fotogramas** — inicio, medio y final — a 1920×1080.
2. Una nota corta de **qué se mueve entre uno y otro**, si tienes una idea concreta.
3. Los colores exactos que hayas usado, si te has salido de la paleta por algún motivo.

Lo que **no** necesito: animación, transiciones entre planos, ni sonido. De eso me encargo yo.

## Lo que más me sirve que cuestiones

Si crees que la composición de un plano no aguanta el mensaje —que hay demasiado, o demasiado
poco, o que el ojo va al sitio equivocado— dímelo y propón otra. El texto y los datos son fijos;
la composición no.
