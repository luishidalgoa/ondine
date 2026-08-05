"""Complemento de YouTube para Ondine: LISTAR una lista de reproduccion.

Lee los metadatos publicos de una lista -titulo, miniatura, duracion- y los
entrega a Ondine para que los coteje con el catalogo abierto. No descarga nada.

Se apoya en yt-dlp, que tiene que estar en el PATH. Se usa `--flat-playlist`, que
pide la ficha de la lista y NO toca los videos: es una peticion, no cuarenta.
"""
import io
import json
import re
import shutil
import subprocess
import sys

# La salida va en UTF-8 pase lo que pase. La consola de Windows arranca en una
# pagina de codigos que se come los acentos, y un titulo con la ñ rota no casa
# con nada del catalogo.
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")


# El titulo tal cual viene trae el nombre del canal delante y la coletilla del
# episodio detras: «Doraemon | El controlador del mar | Episodio 426 en español».
# Todo eso es ruido contra un catalogo que dice solo «El controlador del mar», y
# hunde el parecido lo justo para que Ondine se calle.
#
# Limpiarlo es trabajo del complemento y no de Ondine: quien conoce la fuente es
# quien la envuelve. Ondine compara lo que le den.
_COLETILLA = re.compile(
    r"^\s*(?:episodio|capitulo|capítulo|ep\.?)\s*\d+.*$|^\s*en\s+espa[nñ]ol\s*$",
    re.IGNORECASE,
)


def segmentos(texto):
    """Los trozos con contenido, en orden y sin coletillas."""
    if "|" not in texto:
        t = texto.strip()
        return [t] if t else []

    trozos = [t.strip() for t in texto.split("|") if t.strip()]
    return [t for t in trozos if not _COLETILLA.match(t)]


def limpiar(titulo):
    """Se queda con el trozo que de verdad es el titulo del episodio."""
    utiles = segmentos(titulo)

    # Si al quitar coletillas queda mas de uno, el titulo es el MAS LARGO: el
    # nombre de la serie es corto y se repite en todos, el titulo no.
    if len(utiles) > 1:
        return max(utiles, key=len)
    if utiles:
        return utiles[0]
    return titulo.strip()


def _clave(t):
    """Para comparar trozos sin que un espacio de mas los haga distintos."""
    return " ".join(t.split()).casefold()


def titulo_completo(titulo, descripcion):
    """El titulo, y el segundo segmento si la descripcion demuestra que lo hay.

    Hay videos titulados con UNA historia que en realidad traen DOS, y la
    segunda solo asoma en la descripcion. Sin esto Ondine coteja media cinta
    contra el catalogo y la da por buena: el fichero entra como completo y la
    historia que falta no la reclama nadie.

    La descripcion NO se interpreta -se comprueba-. Solo manda cuando contiene
    todos los trozos del titulo y ademas alguno mas; entonces es el mismo
    titulo escrito largo y lo que sobra son historias. Donde no lo contiene, no
    se afirma nada y manda el titulo.

    Esa comprobacion es lo que hace que esto valga fuera de este canal: cada uno
    escribe la descripcion a su manera -creditos, «suscribete», nada- y ninguna
    de esas pasa la prueba de contener el titulo. El caso raro sale por donde
    debe, callando, en vez de inventarse una historia que no existe.
    """
    corto = limpiar(titulo)
    if not descripcion or not descripcion.strip():
        return corto

    # Solo la PRIMERA linea. Debajo van creditos y avisos legales separados por
    # las mismas barras -«Produced by SHIN-EI Animation | and TV Asahi»- y
    # colarlos como historias es peor que no mirar: no casan con nada.
    primera = descripcion.strip().splitlines()[0]

    del_titulo = segmentos(titulo)
    de_la_desc = segmentos(primera)

    claves = {_clave(s) for s in de_la_desc}
    if len(de_la_desc) <= len(del_titulo):
        return corto
    if not all(_clave(s) in claves for s in del_titulo):
        return corto

    # Lo que en el titulo no era el titulo es el nombre de la serie: se cae
    # tambien de la descripcion. El resto son historias, en su orden.
    ruido = {_clave(s) for s in del_titulo if _clave(s) != _clave(corto)}
    historias = [s for s in de_la_desc if _clave(s) not in ruido]

    # « + » porque es como Ondine une historias, y su motor lo vuelve a partir.
    return " + ".join(historias) if historias else corto


def decir(**campos):
    """Una linea, un mensaje. Se vacia en el acto para que Ondine lo pinte segun llega."""
    print(json.dumps(campos, ensure_ascii=False), flush=True)


def error(mensaje):
    decir(tipo="error", mensaje=mensaje)
    sys.exit(1)


