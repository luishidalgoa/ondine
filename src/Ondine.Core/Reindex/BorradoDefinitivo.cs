namespace Ondine;

/// <summary>
/// Borrar un original de forma definitiva, sin pasar por la Papelera.
///
/// <para>
/// Solo se usa con la opción «Borrar de forma definitiva» de Preferencias, activada a mano por
/// la persona. Por defecto un original siempre va a la Papelera (<see cref="PapeleraDelSistema"/>),
/// que es lo que permite recuperarlo.
/// </para>
/// <para>
/// No borra carpetas, ni nada que no exista. Quien llama tiene que haber comprobado ya que el
/// fichero no es el propio comprimido (<see cref="Engine.ShouldRecycleSource"/>).
/// </para>
/// </summary>
public static class BorradoDefinitivo
{
    /// <summary>Borra el fichero. Devuelve <c>true</c> solo si ya no existe al terminar.</summary>
    public static bool Mandar(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta)) return false;
        // Como la Papelera: no se borra lo que otra copia de Ondine está usando.
        using var reserva = ReservaDeFichero.Esperar(ruta, TimeSpan.FromSeconds(5));
        if (reserva is null) return false;
        try
        {
            File.Delete(ruta);
            return !File.Exists(ruta);
        }
        catch { return false; }
    }
}
