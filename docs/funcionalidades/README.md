# Mapa de funcionalidades

Una ficha por bloque de funcionalidad. Antes de tocar un bloque, lee su ficha: dice qué parte del
código le afecta, qué pruebas lo cubren y qué huecos se conocen. Así un cambio no se queda a medias
en un módulo que no se miró.

## Fichas

| Bloque | Qué hace |
|---|---|
| [Motor de compresión](motor-de-compresion.md) | Sondea, elige el codificador, comprime y previsualiza con ffmpeg. |
| [Cola de trabajos y panel Comprimir](cola-y-panel-comprimir.md) | Encola tandas con sus ajustes y la tabla que las muestra. |
| [Organizar](organizar.md) | Identifica, renombra y reordena una biblioteca de series. |
| [Recortes y partir](recortes-y-partir.md) | Corta sin recodificar y divide un vídeo en tramos. |
| [Pistas](pistas.md) | Quita pistas de audio y subtítulos sin recomprimir. |
| [Películas](peliculas.md) | Biblioteca de películas, identificación en TMDb y decisiones. |
| [Complementos y tienda](complementos-y-tienda.md) | Extensiones de proceso y su tienda. |
| [Preferencias y ajustes](preferencias.md) | Ajustes persistidos y la ventana que los cambia. |
| [Servidor MCP](mcp.md) | Herramientas para que un agente use Ondine. |
| [Línea de comandos](cli.md) | La CLI de terminal, sobre el mismo motor. |
| [Arranque y actualización](arranque-y-actualizacion.md) | Una sola instancia, actualizaciones y arranque de cada interfaz. |
| [Papelera y avisos](papelera-y-avisos.md) | Papelera del sistema, papelera propia y avisos al terminar. |
| [IA](ia.md) | El modelo que sugiere cosas, local o remoto. |
| [Sugerencias](sugerencias.md) | Propuestas que la app hace al usuario. |
| [Localización](localizacion.md) | Textos bilingües: cómo se añade uno y qué comprueba el harness. |

## Qué lleva cada ficha

Una cabecera YAML con `funcion`, `modulos`, `pruebas` y `tareas`; un diagrama Mermaid de sus piezas;
los huecos conocidos. Un módulo es un fichero (`ruta/fichero.cs`) o una carpeta (`ruta/carpeta/`).

## Reglas

1. **Una ficha se actualiza solo cuando el cambio está fusionado en la rama principal**, nunca desde
   la rama de trabajo. Mientras el cambio no esté en `main`, la ficha describe `main`.
2. **Un bloque sin ficha se crea**. Lo que no tiene ficha aparece como aviso en el harness.
3. **El mapa no puede mentir**: si una ficha nombra un módulo o una prueba que no existe, el harness
   falla.

## Cómo se comprueba

```bash
dotnet run --project tools/Verificar
```

El paso 3 del harness (`tools/Verificar/Mapa.cs`) hace tres cosas:

- **Falla** si una ficha nombra un módulo o una prueba que no existen.
- **Avisa** si un módulo cambia en esta rama y su ficha no.
- **Avisa** si hay código del motor, la CLI o el MCP que todavía no tiene ficha.

Los avisos no hacen fallar el comando; son trabajo pendiente del mapa.
