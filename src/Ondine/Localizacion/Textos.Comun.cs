namespace Ondine.Localizacion;

/// <summary>
/// Textos que se repiten por toda la aplicación: botones de diálogo, estados,
/// unidades. Están aquí y no en cada ventana para que «Cancelar» se traduzca
/// una vez y no doce, y sobre todo para que no acabe traducido de doce formas
/// distintas.
/// </summary>
public sealed partial class Textos
{
    // ── Botones de siempre ──────────────────────────────────────────────────
    public string Aceptar => Idioma.Elegir("OK", "Aceptar");
    public string Cancelar => Idioma.Elegir("Cancel", "Cancelar");
    public string Cerrar => Idioma.Elegir("Close", "Cerrar");
    public string Guardar => Idioma.Elegir("Save", "Guardar");
    public string Aplicar => Idioma.Elegir("Apply", "Aplicar");
    public string Quitar => Idioma.Elegir("Remove", "Quitar");
    public string Elegir => Idioma.Elegir("Choose…", "Elegir…");
    public string Si => Idioma.Elegir("Yes", "Sí");
    public string No => Idioma.Elegir("No", "No");

    // ── Estados ─────────────────────────────────────────────────────────────
    public string Pendiente => Idioma.Elegir("Pending", "Pendiente");
    public string Analizando => Idioma.Elegir("Analysing…", "Analizando…");
    public string Listo => Idioma.Elegir("Done", "Listo");
    public string Error => Idioma.Elegir("Error", "Error");
    public string Omitido => Idioma.Elegir("Skipped", "Omitido");
    public string Pausado => Idioma.Elegir("Paused", "Pausado");

    // ── Idioma ──────────────────────────────────────────────────────────────
    public string IdiomaEtiqueta => Idioma.Elegir("Language", "Idioma");
}
