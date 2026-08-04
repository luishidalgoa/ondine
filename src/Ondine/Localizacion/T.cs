using System.Windows.Data;
using System.Windows.Markup;

namespace Ondine.Localizacion;

/// <summary>
/// La extensión de marcado que usa el XAML: <c>Text="{i:T Analizar}"</c>.
///
/// <para>
/// Devuelve un enlace de datos, no una cadena, y esa es toda la gracia: al
/// cambiar de idioma la interfaz se rehace sola, sin reiniciar la aplicación y
/// sin que ninguna ventana tenga que acordarse de refrescar sus textos. Una
/// versión que devolviera la cadena directamente obligaría a reiniciar, y eso
/// se nota.
/// </para>
/// <para>
/// El nombre de la propiedad va sin comprobación de compilación, que es el
/// único punto flojo de este diseño. Lo cubre una prueba: recorre todos los
/// XAML, saca cada nombre usado y falla si alguno no existe en
/// <see cref="Textos"/>.
/// </para>
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class TExtension : MarkupExtension
{
    public TExtension() { }

    public TExtension(string clave) => Clave = clave;

    /// <summary>Nombre de la propiedad de <see cref="Textos"/>.</summary>
    [ConstructorArgument("clave")]
    public string Clave { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var enlace = new Binding(Clave)
        {
            Source = Textos.Instancia,
            Mode = BindingMode.OneWay,
        };
        return enlace.ProvideValue(serviceProvider);
    }
}
