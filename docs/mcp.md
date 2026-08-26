# Ondine por MCP

Un servidor [MCP](https://modelcontextprotocol.io) que deja a un agente usar Ondine: listar los
vídeos de una carpeta, analizarlos contra un catálogo, renombrar lo que salga seguro y mandar
algo a la papelera.

**Sobre el motor, no sobre la interfaz.** No conduce la ventana a golpe de clic simulado: llama
al mismo `Ondine.Core` que usan la app de escritorio y la de terminal. El agente hace lo mismo
que una persona, por el mismo camino y con las mismas reglas.

## Las tres reglas

Son las de la aplicación, y aquí no se relajan:

1. **Analizar propone.** `ondine_analizar` lee y no escribe nada. Es lo que se lee antes de
   aplicar.
2. **Lo que escribe pide permiso.** Sin `"confirmar": true` la herramienta contesta *lo que
   haría* —la lista entera, fichero a fichero— y no toca el disco.
3. **Lo borrado va a la papelera del sistema.** No hay borrado de verdad, ni con `confirmar`.

Y una cuarta que no es una regla sino un límite: **las dudas no se aplican en bloque.** Si el
análisis deja filas dudosas —un conflicto, un título que no casa—, el renombrado las salta. Eso
se resuelve en la aplicación, con una persona delante.

## Dónde está el binario

| Cómo tengas Ondine | Dónde está |
|---|---|
| `.deb` (Mint, Ubuntu, Debian) | `/usr/bin/ondine-mcp` |
| `.dmg` (macOS) | `/Applications/Ondine.app/Contents/MacOS/ondine-mcp` |
| AppImage | dentro del AppImage, que es un archivo comprimido: no se puede ejecutar desde fuera. Descarga el binario suelto. |
| Instalador de Windows | `%LOCALAPPDATA%\Programs\Ondine\ondine-mcp.exe`, al lado de `Ondine.exe`. |

> **En Windows pesa aparte.** Ahí la app se publica en un solo fichero autocontenido, así que
> el servidor no puede compartir el runtime con ella y lleva el suyo: el instalador engorda unos
> 26 MB. Es el precio de que la app instalada lo traiga sin bajar nada más.

El **binario suelto** sale en cada Release, uno por plataforma:
`ondine-mcp-linux-x64.tar.gz`, `ondine-mcp-macos-arm64.tar.gz`, `ondine-mcp-windows-x64.exe`…
No necesita tener Ondine instalado —lleva el motor dentro— y por eso pesa unos 35 MB.

> **Por qué no pesa 14.** Se probó a recortarlo (`PublishTrimmed`) y revienta al arrancar: el
> recortador se lleva los metadatos que `System.Text.Json` necesita por reflexión. Dentro de los
> paquetes de escritorio no se nota, porque ahí comparte el runtime con la interfaz y ocupa
> 200 kB.

## Registrarlo

En **Claude Code**:

```bash
claude mcp add ondine -- /usr/bin/ondine-mcp
```

En Windows, la misma orden con su ruta:

```bash
claude mcp add ondine -- %LOCALAPPDATA%\Programs\Ondine\ondine-mcp.exe
```

En **Claude Desktop**, en `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ondine": {
      "command": "/usr/bin/ondine-mcp"
    }
  }
}
```

Cambia la ruta por la que te corresponda de la tabla de arriba.

## Comprobar que funciona

Sin cliente ninguno, desde un terminal:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | ondine-mcp
```

Tiene que contestar una línea de JSON con las herramientas dentro. Y para leerlas a ojo:

```bash
ondine-mcp --herramientas
```

El servidor habla JSON-RPC por la entrada estándar y **solo** protocolo por la salida; los
mensajes de diagnóstico van por el error estándar, que es lo que deja verlos sin romper la
conversación:

```bash
ondine-mcp --herramientas 2>/dev/null
```

## Las herramientas

### ondine_listar_videos

Los vídeos de una carpeta, con su tamaño. No toca nada.

- `carpeta` *(obligatorio)* — la carpeta a mirar.
- `subcarpetas` — si se recorren. Por defecto sí.

### ondine_analizar

Compara los vídeos con un catálogo de episodios y **propone** un nombre para cada uno. Devuelve
en qué estado queda cada fichero (limpio, corregido, duda, sin identificar), con qué episodio ha
casado y por qué.

- `carpeta` *(obligatorio)*
- `catalogo` *(obligatorio)* — el `.json` del catálogo de la serie.
- `subcarpetas` — por defecto sí.
- `plantilla` — el patrón del nombre. Por defecto `<serie> - S<temp>E<num> - <título>`.

### ondine_aplicar_renombrado

Renombra lo que el análisis dio por seguro. Las dudas se quedan como están.

- `carpeta`, `catalogo`, `subcarpetas`, `plantilla` — como en `ondine_analizar`.
- `confirmar` — `true` para renombrar de verdad. Sin él, dice lo que haría.

Lo aplicado se puede **deshacer** desde la aplicación, en Organizar: el renombrado guarda su
parte igual que cuando lo lanza la ventana.

### ondine_comprimir

Comprime, con **todos los mandos de la pantalla de Comprimir**. Los originales no se tocan: el
resultado va a otra carpeta.

- `carpeta` o `ficheros` — una carpeta entera, o rutas concretas.
- `subcarpetas` — por defecto sí. `limite` — como mucho, ese número de vídeos.
- `salida` — carpeta de destino. Por defecto, una «comprimido» junto a cada original.
- `formato` — `mkv` (por defecto), `mp4`, `webm`, o solo audio: `mp3`, `m4a`, `flac`, `opus`.
- `codec` — `hevc` (por defecto), `h264`, `av1`. Es el **formato** de salida.
- `codificador` — **con qué** se codifica: `software` para el mejor por software, o un nombre
  (`libx265`, `libsvtav1`, `hevc_nvenc`…). Vacío = lo elige la app. Los de GPU son rápidos y
  comprimen bastante menos: para archivar, `software`.
- `calidad` — CRF de 18 a 35, o 0 para automática (27 en hardware, 23 en software).
- `esmero` — `muy_rapido`, `rapido`, `equilibrado`, `lento`, `muy_lento`.
- `alto` — reescala si supera esa altura. `tamano_objetivo_mb` — apunta a ese tamaño y manda
  sobre la calidad.
- `audio_codec` — `copiar` (por defecto), `aac`, `ac3`, `eac3`, `opus`, `flac`.
  `audio_kbps` puesto a solas recodifica a AAC. `audio_estereo` baja lo que traiga más canales.
- `idioma`, `idiomas`, `subtitulos`, `sin_subtitulos` — qué pistas se conservan. En `idiomas`,
  `all` conserva todas, incluidas las que no traen etiqueta de idioma.
- `forzar`, `hardware`, `aceleracion`, `margen_disco_mb` — lo que en la app vive en Preferencias.
- `confirmar` — `true` para comprimir de verdad.

Sin `confirmar` devuelve el pronóstico fichero a fichero, con lo que pesa hoy cada uno y lo que
se prevé que pese, más el resumen de los ajustes que se van a aplicar.

> **Tarda lo que tarde el vídeo.** Una temporada entera puede ser una hora larga, y la llamada no
> contesta hasta el final. Para ir por tandas, `limite`.

### ondine_medir

Codifica tres muestras cortas del fichero con los ajustes que le des y saca de ahí el tamaño
real. Es el «Medir con una muestra» de la app, y es lo que conviene usar antes de una tanda
grande: el pronóstico de `ondine_comprimir` es un modelo, esto es una medida. No escribe nada.

- `fichero` *(obligatorio)*, y los mismos mandos de codificación que `ondine_comprimir`.

### ondine_a_la_papelera

Manda un fichero a la papelera del sistema.

- `ruta` *(obligatorio)*
- `confirmar` — `true` para mandarlo de verdad.

### ondine_preferencias

Lee las Preferencias: idioma, preset por defecto, idioma de audio, qué hacer con el original tras
comprimir, margen de disco, aceleración por hardware, y los ajustes del modelo y de TMDb.

De las **claves** solo dice si hay una puesta. Su valor no sale de la máquina.

### ondine_ajustar_preferencias

Cambia lo que le pases y **solo** lo que le pases: el resto se queda como estaba, incluido lo que
esta herramienta no ofrece (el historial de renombrado, el factor de complejidad que la app
aprende midiendo).

- `idioma_app`, `preset_por_defecto`, `idioma_audio`, `subcarpetas`, `buscar_actualizaciones`
- `tras_comprimir` — `preguntar`, `papelera` o `conservar`
- `margen_disco_mb`, `hardware`, `codificador`, `aceleracion`
- `modelo_activo`, `modelo_url`, `modelo_nombre`, `peliculas_activo`
- `confirmar` — sin él, contesta el **antes y el después** de cada cosa que cambiaría

> **Las claves del modelo y de TMDb no se pueden poner desde aquí, a propósito.** Habría que
> escribirlas en el chat para llegar hasta el servidor. Eso se hace en la ventana de Preferencias.

Y los mandos de compresión heredan lo que haya guardado: si no pasas `hardware`, `aceleracion`,
`idioma` o `margen_disco_mb`, `ondine_comprimir` usa los de Preferencias, igual que la ventana.

### ondine_donde_guarda

Dónde guarda Ondine sus datos —catálogos, decisiones, ajustes— y qué herramientas externas
encuentra (ffmpeg, ffprobe). Útil antes de intentar nada.

## Lo que todavía no hace

- **Recortes**: partir un vídeo en trozos o cortar un fragmento. El motor lo sabe hacer y no está
  expuesto aquí todavía.
- **Organizar, a medias**: `ondine_analizar` propone y `ondine_aplicar_renombrado` aplica lo
  seguro, que es el camino de la mayoría. Lo que queda fuera son las decisiones fila a fila: fijar
  a mano el episodio de una duda, marcar un fichero como «dejar como está» o deshacer una tanda
  aplicada. Eso sigue pidiendo la ventana.
- **La vista previa** de diez segundos, que no significa nada sin alguien mirándola, y **la cola**,
  porque encadenar llamadas ya es la forma de hacer cola de un agente.

## Lo que vigila el harness

Que esto no se quede atrás cuando la app siga cambiando. Las pruebas
(`tests/Reindex.Tests/HerramientasMcpTests.cs` y `ElMcpNoSeQuedaAtrasTests.cs`) fallan si:

- alguna herramienta cambia de nombre, aparece o desaparece **y este documento no se entera**;
- un mando de la pantalla de Comprimir no se puede pedir por MCP (se compara el esquema de
  `ondine_comprimir` contra las propiedades de `EncodeOptions`, y lo que se deja fuera va en una
  lista de exentos **con su motivo escrito**);
- una herramienta que escribe deja de declarar `confirmar`;
- `analizar` toca un fichero;
- la versión del `.csproj` del servidor se desengancha de la del resto;
- un paquete de escritorio deja de llevarlo dentro;
- alguien le quita la globalización al motor (`InvariantGlobalization`), que hunde la
  identificación de títulos con acentos sin dar ningún error.
