using System.Reflection;

namespace Ondine.Peliculas;

/// <summary>
/// De dónde sale la clave de TMDb.
///
/// <para>
/// La decisión, tomada el 20 de agosto de 2026: la clave oficial va
/// <b>horneada</b> en las builds de la release —un secreto de CI que entra como
/// propiedad de MSBuild y viaja como metadato del ensamblado, igual que
/// <c>UpdateRepo</c>— y Preferencias tiene un campo que la <b>sobrescribe</b>.
/// </para>
/// <para>
/// Por qué así y no solo la del usuario: pedirle a alguien que solo quiere
/// ordenar su carpeta que se registre en TMDb y pegue una clave es un muro, y
/// esta app existe para ahorrar trabajo. Y por qué además el campo: quien clona
/// el repo y compila no tiene el secreto, y sin salida su build se queda con la
/// función muerta y sin explicación.
/// </para>
/// <para>
/// Lo que esto NO es: un secreto. Una clave dentro de un binario se saca con un
/// editor hexadecimal, y quien la quiera la tendrá. Se asume a propósito porque
/// TMDb limita <b>por IP y no por clave</b> —así que no se agota entre todos los
/// usuarios— y porque sus términos no lo prohíben mientras Ondine sea gratis.
/// Es lo que hacen Jellyfin, Kodi y tinyMediaManager. <b>Si Ondine alguna vez
/// cobra, esta decisión caduca</b> y hay que volver a mirarla.
/// </para>
/// </summary>
public static class ClaveDeTmdb
{
    /// <summary>Nombre del metadato del ensamblado donde viaja la clave de la build.</summary>
    public const string Metadato = "TmdbKey";

    public enum Origen
    {
        /// <summary>No hay ninguna: ni horneada ni puesta a mano.</summary>
        Ninguna,

        /// <summary>La de la build oficial.</summary>
        Empotrada,

        /// <summary>La que el usuario puso en Preferencias.</summary>
        Usuario,
    }

    /// <summary>La clave que se va a usar, y de dónde ha salido.</summary>
    public sealed record Elegida(string? Clave, Origen De)
    {
        public bool Hay => !string.IsNullOrWhiteSpace(Clave);
    }

    /// <summary>
    /// Cuál de las dos manda. La del usuario siempre que la haya: si se ha
    /// molestado en ponerla, es porque quiere gastar su cuota y no la nuestra.
    /// </summary>
    public static Elegida Elegir(string? delUsuario, string? empotrada)
    {
        // Se recorta porque una clave se pega del navegador, y viene con el
        // espacio y el salto de línea de la selección. Un campo dejado a
        // espacios es un campo vacío, no una clave de espacios.
        var mia = (delUsuario ?? "").Trim();
        if (mia.Length > 0) return new(mia, Origen.Usuario);

        var oficial = (empotrada ?? "").Trim();
        if (oficial.Length > 0) return new(oficial, Origen.Empotrada);

        return new(null, Origen.Ninguna);
    }

    /// <summary>
    /// La clave horneada en esta build, o <c>null</c> si se compiló sin ella —
    /// que es lo que le pasa a cualquiera que clone el repo, y al arnés de
    /// pruebas.
    /// </summary>
    public static string? Empotrada
    {
        get
        {
            var meta = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == Metadato);
            return string.IsNullOrWhiteSpace(meta?.Value) ? null : meta!.Value!.Trim();
        }
    }

    /// <summary>Lo mismo que <see cref="Elegir"/>, ya con la de esta build.</summary>
    public static Elegida Actual(string? delUsuario) => Elegir(delUsuario, Empotrada);
}
