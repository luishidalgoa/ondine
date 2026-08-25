# Los paquetes de Ondine

Qué se publica en cada sistema, para quién es, qué necesita y cómo se monta. Todo lo que hay
aquí lo construye [`.github/workflows/build.yml`](../.github/workflows/build.yml) al empujar un
tag `vX.Y.Z` o a mano, nunca en cada empujón: los minutos de Actions no son gratis y los de
macOS se facturan **a diez veces**.

## Los cinco paquetes

| Paquete | Sistema | Para quién |
|---|---|---|
| `Ondine-Setup-X.Y.Z.exe` | Windows | La app de escritorio de siempre, con auto-actualización |
| `ondine_X.Y.Z_amd64.deb` | Linux Mint, Ubuntu, Debian | Quien va a usar Ondine a menudo: se integra en el menú y en «Abrir con» |
| `Ondine-X.Y.Z-linux-x86_64.AppImage` | Cualquier Linux | Probarlo sin instalar nada, o distribuciones que no son Debian |
| `Ondine-X.Y.Z-macos-arm64.dmg` | macOS con chip de Apple | — |
| `Ondine-X.Y.Z-macos-x64.dmg` | macOS Intel | — |
| `ondine-<plataforma>` | los cinco | La herramienta de terminal, que comparte el mismo motor |

## El nombre dice el sistema

Los tres llevan **el sistema en el nombre**, y no es adorno. La v1.14.0 se publicó con
`Ondine-1.14.0-x64.dmg` y `Ondine-1.14.0-x86_64.AppImage`, y en la página de versiones hay diez
ficheros juntos: alguien con un Linux de 64 bits vio «x64», se lo bajó, y su Mint lo detectó
como **un comprimido cualquiera** — un `.dmg` es una imagen de disco de macOS.

La extensión ya lo decía, pero solo a quien se sepa las extensiones. **La arquitectura sola es
peor que no poner nada**, porque coincide entre sistemas y da una pista falsa que se lee con
confianza.

El `.deb` es la excepción: su nombre lo fija la política de Debian
(`nombre_version_arquitectura.deb`) y ahí «amd64» no se puede tocar. A cambio, es el único de
los tres que el escritorio abre e instala él solo.

## Lo que NO va dentro, y por qué

Las tres interfaces son **autocontenidas** en lo que respecta a .NET: no hay que instalar un
*runtime*. Lo que no llevan dentro son dos programas externos, y la decisión es distinta en cada
sistema:

| | Windows | Linux | macOS |
|---|---|---|---|
| **ffmpeg** (obligatorio) | lo descarga el instalador | `apt install ffmpeg` | `brew install ffmpeg` |
| **libvlc** (solo el reproductor de dentro) | va dentro del paquete | `apt install vlc` | `brew install --cask vlc` |

**En Windows van dentro** porque el sistema no trae ninguno de los dos y el instalador puede
resolverlo sin preguntar.

**En Linux no**, y no es por ahorrar tamaño: la distribución ya los tiene o los instala en una
línea, la aceleración por hardware (VAAPI) sale de los complementos del sistema, y una copia
empaquetada envejece sin recibir los parches de seguridad de la distro.

**En macOS tampoco, y aquí hubo que comprobarlo.** Existe un paquete de VideoLAN para Mac
—`VideoLAN.LibVLC.Mac`— y no sirve: trae un solo fichero de 42 MB con fecha de **julio de
2018**, compilado **solo para Intel** y **sin `libvlccore` ni la carpeta de decodificadores**.
Con él, en un Mac con chip de Apple la biblioteca no carga, y en un Intel abre y no reproduce
nada. Se vio mirando el paquete, no leyendo su descripción: publicar para `osx-arm64` deja el
dylib de Intel en la carpeta sin que nada se queje. Así que macOS usa la libvlc de **VLC.app**,
que VideoLAN publica universal y al día.

Cuando falta, la app **no falla en seco**: dice el nombre del paquete y la orden para ese
sistema. Que el aviso hable del sistema en el que estás lo vigila `RutaDeLibVlcTests` — decía
`sudo apt install vlc` en todos, y en un Mac esa orden no existe.

## Dos cosas que solo fallan en la app empaquetada

Las dos son de macOS y las dos se descubrieron pensando en el paquete, no ejecutando la app.

