# Escribir un complemento para Ondine

Un complemento es **una carpeta con un `plugin.json` dentro**. Se instala
copiándola y se desinstala borrándola. No hay registro que actualizar ni lista
que mantener: en cuanto haya que apuntarlos en algún sitio, alguien se olvidará
y la lista empezará a mentir.

```
%APPDATA%\Ondine\complementos\
  mi-complemento\
    plugin.json
    lo-que-sea.exe
```

Hay uno de verdad en [`ejemplos/complemento-youtube`](../ejemplos/complemento-youtube), que
además es el que sirve la tienda: se lee entero en un rato y hace todo lo que
describe este documento.
No baja nada: escribe por la salida estándar lo mismo que escribiría uno de
verdad, para poder probar la pantalla sin depender de nada.

---

## Por qué son procesos y no librerías

Un complemento **no se carga dentro de Ondine**: se ejecuta como un programa
aparte y se habla con él por texto. Tiene tres motivos, y ninguno es de gusto:

- Ondine no tiene **ni una dependencia de NuGet** a propósito. Un sistema de
  complementos en proceso pide contratos versionados, contextos de carga y, a los
  dos meses, una librería que lo gestione.
- La herramienta de terminal se publica en **fichero único y recortado**. El
  recortador se lleva por delante los tipos que solo se usan por reflexión, que
  es justo como se cargaría un complemento. Ya pasó con los modelos de `ffprobe`.
- **El precedente está en casa**: `ffmpeg` y `ffprobe` se invocan así desde el
  primer día.

Lo que se gana: un complemento que se cuelga no se lleva la aplicación, y se
puede escribir en lo que sea. Lo que se paga: hay que hablar por texto.

---

## El manifiesto

```json
{
  "nombre": "YouTube",
  "descripcion": "Trae vídeos de una lista y los deja listos para identificar",
  "version": "1.0.0",
  "autor": "quien lo escriba",
  "ejecutable": "traer.cmd",
  "capacidades": ["importar", "descargar"],
  "ambito": ["organizar"],
  "integracion": "propia",
  "contrato": 1
}
```

| Campo | Qué es |
|---|---|
| `ejecutable` | **Relativo a su carpeta.** Una ruta absoluta deja de funcionar en cuanto la carpeta se copia a otro equipo, que es lo que se hace para compartirla. |
| `ejecutable_linux` | Opcional. El programa en Linux, cuando el general no vale. Para el caso normal **no hace falta** — ver «El mismo complemento en los tres sistemas». |
| `ejecutable_macos` | Opcional. Lo mismo para macOS. Sin declararlo, macOS sigue el camino general. |
| `argumentos` | Opcional. Van siempre delante del subcomando. |
| `capacidades` | Qué sabe hacer. Hoy: `importar` (leer una fuente y cotejarla con el catálogo) y `descargar` (traer ficheros al disco). Se **declara** en vez de deducirse llamándolo: abrir el menú no puede lanzar un proceso por cada complemento instalado. |

> **`importar` y `descargar` son cosas distintas, y hay que declararlas por separado.**
> Un complemento puede leer una lista y cotejarla sin bajar nada. Cuando la app deducía
> una capacidad de la otra, pintaba un botón «Descargar» aunque el complemento rechazase
> esa orden: al pulsarlo no pasaba nada que se viera. **Sin `descargar` declarado, el
> botón no existe.** El ejemplo de YouTube declara las dos porque implementa las dos.
| `ambito` | Dónde aplica. Vacío o `["global"]` = toda la aplicación. Si no, los modos: `organizar`, `comprimir`, `recortes`. Se pueden declarar varios. |
| `integracion` | `propia` (lo normal) o `nativa`. |
| `contrato` | La versión del contrato. Hoy `1`. |
| `modelo` | Opcional, `false` por defecto. Declara que **sabe** preguntarle a un modelo de lenguaje cuando sus reglas no resuelven algo. Declararlo **no da permiso**: solo hace que Ondine ofrezca dárselo. |

### Ámbito

Se declara en vez de deducirse de lo que hace. Un complemento que trae vídeos de
fuera sirve tanto dentro de Organizar —para llenar huecos del catálogo— como
suelto, y **solo su autor sabe cuál de las dos cosas quería**.

Los que declaran modos solo salen en los suyos. Enseñar todo en todas partes
convierte el botón de complementos en un cajón: con quince instalados, encontrar
el que sirve aquí cuesta más que abrirlo a mano.

