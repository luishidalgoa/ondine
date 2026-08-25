using LibVLCSharp.Shared;

namespace Ondine.Ava;

/// <summary>
/// Arranca libvlc, buscándola donde haga falta.
///
/// <para>
/// <c>Core.Initialize()</c> sin argumento vale en Windows —la biblioteca va dentro de la
/// aplicación— y en Linux —la trae el sistema en una ruta que el cargador mira—. En macOS no
/// vale ninguna de las dos cosas: no hay libvlc, y la que se puede tener vive dentro de
/// VLC.app, que no es una ruta de bibliotecas.
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

        if (OperatingSystem.IsMacOS() && RutaDeLibVlc.EnEsteMac() is { } donde)
            Core.Initialize(donde);
        else
            Core.Initialize();
    }
}
