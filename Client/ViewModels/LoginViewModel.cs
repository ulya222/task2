using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataVault.Client.Screens;
using DataVault.Client.Services;

namespace DataVault.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
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
                ErrorMessage = "Сервер недоступен. Запустите проект Server (dotnet run --project Server).";
                return;
            }
            var response = await _api.LoginAsync(Login.Trim(), Password);
            if (response != null)
            {
                App.Current.Properties["CurrentUser"] = response;
                new MainWindow().Show();
                Application.Current.Windows.OfType<LoginWindow>().First().Close();
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
