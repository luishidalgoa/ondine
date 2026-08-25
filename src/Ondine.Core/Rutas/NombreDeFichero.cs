namespace Ondine;

/// <summary>
/// Dejar un nombre de fichero que valga en los tres sistemas.
///
/// <para>
/// <b>Por qué no se usa <c>Path.GetInvalidFileNameChars()</c>:</b> devuelve nueve caracteres en
/// Windows y <b>dos</b> en Linux —la barra y el nulo—. Un limpiador escrito con esa API deja
/// pasar en Linux justo lo que venía a quitar: los dos puntos de «Alien: Covenant», la
/// interrogación de «¿Quién engañó a Roger Rabbit?», el asterisco, las comillas.
/// </para>
/// <para>
/// Y eso importa porque <b>una biblioteca de vídeo casi nunca se queda donde se creó</b>: acaba
/// en un disco compartido, en un NAS o servida a un cliente de Windows. Un «:» colado desde
/// Linux rompe el fichero justo donde se va a ver, no al crearlo. Y el mismo fichero dejaría
/// de llamarse igual visto desde un sistema o desde otro.
/// </para>
/// <para>
/// El motivo estaba escrito en dos sitios, cada uno con su copia de la lista, y otros dos
/// seguían llamando a la API del sistema. La lista vive aquí y solo aquí; que nadie vuelva a
/// la API lo vigila una prueba.
/// </para>
/// </summary>
public static class NombreDeFichero
{
    /// <summary>
    /// Los nueve que Windows no admite. Se aplican en todas las plataformas a propósito: el
    /// objetivo no es «válido aquí», es «válido donde se vaya a ver».
    /// </summary>
    public static readonly char[] Prohibidos = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// El nombre sin lo que no vale.
    ///
    /// <param name="porQue">
    /// Con qué se sustituye cada carácter prohibido. Un guion bajo por defecto, que es lo que
    /// hacía el renombrado de comprimir.
    /// </param>
    /// </summary>
    public static string Limpiar(string nombre, char porQue = '_')
    {
        if (string.IsNullOrEmpty(nombre)) return nombre;

        var a = nombre.ToCharArray();
        for (int i = 0; i < a.Length; i++)
        {
            // El nulo aparte: no está en la lista de Windows porque no se escribe a mano,
            // pero cortaría el nombre en cualquier sistema.
            if (a[i] == '\0' || Array.IndexOf(Prohibidos, a[i]) >= 0) a[i] = porQue;
        }

        // Un nombre que acaba en punto o en espacio es legal en Linux y NO SE PUEDE CREAR en
        // Windows: el sistema lo recorta al escribir, así que el fichero acaba llamándose
        // distinto de lo que se pidió y quien lo busque por su nombre no lo encuentra.
        return new string(a).TrimEnd(' ', '.');
    }
}
