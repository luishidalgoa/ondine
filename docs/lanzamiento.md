# Lanzamiento de Ondine — plan y textos

> Escrito el 19 de agosto de 2026. El producto lleva público desde el 8 de agosto y **no se ha
> anunciado en ningún sitio**: esto es lo que falta.

## Qué hay publicado ya

| Cosa | Estado | Cuándo |
|---|---|---|
| Repositorio `luishidalgoa/ondine` | público | — |
| Release **v1.8.0** con instalador y 5 binarios de CLI | publicada, no borrador | 8 ago 2026 |
| Landing `ondine.hdglabs.com` (EN por defecto + ES) | viva, con el spot embebido | último despliegue, 5 ago |
| Spot 16:9 ES, 16:9 EN y vertical | renderizados, con miniatura y ficha de YouTube escrita | — |
| **Anuncio en cualquier sitio** | **nada** | — |

La landing enlaza a `releases/latest`, así que no se queda vieja al sacar versión. Eso está bien
resuelto y no hay que tocarlo antes de lanzar.

## La decisión que manda todo: por dónde NO entrar

El análisis de nicho del propio ROADMAP dice dos cosas que la campaña tiene que obedecer:

> el **renombrado contra catálogo está saturado** (FileBot lleva 15 años, y Sonarr/Radarr lo hacen
> gratis) […] Lo que no hace nadie es **partir episodios en sus historias**.

Si el anuncio entra por «renombra tu biblioteca», el primer comentario es *«esto ya lo hace
Sonarr»* y el hilo se acaba ahí. No porque sea injusto: porque es verdad para el 90 % de la gente
que lo lea.

**Por dónde se entra, entonces:**

1. **Un fichero con varias historias dentro** → `E12a`, `E12b`, `E12c`. Encuentra el corte solo
   (fundido a negro más lo que dice el catálogo) y separa **sin recodificar**. Esto no lo hace
   Sonarr, ni FileBot, ni tvnamer. Es el único titular que no tiene respuesta en el primer
   comentario.
2. **Quitar doblajes y subtítulos sin recomprimir**, con el vídeo idéntico bit a bit. Secundario,
   pero es lo que engancha a quien acumula terabytes.
3. El renombrado va **de tercero y sin presumir**: es lo que hace la herramienta usable, no lo que
   la hace distinta.

**Y hay que decir la fricción en el propio anuncio**, no esconderla: Ondine necesita un catálogo
(`reindex/1.0`), y aunque la app lo genera con una IA a partir de cualquier anexo de episodios, ese
es un paso que Sonarr no te pide. Quien lo lea en el post no se siente engañado; quien lo descubra
después, sí — y lo escribe en los comentarios.

## Antes de disparar

- [x] **La cifra de adelgazar, corregida.** La web decía «1 GB → 133 MB» sin recomprimir y la
      medida es **155 MB → 134 MB**. Arreglado en la rama `fix/cifra-adelgazar-medida`; **tiene que
      estar en `main` y desplegado antes del primer post**. Un 87 % de reducción quitando un
      doblaje es una cuenta que este público hace de cabeza.
- [ ] **Subir el spot a YouTube** (ES, EN y el vertical). El texto está escrito en
      `spot/videos/*/YOUTUBE.md`. Sin el vídeo público no hay nada que enlazar en X ni en LinkedIn.
- [ ] **Leer las reglas de cada sub el mismo día.** No he podido comprobarlas desde aquí: Reddit no
      responde sin sesión. r/PleX y r/jellyfin tienen normas propias sobre herramientas de terceros
      y varias piden *flair*. Un post borrado por norma quema el sub durante meses.

## Orden y calendario

Todo el mismo día es lo que dispara los filtros de spam y, peor, deja sin munición si el primero
sale mal. Un canal por día, y **el segundo se escribe después de leer los comentarios del
primero**.

| Día | Sitio | Por qué ahí |
|---|---|---|
| 1 | **r/selfhosted** | El público exacto y el sitio más tolerante con proyectos propios. Es el ensayo: lo que pregunten aquí corrige todo lo demás. |
| 2 | **Lemmy, comunidad `selfhosted`** | Parte de este público se fue de Reddit. Cuesta poco y el hilo dura. |
| 3 | **Show HN** | Entre semana y por la mañana en EE. UU. Pide un texto distinto: aquí interesa cómo está hecho, no para qué sirve. |
| 4 | **r/DataHoarder** | Solo el ángulo del remux sin recomprimir. Ni una palabra de renombrado. |
| 5 y 6 | **r/jellyfin** y **r/PleX**, separados | Solo si los anteriores no han ido mal. El ángulo aquí es «el episodio sale como Desconocido». |
| Cuando esté el vídeo | **LinkedIn** (ES) y **X/Mastodon** | Red propia. No mueve descargas; sí posiciona. |

