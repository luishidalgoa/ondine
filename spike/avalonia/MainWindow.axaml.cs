using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;

namespace Ondine.Spike;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Fila> _filas = Datos.Filas();

    private LibVLC? _vlc;
    private MediaPlayer? _reproductor;
    private bool _arrastrando;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        var tabla = this.FindControl<DataGrid>("tabla")!;
        tabla.ItemsSource = _filas;

        Estado("listo · nada elegido todavía");

        PrepararVideo();
        Closed += (_, _) => SoltarVideo();

        // Con --auto la ventana se abre, se comprueba sola y se cierra. Es la unica
        // forma honesta de contestar: en Avalonia un binding roto no da error, y el
        // arbol visual del RowDetails no existe hasta que la fila se selecciona.
        var argumentos = Environment.GetCommandLineArgs();
        if (argumentos.Contains("--auto"))
            Opened += (_, _) => Dispatcher.UIThread.Post(async () =>
            {
                try { Comprobacion.Correr(this, tabla, _filas); }
                catch (Exception ex) { Comprobacion.Resultados.Add($"REVENTO: {ex}"); }

                // El video solo si se le dan los dos ficheros, para que la prueba del
                // DataGrid siga contestando aunque no haya con que probar el otro.
                var h264 = Tras(argumentos, "--h264");
                var av1 = Tras(argumentos, "--av1");
                if (h264 is not null && av1 is not null && _vlc is not null && _reproductor is not null)
                {
                    try { await ComprobacionVideo.Correr(_vlc, _reproductor, h264, av1); }
                    catch (Exception ex) { ComprobacionVideo.Resultados.Add($"REVENTO: {ex.GetType().Name} · {ex.Message}"); }
                }

                Close();
            }, DispatcherPriority.Background);
    }

    private static string? Tras(string[] args, string bandera)
    {
        var i = Array.IndexOf(args, bandera);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    // ── 1. El DataGrid ───────────────────────────────────────────────────────────

    /// <summary>
    /// El botón que vive dentro de la lista de candidatos y tiene que cambiar la FILA.
    /// Su <c>Tag</c> viene de un binding que sube hasta el <c>DataGridRow</c>; si ese
    /// binding no resuelve, aquí llega null y se dice, que es justo lo que se quiere
    /// medir. El texto del botón dice cuál de las dos sintaxis se usó.
    /// </summary>
    private void AlElegir(object? emisor, RoutedEventArgs e)
    {
        var boton = (Button)emisor!;
        var comoSePidio = boton.Content?.ToString() ?? "?";

        if (boton.Tag is not Fila fila)
        {
            Estado($"✗ {comoSePidio}: el binding a la fila devolvió {(boton.Tag is null ? "null" : boton.Tag.GetType().Name)}");
            return;
        }

        // El candidato es el DataContext del propio botón, que sí es el del elemento.
        if (boton.DataContext is not Candidato candidato)
        {
            Estado($"✗ {comoSePidio}: llegó la fila pero no el candidato");
            return;
        }

        fila.Elegido = candidato;
        Estado($"✓ {comoSePidio} · «{fila.Fichero}» → {fila.Propuesta} · confianza {fila.Confianza}");
    }

    private void AlOlvidar(object? emisor, RoutedEventArgs e)
    {
        if (((Button)emisor!).Tag is not Fila fila)
        {
            Estado("✗ olvidar: el binding directo a la fila devolvió null");
            return;
        }

        fila.Elegido = null;
        Estado($"✓ olvidado · «{fila.Fichero}» vuelve a estar sin decidir");
    }

    private void Estado(string texto) =>
        this.FindControl<TextBlock>("rotuloEstado")!.Text = texto;

    // ── 2. El vídeo ──────────────────────────────────────────────────────────────

    private void PrepararVideo()
    {
        try
        {
            Core.Initialize();
            _vlc = new LibVLC();
            _reproductor = new MediaPlayer(_vlc);
            this.FindControl<VideoView>("vista")!.MediaPlayer = _reproductor;

            var barra = this.FindControl<Slider>("barra")!;

            // El avance manda sobre la barra, salvo mientras la estás arrastrando: si no,
            // el vídeo te pelea el pulgar y saltas a donde no querías.
            _reproductor.TimeChanged += (_, ev) => Dispatcher.UIThread.Post(() =>
            {
                if (_arrastrando || _reproductor is null) return;
                barra.Value = _reproductor.Length > 0
                    ? ev.Time * 1000.0 / _reproductor.Length
                    : 0;
                Tiempo(ev.Time);
            });

            barra.AddHandler(PointerPressedEvent, (_, _) => _arrastrando = true,
                             RoutingStrategies.Tunnel);
            barra.AddHandler(PointerReleasedEvent, (_, _) =>
            {
                _arrastrando = false;
                Buscar(barra.Value);
            }, RoutingStrategies.Tunnel);
        }
        catch (Exception ex)
        {
            VideoDice($"✗ LibVLC no arrancó: {ex.GetType().Name} · {ex.Message}");
        }
    }

    /// <summary>
    /// Lo que de verdad se mide aquí: que la posición que pides sea la que sale. En la
    /// app de hoy un clic en la línea de tiempo se iba diez segundos atrás, así que se
    /// enseñan las dos cifras y su diferencia.
    /// </summary>
    private void Buscar(double valorDeLaBarra)
    {
        if (_reproductor is null || _reproductor.Length <= 0) return;

        var pedido = (long)(valorDeLaBarra / 1000.0 * _reproductor.Length);
        _reproductor.Time = pedido;

        Dispatcher.UIThread.Post(() =>
        {
            var real = _reproductor.Time;
            VideoDice($"pedido {pedido / 1000.0:0.000}s · real {real / 1000.0:0.000}s · " +
                      $"desvío {(real - pedido) / 1000.0:+0.000;-0.000;0.000}s");
        }, DispatcherPriority.Background);
    }

    private async void AlAbrirVideo(object? emisor, RoutedEventArgs e)
    {
        var elegidos = await StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Un vídeo cualquiera",
            AllowMultiple = false,
        });

        var ruta = elegidos.FirstOrDefault()?.TryGetLocalPath();
        if (ruta is null || _vlc is null || _reproductor is null) return;

        _reproductor.Play(new Media(_vlc, new Uri(ruta)));
        VideoDice($"reproduciendo {Path.GetFileName(ruta)}");
    }

    private void AlPlayPausa(object? emisor, RoutedEventArgs e) => _reproductor?.Pause();

    private void Tiempo(long ms) =>
        this.FindControl<TextBlock>("rotuloTiempo")!.Text =
            TimeSpan.FromMilliseconds(ms).ToString(@"hh\:mm\:ss\.fff");

    private void VideoDice(string texto) =>
        this.FindControl<TextBlock>("rotuloVideo")!.Text = texto;

    private void SoltarVideo()
    {
        // El mismo cuidado que hizo falta en WPF: soltar el reproductor al cerrar, o la
        // app se queda arrastrando el pipeline. Allí fue una fuga real.
        _reproductor?.Stop();
        _reproductor?.Dispose();
        _vlc?.Dispose();
        _reproductor = null;
        _vlc = null;
    }
}
