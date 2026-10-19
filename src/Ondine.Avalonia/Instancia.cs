using Ondine.Rutas;

namespace Ondine.Ava;

/// <summary>
/// Que haya una sola Ondine abierta. Ver <see cref="InstanciaUnica"/> para el cómo.
///
/// <para>
/// En Windows la versión de WPF ya lo hacía con un mutex; esta interfaz no, y en Linux se
/// podían abrir tantas ventanas como se quisiera: «Abrir con» sobre un vídeo, el lanzador del
/// menú o la propia app al reiniciarse tras actualizar, cada una por su cuenta.
/// </para>
/// </summary>
internal static class Instancia
{
    private static InstanciaUnica? _mia;

    /// <summary>
    /// Se llama antes de arrancar la interfaz. Devuelve <c>false</c> si ya había otra: en ese
    /// caso se le han pasado los ficheros con los que nos abrieron y esta debe cerrarse sin
    /// abrir ventana.
    /// </summary>
    public static bool Arrancar(string[] args)
    {
        _mia = InstanciaUnica.Tomar(Path.Combine(DatosDeUsuario.Raiz, "instancia.lock"));
        if (_mia is not null) return true;

        var ficheros = VideosQueLlegan.DeLosArgumentos(args);
        if (InstanciaUnica.Avisar(ficheros, TimeSpan.FromSeconds(5))) return false;

        // Tiene el candado pero no contesta (se está arrancando, o está colgada). Mejor dos
        // ventanas que ninguna: quien pulsó el icono tiene que ver algo.
        return true;
    }

    /// <summary>Atiende a las que se abran después. <paramref name="alRecibir"/> llega en un hilo de fondo.</summary>
    public static void Escuchar(Action<IReadOnlyList<string>> alRecibir) => _mia?.Escuchar(alRecibir);
}
