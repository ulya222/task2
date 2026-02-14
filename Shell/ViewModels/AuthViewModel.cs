using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TelecomProd.Shell.Screens;
using TelecomProd.Shell.Services;

namespace TelecomProd.Shell.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    private readonly ApiClient _api = new();

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }
        try
        {
            var healthy = await _api.CheckHealthAsync();
            if (!healthy)
            {
                ErrorMessage = "API недоступен. Запустите Host (dotnet run --project Host) и убедитесь, что БД подключена.";
                return;
            }
            var response = await _api.LoginAsync(Login.Trim(), Password);
            if (response != null)
            {
                App.Current.Properties["CurrentUser"] = response;
                new DashboardWindow().Show();
                Application.Current.Windows.OfType<AuthScreen>().First().Close();
            }
            else
                ErrorMessage = "Неверный логин или пароль";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Ошибка: " + (ex.InnerException?.Message ?? ex.Message);
        }
    }
}
