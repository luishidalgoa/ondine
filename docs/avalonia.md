# Estudio: llevar la interfaz a Avalonia (Linux y macOS)

> **Esto es un estudio, no un plan aprobado.** No se ha tocado una línea de código. Lo que
> sigue es lo que hay medido hoy, dónde duele de verdad, en qué orden tendría sentido
> hacerlo y qué NO se resuelve por migrar la interfaz.
>
> Medido sobre `main` en la 1.10.0 más los dos cambios sin publicar (21/08/2026).

## La conclusión, antes de los detalles

**Se puede, y la forma del código ayuda más de lo que cabría esperar** — pero el primer paso
no es Avalonia. Es **partir un `Ondine.Core` de verdad**, y ese paso vale la pena aunque
Avalonia no se haga nunca.

Tres cosas salieron de medir, y las tres cambian el plan respecto a lo que uno supondría:

1. **El 60% del C# ya es portable** (18 472 líneas de 30 986). No por suerte: hay decisiones
   deliberadas repartidas por el código. `ProcessControl` ya trae SIGSTOP/SIGCONT para Unix,
   `FichaDeWindows` documenta que devuelve `null` fuera de Windows, y `NubeLocal` vive fuera
   de la carpeta del motor «porque esa se compila también para Linux y macOS».
2. **Lo más tedioso de portar está concentrado.** 85 de los 107 *triggers* y 66 de las 85
   `ControlTemplate` viven en **dos ficheros**: `Theme.xaml` y `ThemeOrganizar.xaml`, 1 782
   de las 6 121 líneas de XAML. El tema se porta una vez y el resto va detrás.
3. **La migración arregla un fallo abierto de paso.** El reproductor no puede con AV1 porque
   `MediaElement` va por DirectShow, y eso no se arregla instalando nada. En Avalonia no hay
   `MediaElement`: hay que ir a LibVLC, **que reproduce AV1 nativo**. Lo que parecía el punto
   más caro es también el que paga una deuda que ya teníamos.

## Cómo está hecho hoy

| | |
|---|---|
| C# total | 30 986 líneas |
| — atado a WPF | **12 514** (26 ficheros) |
| — portable | **18 472** |
| XAML | 6 121 líneas en 18 ficheros |
| — los dos temas | 1 782 (29%) |
| *Triggers* (`Trigger`/`Data`/`Multi`/`Event`) | 107, **85 en los temas** |
| `ControlTemplate` | 85, **66 en los temas** |
| Paquetes NuGet | **cero** |

Y el dato que manda sobre todo lo demás: **no hay un proyecto de motor**. `Ondine.Cli` y
`Reindex.Tests` comparten el motor **enlazando ficheros sueltos** del proyecto WPF:

```xml
<Compile Include="../Ondine/Engine.cs" Link="Engine.cs" />
<Compile Include="../Ondine/Reindex/*.cs" LinkBase="Reindex" />
```

Funciona, y el `.csproj` explica muy bien por qué se enlazan carpetas enteras y no listas a
mano. Pero es una decisión que aguanta **dos** consumidores. Con un tercero —la app de
Avalonia— hay que repetir el bloque de enlaces por tercera vez, y ahí deja de aguantar: un
tipo nuevo del motor rompería tres proyectos en vez de dos, y el que se rompe siempre es el
que nadie compila en local.

## Qué se salva y qué no

### Se salva tal cual (18 472 líneas)

`Engine`, `Estimator`, `Reindex/` entero (7 021 líneas), `Peliculas/`, `Pistas/`, `Rutas/`,
`Ia/`, `Complementos/` y `Localizacion/Textos.*` (5 744 líneas). Es el 60% del código y ya
compila para Linux hoy: los tests lo demuestran cada vez que corre CI.

### Se reescribe (12 514 líneas de C# más 6 121 de XAML)

Todo lo que es `.xaml` y `.xaml.cs`, más cinco ayudantes visuales (`PasosVisual`,
`EtapaVisual`, `CirculosCargando`, `CampoTexto`, `HeaderSort`).

### Los cinco puntos duros, por lo que cuestan

