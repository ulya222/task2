using System.Windows;

namespace DataVault.Client;

public partial class App : Application
{
    public static bool IsDarkTheme
    {
        get => !(Current.Resources.MergedDictionaries[0].Source?.OriginalString?.Contains("ThemeLight") ?? false);
        set
        {
            var uri = value ? new Uri("/DataVault.Client;component/Theme.xaml", UriKind.Relative) : new Uri("/DataVault.Client;component/ThemeLight.xaml", UriKind.Relative);
            Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = uri };
        }
    }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }
}
