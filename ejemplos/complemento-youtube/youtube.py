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


def limpiar(titulo):
    """Se queda con el trozo que de verdad es el titulo del episodio."""
    if "|" not in titulo:
        return titulo.strip()

    trozos = [t.strip() for t in titulo.split("|") if t.strip()]
    utiles = [t for t in trozos if not _COLETILLA.match(t)]

    # Si al quitar coletillas queda mas de uno, el titulo es el MAS LARGO: el
    # nombre de la serie es corto y se repite en todos, el titulo no.
    if len(utiles) > 1:
        return max(utiles, key=len)
    if utiles:
        return utiles[0]
    return titulo.strip()


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

    entradas = ficha.get("entries") or []
    if not entradas:
        error("Esa lista no tiene videos, o no es publica.")

    for e in entradas:
        if not e:
            continue   # yt-dlp deja huecos donde habia un video borrado o privado

        # La miniatura: la mas grande de las que trae. Vienen de menor a mayor,
        # asi que la ultima es la buena; en una fila de 104x58 la pequeña se ve
        # pastosa y la grande no cuesta nada mas.
        miniatura = None
        for t in (e.get("thumbnails") or []):
            if t.get("url"):
                miniatura = t["url"]

        decir(
            tipo="elemento",
            id=e.get("id") or "",
            titulo=limpiar(e.get("title") or ""),
            miniatura=miniatura,
            duracion=e.get("duration"),
        )

    decir(tipo="hecho", ficheros=[])


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
