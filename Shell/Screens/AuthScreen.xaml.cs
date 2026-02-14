using System.Windows;
using System.Windows.Controls;
using TelecomProd.Shell.ViewModels;

namespace TelecomProd.Shell.Screens;

public partial class AuthScreen : Window
{
    public AuthScreen()
    {
        InitializeComponent();
    }

    private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AuthViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password ?? "";
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AuthViewModel vm) return;
        vm.Password = PwdBox.Password ?? "";
        await vm.SignInCommand.ExecuteAsync(null);
    }
}
