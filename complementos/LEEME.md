# El índice de complementos

`indice.json` es lo que Ondine lee para saber qué se puede instalar. Hoy lo
publica solo este proyecto: **quien escribe en este fichero decide qué se ejecuta
en el equipo de quien instale**, así que no es una lista abierta.

Cada entrada:

```json
{
  "id": "youtube",
  "nombre": "YouTube",
  "descripcion": "Trae vídeos de una lista",
  "version": "1.0.0",
  "autor": "quien lo escriba",
  "paquete": "https://github.com/luishidalgoa/ondine/releases/download/complementos/youtube-1.0.0.zip",
  "sha256": "…64 caracteres hexadecimales…",
  "bytes": 481920,
  "capacidades": ["importar"],
  "ambito": ["organizar"],
  "integracion": "propia"
}
```

- **`sha256` es obligatorio.** Aunque hoy publiquemos solo nosotros. El día que
  esto se abra, lo único que cambiará es quién puede escribir aquí — no el
  formato ni el instalador.
- **`paquete` solo por HTTPS.** Por HTTP, quien esté en medio no cambia solo el
  paquete: cambia el índice, y con él los checksums.
- **Un paquete publicado NO se reemplaza: se publica otro.** La CDN de las
  descargas de GitHub cachea por URL, así que subir un `.zip` con el mismo nombre
  sigue sirviendo el viejo durante un buen rato — y entonces el índice promete un
  `sha256` que lo bajado no cumple. Pasó al montar esto: reemplacé
  `youtube-1.0.0.zip` y la descarga siguió dando los bytes de antes. La app lo
  caza (por eso el checksum es obligatorio), pero quien lo instala solo ve que no
  se instala. **Nombre nuevo por versión nueva**, y el viejo se retira.
- El **`id`** es el nombre de la carpeta donde se instala. Sin separadores ni
  `..`: eso no es un identificador, es una ruta.

Para sacar el checksum de un paquete:

```bash
sha256sum youtube-1.0.0.zip
```

Cómo escribir un complemento: [`docs/complementos.md`](../docs/complementos.md).
