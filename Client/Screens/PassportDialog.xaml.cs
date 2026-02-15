using System.Windows;

namespace DataVault.Client.Screens;

public partial class PassportDialog : Window
{
    public PassportDialog(string jsonContent, int taskId)
    {
        InitializeComponent();
        PassportText.Text = jsonContent;
        Title = $"Паспорт задачи — №{taskId}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
