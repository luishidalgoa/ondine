#!/bin/sh
# Ondine installer for Linux and macOS.
#
#   curl -fsSL https://ondine.hdglabs.com/install.sh | sh
#
# You are piping a script from the internet into a shell, so read it first.
# That is what this comment is for, and why the script is short enough to read.
#
# What it does, in order:
#   1. Works out your OS and CPU.
#   2. Asks GitHub for the latest release.
#   3. Downloads the tarball for your platform and checks it is not empty.
#   4. Unpacks it into ~/.local/bin, which is a user directory: no sudo, and
#      nothing outside your home is touched.
#   5. Tells you if ffmpeg is missing, because Ondine cannot work without it.
#
# It does not add anything to your shell profile. If ~/.local/bin is not on
# your PATH, it prints the line to add and lets you decide.

set -eu

REPO="luishidalgoa/ondine"
DESTINO="${ONDINE_BIN_DIR:-$HOME/.local/bin}"

rojo()  { printf '\033[31m%s\033[0m\n' "$1" >&2; }
gris()  { printf '\033[2m%s\033[0m\n' "$1"; }
verde() { printf '\033[32m%s\033[0m\n' "$1"; }

morir() { rojo "$1"; exit 1; }

# ── 1 · plataforma ───────────────────────────────────────────────────────────
case "$(uname -s)" in
  Linux)  SO="linux" ;;
  Darwin) SO="macos" ;;
  *) morir "Ondine's terminal tool runs on Linux and macOS. For Windows, download it from https://github.com/$REPO/releases/latest" ;;
esac

case "$(uname -m)" in
  x86_64|amd64)  ARQ="x64" ;;
  arm64|aarch64) ARQ="arm64" ;;
  *) morir "Unsupported CPU: $(uname -m). Only x64 and arm64 are published." ;;
esac

PAQUETE="ondine-$SO-$ARQ.tar.gz"

# ── 2 · herramientas ─────────────────────────────────────────────────────────
if command -v curl >/dev/null 2>&1; then
  BAJAR="curl -fsSL"
elif command -v wget >/dev/null 2>&1; then
  BAJAR="wget -qO-"
else
  morir "Neither curl nor wget is available."
fi

# ── 3 · la versión ───────────────────────────────────────────────────────────
# Se pregunta a la API en vez de asumir un número: así el script no caduca.
gris "Looking up the latest release…"
VERSION=$(
  $BAJAR "https://api.github.com/repos/$REPO/releases/latest" \
    | sed -n 's/.*"tag_name": *"\([^"]*\)".*/\1/p' \
    | head -1
)
[ -n "$VERSION" ] || morir "Could not read the latest version from GitHub. Try again, or download it by hand from https://github.com/$REPO/releases/latest"

URL="https://github.com/$REPO/releases/download/$VERSION/$PAQUETE"
gris "Ondine $VERSION for $SO-$ARQ"

# ── 4 · descarga y desempaquetado ────────────────────────────────────────────
# En un temporal propio que se borra pase lo que pase, incluso si el script
# muere a medias: un fallo no deja basura ni un binario a medio escribir.
TMP=$(mktemp -d 2>/dev/null || mktemp -d -t ondine)
trap 'rm -rf "$TMP"' EXIT INT TERM

gris "Downloading…"
if [ "$BAJAR" = "curl -fsSL" ]; then
  curl -fsSL "$URL" -o "$TMP/$PAQUETE" || morir "Download failed: $URL"
else
  wget -q "$URL" -O "$TMP/$PAQUETE" || morir "Download failed: $URL"
fi

# Un fichero de cuatro bytes es una página de error, no un programa.
[ -s "$TMP/$PAQUETE" ] || morir "The downloaded file is empty: $URL"

tar xzf "$TMP/$PAQUETE" -C "$TMP" || morir "Could not unpack $PAQUETE"
[ -f "$TMP/ondine" ] || morir "The package does not contain the expected 'ondine' binary."

mkdir -p "$DESTINO"
# Se mueve ya terminado, no se escribe en el sitio: si algo falla arriba, lo que
# había instalado sigue funcionando.
mv "$TMP/ondine" "$DESTINO/ondine"
chmod +x "$DESTINO/ondine"

verde "Ondine $VERSION installed in $DESTINO/ondine"

# ── 5 · lo que hace falta después ────────────────────────────────────────────
case ":$PATH:" in
  *":$DESTINO:"*) ;;
  *)
    echo
    rojo "$DESTINO is not on your PATH."
    echo "Add this line to your shell profile (~/.bashrc, ~/.zshrc, ~/.profile):"
    echo
    echo "    export PATH=\"\$PATH:$DESTINO\""
    ;;
esac

if ! command -v ffmpeg >/dev/null 2>&1; then
  echo
  rojo "ffmpeg was not found, and Ondine cannot work without it."
  echo "    Debian/Ubuntu:  sudo apt install ffmpeg"
  echo "    Fedora:         sudo dnf install ffmpeg"
  echo "    Arch:           sudo pacman -S ffmpeg"
  echo "    macOS:          brew install ffmpeg"
fi

echo
gris "Get started with:  ondine --help"
