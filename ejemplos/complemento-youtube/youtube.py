"""Complemento de YouTube para Ondine: listar y traer videos accesibles.

Lee los metadatos publicos de una lista -titulo, miniatura, duracion- y los
entrega a Ondine para que los coteje con el catalogo abierto. Tambien descarga
los videos publicos elegidos, hasta 480p, en la carpeta indicada por Ondine.

Se apoya en yt-dlp, que tiene que estar en el PATH. Para listar se usa
`--flat-playlist`, que pide la ficha de la lista y NO toca los videos: es una
peticion, no cuarenta.
"""
import io
import json
import os
import re
import shutil
import subprocess
import sys

# La salida va en UTF-8 pase lo que pase. La consola de Windows arranca en una
# pagina de codigos que se come los acentos, y un titulo con la ñ rota no casa
# con nada del catalogo.
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

# Y la ENTRADA igual, que es por donde llegan las respuestas del modelo. Sin
# esto se decodifica con la pagina de codigos de la consola y un titulo con
# acentos vuelve roto: el mismo fallo que el de la salida, al reves.
sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8", errors="replace")


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


def es_residuo(titulo, descripcion):
    """La descripcion promete mas historias, pero la comprobacion no lo confirma.

    Es EXACTAMENTE el hueco que deja `titulo_completo`, y el unico sitio donde
    un modelo aporta algo: lo que se puede comprobar ya lo resuelven las reglas,
    y preguntar por ello seria gastar dinero en algo que ya esta bien.

    Ojo con lo que NO es residuo: una descripcion mas corta que el titulo, o sin
    descripcion. Ahi no hay nada prometido, y preguntar por eso convierte cada
    video de la lista en una llamada.
    """
    if not descripcion or not descripcion.strip():
        return False

    primera = descripcion.strip().splitlines()[0]
    del_titulo = segmentos(titulo)
    de_la_desc = segmentos(primera)

    if len(de_la_desc) <= len(del_titulo):
        return False

    claves = {_clave(s) for s in de_la_desc}
    return not all(_clave(s) in claves for s in del_titulo)


# Un episodio de estos trae dos o tres historias. Seis es que el modelo ha
# listado la temporada entera, y eso no es una respuesta: es otra pregunta.
_MAX_HISTORIAS = 4

# Se pidio breve y literal. Si contesta con un parrafo, no ha entendido la
# pregunta y lo que traiga dentro no es una lista de titulos.
_MAX_LARGO = 240


def historias_del_modelo(respuesta, titulo):
    """Lo que dijo el modelo, si se sostiene. `None` si no.

    Un modelo acierta casi siempre y se inventa el resto con el mismo aplomo, asi
    que lo que conteste NO se cree a ciegas. La comprobacion que se puede hacer
    aqui es una: **tiene que incluir el titulo del video**, que es lo unico que se
    sabe seguro. Una respuesta que se lo salta esta hablando de otra cosa.

    La comprobacion de verdad viene despues y gratis: lo que salga de aqui lo
    coteja Ondine contra el catalogo, historia por historia. Un titulo inventado
    no casa con nada y sale como que falta, en vez de darse por bueno.
    """
    if not respuesta:
        return None

    texto = respuesta.strip()
    if not texto or len(texto) > _MAX_LARGO:
        return None

    # Las tres formas en que el contrato le pide decir que no lo sabe -las dos
    # del castellano y la del ingles-, sin acentos para que den igual.
    plano = " ".join(texto.split()).casefold().replace("é", "e")
    if plano in ("no lo se", "i do not know", "no lo se."):
        return None

    # Una por linea o separadas por barras: se aceptan las dos porque un modelo
    # hace una u otra segun el dia, y rechazar la que no toque seria tirar una
    # respuesta buena por su formato.
    if "\n" in texto:
        trozos = [t.strip(" -*•\t") for t in texto.splitlines()]
    else:
        trozos = texto.split("|")

    historias = [" ".join(t.split()) for t in trozos if t.strip()]
    if len(historias) < 2 or len(historias) > _MAX_HISTORIAS:
        return None

    # La comprobacion que sostiene todo lo demas.
    corto = _clave(limpiar(titulo))
    if not any(_clave(h) == corto for h in historias):
        return None

    return " + ".join(historias)


