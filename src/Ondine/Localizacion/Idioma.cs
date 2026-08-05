using System.ComponentModel;
using System.Globalization;

namespace Ondine.Localizacion;

/// <summary>
/// El idioma de la interfaz.
///
/// <para>
/// No se usa <c>.resx</c> a propósito. Un fichero de recursos separa el texto de
/// su clave en dos sitios distintos, y con eso llega lo de siempre: una clave
/// traducida en un idioma y olvidada en el otro, que nadie ve hasta que un
/// usuario abre la app en ese idioma. Aquí cada texto es una PROPIEDAD que
/// recibe las dos versiones a la vez, así que <b>una traducción que falta no
/// compila</b>: falta un argumento.
/// </para>
/// <para>
/// Además, los recursos satélite complican el publicado en fichero único y
/// recortado que usa la herramienta de terminal. Esto son propiedades normales:
/// el recortador las conserva sin que haya que decirle nada.
/// </para>
/// </summary>
public static class Idioma
{
    /// <summary>Los que hay. El primero es el que se usa si no hay nada guardado.</summary>
    public static readonly string[] Disponibles = ["en", "es"];

    /// <summary>Nombre de cada idioma, escrito en ese idioma.</summary>
    public static string Nombre(string codigo) => codigo switch
    {
        "es" => "Español",
        _ => "English",
    };

    private static string _actual = "en";

    /// <summary>
    /// El idioma en curso. Por defecto <c>en</c>: Ondine se distribuye por
    /// GitHub y su público es internacional; el castellano es el caso
    /// particular, no al revés.
    /// </summary>
    public static string Actual
    {
        get => _actual;
        set
        {
            var nuevo = Normalizar(value);
            if (nuevo == _actual) return;
            _actual = nuevo;
            // Un solo aviso para TODAS las propiedades: `null` significa «todo
            // cambió». Sin esto habría que enumerar cientos de nombres a mano
            // y cualquier olvido dejaría un texto sin refrescar en pantalla.
            Textos.Instancia.Refrescar();
            Cambio?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>Se dispara cuando cambia el idioma, para lo que no sea un enlace de datos.</summary>
    public static event EventHandler? Cambio;

    /// <summary>
    /// El texto que toca. Los dos argumentos son obligatorios: es lo que hace
    /// imposible dejarse una traducción a medias sin querer.
    /// </summary>
    public static string Elegir(string en, string es) => _actual == "es" ? es : en;

    /// <summary>
    /// Un texto escrito solo en castellano, a la espera de traducir.
    ///
    /// <para>
    /// Existe porque escribir las dos versiones mientras se diseña una pantalla
    /// cuesta el doble y la mitad de ese trabajo se tira: los textos cambian
    /// tres veces antes de quedarse. Así que <b>mientras se desarrolla se
    /// escribe en castellano</b> y el resto de idiomas se rellenan de una tanda
    /// cuando la pantalla ya está cerrada, y a petición.
    /// </para>
    /// <para>
    /// Pero esto NO es una puerta de atrás: es una <b>deuda anotada</b>. Se ve a
    /// simple vista leyendo el fichero, la prueba la cuenta y la enseña, y el CI
    /// <b>no publica una versión que lleve ni una sola</b>. La diferencia entre
    /// esto y escribir el literal a pelo es que esto deja rastro y aquello no.
    /// </para>
    /// </summary>
    /// <param name="es">El castellano, que se usa en los dos idiomas de momento.</param>
    public static string Pendiente(string es) => es;

    /// <summary>
    /// Deja un código en uno de los admitidos. Acepta cosas como <c>es-ES</c>,
    /// que es lo que devuelve el sistema, y no solo <c>es</c>.
    /// </summary>
    public static string Normalizar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return "en";
        var corto = codigo.Split('-', '_')[0].ToLowerInvariant();
        return Array.IndexOf(Disponibles, corto) >= 0 ? corto : "en";
    }

    /// <summary>
    /// El que propone el sistema la primera vez, antes de que el usuario elija.
    /// A un castellanohablante se le abre en castellano; a cualquier otro, en
    /// inglés.
    /// </summary>
    public static string DelSistema() =>
        Normalizar(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    /// <summary>
    /// Con qué idioma arranca la app, a partir de lo que hubiera guardado.
    ///
    /// <para>
    /// Vacío significa «todavía no he elegido», y entonces manda el sistema. Un
    /// código que aquí no existe -porque se eligió en una versión más nueva y
    /// luego se volvió atrás- se trata igual que no haber elegido: caer en
    /// inglés a secas dejaría la app en un idioma que esa persona no pidió
    /// nunca, teniendo el del sistema a mano.
    /// </para>
    /// </summary>
    /// <param name="guardado">Lo que venga del fichero de preferencias.</param>
    public static string Resolver(string? guardado)
    {
        if (string.IsNullOrWhiteSpace(guardado)) return DelSistema();
        var corto = guardado.Split('-', '_')[0].ToLowerInvariant();
        return Array.IndexOf(Disponibles, corto) >= 0 ? corto : DelSistema();
    }
}

/// <summary>
/// El único objeto al que se enlaza la interfaz.
///
/// <para>
/// Los textos viven en ficheros parciales, uno por ventana o vista
/// (<c>Textos.MainWindow.cs</c>, <c>Textos.Organizar.cs</c>…). Es lo que
/// permite tocar los textos de una pantalla sin abrir un fichero de mil líneas
/// compartido con todas las demás.
/// </para>
/// </summary>
public sealed partial class Textos : INotifyPropertyChanged
{
    public static Textos Instancia { get; } = new();

    private Textos() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Avisa de que TODO cambió. Lo llama <see cref="Idioma.Actual"/>.</summary>
    internal void Refrescar() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
