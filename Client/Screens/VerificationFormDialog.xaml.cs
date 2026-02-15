using System.Windows;

namespace DataVault.Client.Screens;

public partial class VerificationFormDialog : Window
{
    public string ProcedureName => ProcedureBox.Text?.Trim() ?? "";
    public string? ResultValue => string.IsNullOrWhiteSpace(ResultBox.Text) ? null : ResultBox.Text.Trim();
    public bool Passed => PassedBox.IsChecked == true;
    public string? CertificateNumber => string.IsNullOrWhiteSpace(CertBox.Text) ? null : CertBox.Text.Trim();

    public VerificationFormDialog(int _) => InitializeComponent();

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProcedureName)) { MessageBox.Show("Укажите процедуру проверки.", "Проверка"); return; }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