### Integración

- **`propia`** — tiene su propio panel detrás del botón de complementos, como
  una extensión de navegador. **Es lo normal, y el valor por defecto.**
- **`nativa`** — se mete en la interfaz de la aplicación.

El defecto es `propia` a propósito: un complemento que reordena la pantalla de
quien lo instala tiene que ser una decisión consciente, no lo que pasa por no
escribir un campo.

### El mismo complemento en los tres sistemas

Un complemento se escribe casi siempre en Windows y declara `"ejecutable": "algo.cmd"`.
En Linux y en macOS eso **no es un programa**: el sistema contesta «permission denied»
—o «exec format error», que se entiende aún menos— y el complemento aparece instalado,
en su sitio, sin arrancar.

Así que Ondine resuelve qué ejecutar **según dónde corre**, y en el caso normal el
manifiesto no tiene que decir nada:

1. Si el sistema tiene su campo (`ejecutable_linux`, `ejecutable_macos`), manda ese.
2. Si no, manda `ejecutable`.
3. Y si en Unix eso es un `.cmd` o un `.bat`, se busca **al lado y con el mismo nombre**
   un `.sh` —que gana— o un `.py`.

> **Por qué esto y no un campo obligatorio por sistema.** El patrón normal es un
> envoltorio `.cmd` de tres líneas que llama a un script donde está todo el trabajo —el
> ejemplo de YouTube que trae Ondine es exactamente eso—. Obligar a repetir el mismo
> arranque tres veces para el caso de siempre convierte una regla en papeleo. Los campos
> por sistema son para el complemento que trae un programa **distinto de verdad**: un
> binario compilado por plataforma.

**Las barras del manifiesto dan igual.** Un `"ejecutable": "sub\\app.cmd"` escrito en Windows
vale en Linux y en macOS: la barra invertida se traduce antes de nada. Sin eso, en Unix esa barra
**no separa carpetas** —es un carácter más del nombre— y el complemento quedaba descartado por
«solo funciona en Windows» cuando lo único que pasaba era que nadie había traducido la barra. El
precio: un fichero de Unix con una barra invertida *en el nombre* —legal, aunque nadie lo hace— no
se encontraría.

**Un `.py` se ejecuta con el intérprete**, en los tres sistemas. No es lo mismo que un
`.sh`: en Unix haría falta que trajera almohadilla-bang y en Windows depende de una
asociación de ficheros que aquí no se usa. Se buscan **todas** las apariciones de
`python`/`python3` en el `PATH` y se coge **la primera que conteste** a `--version`; si no contesta
ninguna, el complemento queda descartado diciendo justo eso —el arreglo es instalar Python, no
tocar el complemento—.

> **Por qué se le pregunta en vez de mirarlo.** En Windows, la carpeta `WindowsApps` suele ir por
> delante en el `PATH` y trae alias de la Tienda para `python.exe` y `python3.exe`. Pesan **cero
> bytes**: si hay Python detrás, arrancan Python; si no, abren la Tienda y el complemento no se
> ejecuta nunca. No se distinguen mirándolos —se midió: un alias de cero bytes contestó «Python
> 3.14.3»—, así que descartarlos por el tamaño rompería instalaciones que funcionan. Lo único que
> separa a uno bueno de uno malo es preguntárselo.

**El bit de ejecución se pone solo.** Un `.zip` no guarda permisos de Unix —y menos uno
hecho en Windows—, así que el `.sh` sale de la instalación sin poder lanzarse: en su
sitio, con el contenido bueno, y «permission denied» al pulsar. Ondine se lo pone al
instalar y otra vez justo antes de arrancarlo, que es lo que cubre al complemento
copiado a mano. Se lo pone **a los `.sh` y a su propio programa**, no a todo lo
extraído: un `.json` de datos no se ejecuta.

**Y nunca a un enlace.** `chmod` sigue el enlace y cambia los permisos del *otro* fichero, así que
un complemento que trajera dentro un enlace a algo del sistema conseguiría que Ondine le pusiera
permiso de ejecución a lo apuntado. Ni se les da permiso ni se entra por las carpetas enlazadas al
buscar los `.sh`.

### Lo que se rechaza, y por qué

Un manifiesto no entra si:

