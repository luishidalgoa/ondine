using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Ondine.Localizacion;

namespace Ondine.Ava.Localizacion;

/// <summary>
/// La extensión de marcado del XAML: <c>Text="{i:T Analizar}"</c>.
///
/// <para>
/// Es la hermana de la de WPF y hace lo mismo por el mismo motivo: devuelve un ENLACE, no
/// una cadena. Así al cambiar de idioma la interfaz se rehace sola, sin reiniciar y sin que
/// ninguna ventana tenga que acordarse de refrescar sus textos.
/// </para>
/// <para>
/// Y sobre todo: <b>lee del MISMO catálogo</b>. Los textos viven en Ondine.Core y los
/// comparten las dos interfaces, así que traducir algo una vez lo traduce en las dos. Un
/// catálogo por interfaz habría duplicado 5.700 líneas de texto y garantizado que se
/// separaran.
/// </para>
/// <para>
/// El nombre de la propiedad va sin comprobación de compilación, igual que en WPF, y lo
/// cubre la misma prueba: recorre los XAML, saca cada nombre usado y falla si alguno no
/// existe en <see cref="Textos"/>.
/// </para>
/// </summary>
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }

    public TExtension(string clave) => Clave = clave;

    /// <summary>Nombre de la propiedad de <see cref="Textos"/>.</summary>
    public string Clave { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(Clave)
        {
            Source = Textos.Instancia,
            Mode = BindingMode.OneWay,
        };
}
