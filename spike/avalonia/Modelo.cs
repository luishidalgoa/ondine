using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ondine.Spike;

/// <summary>
/// Un calco reducido de <c>PeliculaFila</c>, con lo justo para que la prueba sea la
/// prueba y no una maqueta. Lo que importa es que la fila tiene una lista dentro
/// (<see cref="Candidatos"/>) y que elegir uno de esa lista cambia la FILA, no el
/// elemento: ese es el gesto de la revisión de Organizar.
/// </summary>
public sealed class Fila : INotifyPropertyChanged
{
    public required string Fichero { get; init; }
    public required IReadOnlyList<Candidato> Candidatos { get; init; }

    /// <summary>Sin candidatos no hay nada que revisar y el panel no debe aparecer.</summary>
    public bool TieneDetalle => Candidatos.Count > 0;

    private bool _marcada;
    public bool Marcada { get => _marcada; set => Poner(ref _marcada, value); }

    private string _propuesta = "";
    public string Propuesta { get => _propuesta; set => Poner(ref _propuesta, value); }

    private string _confianza = "dudosa";
    public string Confianza { get => _confianza; set => Poner(ref _confianza, value); }

    private Candidato? _elegido;
    public Candidato? Elegido
    {
        get => _elegido;
        set
        {
            if (!Poner(ref _elegido, value)) return;
            Propuesta = value is null ? "—" : $"{value.Titulo} ({value.Anio})";
            Confianza = value is null ? "dudosa" : "segura";
            Avisar(nameof(HayEleccion));
        }
    }

    public bool HayEleccion => _elegido is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Poner<T>(ref T campo, T valor, [CallerMemberName] string? prop = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor)) return false;
        campo = valor;
        Avisar(prop);
        return true;
    }

    private void Avisar(string? prop) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public sealed record Candidato(string Titulo, string? Original, int? Anio);

/// <summary>Los datos de la prueba. Inventados, pero con la forma de los de verdad.</summary>
public static class Datos
{
    public static ObservableCollection<Fila> Filas() =>
    [
        new Fila
        {
            Fichero = "Psicosis.mkv",
            Propuesta = "—",
            Candidatos =
            [
                new("Psicosis", "Psycho", 1960),
                new("Psicosis", "Psycho", 1998),
            ],
        },
        new Fila
        {
            Fichero = "Blade Runner 2049 (2017).mp4",
            Propuesta = "Blade Runner 2049 (2017)",
            Confianza = "segura",
            // Sin candidatos: la fila NO debe desplegar panel aunque se seleccione.
            Candidatos = [],
        },
        new Fila
        {
            Fichero = "el resplandor.avi",
            Propuesta = "—",
            Candidatos =
            [
                new("El resplandor", "The Shining", 1980),
                new("El resplandor", "The Shining", 1997),
                new("Resplandor", "Doctor Sleep", 2019),
            ],
        },
    ];
}
