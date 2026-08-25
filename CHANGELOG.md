# Registro de cambios

Todos los cambios relevantes de Ondine se anotan aquí.

> Hasta la v1.4.0 la app se llamaba **ShrinkStudio**. Las entradas anteriores conservan ese
> nombre: son el registro de lo que pasó entonces, y reescribirlas sería falsearlo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el
versionado sigue [SemVer](https://semver.org/lang/es/). Antes de la 1.0 la versión
es `0.MINOR.PATCH`: `MINOR` sube con funcionalidad nueva y `PATCH` con arreglos.

## Contrato

Reglas que cumple **toda** versión publicada. El flujo de trabajo `verificar-version`
las comprueba en cada tag y **falla la publicación** si no se cumplen, así que esto no
es un acuerdo de buena voluntad: está verificado.

1. **Una sección por versión**, con el encabezado exacto `## [X.Y.Z] - AAAA-MM-DD`.
   Lo que aún no se ha publicado vive en la sección de cambios pendientes.
2. **El tag manda**: al empujar `vX.Y.Z` debe existir la sección `## [X.Y.Z]` y la
   propiedad `<Version>` de los `.csproj` debe valer exactamente `X.Y.Z`.
3. **Escrito para quien usa la app**, en español y en pasado: qué cambia para ti, no
   qué fichero se tocó. Nada de nombres de clases ni de ramas.
4. **Una entrada por funcionalidad**, no por commit. Lo trivial (formato, refactores
   internos, cambios de comentarios) no aparece.
5. **Categorías permitidas**, en este orden: `Añadido`, `Cambiado`, `Obsoleto`,
   `Eliminado`, `Corregido`, `Seguridad`. Solo se escriben las que tengan contenido.
6. **Los cambios que rompen algo** se marcan con **RUPTURA** al principio de la línea
   y explican qué hacer.
7. **Sin secciones vacías** ni versiones repetidas, y las versiones van de más nueva a
   más antigua.

## [Unreleased]

## [1.14.1] - 2026-08-25

### Corregido

- **Los desplegables no se veían en Linux ni en macOS.** En la ventana principal salían nueve
  rótulos —Idioma, Formato, Códec, Calidad, Esfuerzo…— con un hueco vacío debajo de cada uno, y
  en Preferencias no había forma de elegir el idioma ni el preset. La aplicación no servía para
  nada. Ya se ven.

- **La ventana no se podía mover ni estirar.** La barra de título propia no tenía puesto el
  arrastre, así que la ventana se quedaba clavada donde el sistema la abriera, y sin marco del
  sistema tampoco había bordes que agarrar. Ahora se arrastra por la barra, se maximiza con doble
  clic y se estira por los cuatro bordes y las cuatro esquinas.

- **En Linux Mint el lanzador salía sin icono**, con un engranaje genérico. El icono va ahora
  también en `/usr/share/pixmaps`, que es la ruta que se mira sin depender del tema de iconos.

- **Los paquetes dicen para qué sistema son.** Se llamaban `Ondine-1.14.0-x64.dmg` y
  `Ondine-1.14.0-x86_64.AppImage`, y en la página de versiones hay diez ficheros juntos: alguien
  con un Linux de 64 bits vio «x64», se bajó el `.dmg` —que es de macOS— y su sistema lo detectó
  como un comprimido cualquiera. Ahora llevan `macos` y `linux` en el nombre.

## [1.14.0] - 2026-08-25

### Añadido

- **Empieza la interfaz para Linux y macOS.** Ondine solo corría en Windows porque su interfaz usa
  WPF, que solo existe ahí. Ya hay una segunda interfaz en marcha —sobre **el mismo motor y el
  mismo catálogo de textos**— y por primera vez **la parte gráfica publica para Linux y macOS**.

  **Ya están las tres pantallas** —Comprimir, Organizar y Recortes— y la ventana que las aloja, con
  su barra de título, su panel lateral, su cola de trabajos y su registro. Fuera de Windows, Ondine
  se abre y se usa entera.

  Y ya se puede **instalar**, que es la otra mitad del asunto. Hay tres paquetes nuevos:

  - un **`.deb`** para Linux Mint, Ubuntu y Debian, que se integra en el menú y en «Abrir con» al
    pulsar el botón derecho sobre un vídeo;
  - un **AppImage** para cualquier Linux, que se descarga, se marca ejecutable y se abre sin
    instalar nada;
  - dos **`.dmg`** para macOS, uno para los Mac con chip de Apple y otro para los Intel.

  Ninguno necesita instalar .NET. Sí hacen falta **ffmpeg** —obligatorio, `apt install ffmpeg` o
  `brew install ffmpeg`— y **VLC** si quieres el reproductor de dentro; cuando falta alguno, Ondine
  lo dice con la orden exacta para **tu** sistema en vez de fallar en seco. En un Mac, la primera
  vez hay que abrirla con el botón derecho → Abrir: la app no está firmada con un certificado de
  Apple, que es de pago y anual.

  Para quien usa Ondine en Windows **no cambia nada**: la versión de siempre sigue siendo la que se
  instala, y las dos conviven mientras la nueva se rueda en Linux y macOS.


## [1.13.0] - 2026-08-21

### Añadido

- **Organizar reconoce muchas más carpetas de temporada.** Antes el nombre tenía que ser *solo* la
  temporada: «Season 3» sí, «Season 3 (2011)» no. En una biblioteca real los añadidos son la norma
  —el año, la resolución, «Completa»— y cada carpeta no reconocida dejaba sus capítulos sin
  temporada. Ahora «Season 03 - Complete», «Temporada 2 [1080p]» o «S02 - 720p» se entienden bien.

  Y **«Especiales» pasa a ser la temporada 0**, que es como la llaman Plex y Jellyfin; antes esos
  capítulos se quedaban sin temporada. En la tabla siguen apareciendo tras las temporadas normales.

  Lo que **no** ha cambiado: una carpeta como «Los 4 Fantásticos» sigue sin ser la temporada 4. Un
  falso positivo es peor que no detectar nada, porque manda los capítulos a otro sitio con toda la
  confianza.

- **Ya puedes bajar el audio 5.1 a estéreo.** Un ajuste nuevo, «Canales». Una pista 5.1 pesa como el
  doble y no aporta nada en unos auriculares ni en la tele del cuarto: bajarla es lo que convierte
  una película de 8 GB en una de 5 **sin tocar el vídeo**. Nunca va al revés, y pedir estéreo sobre
  algo que ya lo es no hace nada.

  Dos cosas se arreglan solas: mezclar obliga a recodificar la pista —no se puede copiar y mezclar a
  la vez, y ffmpeg no avisaría: copiaría y se saltaría la mezcla— y **el bitrate sigue a los canales
  con los que queda**, porque mantener el del 5.1 en una estéreo desperdiciaría media pista.

- **Ya puedes elegir el códec de audio: AAC, AC3, E-AC3, Opus o FLAC.** Antes solo había dos
  caminos enterrados en el motor —copiar, o pasarlo a AAC— y ninguno se podía pedir. Ahora AC3 está
  a un clic si tiene que entenderlo el receptor del salón, y Opus si quieres la mitad de peso con la
  misma calidad. **«Sin tocar» sigue siendo lo que viene puesto**, que es copiar los bytes tal cual.

  Y si lo que eliges no cabe en el formato —WebM solo admite Opus, MP4 no guarda FLAC de forma que
  lo reproduzca nada— **se cambia y se dice en el registro**, en vez de cambiarlo en silencio y
  dejarte creyendo que tienes AC3 cuando tienes AAC.

- **Ya puedes decidir cuánto se esmera el codificador.** Un ajuste nuevo, «Esmero», con cinco pasos
  de «lo más rápido» a «lo más lento». Más lento significa más pequeño con la misma calidad — ese es
  todo el intercambio. **«Equilibrado» es lo que la app hacía hasta ahora**, así que si no lo tocas
  todo sigue igual.

  Cada familia de códecs cuenta esto de una forma distinta —y dos de ellas cuentan al revés— así que
  el valor que toca al codificador que acabe usándose se elige solo.

- **Ya puedes decir cuánto quieres que pese, en vez de elegir calidad y ver qué sale.** En «Calidad»
  hay una opción nueva, «Tamaño objetivo…», donde escribes los MB a los que debe quedar cada
  fichero. El bitrate se calcula **por fichero**: un clip corto y una película necesitan bitrates
  muy distintos para pesar lo mismo, así que un único número para todo el lote no serviría.

  Y si el tamaño que pides no da, **se te dice antes de codificar** —con el tamaño mínimo que sí
  valdría— en vez de entregarte un vídeo ilegible que técnicamente pesa lo pedido. Si ni el audio
  cabe, también se dice, porque ahí bajar la calidad del vídeo no arreglaría nada.

  El resultado es aproximado: es una pasada, no dos.


## [1.12.0] - 2026-08-21

### Añadido

- **Cola de trabajos: ya puedes comprimir varias cosas con ajustes distintos sin esperar.** Hasta
  ahora todos los ficheros de una tanda compartían las mismas opciones, así que para pasar una serie
  a H.265 y una película a AV1 había que esperar a que acabase la primera. Con «**Añadir a la
  cola**», los ficheros marcados se apartan como un trabajo **con los ajustes tal y como están en
  ese momento**; luego cambias lo que quieras y encolas otro. Cada trabajo conserva los suyos, así
  que no te cambian por debajo.

  La cola arranca sola si no había nada en marcha y va despachando uno tras otro. Los que esperan se
  pueden subir, bajar o sacar; el que está en curso no se mueve —ya está escribiendo en el disco— y
  «Detener» para la cola entera. Si encolas un fichero que ya estaba esperando, se avisa antes: el
  segundo trabajo leería algo que el primero puede haber mandado a la papelera.

- **Recortes ya puede cortar sin recodificar.** Marcando «Cortar sin recodificar», los trozos salen
  copiando los paquetes tal cual: **no se pierde nada de calidad** y tarda segundos en vez de
  minutos, en lugar de volver a codificar el vídeo entero. Es lo que Organizar ya hacía al partir un
  capítulo en sus historias, y a Recortes le faltaba.

  A cambio, el corte **solo puede caer en un fotograma clave**, así que el arranque retrocede al más
  cercano. La app **te dice cuánto se va a mover antes de cortar** —«el primer tramo arranca 0,4 s
  antes»— en vez de que lo descubras mirando el fichero. Y como copiando no se aplican, los ajustes
  de formato, códec, calidad, resolución y audio se apagan solos mientras está marcado: dejarlos
  encendidos haría creer que hacen algo.

## [1.11.0] - 2026-08-21

### Añadido

- **Cuando dos películas encajan igual de bien, ahora puedes elegir tú — y se recuerda.** La app se
  planta a propósito cuando no lo tiene claro (dos «Psicosis», una de 1960 y otra de 1998, y el
  fichero sin año), porque una película mal identificada es peor que una sin identificar. Pero si no
  podías resolverlo tú, se plantaba **para siempre**. Ahora la fila se despliega con las candidatas,
  eliges, y esa decisión **manda sobre lo que la app habría deducido** y no hay que volver a tomarla
  en cada análisis. La elección **viaja con el fichero** cuando lo renombras o lo mueves, y se puede
  deshacer.

### Corregido

- **Marcar dos veces la misma historia duplicaba el nombre del fichero.** Apuntar que un fichero trae
  también la historia de otro episodio no comprobaba nada: se podía apuntar dos veces la misma, o
  apuntar la que ya era la historia propia del fichero. El resultado era un nombre con todo repetido
  —y tan largo que se cortaba a media palabra—. Ahora una historia ya apuntada no se vuelve a añadir,
  y la propia del fichero tampoco.

## [1.10.0] - 2026-08-20

### Añadido

- **Los vídeos que Windows no sabe reproducir ahora se pueden ver por fotogramas.** El anime en AV1,
  y también HEVC o VP9, se quedaban en pantalla negra dentro de Ondine. Ahora, cuando el reproductor
  de Windows no puede con el vídeo, los fotogramas los saca **ffmpeg** —que sí sabe, y ya venía en la
  app— y se recorren con la misma barra de siempre. No hay sonido ni reproducción seguida, y se dice;
  pero sirve para lo que existe ese reproductor: saber qué capítulo es. Lo mismo en **Recortes**, que
  ya no se queda sin previa: ahí **cortar siempre funcionó**, porque de eso se encarga ffmpeg.

- **Las películas se pueden identificar contra TMDb, y la app dice por qué se lo cree.** Hasta ahora
  los nombres salían solo del fichero y de su carpeta, así que un título mal escrito seguía mal
  escrito. Encendiéndolo en **Preferencias → Películas**, la ventana de películas trae un botón que
  pregunta a The Movie Database qué es cada una: acierta el título, **trae el año cuando el fichero
  no lo tiene** —de una biblioteca de 75, 52 no lo traían— y sabe que «The commuter» y «El pasajero»
  son la misma película. Cada fila enseña qué se encontró **y por qué señal**, y lo que no se pueda
  identificar con confianza se enseña y **no se toca**: dos películas que encajan igual de bien y
  nada que las separe no se resuelven a cara o cruz, porque una película mal identificada es peor
  que una sin identificar.

  **Viene encendido y con la clave puesta**, así que no hay que registrarse en ningún sitio ni ir a
  buscar el ajuste. Aun así nunca se pregunta nada sin que pulses: identificar es un botón, la
  pantalla dice que las va a buscar en TMDb, y se puede apagar en **Preferencias → Películas**. Ahí
  hay además un campo para tu propia clave, con los pasos para sacarla y un botón que abre la página
  de TMDb, si prefieres gastar tu cuota. **De tu equipo solo sale el título ya limpio y el año**
  —nunca el nombre del fichero, que dice de dónde salió—, y las respuestas se guardan en disco: la
  misma película no se pregunta dos veces y lo ya preguntado sigue valiendo sin conexión.

### Cambiado

- **El repaso de películas pasa a la pantalla principal, con la forma del de series.** Antes era un
  diálogo aparte con una lista simple, y lo que le faltaba no era información: era poder **decidir
  fila a fila**. Aplicar era «las once o ninguna». Ahora cada película lleva **su casilla** —y solo
  la llevan las que de verdad se pueden aplicar—, arriba están los mismos chips con recuento que
  filtran la tabla (se colocan · se renombran · hay que mirar · ya están bien), y hay una columna
  **«Por qué»** que dice el motivo en palabras en vez de solo un color. El aviso de lo que va a
  costar el movimiento —cruzar de disco, entrar en una nube— cuenta solo lo que tengas marcado.

### Corregido

- **Ondine seguía sin saber leer una de las dos formas en que ella misma escribe un fichero con
  varios episodios.** Se arregló la forma con corchetes —`[1262+1264]`—, pero la otra
  plantilla pega el añadido **al número**: `S2004E9042f+9044`. De esos solo veía el
  primero, así que
  «Qué falta» pedía un episodio que estaba dentro de ese mismo fichero, con su nombre delante. Ahora
  entiende las dos.

- **«Ordenar por temporadas» ofrecía mover ficheros que ya no estaban ahí, y fallaba sin decir por qué.**
  Si aplicabas un renombrado y abrías la ventana de temporadas sin volver a analizar, la lista seguía
  siendo la de antes: sus rutas ya no existían. El resultado era «0 movidos · 6 no se pudieron», sin
  una palabra sobre el motivo. Ahora esas filas se marcan como **«ya no está ahí: vuelve a analizar la
  carpeta»** y no cuentan para el botón — «Mover 6» sobre seis fantasmas era una promesa falsa.

- **La app se comía un 12% de un núcleo estando parada, sin hacer nada.** Dos animaciones decorativas
  que no paraban nunca: el latido del punto de «comprimiendo» —que corría **aunque la píldora
  estuviera oculta**, que es casi siempre— y la respiración del fondo, que al cambiar de opacidad
  obliga a repintar la ventana entera. Medido apagándolas una a una: **12,2% → 0,2%**. El latido
  ahora solo late cuando se le ve; el fondo hace su pasada al abrir y se queda quieto.

- **Cuando un vídeo no se puede reproducir, ahora se dice qué códec es y qué hacer.** Antes salía
  «códec no soportado» y un número en hexadecimal, que no le dice a nadie qué hacer. Ahora se le
  pregunta al analizador y se te dice cuál es —**AV1**, **HEVC**, **VP9** o el que sea— y se te manda
  al reproductor del sistema, que es lo que de verdad lo abre. Ojo con esto, porque es contra
  intuitivo: **instalar la extensión de vídeo de la Microsoft Store no hace que se vea dentro de
  Ondine**. El reproductor de dentro va sobre la tubería clásica de Windows y esas extensiones son
  para otra, así que se menciona solo por si tu reproductor del sistema tampoco lo abre.

- **En el reproductor, pinchar en la barra te llevaba unos segundos antes de donde habías pinchado.**
  El globo que enseña la hora al pasar el cursor medía a ojo —posición partido por ancho— y la barra
  se coloca con otra cuenta: su recorrido no es todo el ancho, empieza y acaba a medio tirador de los
  bordes. Dos cuentas distintas para el mismo punto. En el centro coincidían, que es por lo que el
  fallo costaba de creer.

- **Un vídeo que todavía se estaba bajando de la nube decía «códec no soportado».** Falla igual que
  uno cuyo códec no está en el sistema, y el aviso mandaba a buscar un problema que no existía. Ahora
  se distingue, lo dice, y **empieza solo en cuanto termina de bajar**.

- **Al elegir carpeta aparecía un momento la pantalla de la otra biblioteca.** Se recolocaba el tipo
  de biblioteca y se repintaba sin salir antes del repaso, así que se pintaba el repaso de la
  biblioteca recién elegida con la tabla anterior o vacía. Duraba lo que tardase el recorrido del
  disco: nada en una carpeta local, un rato en una de OneDrive.

- **La app se quedaba lenta después de cerrar el reproductor.** Y no era una impresión: con el
  reproductor cerrado y sin hacer nada, Ondine seguía gastando **un 11% de un núcleo, para siempre**.
  El péndulo de carga anima en bucle infinito, y el vídeo mandaba un aviso de «estoy cargando» justo
  mientras se cerraba la ventana: eso lo volvía a arrancar cuando ya no había ventana que lo parase,
  y una animación viva obliga a repintar en cada fotograma aunque no se vea nada. Ahora nada de lo
  que llegue tarde hace nada. De paso, el reproductor **suelta el fichero** al cerrarse —antes se
  quedaba con él y con el decodificador—, que es lo que permite devolverlo a la nube.

- **En modo «Películas», elegir carpeta volvía a pintar la pantalla de series encima.** Se cerraba el
  explorador de archivos y aparecían el panel de catálogos y el de ficheros de series **superpuestos**
  a la pantalla de películas, las dos a la vez. Volver al estado inicial enseñaba la pantalla de
  series sin mirar de qué era la biblioteca, y elegir carpeta pasa por ahí.
- **Las pestañas de Preferencias no se podían desplazar.** El alto de la ventana era fijo, así que lo
  que no cabía en una pestaña quedaba cortado y fuera de alcance —el botón del final de «Películas»,
  por ejemplo—. Ahora se desplazan.

## [1.9.2] - 2026-08-20

### Corregido

- **El complemento cotejaba contra el primer catálogo que hubieras cargado, no contra el de ahora.**
  El panel se conserva entre aperturas a propósito —para no tirar una lista que costó minutos
  traer—, y con él se guardaba el catálogo del momento en que se abrió por primera vez. Cargabas
  otro catálogo, volvías al panel, y seguía comparando contra el anterior; **y si la primera vez no
  había ninguno puesto, no comparaba nunca y salía todo como que no lo tienes**. Ahora el panel
  **pregunta** qué hay cargado en vez de guardarse una foto, así que eso no puede volver a pasar, y
  al volver al panel se recalculan las etiquetas de la lista que ya estuviera traída. De paso se
  arregla lo mismo en la casilla de fuente, que enseñaba la dirección guardada del catálogo viejo.

### Añadido

- **El error de un complemento se guarda en el Registro.** Antes se pintaba en el panel y se perdía
  al cerrarlo: cuando hizo falta saber por qué había fallado una lectura semanas antes, el texto
  exacto no estaba en ninguna parte. Lo que se ve una vez y no se guarda no se puede diagnosticar
  después.


### Corregido

- **Ondine no sabía leer los nombres que ella misma escribe.** Cuando un fichero junta historias de
  episodios distintos, la app lo nombra `[1262+1264]`. Al releerlo solo veía el **1262**: el
  corchete con «+» no casaba con su propia expresión, y además se borraba antes de mirarlo. La
  consecuencia se veía en el cotejo de una lista y en «qué falta»: decía **«te falta Autobús
  intergaláctico»** de un episodio que estaba dentro del fichero que tenía delante, con el título
  escrito en el nombre. Ahora los episodios añadidos se leen y **cuentan como cubiertos**. El
  cálculo vive en un solo sitio, para que el informe y el cotejo no puedan volver a responder cosas
  distintas.


- **Al elegir «Películas» seguía viéndose media pantalla de series.** Se ocultaban el desplegable
  de serie y la plantilla, pero quedaban a la vista el panel de catálogos, «Partir en segmentos» y
  «Ordenar por temporadas» — cosas que a una película no le aplican, porque no hay catálogo del que
  sacarla ni temporadas que ordenar. Ahora las películas tienen **su propia pantalla**: la carpeta,
  cuántas hay, y una sola acción. Una pantalla llena de huecos enseña a desconfiar de lo que queda,
  y esconder trozos sueltos no es lo mismo que cambiar de flujo.


## [1.9.1] - 2026-08-19

### Añadido

- **Ordenar una carpeta de películas ya se puede hacer.** Antes se podía marcar la carpeta como de
  películas y ahí acababa todo. Ahora, con «Películas» puesto, **Analizar abre su propia ventana**:
  enseña qué cambiaría para que cada una acabe como `Título (Año)/Título (Año).ext` —que es lo que
  esperan Plex y Jellyfin— y **no toca nada hasta que aceptas**, con el mismo trato que el
  reordenado por temporadas. Los subtítulos y las fichas viajan con su vídeo, nunca se sobrescribe
  nada, y se puede deshacer. Avisa igual de lo que va a costar: cambiar de disco, entrar en una
  carpeta sincronizada, o mover ficheros que solo están en la nube.
- **Los extras se reconocen y no se tocan.** Un `-trailer.mp4` o un `-behindthescenes.mp4` junto a
  la película se renombraba a «Título (Año).mp4», y el servidor lo leía como una **segunda versión
  de la película**: el extra desaparecía de donde debía estar. Ahora se quedan quietos, y tampoco
  cuentan para decidir si una carpeta es una colección — una película con tres extras al lado
  seguía siendo una película.
- **Una película partida en dos deja de pelearse consigo misma.** «… cd1» y «… cd2» daban el mismo
  nombre canónico: la primera perdía el «cd1» y la segunda se quedaba sin sitio. Ahora cada mitad
  conserva su parte, escrita como la documenta Plex: «Título (Año) - part1».
- **El año de una reedición ya no se toma por el del estreno.** En «Alien 1979 REMASTERED (2003)»
  el de los paréntesis es el del remaster. Se exige la marca de reedición para preferir el otro:
  sin ella, un número anterior suele ser parte del título —«The 1900 House (1999)»— y preferirlo
  rompería más de lo que arregla.
- **Una carpeta con varias películas dentro no se desmonta.** Se trata como colección: se limpia el
  nombre de cada fichero, que es lo que lee el escáner, y nada sale de su sitio.

### Corregido

- **Un deshacer a medias ya no tira el registro.** Si al deshacer un fichero no podía volver —lo
  normal es que esté abierto en el reproductor—, la ventana escondía el botón y **borraba la única
  lista que existía de qué había ido a dónde**. Ese fichero se quedaba desplazado para siempre, sin
  forma de recuperarlo desde la app, y encima aparecía en rojo como «ese nombre ya está ocupado»
  señalándose a sí mismo. Ahora el registro se conserva, el botón se queda, y se dice cuántos
  faltan: reintentar es seguro porque lo que ya volvió se salta solo. Afecta a las dos ventanas que
  mueven ficheros.
- **Un subtítulo que se queda atrás ya no se calla.** Cuando el nombre nuevo de un compañero ya
  estaba ocupado, se saltaba sin contarlo en ningún sitio: el vídeo llegaba a su carpeta, la
  ventana decía «hechas, 0 fallos», y el subtítulo se quedaba en otro sitio. No estaba borrado,
  pero para Plex o Jellyfin había dejado de existir. Ahora se cuentan y se dicen.

- **Un vídeo podía viajar como si fuera el subtítulo de otro, y acabar en la carpeta de otra
  película.** Al mover un fichero se llevan con él sus compañeros —el `.srt`, la ficha—, y para
  decidir quién era compañero se miraba solo el nombre: cualquier cosa que empezara por
  «`<nombre>.`» se movía detrás. Hay nombres muy normales donde el nombre base de un vídeo es
  prefijo del de otro —«Up.mkv» y «Up.2009.mkv» en la misma carpeta— y ahí el segundo viajaba como
  si fuera un subtítulo. Lo grave no es que se moviera: es que se movía **a espaldas del plan**, de
  modo que una fila marcada «no lo toco» acababa movida igualmente, y el pie de la ventana promete
  que nada se ha tocado. Ahora un compañero **nunca** puede ser un vídeo. Afecta también al
  reordenado por temporadas, que comparte el mismo motor.

### Cambiado

- **Mover ficheros vive ahora en un solo sitio.** El reordenado por temporadas y el de películas
  comparten el mismo motor. Lo delicado no es mover un fichero: es no pisar nada, llevarse los
  compañeros y poder volver atrás — y de eso no puede haber dos copias, porque divergen y la que se
  quede corta lo hará sobre la biblioteca de alguien.


## [1.9.0] - 2026-08-19

### Añadido

- **Organizar ya sabe que una carpeta puede ser de películas.** Hasta ahora todo asumía episodios:
  la identificación entera gira alrededor de «número + temporada + título del catálogo», y a una
  película no le aplica nada de eso. Ahora se elige **qué hay en la carpeta** —serie o películas—,
  se recuerda por carpeta, y lo que no aplica desaparece en vez de quedarse en gris: sin catálogo
  y sin plantilla de episodios, porque la respuesta a «qué catálogo pongo» es **ninguno**. Para las
  películas el nombre sale del propio fichero —título y año— y el destino es
  `Título (Año)/Título (Año).ext`, que es lo que esperan Plex y Jellyfin; la carpeta propia es lo
  que les deja meter dentro carátula y subtítulos sin confundirlos con los de la película de al
  lado. Leer el año tiene más trampa de la que parece, porque **hay títulos que son un año**: manda
  el que va entre paréntesis, uno al principio del nombre es título —«1917», «2001 A Space
  Odyssey»— y uno que aún no ha pasado tampoco es un estreno, que es lo único que salva a «Blade
  Runner 2049» cuando viene sin paréntesis. **Una carpeta con varias películas dentro no se desmonta.** Medido sobre una biblioteca real de
  75 películas: **53 vivían en carpetas de colección** —«Disney» con 26, «Bob Esponja» con 10,
  «Paco Martínez Soria» con 8—. La convención de Plex es una carpeta por película, así que
  desmontarlas sería lo «correcto» para el escáner y destruiría la forma en que su dueño mira su
  biblioteca. Dentro de una colección se limpia el nombre del fichero, que es lo que el escáner
  lee, y nada sale de su sitio. Sobre esa misma biblioteca: 64 de 75 no se tocan y solo 6 cambian
  de carpeta. **Identificarlas contra una base de datos pública
  todavía no está**: por ahora, marcar la carpeta como de películas y darle a analizar lo dice y se
  para, en vez de no hacer nada en silencio.

- **Ordenar por temporadas avisa de lo que va a costar antes de que pulses.** Un movimiento se
  ve siempre igual —una flecha de una carpeta a otra— y por debajo puede ser gratis o puede ser
  una tarde. Dentro del mismo disco mover es reetiquetar; **a otro disco es copiar entero y
  borrar**. Y si el destino cae dentro de una carpeta sincronizada donde el fichero no estaba,
  se vuelve a subir todo, cosa que no se ve en esta pantalla sino en la barra de tareas, durante
  horas. El tercer caso es el peor y el menos evidente: los ficheros que **solo están en la
  nube** —los que ocupan cero y se descargan al abrirlos— se bajan enteros al moverlos; medido
  en esta misma casa, un marcador de 277 MB tardó 18 segundos. Ahora los tres se cuentan y se
  dicen arriba, con cuántos ficheros afecta cada uno. **No bloquea nada**: hay bibliotecas que
  viven en la nube a propósito y reordenarlas es legítimo; lo que no lo es, es enterarse después.
  Moverse dentro de la misma nube no avisa, porque ahí el cliente mueve una referencia y no sube
  nada — un aviso que salta siempre es un aviso que se deja de leer.

- **La Ayuda ya cuenta «Ordenar por temporadas».** La ventana llegó en la 1.7.0 y se quedó sin
  explicar: qué mueve, qué **no** mueve y por qué, y por qué el nombre de la carpeta se elige a
  mano en vez de seguir al idioma de la app.

### Corregido

- **La protección contra las carpetas numeradas por otro se apagaba sola según ibas
  arreglándolas.** Para decidir si los números de una carpeta son los del catálogo se miran los
  ficheros identificados por su título — pero uno **ya renombrado** trae el título del episodio
  en su propio nombre y su número lo escribió Ondine a partir del catálogo, así que vota
  «cuadra» por construcción. Cuantos más arreglabas, más votos falsos: en una carpeta a mitad
  de arreglar la regla se cayó por 1,3 votos y volvieron las propuestas inventadas — trece
  ficheros con un episodio propuesto que ni siquiera existía en el catálogo. Los ya renombrados
  ya no votan.

## [1.8.0] - 2026-08-08

### Añadido

- **Un fichero que solo está en la nube se puede ver en su web, sin bajárselo.** El aviso ofrecía
  «enséñame el fichero», que abre el explorador — pero eso no contesta la pregunta que lo motiva,
  que es de qué episodio se trata, y desde ahí aún quedaban dos pasos. Ahora, cuando la nube
  ofrece verlo en línea, el aviso lleva **directo a su web**. Sin credenciales y sin saber de
  ningún proveedor: Ondine invoca la misma opción que pulsarías tú en el menú de Windows, así que
  vale para OneDrive y para cualquier otro que la ponga ahí. **También con Nextcloud**, que no
  pone sus opciones en ese menú: a su cliente se le pregunta por el mismo canal que usa su
  propia integración con el explorador. Si el proveedor no ofrece nada, se sigue abriendo el
  explorador.
- **El reloj aprende qué es lo constante en cada serie.** Comparaba siempre contra «lo que dura
  una historia × cuántas trae», y hay series al revés: **el episodio dura siempre lo mismo** y
  dentro caben dos historias o tres según el día. En una carpeta real de Crayon Shin-Chan, con
  ficheros de 24 minutos y episodios de 2 y de 3 mezclados, medir por historia daba 8:03 en unos
  y 13:01 en otros — y con esa vara un episodio de 2 historias salía sospechoso sin serlo. Ahora
  la serie lo dice sola: se compara la estabilidad de las dos lecturas y gana la que menos varíe.
  Con el episodio por molde, el reloj además **deja de opinar sobre cuántas historias trae un
  fichero**, porque ahí la duración no lo distingue.
- **Reproducir un fichero que solo está en la nube ya avisa antes.** Abrirlo se lo bajaba entero
  sin decir nada: Windows recupera el fichero al abrirlo y no hay forma de leer solo un trozo
  —medido, leer **un mega** de uno de 65 MB bloquea más de cinco minutos sin terminar—. Así que
  comprobar de qué episodio es cada uno acababa siendo bajarse la temporada. Ahora se dice el
  tamaño y de qué nube es, y se ofrece la salida. Funciona con **cualquier** proveedor que use la
  API de nube de Windows —OneDrive, Nextcloud y los demás—, porque el nombre y la carpeta se los
  pregunta al propio sistema en vez de llevar una lista.
- **La carpeta se corrobora a sí misma.** Ondine daba confianza alta cuando dos señales
  coincidían —normalmente el título y la fecha—, así que **un catálogo sin fechas dejaba todo
  pidiendo decisión** por bien que casara. Ahora la segunda señal puede ser el propio lote: si
  varios ficheros ordenados por su número apuntan a episodios distintos y en el mismo orden, eso
  los respalda. Y funciona con la carpeta a medio arreglar, donde conviven los ya renombrados y
  los pendientes. Lo que rompe la serie se queda pidiendo mano — que es justo lo que hay que
  mirar.
- **«El nombre solo dice una historia» ya se contesta de una vez.** Cuando el fichero dura lo que
  todas las historias del episodio, lo corto es el nombre y no el contenido, y la respuesta es la
  misma para todos los que estén igual. El aviso se mantiene —renombrarlo entero afirma algo que
  el nombre no decía— pero se puede confirmar en bloque. Medido en una carpeta real de
  Crayon Shin-Chan —59 ficheros contra un catálogo de 1342 episodios **sin una sola fecha**—:
  de **53 decisiones una a una a 25**, porque 26 de las que quedan se contestan de golpe.

### Corregido

- **Cuando la carpeta va numerada por otro, la app deja de proponer el episodio que dice el
  número.** El número del fichero valía como pista cuando no había fecha con la que
  confirmarlo — y eso, en una carpeta numerada por un canal o una lista de reproducción, da
  una propuesta con toda la cara de válida: el número existe en el catálogo, así que sale un
  episodio y un nombre nuevo, y es el que no es. Ahora la carpeta se pregunta a sí misma: de
  los ficheros identificados **por su título**, ¿sus números cuadran con el catálogo? Si no
  cuadran, los que solo se sostenían en el número dejan de proponer nada y lo dicen. Medido en
  una carpeta real de Crayon Shin-Chan: 36 de 42 títulos caían a desfase −30..−40 y los 17 que
  salían del número asumían desfase 0. Se decide en bloque, así que son **un clic, no doce**.
- **El título del metadato también se parte por tandas de espacios.** Dos `.nfo` de la misma
  carpeta y del mismo sitio: el que separaba con `|` acertaba el episodio y el que separaba con
  tres espacios no acertaba ninguno, porque se comparaba el churro entero —nombre de la serie,
  título y «Episodio N en español»— contra el título del catálogo. El título correcto estaba
  leído y en memoria, y la app proponía otro episodio igualmente.
- **El `.nfo` que acompaña al vídeo se lee siempre, no solo cuando la app ya dudaba.** Leerlo
  tarde dejaba fuera justo el caso peligroso: un fichero puede quedar **seguro y equivocado** —
  identificado por su número contra el episodio que no era— y entonces nadie llega a mirar el
  `.nfo` que lo habría desmentido. Ahora informa la identificación desde el principio. El
  `S01E534.mp4` que dio origen a esto pasa de señalar el episodio 534 en ámbar a señalar el 497
  «Kasukabetti Western» en verde, a la primera. El motivo para no hacerlo antes era el coste, y
  medido no existe: **91 ms los 59 ficheros** de una carpeta real, 12 KB en total. De propina,
  la segunda pasada —que abría vídeos uno a uno— deja de hacer falta cuando hay `.nfo`.
- **CRÍTICO: un fichero podía salir en verde apuntando al episodio equivocado.** Cuando el nombre
  no trae título, la app lo identifica por su número; y el respaldo que da la carpeta se calcula
  **también con los números**, así que confirmaba lo que ya había dicho el número — la misma
  señal contada dos veces. Un `S01E534.mp4` que en realidad era el episodio 497 salía en verde
  diciendo «el título coincide al 100 % y el resto de la carpeta lo respalda»: ni había título ni
  había respaldo independiente. Ahora la carpeta solo respalda lo identificado **por el título**,
  que es la única señal distinta de la que forma la serie. Ese fichero pasa a ámbar, la app lee su
  `.nfo` —cosa que solo hace con lo que duda— y acaba en el episodio correcto.

## [1.7.0] - 2026-08-06

### Añadido

- **Los complementos se pueden desinstalar y actualizar desde la propia app.** Antes solo se
  podían instalar, así que quitar uno era ir a borrar su carpeta a mano — justo lo que la tienda
  venía a evitar. Y si salía una versión nueva, lo instalado se quedaba viejo para siempre sin
  que nada lo dijera: ahora el botón pasa a decir *Actualizar a X*.
- **La lista de complementos se relee al volver a abrir el panel.** El panel se conserva entre
  aperturas —para no tirar una lista que costó minutos traer— y con él se conservaba la lista de
  instalados: uno quitado a mano seguía apareciendo hasta reiniciar la app.
- **El complemento de YouTube recuerda la lista de cada catálogo.** Ya no hay que volver a pegar
  el mismo enlace: al abrir el complemento con un catálogo, vuelve puesta la última lista que
  cotejaste con él. Va por catálogo porque la lista de una serie no vale para otra.
- **La tienda de complementos ya sirve algo.** El índice estaba montado pero vacío, así que
  «Disponibles» no enseñaba nada. Ahora trae el complemento de YouTube, y se instala desde la
  propia app sin copiar carpetas a mano. El paquete va con su `sha256`: si lo descargado no es
  exactamente lo que el índice promete, no se instala.
- **Confirmar de una todos los especiales que la app da por seguros.** Un especial nace pidiendo
  confirmación a propósito, pero cuando dieciséis casan sin margen de duda contra dieciséis
  entradas distintas, contestar dieciséis veces no es revisar. Los que casaron flojo se quedan
  fuera y siguen pidiendo mano: esos son justo los que hay que mirar.
- **El título entero al pasar por encima**, en la lista de un complemento. Con el panel estrecho
  se recortaba, y ahí se pierde lo único que identifica el vídeo.
- **La duración de cada vídeo, en Organizar y en el reproductor.** Sale también en los ficheros
  que tienes en la nube sin descargar: se lee de la ficha que Windows ya guarda, así que verla
  no obliga a bajarlos.
- **Ondine avisa cuando un nombre promete más historias de las que caben en el vídeo.** Aprende
  cuánto dura una historia en esa serie —la mediana, con al menos cinco ejemplos— y compara. Un
  fichero de once minutos al que se le iba a poner el nombre de dos episodios deja de darse por
  bueno. Los especiales largos no se marcan.
- **Complementos.** Ondine puede ampliarse con programas de fuera que traen o consultan
  material. Se instalan copiando una carpeta y viven en un **panel lateral** que se abre junto a
  lo que estés mirando —se estira, se encoge y se cierra— en vez de taparlo con otra ventana:
  la lista se coteja contra el catálogo que tienes delante, así que esconderlo no tenía sentido.
  Cada complemento declara dónde aplica —toda la app, o solo Organizar, Comprimir o Recortes— y
  los que no valen aparecen igualmente con el motivo, en vez de desaparecer sin explicación.
  Cómo escribir uno: [`docs/complementos.md`](docs/complementos.md).
- **Un complemento de YouTube que dice qué te falta de una lista.** Lee una lista de
  reproducción pública y la compara con tu catálogo, episodio por episodio: ya lo tienes, te
  falta, o te falta una de sus dos historias. Y **encuentra la segunda historia que el título se
  calla**: muchos vídeos se titulan con un episodio y en realidad traen dos, con el segundo solo
  en la descripción. Sin eso, media cinta se daba por completa y la historia que faltaba no la
  reclamaba nadie. No descarga: lee.
- **El menú superior se recoge en una hamburguesa** cuando la ventana no da para los cuatro.
- **Decidir de una para todas las filas atascadas por lo mismo.** Cuando varias filas piden
  decisión exactamente por la misma causa, el resolutor ofrece *«Dejar igual las otras N»*. En una
  biblioteca de 1411 ficheros, 16 de las 27 filas que pedían mano eran la misma cosa dicha
  dieciséis veces —especiales que ese catálogo no contempla—: ahora es un clic. Solo se ofrece
  cuando esa causa tiene **una única respuesta buena para todas**; con dos ficheros peleando por
  el mismo episodio no aparece, porque ahí cada pareja tiene su propio ganador.
- **Un complemento puede pedirle ayuda al modelo, con tu permiso y sin ver tu clave.** El permiso
  se da en la ficha del complemento, uno a uno, y empieza apagado. El complemento **nunca recibe
  la clave ni la dirección**: él pregunta, Ondine llama, y solo le vuelve la respuesta. Hay cupo
  —40 preguntas por ejecución— porque esto cuesta dinero de verdad. El complemento de YouTube ya
  lo usa: solo para los vídeos en los que la descripción promete más historias y sus reglas no lo
  pueden confirmar, unas pocas de una lista de cientos. Y lo que conteste se coteja con tu
  catálogo como todo lo demás, así que un título inventado sale como que falta, no como bueno.
- **Se puede conectar un modelo de lenguaje, y es opcional.** Está en *Preferencias › Modelo*:
  dirección, clave y nombre del modelo, con un botón para probar la conexión antes de guardar.
  Usa el estándar de OpenAI, que es el que hablan casi todos —OpenAI, Groq, OpenRouter, LM Studio
  y Ollama—, así que vale igual uno de pago que uno corriendo en tu propio ordenador. La clave
  **se guarda cifrada** con la protección de datos de Windows y atada a tu cuenta: nunca se
  escribe en claro, y no se manda por `http://` a una máquina que no sea la tuya. Ondine funciona
  entera sin esto; quien no lo configure no nota que existe.
- **En «Explorar el catálogo», cada episodio dice si lo tienes.** Un distintivo por fila —*lo
  tienes*, *a medias*, *te falta*— con los mismos colores del semáforo de Organizar, y **pulsarlo
  te lleva al fichero** en el explorador de Windows. Hay además una casilla para quedarte solo
  con los que faltan, que se combina con el buscador. Si abres el catálogo sin haber analizado
  ninguna carpeta, no aparece nada: sin haber mirado un disco, decir «te falta» sería inventarlo.
- **Ordenar por temporadas.** Un capítulo que descargaste donde cayó se va a la carpeta de su
  temporada, y la carpeta se crea si no existe. Antes de mover se enseña la simulación entera:
  qué se movería y, sobre todo, qué no y por qué. **Solo se mueve lo ya curado** — un fichero en
  conflicto no se sabe de qué temporada es, así que se queda donde está. Los subtítulos y las
  fichas viajan con su vídeo, nunca se sobrescribe nada, y se puede deshacer. El nombre de la
  carpeta lo eliges tú: *Temporada 03* o *Season 03*, siga la app el idioma que siga —hay quien
  la usa en castellano y mantiene la biblioteca en inglés porque es lo que espera el reproductor.

### Eliminado

- **El complemento de demostración.** Existía para probar la pantalla cuando no había ninguno de
  verdad. Ya hay uno, y el de YouTube sirve mejor de ejemplo porque hace todo lo que el contrato
  describe en vez de fingirlo.

### Corregido

- **El cotejo decía «ya lo tienes» de vídeos que traen dos episodios y solo tienes uno.** Un vídeo
  de una lista puede juntar dos entradas del catálogo —«El controlador del mar + Alquiler estilo
  futurista» son los episodios 985 y 1237—, y solo se miraba la que mejor casaba. La otra no
  aparecía en ninguna cuenta: ni entre lo que tienes ni entre lo que falta. Ahora se pregunta por
  cada cosa que trae el vídeo, en su episodio.
- **Un fichero cuyo nombre solo dice una de las historias del episodio ya no se da por completo.**
  La regla «sin letra de segmento, tapa el episodio entero» sigue siendo el respaldo, pero si el
  nombre nombra solo una de las dos, tapa solo esa. La cuenta vive ahora en un sitio y no en tres.
- **La tarjeta del resolutor decía un nombre y se escribía otro** cuando el fichero juntaba dos
  episodios: enseñaba «quedaría como …E1260 - El invento para hacer bonsáis» mientras el
  renombrado ponía «…E1260+1261 - … + La rueda auxiliar invisible». El nombre se componía en dos
  sitios y uno se olvidaba de las historias añadidas. El renombrado siempre fue el correcto.
- **El distintivo de la lista de complementos era una píldora entera** y desentonaba con el resto
  del panel.
- **El aviso de duración ya no marca capítulos normales.** Aprendía cuánto dura una historia con
  UNA sola medida para toda la serie, y las series largas cambian de formato: en Doraemon (1979)
  una historia dura ~6:12 en 1979, ~12:12 en 1986 y ~23:35 en 1991. Con una vara única se medía
  1986 con la de 1979 y salían en ámbar decenas de capítulos que eran perfectamente normales para
  su año. Ahora cada temporada se mide con la suya, y la global solo se usa donde no hay muestras
  suficientes. El aviso sigue saltando donde importa: un fichero del doble de largo que su año se
  marca igual.
- **«Analizar» ya no descarga tu biblioteca de la nube sin avisar.** Leía las pistas de cada
  vídeo con ffprobe, y abrir un fichero que solo está en OneDrive obliga a Windows a
  **descargarlo entero**. Sobre una carpeta en la nube, pulsar Analizar se convertía en bajar
  decenas de gigas sin haberlo pedido. Ahora esos ficheros se saltan, se dicen cuántos son y por
  qué, y se explica cómo incluirlos si los quieres comprimir.
- **«Analizar» dice por dónde va, y se puede parar.** Antes ponía «Detectando…» una vez y no se
  volvía a mover en más de mil ficheros: una pantalla quieta durante minutos no se distingue de
  una colgada. Ahora cuenta por cuál va, el botón pasa a *Detener*, y un segundo clic ya no
  apila otra tanda encima de la primera.
- **La tabla de Comprimir vuelve a ir suave.** Un ajuste del tema anulaba la virtualización sin
  querer: con mil filas, la lista se construía entera y volvía a medirse en cada scroll. Se
  conserva el desplazamiento suave por píxel, que era lo que ese ajuste buscaba. Arregla de paso
  la vista previa del renombrado, que usa el mismo estilo.
- **Dejar varias filas como están ya no reescribe el catálogo una vez por fila.** Medido sobre un
  catálogo real: un grupo de 16 pasaba de **539 ms de ventana muerta a 25 ms**. Y al escribirlo
  ya no se escapan los acentos, que dejaban el fichero incómodo de leer.
- **El escaneo de la carpeta ya no congela la ventana.** Se hace fuera del hilo de la interfaz y
  se lee el tamaño de cada fichero de la propia enumeración, en vez de preguntarlo otra vez uno
  a uno.

## [1.6.1] - 2026-08-05

### Añadido

- **Ya se puede elegir el idioma.** Está en *Herramientas › Preferencias › General*, el primer
  ajuste de la pestaña. Tres opciones: *El del sistema*, *English* y *Español*. Se aplica al
  momento, sin reiniciar, y se recuerda. Si no eliges nada manda el idioma de Windows, así que
  con Windows en castellano la app se abre en castellano. La 1.6.0 tradujo los textos pero se
  dejó esto fuera: no había ningún sitio donde cambiarlo y siempre arrancaba en inglés.

### Corregido

- **Guardar Preferencias borraba el historial de renombrado.** También el factor de complejidad
  que la app aprende midiendo tus vídeos. Se rehacían los ajustes desde cero cada vez que
  pulsabas *Guardar*, así que se perdía todo lo que no sale en esa ventana.
- **El preset por defecto desaparecía al cambiar de idioma.** Los presets de fábrica se guardan
  por su nombre, y su nombre está traducido: el que elegiste en castellano no existía con la app
  en inglés, y el desplegable se quedaba vacío como si lo hubieras borrado. Ahora se reconoce en
  los dos idiomas.
- **«Previsualizar desde 0:00» se quedaba en el idioma de arranque.** Ese rótulo se escribe al
  abrir la ventana en vez de venir enlazado, así que no se enteraba del cambio de idioma hasta
  que movías el deslizador.
- **La pestaña *General* de Preferencias no cabía entera.** La última casilla, la del menú del
  Explorador, quedaba cortada por debajo del borde.

## [1.6.0] - 2026-08-04

### Añadido

- **Ondine habla inglés.** Toda la interfaz está en inglés y en castellano, y los textos se
  rehacen solos en cuanto cambia el idioma, sin reiniciar. Son 1152 textos, y están todos: los
  rótulos, los mensajes del panel *Registro*, los avisos, los globos de ayuda y el encargo que
  se le pasa a la IA para construir un catálogo.

### Corregido

- **La explicación de «demasiadas dudas» no salía con la app en inglés.** Al decidir qué
  explicación mostrar, la app comparaba un aviso contra un trozo de texto escrito en castellano.
  Con la interfaz en otro idioma esa comparación nunca acertaba, y siempre acababa enseñando la
  explicación genérica en vez de la que tocaba.
- **Los desplegables de *Recortes* se quedaban en el idioma de arranque.** Formato, calidad,
  resolución y audio se rellenaban una sola vez al abrir la pestaña, así que al cambiar de idioma
  seguían como estaban. Ahora se rellenan de nuevo, respetando lo que tuvieras elegido.
- **El mismo ajuste salía en dos idiomas según la pestaña.** Los desplegables de *Recortes* y los
  de *Comprimir* son los mismos, pero cada uno tenía su lista de textos por separado; ahora
  comparten una sola.

## [1.5.0] - 2026-07-30

La app cambia de nombre: **ShrinkStudio pasa a llamarse Ondine**. Al actualizar no tienes que hacer
nada — tus catálogos, decisiones, presets y ajustes se mudan solos.

### Cambiado

- **Logotipo nuevo.** El anterior era un cuadrado con degradado, un botón de *play* y dos flechas
  apretándolo: decía «compresor de vídeo», que es justo lo que la app ya no es. El nuevo son tres
  trazos que van de onda a recta —lo que entra revuelto sale ordenado— sin recuadro ni fondo. Se
  ve en el icono de la aplicación, en la barra de tareas y en la barra de título. A tamaño de
  bandeja lleva un dibujo aparte, hecho a medida para que se distinga a 16 píxeles.
- **La app pasa a llamarse Ondine.** ShrinkStudio nació como relevo de HandBrake, y ese nombre
  contaba solo un tercio de lo que hace hoy: además de comprimir, ordena bibliotecas enteras
  contra un catálogo y parte episodios en sus historias. Lo que de verdad hace es **preparar tu
  biblioteca antes de que Plex o Jellyfin la escaneen**, y el nombre nuevo deja sitio para eso.
  No tienes que hacer nada: al actualizar se conservan tus catálogos, decisiones, presets y
  ajustes — se mudan solos de `%AppData%\ShrinkStudio` a `%AppData%\Ondine` la primera vez que
  abres la app. El acceso directo, el menú del botón derecho y las actualizaciones automáticas
  siguen funcionando igual.

## [1.4.0] - 2026-07-30

Despachar un fichero raro una vez y que la app se acuerde.

### Añadido

- **«Dejarlo como está» ya no se olvida: queda apuntado en el catálogo.** En casi toda carpeta hay
  ficheros que no están en la lista de episodios — capítulos especiales que no salen en ningún
  anexo, cortos, presentaciones. Como no hay contra qué casarlos, salían como conflicto **en cada
  análisis**, y despacharlos con «Dejarlo como está» solo valía hasta el siguiente. Ahora esa
  decisión se apunta en el propio **JSON del catálogo** y al reanalizar la fila sale en verde y sin
  propuesta. El fichero no se toca, y **sigue en la lista**: quitarlo daría a entender que ya no
  está en la carpeta. Va en el catálogo y no en los ajustes de la app a propósito, para que la
  decisión **viaje con él** si te lo llevas a otro equipo o se lo pasas a alguien; y al escribirlo
  se respeta el resto del JSON, incluidos los campos y las notas que hayas añadido por tu cuenta.
  Para deshacerlo, basta con quitar la línea de la lista `dejar_como_esta`.

## [1.3.0] - 2026-07-26

Adelgazar un vídeo **sin recomprimirlo**: quitarle los doblajes y los subtítulos que no vas a usar.

### Añadido

- **Quitar pistas de audio o subtítulos sin comprimir de nuevo.** Un fichero con varios doblajes
  puede llevar decenas de megas de audio que no vas a escuchar. Con el botón derecho sobre un vídeo
  en Comprimir, «Quitar pistas de audio o subtítulos…» enseña todo lo que trae dentro y te deja
  marcar lo que no quieras. **No se recomprime nada**: el fichero se reempaqueta copiando los datos
  tal cual, así que tarda un segundo y el vídeo queda **idéntico bit a bit** — medido sobre un
  episodio real, 155 MB → 134 MB en 0,6 s con el mismo vídeo exacto. Antes de tocar nada se dice
  cuánto se va a ahorrar; la pista de vídeo no se puede quitar, y si el resultado se quedaría sin
  audio se avisa. El original va a la Papelera, así que **Ctrl+Z** lo devuelve.

- **Las pistas se leen con el idioma en claro.** Cada una se describe por lo que es —«Audio ·
  Español · 2 canales · 129 kbps»— en vez de con el código del fichero (`spa`, `eng`, `por`), que
  no sirve para decidir qué doblaje sobra. Si la pista trae un título puesto a mano («Castellano
  AMZN») se enseña, igual que si es la **predeterminada** o si los subtítulos son **forzados** —lo
  único que distingue dos pistas del mismo idioma—. Y lo que no aporta ya no aparece: el idioma sin
  declarar del vídeo, o el caudal de un subtítulo de texto, que no llega a 1 kbps.

## [1.2.0] - 2026-07-25

Saber **qué te falta**. Hasta ahora Organizar decía qué tenías; ahora también lo que no, contándolo
por historias para que no se cuele un capítulo a medias haciéndose pasar por completo.

### Añadido

- **«Qué falta»: la lista de lo que no tienes.** El catálogo ya sabe qué episodios existen y la app
  cuáles has identificado, así que la resta estaba ahí sin aprovechar. Un botón nuevo en Organizar
  compara las dos cosas y lista lo que no está. Lo cuenta **por historias, no por capítulos**: si un
  capítulo trae tres mini-historias y solo tienes dos, sale como **«a medias»** — algo que mirando
  la carpeta parece completo y no lo está. Por defecto solo mira las temporadas que has empezado
  (listar las que ni has tocado no informa de nada), se puede pedir que incluya las demás, y la
  lista se copia al portapapeles. Los especiales se cuentan aparte.
  Se puede **mirar una temporada concreta** —que es lo normal cuando estás completando una— y el
  desplegable dice «Temporada» o «Año» según cómo las numere tu catálogo, porque hay series que van
  por año de emisión y ahí «Temporada 2005» no se entiende.

## [1.1.0] - 2026-07-24

Versión centrada en **Organizar**: separar los capítulos que traen varias mini-historias, decidir a
mano lo que la app dio por bueno, y deshacer cualquier borrado con Ctrl+Z.

### Añadido

- **Cambiar el episodio de cualquier fila, y repartir sus mini-historias.** Antes solo se podía
  corregir una fila cuando la app dudaba; ahora, con el botón derecho sobre **cualquiera** —también
  las que están en verde— tienes «Elegir episodio o historias…». Si el episodio trae varias
  historias, eliges si el fichero es el episodio entero o solo algunas: marcando la b y la c queda
  como **«E1bc»** con esos dos títulos, y la fila lo enseña en una píldora. Para el caso raro de un
  fichero que mezcla historias de episodios **distintos**, «Añadir historia de otro episodio…» las
  suma y el nombre sale con el código compuesto (**«E1b+2b»**): así se ve lo que hay dentro en vez
  de disfrazarlo de episodio normal.

- **«Partir en segmentos»: un fichero por mini-historia, numeradas 1a, 1b, 1c.** Muchas series de
  dibujos meten 2-3 historias en un mismo capítulo. Si tu catálogo las lista por separado, un botón
  nuevo en Organizar las deja en ficheros independientes con la numeración por segmento. Encuentra
  solo el punto de corte —busca el fundido a negro que separa las historias y se queda con el que
  encaja con el reparto que dice el catálogo— y corta **sin recodificar**: no pierde nada de calidad
  y tarda un segundo por episodio. Los originales van a la Papelera (recuperables con **Ctrl+Z**)
  solo si salen todos sus trozos, y lo que no tenga un corte claro se queda intacto y se te dice.

- **Fichero repetido en Organizar: ves las dos rutas y eliges cuál borrar.** Cuando dos ficheros
  son el mismo episodio, al abrir la fila aparecen los **dos** ficheros implicados —este y el que
  la app conserva—, cada uno con su carpeta y un botón para abrirla. Eliges cuál mandar a la
  Papelera (nunca se borra nada por su cuenta); si mandas el que la app conservaba, el otro pasa a
  ser la copia buena. Recuperable con **Ctrl+Z**. (`#128`)

- **Deshacer con Ctrl+Z al enviar un fichero a la Papelera, en toda la app.** Cuando mandas un
  fichero a la Papelera —la copia repetida en **Organizar**, un vídeo en **Comprimir**, o el
  original tras partirlo en **Recortes**— va primero a una papelera propia de la app: pulsa
  **Ctrl+Z** y se restaura exacto a su sitio, con su contenido (en Comprimir, además vuelve a la
  lista). Esos borrados se finalizan en la Papelera de Windows —donde siguen recuperables— por tres
  vías: al cerrar la app, cada pocos minutos, y cuando se acumulan. Fiable también con ficheros de
  OneDrive. (`#137`)

- **Cada catálogo recuerda las carpetas que has analizado con él, y ahora se ve y se puede tocar.**
  Al elegir un catálogo en Organizar, su carpeta se pre-rellena sola — eliges el catálogo y ya
  puedes pulsar «Analizar», sin volver a emparejar carpeta y catálogo cada vez. Un botón nuevo
  junto a «elegir carpeta» abre la lista de carpetas de ese catálogo: saltas a cualquiera con un
  clic, **vinculas la carpeta actual** o **quitas el vínculo**. Bajo el campo se indica siempre si
  la carpeta está vinculada. Al analizar se vincula sola. (`#129`)

- **Prioridad del match por catálogo, en Organizar.** Un desplegable junto al selector de catálogo
  permite elegir en qué se fía ese catálogo al identificar: **«Automática»** (la cascada de
  siempre) o **«El número manda»** — si el fichero trae número de episodio, se usa aunque el título
  no case del todo, sin dejarlo en «revisar». Se guarda por catálogo (cada biblioteca recuerda el
  suyo). Útil cuando sabes que tu numeración es buena aunque los nombres estén sucios. (`#127`)

- **Tutoriales dentro de la app (menú Ayuda → «Tutoriales · cómo funciona…»).** Una ventana con
  índice a la izquierda y cuatro guías: cómo identifica **Organizar** tus ficheros —con un
  diagrama de qué datos lee y en qué orden de prioridad decide el estado (verde/ámbar/rojo)— y su
  paso a paso, más el paso a paso de **Comprimir** y de **Recortes** (y cómo combinarlo con
  Organizar). Todo offline y con el tema de la app. (`#130`)

### Cambiado

- **Un fichero repetido (la misma obra en dos sitios) se distingue de un conflicto de verdad y te
  dice qué hacer.** Cuando dos ficheros caen en el mismo episodio, el motivo ahora aclara si son
  **la misma obra repetida** (mismo título y segmento — el típico caso del vídeo en su carpeta y
  una copia en «Renombrar») o **dos ficheros distintos peleando** por el número. En el primero, el
  mensaje dice «fichero repetido», nombra al otro fichero y ofrece un botón **«Enviar la copia a la
  Papelera»** en el resolutor (envía el sobrante con un clic, el otro no se toca); en el segundo,
  te pide decidir cuál es el correcto. (`#128`)

- **El explorador de catálogos enseña cada mini-historia con su código.** Un capítulo con varias
  historias las juntaba en un renglón separadas por rayas, y se leía como un título kilométrico.
  Ahora va una línea por historia con **su** código —«E1a Se necesita ayudante», «E1b
  Limpiaarrecifes»—, que es como las numeran los anexos de referencia.

### Corregido

- **La carpeta vinculada a un catálogo dejaba «Analizar» apagado.** Al ponerse sola se escribía la
  ruta pero no se contaban sus ficheros, y es esa cuenta la que habilita el botón: se veía la
  carpeta puesta, «Elige una carpeta para empezar» debajo y «Analizar» sin poder pulsarse.

## [1.0.0] - 2026-07-24

Primera versión estable. Reúne el conjunto de mejoras de identificación y renombrado, el nuevo
conmutador de páginas y los arreglos de rendimiento de Recortes acumulados desde la 0.14.

### Añadido

- **El conmutador de páginas es ahora un desplegable compacto que no se come la cabecera.** En
  vez de una tira de pestañas anchas ocupando su trozo del título, un solo botón muestra la página
  en la que estás («Comprimir ▾») y, al pulsarlo, despliega la lista con todas las páginas para
  saltar a cualquiera. Ocupa lo mínimo y da lo mismo que haya 3 páginas o 20.

- **Ordenar las tablas pulsando en la cabecera de una columna.** En «Comprimir» y en «Organizar»,
  al hacer clic en una cabecera la tabla se ordena por esa columna (una flecha ▲/▼ marca cuál) y
  volver a pulsarla alterna entre ascendente y descendente. Los números ordenan como números: en
  «Comprimir», TAMAÑO ordena por el peso real del fichero y DURACIÓN por su duración, no por el
  texto. En «Organizar», un tercer clic en la misma columna quita el orden y devuelve la tabla a
  su orden natural por temporadas (con sus bandas de separación); mientras hay un orden manual,
  esas bandas se ocultan porque las temporadas quedan entremezcladas.

- **Un indicador de proceso en la cabecera, visible desde cualquier pestaña.** Mientras comprimes
  o exportas en Recortes, si te vas a otra pestaña ya no pierdes de vista que sigue en marcha:
  aparece una píldora en la cabecera con el avance («Comprimiendo 3/8 · 40 %» o «Exportando · 65 %»)
  y, al pulsarla, te lleva de vuelta a la pestaña de esa tarea. Si hay dos tareas a la vez (una
  compresión y un export), enseña la de mayor prioridad: nunca se solapan dos indicadores. Al
  terminar una compresión, la píldora dice «✓ N hechos» unos segundos antes de retirarse sola.

- **Botón «Vaciar» en Recortes para soltar el vídeo y liberar la memoria.** Deja la página como
  recién abierta —sin vídeo, sin cortes, sin historial— y devuelve al sistema la memoria que
  ocupaban el vídeo y las miniaturas, sin cerrar la app. Aparece en la cabecera cuando hay algo
  cargado; si tienes cortes preparados, pregunta antes de descartarlos.

- **Al elegir historia en un episodio multi-historia, puedes marcar VARIAS (no solo una).** El
  diálogo «¿Cuáles trae este fichero?» ahora usa casillas: si un fichero trae dos de las tres
  historias de un episodio (la «a» y la «c», pongamos), márcalas las dos y el nombre queda como
  «E413ac» con los dos títulos juntos. Marcar todas equivale a «el episodio completo». El nombre
  se relee igual, así que renombrar y volver a analizar sigue dando lo mismo.

- **«Elegir otro episodio…» también cuando detecta dos episodios en un fichero.** Antes, si la
  app veía que un fichero traía dos capítulos, solo dejaba «Partirlo en dos» o «Dejarlo como
  está» y escondía el selector de episodio. Ahora la opción de asignar un episodio a mano está
  siempre disponible: si la detección de «dos episodios» fue un falso positivo, puedes corregirlo
  eligiendo el episodio correcto — y la fila deja de recomendar partir y pasa a un renombrado
  normal. «Partirlo en dos» sigue siendo la acción destacada cuando de verdad son dos.

### Corregido

- **Los ficheros con la morralla de la web de descarga y numeración «4x01» ya se identifican con
  confianza alta.** Un fichero como «Bob_Esponja_5x01_Amigo_o_Enemigo_AMZN_WEB_DLtrialeng…»
  metía dentro del título el prefijo de la serie, el «5x01» (que no se reconocía) y la coletilla
  de la descarga, y todo eso hundía el parecido con el catálogo hasta dejarlo en «revisar» (no se
  podía aplicar en bloque) — sobre todo en títulos cortos. Ahora se reconoce el formato
  «temporada × episodio» (4x01, 12x05…) —que se lleva el prefijo de serie— y se corta la
  coletilla de descarga desde su primer marcador inequívoco (AMZN, WEB-DL, x265, 1080p…), sin
  tocar palabras reales del título. Resultado medido en una biblioteca de Bob Esponja: los 27
  ficheros que quedaban en «revisar» pasan a confianza alta, sin cambiar nada de lo que ya se
  resolvía bien.

- **Un fichero con las dos historias juntas en el nombre ya casa bien con un catálogo que las
  tiene separadas.** Muchas descargas nombran el episodio con sus dos historias seguidas
  («Historia A Historia B»), sin separador. Contra un catálogo que guarda cada historia por
  separado, ese nombre no se parecía a ninguna historia suelta y el episodio caía en «revisar»
  (no se podía aplicar en bloque). Ahora el motor también compara contra las historias unidas,
  así que esos ficheros se identifican con confianza alta y entran en el renombrado automático —
  sin perder los que traen una sola historia, que siguen casando su título.

- **El separador «|» que escribes en la plantilla ya se ve en el nombre.** El «|» es un carácter
  ilegal en los nombres de fichero de Windows y la app lo borraba, así que el separador entre las
  historias de un episodio no aparecía. Ahora se sustituye por «┃» (una barra Unicode legal y casi
  idéntica), de modo que una plantilla como «<título: | >» produce «Historia A ┃ Historia B». La
  app lee «┃» y «|» igual, así que renombrar y volver a analizar sigue dando lo mismo.

- **El recuadro de selección por arrastre ya llega a los vídeos que quedan fuera de la vista.**
  Antes, al dibujar el recuadro (arrastrando con el botón izquierdo sobre la lista de Comprimir),
  solo seleccionaba lo que cabía en pantalla: si la lista era larga, no podías abarcar de un tirón
  los de más abajo. Ahora, al llegar con el ratón al borde superior o inferior, la lista se
  desplaza sola y el recuadro sigue seleccionando mientras avanza, como en el explorador de
  archivos.

- **Un fichero bien nombrado ya no se confunde con un «remake» del mismo título al estar en una
  subcarpeta.** La temporada se leía solo del nombre de la carpeta; un fichero como
  «…S2020E574 - El aro de la gratitud.mkv» metido en una subcarpeta de trabajo (p. ej.
  «Renombrar», sin año) perdía su temporada, y como en Doraemon hay historias que se repiten
  años después con el mismo título, el motor lo tomaba por el episodio equivocado (el 88 de 2007
  en vez del 574 de 2020) y lo dejaba en conflicto una y otra vez. Ahora la temporada también se
  lee del propio nombre («S2020E…»), así que identifica el episodio correcto aunque el fichero no
  esté en su carpeta de temporada.

- **Un fichero ya correctamente nombrado deja de salir en «Conflicto» una y otra vez.** Cuando
  otro fichero reclamaba el mismo número de episodio, la app podía marcar como conflicto al que
  YA estaba bien nombrado (perdía un desempate alfabético) en vez de al aspirante. Lo «corregías»
  y volvía a aparecer en cada análisis. Ahora el **titular** —el fichero que ya lleva el nombre
  correcto— manda sobre su número y se queda en verde; el conflicto recae en el otro fichero,
  que es el que de verdad hay que decidir. Y si son dos copias del MISMO fichero (el típico caso
  de tener el vídeo en su carpeta de temporada y una copia en una subcarpeta de trabajo tipo
  «Renombrar»), la que queda verde es siempre la de la **biblioteca** —la más superficial—, no la
  de staging, sin depender del orden de escaneo. (La copia sobrante seguirá marcada como
  duplicada: para que desaparezca del todo hay que borrar ese segundo fichero.)

- **Recortes ya no se vuelve más lento cuanto más exportas.** Había una fuga de recursos: cada
  vez que exportabas un tramo y cargabas otro vídeo, el proceso se quedaba con un puñado de
  «handles» del sistema que nunca soltaba, y al repetir el ciclo muchas veces la app se iba
  arrastrando. La causa era doble: el reproductor se cerraba con una llamada que en WPF filtra
  handles a cada uso, y la exportación abría una tubería hacia ffmpeg que no hacía falta. Se
  arreglaron las dos. Medido en la máquina, la fuga por ciclo baja de ~23 handles a ~5 (el
  grueso, eliminado), y la exportación sigue produciendo exactamente los mismos ficheros.

## [0.14.7] - 2026-07-23

### Cambiado

- **Se retira el fondo de plasma animado de Recortes.** Era bonito, pero se calculaba píxel a
  píxel en la CPU y —en una app cuyo trabajo es justo saturar la CPU comprimiendo— se comía un
  núcleo entero **incluso en reposo**, y era la causa real de que la interfaz fuera lenta al
  importar y después de exportar un vídeo pesado. En su lugar queda un fondo degradado sobrio
  que no cuesta nada. Medido: el consumo en reposo cae de **~110 % de un núcleo a ~7 %**.

### Corregido

- **«Partir en dos» de otro vídeo justo después de exportar ya no carga el vídeo equivocado.**
  Al terminar una exportación, la previsualización del vídeo recién exportado se reabría con un
  pequeño retardo. Si en ese hueco cargabas otro fichero (por ejemplo, «Partir en dos» de otro
  episodio), esa reapertura tardía pisaba el nuevo vídeo con el anterior y la partición salía
  sobre el material equivocado. Ahora la reapertura comprueba antes que el vídeo en pantalla
  sigue siendo el que se exportó; si cargaste otro, no lo toca.
- **La interfaz ya no se arrastra mientras exportas con un códec por software.** Al comprimir,
  ffmpeg usaba los ocho núcleos y ahogaba a la propia app; bajarle la prioridad no bastaba.
  Ahora se le **reservan** un par de núcleos a la interfaz (y también al sondeo y las miniaturas
  del import), así responde al momento aunque la codificación esté a tope. La codificación tarda
  un pelín más, imperceptible en una tarea de fondo.

## [0.14.6] - 2026-07-23

### Cambiado

- **El gradiente de plasma también sale de fondo cuando Recortes está vacío.** Antes solo
  aparecía al exportar; ahora, cuando no hay ningún vídeo cargado, el «Elige un vídeo para
  empezar a cortarlo» se muestra sobre el mismo plasma en movimiento (atenuado para que el
  texto se lea). Mismo motor barato de siempre, y se **congela** si minimizas la ventana o
  cambias de pestaña, para no gastar batería moviendo algo que nadie está mirando.
- **El plasma se mueve más suave y ya no se congela al arrastrar la ventana.** Iba a 14
  fotogramas por segundo, que se veía a saltos y parecía lento; ahora va a 30 y fluye. Y
  arrastrar la ventana ya no lo congela: se comprobó que dejarlo correr no mete tirones (sigue
  yendo como en reposo), así que congelarlo solo se veía peor.

## [0.14.5] - 2026-07-23

### Cambiado

- **Los catálogos ya no se copian: se leen de donde están.** Al importar, la app referencia
  tu JSON en su sitio — si lo editas, cuenta al momento; ya no existe la copia interna que
  se quedaba vieja en silencio. La tarjeta enseña **la ruta del fichero** (pulsable: abre la
  carpeta con él seleccionado) y el clic derecho ofrece abrir la ubicación o copiar la ruta.
  Si mueves o borras el JSON, el catálogo **desaparece del programa** (no queda una tarjeta
  rota apuntando a un fichero que ya no está). «Quitar» de una copia interna de una versión
  anterior sí borra esa copia, para que no reaparezca al refrescar. Tu fichero original nunca
  se toca.
- **«Simular» pasa a llamarse «Analizar».** Analizar la carpeta es lo que hace; «simular»
  sugería un ensayo de mentira.

### Corregido

- **La ventana ya no va a tirones mientras se exporta o comprime.** El motor es asíncrono,
  pero sus tramos síncronos (candados, sondeos, espacio en disco) corrían en el hilo de la
  interfaz — y sobre OneDrive cada uno es un viaje de red. Ahora todo el trabajo del motor
  corre en un hilo aparte, también el escaneo de la carpeta al pulsar «Analizar».
- **El conteo de tramos exportados ya no miente.** Decía «1 de 2 sin salir» con los dos
  ficheros en el disco: si la carpeta ya tenía un fichero del mismo nombre (de un intento
  anterior), el motor saca la salida con sufijo y la comprobación buscaba el nombre sin él.
  Ahora se cuenta lo que el motor dice que escribió, comprobado en disco.
- **El tirón del final de la exportación, suavizado.** Al terminar, reabrir el vídeo en el
  reproductor costaba 100-200 ms del hilo de interfaz justo encima del desmontaje de la capa
  de aviso — era el único bloqueo medible de toda la exportación (durante la codificación el
  hilo va limpio). Ahora se reabre un instante después, cuando la interfaz está ociosa.
- **Nuevo aviso de exportación: un gradiente de plasma en movimiento.** En vez de las dos
  franjas de luz, la capa que aparece al exportar es ahora un gradiente animado de colores que
  fluyen (el efecto de plasma con viñeta y glow). Y lo importante: se hizo sin traer de vuelta
  el problema de fluidez. Se calcula diminuto (160×90) en un hilo de fondo por CPU —que
  durante un encode por hardware está ocioso— y se escala a la capa; así no toca ni el hilo de
  la interfaz (el del arrastre) ni la GPU (la del codificador), y además se congela mientras
  mueves la ventana. Medido con un export pesado de 1080p entero: arrastrar exportando va igual
  que en reposo. La intensidad del brillo respira sola, cambiando al azar cada pocos segundos.
- **Medidor de fluidez durante la exportación.** Por si algún tirón se escapa: la app mide en
  tu máquina los dos hilos que pueden causarlo —la entrada y el render— y, si de verdad hubo
  tela, lo anota en el Registro con el número. Si no sale nada y aun así lo notaste, el freno
  viene de fuera del proceso (el grabador de pantalla, que también codifica por GPU; la
  memoria llena; el compositor de Windows).
- **Los tabs que no miras dejan de trabajar.** Al cambiar de pestaña, Recortes deja de mover
  su reloj, de pedir fotogramas de previsualización y —si tenías el vídeo reproduciéndose— de
  decodificarlo; lo retoma al volver. Un tab oculto ya no se DIBUJA (de eso se encarga
  Windows), pero seguía trabajando en segundo plano sin que se viera.
- **Las tarjetas de catálogo se refrescan al volver a la app:** si borras o mueves el JSON
  desde el Explorador, la tarjeta desaparece al volver, sin reiniciar.
- **La barra de «Descargando de la nube» ahora avanza de verdad.** OneDrive suele traer el
  fichero entero de una vez, así que contar lo leído dejaba la barra a cero hasta el final;
  ahora se mide por los bytes que ya hay en disco, que crecen según baja.

## [0.14.4] - 2026-07-23

### Corregido

- **Un fichero con dos episodios distintos ahora recomienda partirlo, no elegir uno.** Cuando
  las dos historias de un vídeo casan cada una con un episodio distinto del catálogo (el
  trozo A con el 588, el B con el 589), la app lo trataba como un empate de «elige 588 o 589»
  — cuando ponerle el número de uno pierde el otro para siempre. Ahora lo detecta también en
  ese caso (antes solo si venía con número seguro), lo dice claro («trae dos episodios: el
  588 y el 589») y ofrece **partirlo en dos**, ocultando el selector de episodio que llevaba
  al error.

### Cambiado

- **Nombres de estado más claros en Organizar.** «Limpios» pasa a **«Correctos»** (el nombre
  ya está bien) y «Corregidos» a **«Con cambios»** (había un cambio propuesto que aún no se
  ha aplicado — «corregido» daba a entender que ya estaba hecho). Los ficheros que hay que
  partir se marcan aparte, con «✂ Partir en 2».

## [0.14.3] - 2026-07-23

### Corregido

- **Recortes ya no se queda «SALTADO: Descargando» hasta reiniciar la app.** La comprobación
  de «¿este fichero aún se está descargando?» abría el vídeo en exclusiva, así que saltaba
  con cualquier LECTOR: OneDrive hidratando, el indexador o el propio reproductor de la
  página, que suelta el fichero con retraso. Y tras cada intento fallido la app reabría el
  vídeo, con lo que el siguiente intento volvía a encontrarlo cogido — de ese bucle solo se
  salía reiniciando. Ahora solo salta si alguien lo tiene abierto para ESCRIBIR, que es lo
  que de verdad delata una descarga a medias.
- **Los vídeos que están solo en la nube se descargan enteros al abrirlos en Recortes,**
  con barra de progreso y Esc para cancelar. Trabajar sobre el marcador a medias era la
  otra mitad de la lentitud: las miniaturas y la codificación iban a velocidad de red y la
  app parecía ahogada sin decir por qué. Ahora la descarga se paga una vez, al principio y
  a la vista; si el sistema vuelve a soltar el fichero («liberar espacio»), exportar lo
  baja de nuevo con el mismo progreso.
- **Durante la exportación se paran las miniaturas de la pista:** goteaban lecturas sobre
  el mismo fichero que se estaba codificando.

## [0.14.2] - 2026-07-23

### Cambiado

- **La cola es ahora una cola de verdad, no un filtro.** Estaba mal planteada: añadir pedía
  escribir un motivo y verla era encender un filtro que escondía filas. Ahora funciona como
  la cola de un reproductor de música: el botón derecho la añade **de un clic, sin preguntar
  nada**, y el botón «Cola» de abajo a la derecha abre la lista con todo lo guardado. Desde
  ahí pulsas uno y te lleva a él —quitando el filtro que lo estuviera tapando—, o lo sacas
  con la ✕. El botón está siempre a la vista, también antes de simular, que es cuando abres
  la app y quieres saber qué tenías pendiente.

### Corregido

- **La app ya no se arrastra mientras exportas.** Dos causas, las dos medidas. La capa de
  aviso que se pone sobre el reproductor gastaba un **16-18 % de CPU ella sola**, en bucle,
  todo el rato que durase la exportación: la caché de dibujo estaba puesta en el grupo y no
  en cada luz, así que cada latido obligaba a repintar la capa entera. Ahora cuesta un
  **3-5 %** — y va a 20 fotogramas por segundo en vez de a 5, así que además se ve suave.
  Y ffmpeg, que codifica con todos los núcleos, ahogaba a la propia app: pasa a prioridad
  por debajo de lo normal, con lo que la ventana recupera CPU (medido: del 3,1 % al 4,9 %
  sobre un 5,9 % sin carga) a cambio de codificar un ~4 % más despacio.
- **Los pulsos de luz nacen ahora en los cantos.** Eran dos círculos flotando sobre el
  vídeo, con su silueta a la vista compitiendo con el mensaje. Ahora son dos franjas que
  asoman desde los laterales y se apagan hacia el centro: se leen como luz, no como formas.

## [0.14.1] - 2026-07-22

### Corregido

- **La versión de terminal (`shrinkstudio`) vuelve a publicarse.** Llevaba sin compilar desde
  que Recortes enseñó al motor a cortar por tramos: la CLI no incluía esa parte del motor y
  ninguna de las cinco variantes (Windows, Linux, macOS) llegaba a la publicación. La app de
  escritorio nunca se vio afectada.
- **Deshacer un lote devuelve también la marca de revisión.** Si tenías un fichero apartado
  y deshacías el renombrado, la marca seguía apuntando al nombre que ya no existía.

## [0.14.0] - 2026-07-22

### Añadido

- **Cola de revisión en Organizar.** Con el botón derecho sobre un fichero puedes apartarlo
  para mirarlo con calma, dejando escrito qué le pasa. Queda marcado con un 🔖, hay un chip
  que deja ver solo los apartados, y **sobrevive al cierre de la app**: al volver los tienes
  ahí sin buscarlos otra vez entre cientos. Si mientras tanto aplicas el renombrado, la marca
  se va con el fichero a su nombre nuevo.
- **Al terminar de exportar en Recortes se ofrece borrar el original.** Solo si TODOS los
  tramos están de verdad en disco, y va a la papelera de reciclaje, así que se puede
  recuperar si al verlos algo no cuadra.
- **Pausar y detener la exportación en Recortes.** Pausar suspende ffmpeg donde va y reanudar
  sigue desde ahí, no desde el principio. Detener corta y **mata el proceso**: comprobado que
  no queda ninguno de fondo, también en el caso traicionero de pausar primero y detener
  después.
- **Aviso mientras se exporta.** El reproductor se apaga a propósito —el vídeo tiene que
  estar libre para poder cortarlo—, así que en vez de un rectángulo negro que parece una
  avería se vela la imagen, late una luz suave y un triángulo explica por qué.
- **Deshacer y rehacer en Recortes (Ctrl+Z, Ctrl+Y y Ctrl+Mayús+Z).**
- **Se avisa antes de perder el trabajo.** Cargar otro vídeo con una exportación en marcha
  ya no se hace a medias: se dice que la detengas. Y si tenías tramos preparados, se
  pregunta antes de descartarlos.

- **Ampliar la línea de tiempo en Recortes (Ctrl + rueda, Ctrl + y Ctrl −).** Hasta 40×, para
  clavar un corte al fotograma en vez de a ojo. Amplía **por donde apuntas**: el punto bajo el
  cursor no se mueve, así que no pierdes de vista lo que estabas mirando. Ctrl+0 vuelve a ver
  el vídeo entero, y arriba a la derecha se indica el aumento.

- **«Partirlo en dos» cuando un fichero trae dos episodios.** Ese caso no se arregla
  eligiendo número —le pongas el que le pongas, pierdes el otro—: hay que partir el vídeo.
  Ahora el resolutor lo ofrece con un botón destacado que lo lleva a Recortes con un corte
  ya puesto por la mitad, listo para arrastrarlo al sitio exacto.
- **Previsualización en la barra del reproductor.** Pasando el ratón por la barra sale el
  fotograma de ese punto con su tiempo, como en Recortes. Si el vídeo todavía está solo en
  la nube no se saca ningún fotograma —eso lo descargaría entero—; en cuanto termina de
  bajar, las previas salen solas.
- **El vídeo arranca solo de verdad.** Se abre con doble clic, y el «soltar» de ese segundo
  clic aterrizaba ya dentro del reproductor recién abierto: lo pausaba al nacer y se quedaba
  en la animación de carga hasta que dabas al play. Ahora solo cuenta como clic el que
  EMPIEZA dentro de la ventana.
- **Una animación mientras el vídeo carga.** El rectángulo negro no decía nada, y con
  «Archivos a petición» la espera puede ser larga de verdad porque el fichero se está
  descargando entero. Ahora laten cuatro círculos en cascada —cada uno con su onda
  expansiva— y el texto dice si el vídeo está bajando de la nube o solo abriéndose. Se
  retira cuando el vídeo AVANZA de verdad, no cuando dice estar abierto: con un fichero
  descargándose eso ocurre mucho antes que el primer fotograma.


- **Vista previa del fotograma al recorrer la barra en Recortes.** Encontrar el punto de
  corte mirando una barra lisa era adivinar: ahora, al pasar o arrastrar por la linea de
  tiempo, sale un globo con el fotograma de ese punto y su minuto. Se sacan bajo demanda de
  donde esta el cursor y se sueltan enteros al cambiar de video o salir de la pagina: ni un
  fichero temporal ni un mapa de bits se quedan acumulados.
- **La pagina avisa mientras prepara el video.** Analizar y sacar los fotogramas lleva unos
  segundos: durante ese rato los controles estan deshabilitados y se ve que se esta
  haciendo y cuanto queda, en vez de parecer que la app se ha colgado.
- **Mas feedback al exportar:** el boton dice cuantos tramos va a sacar («Exportar 2
  tramos») y el tramo que se esta procesando se resalta en la lista con la marca
  EXPORTANDO.
- **Recortes tiene una pista de edición, como un editor de vídeo.** Donde antes había dos
  cosas separadas —una línea morada que solo se miraba y una barra debajo para moverse—
  ahora hay una sola pista: el fondo son fotogramas del propio vídeo, cada tramo es un
  bloque con su número y el nombre del fichero que va a salir, y lo que has quitado se ve
  oscurecido, así que de un vistazo sabes qué se va a exportar y qué no.
- **Las juntas entre tramos se arrastran para afinar el corte.** Cortas a ojo y luego tiras
  del tirador hasta el fotograma exacto, con el globo de la previa siguiéndote. Una junta
  no puede pasarse de los tramos de al lado, así que ningún tramo se queda del revés ni a
  cero. Si has quitado un trozo y hay hueco, cada borde se estira por su lado.
- **Las acciones van donde actúan.** «Cortar aquí» ya no está abajo a la derecha: es una ✂
  pegada al cabezal, justo por donde va a partir. Y cada bloque lleva su ✕ para quitarlo,
  que asoma al pasar por encima.


- **Un fichero que contiene dos episodios ya no se renombra solo.** Hay ficheros que
  emparejan dos historias que el catálogo cuenta como episodios distintos: ponerles el
  número de uno pierde al otro en silencio. Ahora se detecta y se te pregunta, diciendo
  cuáles son los dos. No confunde esto con un remake —la misma historia en un episodio
  viejo y en uno moderno es lo normal—: solo salta si el episodio elegido no cubre lo que
  el fichero trae.


- **Progreso de verdad al exportar:** que tramo va, de cuantos, con su nombre y el
  porcentaje; y si el motor se salta uno, se ve el motivo en la propia barra.
- **Se puede elegir donde guardar los recortes.** Por defecto van junto al video original.
- Exportar ya no se puede lanzar dos veces a la vez: cada clic arrancaba otra tanda entera
  sobre los mismos ficheros.


- **Recortes: una tercera sección para partir un vídeo o quitarle un trozo.** Sirve para el
  caso de «este fichero son dos capítulos»: cargas el vídeo, lo llevas por donde separan,
  pulsas «Cortar aquí» y salen dos tramos — cada uno será un fichero. Quitar un tramo
  descarta ese trozo, así que recortar es lo mismo con un paso menos. Si el nombre del
  fichero trae las dos historias («A ┃ B»), cada tramo se nombra solo con la suya.
  Desde Organizar, el botón derecho sobre una fila lo abre ahí directamente.
- **La salida de Recortes usa los mismos ajustes y la misma estimación que Comprimir.**
  Formato, códec, calidad, resolución y audio son los de siempre, y el tamaño estimado sale
  de la misma fórmula, ajustada a lo que de verdad se va a exportar.

### Cambiado

- **Muchas menos decisiones a mano: de 1 a 17 renombrados automáticos sobre la misma
  biblioteca, y de 40 a 17 pendientes.** Tres cosas que obligaban a mirar y no lo merecían:
  las etiquetas de la fuente pegadas al nombre («[Boing HD]») restaban parecido; los
  separadores de historias «A + B» y «A - B» no se reconocían —solo «┃» y «|»—, así que el
  título entero se comparaba contra medio episodio y salía un 58 %; y el nombre de la serie
  se quedaba delante del título («Doraemon (2005) - - El elixir…»), donde su guion se
  confundía con el que separa historias.
- **Que dos ficheros apunten al mismo episodio ya no manda a los dos a revisión.** Solo se
  pide mirarlo cuando el rival llegaba con la misma solvencia. Que uno lo clave por título
  y el otro solo trajera un número dudoso no es una ambigüedad: es un número dudoso.

### Corregido

- **Lo ya renombrado deja de contar como «corregido».** Tras aplicar, esas filas seguían
  sumando en el chip de corregidos y saliendo al filtrar por él, como si quedara trabajo
  pendiente que ya no existe. Ahora cuentan como limpias —están bien en el disco— y, si
  tenías el filtro de corregidos puesto, salen de la vista solas.
- **Simular tarda la mitad.** Sobre 546 ficheros: de ~50 s a ~28 s. El motor preguntaba al
  catálogo dos veces lo mismo — un recorrido completo comparando títulos para elegir el
  mejor episodio y otro idéntico para sacar las alternativas que se te ofrecen. Ahora es un
  solo recorrido del que salen las dos cosas. Ni un fichero de los 546 cambia de respuesta.
- **La app ya no gasta media máquina parada, y todo va más suelto.** Con la app en reposo
  absoluto se consumía un 6-8 % de CPU (y de batería) en repintar decoraciones: la luz
  ambiental redibujaba TODA la interfaz 60 veces por segundo para un latido de 9 segundos,
  el brillo de cada barra de progreso recalculaba su desenfoque en cada fotograma (con una
  cola larga, la app entera se arrastraba: escribir iba a tirones y arrastrar ventanas a
  golpes), y el halo del campo con foco se recalculaba mientras el haz giraba. Medido pieza
  a pieza y arreglado sin quitar nada: los mismos brillos y latidos, pero componiendo
  texturas ya pintadas en vez de repintar. En reposo: de 6-8 % a ~1 %.


- **La app ya sabe releer los ficheros que ella misma marcó como una sola historia.** Al
  decidir «esto es solo la historia b», escribe la letra pegada al número («S2017E487b»)
  — pero no sabía volver a leerla: el fichero se quedaba sin número ni segmento, se
  reidentificaba solo por el título, casaba con el episodio entero y proponía deshacer tu
  decisión. Cada pasada deshacía la anterior.
- **Renombrar y volver a simular ya no puede cambiar de episodio.** Si el título de un
  episodio del catálogo llevaba un número entre corchetes —los hay: «Cuido de mamá
  (LA)[30]»— ese número acababa dentro del nombre propuesto, y al releerlo ganaba al
  «S2005E536» que la propia app había escrito: la segunda pasada creía que era el episodio
  30. Ahora el marcador explícito manda sobre cualquier número suelto. Los ficheros que
  usan la convención de corchetes («[499b] Título») siguen funcionando igual.


- **En Recortes ya se puede escribir el nombre de un tramo entero.** Los atajos de la
  página se comían las teclas antes de que llegaran al cuadro de texto: en el nombre de un
  tramo no se podía poner un espacio, ni escribir una «c», ni mover el cursor con las
  flechas. Ahora, mientras estás escribiendo, las teclas son letras y no atajos.
- **Recortes no exportaba nada.** El motor comprueba que nadie tenga el fichero cogido
  (para no pillar una descarga a medias) y quien lo tenia cogido era el propio reproductor
  de la pagina: se saltaba el video y no salia ni un fichero. Ahora se suelta antes de
  codificar y vuelve al terminar.
- **Recortes decia «2 ficheros creados» sin haber creado ninguno.** Daba por bueno que la
  llamada al motor volviera. Ahora se comprueba el fichero en disco y, si falta, se dice
  cuantos no salieron y con que nombre.

## [0.13.0] - 2026-07-22

### Añadido

- **Menú contextual en la tabla de Organizar.** Clic derecho sobre un fichero para
  reproducirlo o **abrir su ubicación** en el explorador, con el fichero ya seleccionado.
  Abrir la ubicación no descarga nada, así que sirve igual para los que están en la nube.
  También responde a la tecla Menú del teclado, sobre la fila seleccionada.
- **Los vídeos que se descargan de la nube para verlos vuelven a la nube al cerrar.**
  Identificar un capítulo mirándolo medio minuto no debería dejar 250 MB ocupados para
  siempre: si el fichero estaba solo en la nube antes de abrirlo, al cerrar el reproductor
  se pide que se libere. Solo se toca lo que ya estaba en la nube — lo que tengas guardado
  a propósito se queda.

### Cambiado

- **Lo de la nube ya no habla de un proveedor concreto ni promete tirones.** El aviso del
  reproductor pasa a ser «En la nube · descargando para verlo». El mecanismo lo define
  Windows, no un proveedor: funciona igual con OneDrive, Nextcloud, Dropbox, Google Drive
  o iCloud, porque se miran los atributos del fichero y no quién los puso.

## [0.12.1] - 2026-07-22

### Corregido

- **Identificar una carpeta ya no descarga vídeos de la nube.** Para los ficheros que
  quedaban en duda sin `.nfo`, la app abría el vídeo con ffprobe a leer su título — y con
  «Archivos a petición» de OneDrive abrir un fichero lo descarga **entero**: medido, 277 MB
  en 18 segundos por vídeo. En una biblioteca con 467 ficheros así son 90 GB de tarifa y de
  disco sin haberlo pedido. Ahora se reconoce el marcador y no se abre; el resumen dice
  cuántos se han dejado sin mirar por eso. Los `.nfo`, que pesan nada, se siguen leyendo.

## [0.12.0] - 2026-07-22

### Añadido

- **Doble clic sobre una fila para ver el vídeo, sin salir de la app.** Ante la duda de qué
  capítulo es, verlo gana a cualquier metadato — también después de aplicar, donde abre el
  fichero con su nombre nuevo. Se abre una ventana oscura en modo focus. Los controles flotan
  sobre la imagen y se apartan solos a los 2,6 s de no usarlos (vuelven al mover el ratón;
  en pausa se quedan). Barra de posición con punto, salto de ±10 s, volumen, silencio y
  pantalla completa. Atajos: espacio pausa, flechas saltan (con Mayús, 30 s), F pantalla
  completa, M silencio, Esc sale. Doble clic sobre la imagen también expande. Si el códec
  no está soportado, lo dice y ofrece el reproductor del sistema con un botón.
- **Aviso cuando el vídeo está solo en la nube.** Con «Archivos a petición» de OneDrive el
  fichero se descarga mientras se reproduce y la imagen va a tirones. El reproductor lo
  detecta y lo dice, en lugar de parecer que está roto.
- **Los dudosos se identifican también por su `.nfo` y por los metadatos del vídeo.** Un
  fichero sin título en el nombre («S2018E01.mkv») suele llevarlo en su `.nfo` de Kodi o en
  la etiqueta del contenedor. Tras la primera pasada, la app lee esas dos fuentes SOLO de
  los que quedaron en duda —el `.nfo` primero, que es instantáneo— y re-identifica. La
  Season 2018 pasó de 18 dudas a 18 listos con su título de verdad.
- **Los ficheros numerados por temporada («S2018E01», el 1.º de 2018) ya se entienden.**
  Cuando el número del fichero contradice a su carpeta —el episodio 1 del catálogo es de
  2005, no de 2018— o directamente no existe en la numeración global, se relee como «el N.º
  de esa temporada». Sale en ámbar (sin título ni fecha que lo confirme, se revisa) con la
  lectura global de alternativa y la etiqueta «nº de temporada» en la columna del porqué.
- **Los ficheros compañeros (.nfo, .srt…) se renombran junto al vídeo.** Un .nfo con el
  nombre viejo queda huérfano y tu reproductor de biblioteca deja de asociarlo. Van al mismo
  diario del lote, así que «Deshacer» también los devuelve. Un subtítulo «.es.srt» conserva
  su sufijo completo.
- **Buscador dentro de la tabla de Organizar (Ctrl+K).** Filtra en vivo por el nombre
  original o por la propuesta, con la misma normalización del identificador: «animo» sin
  tilde encuentra «¡Ánimo, antepasado!». Esc lo limpia. Se combina con los filtros de
  estado que ya había.
- **Un fichero puede ser SOLO una historia de un episodio, y ya hay forma estándar de
  decirlo.** En el resolutor, «Elegir otro episodio…» abre por fin el explorador (buscando ya
  el título del fichero); al elegir un episodio con varias historias, la app pregunta si el
  fichero es el episodio completo o solo una de ellas. Si es solo una, la letra va pegada al
  número —`E413b`, para no pisarse con el episodio completo ni con la otra mitad— y el título
  es el de esa historia, no el del episodio entero. La decisión se recuerda para ese fichero.

### Corregido

- **«Deshacer este lote» ya no te saca de la tabla.** Deshace en el sitio: las filas del
  lote vuelven de «Hecho» a su estado anterior —con su casilla y su propuesta intactas,
  listas para re-aplicar si era eso lo que querías— y sigues exactamente donde estabas.
- **El texto de los campos se veía ligeramente borroso.** El halo de foco (el efecto de
  sombra) envolvía al propio texto y lo rasterizaba sin ClearType. El halo sigue; el texto
  ya vive fuera de él.
- **Buscar el nombre viejo de un fichero ya renombrado ya no lo encuentra.** Tras aplicar,
  la fila solo responde a su nombre nuevo — que es el que existe en disco. Encontrarla por
  el viejo hacía dudar de si el renombrado había ocurrido de verdad.
- **Volver a simular tras aplicar enseñaba el pasado.** La lista de ficheros se escaneaba al
  elegir la carpeta y no se refrescaba nunca: después de renombrar 462 ficheros, re-simular
  volvía a resolver los nombres viejos y la tabla enseñaba los mismos «Corregido» de antes
  — como si aplicar no hubiera hecho nada, cuando el renombrado sí se había hecho. Ahora
  cada simulación re-escanea la carpeta.
- **Aplicar cientos de renombrados ya no congela la ventana.** Los movimientos van en
  segundo plano y la barra dice «Renombrando N ficheros…» mientras tanto.

## [0.11.0] - 2026-07-22

### Cambiado

- **El panel de ficheros es ahora el centro de la identificación, con progreso animado.**
  La carpeta a organizar, el recuento y «Simular» estaban repartidos por tres sitios de la
  pantalla; ahora viven juntos en el panel. Y al simular, el panel enseña las tres fases
  reales del trabajo —leer los nombres, identificar contra el catálogo, preparar la
  revisión— cada una con su círculo en espera, su arco girando mientras corre y su check
  verde que se dibuja y da un pequeño salto al terminar, con el haz de luz recorriendo el
  borde del panel mientras trabaja.

### Corregido

- **La casilla de aplicar salía recortada por la columna de al lado.** Su columna medía lo
  justo sin contar el relleno interno de la celda.

## [0.10.0] - 2026-07-22

### Añadido

- **Eliges qué se aplica, fichero a fichero.** Cada fila lista lleva su casilla (marcadas
  todas de inicio), la cabecera marca o desmarca todas, y el botón dice exactamente cuántos
  va a tocar («Aplicar 30 de 31»). El cuadro de confirmación cuenta también lo que se queda
  fuera y por qué: dudas, conflictos y lo que tú hayas desmarcado. Los conflictos no llevan
  casilla a propósito: no se aplican jamás, estén como estén.
- **Un explorador del catálogo para comprobar propuestas sin abrir el JSON.** La lupa junto a
  «Catálogos…» abre el catálogo elegido con buscador por número («175») o por título
  («planeta espejo»), con la misma normalización que usa el identificador. Antes, dudar de
  una sugerencia obligaba a rebuscar en el JSON a mano — y esa fricción deja dudas razonables
  sin comprobar.
- **Al elegir un episodio en el explorador, su JSON emerge en el lateral.** Es el fragmento
  del catálogo tal y como lo está leyendo el identificador —no una reconstrucción— con botón
  de copiar. Para cuando la vista bonita no basta y quieres ver la fuente. El JSON va
  **coloreado** (claves, textos, números y símbolos, con los colores del tema) y el panel se
  cierra con su aspa.

### Corregido

- **Los botones de las tarjetas de catálogo, ahora opacos y sin pisar el texto.** Seguían
  siendo transparentes (se leía el resumen a través) y el texto corría por debajo de ellos.
  Ahora tienen acabado de cristal opaco con brillo en el canto, y el resumen se recorta en su
  columna en vez de invadir la de los botones.
- **Las casillas de aplicar no respondían al ratón y ya se pueden marcar arrastrando.** El
  «volver a pinchar una fila la cierra» oía el clic antes que la casilla y se lo comía
  cuando la fila estaba seleccionada: la casilla parecía muerta. Ahora el clic de la casilla
  es de la casilla, y además puedes arrastrar por la columna para marcar o desmarcar varias
  de una pasada — el arrastre contagia el valor del primer toque, sin alternar fila a fila.
- **El nombre de la serie dentro del fichero ya no estropea la identificación por título.**
  «Doraemon (2005) S2009E175 - El planeta espejo» se comparaba con el prefijo incluido, el
  parecido caía por debajo del umbral y acababa ganando el número equivocado del propio
  fichero. Ahora el título se compara también sin la serie delante: ese fichero pasa a
  identificarse como E173 con el título al 100 %. En la biblioteca de prueba, los conflictos
  bajan de 49 a 31.
- **«2.ª parte» ya iguala a «segunda parte»** (y 1.ª/3.ª/4.ª): el fichero y el catálogo suelen
  escribir el ordinal de forma distinta y eso restaba parecido justo donde más dolía.
- **Importar un catálogo también lo guarda como última serie.** Quitar uno y reimportarlo
  dejaba la preferencia vacía y el siguiente arranque volvía a caer en el primero por
  alfabeto.

## [0.9.0] - 2026-07-21

### Añadido

- **Un botón «← Volver» para salir de la simulación** y regresar a la pantalla de inicio. Antes,
  una vez simulabas te quedabas en la tabla y la única salida era cambiar de página y volver.
  No pregunta nada porque no se pierde nada: las decisiones que hayas tomado a mano se guardan
  en cuanto las tomas y se reaplican solas al volver a simular. La carpeta elegida se conserva.

### Eliminado

- **La pestaña «Pasos» se retira.** Se añadió en la 0.8.0 y no ha convencido en el uso, así
  que se quita entera en vez de dejarla ocupando sitio. El registro sigue igual: nunca llegó
  a sustituirlo, así que no se pierde nada de lo que ya había.

### Corregido

- **Las marcas de la plantilla salían dentadas.** Cada una empezaba en una sangría distinta,
  así que la lista se leía como texto centrado en vez de como una tabla. La plantilla de los
  botones clavaba su contenido al centro e ignoraba a quien pidiera otra cosa — le pasaba
  igual a la lista de idiomas.
- **El ejemplo del nombre final ya se puede leer entero.** La línea «Quedaría:» se corta casi
  siempre porque estos títulos son larguísimos; ahora el nombre completo está también en el
  globo de ayuda del campo, junto con la explicación de para qué sirve.

## [0.8.0] - 2026-07-21

### Añadido

- **Una caja de pasos enseña por dónde va el vídeo que se está comprimiendo.** Nueva pestaña
  «Pasos», que se abre sola al empezar: leer el vídeo, elegir pistas y calidad, codificar y
  guardar, cada uno con su marca y lo que se ha averiguado («audio: spa», «42 %», «1,2 GB →
  380 MB»). **No sustituye al registro**: el registro cuenta qué pasó *después* y sirve para
  revisar; esto contesta «¿por dónde va?» de un vistazo, que es lo que se mira *mientras*
  corre. Si algo se tuerce, el paso que falló se marca y los siguientes quedan como
  «saltados», no como fallidos: no fallaron, es que ya no se intentan.
- **La plantilla admite relleno con ceros y separador propio: `<num:000>` y `<título: ┃ >`.**
  Sin esto no se podía describir una biblioteca que ya estuviera ordenada con otra
  convención, y entonces salía todo como pendiente de renombrar aunque el trabajo estuviera
  hecho. El caso que lo destapó: ficheros `S2005E001 - A ┃ B`, que la app sabía **leer** —la
  barra `┃` ya era separador de historias— pero no sabía **escribir**.
- **Los catálogos dicen de qué fichero salieron y cuándo, y se pueden quitar.** La app trabaja
  con una copia del JSON que importas, así que si luego editas el original tu copia se queda
  vieja sin que nada lo delate: ahora cada tarjeta lo dice. Y «Quitar» lo saca de la app sin
  tocar tu fichero, que sigue donde estaba por si quieres volver a importarlo.
- **Se recuerda la última serie elegida.** Con más de un catálogo, cada arranque empezaba en
  el primero por orden alfabético y había que volver a elegir.
- **El selector de idiomas es ahora la norma ISO entera, con buscador.** Antes eran siete
  opciones fijas elegidas a ojo; si tu serie venía titulada en cualquier otro idioma, no
  había forma de decirlo. Ahora se busca por nombre o por código, sin tildes y a medias
  («japones», «ja», «catal»), los elegidos quedan a la vista como etiquetas y se quitan de
  una en una.

### Cambiado

- **Dos códigos de idioma estaban mal y se han corregido a ISO**: el japonés era `jp` —que
  es el código del *país*, no del idioma— y ahora es `ja`; el español de Hispanoamérica era
  `lat` y ahora es `es-419`. **Tus catálogos existentes se siguen leyendo igual**: los
  códigos viejos se traducen solos al abrirlos, así que no hay que regenerar nada. Lo que
  cambia es que los catálogos nuevos ya salen con códigos correctos.
- **«Idiomas para reconocer los ficheros» se llama ahora «Idiomas en los que vienen
  titulados tus ficheros»**, y explica en el globo de ayuda para qué sirve exactamente y en
  qué se diferencia del idioma del nombre final. El rótulo viejo se leía como si fuera el
  idioma del programa.


- **Los avisos y las preguntas ya no son los cuadros grises de Windows.** Toda la app usa
  ahora su propio diálogo, con el mismo tema que el resto, y el texto se puede seleccionar
  y copiar — que es lo primero que quieres hacer cuando el aviso trae una ruta o el texto
  de un error.
- **Un haz de luz recorre el borde de lo que tiene el foco**, en toda la app: campos,
  botones, desplegables y casillas. Se ve de un vistazo dónde estás, sobre todo moviéndote
  con el tabulador. Solo gira mientras ese control tiene el foco, así que nunca hay más de
  uno encendido.
- **Lo que se corta con puntos suspensivos enseña el texto completo al pasar el ratón.** No
  hace falta ensanchar la ventana para leer un nombre largo. Solo aparece cuando el texto
  está recortado de verdad, para no repetir lo que ya se ve.

- **El encargo para la IA ya no da por hecho que el anexo lo tiene todo.** Antes enseñaba un
  único ejemplo con todos los campos rellenos, y ante una tabla pobre la IA acababa
  inventándose fechas o improvisando estructura. Ahora lista los campos que admite el
  programa separando lo obligatorio de lo opcional, enseña también un catálogo mínimo
  igual de válido, y deja una sola regla sin excepción: se pueden omitir campos, nunca
  inventarlos.

### Corregido

- **En las tarjetas de catálogo, «Usar», «Quitar» y «seleccionado» se dibujaban unos encima
  de otros.** Iban los tres pegados a la derecha en el mismo sitio; mientras «Quitar» no
  existía no se notaba, porque los otros dos nunca salen a la vez. Ahora van en fila y con
  fondo sólido: transparentes sobre el título de al lado no había quien los leyera.
- **La lista de idiomas ya no ocupa media ventana ni pisa la vista previa.** Estaba siempre
  desplegada —183 idiomas— cuando lo normal es tocarla una vez y olvidarse. Ahora se ven las
  etiquetas de los elegidos y el buscador se abre con «+ Añadir», igual que el menú de marcas.
- **Un fichero que ya se llama exactamente como debe sale en verde y no cuenta como
  pendiente.** Antes el color lo decidía la confianza de la identificación, así que un
  fichero al que no había que tocarle nada podía salir en ámbar. Y es al revés: que el
  nombre coincida entero con el que produciría la plantilla es la confirmación más fuerte
  que hay. El recuento lo dice aparte («46 ya estaban bien»), para que no parezca que se han
  perdido por el camino.
- **Volver a pinchar una fila abierta la cierra.** Antes el desplegable se quedaba abierto y
  la única forma de recogerlo era abrir otro. Los botones de dentro siguen funcionando: solo
  cierra el clic sobre la fila, no sobre sus opciones.
- **Un «Limpio» en ámbar ya explica por qué.** Desconcertaba con razón: la palabra dice que el
  fichero ya se llama como toca y el color dice que hay algo que mirar. Son dos cosas
  distintas —el nombre puede estar bien y aun así no ser ese episodio— y ahora el globo de
  ayuda lo cuenta al pasar por encima.
- **Los campos de Origen, Destino, Carpeta y Plantilla ya se encienden al escribir en ellos.**
  Eran los únicos que se quedaron sin el haz de foco, porque estaban montados a mano en cada
  pantalla en vez de ser el mismo componente. Ahora lo son, así que lo que se arregle en uno
  vale para todos.
- **Organizar ya lee la serie entera, no solo el primer nivel de la carpeta.** Al apuntar a
  la carpeta de una serie —la que tiene dentro `Season 2005`, `Season 2006`…— decía «no hay
  vídeos» sobre una carpeta con cientos, porque solo miraba los ficheros sueltos de arriba.
  Ahora baja por las subcarpetas y te dice en cuántas ha encontrado los ficheros, así que se
  ve al momento si has apuntado demasiado adentro.
- **La tabla sale separada por temporada**, con su cabecera y su recuento entre una y otra,
  en el orden de la biblioteca: 2005, 2006, 2007… y los vídeos sueltos de la raíz al final.
  Saber de qué carpeta viene cada fila es la mitad de la información cuando hay que decidir
  si una propuesta tiene sentido.


- **El texto se cortaba por abajo en los campos de «Generar con IA».** Tenían una altura
  fija que no daba para una línea con su espaciado.

## [0.7.0] - 2026-07-21

### Añadido

- **El formato del catálogo está documentado y se comprueba al importar.** Un botón
  «¿Qué formato?» abre la especificación con todos los campos, las reglas y un ejemplo
  completo, y «Crear ejemplo…» te guarda un catálogo válido para que lo edites en vez de
  escribirlo a ciegas. Si el archivo tiene fallos, se te dicen **todos juntos** y con el
  episodio concreto: números repetidos (que antes hacían perder un episodio en silencio),
  fechas imposibles o números negativos.
- **Generador de catálogos con IA.** El botón «Generar con IA…» arma el encargo para que una
  IA convierta un anexo de episodios (Wikipedia, Fandom, el que uses) en el catálogo, con el
  formato y las reglas ya dentro. Eliges la serie, la dirección y los idiomas, y lo copias.
  Cada anexo está montado a su manera, así que el texto le dice cómo resolver lo que cambia
  entre ellos: qué columna es el número, qué hacer si solo numeran por temporada, cómo
  tratar los episodios con varias historias y qué fecha usar si hay más de una.

### Cambiado

- **Reconoce ficheros en un idioma y los nombra en otro.** Antes solo se comparaba contra
  los títulos en español, así que un fichero titulado en inglés no se identificaba nunca.
  Ahora el catálogo declara en qué idioma quieres el nombre final y con cuáles hay que
  comparar: `Help Wanted.mkv` se reconoce por su título inglés y se renombra al español.
- **La ventana se puede encoger mucho más.** El mínimo baja de 1000×660 a 820×560, y el
  contenido se adapta en vez de recortarse: los campos se estiran, los botones bajan de
  línea si no caben, el panel de detalle se pliega cuando le quita sitio a la tabla, y en
  la barra de título el conmutador se queda en iconos para que no se coma el menú.
- **El generador de prompts avisa de la trampa de la numeración.** Muchos anexos traen a la
  vez el «número de transmisión» (orden de emisión) y el «número de episodio» (oficial), y
  no dan el mismo resultado: en Doraemon (2005) el estreno es la transmisión 1 pero en la
  numeración oficial es un especial, así que elegir mal desplaza la serie entera. Ahora el
  encargo manda usar el de transmisión salvo que digas otra cosa, y lo explica con ese caso.

- **La plantilla de nombres se construye, no se adivina.** El botón «Editar» no hacía nada
  que no hicieras pinchando en el propio campo; en su sitio hay un desplegable de marcas que
  las inserta donde tengas el cursor y explica qué hace cada una. Debajo, un ejemplo en vivo
  con un episodio real del catálogo, para ver cómo queda el nombre antes de aplicar nada.

### Corregido

- **Se ofrecían «alternativas» en filas que ya estaban resueltas.** Un episodio identificado
  al 100 % venía acompañado de dos candidatos al 67 % y al 65 %: ruido en una fila correcta,
  que encima invitaba a un clic equivocado. Ahora las alternativas solo salen donde hay algo
  que decidir de verdad.
- **El resolutor de conflictos ofrecía dos episodios y ninguno era el bueno.** Enseñaba
  solo los descartados, así que la propuesta correcta no aparecía por ningún lado y las dos
  opciones parecían equivocadas —lo estaban—. Ahora la primera tarjeta es la que propone la
  app, marcada como «más probable», y **cada opción enseña el nombre que quedaría** si la
  eliges, para no decidir a ciegas.
- **La temporada del fichero no se usaba para nada.** Se leía del nombre y de la carpeta y
  después se tiraba, así que un episodio de 2014 competía de tú a tú con el de 2005 que el
  propio fichero estaba declarando. Además, cuando un fichero trae varios títulos, ahora
  gana el episodio que explica **más** de ellos, no el que casa mejor con uno solo. Entre las
  dos cosas, muchas preguntas que antes te hacía se resuelven solas.
- **Cuando dos ficheros se disputaban el mismo episodio no se veía qué episodio era.** El
  aviso nombraba el número pero no su título, así que no había forma de juzgar cuál de los
  dos ficheros tenía razón — que es justo lo que hay que decidir ahí. Ahora se dice qué
  título espera el catálogo para ese número, y con qué fichero compite.
- **Con el aviso de actualización en pantalla no se podía mover la ventana.** El aviso se
  colocaba encima de la barra de título y se quedaba con la franja que Windows reserva para
  arrastrar, así que no valía ni arrastrar por el aviso ni por la barra. Ahora el aviso va
  debajo y la ventana se mueve como siempre.

## [0.6.0] - 2026-07-21

### Añadido

- Nueva página **Organizar**, que identifica qué episodio es cada fichero comparándolo con
  un catálogo de la serie y propone su nombre definitivo. Se cambia entre «Comprimir» y
  «Organizar» desde la barra de título, y la compresión sigue su curso mientras tanto
  (una píldora avisa del avance). Nada se renombra sin aprobación: primero se simula, se
  revisa el resultado en una tabla con semáforo —limpio, corregido, especial, conflicto,
  error— y solo entonces se aplica. Cada lote aplicado se puede deshacer entero, y las
  decisiones que tomas se recuerdan para no volver a preguntarte lo mismo.

### Cambiado

- **La columna «Estado» de la tabla ahora sirve para algo.** Antes ponía «listo» tras analizar
  y no volvía a cambiar nunca. Ahora cuenta lo que pasa con cada vídeo: si ya está bien
  comprimido y por qué se salta, cuándo está en cola, el avance mientras se comprime, y al
  terminar cuánto se ha ahorrado y cuánto ocupa.
- **Al analizar se marcan solos los vídeos que conviene comprimir.** Los que ya están en un
  códec eficiente se quedan sin marcar, con su motivo a la vista, en vez de descubrirlo al
  lanzar la tanda.
- La lista vacía ahora explica qué hacer en vez de ser un hueco en negro.
- **La interfaz se ilumina.** El fondo tiene ahora una luz ambiental tenue que respira muy
  despacio, y brillan los puntos que importan: el botón de la acción principal al apuntarlo,
  el campo donde vas a escribir, la página en la que estás y el progreso mientras trabaja.
  Está medido para que ayude a mirar donde toca, no para llenar la pantalla de luces.
- **Las ventanas tienen las esquinas redondeadas de Windows 11.** Como la app dibuja su
  propia barra de título, Windows dejaba de redondearlas y quedaban como un rectángulo
  recto que desentonaba con el resto del escritorio. El redondeo lo pone ahora el propio
  sistema, así que la sombra y el radio son los suyos y desaparecen al maximizar, igual
  que en cualquier otra ventana. En Windows 10 se mantienen rectas, que es su aspecto.

### Corregido

- **Los MP4 salían sin subtítulos, y además no eran MP4 de verdad.** El archivo temporal
  se creaba siempre con extensión `.mkv`, y como ffmpeg decide el formato por la extensión,
  el resultado era un Matroska con el nombre cambiado. Ahora el MP4 es un MP4 y conserva
  los subtítulos de texto. Los de imagen (los de los DVD y Blu-ray) no caben en MP4: se
  descartan avisándote, en vez de tumbar la compresión entera.

## [0.5.0] - 2026-07-21

### Añadido

- **Atajos desde el Explorador de Windows** (se activan en Preferencias → General):
  - **«Abrir con → ShrinkStudio»** en el menú contextual de primer nivel, junto a Fotos o
    Clipchamp, para uno o pocos vídeos.
  - **«Enviar a → ShrinkStudio»** y **«Comprimir con ShrinkStudio»** (en «Mostrar más
    opciones»), y también puedes **arrastrar vídeos o carpetas enteras** a la ventana.
    Estas vías admiten selecciones grandes, que Windows recorta en el menú clásico.
  - Los vídeos llegan a la lista tanto si la app estaba cerrada como abierta, sin duplicar
    ventanas ni filas.

### Cambiado

- **Icono nuevo, en el morado de la app.** El anterior era turquesa y no se parecía al
  logotipo de la propia ventana. Ahora el icono del programa, del instalador, de los
  accesos directos y del repositorio es el mismo glifo morado de la barra de título.
- La herramienta de terminal ocupa ahora **13 MB en vez de 68**, y en Windows se descarga
  como un `.exe` suelto, sin comprimir. En Linux y macOS sigue en `.tar.gz`, que es lo que
  conserva el permiso de ejecución.
- Más información durante la actualización: se ve qué archivo se descarga, con barra de
  progreso, y se avisa de que la app se cerrará para instalar.

### Corregido

- **Los botones del aviso de actualización no respondían.** «Actualizar ahora» y «Después»
  caían dentro de la franja que Windows reserva para arrastrar la ventana, que se tragaba
  los clics.
- **«Buscar actualizaciones» se quedaba colgado en «Buscando…»** cuando sí había versión
  nueva: el mensaje no se actualizaba nunca.
- **Se decía «ya tienes la última versión» aunque no hubiera habido conexión.** Ahora se
  distingue entre estar al día y no haber podido comprobarlo, y se explica el motivo.
- **El actualizador podía descargar el archivo equivocado.** Cogía el primer `.exe` del
  release y, desde que también se publica la herramienta de terminal para Windows, ese
  podía no ser el instalador.
- Al recortar la herramienta de terminal se perdían los tipos con los que se lee la salida
  de ffmpeg y el análisis devolvía datos vacíos. Ahora esos tipos se generan en compilación.

## [0.4.0] - 2026-07-21

### Añadido

- **Pausa automática cuando el disco se llena.** Si te quedas sin espacio, la compresión
  se pausa en lugar de cancelarse o colgarse, y continúa sola en cuanto liberas sitio,
  conservando la cola de archivos pendientes. Puedes seguir usando «Detener» mientras
  está en pausa.
- **Selección estilo explorador en la tabla.** Se procesa lo que esté seleccionado:
  arrastra para seleccionar en banda, o usa Ctrl+clic, Mayús+clic y Ctrl+A.
- **Quitar vídeos de la lista** con la tecla Supr o desde el nuevo menú contextual del
  botón derecho, que además permite enviar el archivo a la papelera, abrir su carpeta y
  copiar la ruta. Quitar de la lista nunca borra el archivo.
- **Barra de menú** con Archivo, Selección, Herramientas y Ayuda.
- **Preferencias por pestañas**: preset e idioma por defecto, qué hacer con los originales
  al terminar, margen mínimo de disco y uso de la aceleración por hardware.
- **Aviso antes de comprimir** para elegir si los originales se envían a la papelera
  según van terminando, con opción de no volver a preguntar.
- **Renombrado de los archivos de salida al estilo PowerRename**: buscar y reemplazar con
  expresiones regulares, contadores, variables de fecha y formato del texto, con vista
  previa en vivo y autocompletado en los campos.
- **Medición real del tamaño final.** El botón «Medir con una muestra» codifica tres
  fragmentos cortos con tus ajustes y calcula el peso de verdad, además de calibrar la
  estimación del resto de la lista.
- **Descripciones emergentes** en los controles, explicando el efecto de cada opción.
- **Versión de línea de órdenes para Linux, macOS y Windows** (`shrinkstudio`), con el
  mismo motor que la app: comprimir, analizar y medir desde la terminal.

### Cambiado

- **Una sola instancia**: si la app ya está abierta y vuelves a lanzarla, se trae al
  frente la ventana existente en vez de abrir otra.
- La estimación de tamaño era demasiado optimista con dibujos animados y material plano;
  ahora parte de una referencia más ajustada y se puede calibrar midiendo.

### Corregido

- La preferencia «analizar subcarpetas» se reactivaba sola al arrancar o al cambiar de
  preset, porque los presets la sobrescribían.
- Los menús se veían con el estilo claro de Windows y con contrastes insuficientes: el
  resalte del elemento activo era casi invisible y el texto de los atajos no llegaba al
  mínimo de accesibilidad AA.
- El texto de la barra de menú aparecía descolocado dentro de su recuadro.

### Notas

- La interfaz gráfica es **solo para Windows** porque usa WPF, que no existe en Linux ni
  macOS. Para esos sistemas se publica la versión de línea de órdenes, que comparte
  exactamente el mismo motor.

## [0.2.1] - 2026-07-20

### Añadido

- Primera versión distribuida con instalador propio y actualización automática desde
  GitHub: comprime a HEVC, H.264 o AV1 en MKV o MP4, conserva los idiomas de audio que
  elijas y nunca toca los archivos originales.
