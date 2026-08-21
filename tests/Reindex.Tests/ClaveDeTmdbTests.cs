using Ondine.Peliculas;

namespace Ondine.Reindex.Tests;

/// <summary>
/// De dónde sale la clave de TMDb, y por dónde viaja.
///
/// <para>
/// La decisión: la clave oficial va <b>horneada</b> en las builds de la release,
/// y Preferencias tiene un campo que la <b>sobrescribe</b>. Así quien instala el
/// instalador no tiene que registrarse en ningún sitio, y quien compile el repo
/// sin el secreto —o quiera gastar su propia cuota— pone la suya.
/// </para>
/// <para>
/// Lo que se prueba aquí no es «funciona»: es la <b>precedencia</b> y que la
/// ausencia de clave se note en vez de fallar por dentro. Una build sin secreto
/// tiene que poder decir «falta la clave, pon la tuya» y no dar un 401 que
/// parezca un fallo de red.
/// </para>
/// </summary>
public static class ClaveDeTmdbTests
{
    public static void Todas()
    {
        Program.Seccion("La clave de TMDb: de dónde sale y por dónde va");

        // ── Precedencia ───────────────────────────────────────────────────────
        var mia = ClaveDeTmdb.Elegir(delUsuario: "la-mia", empotrada: "la-oficial");
        Program.Assert(mia.Clave == "la-mia" && mia.De == ClaveDeTmdb.Origen.Usuario,
            "la del usuario manda sobre la horneada: si la pone, es porque quiere usar la suya");

        var oficial = ClaveDeTmdb.Elegir(delUsuario: null, empotrada: "la-oficial");
        Program.Assert(oficial.Clave == "la-oficial" && oficial.De == ClaveDeTmdb.Origen.Empotrada,
            "sin clave propia se usa la de la build, que es el caso del 99% de la gente");

        var borrada = ClaveDeTmdb.Elegir(delUsuario: "   ", empotrada: "la-oficial");
        Program.Assert(borrada.De == ClaveDeTmdb.Origen.Empotrada,
            "un campo vaciado a espacios es un campo vacío, no una clave de espacios");

        var pegada = ClaveDeTmdb.Elegir(delUsuario: "  abc123\n", empotrada: null);
        Program.Assert(pegada.Clave == "abc123",
            "una clave pegada del navegador trae espacios y un salto de línea: se recortan");

        // ── El caso que la decisión tenía que resolver ────────────────────────
        // Quien clona el repo y compila no tiene el secreto de CI. Eso NO puede
        // ser un misterio: la app tiene que saber que no tiene clave.
        var nada = ClaveDeTmdb.Elegir(delUsuario: null, empotrada: null);
        Program.Assert(!nada.Hay && nada.De == ClaveDeTmdb.Origen.Ninguna,
            "sin secreto de CI y sin clave propia: se sabe que no hay clave, no se descubre en el 401");

        // Y esto corre en el arnés, que se compila SIN el secreto: la prueba de
        // que compilar el repo a pelo deja la función apagada y visible.
        Program.Assert(ClaveDeTmdb.Empotrada is null,
            "este arnés se compila sin secreto, así que aquí no hay clave horneada");

        // ── Que el que hornea y el que lee sean el MISMO ensamblado ───────────
        // `Empotrada` busca el metadato con `Assembly.GetExecutingAssembly()`, así
        // que el atributo tiene que plantarlo el .csproj de ESTE proyecto. Si se
        // planta en otro -pasó a un paso de pasar al separar el motor de la app,
        // porque el atributo llevaba años en Ondine.csproj-, la búsqueda devuelve
        // null, la identificación se apaga sola y dice que no hay clave. Nadie ve
        // un error: la comprobación de arriba seguiría en verde, porque también
        // espera null. Por eso hace falta esta.
        Program.Assert(typeof(ClaveDeTmdb).Assembly.GetName().Name == "Ondine.Core",
            "la clave se hornea en Ondine.Core.csproj, que es donde vive quien la lee");

        // ── Por dónde viaja: nunca en la URL si es un token v4 ────────────────
        // TMDb da DOS credenciales distintas en la misma página de ajustes: la
        // «API Key (v3)», que va en la query, y el «API Read Access Token (v4)»,
        // que es un JWT y va en la cabecera. Quien pega el que no toca recibe un
        // 401 sin explicación, así que se distingue por la forma.
        var v3 = Tmdb.Peticion("https://api.themoviedb.org/3/search/movie?query=x", "0123456789abcdef");
        Program.Assert(v3.RequestUri!.ToString().Contains("api_key=0123456789abcdef"),
            "una clave v3 viaja como api_key en la query, que es como la espera TMDb");
        Program.Assert(v3.Headers.Authorization is null,
            "y no se manda además una cabecera que TMDb no va a mirar");

        var v4 = Tmdb.Peticion("https://api.themoviedb.org/3/search/movie?query=x",
                               "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiJ4In0.firma");
        Program.Assert(v4.Headers.Authorization?.Scheme == "Bearer",
            "un token v4 es un JWT y viaja en la cabecera");
        Program.Assert(!v4.RequestUri!.ToString().Contains("eyJ"),
            "y NO acaba en la URL: las URL se quedan en los registros de los proxys");

        // ── Encendido de fabrica ──────────────────────────────────────────────
        // Decision del 20 de agosto de 2026: viene encendido. El motivo es que la
        // clave ya la trae la app, asi que apagado significaba que la funcion
        // existia y nadie la encontraba: hay que saber que TMDb es una cosa, ir a
        // Preferencias y buscarla. Una funcion que solo usa quien ya sabia que
        // estaba no sirve de nada.
        //
        // Lo que lo compensa, y tiene que seguir estando: la pantalla de
        // peliculas DICE que se van a buscar en TMDb, identificar es un paso
        // aparte que hay que pulsar, y se puede apagar. Si eso desaparece, esto
        // se convierte en salir a internet a escondidas.
        var ajustes = new AjustesDeTmdb();
        Program.Assert(ajustes.Activo,
            "viene encendido: con la clave puesta, apagado era una funcion que nadie encontraba");

        // Y aun encendido, sin ninguna clave NO esta listo. Es lo que separa
        // «encendido» de «funciona», y lo que hace que una copia compilada del
        // repo sin el secreto lo diga en vez de fallar por dentro.
        Program.Assert(!ajustes.Listo,
            "encendido pero sin ninguna clave NO esta listo: eso hay que decirlo, no descubrirlo en el primer intento");

        // Cifrar la clave es DPAPI, que solo existe en Windows. Se dice en voz
        // alta en vez de saltarlo callando: un salto silencioso se lee como
        // cobertura.
        if (OperatingSystem.IsWindows())
        {
            ajustes.PonerClave("mi-clave-de-tmdb");
            Program.Assert(ajustes.TieneClave && ajustes.ClaveCifrada != "mi-clave-de-tmdb",
                "la clave se guarda cifrada, nunca en claro: settings.json se copia y se pega en informes de fallo");
            Program.Assert(ajustes.Clave() == "mi-clave-de-tmdb", "y se recupera entera");
            Program.Assert(ajustes.Listo, "con clave propia y encendido, listo");

            ajustes.PonerClave("");
            Program.Assert(!ajustes.TieneClave, "y vaciar el campo la borra, que es como se vuelve a la de la build");
        }
        else
        {
            Console.WriteLine("  · saltado: cifrar la clave es DPAPI y solo existe en Windows");
        }

        // Copiar los ajustes tiene que copiar TAMBIEN esto. Es el fallo que ya
        // esta documentado con los ajustes del modelo: MemberwiseClone es
        // superficial, y sin esto la ventana de preferencias editaria los
        // ajustes de verdad aunque luego se cancele.
        // Se APAGA en la copia, no se enciende: ahora viene encendido de fabrica,
        // y encenderlo otra vez no probaria nada -pasaria igual con una copia
        // superficial-.
        var reales = new Settings();
        var copia = reales.Clone();
        copia.Tmdb.Activo = false;
        Program.Assert(reales.Tmdb.Activo,
            "editar la copia de los ajustes no cambia los de verdad: cancelar tiene que cancelar");
    }
}
