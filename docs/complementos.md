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

Hay uno de ejemplo en [`ejemplos/complemento-demo`](../ejemplos/complemento-demo).
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
  "capacidades": ["importar"],
  "ambito": ["organizar"],
  "integracion": "propia",
  "contrato": 1
}
```

| Campo | Qué es |
|---|---|
| `ejecutable` | **Relativo a su carpeta.** Una ruta absoluta deja de funcionar en cuanto la carpeta se copia a otro equipo, que es lo que se hace para compartirla. |
| `argumentos` | Opcional. Van siempre delante del subcomando. |
| `capacidades` | Qué sabe hacer. Hoy: `importar`. Se **declara** en vez de deducirse llamándolo: abrir el menú no puede lanzar un proceso por cada complemento instalado. |
| `ambito` | Dónde aplica. Vacío o `["global"]` = toda la aplicación. Si no, los modos: `organizar`, `comprimir`, `recortes`. Se pueden declarar varios. |
| `integracion` | `propia` (lo normal) o `nativa`. |
| `contrato` | La versión del contrato. Hoy `1`. |

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

### Lo que se rechaza, y por qué

Un manifiesto no entra si:

- **Su ejecutable apunta fuera de su carpeta.** No es un complemento mal escrito:
  es uno pidiendo ejecutar cualquier cosa del disco. Se comprueba sobre la ruta
  ya resuelta, porque `../../Windows/System32/cmd.exe` solo se ve por lo que es
  después de combinarla.
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