- **Su ejecutable apunta fuera de su carpeta.** No es un complemento mal escrito:
  es uno pidiendo ejecutar cualquier cosa del disco. Se comprueba sobre la ruta
  ya resuelta, porque `../../Windows/System32/cmd.exe` solo se ve por lo que es
  después de combinarla. **Vale para los tres campos y se comprueba en los tres
  sistemas**: un `ejecutable_linux` que se sale colaría en Windows —donde ese campo ni
  se mira— y el mismo paquete quedaría aceptado en una máquina y rechazado en otra.
- **No hay nada que ejecutar en este sistema.** Un `.cmd` sin `.sh` ni `.py` al lado,
  visto desde Linux, se descarta **diciendo qué falta**: «no funciona aquí» dejaría a su
  autor sin nada que hacer, y lo que hay que hacer son tres líneas de script.
- Declara un **modo que no existe**. No se ignora en silencio: el complemento no
  saldría en ninguna parte y su autor lo daría por instalado. Callarlo convierte
  una errata en un fantasma.
- Declara una **integración** que no es `propia` ni `nativa`.
- Habla un **contrato** que esta versión no entiende.
- No declara ninguna capacidad, o su programa no está.

Todos los rechazos **se enseñan con su motivo** en la pantalla de complementos.
Uno que no aparece y no dice por qué es peor que uno que no está.

---

## El contrato

Ondine ejecuta el programa **en su propia carpeta** —así puede traer sus cosas al
lado y buscarlas por ruta relativa— y le pasa un subcomando:

```
<ejecutable> [argumentos fijos] listar [fuente]
<ejecutable> [argumentos fijos] traer <id> <id> ... --destino <carpeta>
```

El complemento contesta por la **salida estándar**: **una línea, un mensaje,
JSON**.

```json
{"tipo":"elemento","id":"abc","titulo":"El gorro de la suerte","miniatura":"https://...","duracion":662}
{"tipo":"progreso","avance":0.42,"texto":"Bajando 3 de 7"}
{"tipo":"hecho","ficheros":["C:\\...\\uno.mkv"]}
{"tipo":"error","mensaje":"La fuente no responde"}
```

Una línea por mensaje y no un JSON al final: traer cuarenta vídeos tarda minutos,
y con una única respuesta la aplicación se queda muda todo ese rato sin forma de
saber si avanza o se ha colgado.

| Tipo | Cuándo | Campos |
|---|---|---|
| `elemento` | uno por cosa encontrada | `id`, `titulo`, `miniatura`, `duracion` (segundos) |
| `progreso` | mientras trabaja | `avance` (0 a 1), `texto` |
| `hecho` | al terminar | `ficheros` (lo que dejó en disco) |
| `error` | cuando algo falla | `mensaje` |

### Reglas que conviene saber

- **Lo que no sea JSON válido se ignora.** Las herramientas que se envuelven son
  habladoras (`[download] 42.0% of 55MiB`) y un complemento no debería romperse
  por eso. Puedes escribir lo que quieras por en medio.
- **Un mensaje sin `tipo` se descarta.** Adivinarlo por los campos que trae sería
  inventarse el contrato en tiempo de ejecución.
- **El error estándar no se interpreta**, pero sí se lee. No hace falta que lo
  evites.
- **Sal con código 0 si todo fue bien.** Un código distinto de cero sin un mensaje
  de `error` propio se reporta como «se murió sin explicarse», porque callarlo
  dejaría la lista a medias pareciendo que eso era todo lo que había.
- **La duración va en segundos**, no en texto. Formatear es cosa de quien pinta.

---

## Preguntarle a un modelo de lenguaje

Si tu complemento declara `"modelo": true`, puede preguntarle a un modelo cuando
sus propias reglas no bastan. **Tú no ves la clave.** Preguntas por tu salida,
Ondine llama, y te devuelve **solo la respuesta** por tu **entrada estándar**.

```json
{"tipo":"preguntar","id":"7","texto":"¿Qué episodio es «El gorro de la suerte»?"}
```

Ondine te escribe una línea por tu entrada, con el mismo `id`:

```json
{"tipo":"respuesta","id":"7","texto":"El 413"}
{"tipo":"respuesta","id":"7","mensaje":"Este complemento no tiene permiso para usar el modelo conectado."}
```

**Siempre llega una respuesta**, también cuando es que no. Nunca te quedas
esperando una línea que no viene.

### Por qué así y no dándote la clave

