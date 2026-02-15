using System.Windows;

namespace DataVault.Client.Screens;

public partial class RemarkFormDialog : Window
{
    public string RemarkType => TypeBox.Text?.Trim() ?? "";
    public string? Description => string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim();

    public RemarkFormDialog(int _) => InitializeComponent();

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RemarkType)) { MessageBox.Show("Укажите тип замечания.", "Проверка"); return; }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
