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
| Instalador de Windows | no lo trae: descarga el binario suelto. |

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

### ondine_a_la_papelera

Manda un fichero a la papelera del sistema.

- `ruta` *(obligatorio)*
- `confirmar` — `true` para mandarlo de verdad.

### ondine_donde_guarda

Dónde guarda Ondine sus datos —catálogos, decisiones, ajustes— y qué herramientas externas
encuentra (ffmpeg, ffprobe). Útil antes de intentar nada.

## Lo que vigila el harness

Que esto no se quede atrás cuando la app siga cambiando. Las pruebas
(`tests/Reindex.Tests/HerramientasMcpTests.cs` y `ElMcpNoSeQuedaAtrasTests.cs`) fallan si:

- alguna herramienta cambia de nombre, aparece o desaparece **y este documento no se entera**;
- una herramienta que escribe deja de declarar `confirmar`;
- `analizar` toca un fichero;
- la versión del `.csproj` del servidor se desengancha de la del resto;
- un paquete de escritorio deja de llevarlo dentro;
- alguien le quita la globalización al motor (`InvariantGlobalization`), que hunde la
  identificación de títulos con acentos sin dar ningún error.