Sería más simple pasarte la dirección y la clave por una variable de entorno y
que llamaras tú. Pero eso es entregarle a un programa de fuera la cuenta de quien
te instaló. El puente cuesta veinte líneas y hace que el peor caso —un
complemento con mala idea— sea «gasta cuarenta preguntas» y no «se lleva tu
clave».

### Lo que hay que saber

- **El permiso se da a mano, uno a uno, y empieza apagado.** Está en tu ficha
  dentro del panel de complementos. Quitarlo surte efecto en la siguiente
  llamada.
- **Cupo: 40 preguntas por ejecución.** Esto cuesta dinero de verdad; sin cupo,
  un bucle mal escrito vacía el saldo de alguien mientras mira una barra de
  progreso. Pregunta solo por lo que tus reglas no resuelvan, no por cada
  elemento.
- **Límite de 8000 caracteres por pregunta**, por lo mismo.
- **La instrucción de sistema la pone Ondine**, no tú, y va en el idioma de la
  app. Pide respuestas breves y literales, y `NO LO SÉ` cuando no lo sepa.
- **Comprueba lo que te conteste.** Un modelo acierta casi siempre y se inventa
  el resto con el mismo aplomo. Si la respuesta se puede contrastar con algo que
  ya tienes —el catálogo, la duración, la fecha—, contrástala antes de usarla; y
  si no casa, mejor no proponer nada que proponer algo inventado.

---

## Qué pasa después de traer

Cuando devuelves `hecho`, Ondine ofrece **llevar la carpeta a Organizar** para
identificar lo traído contra el catálogo. La carpeta la elige quien trae, no tú:
es su biblioteca, y un destino decidido por un programa de fuera es lo último que
se quiere de algo que baja ficheros. Te llega en `--destino`.

Puedes dejar las cosas en subcarpetas por temporada si quieres: Ondine apunta al
destino que se eligió, no a la carpeta del primer fichero.

## El cotejo con el catálogo

Esto es lo que hace útil un complemento de importación, y **no cuesta nada al que
lo escribe**: basta con devolver títulos razonables.

Ondine coteja lo que devuelvas contra el catálogo abierto en Organizar y marca
cada elemento como **ya lo tienes**, **te falta la historia b**, **te falta** o
**no se sabe**. Usa el mismo motor de identificación que resuelve los ficheros
del disco, así que cuanto más se parezca tu `titulo` al del catálogo, mejor sale.

Por debajo del umbral no se afirma nada. Decir «te falta» sobre algo que ya
tienes te lo hace bajar dos veces; decir «ya lo tienes» sobre algo que no, te lo
hace perder.

### Cuando una cosa trae dos historias

Muchas series de media hora meten dos episodios en un vídeo. Si lo sabes, **une
los dos títulos con ` + `**:

```json
{"tipo":"elemento","id":"abc","titulo":"El controlador del mar + Alquiler estilo futurista"}
```

Es el mismo separador que usa Ondine por dentro, así que su motor lo vuelve a
partir solo y coteja las dos historias por separado. Sin esto lo da por un
episodio y **la mitad que falta no la reclama nadie**.

Aviso, por experiencia con el de YouTube: el segundo título suele estar en un
sitio distinto del primero —en la descripción y no en el título— y ahí es fácil
recoger basura. Merece la pena **comprobar** en vez de interpretar: si ese otro
texto contiene todos los trozos del título y además alguno más, es el mismo
título escrito largo y lo que sobra son historias. Si no lo contiene, no es
comparable y manda el título. Esa comprobación es lo que hace que la regla
aguante en una fuente que no habías visto, en vez de acertar solo donde la
probaste.

---

## Lo que un complemento no debe hacer

Ondine ejecuta lo que le pongas en esa carpeta, así que la responsabilidad es de
quien lo escribe y de quien lo instala. Dicho eso, hay una línea clara:

**No se aceptan complementos cuyo propósito sea saltarse restricciones de acceso
o de descarga** —sesiones prestadas, tokens de terceros, medidas de protección de
contenido con derechos—. Que la herramienta lo permita técnicamente no lo hace
parte del proyecto.

Traer contenido propio, con licencia abierta, o por las vías que el propio
servicio ofrece: eso sí.

Y lo mismo con el modelo: se te da para resolver dudas de identificación, no para
que le mandes lo que haya en el disco de quien te instaló.
