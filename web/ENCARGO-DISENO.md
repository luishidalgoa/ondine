# Encargo para Claude Design — la web de Ondine

Necesito el **diseño visual** de una página única (SPA de scroll, sin navegación entre vistas).
No hace falta que animes nada: quiero **fotogramas clave**, y la animación la pongo yo después.

**Una página por sección.** Cada página es un lienzo de **1440×900** para el estado de escritorio.
Para las tres secciones que marco, añade además su versión **390×844** de móvil.

---

## Qué es Ondine

Una app de escritorio para Windows —y una herramienta de terminal para Linux, macOS y Windows—
que **prepara una biblioteca de series y películas antes de que Plex, Jellyfin o Kodi la escaneen**.

Esos servidores enseñan una biblioteca preciosa, pero solo si los ficheros ya están bien nombrados.
Cuando no lo están se rinden: el episodio sale como «Desconocido», sin carátula y sin sinopsis.
Ondine es el paso intermedio.

Hace tres cosas, y las tres suben **la calidad del dato**:

- **Comprime** para que quepa. Un 80-90 % menos, con aceleración por hardware.
- **Ordena** para que el servidor lo reconozca: identifica cada episodio contra un catálogo y
  propone su nombre canónico.
- **Parte y recorta** lo que viene pegado o sobra.

Y una cuarta que es la más difícil de explicar y la que más convence: **adelgaza sin recomprimir**.
Quita los doblajes y subtítulos que no usas y el vídeo queda **idéntico bit a bit**. 155 MB a 134 MB
en 0,6 s.

**A quién le habla:** gente que se monta su propio servidor de medios en casa. Mundo del
*self-hosting* y el *homelab*. Técnicos o semitécnicos, alérgicos al software cerrado, que premian
la sobriedad y detestan lo pomposo.

**Tono:** herramienta de precisión, no app de consumo. Nada de brillos, biseles ni degradados de
app de móvil. La sobriedad es el argumento.

---

## Qué tiene que conseguir la página

Una sola cosa: **que se descargue**. Todo lo demás está al servicio de eso.

El visitante llega casi siempre desde un sitio donde ya se habla de servidores de medios —Reddit,
un foro de homelab, un comentario—, así que **ya conoce el problema**. No hay que explicárselo desde
cero: hay que hacer que se reconozca en él en los primeros tres segundos.

No hay precios, no hay registro, no hay newsletter. Es gratis y de código abierto.

---

## Sistema visual — esto no se negocia

Es la paleta real de la aplicación y del spot. Úsala exacta, no aproximada.

| Papel | Color |
|---|---|
| Lienzo / fondo | `#161826` |
| Superficie, tarjetas | `#232532` |
| Campos, hundidos | `#292B31` |
| Texto | `#E9E9ED` |
| Texto secundario | `#C9CCD6` · `#8A8FA3` |
| Texto apagado | `#6C7080` · `#4A4F63` |
| **Acento** | **`#968AE0`** |
| Acento claro | `#B5ABFC` · `#D2CEFD` · `#E7E5FE` |
| Acento oscuro | `#796CBF` · `#5D5294` |
| Superficie honda (código) | `#101120` |
| Verde de «correcto» | `#5DB872` |
| Rojo de error | `#C64545` |

**Tipografía:** Inter para todo. JetBrains Mono para nombres de fichero, código y datos.

**El acento es escaso.** Un solo momento de voltaje por sección. Si el morado está en todas partes,
deja de significar nada. En el spot esa regla se aplicó a rajatabla y es la mitad de por qué
funciona.

**Todo va sobre fondo oscuro.** La página no tiene modo claro y no lo va a tener.

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

## Lo que ya existe y hay que respetar

Hay un **spot de 44 segundos** ya producido y publicado. La web y el spot tienen que parecer la
misma cosa, porque lo son.

De él salen tres cosas que quiero reutilizar como material, no reinventar:

