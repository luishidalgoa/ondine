#!/usr/bin/env bash
#
# Monta el AppImage de Ondine: un solo fichero que se descarga, se marca como ejecutable y
# se abre, sin instalar nada y sin permisos de administrador.
#
#   ./empaquetado/linux/hacer-appimage.sh [version]
#
# ¿POR QUÉ ADEMÁS DEL .deb? Porque son para dos situaciones distintas y ninguna cubre la
# otra. El .deb se integra: sale en el menú, en «Abrir con» y lo actualiza el gestor de
# paquetes — es lo que quiere quien usa Mint y va a usar Ondine a menudo. El AppImage no se
# integra en nada, y eso es su ventaja: sirve para probarlo sin ensuciar el sistema, para
# distribuciones que no son Debian (Fedora, openSUSE, Arch) y para llevarlo en un USB.
#
# LO QUE EL APPIMAGE NO PUEDE PROMETER, y conviene decirlo aquí y no en la página de
# descargas: dentro va Ondine entera, con .NET incluido, pero NO van ffmpeg ni libvlc.
#
#   · ffmpeg es obligatorio -sin él no comprime, no saca miniaturas y no corta-.
#   · libvlc solo hace falta para el reproductor de dentro, y si falta se dice con la orden
#     para instalarlo en vez de fallar en seco.
#
# Meterlos dentro se estudió y sale mal: son ~120 MB más, se quedarían sin las
# actualizaciones de seguridad de la distribución y la aceleración por hardware (VAAPI) sale
# de los complementos del sistema, no de una copia empaquetada. Un AppImage de 400 MB que
# decodifica peor que el ffmpeg que ya tienes no es un favor.

set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
proyecto="$raiz/src/Ondine.Avalonia/Ondine.Avalonia.csproj"

version="${1:-$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$proyecto" | head -1)}"
[ -n "$version" ] || { echo "no se pudo averiguar la versión"; exit 1; }

rid="linux-x64"
arquitectura="x86_64"
salida="$raiz/empaquetado/salida"
appdir="$salida/Ondine.AppDir"

echo "▸ Ondine $version — AppImage para $arquitectura"
rm -rf "$appdir"
mkdir -p "$appdir/usr/bin" \
         "$appdir/usr/share/applications" \
         "$appdir/usr/share/icons/hicolor/256x256/apps" \
         "$appdir/usr/share/icons/hicolor/scalable/apps"

echo "▸ publicando…"
dotnet publish "$proyecto" \
  -c Release -r "$rid" --self-contained true \
  -o "$appdir/usr/bin" \
  --nologo -v quiet

chmod +x "$appdir/usr/bin/Ondine.Avalonia"

# El de la línea de órdenes va dentro también, como en el .deb: es el mismo motor y no
# ocupa casi nada al lado de la interfaz.
dotnet publish "$raiz/src/Ondine.Cli/Ondine.Cli.csproj" \
  -c Release -r "$rid" --self-contained true \
  -o "$salida/cli-tmp" --nologo -v quiet
cp "$salida/cli-tmp/ondine" "$appdir/usr/bin/ondine-cli"
chmod +x "$appdir/usr/bin/ondine-cli"
rm -rf "$salida/cli-tmp"

# Y EL SERVIDOR MCP, que es lo que deja usar Ondine desde un agente.
#
# Va publicado en la MISMA carpeta que la interfaz a proposito: ahi ya esta el runtime
# entero, asi que lo unico que se anade son sus dos ficheros -unos 200 kB-. Publicarlo
# aparte y autocontenido costaria 76 MB de runtime duplicado para lo mismo.
dotnet publish "$raiz/src/Ondine.Mcp/Ondine.Mcp.csproj"   -c Release -r "$rid" --self-contained true   -p:PublishSingleFile=false   -o "$appdir/usr/bin" --nologo -v quiet
chmod +x "$appdir/usr/bin/ondine-mcp"

