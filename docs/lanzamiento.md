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
- [x] **Reglas de r/selfhosted, leídas.** Y cambian el plan, ver abajo. Faltan las de r/jellyfin,
      r/PleX y r/DataHoarder, que hay que leer el mismo día de cada post.

## Lo que dicen las reglas de r/selfhosted, que manda sobre todo lo demás

Leídas el 19 de agosto. Dos reglas afectan de lleno:

**Regla 6 — proyectos nuevos.** *«Only in the current "New Project Megathread", you may post
projects that are younger than 3 months (measured by first public presence, e.g. git commit).»* Y
el propio megahilo lo remacha: *«Standalone new project posts will be removed and the author will
be redirected to the current week's megathread.»*

El primer commit de Ondine es del **20 de julio de 2026**. Tiene **un mes**. Así que un hilo propio
**se borra**, y de paso quema el sub. No es interpretación: lo dice el texto del megahilo.

**Regla 5 — herramientas, los miércoles.** *«On Wednesdays, you may post dashboards or tools that
help self-hosters provided it is flaired as such, even if they are not self-hosted.»* Esta es la
puerta buena, y resuelve además la duda de si Ondine encaja en el sub sin ser un servicio que se
aloje uno mismo: encaja, y lo dice la regla.

Las dos juntas dan la única lectura segura:

- **Hoy**: comentario en el **New Project Megathread** vigente, con la plantilla que piden.
- **Desde el 20 de octubre de 2026** (tres meses del primer commit): hilo propio, **en miércoles**
  y con el *flair* de herramienta. El texto largo de más abajo es para ese día, no para hoy.

Y una cosa que el sub deja clarísima, porque está en la plantilla y en casi todos los hilos de
proyecto: **hay que declarar el uso de IA**. La comunidad está saturada de proyectos generados con
IA y presentados como si no; declararlo de frente es lo que separa un hilo que se lee de uno que se
entierra.

## Orden y calendario

Todo el mismo día es lo que dispara los filtros de spam y, peor, deja sin munición si el primero
sale mal. Un canal por día, y **el segundo se escribe después de leer los comentarios del
primero**.

| Cuándo | Sitio | Por qué ahí |
|---|---|---|
| Hoy | **New Project Megathread de r/selfhosted** | Es el único sitio donde un proyecto de un mes puede ir en ese sub. Menos visible que un hilo propio, pero legal y con gente que lo lee a diario. |
| +1 día | **Show HN** | No tiene regla de antigüedad, así que aquí sí puede ir el lanzamiento de verdad. Entre semana y por la mañana en EE. UU. Pide otro texto: interesa cómo está hecho, no para qué sirve. |
| +2 días | **Lemmy, comunidad `selfhosted`** | Parte de este público se fue de Reddit. Cuesta poco y el hilo dura. |
| +3 días | **r/DataHoarder** | Solo el ángulo del remux sin recomprimir. Ni una palabra de renombrado. Leer sus reglas ese día. |
| +4 y +5 | **r/jellyfin** y **r/PleX**, separados | Solo si lo anterior no ha ido mal. El ángulo aquí es «el episodio sale como Desconocido». Leer sus reglas ese día. |
| Cuando esté el vídeo | **LinkedIn** (ES) y **X/Mastodon** | Red propia. No mueve descargas; sí posiciona. |
| **20 oct 2026, miércoles** | **Hilo propio en r/selfhosted**, con *flair* de herramienta | Ya no aplica la regla 6. Es el post grande, y para entonces habrá comentarios de todo lo anterior con los que afinarlo. |

---

# Los textos

## 1 · New Project Megathread de r/selfhosted (EN) — **el de hoy**

Va como **comentario de primer nivel** en el megahilo de la semana, con la plantilla que pide el
sub. Enlace del vigente:
`https://old.reddit.com/r/selfhosted/comments/1vnoqxf/new_project_megathread_week_of_13_aug_2026/`
(sale uno nuevo cada viernes; comprobar cuál es el vigente antes de pegar).