1. **La comparativa antes/después.** Dos capturas de un servidor de medios: a la izquierda la
   biblioteca con los nombres de fichero en crudo (`Cap 12 (2).mkv`, `episodio final BUENO.mkv`)
   y carátulas de relleno; a la derecha la misma biblioteca con títulos, numeración, sinopsis y
   portada. Es el argumento entero en una imagen.
2. **El rótulo de sección** en mono, versales, tracking 0,22 em, precedido de un `✳` en acento.
   Es el único sitio donde el morado aparece en texto pequeño. **Ojo: en el spot hay uno por
   plano porque cada plano dura cinco segundos y el rótulo es la única ancla. En una página que
   se recorre entera de un tirón, uno por sección se convierte en plantilla.** Máximo tres en
   toda la página, contando el del hero. Ver «Reglas de composición».
3. **El acero.** Los rótulos grandes llevan un degradado metálico horizontal muy suave
   (`#AEB4BF` a `#F6F8FB`, altos de color a distancias desiguales). No lo apliques al logotipo
   ni al nombre «Ondine» — ahí la palabra es la marca y va en blanco plano.

---

## Reglas de composición

Esto va aparte de la paleta porque no es cuestión de gusto: son las cosas que hacen que una
página se lea como generada en lugar de diseñada. Las he ordenado por lo que más daño hace.

**1 · Ninguna familia de composición se repite.** El riesgo grande de este encargo es que casi
todo lo que cuenta Ondine es un «esto se convierte en esto», y la salida natural para eso es
partir la pantalla en dos. Si «Ordenar», «Partir», «Adelgazar», «Comprimir» y «Antes/después»
son cinco pantallas partidas por la mitad, la página se lee como una sola sección repetida cinco
veces y deja de haber jerarquía: el argumento diferencial pesa lo mismo que el de relleno.

Como máximo **dos secciones seguidas** pueden usar el reparto izquierda/derecha, y en toda la
página quiero **al menos cuatro familias distintas**. Alternativas que sí encajan aquí:

- un bloque a sangre en el que el objeto está centrado y lo que cambia es él (para «Partir»)
- una banda horizontal larga que se recorre lateralmente (para una lista de formatos o pistas)
- apilado vertical con el dato grande arriba y la evidencia debajo (para «Adelgazar»)
- una sección de una sola cifra enorme, sin ilustración

**2 · Cero rayas largas.** Ni `—` ni `–` en ningún texto visible: ni titulares, ni rótulos, ni
píldoras, ni botones, ni pies de imagen, ni cuerpo. Guion normal `-` y punto y aparte. Los rangos
van con guion: `80-90 %`, `0:00-34:12`, `34:12-1:08:40`. **Este documento sí usa rayas largas
porque es un documento; la página no.** No copies el registro de aquí.

**3 · Máximo tres rótulos de sección en toda la página**, contando el del hero. No uno por
sección. Si una sección no lleva rótulo, el titular basta: el sitio donde está ya dice qué es.
Y el `✳` va solo en esos tres, no delante de cada elemento de lista ni de cada fila de tabla.

**4 · Nada de numerar secciones a la vista.** Ni `01 / EL PROBLEMA`, ni `002 · Ordenar`, ni
`Paso 1 / Paso 2 / Paso 3`. Los números del 1 al 9 de este documento son para que nos entendamos
tú y yo, no para imprimirlos.

**5 · El punto medio `·` como mucho una vez por línea.** No es el separador por defecto de todo.

**6 · Sin señuelos de scroll.** Ni «Scroll», ni flechita animada, ni ratoncito. Quien está
mirando el hero ya sabe que la página baja.

**7 · Las capturas son capturas de verdad.** Las maquetas de biblioteca y de la app no se
dibujan con rectángulos: son pantallazos reales de Ondine y de un servidor real, y yo te los
paso. Una interfaz falsa construida a base de cajas se nota siempre, y además este público la
va a mirar con lupa porque es la que usa a diario. Dime qué pantallazos necesitas y de qué
sección, y te los mando. Lo único que sí es ilustración son las carátulas, que van en formas
planas de la paleta con títulos inventados por la regla de más abajo.