---

# Los textos

## 1 · r/selfhosted (EN)

**Título**

```
Ondine — splits episodes that carry two or three stories in one file, and renames them the way Plex and Jellyfin expect
```

**Cuerpo**

```
I maintain a library of old cartoons and anime, and I kept hitting a problem none of the usual
tools solve: one file often contains two or three separate stories. Sonarr and FileBot can rename
it, but they cannot express it — there is no name for "this file is episodes 12a and 12b".

Ondine is what I built for that. It matches every file in a folder against a catalogue, and when a
file carries several stories it names them E12a / E12b / E12c — or splits them into separate files,
finding the cut on its own (black fade plus what the catalogue says each story is) and remuxing
without re-encoding.

The rest is the boring part that makes it usable:

- Nothing is touched without approval. Analysing only proposes; you apply what you tick, and there
  is undo for the whole batch.
- It tells you which episodes you are missing.
- It drops dubs and subtitle tracks without re-encoding — a 155 MB episode goes to 134 MB in 0.6 s,
  video identical bit for bit.
- It compresses with hardware acceleration when you do want to re-encode, and forecasts the final
  size before it starts.

Honest about where it does not win: for anything with a clean TheTVDB entry, Sonarr already renames
it better and I am not going to pretend otherwise. Ondine earns its place on material that has no
clean entry anywhere — regional dubs, old cartoons with different numbering per country, episodes
split by story.

Honest about the friction too: it needs a catalogue file, a JSON listing the episodes. The app can
build one with an AI from any episode-list page, but that is a step Sonarr does not ask of you.

Windows desktop app, plus a CLI for Linux, macOS and Windows sharing the same engine. MIT, free,
no account, no telemetry.

https://ondine.hdglabs.com
```

> **Al publicar:** contestar en los primeros 30 minutos o el hilo se apaga. La pregunta que va a
> salir seguro es *«¿en qué se diferencia de Sonarr o FileBot?»*: ya está contestada en el cuerpo,
> así que remitir a ello sin repetirlo entero.

## 2 · Lemmy, comunidad `selfhosted` (EN)

Mismo título. El cuerpo, el de Reddit a la mitad: aquí los textos largos se leen menos. Dejar los
dos primeros párrafos, la línea de honestidad sobre Sonarr y el enlace; fuera la lista de «lo
aburrido».

## 3 · Show HN (EN)

**Título**

```
Show HN: Ondine – renames and splits TV episodes before Plex or Jellyfin scans them
```

**Primer comentario.** En HN el texto va como comentario del autor, no dentro del envío.

```
Author here. This started as a batch compressor and turned into something else when I tried to
file a cartoon library that has three stories per episode file and a different numbering in every
country.

Two things in it are more interesting than the product itself.

Identification is a cascade with the confidence visible, not a single score. Number plus exact
date, then title similarity, then number plus approximate date — and the UI shows you which rung
matched, in green, amber or red. The reason is that the cheap signal is the one that lies: the
number in a filename very often is not the episode's real number, and a tool that trusts it
silently is worse than one that asks.

The batch corroborates itself. A catalogue with no broadcast dates used to leave everything asking
for a decision, because one matching signal is not enough. Now the batch is the second signal: if
several files, ordered by their own numbers, point at different episodes in the same order, that
backs them up. The subtle bug there was circular — the coherence check was built from the same
filename numbers it was meant to corroborate, so a wrong batch confirmed itself. It now counts only
files identified by title.

Where it does not compete: for anything with a clean TheTVDB entry, Sonarr does the renaming and
does it better. This earns its keep on material that has no clean entry — and on splitting one file
into its stories, which as far as I know nothing else does.

Windows GUI plus a CLI for Linux/macOS/Windows over the same engine. C#/.NET 9, MIT.

https://github.com/luishidalgoa/ondine
```

## 4 · r/DataHoarder (EN)

Sin una palabra de renombrado.

