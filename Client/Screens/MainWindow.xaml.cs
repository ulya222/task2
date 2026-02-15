using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataVault.Client.ViewModels;

namespace DataVault.Client.Screens;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void TasksViewTable_Checked(object sender, RoutedEventArgs e)
    {
        if (KanbanScroll != null) KanbanScroll.Visibility = Visibility.Collapsed;
        if (TasksDataGrid != null) TasksDataGrid.Visibility = Visibility.Visible;
    }

    private void TasksViewKanban_Checked(object sender, RoutedEventArgs e)
    {
        if (KanbanScroll != null) KanbanScroll.Visibility = Visibility.Visible;
        if (TasksDataGrid != null) TasksDataGrid.Visibility = Visibility.Collapsed;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        if (ThemeToggleBtn != null)
            ThemeToggleBtn.Content = App.IsDarkTheme ? "Светлая тема" : "Тёмная тема";
    }

    private void ScanBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        if (DataContext is MainViewModel vm && !string.IsNullOrWhiteSpace(vm.ScanCode))
            vm.ScanCodeCommand.Execute(vm.ScanCode);
    }
}
