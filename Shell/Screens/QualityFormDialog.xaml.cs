using System.Windows;

namespace TelecomProd.Shell.Screens;

public partial class QualityFormDialog : Window
{
    public string TestProcedure => ProcedureBox.Text?.Trim() ?? "";
    public string? MeasurementResult => string.IsNullOrWhiteSpace(ResultBox.Text) ? null : ResultBox.Text.Trim();
    public bool Passed => PassedBox.IsChecked == true;
    public string? CertificateNumber => string.IsNullOrWhiteSpace(CertBox.Text) ? null : CertBox.Text.Trim();

    public QualityFormDialog(int _) { InitializeComponent(); }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TestProcedure)) { MessageBox.Show("Укажите процедуру тестирования.", "Проверка"); return; }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