```
**Project Name:** Ondine

**Repo/Website Link:** https://github.com/luishidalgoa/ondine · https://ondine.hdglabs.com

**Description:** The step you run before Plex, Jellyfin or Kodi scans your library. Those servers
show you a beautiful library, but only when the filenames already say what the file is; when they
do not, the episode sits there as Unknown.

The case I built it for, and the one the usual tools cannot even express: one file carrying two or
three separate stories. Sonarr and FileBot can rename that file, but there is no name for "this is
episodes 12a and 12b". Ondine matches each file against a catalogue, names them E12a / E12b / E12c,
and can split them into separate files — finding the cut on its own (black fade plus what the
catalogue says each story is) and remuxing without re-encoding.

The rest: nothing is renamed without your approval and there is undo for the whole batch; it tells
you which episodes you are missing; it drops dubs and subtitle tracks without re-encoding (a 155 MB
episode goes to 134 MB in 0.6 s, video identical bit for bit); and it compresses with hardware
acceleration when you do want to re-encode, forecasting the size before it starts.

Where it does not win, so nobody wastes an evening: if your library is mainstream and has clean
TheTVDB entries, Sonarr already renames it better. Ondine earns its keep on material with no clean
entry anywhere — regional dubs, old cartoons numbered differently per country, episodes split by
story. And the friction: it needs a catalogue file, a JSON listing the episodes. The app can build
one with an AI from any episode-list page, but that is a step Sonarr does not ask of you.

**Deployment:** Not a service and not a container — it is a desktop app plus a CLI, so there is no
Docker image and I am not going to pretend otherwise. Windows: per-user installer from the releases page, which
auto-updates itself. Linux and macOS: a single CLI binary (x64 and arm64) that
shares the same engine, in the same release. Needs ffmpeg on PATH. README covers both, and the
catalogue format is documented in docs/catalogo-reindex.md. MIT, free, no account, no telemetry.
https://github.com/luishidalgoa/ondine/releases/latest

**AI Involvement:** Heavy and worth stating plainly: it is written with AI coding assistants
(Claude Code) throughout. What is mine is the architecture, every product decision, and the review
of every change before it lands. One house rule that is relevant to you: every number in the README
and on the site is measured, not estimated — the 155 MB to 134 MB above is a real file, and today I
had to correct the site because it claimed a figure I could not reproduce.
```

> **Al publicar:** contestar en los primeros 30 minutos. La pregunta segura es *«¿en qué se
> diferencia de Sonarr o FileBot?»*, ya contestada en el cuerpo. La segunda, por cómo está el sub,
> será sobre la IA: la respuesta es la que ya va escrita, sin defenderse.

## 1-bis · Hilo propio en r/selfhosted (EN) — **para el 20 de octubre, no para hoy**

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

## 3 · Show HN (EN) — **bloqueado, no es cosa del texto**

Intentado el 19 de agosto. HN lo rechaza antes de publicar y redirige a `/showlim`:

> *«We're temporarily restricting Show HNs because of a massive influx, mostly by users who aren't
> yet familiar with the site or its culture. […] Take some time to get to know the community, become
> a good contributor, and then it will be fine to post an occasional Show HN.»*

La cuenta `luishidalgoa` tiene **1 punto de karma y cero envíos**, que es justo el perfil que están
frenando. No hay nada que arreglar en el título ni en el enlace: comprobado, no se publicó nada.

**Y no se rodea.** Mandarlo sin el prefijo «Show HN:» es exactamente lo que la restricción intenta
evitar, y ahí lo que se juega es la cuenta. La única vía es la que dicen: participar en la
comunidad —comentar donde se tenga algo que aportar— y volver a intentarlo cuando la cuenta tenga
recorrido. Semanas, no días.

El texto de abajo queda escrito para ese momento.

**Título** — 80 caracteres justos, que es el límite de HN. La versión con «them» al final se pasa
en cinco y la corta el formulario. Y empieza por «splits» a propósito: es lo que no hace nadie.

```
Show HN: Ondine – splits and renames TV episodes before Plex or Jellyfin scans
```

**URL:** `https://github.com/luishidalgoa/ondine`

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