def decir(**campos):
    """Una linea, un mensaje. Se vacia en el acto para que Ondine lo pinte segun llega."""
    print(json.dumps(campos, ensure_ascii=False), flush=True)


def error(mensaje):
    decir(tipo="error", mensaje=mensaje)
    sys.exit(1)


_NO_ACCESIBLE = {"private", "premium_only", "subscriber_only", "needs_auth"}


def es_accesible(entrada):
    """Si yt-dlp ha entregado metadatos suficientes para cotejar el video."""
    if not entrada or not (entrada.get("title") or "").strip():
        return False
    return (entrada.get("availability") or "").casefold() not in _NO_ACCESIBLE


def diagnostico_no_disponibles(entradas):
    """Resume los huecos sin afirmar una causa que YouTube no haya revelado."""
    fuera = [e for e in entradas if not es_accesible(e)]
    if not fuera:
        return ""

    causas = {}
    for e in fuera:
        causa = (e.get("availability") or "").casefold()
        etiqueta = {
            "private": "privados",
            "premium_only": "solo Premium",
            "subscriber_only": "solo para miembros",
            "needs_auth": "requieren iniciar sesion",
        }.get(causa, "sin detalle (eliminados, privados o bloqueados)")
        causas[etiqueta] = causas.get(etiqueta, 0) + 1

    detalle = ", ".join(f"{n} {causa}" for causa, n in causas.items())
    ids = [e.get("id") for e in fuera if e.get("id")]
    muestra = f". IDs: {', '.join(ids[:5])}" if ids else ""
    if len(ids) > 5:
        muestra += f" y {len(ids) - 5} mas"
    return f"{len(fuera)} videos no disponibles: {detalle}{muestra}"


def ficha_aprovechable(salida):
    """El JSON valido, incluso si yt-dlp marco como fallida alguna entrada."""
    try:
        if (salida.stdout or "").strip():
            ficha = json.loads(salida.stdout)
            return ficha if isinstance(ficha, dict) else None
    except json.JSONDecodeError:
        pass
    return None


# El contador de las preguntas al modelo. Ondine tiene su propio cupo -40 por
# ejecucion-, pero el complemento lleva el suyo tambien: agotarlo y que las
# ultimas veinte vuelvan con «cupo agotado» es gastar el tiempo de quien mira.
_preguntas = 0
_MAX_PREGUNTAS = 20


