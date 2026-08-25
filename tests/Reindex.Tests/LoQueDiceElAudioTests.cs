namespace Ondine.Reindex.Tests;

/// <summary>
/// Que los dos controles de audio digan la verdad y no se contradigan.
///
/// <para>
/// El fallo del E-AC-3 no fue solo del motor. Encima había <b>dos desplegables hablando de lo
/// mismo</b>: «Códec de audio» decía «Sin tocar» mientras el de al lado decía «AAC 128 kbps», y
/// nada los sincronizaba. El motor desempataba en silencio a favor del segundo. Quitar el
/// desempate del motor —que es lo correcto— deja al descubierto las otras dos mitades: los
/// rótulos que nombran un códec que ya no eligen, y los presets, que guardan el caudal pero no
/// el códec.
/// </para>
/// <para>
/// Los presets importan más de lo que parece: el caso del usuario venía de aplicar «Ligero para
/// móvil (720p)», que pone el caudal en 128 y deja el códec en «Sin tocar» porque no sabe
/// guardarlo. Sin este arreglo, ese preset dejaría de adelgazar el audio — que es justo lo que
/// promete su nombre.
/// </para>
/// </summary>
public static class LoQueDiceElAudioTests
{
    public static void Todas()
    {
        Program.Seccion("Lo que dicen los controles de audio");

        LosPresetsGuardanElCodec();
        LosDeFabricaQueAdelgazanLoPiden();
        LosRotulosNoNombranUnCodec();
        LasDosInterfacesLoAplicanIgual();
    }

    /// <summary>
    /// El preset lleva el índice del códec. Los guardados con versiones anteriores no lo
    /// traen: al leerse, ese campo sale 0 —«Sin tocar»—, que es exactamente lo que aquellos
    /// presets tenían a la vista cuando se guardaron.
    /// </summary>
    private static void LosPresetsGuardanElCodec()
    {
        var p = new Preset { Name = "prueba", Audio = 3, ACodec = 1 };
        Program.Assert(p.ACodec == 1, "un preset puede guardar el códec de audio");

        var viejo = System.Text.Json.JsonSerializer.Deserialize<Preset>(
            """{"Name":"de antes","Fmt":1,"Codec":0,"Quality":0,"Res":2,"Audio":3}""");
        Program.Assert(viejo is not null && viejo.ACodec == 0,
            "y un preset guardado antes de que existiera el campo se sigue leyendo, con «Sin tocar»");
    }

    /// <summary>
    /// Un preset de fábrica que elige un caudal está pidiendo recodificar: si no dice también
    /// el códec, tras el arreglo del motor no adelgazaría nada. Es la regresión concreta que
    /// este cambio podía introducir.
    /// </summary>
    private static void LosDeFabricaQueAdelgazanLoPiden()
    {
        foreach (var p in PresetStore.Factory())
        {
            var coherente = p.Audio == 0 || p.ACodec != 0;
            Program.Assert(coherente,
                $"«{p.Name}»: elige caudal de audio ({p.Audio}), así que también dice el códec ({p.ACodec})");
        }

        Program.Assert(PresetStore.Factory().Any(p => p.Audio == 0 && p.ACodec == 0),
            "y los que no tocan el audio siguen sin tocarlo: si todos pidieran códec, esto no diría nada");
    }

    /// <summary>
    /// El desplegable del caudal ya no nombra un códec. Nombrarlo era mentir desde que existe
    /// el desplegable de códec al lado: «AAC 128 kbps» con «Sin tocar» puesto son dos
    /// afirmaciones incompatibles en la misma fila de la pantalla.
    /// </summary>
    private static void LosRotulosNoNombranUnCodec()
    {
        foreach (var idioma in new[] { "es", "en" })
        {
            var antes = Ondine.Localizacion.Idioma.Actual;
            try
            {
                Ondine.Localizacion.Idioma.Actual = idioma;
                var t = Ondine.Localizacion.Textos.Instancia;

                foreach (var r in new[] { t.MainAudioKbps192, t.MainAudioKbps160, t.MainAudioKbps128, t.MainAudioKbps96 })
                    Program.Assert(!r.Contains("AAC", StringComparison.OrdinalIgnoreCase),
                        $"«{r}» ({idioma}) no nombra el códec: eso lo elige el desplegable de al lado");
            }
            finally { Ondine.Localizacion.Idioma.Actual = antes; }
        }
    }

    /// <summary>
    /// Y las dos interfaces lo aplican y lo guardan igual. Se mira el código fuente porque
    /// abrir las dos ventanas desde aquí no se puede: es tosco, y es lo que hay. Sin esto una
    /// de las dos se queda atrás sin que nadie se entere, que ya ha pasado en este proyecto.
    /// </summary>
    private static void LasDosInterfacesLoAplicanIgual()
    {
        var raiz = LocalizarRaiz();
        var caras = new[]
        {
            Path.Combine(raiz, "src", "Ondine", "MainWindow.xaml.cs"),
            Path.Combine(raiz, "src", "Ondine.Avalonia", "VentanaPrincipal.axaml.cs"),
        };

        foreach (var f in caras)
        {
            var nombre = Path.GetFileName(Path.GetDirectoryName(f)!);
            var texto = File.Exists(f) ? File.ReadAllText(f) : "";
            var sinComentarios = string.Join(" ", texto.Split('\n').Where(l => !l.TrimStart().StartsWith("//")));

            Program.Assert(sinComentarios.Contains("cboACodec.SelectedIndex = p.ACodec"),
                $"{nombre} aplica el códec de audio del preset");
            Program.Assert(sinComentarios.Contains("ACodec = cboACodec.SelectedIndex"),
                $"{nombre} lo guarda al crear un preset");
        }
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }
}
