namespace Ondine.Localizacion;

public sealed partial class Textos
{
    // ═══ Aviso del sistema al terminar una tanda ════════════════════════════
    public string AvisoTandaTitulo => Idioma.Elegir(
        "Compression finished",
        "Compresión terminada");

    // {0} = ficheros comprimidos bien, {1} = ficheros que fallaron.
    public string AvisoTandaCuerpo => Idioma.Elegir(
        "{0} compressed, {1} failed.",
        "{0} comprimido(s), {1} con error.");
}
