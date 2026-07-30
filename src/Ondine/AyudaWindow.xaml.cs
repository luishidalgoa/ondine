using System.Windows;

namespace Ondine;

/// <summary>
/// Ventana de Ayuda: índice de tutoriales a la izquierda y su contenido a la derecha. Cada
/// tutorial es un panel que se muestra al elegirlo. Sin lógica: solo explica cómo funcionan
/// Organizar, Comprimir y Recortes, con el diagrama de prioridad del match dibujado en XAML.
/// </summary>
public partial class AyudaWindow : Window
{
    public AyudaWindow()
    {
        InitializeComponent();
        btnCerrar.Click += (_, _) => Close();
        navOrgComo.Checked += (_, _) => Mostrar(pagOrgComo);
        navOrgPasos.Checked += (_, _) => Mostrar(pagOrgPasos);
        navComprimir.Checked += (_, _) => Mostrar(pagComprimir);
        navRecortes.Checked += (_, _) => Mostrar(pagRecortes);
    }

    private void Mostrar(FrameworkElement pagina)
    {
        pagOrgComo.Visibility = Visibility.Collapsed;
        pagOrgPasos.Visibility = Visibility.Collapsed;
        pagComprimir.Visibility = Visibility.Collapsed;
        pagRecortes.Visibility = Visibility.Collapsed;
        pagina.Visibility = Visibility.Visible;
    }
}