def preguntar(texto):
    """Le pregunta al modelo conectado, si Ondine deja. `None` si no.

    Ondine responde por la ENTRADA estandar, una linea, con el mismo id. La
    clave no llega hasta aqui y no tiene por que: se pregunta y se recibe.
    """
    global _preguntas
    if _preguntas >= _MAX_PREGUNTAS:
        return None
    _preguntas += 1

    decir(tipo="preguntar", id=str(_preguntas), texto=texto)

    linea = sys.stdin.readline()
    if not linea:
        return None   # Ondine cerro la tuberia: se sigue sin modelo
    try:
        r = json.loads(linea)
    except json.JSONDecodeError:
        return None
    return r.get("texto")


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

    # yt-dlp puede devolver un error por una entrada bloqueada y, aun asi,
    # entregar un JSON valido con el resto de la lista. Se intenta aprovechar
    # esa respuesta antes de tirar todos los videos accesibles.
    ficha = ficha_aprovechable(salida)

    if ficha is None:
        detalle = (salida.stderr or "").strip().splitlines()
        if salida.returncode != 0:
            error(detalle[-1] if detalle else "yt-dlp no ha podido leer esa lista.")
        error("yt-dlp ha contestado algo que no es JSON.")

    # Sin titulo no hay nada que cotejar. Son los borrados y los privados: yt-dlp
    # los deja en la lista con su id y el resto en blanco. Pasarlos adelante los
    # pinta como un episodio mas llamado «NA», y un hueco disfrazado de episodio
    # es peor que un hueco.
    todas = [e for e in (ficha.get("entries") or []) if e]
    entradas = [e for e in todas if es_accesible(e)]
    if not entradas:
        diagnostico = diagnostico_no_disponibles(todas)
        error(diagnostico or "Esa lista no tiene videos, o no es publica.")

    diagnostico = diagnostico_no_disponibles(todas)
    aviso = f". {diagnostico}" if diagnostico else ""

    # Se dice QUE va a tardar y POR QUE antes de empezar, no despues. Lo que
    # viene es una peticion por episodio y son decenas de segundos sin nada que
    # pintar: una pantalla quieta que no explica el silencio es indistinguible
    # de una colgada, y quien mira acaba cortando algo que iba bien.
    decir(tipo="progreso", avance=0.0,
          texto=f"Lista leida: {len(entradas)} videos accesibles{aviso}")
    decir(tipo="progreso", avance=0.02,
          texto=f"Consultando la ficha de cada video para ver si alguno trae dos "
                f"historias ({len(entradas)} consultas, puede tardar un minuto)")

    # len(TODAS), no len(entradas): yt-dlp consulta tambien los borrados, asi que
    # contra el numero de los buenos el contador se pasaba -«video 13 de 8»-.
    descripciones = _descripciones(exe, fuente, len(todas))

    for i, e in enumerate(entradas):
        # La miniatura: la mas grande de las que trae. Vienen de menor a mayor,
        # asi que la ultima es la buena; en una fila de 104x58 la pequeña se ve
        # pastosa y la grande no cuesta nada mas.
        miniatura = None
        for t in (e.get("thumbnails") or []):
            if t.get("url"):
                miniatura = t["url"]

        titulo = e.get("title") or ""
        descripcion = descripciones.get(e.get("id"))
        final = titulo_completo(titulo, descripcion)

        # El modelo SOLO para el residuo: los casos en que la descripcion promete
        # mas historias y las reglas no lo pueden confirmar. Lo que las reglas
        # resuelven no se pregunta -saldria lo mismo y costaria dinero-, y por eso
        # esto son unas pocas llamadas de una lista de cientos y no una por video.
        if es_residuo(titulo, descripcion):
            dijo = preguntar(
                "De este video de una serie de dibujos, dime los titulos de las "
                "historias que contiene, uno por linea y nada mas.\n"
                f"Titulo del video: {titulo}\n"
                f"Primera linea de su descripcion: "
                f"{descripcion.strip().splitlines()[0]}"
            )
            # Lo que conteste se comprueba; y lo que pase la comprobacion lo vuelve
            # a comprobar Ondine contra el catalogo, historia por historia. Un
            # titulo inventado no casa con nada y sale como que falta.
            final = historias_del_modelo(dijo, titulo) or final

        decir(
            tipo="elemento",
            id=e.get("id") or "",
            titulo=final,
            miniatura=miniatura,
            duracion=e.get("duration"),
        )
        decir(tipo="progreso", avance=(i + 1) / len(entradas),
              texto=f"Cotejando con tu catalogo: {i + 1} de {len(entradas)}")

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
    orden = [exe, "--no-warnings",
             # Sin esto YouTube contesta «This video is not available» a la
             # ficha del video, aunque la lista se lea sin problema.
             "--extractor-args", "youtube:player_client=web_safari",
             # Solo se quieren los metadatos. Sin esto yt-dlp busca ademas un
             # formato que bajar y aborta la ficha entera cuando no lo hay.
             "--ignore-no-formats-error",
             # En Windows yt-dlp escribe con la pagina de codigos de la consola:
             # «confesion» salia con un byte suelto que no es UTF-8 valido y se
             # perdia. El listado se libraba por venir en JSON, que escapa lo que
             # no es ASCII; la descripcion sale en crudo y no.
             "--encoding", "utf-8",
             "--print", _MARCA + "%(id)s@@%(description)s",
             fuente]

    # Se lee SEGUN SALE, no al final. Con una sola llamada que devuelve todo de
    # golpe hay medio minuto en que el complemento no puede decir nada aunque
    # quiera: no es que se olvide de informar, es que no tiene el turno. Leyendo
    # linea a linea, cada video que yt-dlp termina se cuenta en el acto.
    fuera = {}
    try:
        proc = subprocess.Popen(orden, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                                text=True, encoding="utf-8", errors="replace", bufsize=1)
    except OSError:
        return {}

    actual = None
    trozos = []

    def cerrar():
        if actual:
            fuera[actual] = "".join(trozos)

    try:
        for linea in proc.stdout:
            if not linea.startswith(_MARCA):
                trozos.append(linea)
                continue

            cerrar()
            ident, _, resto = linea[len(_MARCA):].partition("@@")
            actual = ident.strip() or None
            trozos = [resto]

            hechos = len(fuera) + (1 if actual else 0)
            decir(tipo="progreso", avance=min(0.95, 0.02 + 0.93 * hechos / max(1, cuantos)),
                  texto=f"Leyendo la ficha del video {hechos} de {cuantos}")
        cerrar()
    except Exception:
        pass
    finally:
        try:
            proc.wait(timeout=30)
        except Exception:
            proc.kill()

    return fuera


