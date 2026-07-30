# Encargo: logotipo de Ondine

> Esto **no** es una descripción del logotipo que quiero. Es una descripción de qué es la app, para
> qué sirve y a quién le habla. El diseño lo pones tú.

---

## Qué es Ondine

Ondine prepara una biblioteca personal de series y películas **antes** de que Plex, Jellyfin o Kodi
la escaneen.

Esos servidores enseñan una biblioteca preciosa —carátulas, sinopsis, temporadas ordenadas— pero
solo si los ficheros ya están bien nombrados y colocados. Cuando no lo están, se rinden: el
episodio aparece como «Desconocido», la película se confunde con otra, o directamente no sale.
Arreglarlo a mano es trabajo de horas, fichero a fichero.

Ondine es el paso intermedio. **No es un destino, es algo por lo que se pasa.** Entra el desorden
de una carpeta de descargas y sale una biblioteca que el servidor reconoce.

Hace tres cosas, y las tres van de lo mismo: subir la calidad de los datos de una colección.

1. **Comprimir**, para que quepa. Reduce el tamaño manteniendo la calidad, con aceleración por
   hardware. También sabe quitar doblajes y subtítulos que no vas a usar **sin recomprimir nada**:
   un episodio de 155 MB baja a 134 MB en seis décimas de segundo, y el vídeo queda idéntico bit a
   bit.
2. **Ordenar**, para que el servidor lo reconozca. Compara cada fichero con un catálogo de
   episodios y le pone su nombre canónico, aunque venga con la numeración cambiada y la morralla de
   la descarga.
3. **Partir**, que es lo que no hace nadie más. Muchas series de dibujos meten dos o tres historias
   dentro de un mismo capítulo. Ondine entiende esa estructura, encuentra el corte solo y separa el
   fichero en piezas independientes, cada una con su título.

## Qué se siente al usarla

- **Nada se toca sin permiso.** La app propone y tú apruebas. Lo dudoso lo deja marcado en vez de
  inventar. Lo que borra va a la papelera, nunca a borrado definitivo, y siempre se puede deshacer.
- **Todo ocurre en tu máquina.** No sube tus ficheros a ninguna parte ni pide cuenta de nada.
- **Es una herramienta de precisión, no una app de consumo.** Enseña la confianza que tiene en cada
  decisión con un color, y te deja llevarle la contraria.

## A quién le habla

Gente que se monta su propio servidor de medios en casa. Técnicos o semitécnicos, del mundo del
*self-hosting* y el *homelab*. Desconfían del software cerrado, valoran el control y la precisión, y
tienen buen ojo para detectar cuándo algo está hecho con cuidado y cuándo no. Es un público que
premia la sobriedad y detesta lo pomposo.

El vecindario donde vive: Plex, Jellyfin, Kodi, Emby, Sonarr, Radarr. Marcas de nombre corto,
inventado y sin adornos.

## El nombre

**Ondine** viene del latín *unda*, «ola» — la misma raíz que «onda» y «ondina» en español. Es la
ninfa de agua del mito europeo. Se eligió por eso: por sonar a algo que fluye y transforma, y
porque la familia acuática es donde ya vive Jellyfin (*jellyfish* + *dolphin*).

Se pronuncia *on-DÍN*. Dos sílabas.

## De dónde venimos, y por qué se cambia el logotipo

La app se llamaba **ShrinkStudio** y era un compresor de vídeo. Su icono lo cuenta: un cuadrado
redondeado con degradado, un triángulo de *play* en el centro y dos flechas apuntando hacia dentro.
Dice «herramienta de vídeo» y no dice nada de lo que la app hace hoy.

El nombre ya se cambió. Falta el logotipo.

---

## Restricciones

**Obligatorias:**

- **No puede ser un cuadrado (ni un *squircle*) con fondo de color y un símbolo encima.** Eso es
  justo lo que hay ahora. Quiero una **marca que se sostenga sola**, sin contenedor.
- Tiene que **leerse a 16 px** —favicon, barra de tareas, bandeja del sistema— y aguantar a 512 px
  sin verse pobre.
- Tiene que funcionar **sobre fondo oscuro y sobre fondo claro**, y también **en un solo color**
  (para el icono monocromo de la bandeja y para grabarlo o estamparlo).
- Entregar en **vectorial (SVG)**.

**Paleta de la app** — es el código de color real, el que se ejecuta hoy:

| Uso | Color |
|---|---|
| Fondo | `#161826` |
| Superficie | `#232532` |
| Campos | `#292B31` |
| Texto | `#E9E9ED` |
| **Acento** | **`#968AE0`** (blurple) |
| Acento claro | `#B5ABFC` · `#D2CEFD` · `#E7E5FE` |
| Acento oscuro | `#796CBF` · `#5D5294` · `#423A6A` |

> Nota: existe un `docs/design-brief.md` que propone virar a turquesa `#6CE8D0`. Ese rediseño
> **está pendiente y sin fecha**. Manda la paleta de arriba.

**Lo que el logotipo NO debe decir:**

- Que es un compresor o un editor de vídeo. Es lo que estamos dejando atrás.
- Los tópicos del sector: bobinas de película, claquetas, botones de *play*, flechas de compresión.
  Están gastados, y además son literalmente el icono que sustituimos.
- Nada de brillos, biselados ni degradados de app de móvil de 2014.

## Cómo se va a juzgar

Poniéndolo a 16 px al lado de los iconos de Plex, Jellyfin y Sonarr, en una barra de tareas oscura.
Si a ese tamaño se distingue, se recuerda y no parece un reproductor de vídeo, está bien.

## Entregables

1. La marca en SVG, sobre fondo oscuro y sobre fondo claro.
2. Versión monocroma de un solo trazo.
3. Prueba a 16, 32, 64 y 256 px.
4. Dos o tres direcciones distintas antes de refinar ninguna.