**8 · Los números ya están y son reales.** `246 correctos`, `0 conflictos`, `155 MB a 134 MB`,
`0,6 s`, `80-90 %`. Salen de la aplicación. No inventes más cifras para rellenar, ni porcentajes
de satisfacción, ni número de usuarios, ni «4,8/5». No hay ninguno y no lo va a haber.

**9 · Cada sección tiene un mensaje.** No hay titular grande a la izquierda con un párrafo
pequeño flotando arriba a la derecha. Si hace falta explicar, va debajo del titular y en una
medida legible.

**10 · Una llamada a la acción, con un solo nombre.** Es «Descargar para Windows» en el hero y
en la sección 8, con el mismo texto en los dos sitios. Nada de «Pruébalo», «Empezar» o
«Consíguelo» mezclados.

**11 · Toda la página es oscura, sin excepción.** Esto ya lo dice la paleta, pero lo repito
porque es donde más se rompe: ninguna sección se pone clara para «airear». La sección clara en
mitad de una página oscura se lee como haber aterrizado en otra web.

**12 · Un solo radio de esquina** en toda la página. Elige tú cuál, pero que no haya tarjetas
redondeadas conviviendo con botones cuadrados.

---

# Las secciones

## 1 · Hero

**Qué tiene que entenderse en tres segundos:** esto arregla la biblioteca que tienes hecha un
desastre.

Titular: **«Ordena bibliotecas de series y películas»**
Bajada: *lo que pasas antes de que Plex o Jellyfin escaneen.*

Un botón primario **Descargar para Windows** y un secundario **Instalar por terminal** que ancla a
la sección de descarga. El comando en mono **no va en el hero**: es un quinto elemento de texto y
el hero aguanta cuatro.

**Cuatro elementos de texto como máximo**, y ya están gastados: marca, titular, bajada, botones.
Nada de línea pequeña bajo los botones, ni tira de compatibilidad, ni versión, ni «beta», ni
«código abierto» suelto ahí. Todo eso tiene su sección más abajo.

**El titular cabe en dos líneas y la bajada en una.** Si no cabe, baja el cuerpo de letra; no
partas más líneas ni recortes la ilustración.

**No pongas una captura de la app en el hero.** La app es una ventana de Windows con tablas; a
tamaño hero se lee como software de contabilidad.

Pero **tampoco lo dejes en tipografía sola sobre fondo**: un titular grande sobre `#161826` con
un degradado detrás no es un hero, es un marcador de posición. Mi propuesta, y quiero que la
discutas si no la ves: **un recorte muy cerrado de la comparativa**. Una sola ficha de episodio,
la de antes y la de después, a tamaño grande, donde se lea el nombre de fichero en crudo
convirtiéndose en el título de verdad. La comparativa completa de biblioteca ya vuelve en
«Antes / después», y a esa escala tan distinta no se lee como repetición.

También quiero **móvil** de esta sección.

## 2 · El problema

**Qué tiene que entenderse:** el servidor no es tonto, es que le has dado datos malos.

Contenido exacto, en mono y sin cambiar los nombres:

```
Cap 12 (2).mkv
episodio final BUENO.mkv
Temporada2_04-05.mkv
Sin titulo (copia).mkv
aqui no hay quien viva 3x07.avi
```

Al lado o debajo, lo que el servidor hace con eso: fichas sin carátula, el icono de película de
relleno y la duración como único dato que sí sabe leer.

**Prohibido:** nombres de *scene-release* (nada de `AMZN`, `WEB-DL`, `x265`, nombres de grupo). El
desorden de una casa son nombres escritos a mano, no descargas. Es una regla dura.

## 3 · Partir

**La sección más importante, y por eso está aquí y no más abajo.** Es lo único que hace Ondine y
no hace ninguna competidora: entender que un fichero trae **dos capítulos pegados** y partirlo.

Un bloque ancho etiquetado `Temporada2_04-05.mkv` que se parte por la mitad. Las dos piezas reciben
su nombre:

