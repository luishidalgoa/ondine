# Fotogramas de los episodios

Aquí van los fotogramas que se ven en el mural del hero y en la comparativa
final. **El nombre del fichero se saca del código del episodio, en minúscula.**
Si un fichero no está, la ficha no se rompe: se degrada al rectángulo de color
con su geometría, que es como estaba antes de haber fotogramas.

Formato: **JPG, apaisado 16:9**, 640×360 sobra. Por encima de eso solo pesan
más sin verse mejor, porque en la página la ficha más grande no llega a 400 px
de ancho.

| Fichero | Episodio | Título |
|---|---|---|
| `s01e01.jpg` | S01E01 | Sin blanca Navidad |
| `s02e01.jpg` | S02E01 | Bart suspende |
| `s02e02.jpg` | S02E02 | Simpson y Dalila |
| `s04e03.jpg` | S04E03 | Homer, el hereje |
| `s04e12.jpg` | S04E12 | Marge contra el monorraíl |
| `s05e02.jpg` | S05E02 | Cabo Miedo |
| `s05e17.jpg` | S05E17 | Bart consigue un elefante |
| `s06e25.jpg` | S06E25 | ¿Quién disparó al señor Burns? |
| `s07e21.jpg` | S07E21 | 22 historias cortas sobre Springfield |
| `s07e24.jpg` | S07E24 | Homerpalooza |
| `s08e23.jpg` | S08E23 | El limonero de Troya |
| `s09e01.jpg` | S09E01 | La ciudad de Nueva York contra Homer |

Los ocho primeros son además los que salen en la comparativa del final, así
que si vas a poner solo unos cuantos, empieza por esos.

Para sacarlos de tus propios ficheros, con la misma herramienta que ya tienes
instalada para el resto:

```
ffmpeg -ss 00:04:30 -i "el fichero.mkv" -frames:v 1 -vf scale=640:-2 -q:v 4 s01e01.jpg
```

Cambia el `-ss` hasta dar con un plano que se entienda solo. Los planos con
un personaje reconocible de cerca funcionan mejor que los generales: en la
ficha se ven a 300 px de ancho.

---

**Una advertencia que conviene tener escrita aquí y no solo en una
conversación:** estos fotogramas tienen dueño, y esta página es pública e
indexable. Es una decisión tomada a sabiendas, no un descuido. Si algún día
llega una reclamación, borrar esta carpeta basta: la página vuelve sola a los
rectángulos de color y no se rompe nada.
