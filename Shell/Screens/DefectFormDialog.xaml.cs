using System.Windows;

namespace TelecomProd.Shell.Screens;

public partial class DefectFormDialog : Window
{
    public string DefectType => TypeBox.Text?.Trim() ?? "";
    public string? Description => string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim();

    public DefectFormDialog(int _) { InitializeComponent(); }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DefectType)) { MessageBox.Show("Укажите тип дефекта.", "Проверка"); return; }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
