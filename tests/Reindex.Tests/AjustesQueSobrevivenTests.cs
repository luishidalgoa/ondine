using Ondine.Localizacion;

namespace Ondine.Reindex.Tests;

/// <summary>
/// Que lo que se guarda en Preferencias siga ahí al volver a abrir.
///
/// <para>
/// <b>El caso real:</b> cambiabas el idioma, se aplicaba al momento, y al reiniciar la
/// aplicación volvía al de antes. La primera sospecha —que no se guardara— era falsa: el
/// fichero se escribía perfectamente. Lo que faltaba era <b>leerlo al arrancar</b>. La versión
/// de WPF lo hacía y al portar se quedó fuera, así que el ajuste vivía en el disco sin que
/// nadie lo mirara.
/// </para>
/// <para>
/// Es una familia de fallo con nombre propio: <i>se guarda y no se aplica</i>. No da error, el
/// fichero está bien, y quien lo mire desde fuera dirá que funciona. Solo se nota reiniciando.
/// </para>
/// <para>
/// Por eso esta prueba hace el viaje entero: guarda, <b>olvida lo que hay en memoria</b> y
/// vuelve a leer — que es lo que hace un reinicio. Comprobar que <c>Save</c> escribe no habría
/// cazado nada.
/// </para>
/// </summary>
public static class AjustesQueSobrevivenTests
{
    public static void Todas()
    {
        Program.Seccion("Los ajustes sobreviven a reiniciar");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-ajustes-prueba");
        var antesRaiz = DatosDeUsuario.RaizOverride;
        var antesIdioma = Idioma.Actual;
        try
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, true);
            Directory.CreateDirectory(raiz);
            DatosDeUsuario.RaizOverride = raiz;

            ElIdiomaVuelveAlArrancar();
            LoDemasTambien();
            LasDosAppsLoAplicanAlArrancar();
        }
        finally
        {
            DatosDeUsuario.RaizOverride = antesRaiz;
            Idioma.Actual = antesIdioma;
            try { Directory.Delete(raiz, true); } catch { }
        }
    }

    /// <summary>
    /// El idioma, que es el que lo destapó. Se guarda, se «reinicia» poniendo otro en memoria,
    /// y se comprueba que al aplicar lo guardado vuelve el elegido.
    /// </summary>
    private static void ElIdiomaVuelveAlArrancar()
    {
        var s = SettingsStore.Load();
        s.Idioma = "en";
        SettingsStore.Save(s);

        // El «reinicio»: la memoria se queda con otra cosa, como estaría al abrir la app.
        Idioma.Actual = "es";

        // Y esto es lo que hace el arranque de la aplicación. Si esta línea no existiera en
        // App.axaml.cs —que es justo lo que pasaba—, el idioma guardado no volvería.
        Idioma.Actual = Idioma.Resolver(SettingsStore.Load().Idioma);

        Program.Assert(Idioma.Actual == "en",
            $"el idioma guardado vuelve al arrancar ({Idioma.Actual})");
    }

    /// <summary>
    /// Y que las DOS aplicaciones lo hagan de verdad al arrancar.
    ///
    /// <para>
    /// Lo de arriba comprueba la función, y la función nunca estuvo rota: lo que faltaba era la
    /// llamada. Una prueba que ejercita el ayudante y no a quien debería usarlo <b>habría pasado
    /// con el fallo puesto</b> — que es como se cuela esto.
    /// </para>
    /// <para>
    /// Se mira el código fuente porque el arranque de una app de escritorio no se puede invocar
    /// desde aquí. Es tosco y es lo que hay; lo importante es que mira a las dos, porque el
    /// fallo fue precisamente que una lo hacía y la otra no.
    /// </para>
    /// </summary>
    private static void LasDosAppsLoAplicanAlArrancar()
    {
        var raiz = LocalizarRaiz();
        var arranques = new[]
        {
            Path.Combine(raiz, "src", "Ondine", "App.xaml.cs"),
            Path.Combine(raiz, "src", "Ondine.Avalonia", "App.axaml.cs"),
        };

        foreach (var f in arranques)
        {
            var nombre = Path.GetFileName(Path.GetDirectoryName(f)!);
            if (!File.Exists(f)) { Program.Assert(false, $"no encuentro el arranque de {nombre}"); continue; }

            var texto = File.ReadAllText(f);
            // SIN LAS LÍNEAS, y esto lo enseñó su propio fallo. La primera versión miraba
            // línea a línea buscando «Idioma.Actual» y «Resolver» en la MISMA, y acusó a la app
            // de Avalonia de no aplicarlo cuando sí lo hace: la sentencia está partida en dos
            // líneas por longitud. Una comprobación que depende de dónde caiga un salto de
            // línea no comprueba el código, comprueba el formateo.
            var sinComentarios = string.Join(" ", texto
                .Split('\n')
                .Where(l => !l.TrimStart().StartsWith("//")));

            bool ponIdioma = sinComentarios.Contains("Idioma.Actual")
                          && sinComentarios.Contains("Resolver");
            bool leeAjustes = sinComentarios.Contains("SettingsStore.Load()");

            Program.Assert(ponIdioma && leeAjustes,
                $"{nombre} aplica el idioma guardado al arrancar (pone={ponIdioma}, lee={leeAjustes})");
        }
    }

    private static string LocalizarRaiz()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "src"))) d = d.Parent;
        return d?.FullName ?? "";
    }

    /// <summary>
    /// Y el resto de ajustes: que el fichero los conserve de verdad, con sus tipos. Si esto
    /// fallara, el problema sería otro —el guardado— y conviene poder distinguirlos.
    /// </summary>
    private static void LoDemasTambien()
    {
        var s = SettingsStore.Load();
        s.DefaultLang = "jpn";
        s.MinFreeMb = 4321;
        s.UseHardware = false;
        s.Recurse = false;
        SettingsStore.Save(s);

        var vuelto = SettingsStore.Load();
        Program.Assert(vuelto.DefaultLang == "jpn", $"el idioma de audio vuelve ({vuelto.DefaultLang})");
        Program.Assert(vuelto.MinFreeMb == 4321, $"y el margen de disco ({vuelto.MinFreeMb})");
        Program.Assert(!vuelto.UseHardware, "y la aceleración apagada sigue apagada");
        Program.Assert(!vuelto.Recurse, "y las subcarpetas");
    }
}
