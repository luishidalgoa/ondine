# Películas

Cómo Ondine identifica y coloca una biblioteca de películas, y por qué está
decidido así.

Las series tienen catálogo: alguien mantiene la lista de episodios y se importa
una vez para reutilizarla trescientas. Las películas **no lo tienen** — una
película es solo una película, no hay anexo del que sacar filas—. Así que aquí la
verdad sale de dos sitios: el nombre del fichero, y opcionalmente una base de
datos pública.

## Lo que funciona sin red, que es la mayor parte

Sin conexión y sin configurar nada:

1. **Limpiar el nombre** — se quita la morralla de release (`1080p`, `x264-GRUPO`,
   separadores) con el mismo `SinMorralla` que usan las series.
2. **Sacar título y año** del fichero, y del **nombre de la carpeta** cuando el
   fichero no trae año. Medido sobre una biblioteca real de 75 películas: **52 no
   traen año en el nombre del fichero**, y en buena parte de esos casos la
   carpeta sí lo tiene.
3. **Proponer `Título (Año)`**, que es lo que Plex y Jellyfin esperan encontrar.

Las trampas de esto están en `TituloDePelicula`, cada una con su prueba: títulos
que **son** un año («1917», «Blade Runner 2049»), años de reedición («Alien 1979
REMASTERED (2003)»), extras que no son la película, y películas partidas en
`cd1`/`cd2`.

Y una regla que salió de medir esa biblioteca: **una carpeta con más de una
película no se desmonta**. 53 de las 75 vivían en carpetas de colección
—«Disney», «Bob Esponja», «Paco Martínez Soria»—. Desmontarlas sería lo correcto
para el escáner y destruiría la forma en que su dueño mira su biblioteca.

## Identificar contra TMDb (opcional)

Lo que el nombre no puede arreglar: un título mal escrito, dos películas que se
llaman igual, o media biblioteca en castellano y media en inglés. Para eso se
consulta **TMDb**, que es el estándar de facto y lo que usan Jellyfin, Kodi y
Plex.

### Apagado de fábrica

Identificar significa mandar a un servicio de fuera los títulos de lo que hay en
el disco de alguien. Una app que ordena tu disco no debería contar qué tienes sin
que se lo pidas, así que **lo enciende el usuario** en Preferencias y puede
apagarlo.

**Qué sale de esta máquina:** el título ya limpio y el año. Nunca el nombre del
fichero — la resolución, el códec y el nombre del grupo de release no hacen falta
para identificar nada y dicen de dónde salió el fichero. Hay una prueba que lo
comprueba justo con eso: de `Pelicula.2019.1080p.BluRay.x264-GRUPO.mkv` sale
`query=Pelicula&year=2019` y ni una palabra más.

### La clave (decidido el 20 de agosto de 2026)

Dos vías, y se usan las dos:

- La **clave oficial va horneada** en las builds de la release. Sale del secreto
  `ONDINE_TMDB_KEY` del repo, entra como propiedad de MSBuild y viaja como
  metadato del ensamblado, igual que `UpdateRepo`.
- **Preferencias tiene un campo que la sobrescribe**, cifrado con DPAPI y atado a
  la cuenta de Windows, como la clave del modelo de lenguaje.

Por qué las dos: pedirle a alguien que solo quiere ordenar su carpeta que se
registre en TMDb es un muro, y esta app existe para ahorrar trabajo. Y sin el
campo, quien clone el repo y compile —sin el secreto— se queda con una función
muerta y sin explicación.

Lo que esto **no** es: un secreto. Una clave dentro de un binario se saca con un
editor hexadecimal. Se asume a propósito porque TMDb limita **por IP y no por
clave** —así que no se agota entre todos los usuarios— y porque sus términos no lo
prohíben mientras Ondine sea gratis. Es lo que hacen Jellyfin, Kodi y
tinyMediaManager. **Si Ondine alguna vez cobra, esta decisión caduca.**

Una build sin clave arranca igual: la identificación queda apagada y **lo dice**,
en vez de fallar por dentro con un 401 que parece un problema de red.

### Poner el secreto en el repo

```bash
gh secret set ONDINE_TMDB_KEY --repo luishidalgoa/ondine
```