**1 · La app abierta desde el Finder no hereda tu PATH.** Recibe uno mínimo
(`/usr/bin:/bin:/usr/sbin:/sbin`), y Homebrew instala en `/opt/homebrew/bin` (chip de Apple) o
`/usr/local/bin` (Intel). Ninguno de los dos está ahí. O sea: `brew install ffmpeg`, funciona en
el terminal, y Ondine abierta con doble clic dice que no está instalado. Se arregla mirando esas
carpetas a mano (`Engine.HerramientaEnMac`, con el orden de la arquitectura nativa primero para
no acabar usando el ffmpeg de Rosetta sin saberlo).

**2 · Un binario sin firmar no arranca en los Mac con chip de Apple.** No es un aviso: el
sistema no lo ejecuta, la app se cierra al abrirse y no queda ningún mensaje que lo explique.
Por eso el guion firma **ad hoc** (`codesign --sign -`), que es una firma propia sin
certificado.

## Gatekeeper: la primera vez hay que abrirla a mano

Una firma ad hoc no contenta a Gatekeeper. Al bajar el `.dmg` de internet, macOS lo marca en
cuarentena y dirá que Ondine *no se puede abrir porque no se ha podido comprobar el
desarrollador*. Dos formas de abrirla igual:

- **botón derecho sobre Ondine → Abrir** (el doble clic no basta), o
- `xattr -dr com.apple.quarantine /Applications/Ondine.app`

Para que no lo diga hace falta una cuenta de desarrollador de Apple, que es **de pago y anual**.
Es la misma razón por la que no se compra Avalonia XPF: el proyecto no cobra nada. Si algún día
cobra, esto y la clave de TMDb se revisan juntos.

## El AppImage no promete lo que parece prometer

Un AppImage se descarga, se marca ejecutable y se abre. Lo que **no** hace es traer ffmpeg y
libvlc dentro, así que sigue necesitando los dos programas del sistema. Meterlos se estudió:
son unos 120 MB más, se quedarían sin los parches de la distribución y la aceleración por
hardware saldría peor que con el ffmpeg que ya tienes. Un AppImage de 400 MB que decodifica peor
no es un favor.

## Montarlos a mano

```bash
./empaquetado/linux/hacer-deb.sh          # en Linux
./empaquetado/linux/hacer-appimage.sh     # en Linux (se baja appimagetool si falta)
./empaquetado/macos/hacer-dmg.sh          # en un Mac, obligatoriamente
```

Los tres sacan la versión del `.csproj`, que es la que el flujo `verificar-version` obliga a
mantener igual en los cuatro proyectos. Ninguno la lleva escrita a mano, y eso lo vigila una
prueba: un guion con la versión pegada publicaría `Ondine-1.12.0.dmg` el día que la app ya va por
la 1.14, y el paquete parecería viejo sin serlo.

El `.dmg` necesita un Mac de verdad: `iconutil`, `codesign` y `hdiutil` son de macOS. La
publicación de .NET sí se puede hacer desde cualquier sistema —está comprobado publicando para
`osx-arm64` desde Windows—, pero el envoltorio no.

## Qué está verificado y qué no

Conviene que esto esté escrito, porque la diferencia importa.

**Verificado por máquina:** que el `.deb` está bien formado y lleva dentro el lanzador, el icono
y los dos binarios (con `lintian` y `dpkg-deb`); que el AppImage sale ejecutable y su lanzador no
apunta a `/opt`; que los dos `.dmg` llevan `Ondine.app` dentro, con su firma y con el ejecutable
que el `Info.plist` declara; y que ni el `.deb` ni el AppImage se han llevado los 283 MB de
libVLC de Windows dentro, que ya pasó una vez.

**Verificado leyendo el proyecto:** que el `MimeType` del `.desktop` cubre las mismas extensiones
que el motor sabe abrir, que el `Info.plist` declara el binario que de verdad se publica, y que
los tres guiones sacan la versión del `.csproj`. Son las cosas que, si están mal, dan un paquete
**perfecto que al abrirse no hace nada**.

**Sin verificar, y hace falta una persona:** que la app **arranque** en un Mint de verdad y en un
Mac de verdad. Ningún runner tiene escritorio, así que ninguna comprobación de estas la abre. Es
lo único que queda de la fase de empaquetado.

---
*Escrito al cerrar la fase 5 del puerto a Avalonia. El estudio completo del puerto está en
[`avalonia.md`](avalonia.md).*
