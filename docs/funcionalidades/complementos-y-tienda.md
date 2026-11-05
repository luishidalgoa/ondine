---
funcion: Complementos y tienda
modulos:
  - src/Ondine.Core/Complementos/
  - src/Ondine.Core/Localizacion/Textos.Complementos.cs
  - src/Ondine.Core/Settings.cs
  - src/Ondine.Core/Rutas/DatosDeUsuario.cs
  - src/Ondine/ComplementosPanel.xaml
  - src/Ondine/ComplementosPanel.xaml.cs
  - src/Ondine/MainWindow.xaml.cs
  - src/Ondine.Avalonia/ComplementosPanel.axaml
  - src/Ondine.Avalonia/ComplementosPanel.axaml.cs
  - src/Ondine.Avalonia/VentanaPrincipal.axaml.cs
  - docs/complementos.md
  - ejemplos/complemento-youtube/
  - complementos/
pruebas:
  - tests/Reindex.Tests/ComplementoMultiplataformaTests.cs
  - tests/Reindex.Tests/LoQueDiceElComplementoTests.cs
  - tests/Reindex.Tests/IndiceTests.cs
  - tests/Reindex.Tests/PuenteDelModeloTests.cs
  - tests/Reindex.Tests/TiendaGestionTests.cs
  - tests/Reindex.Tests/CotejoDeDosEpisodiosTests.cs
  - tests/Reindex.Tests/IdiomaElegidoTests.cs
---

# Complementos y tienda

**Qué hace:** ejecuta programas de fuera como procesos (por ejemplo, el complemento de YouTube),
los descubre en su carpeta, los instala desde el índice oficial verificando su sha256 y les habla
por JSON en la salida estándar. El panel los lista, los enciende o apaga y deja traer episodios
o películas desde ellos.

## Piezas y dependencias

```mermaid
flowchart LR
  BOTON["Botón Complementos<br/>MainWindow y VentanaPrincipal"] -- "Encendidos()" --> DESC
  PANEL["ComplementosPanel<br/>WPF y Avalonia"] -- "Buscar()" --> DESC
  PANEL -- "listar y traer tienda" --> TIENDA
  PANEL -- "Instalar y Desinstalar" --> INST
  PANEL -- "CorrerAsync" --> INV
  PANEL -- "Cotejar" --> COTEJO
  DESC["Descubridor<br/>Buscar y Encendidos"] -- "Leer manifiesto" --> COMP
  DESC -- "ComplementosApagados" --> SET[("Settings")]
  DESC -- "Carpeta complementos" --> DATOS["DatosDeUsuario"]
  TIENDA["Tienda<br/>TraerIndiceAsync y TraerPaqueteAsync"] -- "Leer índice" --> IDX["Indice<br/>Entrada, Reparo, Cuadra"]
  TIENDA -- "HTTPS, tope 80 MB" --> WEB[("indice.json y paquetes<br/>en GitHub")]
  INST["Instalador<br/>Instalar y Desinstalar"] -- "Verificar sha256" --> IDX
  INST -- "Validar tras extraer" --> COMP
  INST -- "Dar permisos" --> PERM
  COMP["Complemento<br/>plugin.json, Reparo, ComoArrancar"] -- "Resolver" --> ARR
  ARR["Arranque<br/>qué programa y cómo"] --> PERM["Permisos<br/>bit de ejecución en Unix"]
  INV["Invocador<br/>CorrerAsync, una línea JSON por mensaje"] -- "ComoArrancar" --> COMP
  INV -- "Asegurar ejecutable" --> PERM
  INV -- "Interpretar línea" --> MSG["Mensaje"]
  INV -- "Pregunta del complemento" --> PUENTE["PuenteDelModelo<br/>límite de preguntas"]
  INV -- "Arranca el proceso" --> PROC[("Complemento<br/>proceso externo")]
  EJ["ejemplos/complemento-youtube<br/>referencia"] -.-> PROC
  COTEJO["CotejoDeLista<br/>títulos contra el catálogo"]
```

## Huecos conocidos

- **El interruptor de encendido no se guarda en WPF.** `src/Ondine/ComplementosPanel.xaml` enlaza
  `Activo` en modo TwoWay, pero `ComplementosPanel.xaml.cs` nunca lee ni escribe
  `ComplementosApagados`, y el valor arranca siempre en `true`. Apagar un complemento en WPF no
  persiste: al volver a abrir el panel sale encendido, y el botón de la barra (que usa
  `Descubridor.Encendidos`) sigue ofreciéndolo. En Avalonia sí se guarda.
- **Nada prueba que un complemento apagado deje de ofrecerse.** `Descubridor.Encendidos` filtra por
  `ComplementosApagados`, pero ninguna prueba de `tests/Reindex.Tests` la llama.
- **Reinstalar no es atómico.** `Instalador.Instalar` borra la versión anterior antes de mover la
  nueva (`Directory.Delete` y luego `Directory.Move`). Si el movimiento falla en ese hueco, la
  versión que funcionaba ya no está, lo que contradice el comentario de que lo que había sigue
  funcionando.
- **Un complemento que cierra su salida pero no termina puede colgar la app.** En el `finally` de
  `Invocador.CorrerAsync` se espera con `WaitForExitAsync(CancellationToken.None)`. El `Kill` solo
  se intenta si la cancelación ya estaba pedida en ese momento, así que si el proceso se queda vivo
  después de cerrar stdout, la espera no tiene límite y cancelar ya no lo detiene.
- **La dirección del índice no se puede cambiar.** Los dos paneles usan la constante
  `Tienda.IndiceOficial` directamente. `TraerIndiceAsync` acepta otra URL, y el comentario de
  `Tienda.cs` dice que se puede apuntar a otro índice, pero no hay ajuste ni campo que lo permita.
- **La descarga no tiene prueba.** Ninguna prueba llama a `TraerIndiceAsync` ni a
  `TraerPaqueteAsync`, así que el tope de 80 MB y la exigencia de HTTPS en la descarga solo se
  revisan a mano.
