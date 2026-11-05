---
funcion: Preferencias y ajustes persistidos
modulos:
  - src/Ondine.Core/Settings.cs
  - src/Ondine.Core/RenameRule.cs
  - src/Ondine.Core/Ia/AjustesDeModelo.cs
  - src/Ondine.Core/Peliculas/AjustesDeTmdb.cs
  - src/Ondine.Core/Localizacion/Textos.Preferencias.cs
  - src/Ondine.Mcp/Preferencias.cs
  - src/Ondine/PreferencesWindow.xaml
  - src/Ondine/PreferencesWindow.xaml.cs
  - src/Ondine.Avalonia/Preferencias.axaml
  - src/Ondine.Avalonia/Preferencias.axaml.cs
pruebas:
  - tests/Reindex.Tests/AjustesQueSobrevivenTests.cs
  - tests/Reindex.Tests/PreferenciasPorMcpTests.cs
  - tests/Reindex.Tests/ClaveDeTmdbTests.cs
  - tests/Reindex.Tests/ModeloConectadoTests.cs
  - tests/Reindex.Tests/IdiomaElegidoTests.cs
  - tests/Reindex.Tests/ElMcpNoSeQuedaAtrasTests.cs
  - tests/Reindex.Tests/CerrarModalesTests.cs
tareas:
  - ROADMAP.md › Transversal
---

# Preferencias y ajustes persistidos

**Qué hace:** guarda en `settings.json` las preferencias de la app (idioma, ajustes de
compresión, disco, aceleración, modelo y TMDb) y las devuelve a las ventanas de Preferencias
y al agente del MCP. Las claves del modelo y de TMDb van cifradas aparte, con DPAPI en Windows.

## Piezas y dependencias

```mermaid
flowchart TD
  WPF["PreferencesWindow (WPF)<br/>Save: parte de _previos.Clone()"] -- "Result" --> VP
  AVA["Preferencias (Avalonia)<br/>Guardar: parte de _previos.Clone()"] -- "Result" --> VP
  MCP["Ondine.Mcp · Preferencias<br/>Ajustar: Load, Clone, Save con confirmar"] --> STORE
  VP["VentanaPrincipal<br/>_settings cacheado al arrancar"] -- "SettingsStore.Save(_settings)" --> STORE
  CPANEL["ComplementosPanel (Avalonia)<br/>Load, cambia ComplementosApagados, Save"] --> STORE
  STORE["SettingsStore<br/>Load · Save"] --> JSON[("settings.json<br/>DatosDeUsuario.Raiz")]
  SET["Settings<br/>Clone()"] --> RR["RenameRule"]
  SET --> IA["Ia/AjustesDeModelo<br/>ClaveCifrada"]
  SET --> TM["Peliculas/AjustesDeTmdb<br/>ClaveCifrada"]
  IA --> PROT["Proteccion (DPAPI)<br/>solo Windows"]
  TM --> PROT
  TXT["Textos.Preferencias<br/>etiquetas de la ventana"] -.-> WPF
  TXT -.-> AVA
  SET -. "Idioma.Actual" .-> IDI["Idioma"]
```

## Huecos conocidos

- **Un `settings.json` que no se lee se pierde al guardar.** `SettingsStore.Load` traga cualquier
  error y devuelve ajustes por defecto. Basta un valor de enum que no reconoce (por ejemplo un
  `AfterCompress` con otro nombre) para que toda la carga falle; el siguiente `Save` escribe los
  valores por defecto encima del fichero original, sin copia de seguridad.
- **Un guardado fallido no avisa.** `SettingsStore.Save` tiene un `catch {}` vacío y la ventana
  de Preferencias se cierra como si hubiera guardado. Además la escritura es directa
  (`File.WriteAllText`), sin fichero temporal: un corte a mitad deja el JSON truncado y cae en el
  caso anterior.
- **Se pierde una actualización entre ventanas.** `VentanaPrincipal` guarda su `_settings`
  cacheado (cargado al arrancar) cada vez que cambia algo, por ejemplo al marcar «subcarpetas» o
  al terminar una compresión que aprende el factor de complejidad. Si antes `ComplementosPanel`
  apagó un complemento con su propio `Load`/`Save`, ese cambio se pisa. El mensaje de
  `ondine_ajustar_preferencias` avisa de lo mismo para la ventana, pero la ventana principal
  tampoco recarga.
- **En Linux y macOS la clave no se guarda y la ventana no lo dice.** `Proteccion.Cifrar` devuelve
  vacío fuera de Windows, así que `PonerClave` deja `ClaveCifrada` vacía. Es lo previsto (no se
  guarda en claro), pero la ventana de Avalonia acepta la clave del modelo o de TMDb, la muestra
  como «sin poner» tras guardar y no explica por qué.
- **`Settings.Clone` es incompleto.** Copia a mano solo `Ia`, `Tmdb` y `ComplementosConModelo`.
  `Rename`, `RenameSearchHistory`, `RenameReplaceHistory` y `ComplementosApagados` siguen
  compartidos con el original, así que editar la copia y cancelar sí cambia lo de verdad. Hoy
  las ventanas no tocan esas listas en la copia, pero el primero que lo haga rompe la garantía.
- **El MCP ignora en silencio un valor del tipo equivocado.** En `Entero` y `Bandera`, un valor que
  no se convierte (por ejemplo `"margen_disco_mb": "mucho"`) sale sin error ni cambio, así que la
  respuesta puede ser «No hay nada que cambiar» o un guardado a medias. `idioma_audio` tampoco se
  valida: acepta cualquier texto.
- **El cifrado de las claves solo se prueba en Windows.** `ClaveDeTmdbTests` y
  `ModeloConectadoTests` hacen el viaje completo de la clave (guardar, cifrada, recuperar), pero
  solo si `OperatingSystem.IsWindows()`. En Linux la prueba sale como «saltado», así que el CI no
  vigila ese camino.
