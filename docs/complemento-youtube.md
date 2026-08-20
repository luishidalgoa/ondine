# Complemento YouTube

Documento de continuidad para desarrollar, diagnosticar y publicar el complemento
de YouTube de Ondine sin depender del historial de una conversación.

## Dónde vive

- Fuentes publicables: `ejemplos/complemento-youtube/`.
- Manifiesto: `ejemplos/complemento-youtube/plugin.json`.
- Implementación: `ejemplos/complemento-youtube/youtube.py`.
- Lanzador para Windows: `ejemplos/complemento-youtube/youtube.cmd`.
- Pruebas: `ejemplos/complemento-youtube/test_youtube.py`.
- Índice de la tienda: `complementos/indice.json`.
- Release permanente de GitHub: etiqueta `complementos`.

El ZIP publicado lleva esos cuatro archivos del complemento en la raíz. Cada
versión usa un nombre nuevo (`youtube-X.Y.Z.zip`): un asset publicado nunca se
reemplaza porque la CDN de GitHub puede seguir sirviendo los bytes antiguos.

## Contrato con Ondine

El manifiesto declara `importar` y `descargar`. Eso hace que Ondine ofrezca los
comandos siguientes:

```text
youtube.cmd listar <URL de playlist>
youtube.cmd traer <id> <id> ... --destino <carpeta>
```

El complemento escribe un objeto JSON por línea en UTF-8:

- `elemento`: vídeo encontrado, con ID, título, miniatura y duración.
- `progreso`: texto y `avance` entre 0 y 1.
- `hecho`: rutas finales que se dejaron en disco.
- `error`: fallo que impide completar la operación.
- `preguntar`: consulta opcional al modelo conectado, sin recibir su clave.

La aplicación decide la carpeta de destino. El complemento valida que exista y
que todos los IDs tengan la forma cerrada de un ID de vídeo de YouTube; nunca
acepta una URL o una ruta como ID.

## Cómo lista y coteja

`listar` ejecuta `yt-dlp --flat-playlist -J`. Una lista parcialmente bloqueada
puede traer un JSON válido y a la vez un código de error: el JSON se aprovecha
antes de decidir que toda la lista falló.

Las entradas con título y sin una restricción explícita se entregan a Ondine.
Las privadas, Premium, exclusivas para miembros, las que requieren sesión y los
huecos sin título no se convierten en episodios. El diagnóstico cuenta esos
huecos y muestra la causa solo cuando YouTube la comunica; si no, dice
«eliminados, privados o bloqueados» sin inventar cuál de las tres es.

Después se leen las descripciones porque algunos vídeos contienen dos historias
y el título solo nombra una. El parser únicamente incorpora historias cuando la
primera línea de la descripción contiene el título conocido y aporta segmentos
adicionales. Los casos ambiguos pueden preguntarse al modelo si el usuario dio
permiso; la respuesta se valida y nunca se acepta a ciegas.

## Cómo descarga

`traer` recibe exclusivamente los IDs marcados en la interfaz y los procesa uno
a uno. Usa este orden de formatos:

```text
best[height<=480]/bestvideo[height<=480]+bestaudio/best
```

Primero intenta un formato completo de hasta 480p, que no necesita combinar
pistas. Si no existe, permite vídeo de hasta 480p más audio; `yt-dlp` usa ffmpeg
para unirlos. El último `best` mantiene compatibilidad con vídeos cuya altura no
está declarada.

El nombre de salida es `Título [ID].ext`. `yt-dlp` comunica la ruta definitiva
con una marca privada después de cualquier unión o cambio de contenedor. Solo
esas rutas se devuelven a Ondine.

El progreso de `yt-dlp` sale por otra marca privada. El complemento transforma
el porcentaje del vídeo actual al avance total del lote:

```text
avance = (índice del vídeo + porcentaje / 100) / total
```

Ondine muestra el texto, el porcentaje y una barra. Si un vídeo falla pero otro
se descargó, conserva y entrega los correctos y resume cuántos no estaban
disponibles. Si ninguno se pudo descargar, emite `error` con el último detalle
de cada ID.

## Límite importante

Que `listar` obtenga título, miniatura y duración no implica que el contenido
sea descargable. La playlist de Doraemon
`PLeR3zeCU2xypIMeCpVpt7LFn-KMTycS2m` expone metadatos de algunos vídeos, pero al
pedir formatos responde `This video is not available` y solo llega a enseñar
storyboards. No existe formato de vídeo a 480p que el complemento pueda elegir.

El complemento no usa cookies, sesiones prestadas, cambios de región ni otros
mecanismos para saltarse restricciones. Descarga contenido que YouTube ofrece
públicamente; un bloqueo de origen se informa y se conserva.

## Cómo verificar

```powershell
python -m unittest discover -s ejemplos/complemento-youtube -p 'test_*.py'
dotnet run --project tests/Reindex.Tests
dotnet run --project tests/Ui.Smoke
```

Para una prueba real autorizada se puede usar el spot público de Ondine,
`L8F6kxHy2z8`. La playlist de Doraemon sirve para verificar el diagnóstico de
contenido no disponible, no una descarga correcta.

Antes de publicar se prueba el ZIP extraído en una carpeta vacía, se calcula su
SHA-256 y tamaño, se actualiza `complementos/indice.json`, se descarga de nuevo
desde GitHub y se comprueba que los bytes remotos coincidan con el índice.

