#!/usr/bin/env bash
#
# Monta el .deb de Ondine para Linux Mint (y cualquier cosa basada en Debian o Ubuntu).
#
# Se ejecuta EN LINUX. Publica la interfaz de Avalonia autocontenida, la coloca en /opt y
# deja el lanzador, el icono y las asociaciones de tipo donde el escritorio los busca.
#
#   ./empaquetado/linux/hacer-deb.sh [version]
#
# Sin argumento coge la versión del csproj, que es la que el flujo `verificar-version`
# obliga a mantener igual en los cuatro sitios.

set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
proyecto="$raiz/src/Ondine.Avalonia/Ondine.Avalonia.csproj"

# sed y no grep -P: el -P de GNU grep no está en todas partes y falla según el locale,
# y esto tiene que correr igual en el CI que en el portátil de cualquiera.
version="${1:-$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$proyecto" | head -1)}"
[ -n "$version" ] || { echo "no se pudo averiguar la versión"; exit 1; }

arquitectura="amd64"
rid="linux-x64"
nombre="ondine_${version}_${arquitectura}"
salida="$raiz/empaquetado/salida"
arbol="$salida/$nombre"

echo "▸ Ondine $version para $arquitectura"
rm -rf "$arbol"
mkdir -p "$arbol/DEBIAN" "$arbol/opt/ondine" "$arbol/usr/bin" \
         "$arbol/usr/share/applications" \
         "$arbol/usr/share/icons/hicolor/scalable/apps" \
         "$arbol/usr/share/icons/hicolor/256x256/apps" \
         "$arbol/usr/share/pixmaps"

# ── La app ───────────────────────────────────────────────────────────────────
# AUTOCONTENIDA a propósito. Linux Mint no trae .NET 9 en sus repositorios, así que un
# paquete que dependiera del runtime obligaría a añadir el repositorio de Microsoft antes
# de poder instalarlo. Pesa más y se instala sin preguntar nada, que es lo que se quiere de
# algo que se baja una vez.
echo "▸ publicando…"
dotnet publish "$proyecto" \
  -c Release -r "$rid" --self-contained true \
  -p:PublishSingleFile=false \
  -o "$arbol/opt/ondine" \
  --nologo -v quiet

chmod +x "$arbol/opt/ondine/Ondine.Avalonia"

# El de la línea de órdenes va dentro también: es el mismo motor y no ocupa casi nada al
# lado de la interfaz. Quien tenga que automatizar algo lo tiene sin instalar otra cosa.
dotnet publish "$raiz/src/Ondine.Cli/Ondine.Cli.csproj" \
  -c Release -r "$rid" --self-contained true \
  -o "$salida/cli-tmp" --nologo -v quiet
cp "$salida/cli-tmp/ondine" "$arbol/opt/ondine/ondine-cli"
chmod +x "$arbol/opt/ondine/ondine-cli"
rm -rf "$salida/cli-tmp"

# ── Lo que ve el escritorio ──────────────────────────────────────────────────
ln -sf /opt/ondine/Ondine.Avalonia "$arbol/usr/bin/ondine"
ln -sf /opt/ondine/ondine-cli      "$arbol/usr/bin/ondine-cli"

cp "$raiz/empaquetado/linux/ondine.desktop" "$arbol/usr/share/applications/ondine.desktop"
cp "$raiz/docs/marca/ondine-marca-oscuro.svg" \
   "$arbol/usr/share/icons/hicolor/scalable/apps/ondine.svg"
cp "$raiz/docs/icon.png" \
   "$arbol/usr/share/icons/hicolor/256x256/apps/ondine.png"

# Y EL MISMO ICONO EN /usr/share/pixmaps, que parece redundante y no lo es.
#
# En Linux Mint el lanzador salio en el menu SIN ICONO: un engranaje generico. El icono
# estaba instalado y el .desktop lo pedia bien; lo que falla en medio es el tema de iconos,
# que son varias piezas -el indice del tema, la cache de GTK, el cargador de SVG- y basta
# con que una no este para que la busqueda por nombre no devuelva nada. Ninguna avisa: se
# cae al icono generico y ya.
#
# /usr/share/pixmaps es la ruta de toda la vida, anterior a los temas: se mira por nombre,
# sin indice y sin cache. Es la que queda cuando lo otro falla, y ocupa tres kilobytes.
cp "$raiz/docs/icon.png" "$arbol/usr/share/pixmaps/ondine.png"

# ── El control ───────────────────────────────────────────────────────────────
# ffmpeg es OBLIGATORIO: sin él la app no comprime, no saca miniaturas y no corta. En
# Windows el instalador lo descarga; en Mint está en los repositorios y lo pone apt.
#
# vlc es RECOMENDADO y no obligatorio: solo hace falta para el reproductor integrado, y
# quien solo venga a renombrar su biblioteca no tiene por qué arrastrar VLC entero. Si
# falta, el reproductor lo dice con el nombre del paquete en vez de fallar en seco.
#
# hicolor-icon-theme es lo que crea el árbol de carpetas donde va el icono y lo que trae el
# disparador que refresca el tema al instalar. En un escritorio completo ya está, pero
# declararlo cuesta nada y sin él el icono se instalaría en un sitio que nadie mira.
tamano=$(du -sk "$arbol" | cut -f1)
cat > "$arbol/DEBIAN/control" <<CONTROL
Package: ondine
Version: $version
Section: video
Priority: optional
Architecture: $arquitectura
Depends: ffmpeg, hicolor-icon-theme
Recommends: vlc
Installed-Size: $tamano
Maintainer: luishidalgoa <https://github.com/luishidalgoa/ondine>
Homepage: https://github.com/luishidalgoa/ondine
Description: Prepara tu biblioteca de series y películas para Plex y Jellyfin
 Ondine pone nombre y orden a una biblioteca de vídeo a partir de un catálogo
 de referencia, la comprime por lotes y parte capítulos en sus historias.
 .
 Nada se toca sin aprobación: analizar solo propone, se aplica lo que marcas y
 hay deshacer. Lo borrado va a la papelera del escritorio, no se elimina.
CONTROL

# Al instalar y al desinstalar hay que refrescar las cachés del escritorio, o el lanzador
# no sale en el menú y el icono queda en blanco hasta reiniciar la sesión.
cat > "$arbol/DEBIAN/postinst" <<'POSTINST'
#!/bin/sh
set -e
if [ "$1" = "configure" ]; then
    update-desktop-database -q /usr/share/applications 2>/dev/null || true
    gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor 2>/dev/null || true
fi
POSTINST

cat > "$arbol/DEBIAN/postrm" <<'POSTRM'
#!/bin/sh
set -e
if [ "$1" = "remove" ] || [ "$1" = "purge" ]; then
    update-desktop-database -q /usr/share/applications 2>/dev/null || true
    gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor 2>/dev/null || true
fi
POSTRM

chmod 755 "$arbol/DEBIAN/postinst" "$arbol/DEBIAN/postrm"

# ── A empaquetar ─────────────────────────────────────────────────────────────
dpkg-deb --build --root-owner-group "$arbol" "$salida/$nombre.deb" >/dev/null
rm -rf "$arbol"

echo "▸ $salida/$nombre.deb"
echo
echo "  Instalar:     sudo apt install $salida/$nombre.deb"
echo "  Desinstalar:  sudo apt remove ondine"
