using System.Windows;

namespace Ondine;

/// <summary>
/// Propiedades adjuntas para el orden por cabecera de las tablas basadas en <c>GridView</c>
/// (la de «Comprimir»). <c>Field</c> dice por qué propiedad ordena cada columna —así TAMAÑO
/// ordena por <c>Bytes</c> y no por su texto «1,2 GB», y DURACIÓN por segundos y no por
/// «1:03:22»—; <c>Glyph</c> es la flecha ▲/▼ que la cabecera activa pinta a su derecha.
/// </summary>
public static class HeaderSort
{
    /// <summary>Propiedad del modelo por la que ordena la columna (se pone en cada GridViewColumn).</summary>
    public static readonly DependencyProperty FieldProperty =
        DependencyProperty.RegisterAttached(
            "Field", typeof(string), typeof(HeaderSort), new PropertyMetadata(null));
    public static void SetField(DependencyObject o, string v) => o.SetValue(FieldProperty, v);
    public static string? GetField(DependencyObject o) => (string?)o.GetValue(FieldProperty);

    /// <summary>Flecha del orden actual («▲», «▼» o «») que muestra la cabecera.</summary>
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.RegisterAttached(
            "Glyph", typeof(string), typeof(HeaderSort), new PropertyMetadata(""));
    public static void SetGlyph(DependencyObject o, string v) => o.SetValue(GlyphProperty, v);
    public static string GetGlyph(DependencyObject o) => (string)o.GetValue(GlyphProperty);
}
