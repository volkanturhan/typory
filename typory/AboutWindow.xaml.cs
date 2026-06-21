using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using typory.Services;

// Disambiguate from System.Windows.Localization (pulled in via System.Windows).
using Localization = typory.Services.Localization;

namespace typory;

/// <summary>
/// A small "About" dialog: icon, name, version, author, project and website
/// links, and licence. Its localized strings follow the app language.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"{Localization.Instance["AboutVersion"]} {version?.ToString(3)}";
    }

    private void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
