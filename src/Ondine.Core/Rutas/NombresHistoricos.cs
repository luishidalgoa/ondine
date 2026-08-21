namespace Ondine;

/// <summary>
/// Los nombres que la app usó ANTES de llamarse Ondine, y que hay que seguir conociendo para
/// limpiar lo que dejaron: la carpeta de datos, las claves del registro y los accesos directos.
///
/// <para>
/// Están todos juntos y aparte por una razón concreta: son los únicos literales del proyecto que
/// <b>NO deben seguir al nombre de la app</b>. Cuando el rename se hizo con un buscar-y-reemplazar,
/// estos se fueron con el resto y las funciones de limpieza quedaron apuntando a claves que nunca
/// existieron — sin lanzar, sin avisar, simplemente sin borrar nada. Sueltos por el código vuelve
/// a pasar; juntos y con un test que los fija, no.
/// </para>
///
/// <para>
/// <b>No los deduzcas del nombre del ensamblado.</b> Ese fue exactamente el error: el assembly se
/// llamaba <c>ShrinkVideo</c>, pero lo que se escribía en el registro era <c>ShrinkStudio.*</c>.
/// Si algún día hay que añadir otro, míralo en el fichero de la versión que lo escribía.
/// </para>
/// </summary>
public static class NombresHistoricos
{
    /// <summary>Carpeta de datos en %AppData% hasta la v1.4.0.</summary>
    public const string CarpetaDatos = "ShrinkStudio";

    /// <summary>Verbo del menú contextual (<c>SystemFileAssociations\{ext}\shell\...</c>).</summary>
    public const string VerboMenu = "ShrinkStudio.Comprimir";

    /// <summary>ProgId de «Abrir con» (<c>Software\Classes\...</c>).</summary>
    public const string ProgId = "ShrinkStudio.Video";

    /// <summary>Acceso directo dentro de la carpeta «Enviar a».</summary>
    public const string AccesoSendTo = "ShrinkStudio.lnk";

    /// <summary>
    /// Mutex de instancia única. Este SÍ llevaba el nombre del ensamblado, no el del producto —
    /// prueba de que no hay una regla que valga para todos y hay que mirarlos uno a uno.
    /// El instalador lo necesita para detectar la versión vieja abierta y poder cerrarla.
    /// </summary>
    public const string Mutex = "ShrinkVideoSingleInstanceMutex";

    /// <summary>Nombre del ejecutable, que el instalador tiene que borrar al actualizar.</summary>
    public const string Ejecutable = "ShrinkVideo.exe";

    /// <summary>Nombre del producto: el que llevan los accesos directos del menú Inicio y del Escritorio.</summary>
    public const string Producto = "ShrinkStudio";
}