```
Aqui no hay quien viva - S02E04 - Érase una okupa.mkv
Aqui no hay quien viva - S02E05 - Érase un ascensor.mkv
```

Con sus píldoras **E04** y **E05** y sus tiempos: `0:00-34:12` y `34:12-1:08:40`.

Diseña el estado **antes** del corte y el estado **después**: la animación entre los dos la pongo yo.

**Esta es la sección donde menos quiero la pantalla partida en dos.** El objeto que se parte es
uno solo y debería estar centrado y grande, ocupando el ancho: lo que cambia es él, no hay un
«lado de antes» y un «lado de después». Si la compones a dos columnas, se confunde con la
comparativa del final.

## 4 · Ordenar

**Qué tiene que entenderse:** el catálogo no lo tecleas tú, y no se renombra nada sin tu permiso.

Dos momentos:

- **De dónde sale el catálogo.** Pegas la dirección de cualquier anexo de episodios de la web y la
  IA lo convierte en el fichero que la app necesita. Enseña el JSON escribiéndose, en mono sobre
  `#101120`, con líneas reales: `{ "num": 4, "temp": 2, "titulo": "Érase una okupa" }`.
- **La propuesta.** Una tabla con el nombre original y el nombre propuesto, agrupada por temporada,
  con una píldora verde «Correcto» por fila. Contadores grandes: **246 correctos · 0 conflictos**.

Ese `0 conflictos` es el dato tranquilizador y merece peso visual.

También quiero **móvil** de esta sección, porque la tabla a dos columnas es justo lo que peor cabe
en un teléfono y quiero ver tu solución.

## 5 · El tamaño

Esta sección eran dos y ahora es una, con **dos tiempos muy desiguales**. El de arriba es el que
sorprende; el de abajo es el que la gente ya espera de cualquier herramienta. Si les das el mismo
peso, el bueno se pierde.

**Arriba: adelgazar sin recomprimir.** Puedes quitarle a un vídeo los doblajes y subtítulos que no
usas **sin tocar el vídeo**. Queda idéntico.

A un lado, el fotograma de un vídeo. Al otro, el tamaño bajando de **155 MB a 134 MB**, un cronómetro
que marca **0,6 s**, y las pistas: se queda `spa`, se van `eng` (audio) y `eng` (subtítulos).
Al final, un sello: **«vídeo idéntico»**.

**Regla dura:** el fotograma del vídeo tiene que leerse como lo único que no cambia. Que la
composición lo diga sola.

**Abajo, más contenido: comprimir.** Cuando sí quieres recomprimir, lo hace bien y te lo dice
antes. Reduce un **80-90 %** con aceleración por hardware, y antes de empezar enseña un
**pronóstico** del tamaño final; si quieres afinarlo, lo **mide de verdad** codificando muestras
cortas.

Ese «te lo digo antes de hacerlo» es el argumento, no el porcentaje.

**Y el contraste entre los dos tiempos es el remate:** uno es gratis y no toca el vídeo, el otro
baja mucho más pero cuesta calidad. Que se puedan comparar de un vistazo.

## 6 · Antes / después

La comparativa completa, a sangre. Es el cierre del argumento y quiero que ocupe pantalla entera.

Rótulos **ANTES** y **DESPUÉS**. El «antes» en gris de estado, el «después» con el acento.

## 7 · Descarga

Repite la llamada del hero, ahora que ya está convencido.

**Descargar para Windows** grande, y debajo las otras salidas: Linux, macOS y Windows por terminal,
cada una con su comando en mono.

Y tres promesas que importan a este público, en pequeño y sin adorno:

- Nunca toca los originales salvo que se lo pidas, y entonces van a la papelera, no a borrado.
- Todo ocurre en tu máquina. No manda tus ficheros a ninguna parte.
- Código abierto, MIT, en GitHub.

## 8 · Pie

Enlace al repositorio, a las releases, al CHANGELOG y al ROADMAP. Licencia. Nada más.

---

## Reglas duras

