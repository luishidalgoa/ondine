# Roadmap — Ondine

Nació como **ShrinkStudio**, relevo de HandBrake (comprimir por lotes sin pelearse con los
ajustes), y hoy son tres herramientas: **Comprimir**, **Organizar** (poner nombre a una biblioteca
a partir de un catálogo) y **Recortes** (partir un vídeo o quitarle un trozo).

> **Hacia dónde va**: ser **la pieza que le falta a Plex y Jellyfin**. Esos servicios enseñan una
> biblioteca preciosa, pero solo si los ficheros ya están bien nombrados y colocados; cuando no lo
> están, se rinden. Preparar eso es lo que Organizar sabe hacer y ningún compresor hace.
> El cambio de nombre a **Ondine** viene de ahí. Ver el epic
> [#153](https://github.com/luishidalgoa/ondine/issues/153), y
> [#168](https://github.com/luishidalgoa/ondine/issues/168) para conducirla desde un agente
> de IA por MCP.
>
> Dos avisos del análisis del nicho, que condicionan por dónde crecer: el **renombrado contra
> catálogo está saturado** (FileBot lleva 15 años, y Sonarr/Radarr lo hacen gratis), así que no es
> por ahí; y **recortar clips es la pata más floja** frente a LosslessCut. Lo que no hace nadie es
> **partir episodios en sus historias**.

Leyenda: ✅ hecho · 🔜 siguiente · ⬜ pendiente

## Base
- ✅ Compresión H.265 por hardware (QSV/NVENC/AMF) con fallback a CPU.
- ✅ Calidad ajustable, downscale de resolución, idiomas de audio, subtítulos por idioma.
- ✅ Lote: análisis de pistas, lista con miniaturas, marcar/comprimir, papelera.
- ✅ Instalador per-user + auto-update.

## Comprimir — presets y formatos
- ✅ **Formato de salida**: MKV, **MP4** y **WebM** (VP9 + Opus). También **solo audio** (MP3/M4A/FLAC/Opus).
- ✅ **Presets**: combo con presets de fábrica («Máxima compatibilidad», «Archivar», «Móvil»…) + guardar los tuyos (JSON en `%AppData%`).
- ✅ **Códec elegible**: H.265 / H.264 / **AV1** (hardware si hay, si no software).
- ✅ Modo calidad (CRF/CQ) por presets de calidad.
- ✅ **Pausar / Reanudar** y **Detener** limpios (suspender/continuar FFmpeg; al detener se corta y se borra el temporal).
- 🔜 Modo **bitrate objetivo** (VBR de N kbps / tamaño objetivo) además de calidad.
- 🔜 Preset de velocidad del codificador (ultrafast…slow) expuesto en la UI.
- ⬜ **Recorte (crop)** y **dimensiones exactas** (con anamórfico/relación de aspecto).
- ⬜ **Filtros**: desentrelazado, denoise, deblock, nitidez, rotación.

## Comprimir — audio y subtítulos
- ✅ Copiar original o recodificar a AAC (por bitrate).
- 🔜 **Codecs de audio** elegibles: AAC / AC3 / E-AC3 / Opus / FLAC / passthrough.
- 🔜 **Mezcla** (downmix a estéreo, mantener 5.1) y bitrate/samplerate por pista.
- ⬜ Ganancia y compresión de rango dinámico (DRC).
- ⬜ Subtítulos: **quemar (burn-in)**, marcar *forced*, importar `.srt` externos.

## Organizar — identificar y renombrar una biblioteca
- ✅ **Catálogos de referencia** (formato `reindex/1.0`): importar, validar con errores concretos y explorar.
- ✅ **Identificación en cascada**: número + fecha exacta → título → número + fecha aproximada, con la confianza a la vista (verde/ámbar/rojo).
- ✅ **Plantilla de biblioteca** configurable (`<serie>`, `<temp>`, `<num>`, `<título>`, `<seg>`), con vista previa en vivo.
- ✅ **Nada se toca sin aprobación**: analizar solo propone; se aplica lo marcado y hay **deshacer lote**.
- ✅ **Cola de revisión** y **memoria de decisiones** (lo que decides una vez no se vuelve a preguntar).
- ✅ **Prioridad del match por catálogo**: «automática» o «el número manda».
- ✅ **Carpetas vinculadas** al catálogo: eliges la serie y su carpeta viene sola.
- ✅ **Fichero repetido**: se distingue de un conflicto real, enseña las **dos rutas** y eliges cuál va a la Papelera.
- ✅ **Mini-historias («segmentos»)**: un capítulo con 2-3 historias se numera `1a`, `1b`, `1c`.
  - ✅ **Partir en segmentos**: encuentra el corte solo (fundido a negro + reparto que dice el catálogo) y corta **sin recodificar**.
  - ✅ Decidir a mano qué historias trae un fichero, desde **cualquier** fila.
  - ✅ Combinar historias de episodios distintos (`E1b+2b`), con nombre compuesto honesto.
- ✅ **Generar el catálogo con una IA**: la app arma el encargo con el formato y las reglas dentro.
- 🔜 Detección de **temporadas por carpeta** más lista cuando el nombre no la dice.
- ⬜ Leer el título del **metadato** del contenedor como una señal más.
- ⬜ Catálogos **compartibles**: exportar/importar sin duplicar trabajo.

## Organizar — más allá de las series ([epic #153](https://github.com/luishidalgoa/ondine/issues/153))
- 🔜 **Tipo de biblioteca** (serie / película): hoy todo asume que son episodios, y una película no
  tiene temporada ni número. ([#154](https://github.com/luishidalgoa/ondine/issues/154))
- ⬜ **Películas sin catálogo**: identificarlas contra una base de datos pública, porque para las
  películas no existe una lista que importar. El reto es que el nombre del fichero es una fuente
  poco fiable. ([#155](https://github.com/luishidalgoa/ondine/issues/155))
- ⬜ **Montar la estructura de carpetas**, no solo renombrar: hoy 200 capítulos sueltos se quedan
  sueltos aunque queden bien nombrados. ([#156](https://github.com/luishidalgoa/ondine/issues/156))

## Recortes
- ✅ Línea de tiempo con miniaturas, marcar tramos y exportar uno por tramo.
- ✅ Integración con Organizar: un fichero con dos episodios se abre aquí con el corte ya puesto.
- ✅ El original solo se borra si salen **todos** los tramos, y va a la Papelera.
- 🔜 Corte **sin recodificar** también aquí (hoy recodifica; Organizar ya corta con `-c copy`).
- ⬜ Ajuste fino del corte fotograma a fotograma.

## Flujo de trabajo
- ✅ **Recortar por tiempo** (procesar solo un tramo) — cubierto por Recortes.
- ✅ **Papelera propia con Ctrl+Z**: lo que la app manda a la papelera se recupera al instante, y se finaliza en la Papelera de Windows al cerrar, por antigüedad o al acumularse.
- ✅ **Tutoriales dentro de la app** (Ayuda → «Tutoriales»), con el diagrama de cómo decide Organizar.
- ✅ Ordenar las tablas por columna; conmutador de páginas compacto.
- 🔜 **Cola de trabajos** con ajustes distintos por trabajo (hoy todos comparten opciones).
- ⬜ **Previsualización** (muestra corta del resultado antes de codificar todo).
- ⬜ «Al terminar»: no hacer nada / apagar / suspender.

## Foco: ahorro de almacenamiento
- ✅ **Pronóstico** de tamaño final y ahorro (GB y %) por vídeo, en la pestaña *Estimación*.
- ✅ **Valoración calidad↔ahorro** en barras (0–5) para vídeo y audio, recalculada al cambiar las opciones.
- ✅ **Limpieza de temporales**: el `.tmp` se borra al terminar/cancelar; las miniaturas se liberan al cerrar.
- 🔜 Modo bitrate/tamaño objetivo (llegar a un tamaño concreto) — se apoya en el mismo modelo de estimación.

## Transversal
- ✅ **FFmpeg**: el instalador lo detecta y, si falta, lo descarga e instala junto a la app (el usuario no configura nada).
- ✅ **Compilación en la nube** (GitHub Actions): al empujar un tag `vX.Y.Z`, el instalador se compila y se adjunta al Release automáticamente.
- ✅ **CLI** (`Ondine.Cli`) para comprimir, analizar y medir sin abrir la interfaz.
- ✅ **Motor con tests** (640+) que corren en CI sin restaurar paquetes.
- ✅ **Capturas en el README** de las tres herramientas, con datos reales dentro.
- ⬜ Firmar el instalador (evitar el aviso de SmartScreen).
- ⬜ Rediseño visual (brief en [`docs/design-brief.md`](docs/design-brief.md)).
- ⬜ **Linux / macOS**: WPF es solo Windows. Requiere migrar la interfaz a **Avalonia** (multiplataforma);
  el motor (`Engine`/`Estimator`/`Reindex`) ya es portable. Es el paso grande pendiente.
