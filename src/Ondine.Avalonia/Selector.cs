using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Ondine.Ava;

/// <summary>
/// Elegir una carpeta o un fichero, en cualquier sistema.
///
/// <para>
/// En WPF eran <c>Microsoft.Win32.OpenFolderDialog</c> y compañía, que son de Windows y
/// además <b>síncronos</b>. Aquí lo pide el <c>StorageProvider</c> de la ventana, que en
/// cada sistema abre el selector de ese sistema —en Linux Mint el de GTK— y devuelve una
/// tarea. Eso arrastra: todo método que pregunte por una carpeta pasa a ser <c>async</c>.
/// </para>
/// <para>
/// Vive aparte porque lo van a usar las tres pantallas que quedan, y porque así la
/// conversión de «lo que devuelve el selector» a «una ruta de toda la vida» está escrita una
/// sola vez. Esa conversión tiene truco: el selector devuelve un <c>IStorageFolder</c>, que
/// puede no ser un sitio del disco —una carpeta de red, un servicio montado— y entonces
/// <c>TryGetLocalPath</c> devuelve <c>null</c>. El motor trabaja con rutas, así que eso hay
/// que tratarlo como «no se eligió nada» en vez de pasarle una cadena vacía.
/// </para>
/// </summary>
internal static class Selector
{
    /// <summary>Una carpeta, o <c>null</c> si se canceló o no es un sitio del disco.</summary>
    public static async Task<string?> CarpetaAsync(Window? duena, string titulo, string? empezarEn = null)
    {
        if (duena is null) return null;

        var elegidas = await duena.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = titulo,
            AllowMultiple = false,
            SuggestedStartLocation = await Desde(duena, empezarEn),
        });

        return elegidas.Count == 0 ? null : Ruta(elegidas[0]);
    }

    /// <summary>Un fichero para abrir, o <c>null</c>.</summary>
    public static async Task<string?> FicheroAsync(Window? duena, string titulo,
                                                   string nombreDelTipo, params string[] extensiones)
    {
        if (duena is null) return null;

        var elegidos = await duena.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = titulo,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(nombreDelTipo) { Patterns = extensiones }],
        });

        return elegidos.Count == 0 ? null : Ruta(elegidos[0]);
    }

    /// <summary>Dónde guardar, o <c>null</c>.</summary>
    public static async Task<string?> GuardarComoAsync(Window? duena, string titulo, string nombreSugerido,
                                                       string nombreDelTipo, params string[] extensiones)
    {
        if (duena is null) return null;

        var elegido = await duena.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = titulo,
            SuggestedFileName = nombreSugerido,
            FileTypeChoices = [new FilePickerFileType(nombreDelTipo) { Patterns = extensiones }],
        });

        return elegido is null ? null : Ruta(elegido);
    }

    /// <summary>
    /// La ruta de toda la vida, o <c>null</c> si eso que se eligió no está en el disco.
    /// Devolver una cadena vacía sería peor: el motor la aceptaría y fallaría más adelante,
    /// lejos de donde se decidió.
    /// </summary>
    private static string? Ruta(IStorageItem item)
    {
        var r = item.TryGetLocalPath();
        return string.IsNullOrEmpty(r) ? null : r;
    }

    private static async Task<IStorageFolder?> Desde(Window duena, string? carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta) || !Directory.Exists(carpeta)) return null;
        try { return await duena.StorageProvider.TryGetFolderFromPathAsync(carpeta); }
        catch { return null; }
    }
}