- **Plex, Jellyfin y Kodi van solo como texto de compatibilidad.** Nada de sus logotipos, ni sus
  colores, ni imitar sus interfaces — insinuaría que respaldan el producto. La única excepción ya
  acordada es la marca de Jellyfin como imagen en la fila de compatibilidad; para Plex, un icono
  neutro y el nombre en texto.
- **Nada de carátulas de obras reales** en las maquetas de biblioteca. Formas planas de la paleta
  con títulos inventados.
- **Sin *dark patterns*:** no hay cuenta atrás, ni «solo hoy», ni contadores de descargas falsos.
- **Sin capturas mentirosas.** Si enseñas la app, que sea lo que la app hace de verdad.

---

## Qué necesito de vuelta

Para **cada sección**, en su propia página:

1. El fotograma a **1440×900** (y **390×844** en el hero, «Ordenar» y «Antes/después»).
2. Una nota corta de **qué se mueve al entrar la sección**, si tienes una idea concreta. La
   implementación va con GSAP y ScrollTrigger, así que piensa en términos de qué se ancla, qué se
   apila y qué se escala al hacer scroll.
3. Para cada sección que propongas anclar o recorrer con el scroll, **qué fotograma se queda fijo
   si el visitante tiene desactivadas las animaciones**. No es un extra: parte de este público
   navega con `prefers-reduced-motion` puesto, y una sección anclada sin ese estado se queda en
   blanco. Si un fotograma no aguanta quieto, es que la sección depende del movimiento para
   contar su argumento, y entonces el problema es la composición.
4. Los colores exactos que hayas usado, si te has salido de la paleta por algún motivo.

Lo que **no** necesito: animación, transiciones entre secciones, ni copia alternativa. De eso me
encargo yo.

## Dos cambios que ya he decidido, y por qué

Los enumero antes de las preguntas porque cambian el índice que acabas de leer.

**«Partir» sube al tercer puesto**, justo detrás de «El problema» y por delante de «Ordenar». El
índice de arriba estaba ordenado por lógica de producto: primero lo que más se usa, luego lo raro.
Pero es lo raro lo que decide la descarga. Partir un fichero con dos capítulos pegados es lo único
que no hace ninguna otra herramienta, y estaba en quinto lugar, que es donde se abandona la página.

**«Comprimir» deja de ser sección y pasa a ser el cierre de «Adelgazar».** Son dos secciones que
dicen «ocupa menos» seguidas, y eso reparte el impacto en lugar de sumarlo. Además la comparación
entre las dos es justamente el argumento: adelgazar es gratis y no toca el vídeo, comprimir cuesta
calidad pero baja mucho más. Van juntas o no se entiende ninguna. La sección queda como **una sola,
con dos tiempos**: arriba el número que sorprende (155 MB a 134 MB en 0,6 s, vídeo idéntico) y abajo,
más contenido, el 80-90 % con su pronóstico.

Con eso el índice queda en **ocho secciones**: hero, el problema, partir, ordenar, el tamaño,
antes/después, descarga, pie. Que son también las que permiten los tres rótulos de la regla 3.

## Lo que más me sirve que cuestiones

- **El hero.** Es el que tengo menos resuelto y el que más decide. Te he dejado una propuesta ahí
  arriba, pero es la parte del encargo donde menos apego tengo a lo que he escrito.
- **Los dos cambios de aquí encima.** Están razonados, no son inamovibles. Si al componerlo ves
  que «Comprimir» necesita su aire, o que «Partir» tan arriba deja la página sin recorrido, dilo.
- **Si todavía sobra alguna.** Ocho es mejor que nueve, pero puede que siga siendo una más de la
  cuenta. Sospecho de «El problema»: si el hero acaba usando el recorte de la comparativa, puede
  que ya esté contado y esa sección sea repetirlo con menos fuerza.

Si crees que la composición de una sección no aguanta el mensaje —que hay demasiado, o demasiado
poco, o que el ojo va al sitio equivocado— dímelo y propón otra. El texto y los datos son fijos;
la composición no.
