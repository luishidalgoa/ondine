"""Pruebas del complemento de YouTube.

Se corren con la biblioteca estandar -`python -m unittest`- a proposito: un
complemento de ejemplo que pide instalar pytest para leerlo es un complemento
que nadie lee.
"""
import unittest

from youtube import (es_residuo, historias_del_modelo, limpiar, segmentos,
                     titulo_completo)


class LimpiarTitulo(unittest.TestCase):
    def test_quita_el_canal_y_la_coletilla(self):
        self.assertEqual(
            limpiar("Doraemon | El controlador del mar | Episodio 426 en español"),
            "El controlador del mar")

    def test_un_titulo_sin_barras_se_deja_en_paz(self):
        self.assertEqual(limpiar("El gorro de la suerte"), "El gorro de la suerte")


class SegmentosDelTitulo(unittest.TestCase):
    def test_devuelve_los_trozos_utiles_en_orden(self):
        self.assertEqual(
            segmentos("Doraemon | El controlador del mar | Alquiler estilo futurista | Episodio 426"),
            ["Doraemon", "El controlador del mar", "Alquiler estilo futurista"])


class TituloConLaDescripcion(unittest.TestCase):
    """La descripcion manda SOLO cuando se demuestra que es el titulo y mas.

    El caso real: el video se titula con una historia y en realidad trae dos, y
    la segunda solo aparece en la descripcion. Sin esto Ondine da por bueno un
    fichero al que le falta la mitad.
    """

    def test_la_descripcion_aporta_el_segundo_segmento(self):
        self.assertEqual(
            titulo_completo(
                "Doraemon | El controlador del mar | Episodio 426 en español",
                "Doraemon | El controlador del mar | Alquiler estilo futurista | "
                "Episodio 426 en español - castellano\n\nOriginal Author: FUJIKO F FUJIO"),
            "El controlador del mar + Alquiler estilo futurista")

    def test_las_comillas_de_la_descripcion_no_estorban(self):
        self.assertEqual(
            titulo_completo(
                "Doraemon | Colonia para la memoria | Episodio 425 en español",
                'Doraemon | Colonia para la memoria | Robot "confesión" | Episodio 425'),
            'Colonia para la memoria + Robot "confesión"')

    def test_una_descripcion_que_no_habla_del_titulo_no_se_usa(self):
        """La red de seguridad, y la razon de que esto valga en cualquier canal.

        Cada canal escribe la descripcion a su manera: uno repite el titulo
        entero, otro pone «Suscribete». Por eso no se interpreta la descripcion
        -se comprueba que CONTIENE el titulo-. Donde no lo contiene, no se
        afirma nada y manda el titulo. Lo que falla es callarse, no inventarse
        una historia que no existe.
        """
        self.assertEqual(
            titulo_completo(
                "Doraemon | El controlador del mar | Episodio 426 en español",
                "Suscribete al canal y activa la campanita | Siguenos en redes"),
            "El controlador del mar")

    def test_una_descripcion_que_repite_el_titulo_no_añade_nada(self):
        self.assertEqual(
            titulo_completo(
                "Doraemon | El controlador del mar | Episodio 426 en español",
                "Doraemon | El controlador del mar | Episodio 426 en español"),
            "El controlador del mar")

    def test_el_marcador_NA_de_yt_dlp_no_es_una_historia(self):
        """«NA» es lo que imprime yt-dlp cuando el campo no existe.

        Tomarlo por texto convierte «no hay descripcion» en un titulo llamado
        NA, que se coteja contra el catalogo como cualquier otro.
        """
        self.assertEqual(
            titulo_completo("Doraemon | El controlador del mar | Episodio 426", "NA"),
            "El controlador del mar")

    def test_sin_descripcion_manda_el_titulo(self):
        for vacia in (None, "", "   "):
            self.assertEqual(
                titulo_completo("Doraemon | El controlador del mar | Episodio 426", vacia),
                "El controlador del mar")

    def test_solo_se_mira_la_primera_linea(self):
        """Las lineas de abajo son creditos y avisos legales, no titulos.

        «SHIN-EI Animation» colandose como segunda historia es peor que no
        mirar la descripcion: se compara contra el catalogo y no casa con nada.
        """
        self.assertEqual(
            titulo_completo(
                "Doraemon | El controlador del mar | Episodio 426",
                "Doraemon | El controlador del mar | Episodio 426\n"
                "Produced by SHIN-EI Animation | and TV Asahi"),
            "El controlador del mar")


