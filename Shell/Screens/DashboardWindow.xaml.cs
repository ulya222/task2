using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TelecomProd.Shell.ViewModels;

namespace TelecomProd.Shell.Screens;

public partial class DashboardWindow : Window
{
    public DashboardWindow() => InitializeComponent();

    private void OrdersViewTable_Checked(object sender, RoutedEventArgs e)
    {
        if (KanbanScroll != null) KanbanScroll.Visibility = Visibility.Collapsed;
        if (OrdersDataGrid != null) OrdersDataGrid.Visibility = Visibility.Visible;
    }

    private void OrdersViewKanban_Checked(object sender, RoutedEventArgs e)
    {
        if (KanbanScroll != null) KanbanScroll.Visibility = Visibility.Visible;
        if (OrdersDataGrid != null) OrdersDataGrid.Visibility = Visibility.Collapsed;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        if (ThemeToggleBtn != null)
            ThemeToggleBtn.Content = App.IsDarkTheme ? "Светлая тема" : "Тёмная тема";
    }

    private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        if (DataContext is DashboardViewModel vm && !string.IsNullOrWhiteSpace(vm.BarcodeText))
            vm.BarcodeScannedCommand.Execute(vm.BarcodeText);
    }
}
