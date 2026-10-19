using System.Reflection;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace Ondine.Ava;

/// <summary>
/// Arranca libvlc, buscándola donde haga falta.
///
/// <para>
/// <c>Core.Initialize()</c> sin argumento vale en Windows —la biblioteca va dentro de la
/// aplicación—. En macOS no vale: no hay libvlc, y la que se puede tener vive dentro de
/// VLC.app, que no es una ruta de bibliotecas.
/// </para>
/// <para>
/// <b>Y en Linux tampoco, aunque se creyó que sí.</b> El sistema trae <c>libvlc.so.5</c>,
/// pero .NET busca <c>libvlc.so</c>, que solo existe con el paquete de desarrollo. Con solo
/// <c>vlc</c> instalado el reproductor moría con «falta libvlc». Se le enseña el nombre real
/// con un resolutor de bibliotecas; ver <see cref="RutaDeLibVlc.NombresEnLinux"/>.
/// </para>
/// <para>
/// Así que en un Mac se le dice dónde está. Dónde buscar lo decide
/// <see cref="RutaDeLibVlc"/>, en el motor y con sus pruebas; aquí solo se pasa el resultado.
/// Si no hay ninguna se llama igual sin argumento: fallará, y ese fallo es el que el
/// reproductor convierte en «instala VLC así». Adelantarse a él aquí sería tener el mensaje
/// escrito en dos sitios.
/// </para>
/// </summary>
internal static class MotorDeVideo
{
    private static bool _hecho;

    /// <summary>Una sola vez por ejecución: llamarlo dos veces no aporta y libvlc no lo pide.</summary>
    public static void Arrancar()
    {
        if (_hecho) return;
        _hecho = true;

        if (OperatingSystem.IsLinux())
            NativeLibrary.SetDllImportResolver(typeof(LibVLC).Assembly, ResolverEnLinux);

        if (OperatingSystem.IsMacOS() && RutaDeLibVlc.EnEsteMac() is { } donde)
            Core.Initialize(donde);
        else
            Core.Initialize();
    }

    /// <summary>
    /// Si ningún nombre carga se devuelve cero y manda el comportamiento de siempre, que es
    /// fallar con <see cref="DllNotFoundException"/>: esa excepción es la que el reproductor
    /// convierte en «instala VLC así».
    /// </summary>
    private static IntPtr ResolverEnLinux(string biblioteca, Assembly ensamblado, DllImportSearchPath? ruta)
    {
        foreach (var nombre in RutaDeLibVlc.NombresEnLinux(biblioteca))
            if (NativeLibrary.TryLoad(nombre, ensamblado, ruta, out var asa))
                return asa;
        return IntPtr.Zero;
    }
}