if __name__ == "__main__":
    unittest.main()


class ElResiduoDelParser(unittest.TestCase):
    """Cuando la descripcion promete mas y la comprobacion no lo confirma.

    Es el unico sitio donde un modelo aporta algo: el parser ya resuelve lo que
    se puede comprobar, y lo que queda es justo lo que no se puede.
    """

    def test_sin_descripcion_no_hay_residuo(self):
        self.assertFalse(es_residuo("Doraemon | El gorro de la suerte", ""))

    def test_si_la_comprobacion_pasa_no_hay_residuo(self):
        # Aqui el parser YA lo resuelve solo: no hay nada que preguntar.
        self.assertFalse(es_residuo(
            "Doraemon | El gorro de la suerte | Episodio 12",
            "Doraemon | El gorro de la suerte | La maquina del tiempo | Episodio 12"))

    def test_si_la_descripcion_promete_mas_pero_no_contiene_al_titulo(self):
        # Tres trozos frente a los dos del titulo -«Doraemon» y el titulo, que
        # la coletilla ya se cae-, pero el titulo NO esta entre ellos: el parser
        # se calla -y hace bien-, y esto es lo que se le pregunta al modelo.
        self.assertTrue(es_residuo(
            "Doraemon | El gorro de la suerte | Episodio 12",
            "Doraemon | Un dia raro | Otro dia raro"))

    def test_una_descripcion_mas_corta_no_promete_nada(self):
        self.assertFalse(es_residuo(
            "Doraemon | El gorro de la suerte | Episodio 12",
            "Suscribete al canal"))


class LoQueDiceElModelo(unittest.TestCase):
    """Lo que conteste NO se cree a ciegas."""

    TITULO = "Doraemon | El gorro de la suerte | Episodio 12"

    def test_lo_normal(self):
        self.assertEqual(
            historias_del_modelo("El gorro de la suerte | La maquina del tiempo", self.TITULO),
            "El gorro de la suerte + La maquina del tiempo")

    def test_acepta_una_por_linea(self):
        self.assertEqual(
            historias_del_modelo("El gorro de la suerte\nLa maquina del tiempo", self.TITULO),
            "El gorro de la suerte + La maquina del tiempo")

    def test_si_dice_que_no_lo_sabe_no_se_usa(self):
        self.assertIsNone(historias_del_modelo("NO LO SE", self.TITULO))
        self.assertIsNone(historias_del_modelo("NO LO SÉ", self.TITULO))
        self.assertIsNone(historias_del_modelo("I DO NOT KNOW", self.TITULO))

    def test_una_respuesta_vacia_no_se_usa(self):
        self.assertIsNone(historias_del_modelo("", self.TITULO))
        self.assertIsNone(historias_del_modelo("   \n  ", self.TITULO))

    def test_si_no_incluye_el_titulo_que_ya_sabemos_no_se_usa(self):
        # Lo unico que se sabe seguro es el titulo del video. Una respuesta que
        # se lo salta esta hablando de otra cosa, y ahi el modelo se lo esta
        # inventando con el mismo aplomo con el que acierta.
        self.assertIsNone(historias_del_modelo(
            "Un dia raro | Otro dia raro", self.TITULO))

    def test_una_parrafada_no_se_usa(self):
        # Se pidio breve y literal. Si contesta explicando, no ha entendido la
        # pregunta y lo que traiga dentro no es una lista de titulos.
        largo = "El gorro de la suerte, " + "bla " * 200
        self.assertIsNone(historias_del_modelo(largo, self.TITULO))

    def test_demasiadas_historias_no_se_usan(self):
        # Un episodio de estos trae dos o tres. Seis es que ha listado la
        # temporada entera.
        seis = " | ".join(["El gorro de la suerte"] + [f"Historia {i}" for i in range(5)])
        self.assertIsNone(historias_del_modelo(seis, self.TITULO))

    def test_si_solo_dice_el_titulo_que_ya_sabemos_no_aporta(self):
        self.assertIsNone(historias_del_modelo("El gorro de la suerte", self.TITULO))
