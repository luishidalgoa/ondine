using System.IO;

namespace Ondine.Reindex;

/// <summary>
/// Lo que va a costar el reordenado, dicho <b>antes</b> de empezarlo.
///
/// <para>
/// Un movimiento se ve siempre igual —una flecha de una carpeta a otra— y por
/// debajo puede ser gratis o puede ser una tarde. Dentro del mismo volumen mover
/// es reetiquetar; entre volúmenes es copiar entero y borrar. Y si por medio hay
/// una carpeta sincronizada, lo que se dispara no se ve en esta pantalla: se ve
/// en la barra de tareas, subiendo, durante horas.
/// </para>
/// <para>
/// Aquí <b>no se decide nada</b>: no se bloquea el reordenado ni se cambia el
/// plan. Solo se cuenta lo que va a pasar, porque quien lo lanza tiene derecho a
/// saberlo y no hay forma de deducirlo mirando la lista.
/// </para>
/// <para>
/// Como <see cref="PlanDeReordenado"/>, esto es cuentas y no disco: lo único que
/// hace falta preguntarle al sistema —si un fichero es un marcador— entra
/// inyectado, para que la decisión se pueda probar entera sin una nube delante.
/// </para>
/// </summary>
public static class RiesgoDelReordenado
{
    public enum Riesgo
    {
        /// <summary>Origen y destino están en discos distintos: se copia y se borra.</summary>
        CruzaVolumen,

        /// <summary>El destino está en una nube en la que el origen no estaba: se sube todo.</summary>
        Nube,

        /// <summary>Ficheros que solo están en el disco de nombre. Moverlos puede bajarlos.</summary>
        Marcador,
    }

    /// <param name="Detalle">Para <see cref="Riesgo.Nube"/>, el proveedor. Si no, vacío.</param>
    public sealed record Aviso(Riesgo Que, string Detalle, int Cuantos);

    /// <summary>
    /// Los avisos del plan, o una lista vacía si no hay ninguno.
    /// <paramref name="esMarcador"/> se inyecta; en la aplicación es
    /// <see cref="Nube.EsMarcador(string)"/>.
    /// </summary>
    /// <param name="montajes">
    /// Los puntos de montaje con los que decidir si dos rutas están en el mismo volumen. Se
    /// inyecta por lo mismo que <paramref name="esMarcador"/>: sin él, esta comprobación
    /// dependía de los discos que tuviera la máquina que ejecuta las pruebas, y por eso la
    /// suya se saltaba entera fuera de Windows.
    /// </param>
    public static List<Aviso> Mirar(
        IEnumerable<PlanDeReordenado.Paso> plan,
        IEnumerable<Nube.Sincronizacion> raices,
        Func<string, bool>? esMarcador = null,
        IReadOnlyList<string>? montajes = null)
    {
        esMarcador ??= Nube.EsMarcador;
        montajes ??= PuntoDeMontaje.DeEstaMaquina();
        var nubes = raices as IReadOnlyCollection<Nube.Sincronizacion> ?? raices.ToList();

        var cruzan = 0;
        var marcadores = 0;
        var porNube = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var paso in plan)
        {
            // Solo lo que se mueve de verdad. Contar los que se quedan quietos
            // asusta por trabajo que nadie va a hacer, y un aviso que exagera se
            // deja de leer a la segunda vez.
            if (paso.Motivo != PlanDeReordenado.Porque.Va || paso.Destino is not { } destino) continue;

            if (!MismoVolumen(paso.Origen, destino, montajes)) cruzan++;

            // La nube del DESTINO solo importa si no es ya la del origen: moverse
            // dentro de la misma carpeta sincronizada es mover una referencia, no
            // volver a subir el fichero.
            var destinoEn = Nube.Duena(destino, nubes);
            if (destinoEn is not null)
            {
                var origenEn = Nube.Duena(paso.Origen, nubes);
                if (origenEn is null || !string.Equals(origenEn.Raiz, destinoEn.Raiz, StringComparison.OrdinalIgnoreCase))
                    porNube[destinoEn.Proveedor] = porNube.GetValueOrDefault(destinoEn.Proveedor) + 1;
            }

            if (esMarcador(paso.Origen)) marcadores++;
        }

        var avisos = new List<Aviso>();
        if (cruzan > 0) avisos.Add(new(Riesgo.CruzaVolumen, "", cruzan));
        foreach (var (proveedor, cuantos) in porNube) avisos.Add(new(Riesgo.Nube, proveedor, cuantos));
        if (marcadores > 0) avisos.Add(new(Riesgo.Marcador, "", marcadores));
        return avisos;
    }

    /// <summary>
    /// Si las dos rutas viven en el mismo volumen. Ante una ruta que no se puede
    /// resolver se responde que sí: el aviso existe para avisar de algo cierto, y
    /// uno disparado por una ruta rara es ruido.
    /// </summary>
    private static bool MismoVolumen(string a, string b, IReadOnlyList<string> montajes)
    {
        try
        {
            // El punto de montaje y NO Path.GetPathRoot: en Linux y macOS esa función
            // devuelve «/» para cualquier ruta absoluta, así que la carpeta personal y un NAS
            // montado en /mnt salían como el mismo volumen y este aviso NO SALTABA NUNCA
            // fuera de Windows. Justo donde más importa: en un NAS o en un USB, mover no es
            // renombrar, es copiar el fichero entero. Ver PuntoDeMontaje.
            // Sin GetFullPath, por lo mismo que PuntoDeMontaje no lo llama: es dependiente
            // del sistema y convertiría una comparación de rutas en algo que depende de dónde
            // corra. Las rutas de un plan salen del disco y ya son absolutas; si alguna no lo
            // fuera, no casaría con ningún montaje y se responde «el mismo volumen», que es
            // el lado bueno de equivocarse.
            var ra = PuntoDeMontaje.De(a, montajes);
            var rb = PuntoDeMontaje.De(b, montajes);

            // Ante la duda, el mismo volumen: un aviso que salta en falso es ruido, y este
            // existe para avisar de algo cierto.
            if (string.IsNullOrEmpty(ra) || string.IsNullOrEmpty(rb)) return true;
            return string.Equals(ra, rb, StringComparison.OrdinalIgnoreCase);
        }
        catch { return true; }
    }
}
