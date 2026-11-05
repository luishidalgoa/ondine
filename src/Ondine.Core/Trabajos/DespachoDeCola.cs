namespace Ondine.Trabajos;

/// <summary>
/// Quien saca los trabajos de la cola y los lanza, uno a uno.
///
/// <para>
/// <b>Hay un solo bucle a la vez.</b> Un trabajo añadido mientras otro corre no arranca un
/// segundo bucle: el que ya está en marcha lo recoge al terminar el suyo.
/// </para>
/// <para>
/// <b>Y si hay otra compresión en marcha que no es de la cola</b> (la del botón Comprimir),
/// el bucle no arranca. Esa compresión, al terminar, vuelve a llamar aquí para vaciar lo que
/// haya quedado. Sin esa segunda llamada, los trabajos añadidos durante la compresión directa
/// se quedaban esperando a que el usuario añadiera otro.
/// </para>
/// </summary>
public sealed class DespachoDeCola
{
    private readonly ColaDeTrabajos _cola;
    private bool _drenando;

    public DespachoDeCola(ColaDeTrabajos cola) => _cola = cola;

    /// <summary>
    /// Vacía la cola mientras <paramref name="ocupado"/> diga que no hay otra compresión en
    /// marcha. <paramref name="correr"/> comprime un trabajo y dice cómo acabó.
    /// <paramref name="alCambiar"/> se llama cada vez que cambia el estado de un trabajo, para
    /// que la pantalla se repinte.
    /// </summary>
    public async Task DrenarAsync(
        Func<bool> ocupado,
        Func<Trabajo, Task<(bool Cancelado, int Salieron, int Fallaron)>> correr,
        Action alCambiar)
    {
        if (_drenando) return;   // ya hay un bucle: él recoge lo que llegue
        _drenando = true;
        try
        {
            while (!ocupado() && _cola.Siguiente() is { } t)
            {
                _cola.Empezar(t.Id);
                alCambiar();

                // Un trabajo que revienta cuenta como fallido, y la cola sigue con el siguiente.
                // Si no, el trabajo se quedaría «en curso» para siempre y bloquearía la cola.
                (bool Cancelado, int Salieron, int Fallaron) r;
                try { r = await correr(t); }
                catch (Exception) { r = (false, 0, 0); }

                // Cancelar detiene la cola entera: quien pulsa «Detener» quiere parar, no pasar
                // al siguiente.
                if (r.Cancelado) { _cola.Cancelar(t.Id); alCambiar(); return; }

                _cola.Terminar(t.Id, r.Salieron, r.Fallaron);
                alCambiar();
            }
        }
        finally { _drenando = false; }
    }
}
