#!/usr/bin/env bash
#
# Monta el .app y el .dmg de Ondine para macOS.
#
#   ./empaquetado/macos/hacer-dmg.sh [version]
#
# SE EJECUTA EN UN MAC, y no por comodidad: `iconutil` (el icono), `codesign` (la firma) y
# `hdiutil` (la imagen de disco) son herramientas de macOS y no existen en otra parte. La
# publicación de .NET sí se puede hacer desde cualquier sistema — se comprobó publicando
# para osx-arm64 desde Windows —, pero el envoltorio no.
#
# DOS ARQUITECTURAS Y DOS FICHEROS. Un Mac de hoy es arm64 (chip de Apple) y uno de antes de
# 2021 es x64 (Intel). .NET no sabe hacer un binario universal de los dos, así que salen dos
# .dmg con el nombre de su arquitectura. La alternativa —dar solo el de Intel y dejar que
# Rosetta lo traduzca— haría que en los Mac nuevos la app fuera más lenta sin explicación.
#
# LO QUE NO VA DENTRO: ffmpeg (obligatorio para comprimir, cortar y sacar miniaturas) ni
# libvlc (solo para el reproductor de dentro). Los dos se instalan en un Mac con una línea de
# Homebrew, y la app lo dice cuando falta alguno. El motivo de no meterlos está en el
# .csproj: el paquete de libvlc para Mac es de 2018, solo Intel y sin decodificadores.

set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
proyecto="$raiz/src/Ondine.Avalonia/Ondine.Avalonia.csproj"

version="${1:-$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$proyecto" | head -1)}"
[ -n "$version" ] || { echo "no se pudo averiguar la versión"; exit 1; }

salida="$raiz/empaquetado/salida"
mkdir -p "$salida"

# ── El icono, una vez para las dos arquitecturas ─────────────────────────────
# macOS quiere un .icns con todos los tamaños dentro, incluidos los @2x de las pantallas
# Retina. Sin ellos el icono sale borroso en el Dock, que es donde más se mira.
icns="$salida/ondine.icns"
if [ ! -f "$icns" ]; then
  echo "▸ icono…"
  conjunto="$salida/ondine.iconset"
  rm -rf "$conjunto"; mkdir -p "$conjunto"
  for t in 16 32 64 128 256 512; do
    sips -z $t $t "$raiz/docs/icon.png" --out "$conjunto/icon_${t}x${t}.png" >/dev/null
    doble=$((t * 2))
    sips -z $doble $doble "$raiz/docs/icon.png" \
         --out "$conjunto/icon_${t}x${t}@2x.png" >/dev/null
  done
  iconutil -c icns "$conjunto" -o "$icns"
  rm -rf "$conjunto"
fi

for par in "arm64:osx-arm64" "x64:osx-x64"; do
  nombre_arq="${par%%:*}"
  rid="${par##*:}"

  echo "▸ Ondine $version para $nombre_arq"

  app="$salida/Ondine.app"
  rm -rf "$app"
  mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"

  dotnet publish "$proyecto" \
    -c Release -r "$rid" --self-contained true \
    -o "$app/Contents/MacOS" \
    --nologo -v quiet

  chmod +x "$app/Contents/MacOS/Ondine.Avalonia"

  dotnet publish "$raiz/src/Ondine.Cli/Ondine.Cli.csproj" \
    -c Release -r "$rid" --self-contained true \
    -o "$salida/cli-tmp" --nologo -v quiet
  cp "$salida/cli-tmp/ondine" "$app/Contents/MacOS/ondine-cli"
  chmod +x "$app/Contents/MacOS/ondine-cli"
  rm -rf "$salida/cli-tmp"

  # Y EL SERVIDOR MCP, que es lo que deja usar Ondine desde un agente.
  #
  # Va publicado en la MISMA carpeta que la interfaz a proposito: ahi ya esta el runtime
  # entero, asi que lo unico que se anade son sus dos ficheros -unos 200 kB-. Publicarlo
  # aparte y autocontenido costaria 76 MB de runtime duplicado para lo mismo.
  dotnet publish "$raiz/src/Ondine.Mcp/Ondine.Mcp.csproj"     -c Release -r "$rid" --self-contained true     -p:PublishSingleFile=false     -o "$app/Contents/MacOS" --nologo -v quiet
  chmod +x "$app/Contents/MacOS/ondine-mcp"

  cp "$icns" "$app/Contents/Resources/ondine.icns"

  # ── Info.plist ─────────────────────────────────────────────────────────────
  # CFBundleDocumentTypes es el equivalente en macOS del MimeType del .desktop de Linux:
  # es lo que hace que Ondine salga en «Abrir con» al pulsar con el botón derecho sobre un
  # vídeo en el Finder. Los tipos se declaran por su identificador uniforme (UTI), que es
  # como Apple los nombra, no por extensión.
  #
  # LSMinimumSystemVersion 12.0: es lo que .NET 9 pide como mínimo en macOS. Poner menos
  # sería prometer que arranca en un sistema donde no arranca.
  cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>              <string>Ondine</string>
  <key>CFBundleDisplayName</key>       <string>Ondine</string>
  <key>CFBundleIdentifier</key>        <string>com.github.luishidalgoa.ondine</string>
  <key>CFBundleVersion</key>           <string>$version</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleExecutable</key>        <string>Ondine.Avalonia</string>
  <key>CFBundleIconFile</key>          <string>ondine.icns</string>
  <key>CFBundlePackageType</key>       <string>APPL</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>LSMinimumSystemVersion</key>    <string>12.0</string>
  <key>LSApplicationCategoryType</key> <string>public.app-category.video</string>
  <key>NSHighResolutionCapable</key>   <true/>
  <key>CFBundleDocumentTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeName</key><string>Video</string>
      <key>CFBundleTypeRole</key><string>Editor</string>
      <key>LSHandlerRank</key><string>Alternate</string>
      <key>LSItemContentTypes</key>
      <array>
        <string>public.movie</string>
        <string>public.mpeg-4</string>
        <string>public.avi</string>
        <string>com.apple.quicktime-movie</string>
        <string>org.matroska.mkv</string>
        <string>org.webmproject.webm</string>
      </array>
    </dict>
    <dict>
      <key>CFBundleTypeName</key><string>Folder</string>
      <key>CFBundleTypeRole</key><string>Editor</string>
      <key>LSHandlerRank</key><string>Alternate</string>
      <key>LSItemContentTypes</key><array><string>public.folder</string></array>
    </dict>
  </array>