**Título**

```
Dropping unused dub and subtitle tracks without re-encoding: 155 MB → 134 MB in 0.6 s, video bit-identical
```

**Cuerpo**

```
Nothing new in the technique — it is a remux, ffmpeg has done it forever — but I got tired of
writing the command line, so I put it behind a right-click: it shows every track described in plain
language (Audio · Spanish · 2 channels · 129 kbps, not "spa"), you tick what goes, and it tells you
how much you will save before touching anything. The original goes to the recycle bin, so Ctrl+Z
brings it back.

Numbers so nobody has to ask: on a real episode, 155 MB down to 134 MB in 0.6 s. That is 13%, not
the 80% you get from re-encoding — the point is that the video is identical bit for bit, not that
it is dramatic. Across a series with three dubs it adds up; on one file it does not.

It is part of a bigger tool for preparing libraries before Plex or Jellyfin scans them, but this
part stands alone and works on any file.

https://ondine.hdglabs.com
```

## 5 · r/jellyfin y r/PleX (EN) — dos días distintos y dos textos distintos

**Título, jellyfin**

```
Fixing the "Unknown episode" case Jellyfin cannot fix: files with two stories inside, and dubs with their own numbering
```

**Título, Plex**

```
For libraries Plex shows as Unknown: matching episodes against your own catalogue before the scan
```

**Cuerpo.** Cambiar el nombre del servidor en cada uno.

```
Jellyfin shows a beautiful library, but only when the filenames already say what the file is. When
they do not, it gives up and the episode sits there as Unknown, with no artwork and no synopsis.

Ondine is the step before the scan. It matches each file against a catalogue and proposes the
canonical name; nothing is renamed without your approval, and there is undo for the whole batch.

The case it exists for, and the one the usual tools cannot express: a file carrying two or three
stories. It names them E12a / E12b / E12c, or splits them into separate files without re-encoding.

If your library is mainstream and has clean TheTVDB entries, Sonarr already covers you and this is
not for you. It is for regional dubs, old cartoons and anything numbered differently depending on
the country — which is exactly the material that ends up Unknown.

It needs a catalogue file, which the app can generate with an AI from any episode-list page.

Free, MIT, Windows app plus a CLI. https://ondine.hdglabs.com
```

## 6 · LinkedIn (ES)

```
Plex y Jellyfin enseñan una biblioteca preciosa, pero solo si los ficheros ya están bien nombrados.
Cuando no lo están, se rinden: el episodio sale como «Desconocido».

Ondine es el paso de antes, y ya está publicado: aplicación de escritorio para Windows, herramienta
de terminal para Linux y macOS, gratis y de código abierto.

Lo que me interesó del problema no es renombrar ficheros —eso lleva quince años resuelto— sino el
caso que ninguna herramienta sabe nombrar: un fichero que trae dos o tres historias dentro. Ondine
las numera 12a, 12b y 12c, encuentra el corte sola y las separa sin recodificar.

Y una decisión de la que estoy razonablemente orgulloso: la identificación enseña por qué señal ha
acertado —número y fecha, título, o solo número— con la confianza a la vista. La señal barata es la
que miente: el número del nombre de un fichero muy a menudo no es el número real del episodio, y una
herramienta que se lo cree en silencio hace más daño que una que pregunta.

Nada se toca sin aprobación, y hay deshacer.

https://ondine.hdglabs.com
```

## 7 · X y Mastodon (EN)

```
Ondine is out. It renames TV episodes the way Plex and Jellyfin expect — and handles the case
nothing else can name: one file with two or three stories inside, split without re-encoding.

Free, MIT, Windows app + CLI.
https://ondine.hdglabs.com
```

---

## Qué mirar después

No hay analítica en la landing y no la voy a poner: se mide con lo que ya hay.

- **Descargas por release** (`gh release view v1.8.0 --json assets`). Es la única cifra que importa
  y GitHub la da gratis.
- **Estrellas del repo** antes y después de cada post, para saber qué canal movió algo.
- **Los comentarios**, que es lo que de verdad se saca de esto: cada «¿y por qué no usas X?» es una
  entrada de ROADMAP o una frase que falta en la landing.

Si el post de r/selfhosted no pasa de veinte votos, el problema no es el canal: es que el titular ha
entrado por el renombrado y no por lo que nadie más hace. Reescribir antes de seguir.