**1. El reproductor de vídeo: no hay equivalente.** Avalonia
[no tiene `MediaElement`](https://github.com/AvaloniaUI/Avalonia/issues/11451) y no está
previsto. La salida real es [`LibVLCSharp.Avalonia`](https://www.nuget.org/packages/LibVLCSharp.Avalonia).
Afecta a `ReproductorWindow` (816 + 167 líneas) y a `RecortesView` (1 759 + 442), que es la
pantalla con más lógica de tiempo de toda la app. **Y rompe una norma del repo: hoy hay cero
paquetes NuGet, y LibVLC además arrastra binarios nativos por plataforma.** Es la decisión de
fondo de todo esto, y no es una decisión técnica.
La contrapartida, ya dicha: AV1 pasa a funcionar en el reproductor y en Recortes.

**2. Los *triggers*.** Avalonia
[sustituye los triggers de WPF por un sistema tipo CSS](https://docs.avaloniaui.net/docs/migration/wpf):
selectores, clases y pseudoclases (`:pointerover`, `:pressed`, `:checked`). No hay traducción
automática, los 107 se reescriben a mano. Lo que salva es la concentración: 85 están en los
dos temas, así que es **un trabajo grande de un solo sitio**, no 107 sorpresas repartidas.

**3. El `DataGrid` de Organizar.** 35 usos en `OrganizarView.xaml`, más su tema. En Avalonia
es un paquete aparte y es menos capaz que el de WPF. Lo que hay que comprobar antes de
prometer nada es el `RowDetailsTemplate`, porque de él cuelga toda la revisión por filas: las
candidatas de TMDb, las historias de un capítulo. **Este es el riesgo que más me preocupa**,
más que el vídeo, porque no tiene alternativa: si el DataGrid no da, hay que rehacer la
pantalla con otra estructura.

**4. El código detrás es gordo y mezcla.** `OrganizarView.xaml.cs` son 2 961 líneas,
`MainWindow.xaml.cs` 1 973 y `RecortesView.xaml.cs` 1 759. Solo 7 ficheros implementan
`INotifyPropertyChanged` y hay 38 manejadores cableados desde el XAML: la app es de código
detrás, no de MVVM. Al portar, buena parte de esas líneas **no son interfaz, son reglas
metidas en la interfaz**, y esas se pueden bajar al motor antes de tocar Avalonia. Es trabajo
que mejora la app aunque el puerto se pare.

**5. Cosas sueltas.** Un `RichTextBox` —el JSON coloreado de `CatalogoWindow`— sin
equivalente: se resuelve con texto por trozos o con AvaloniaEdit. El `WindowChrome` de las 9
ventanas con barra propia pasa a `ExtendClientAreaToDecorationsHint`. Y `T.cs`, la extensión
de marcado de la traducción, se reescribe: son 43 líneas y el concepto existe igual.

## Lo que NO arregla migrar la interfaz

Esto es lo que más me importa dejar dicho, porque es lo que separa «la app ya corre en Linux»
de «la app **sirve** en Linux»:

| Qué | Hoy | Fuera de Windows |
|---|---|---|
| **Papelera** | `SHFileOperation` con `FOF_ALLOWUNDO` | Hay que implementar el estándar XDG en Linux y `NSFileManager` en macOS. **No es opcional**: «nunca se borra, va a la papelera» es norma dura del repo |
| **Instalador y auto-update** | Inno Setup más Releases de GitHub | Otro empaquetado por plataforma: AppImage o deb, y en macOS un .dmg con notarización de Apple, que cuesta dinero y cuenta de desarrollador |
| **«Abrir con» y menú contextual** | Registro de Windows | Ficheros `.desktop` en Linux; en macOS va dentro del bundle |
| **Ficha de Windows y marcadores de nube** | `NubeLocal`, `FichaDeWindows` | **Ya resuelto**: devuelven `null` o no-aplica por diseño, y está documentado |
| **Suspender ffmpeg** | `ntdll` | **Ya resuelto**: SIGSTOP/SIGCONT ya está escrito |
| **ffmpeg** | El instalador lo descarga | En Linux es un paquete del sistema, y `ResolveTool` ya cae al PATH: sale casi solo |
| **Humo de la interfaz** | 907 líneas contra WPF | Se rehace contra `Avalonia.Headless`, que corre en CI en Linux — cosa que el de ahora no puede |

## El orden que propongo

**Fase 0 — `Ondine.Core`, y parar ahí a mirar.** Sacar el motor a su propio proyecto `net9.0`
con referencia de proyecto de verdad, y que `Ondine` (WPF), `Ondine.Cli` y `Reindex.Tests`
tiren de él. Cero cambios de comportamiento, los 640+ tests en verde igual. **Esto se hace
aunque Avalonia se cancele mañana**: quita el bloque de enlaces duplicado, que es una fuente
de roturas conocida y documentada en el propio `.csproj`.

**Fase 1 — bajar reglas del código detrás al motor.** Sin tocar interfaz: lo que en
`OrganizarView.xaml.cs` y `RecortesView.xaml.cs` sea decisión y no pintura, al motor y con
tests. Reduce lo que hay que portar y mejora la app por sí solo.

**Fase 2 — la prueba de fuego, antes de comprometerse.** Una app Avalonia mínima que haga
**solo** las dos cosas que pueden hundir el plan: un `DataGrid` con `RowDetails` como el de
Organizar, y un vídeo con LibVLC buscando por la línea de tiempo. Si esas dos van, el resto es
trabajo conocido. Si no van, se sabe **antes** de haber reescrito nada.

**Fase 3 — el tema.** `Theme.xaml` y `ThemeOrganizar.xaml` al sistema de selectores. Es donde
está el 80% de lo tedioso y se hace de una vez.

**Fase 4 — las pantallas**, de menos a más: los diálogos pequeños primero, `MainWindow`
después, `OrganizarView` y `RecortesView` al final.

**Fase 5 — empaquetado y piezas de sistema**: papelera, `.desktop`, AppImage y dmg.

Las fases 0 y 1 no son coste de Avalonia: son mejoras que se quedan pase lo que pase. La 2 es
barata y es la que decide. De la 3 en adelante es cuando se gasta de verdad.

## Lo que todavía no sé

- **Si el `DataGrid` de Avalonia aguanta Organizar.** Es la incógnita que más pesa y solo se
  despeja probándola (fase 2).
- **Cuánto de las 12 514 líneas de código detrás es lógica y cuánto es pintura.** Lo sabré al
  hacer la fase 1. Hoy solo puedo decir que 7 ficheros de 26 tienen `INotifyPropertyChanged`,
  lo que sugiere que hay bastante lógica mezclada.
- **Si LibVLC es aceptable como dependencia.** Eso no lo decide el código.

## Lo que ya está hecho (21/08/2026)

El plan de abajo dejó de ser un plan. Al día de hoy:

- **Fase 0** ✅ — el motor separado en `Ondine.Core`.
- **Fase 1** ✅ — reglas de Organizar bajadas al motor, con pruebas.
- **Fase 2** ✅ — la prueba de fuego, en `spike/avalonia`. **Las dos incógnitas salen que sí**: el
  `DataGrid` con `RowDetails` aguanta el patrón de Organizar (y el binding de WPF vale tal cual), y
  LibVLC busca donde se le pide y reproduce AV1 con decodificación por hardware.
- **Fase 3a** ✅ — existe `src/Ondine.Avalonia`, **publica para Linux y macOS**, y comparte con la
  interfaz de WPF los 30 colores del tema y el catálogo de textos entero.

**La decisión de LibVLC está tomada: entra.** Era la única que quedaba y no era técnica. Es el
primer paquete con binarios nativos del proyecto y entró con permiso explícito.

- **Fase 3b** 🔜 — la familia de **botones** portada (6 estilos) con el haz del foco como
  animación de Avalonia, y **los otros 18 apuntados con su motivo**. La mayoría no se portan y esa
  es la conclusión útil: `TableView`, `RowStyle` y `ColHeader` visten un `ListView`+`GridView` que
  en Avalonia **es un `DataGrid`**, así que portarlos sería trabajo tirado; y los del reproductor
  van con Recortes en la Fase 4. Lo vigila `TemaPortadoTests`, que exige que cada estilo esté en
  una de las dos listas.

  **La traducción de fondo**, para quien siga: un `<Style x:Key>` de WPF es un `ControlTheme` en
  Avalonia, y los `<Trigger Property="IsMouseOver">` son selectores `^:pointerover`. Las partes de
  la plantilla se apuntan con `/template/ Border#b` en vez de `TargetName`. **Un selector que no
  casa no da error** — el control sale con el aspecto de fábrica y nadie lo dice —, así que el
  proyecto trae un `--auto` que abre la ventana y comprueba que el tema se aplicó de verdad.

- **Fase 4** 🔜 — el **diálogo compartido** portado: modal, centrado, Esc cancela, Intro acepta y
  el mensaje se puede copiar. Lo usan todas las pantallas, así que desbloquea el resto.

  **La diferencia que menos se ve venir: en Avalonia mostrar un modal es asíncrono.** En WPF
  `ShowDialog()` devuelve el resultado ahí mismo y quien pregunta sigue en la línea siguiente; en
  Avalonia devuelve una tarea. Eso obliga a que **todo método que pregunte algo pase a ser
  `async`**, y en `OrganizarView` eso son bastantes. Conviene contarlo en el presupuesto de la
  fase, porque no aparece en ninguna tabla de equivalencias.

  De propina, el mensaje pasa de ser un `TextBox` de solo lectura a un `SelectableTextBlock`, que
  es más honesto y quita el apaño que tenía la versión de WPF para que el foco no se comiera la
  tecla Intro.

  Y el **tema de los campos** —caja de texto, casilla, desplegable—. Va como estilo **implícito**
  y no con nombre, al revés que los botones, y el motivo es de presupuesto: en el XAML de WPF esos
  tres se usan a pelo (`<TextBox/>`), así que si aquí pidieran un tema por nombre habría que tocar
  cada uno de los cientos que hay en las dieciocho pantallas — y el que se olvidara saldría con el
  aspecto de Fluent sin que nada lo dijera. Así, portar una pantalla es copiar su XAML y traducir
  lo que cambia.

  El desplegable se viste con *setters* y no con plantilla propia: la de Fluent ya trae el popup,
  el desplazamiento y el teclado resueltos, y rehacerla para cambiar cuatro colores es mucho código
  por poco. Los botones sí la necesitaban, porque el haz del foco no existe en ningún tema de serie.

Quedan el resto de pantallas y el empaquetado. Las dos
interfaces conviven mientras dure: cambiar las dieciocho pantallas de golpe habría dejado la app
sin poder publicarse durante semanas.

## La alternativa que existe, y por qué no la propongo

**Avalonia XPF** corre XAML de WPF casi sin cambios. Es de pago y por aplicación. Ahorraría la
mayor parte de este documento, pero mete un coste recurrente en un proyecto que hoy no cobra
nada — y la decisión de hornear la clave de TMDb ya está condicionada exactamente a eso. La
dejo apuntada porque existe, no porque la recomiende.

---
*Estudio, 21/08/2026. Nada de esto se ha implementado.*