</dict>
</plist>
PLIST

  # ── La firma ───────────────────────────────────────────────────────────────
  # AD HOC («--sign -»), y no es un adorno que se pueda saltar: en los Mac con chip de Apple
  # el sistema NO EJECUTA un binario sin firma, ni siquiera una propia. Sin esto la app se
  # cierra al abrirse y el motivo no sale por ninguna parte.
  #
  # Lo que una firma ad hoc NO hace es contentar a Gatekeeper: al bajar el .dmg de internet
  # macOS lo marca en cuarentena y dirá que Ondine «no se puede abrir porque no se ha podido
  # comprobar el desarrollador». Para que no lo diga hace falta una cuenta de desarrollador
  # de Apple, que es de pago y anual — la misma razón por la que no se compra Avalonia XPF:
  # el proyecto no cobra nada. Se explica cómo abrirla igual en las notas de la Release.
  codesign --force --deep --sign - "$app" >/dev/null 2>&1 \
    || echo "  aviso: no se pudo firmar (en un Mac con chip de Apple la app no arrancará)"

  # ── La imagen de disco ─────────────────────────────────────────────────────
  # Con el enlace a Aplicaciones dentro: es el gesto que todo el mundo conoce en un Mac
  # —arrastrar el icono a la carpeta— y sin él no se sabe qué hacer con la ventana que abre.
  monte="$salida/dmg-tmp"
  rm -rf "$monte"; mkdir -p "$monte"
  cp -R "$app" "$monte/Ondine.app"
  ln -s /Applications "$monte/Aplicaciones"

  # EL NOMBRE DICE «macos», y no es adorno. Se publico la v1.14.0 con estos llamados
  # «Ondine-1.14.0-x64.dmg», y en la pagina de versiones hay diez ficheros juntos: alguien
  # con un Linux de 64 bits vio «x64», se lo bajo, y su escritorio lo detecto como un
  # comprimido cualquiera. La arquitectura sola es peor que nada, porque coincide entre
  # sistemas y da una pista falsa que se lee con confianza.
  dmg="$salida/Ondine-${version}-macos-${nombre_arq}.dmg"
  rm -f "$dmg"
  hdiutil create -quiet -volname "Ondine $version" -srcfolder "$monte" \
                 -ov -format UDZO "$dmg"
  rm -rf "$monte" "$app"

  echo "▸ $dmg"
done

echo
echo "  Instalar:  abrir el .dmg y arrastrar Ondine a Aplicaciones"
echo "  La primera vez: botón derecho sobre Ondine → Abrir (sin firma de Apple, el doble"
echo "  clic no basta), o quitar la cuarentena con:"
echo "      xattr -dr com.apple.quarantine /Applications/Ondine.app"
echo "  Requiere:  brew install ffmpeg   ·   brew install --cask vlc (reproductor de dentro)"
