using System.Windows;
using System.Windows.Controls;
using DataVault.Client.ViewModels;

namespace DataVault.Client.Screens;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password ?? "";
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;
        vm.Password = PwdBox.Password ?? "";
        await vm.SignInCommand.ExecuteAsync(null);
    }
}
