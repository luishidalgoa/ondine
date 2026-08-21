# La prueba de fuego de Avalonia

**Resultado: las dos preguntas salen que sí. 13 comprobaciones, 0 fallos.**

Esto no es un producto y no entra en ninguna compilación: no lo referencia nadie, no hay
solución que lo arrastre y CI compila proyectos por ruta. Sus paquetes NuGet no tocan la
norma de «CI no restaura nada», porque CI no lo ve.

Existe para contestar, con código que corre, las dos incógnitas que el estudio
([`docs/avalonia.md`](../../docs/avalonia.md)) decía que podían cancelar el puerto entero.
Se hace **antes** de reescribir nada, que es el único momento en que la respuesta sale barata.

```bash
dotnet run --project spike/avalonia                       # abrir y trastear a mano
dotnet run --project spike/avalonia -- --auto             # solo el DataGrid, se comprueba y cierra
dotnet run --project spike/avalonia -- --auto --h264 <fichero.mp4> --av1 <fichero.mkv>
```

## Por qué se comprueba sola y no a ojo

Porque **en Avalonia un binding que no resuelve no da error**: deja `null` y sigue. Que el
XAML compile no dice nada — el compilador acepta encantado `$parent[DataGridRow]` apunte a
algo o no. Así que la ventana se abre de verdad, se selecciona una fila, se busca en el
árbol visual lo que el `RowDetails` haya realizado, y se pulsan los botones.

## Pregunta 1 · ¿El DataGrid aguanta Organizar?

Era **el riesgo que más preocupaba**, por encima del vídeo: el vídeo tiene alternativas y
esto no. Se reprodujo el patrón exacto de `OrganizarView.xaml`: `RowDetailsTemplate` con un
`ItemsControl` dentro, y un botón que vive en la lista pero tiene que cambiar **su fila**.

| | |
|---|---|
| El `RowDetails` se realiza al seleccionar | ✓ |
| `{Binding $parent[DataGridRow].DataContext}` | ✓ llega la `Fila` |
| `{Binding DataContext, RelativeSource={RelativeSource AncestorType=DataGridRow}}` | ✓ **la sintaxis de WPF funciona tal cual** |
| Pulsar cambia la fila y la celda se repinta | ✓ |
| El semáforo de confianza con `Classes.*` en vez de `Trigger` | ✓ |
| Una fila sin candidatos no despliega panel | ✓ |

Lo que más ahorra es la segunda fila de esa tabla: **el binding que hoy está escrito en WPF
se copia sin tocarlo**. Se probaron las dos sintaxis a propósito para saber si había que
reescribirlas; no hace falta.

Y dos cosas salen más simples que en WPF: `IsVisible` es booleano y se ata directo, así que
los 12 usos del converter `B2V` desaparecen; y el semáforo, que en WPF es un `Trigger`,
aquí es una clase condicional de una línea.

### Los dos fallos que hubo eran de la prueba, no de Avalonia

Vale la pena dejarlo escrito porque es el error que cualquiera repetiría:

1. Buscar el panel «en la tabla» encontraba el de la fila **anterior**, que sigue en el
   árbol un rato porque el `DataGrid` está virtualizado. Hay que mirar dentro de *su*
   `DataGridRow`.
2. Buscar «el primer `Border` que contenga tal botón» devolvía el chrome del propio
   `DataGridRow`, que envuelve al del template y **hereda el mismo `DataContext`** — así
   que parecía el bueno. Hay que buscar por nombre.

Las dos veces la prueba acusaba a Avalonia de algo que era mío. En un puerto de verdad eso
se traduce en horas persiguiendo un fantasma.

## Pregunta 2 · ¿LibVLC sirve de reproductor?

`LibVLCSharp.Avalonia` 3.10.1 convive con Avalonia 12.1.1 sin conflicto de versiones — que
no era obvio, siendo un paquete que aún numera por la 3.x.

**La búsqueda cae donde se pide.** Sobre un patrón de 120 s con fotograma clave cada 2 s:

| pedido | real | desvío |
|---|---|---|
| 12,000 s | 12,576 s | +0,576 s |
| 60,000 s | 60,823 s | +0,823 s |
| 108,000 s | 108,574 s | +0,574 s |
| 30,000 s | 30,575 s | +0,575 s |

Peor caso 0,823 s, y **siempre hacia delante**. Importa la dirección: el fallo que se vio
en la app de hoy eran diez segundos *hacia atrás*, que es lo que hace que un corte se
coloque donde no es.

**AV1 reproduce.** Es la deuda que el estudio dice que esta migración paga, y ahora está
comprobada en vez de supuesta: `MediaElement` va por DirectShow y con AV1 no puede por
mucha extensión que se instale. LibVLC lo abre — y de propina el registro dice que usó
**D3D11VA**, o sea decodificación por hardware.

### Lo que se vio y conviene no olvidar

Al buscar de forma agresiva, VLC escupe por el registro `Timestamp conversion failed` y
`SetThumbNailClip failed`. No rompieron nada aquí y las cuatro búsquedas cayeron donde
debían, pero son ruido real y aparecerán otra vez en cuanto se monte una línea de tiempo de
verdad. Queda apuntado para no descubrirlo como sorpresa.

## Lo que esto NO contesta

Sigue en pie todo lo del estudio que no era una incógnita sino trabajo: los 107 *triggers*,
las 12 514 líneas de código detrás, la papelera fuera de Windows y el empaquetado por
plataforma. Y la decisión de fondo tampoco es técnica: **meter LibVLC significa el primer
paquete NuGet del proyecto**, con binarios nativos por plataforma detrás.
