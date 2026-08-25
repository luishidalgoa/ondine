namespace Ondine.Reindex.Tests;

/// <summary>
/// Mandar un fichero a la papelera DEL SISTEMA, en cada sistema.
///
/// <para>
/// Esto existe porque la app promete una cosa y hasta ahora solo la cumplía en Windows.
/// Lo que manda a la papelera es <c>shell32</c>, y quien lo conectaba al motor era la ventana
/// principal <b>de WPF</b>. La de Avalonia no lo conectaba —no existía nada que conectar— y
/// el motor, sin nadie enchufado, <b>borra sin más</b>. Es decir: en Linux, lo que la app
/// dice que va a la papelera se perdía.
/// </para>
/// <para>
/// En Linux y macOS la papelera no es una llamada del sistema: es un <b>acuerdo sobre
/// carpetas</b> —la especificación de freedesktop.org—. El fichero se mueve a
/// <c>~/.local/share/Trash/files</c> y al lado se deja un <c>.trashinfo</c> que dice de dónde
/// venía y cuándo. Sin ese fichero de al lado, el gestor de archivos lo enseña pero
/// «Restaurar» no sabe dónde devolverlo — y eso es peor que no haberlo movido: parece
/// recuperable y no lo es.
/// </para>
/// <para>
/// Se prueba con ficheros de verdad en una carpeta temporal que hace de «home», así que
/// corre igual en Windows que en el Linux de CI.
/// </para>
/// </summary>
public static class PapeleraDelSistemaTests
{
    public static void Todas()
    {
        Program.Seccion("La papelera del sistema, en cada sistema");

        var raiz = Path.Combine(Path.GetTempPath(), "ondine-papelera-prueba");
        try
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, true);
            Directory.CreateDirectory(raiz);

            NadieTieneQueAcordarseDeConectarla();
            LoBasico(raiz);
            NoSePisanDosConElMismoNombre(raiz);
            LoQueNoSePuede(raiz);
        }
        finally { try { Directory.Delete(raiz, true); } catch { } }
    }

    /// <summary>
    /// El guardián del agujero que había: que la papelera venga PUESTA.
    ///
    /// <para>
    /// El fallo no fue escribir mal la papelera: fue que era un hueco que alguien tenía que
    /// rellenar al arrancar, y una interfaz nueva no lo rellenó. Sin nadie enchufado, el
    /// servicio borraba. Esto se planta si vuelve a quedarse vacío — porque el valor por
    /// defecto no lo protege ninguna otra prueba: <b>todas pasarían igual borrando</b>.
    /// </para>
    /// </summary>
    private static void NadieTieneQueAcordarseDeConectarla()
    {
        Program.Assert(PapeleraApp.EnviarASistema is not null,
            "la papelera del sistema viene puesta de fábrica: nadie tiene que acordarse de conectarla");
    }

    private static void LoBasico(string raiz)
    {
        var casa = Path.Combine(raiz, "casa");
        var origen = Path.Combine(raiz, "peli.mkv");
        File.WriteAllText(origen, "contenido");

        var ok = PapeleraDelSistema.Mandar(origen, casa);

        Program.Assert(ok, "un fichero se manda a la papelera");
        Program.Assert(!File.Exists(origen), "y deja de estar donde estaba");

        var enPapelera = Path.Combine(casa, ".local", "share", "Trash", "files", "peli.mkv");
        Program.Assert(File.Exists(enPapelera), "aparece en la carpeta de la papelera");
        Program.Assert(File.ReadAllText(enPapelera) == "contenido",
            "con su contenido intacto: se MUEVE, no se copia y se borra");

        // ══ Lo que hace que «Restaurar» funcione ═════════════════════════════
        var info = Path.Combine(casa, ".local", "share", "Trash", "info", "peli.mkv.trashinfo");
        Program.Assert(File.Exists(info), "y con su fichero de información al lado");

        var texto = File.ReadAllText(info);
        Program.Assert(texto.StartsWith("[Trash Info]"),
            "que empieza por la cabecera que la especificación exige");
        Program.Assert(texto.Contains("Path="), "y dice de dónde venía —sin eso, «Restaurar» no sabe dónde devolverlo—");
        Program.Assert(texto.Contains("DeletionDate="), "y cuándo se borró, que es por lo que el gestor las ordena");

        // La ruta va CODIFICADA como una URL. Un fichero con un espacio o un acento en su
        // carpeta es lo normal, y sin codificar la línea queda ambigua.
        var conRaro = Path.Combine(raiz, "una carpeta");
        Directory.CreateDirectory(conRaro);
        var raro = Path.Combine(conRaro, "ñandú #1.mkv");
        File.WriteAllText(raro, "x");
        PapeleraDelSistema.Mandar(raro, casa);

        var infoRaro = Path.Combine(casa, ".local", "share", "Trash", "info", "ñandú #1.mkv.trashinfo");
        Program.Assert(File.Exists(infoRaro), "un nombre con espacios y acentos también entra");
        var lineaPath = File.ReadAllLines(infoRaro).First(l => l.StartsWith("Path="));
        Program.Assert(!lineaPath.Contains(' ') && !lineaPath.Contains('#'),
            $"y su ruta va codificada, no en crudo ({lineaPath})");
    }

    private static void NoSePisanDosConElMismoNombre(string raiz)
    {
        // Borrar dos ficheros que se llaman igual desde carpetas distintas es de lo más
        // normal —«temporada 1/cap01.mkv» y «temporada 2/cap01.mkv»—. Si el segundo pisara
        // al primero, la papelera se habría comido uno de los dos EN SILENCIO.
        var casa = Path.Combine(raiz, "casa2");
        var a = Path.Combine(raiz, "a"); Directory.CreateDirectory(a);
        var b = Path.Combine(raiz, "b"); Directory.CreateDirectory(b);
        File.WriteAllText(Path.Combine(a, "cap01.mkv"), "el de la a");
        File.WriteAllText(Path.Combine(b, "cap01.mkv"), "el de la b");

        PapeleraDelSistema.Mandar(Path.Combine(a, "cap01.mkv"), casa);
        PapeleraDelSistema.Mandar(Path.Combine(b, "cap01.mkv"), casa);

        var dentro = Directory.GetFiles(Path.Combine(casa, ".local", "share", "Trash", "files"));
        Program.Assert(dentro.Length == 2,
            $"dos ficheros con el mismo nombre caben los dos ({dentro.Length})");

        var contenidos = dentro.Select(File.ReadAllText).OrderBy(x => x).ToList();
        Program.Assert(contenidos[0] == "el de la a" && contenidos[1] == "el de la b",
            "y ninguno se ha comido al otro");

        var infos = Directory.GetFiles(Path.Combine(casa, ".local", "share", "Trash", "info"));
        Program.Assert(infos.Length == 2, "cada uno con su ficha, que es como se distinguen al restaurar");
    }

    private static void LoQueNoSePuede(string raiz)
    {
        var casa = Path.Combine(raiz, "casa3");

        Program.Assert(!PapeleraDelSistema.Mandar(Path.Combine(raiz, "no-existe.mkv"), casa),
            "lo que no está no se manda, y se dice que no en vez de fingir que sí");

        // Una carpeta entera también va: la app manda carpetas vacías a la papelera al
        // reordenar por temporadas.
        var carpeta = Path.Combine(raiz, "vacia");
        Directory.CreateDirectory(carpeta);
        Program.Assert(PapeleraDelSistema.Mandar(carpeta, casa), "una carpeta entera también va");
        Program.Assert(!Directory.Exists(carpeta), "y deja de estar donde estaba");
    }
}
