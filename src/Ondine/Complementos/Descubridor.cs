using System.IO;
using Ondine.Localizacion;

namespace Ondine.Complementos;

/// <summary>
/// Encuentra los complementos instalados.
///
/// <para>
/// Una carpeta por complemento, con su <c>plugin.json</c> dentro. No hay lista
/// que mantener ni registro que actualizar: instalar es copiar una carpeta y
/// desinstalar es borrarla. En cuanto haya que apuntarlos en algún sitio,
/// alguien se olvidará y la lista empezará a mentir.
/// </para>
/// </summary>
public static class Descubridor
{
    /// <summary>Dónde se buscan. Junto a los ajustes y los presets, no en Archivos de programa.</summary>
    public static string Carpeta => Path.Combine(DatosDeUsuario.Raiz, "complementos");

    /// <param name="Bueno">Los que se pueden usar.</param>
    /// <param name="Descartado">
    /// Los que no, con su motivo. Se devuelven en vez de tirarse: un complemento
    /// que no aparece y no dice por qué deja a quien lo instaló mirando una lista
    /// vacía sin nada que corregir.
    /// </param>
    public sealed record Hallazgo(
        IReadOnlyList<Complemento> Bueno,
        IReadOnlyList<(Complemento Cual, string Motivo)> Descartado);

    public static Hallazgo Buscar() => BuscarEn(Carpeta);

    /// <summary>La versión con carpeta explícita, que es la que se puede probar.</summary>
    public static Hallazgo BuscarEn(string carpeta)
    {
        var buenos = new List<Complemento>();
        var malos = new List<(Complemento, string)>();

        if (!Directory.Exists(carpeta)) return new(buenos, malos);

        foreach (var sub in Directory.EnumerateDirectories(carpeta))
        {
            var manifiesto = Path.Combine(sub, "plugin.json");
            if (!File.Exists(manifiesto)) continue;   // una carpeta suelta no es un complemento roto

            var c = Complemento.Leer(manifiesto);
            if (c is null)
            {
                // Un manifiesto ilegible sí se cuenta: alguien puso ahí una carpeta
                // queriendo instalar algo, y merece saber que no ha entrado.
                malos.Add((new Complemento { Id = new DirectoryInfo(sub).Name, Carpeta = sub },
                           Textos.Instancia.ComplementoManifiestoIlegible));
                continue;
            }

            var reparo = c.Reparo();
            if (reparo is null) buenos.Add(c); else malos.Add((c, reparo));
        }

        // Por nombre, no por orden del sistema de ficheros: el orden en que el
        // disco devuelve las carpetas no es estable y la lista bailaría entre
        // arranques sin que nadie hubiera tocado nada.
        return new(
            buenos.OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList(),
            malos.OrderBy(m => m.Item1.Id, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
