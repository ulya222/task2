using System.Windows;
using TelecomProd.Core.Entities;

namespace TelecomProd.Shell.Screens;

public partial class MovementsDialog : Window
{
    public MovementsDialog(List<StockMovement> movements, int? componentId)
    {
        InitializeComponent();
        MovementsGrid.ItemsSource = movements;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
