using System.Windows;

namespace TelecomProd.Shell;

public partial class App : Application
{
    public static bool IsDarkTheme
    {
        get => !(Current.Resources.MergedDictionaries[0].Source?.OriginalString?.Contains("ThemeLight") ?? false);
        set
        {
            var uri = value ? new Uri("/TelecomProd.Shell;component/Theme.xaml", UriKind.Relative) : new Uri("/TelecomProd.Shell;component/ThemeLight.xaml", UriKind.Relative);
            Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = uri };
        }
    }

    public static void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }
}
