# Ondine — AGENTS.md

App de escritorio para Windows (C# · .NET 9 · WPF) y herramienta de terminal
multiplataforma que comparte **exactamente el mismo motor**, enlazando los
fuentes en vez de copiarlos. Prepara bibliotecas de series y películas antes de
que Plex, Jellyfin o Kodi las escaneen.

Cero dependencias de NuGet en la app de escritorio. La única externa es
**ffmpeg**, que se invoca como proceso.

---

## LA NORMA QUE MANDA: todo texto va en los dos idiomas

**Ondine es bilingüe. El inglés es el idioma por defecto y el castellano el
segundo.** Esto no es una tarea que se hizo una vez, es una condición
permanente del proyecto: cualquier trabajo sobre la app se hace **orientado a la
traducción**, no traduciendo al final.

### Se trabaja en castellano; los demás idiomas se rellenan al final

Escribir las dos versiones de un texto **mientras se está diseñando** una
pantalla cuesta el doble y la mitad de ese trabajo se tira: los textos cambian
tres veces antes de quedarse. Así que:

| Momento | Qué se escribe |
|---|---|
| Mientras se desarrolla | `Idioma.Pendiente("Exportar tramos")` |
| Antes de cortar versión, y a petición | `Idioma.Elegir("Export segments", "Exportar tramos")` |

**`Pendiente` no es una puerta de atrás, es una deuda anotada.** Se ve leyendo
el fichero, la prueba la cuenta y la enseña en cada tanda, y **el CI no publica
una versión que lleve ni una sola**: el trabajo que valida el CHANGELOG corre
las pruebas con `ONDINE_RELEASE=1` y ahí el recuento pasa a fallo.

La diferencia entre esto y escribir el literal a pelo es que **esto deja
rastro**. Un literal suelto no lo encuentra nadie; un `Pendiente` sale en la
lista.

### Cómo se añade un texto

Nunca se escribe un literal visible. Se añade una propiedad en el fichero
parcial de su pantalla, en `src/Ondine/Localizacion/Textos.<pantalla>.cs`:

```csharp
namespace Ondine.Localizacion;

public sealed partial class Textos
{
    // Ya cerrado: las dos versiones.
    public string RecortesExportar => Idioma.Elegir("Export segments", "Exportar tramos");

    // Todavía en obra: solo castellano, y el CI no dejará publicar así.
    public string RecortesRehacer => Idioma.Pendiente("Rehacer el corte");
}
```

Y se usa:

| Dónde | Cómo |
|---|---|
| XAML | `Content="{i:T RecortesExportar}"` |
| C# | `Textos.Instancia.RecortesExportar` |

El XAML necesita `xmlns:i="clr-namespace:Ondine.Localizacion"` en su raíz.

### Por qué no se usa `.resx`

Un fichero de recursos separa la clave de su texto en dos sitios distintos, y
con eso llega siempre lo mismo: **una clave traducida en un idioma y olvidada en
el otro**, que nadie ve hasta que un usuario abre la app en ese idioma. Aquí
cada texto recibe las dos versiones **en la misma línea**, así que una traducción
que falta **no compila**: falta un argumento.

Además, los recursos satélite complican el publicado en fichero único y recortado
que usa la herramienta de terminal. Esto son propiedades normales.

### Reglas del texto

- **El inglés va primero.** Es el idioma por defecto.
- **Inglés británico**: *analyse*, *licence*, *organise*, *colour*.
- **Ni una raya larga** (`—` ni `–`) en ningún texto visible. Guion normal.
- **Marcadores de formato** (`{0}`, `{1}`) idénticos en los dos idiomas.
- **No se traducen**: rutas, nombres de fichero, códecs (HEVC, H.264, AV1),
  extensiones, `ffmpeg`, `ffprobe`, `Ondine`, `Plex`, `Jellyfin`, `Kodi`,
  códigos ISO de idioma (`spa`, `eng`) ni los mensajes de diagnóstico.
- **Lo que se repite va en `Textos.Comun.cs`**, no en cada pantalla: «Cancelar»
  se traduce una vez, no doce, y sobre todo no de doce formas distintas.

### El arnés

Tres capas, y solo la última es la que de verdad protege:

1. **Un gancho** (`.claude/comprobar-traduccion.ps1`) avisa al instante al
   editar cualquier fichero de `src/Ondine`. Solo avisa, nunca bloquea: un
   gancho que impide guardar acaba desactivado, y desactivado no protege nada.
2. **Cinco pruebas** en `tests/Reindex.Tests/TraduccionTests.cs`, dentro de la
   tanda que corre en cada cambio. No comprueban que la traducción sea buena
   —eso no lo juzga una máquina— sino que esté **completa**: que ningún texto
   tenga las dos versiones idénticas por pereza, que toda clave usada en XAML
   exista, que no quede ni un texto a pelo en el XAML, que los marcadores
   cuadren, y que no se cuele una raya larga.
3. **El CI**, que corre esas pruebas y no publica si fallan.

**Si añades una pantalla nueva, añade su fichero parcial.** Si un texto tiene
que ser igual en los dos idiomas de verdad (siglas, unidades, nombres propios),
la prueba ya lo contempla; si es un caso nuevo, se añade a la lista de
excepciones de `EsIgualAProposito`, nunca se relaja la prueba.

---

## Otras normas duras

- **CHANGELOG por versión, no por commit.** Formato *Keep a Changelog*, en
  castellano de cara al usuario. Los triviales (typo, formato, refactor) no
  entran. Al cortar versión: subir `<Version>` en **los dos** `.csproj`, cerrar
  la sección del CHANGELOG y etiquetar `vX.Y.Z`. El CI **verifica el contrato
  del CHANGELOG antes de compilar** y no publica si no cuadra.
- **El motor es compartido, no duplicado.** `src/Ondine.Cli` enlaza los ficheros
  de `src/Ondine`, carpetas enteras y no listas a mano: una lista concreta vuelve
  a romper la CLI la próxima vez que el motor use un tipo nuevo. Ya pasó.
- **Nunca se tocan los originales** salvo petición explícita, y entonces van a la
  papelera, no a borrado. Todo ocurre en la máquina del usuario.
- **Rama y PR por tarea.** Nunca empujar directo a `main`.
- **Los complementos son PROCESOS, no ensamblados cargados dentro.** El contrato
  y las razones están en [`docs/complementos.md`](docs/complementos.md), que es
  también lo que se le da a quien quiera escribir uno. Si se toca el contrato,
  se toca ese documento en el mismo cambio: un contrato documentado a medias es
  peor que uno sin documentar, porque el de fuera se lo cree.

## Estructura

| Carpeta | Qué es |
|---|---|
| `src/Ondine/` | App WPF. `Engine.cs` es el motor; el resto es interfaz y autoactualización. |
| `src/Ondine/Localizacion/` | La espina de la traducción y los textos por pantalla. |
| `src/Ondine.Cli/` | Terminal multiplataforma. Enlaza el motor. |
| `tests/Reindex.Tests/` | Todas las pruebas, sin dependencias externas. |
| `web/` | El sitio de [ondine.hdglabs.com](https://ondine.hdglabs.com), en Astro, también bilingüe. |
| `spot/` | El spot de 44 s, en composiciones HTML. Hay versión castellana e inglesa. |
| `src/Ondine/Complementos/` | El sistema de complementos: manifiesto, descubrimiento e invocación. |
| `ejemplos/complemento-demo/` | Un complemento de ejemplo que no baja nada, para probar la pantalla. |
| `installer/` | Script de Inno Setup. |
| `legacy/` | La versión original en PowerShell con la que nació el proyecto. |

## Comandos

| Tarea | Comando |
|---|---|
| Ejecutar la app | `dotnet run --project src/Ondine` |
| Ejecutar la CLI | `dotnet run --project src/Ondine.Cli -- --help` |
| Pruebas | `dotnet run --project tests/Reindex.Tests` |
| Instalador completo | `pwsh -File build.ps1` |
| Sitio web | `cd web && npm run dev` |
| Renderizar el spot | `cd spot/videos/<proyecto> && npx hyperframes render -q high` |

---
*La app se traduce a la vez que se escribe, no después.*