# ── AppRun ───────────────────────────────────────────────────────────────────
# Es lo que se ejecuta al abrir el AppImage. Hace dos cosas y las dos hacen falta:
#
#   · Se coloca en la carpeta de la app antes de arrancar. Sin esto, el directorio de
#     trabajo es el de quien lo lanzó y las rutas relativas -los recursos de Avalonia, los
#     temas- se buscan donde no están.
#   · Pasa los argumentos tal cual («$@»), que es lo que permite soltar un vídeo encima del
#     AppImage o abrirlo con «ondine.AppImage peli.mkv».
cat > "$appdir/AppRun" <<'APPRUN'
#!/bin/sh
AQUI="$(dirname "$(readlink -f "$0")")"
cd "$AQUI/usr/bin" || exit 1
exec "$AQUI/usr/bin/Ondine.Avalonia" "$@"
APPRUN
chmod +x "$appdir/AppRun"

# ── El lanzador ──────────────────────────────────────────────────────────────
# EL MISMO FICHERO que usa el .deb, con una línea cambiada: el Exec. En el paquete es la
# ruta absoluta de /opt, y aquí eso apuntaría a algo que no existe — un AppImage no se
# instala en ninguna parte. La especificación de AppImage pide una orden a secas, y quien la
# resuelve es el propio AppRun.
#
# Se copia y se reescribe en vez de mantener dos ficheros: el resto -los tipos que abre, las
# categorías, las traducciones- es idéntico, y dos copias significan que la próxima
# extensión de vídeo se añade en una y se olvida en la otra.
sed 's|^Exec=.*|Exec=ondine %F|' "$raiz/empaquetado/linux/ondine.desktop" \
  > "$appdir/ondine.desktop"
cp "$appdir/ondine.desktop" "$appdir/usr/share/applications/ondine.desktop"

# El icono va DOS veces, y no es descuido: appimagetool exige uno en la raíz del AppDir
# —es el que se ve en el gestor de archivos— y los escritorios que integran AppImages
# (con AppImageLauncher) buscan el del árbol de iconos.
cp "$raiz/docs/icon.png" "$appdir/ondine.png"
cp "$raiz/docs/icon.png" "$appdir/usr/share/icons/hicolor/256x256/apps/ondine.png"
cp "$raiz/docs/marca/ondine-marca-oscuro.svg" \
   "$appdir/usr/share/icons/hicolor/scalable/apps/ondine.svg"

# ── A empaquetar ─────────────────────────────────────────────────────────────
# appimagetool se baja si no está. Se ejecuta con --appimage-extract-and-run porque él
# mismo es un AppImage y montarse necesita FUSE, que en un contenedor de CI no hay; así se
# descomprime y corre sin pedirlo.
herramienta="$salida/appimagetool"
if [ ! -x "$herramienta" ]; then
  echo "▸ bajando appimagetool…"
  curl -fsSL -o "$herramienta" \
    "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-${arquitectura}.AppImage"
  chmod +x "$herramienta"
fi

# Con «linux» en el nombre, por lo mismo que el .dmg lleva «macos»: la arquitectura sola
# coincide entre sistemas y no distingue nada. (El .deb no puede: su nombre lo fija la
# politica de Debian, y a cambio es el unico que el escritorio instala el solo.)
destino="$salida/Ondine-${version}-linux-${arquitectura}.AppImage"
rm -f "$destino"

# ARCH se le pasa por el entorno: appimagetool no lo deduce del contenido y falla sin ella.
ARCH="$arquitectura" "$herramienta" --appimage-extract-and-run \
  "$appdir" "$destino" >/dev/null

rm -rf "$appdir"
chmod +x "$destino"

echo "▸ $destino"
echo
echo "  Ejecutar:  chmod +x $(basename "$destino") && ./$(basename "$destino")"
echo "  Requiere:  ffmpeg (obligatorio) · vlc (solo para el reproductor de dentro)"