_ID_VIDEO = re.compile(r"^[A-Za-z0-9_-]{11}$")
_MARCA_FICHERO = "@@ONDINE_FILE@@"


def _peticion_de_descarga(argumentos):
    """Los ids y el destino del contrato `traer`, o el motivo del rechazo."""
    try:
        separador = argumentos.index("--destino")
    except ValueError:
        return None, None, "No se ha indicado la carpeta de destino."

    ids = argumentos[:separador]
    resto = argumentos[separador + 1:]
    if len(resto) != 1 or not resto[0]:
        return None, None, "La carpeta de destino no es valida."
    if not ids:
        return None, None, "No se ha elegido ningun video."
    if any(not _ID_VIDEO.fullmatch(ident) for ident in ids):
        return None, None, "La seleccion contiene un identificador de video no valido."
    if not os.path.isdir(resto[0]):
        return None, None, "La carpeta de destino no existe."
    return ids, os.path.abspath(resto[0]), None


def traer(argumentos):
    """Descarga los videos publicos elegidos, como maximo a 480p."""
    ids, destino, reparo = _peticion_de_descarga(argumentos)
    if reparo:
        error(reparo)

    exe = shutil.which("yt-dlp") or shutil.which("yt-dlp.exe")
    if not exe:
        error("Hace falta yt-dlp y no esta en el PATH. Instalalo y vuelve a probar.")

    ficheros = []
    fallos = []
    total = len(ids)
    for i, ident in enumerate(ids, 1):
        decir(tipo="progreso", avance=(i - 1) / total,
              texto=f"Descargando video {i} de {total}")
        orden = [
            exe, "--no-playlist", "--no-warnings", "--no-progress",
            # Primero se intenta un formato completo: no necesita ffmpeg. Si no
            # existe, yt-dlp puede juntar video y audio. Nunca se elige mas de
            # 480p: es suficiente para este uso y evita bajar un original enorme.
            "--format", "best[height<=480]/bestvideo[height<=480]+bestaudio/best",
            "--merge-output-format", "mp4",
            "--output", os.path.join(destino, "%(title)s [%(id)s].%(ext)s"),
            "--print", "after_move:" + _MARCA_FICHERO + "%(filepath)s",
            "https://www.youtube.com/watch?v=" + ident,
        ]
        try:
            salida = subprocess.run(
                orden, capture_output=True, text=True, encoding="utf-8",
                errors="replace", timeout=7200,
            )
        except subprocess.TimeoutExpired:
            fallos.append(f"{ident}: ha tardado demasiado")
            continue

        rutas = [linea[len(_MARCA_FICHERO):].strip()
                 for linea in (salida.stdout or "").splitlines()
                 if linea.startswith(_MARCA_FICHERO)]
        if salida.returncode == 0 and rutas:
            ficheros.extend(rutas)
            continue

        detalle = (salida.stderr or "").strip().splitlines()
        fallos.append(f"{ident}: {detalle[-1] if detalle else 'no disponible'}")

    if not ficheros:
        error("No se ha podido descargar ningun video. " + "; ".join(fallos))
    if fallos:
        decir(tipo="progreso", avance=1.0,
              texto=f"Descarga terminada: {len(ficheros)} correctos y "
                    f"{len(fallos)} no disponibles")
    decir(tipo="hecho", ficheros=ficheros)


def main():
    if len(sys.argv) < 2:
        error("No se ha dicho que hacer.")

    orden = sys.argv[1].lower()
    if orden == "listar":
        listar(sys.argv[2] if len(sys.argv) > 2 else "")
    elif orden == "traer":
        traer(sys.argv[2:])
    else:
        error(f"No se que hacer con la orden '{orden}'.")


if __name__ == "__main__":
    main()
