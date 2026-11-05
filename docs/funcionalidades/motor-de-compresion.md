---
funcion: Motor de compresión
modulos:
  - src/Ondine.Core/Engine.cs
  - src/Ondine.Core/Ondine.Core.csproj
  - src/Ondine.Core/EncodeOptions.cs
  - src/Ondine.Core/Estimator.cs
  - src/Ondine.Core/Preset.cs
  - src/Ondine.Core/Native.cs
  - src/Ondine.Core/MotivoDeFfmpeg.cs
  - src/Ondine.Core/Objetivo/
  - src/Ondine.Core/Audio/
pruebas:
  - tests/Reindex.Tests/ComprimirPorMcpTests.cs
  - tests/Reindex.Tests/ElCodificadorElegidoTests.cs
  - tests/Reindex.Tests/ElInterruptorDeHardwareTests.cs
  - tests/Reindex.Tests/AceleracionDeVideoTests.cs
  - tests/Reindex.Tests/ArgumentosDeBitrateTests.cs
  - tests/Reindex.Tests/VelocidadDelCodificadorTests.cs
  - tests/Reindex.Tests/TamanoObjetivoTests.cs
  - tests/Reindex.Tests/MotivoDeFfmpegTests.cs
  - tests/Reindex.Tests/FfmpegEnMacTests.cs
  - tests/Reindex.Tests/CodecDeAudioTests.cs
  - tests/Reindex.Tests/MezclaDeAudioTests.cs
  - tests/Reindex.Tests/PlanDeAudioTests.cs
  - tests/Reindex.Tests/LoQueDiceElAudioTests.cs
  - tests/Reindex.Tests/PistasQueSeQuedanTests.cs
tareas:
  - ROADMAP.md › Comprimir — presets y formatos
  - ROADMAP.md › Comprimir — audio y subtítulos
  - ROADMAP.md › Foco: ahorro de almacenamiento
---

# Motor de compresión

**Qué hace:** convierte un vídeo en otro más pequeño con ffmpeg: sondea el fichero, elige el
codificador que arranca de verdad en esta máquina, calcula el bitrate o el tamaño objetivo,
decide qué pistas de audio y subtítulos se quedan, y comprime. También sirve la previsualización
y la medición de bitrate.

## Piezas y dependencias

```mermaid
flowchart LR
  WPF["MainWindow (WPF)"] --> ENG
  AVA["VentanaPrincipal (Avalonia)"] --> ENG
  MCP["Ondine.Mcp · Comprimir"] --> ENG
  CLI["Ondine.Cli"] --> ENG
  COLA["Cola de trabajos"] -- "copia de EncodeOptions" --> OPT
  ENG["Engine.cs<br/>ProbeAsync · CompressAsync · PreviewAsync"] --> OPT["EncodeOptions<br/>AllowHardware · AceleracionPedida · CodificadorPedido"]
  ENG --> OBJ["Objetivo/<br/>codificador, bitrate, tamaño objetivo"]
  ENG --> AUD["Audio/<br/>plan de audio, codecs, pistas que se quedan"]
  ENG --> EST["Estimator"]
  ENG --> FF[("ffmpeg / ffprobe<br/>procesos")]
```

## Huecos conocidos

- `Engine.MinFreeBytes` sigue siendo un estático de proceso: lo escriben la CLI y el MCP. Por eso
  el margen de disco no se congela en una tanda encolada. Es el último estático de este tipo.
- `Engine` no tiene pruebas de la compresión en sí (solo del codificador elegido y del
  interruptor de hardware); la compresión real se prueba a mano.
- No hay ficha para los presets (`Preset.cs`); se mencionan aquí porque `Preset` viaja con
  `EncodeOptions`.
