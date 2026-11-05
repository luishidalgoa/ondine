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
