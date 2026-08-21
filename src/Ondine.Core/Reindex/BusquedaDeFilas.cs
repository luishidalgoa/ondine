namespace Ondine.Reindex;

/// <summary>Los nombres de una fila que se pueden buscar. Nada más.</summary>
/// <param name="Aplicado">Si el renombrado ya ocurrió en el disco.</param>
/// <param name="Original">El nombre con el que llegó.</param>
/// <param name="Propuesta">El nombre que tendría al aplicar.</param>
/// <param name="NombreNuevo">El que tiene ya, si se aplicó.</param>
public readonly record struct FilaBuscable(
    bool Aplicado,
    string Original,
    string? Propuesta,
    string? NombreNuevo);

/// <summary>
/// Buscar por texto en la tabla de Organizar.
///
/// <para>
/// Se compara con la normalización del identificador, la misma que usa el cotejo: así
/// «sonrisa» encuentra «¡En busca de una sonrisa!» aunque el nombre lleve signos y tildes,
/// y «shin chan» encuentra «Shin-Chan».
/// </para>
/// <para>
/// La regla que importa: <b>una fila ya aplicada solo se encuentra por su nombre nuevo</b>.
/// El viejo ya no existe en el disco, y que siguiera apareciendo al buscarlo hacía dudar
/// de si el renombrado había ocurrido de verdad.
/// </para>
/// </summary>
public static class BusquedaDeFilas
{
    /// <summary>
    /// Lo que se busca, ya normalizado.
    ///
    /// <para>
    /// Existe por rendimiento, y no es una optimización de las de por si acaso: el filtro
    /// corre <b>a cada tecla</b> sobre la tabla entera, y una biblioteca de verdad trae
    /// cientos de filas. Normalizar la consulta dentro del bucle la normalizaría una vez
    /// por fila y por pulsación. Se hace una vez y se pasa hecha.
    /// </para>
    /// </summary>
    public readonly record struct Consulta(string Normalizada)
    {
        public static Consulta De(string? texto) => new(TitleMatch.Norm(texto ?? ""));

        /// <summary>Un filtro vacío no filtra. Unos espacios tampoco son una búsqueda:
        /// la normalización los deja en nada, así que esto los cubre solos.</summary>
        public bool Vacia => Normalizada.Length == 0;
    }

    /// <summary>Cómodo para una comprobación suelta. En un bucle, usa la sobrecarga
    /// que recibe la <see cref="Consulta"/> ya hecha.</summary>
    public static bool Pasa(FilaBuscable f, string consulta) => Pasa(f, Consulta.De(consulta));

    public static bool Pasa(FilaBuscable f, Consulta consulta)
    {
        if (consulta.Vacia) return true;

        var q = consulta.Normalizada;

        bool Contiene(string? s) =>
            s is not null && TitleMatch.Norm(s).Contains(q, StringComparison.Ordinal);

        // Aplicada: solo por lo que existe ahora. Si no se sabe el nombre nuevo se cae al
        // original — una fila que no aparece nunca es peor que una que aparece por un
        // nombre viejo.
        if (f.Aplicado) return Contiene(f.NombreNuevo ?? f.Original);

        return Contiene(f.Original) || Contiene(f.Propuesta);
    }
}
