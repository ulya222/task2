using System.Windows;

namespace TelecomProd.Shell.Screens;

public partial class PassportDialog : Window
{
    public PassportDialog(string jsonContent, int orderId)
    {
        InitializeComponent();
        PassportText.Text = jsonContent;
        Title = $"Паспорт изделия — заказ №{orderId}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
