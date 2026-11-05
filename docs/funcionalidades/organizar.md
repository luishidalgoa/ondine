---
funcion: Organizar (identificar, renombrar y reordenar una biblioteca)
modulos:
  - src/Ondine.Core/Reindex/
  - src/Ondine/OrganizarView.xaml
  - src/Ondine/OrganizarView.xaml.cs
  - src/Ondine/OrganizarRow.cs
  - src/Ondine.Avalonia/OrganizarView.axaml
  - src/Ondine.Avalonia/OrganizarView.axaml.cs
  - src/Ondine.Avalonia/OrganizarRow.cs
pruebas:
  - tests/Reindex.Tests/CatalogoQueSeReleeTests.cs
  - tests/Reindex.Tests/MudanzaTests.cs
  - tests/Reindex.Tests/MudanzaDeTemporadaTests.cs
  - tests/Reindex.Tests/OrganizarPorMcpTests.cs
  - tests/Reindex.Tests/CarpetaDeTemporadaTests.cs
  - tests/Reindex.Tests/DestinoDeTemporadaTests.cs
  - tests/Reindex.Tests/NumeracionDeFicherosTests.cs
  - tests/Reindex.Tests/PlanDeReordenadoTests.cs
  - tests/Reindex.Tests/CoherenciaDelLoteTests.cs
  - tests/Reindex.Tests/MulticargaTests.cs
tareas:
  - ROADMAP.md › Organizar — identificar y renombrar una biblioteca
  - ROADMAP.md › Organizar — más allá de las series (epic #153)
---

# Organizar

**Qué hace:** identifica los episodios de una biblioteca de series contra un catálogo, propone
el nuevo nombre y la carpeta de cada fichero, y los mueve o renombra. Antes de tocar nada muestra
el plan, con los riesgos (choques de nombre, temporadas a medias).

## Piezas y dependencias

```mermaid
flowchart LR
  UI["OrganizarView (WPF y Avalonia)<br/>OrganizarRow"] --> PLAN
  MCP["Ondine.Mcp · Organizar"] --> PLAN
  PLAN["ReindexEngine<br/>plan y ejecución"] --> CAT["ReindexCatalog<br/>episodios del catálogo"]
  PLAN --> MOV["Mudanza · MudanzaDeTemporada<br/>mover y renombrar"]
  PLAN --> RIE["RiesgoDelReordenado · CoherenciaDelLote"]
  PLAN --> ESC["LibraryScan · FileSignals · TitleMatch"]
  CAT --> STO["ReindexStore<br/>diario y catálogos guardados"]
  PLAN --> SIDE["SidecarPlanner<br/>subtítulos y ficheros asociados"]
  PLAN -. "usa" .-> PAP["PapeleraDelSistema<br/>(ficheros que se quitan)"]
```

## Huecos conocidos

- **Dos vistas por separado.** WPF y Avalonia mantienen `OrganizarView` y `OrganizarRow` por
  duplicado (miles de líneas cada una). Un cambio de comportamiento hay que hacerlo dos veces.
- **Sin pruebas de interfaz.** La lógica tiene pruebas; la pantalla no.
- **Acoplamiento con Preferencias:** `CarpetaTemporadaIdioma` se decide en esta pantalla y se
  guarda en Ajustes.
- **Fuera de la ficha:** la parte de «más allá de las series» (epic #153) aún no tiene código
  propio distinto; ver el ROADMAP.