def listar(fuente):
    if not fuente:
        error("Pega el enlace de una lista de reproduccion en la casilla de fuente.")

    exe = shutil.which("yt-dlp") or shutil.which("yt-dlp.exe")
    if not exe:
        error("Hace falta yt-dlp y no esta en el PATH. Instalalo y vuelve a probar.")

    try:
        # --flat-playlist: la ficha de la lista, sin entrar en cada video. La
        # diferencia es una peticion frente a una por episodio, y con listas de
        # cientos eso es la diferencia entre segundos y minutos.
        salida = subprocess.run(
            [exe, "--flat-playlist", "-J", "--no-warnings", fuente],
            capture_output=True, text=True, encoding="utf-8", timeout=180,
        )
    except subprocess.TimeoutExpired:
        error("yt-dlp ha tardado demasiado en contestar.")

    if salida.returncode != 0:
        # Su ultima linea suele ser la util; el resto es ruido de progreso.
        detalle = (salida.stderr or "").strip().splitlines()
        error(detalle[-1] if detalle else "yt-dlp no ha podido leer esa lista.")

    try:
        ficha = json.loads(salida.stdout)
    except json.JSONDecodeError:
        error("yt-dlp ha contestado algo que no es JSON.")

    # Sin titulo no hay nada que cotejar. Son los borrados y los privados: yt-dlp
    # los deja en la lista con su id y el resto en blanco. Pasarlos adelante los
    # pinta como un episodio mas llamado «NA», y un hueco disfrazado de episodio
    # es peor que un hueco.
    todas = [e for e in (ficha.get("entries") or []) if e]
    entradas = [e for e in todas if (e.get("title") or "").strip()]
    if not entradas:
        error("Esa lista no tiene videos, o no es publica.")

    perdidos = len(todas) - len(entradas)
    aviso = f" ({perdidos} ya no estan disponibles)" if perdidos else ""
    decir(tipo="progreso", avance=0.0,
          texto=f"{len(entradas)} videos{aviso}. Leyendo las descripciones...")

    descripciones = _descripciones(exe, fuente, len(entradas))

    for i, e in enumerate(entradas):
        # La miniatura: la mas grande de las que trae. Vienen de menor a mayor,
        # asi que la ultima es la buena; en una fila de 104x58 la pequeña se ve
        # pastosa y la grande no cuesta nada mas.
        miniatura = None
        for t in (e.get("thumbnails") or []):
            if t.get("url"):
                miniatura = t["url"]

        titulo = e.get("title") or ""
        decir(
            tipo="elemento",
            id=e.get("id") or "",
            titulo=titulo_completo(titulo, descripciones.get(e.get("id"))),
            miniatura=miniatura,
            duracion=e.get("duration"),
        )
        decir(tipo="progreso", avance=(i + 1) / len(entradas), texto="")

    decir(tipo="hecho", ficheros=[])


# Cada ficha empieza por esta marca. NO se parte por lineas: una descripcion las
# trae dentro, y contarlas dejaria los campos corridos a partir del primer video
# con dos parrafos -que son casi todos-.
_MARCA = "@@ONDINE@@"


def _descripciones(exe, fuente, cuantos):
    """El id y la primera linea de la descripcion de cada video.

    Esto SI entra en cada video, al contrario que el listado: es una peticion
    por episodio. Se paga porque es donde aparece la segunda historia de los
    que van titulados con una sola, y ese es justo el error caro -dar por
    completo un fichero al que le falta la mitad-.

    Si falla no se aborta: se devuelve lo que haya. Quedarse sin lista por no
    poder leer un extra seria cambiar un dato de mas por todos los datos.
    """
    try:
        salida = subprocess.run(
            [exe, "--no-warnings",
             # Sin esto YouTube contesta «This video is not available» a la
             # ficha del video, aunque la lista se lea sin problema.
             "--extractor-args", "youtube:player_client=web_safari",
             # Solo se quieren los metadatos. Sin esto yt-dlp busca ademas un
             # formato que bajar y aborta la ficha entera cuando no lo hay.
             "--ignore-no-formats-error",
             # En Windows yt-dlp escribe con la pagina de codigos de la consola:
             # «confesión» salia como el byte f3 suelto, que no es UTF-8 valido y
             # se perdia. El listado se libraba por venir en JSON, que escapa lo
             # que no es ASCII; la descripcion sale en crudo y no.
             #
             # Se pide aqui y no por PYTHONIOENCODING porque yt-dlp se distribuye
             # como ejecutable congelado y esa variable no le llega.
             "--encoding", "utf-8",
             "--print", _MARCA + "%(id)s@@%(description)s",
             fuente],
            capture_output=True, text=True, encoding="utf-8",
            errors="replace", timeout=60 + 20 * cuantos,
        )
    except (subprocess.TimeoutExpired, OSError):
        return {}

    fuera = {}
    for ficha in (salida.stdout or "").split(_MARCA)[1:]:
        ident, _, resto = ficha.partition("@@")
        if ident.strip():
            fuera[ident.strip()] = resto
    return fuera


def main():
    if len(sys.argv) < 2:
        error("No se ha dicho que hacer.")

    orden = sys.argv[1].lower()
    if orden == "listar":
        listar(sys.argv[2] if len(sys.argv) > 2 else "")
    elif orden == "traer":
        # Listar y cotejar es leer metadatos publicos. Descargar es otra cosa, y
        # esta lista en concreto la tiene restringida su titular de derechos.
        error("Este complemento solo lee la lista y la coteja con tu catalogo. "
              "No descarga.")
    else:
        error(f"No se que hacer con la orden '{orden}'.")


if __name__ == "__main__":
    main()