Lo pide por la entrada estándar, así que la clave no queda en el historial del
terminal. Vale tanto la «API Key (v3 auth)» como el «API Read Access Token (v4
auth)» de la página de ajustes de TMDb: Ondine distingue una de otra **por la
forma** y la manda por donde corresponda —la v3 como parámetro, el token v4 en la
cabecera—, porque quien pega la que no toca solo recibe un 401 pelado.

## Desde la pantalla

Identificar es un **paso aparte y a petición**, no algo que pase al abrir la
ventana: una app de disco que sale a internet sola, sin que se lo pidas, no es lo
que nadie instaló. Así que primero se ve el plan tal y como sale de los nombres,
y el botón «Identificar con TMDb» está al lado del filtro.

Cuando está apagado, el botón se queda **visible y apagado con el motivo al
lado**. No escondido, y no apagado a secas: un botón apagado sin explicación se
lee como una función rota en vez de una que hay que encender.

Cada fila enseña, además de a dónde iría, **qué se encontró y por qué señal** —en
verde lo que se va a aplicar, en ámbar lo que no—. Se enseña también cuando
acierta: una confianza que solo aparece al fallar no se aprende a leer.

## La cascada de confianza

Es lo que Ondine aporta encima del proveedor. El dato lo da TMDb; lo que no da
nadie es la decisión con la señal a la vista. La regla que manda:

> **Una película mal identificada es peor que una sin identificar.**

Si «El Padrino II» acaba renombrado como «El Padrino», meses después nadie sabe
qué pasó. Así que aquí se es más estricto que en el resto de la app.

| Grado | Qué significa | Qué se hace |
|---|---|---|
| **Segura** | Las señales cuadran | Se puede aplicar |
| **Dudosa** | Candidata plausible, señales incompletas | Se enseña y **no** se toca |
| **Ninguna** | Nada, o nada que se pueda separar | Se enseña el problema |

Y la señal por la que se decidió, que es lo que se le enseña a quien lo mire:

- **`AnioYTitulo`** — título y año cuadran. Un año de diferencia se admite: es el
  estreno en otro país, no otra película.
- **`TituloOriginal`** — cuadró por el título **original** y no por el traducido.
  Es lo que permite saber que «The commuter» y «El pasajero» son la misma
  película, y en una biblioteca real están las dos formas mezcladas.
- **`SinAnio`** — el fichero no traía año. Se exige entonces un parecido de
  **0,95** y ninguna competencia, más alto que el 0,78 de los episodios: allí una
  equivocación se ve porque el número no cuadra con la lista, y aquí no hay lista.
- **`SoloTitulo`** — el título cuadra y el año no. Puede ser el remake. **Duda.**
- **`Empate`** — dos candidatas igual de buenas y nada que las separe: «Psicosis»
  de 1960 y de 1998, sin año en el fichero. **No se elige**; acertar la mitad de
  las veces no es identificar.
- **`TituloFlojo`** — el buscador devolvió cosas y ninguna se parece.
- **`SinCandidatos`** — no encontró nada, que es un resultado y no un fallo.

## Sin red, y sin preguntar dos veces

Lo consultado se guarda en `tmdb-cache.json`, junto al resto de los datos del
usuario. No caduca: lo que se guarda es **qué película es**, y eso no cambia —una
caducidad convertiría «funciona sin red» en «funcionó sin red durante un mes»—.

Un «no encontré nada» también se recuerda, porque si no cada análisis vuelve a
preguntar por las que nunca se van a encontrar, que son las que más se repiten.
Lo que **no** se guarda es un «no se pudo preguntar»: un rato sin conexión dejaría
esa película marcada como imposible para siempre.

## Dónde está cada cosa

| Fichero | Qué hace |
|---|---|
| `Reindex/TituloDePelicula.cs` | Leer título, año, extras y partes del nombre |
| `Reindex/PlanDePeliculas.cs` | Qué se mueve, qué se renombra y qué se deja quieto |
| `Reindex/Mudanza.cs` | Mover de verdad, con compañeros y con deshacer |
| `Peliculas/ClaveDeTmdb.cs` | De dónde sale la clave |
| `Peliculas/Tmdb.cs` | La consulta: qué se manda y qué se entiende |
| `Peliculas/IdentificacionDePelicula.cs` | La cascada de confianza |
| `Peliculas/CacheDePeliculas.cs` | Lo ya preguntado |
| `Peliculas/AjustesDeTmdb.cs` | El interruptor y la clave del usuario |
