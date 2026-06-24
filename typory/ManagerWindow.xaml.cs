using System.Windows;
using typory.Models;
using typory.Services;

// Disambiguate from System.Windows.Localization (pulled in via System.Windows).
using Localization = typory.Services.Localization;

namespace typory;

/// <summary>
/// The snippet manager: an editable grid of abbreviation → expansion rules plus
/// a menu mirroring the tray settings (language, enable/disable, start with
/// Windows, about), so the window stands on its own. Edits bind straight to the
/// live <see cref="SnippetManager"/>, which persists them automatically, so this
/// window has no Save button.
/// </summary>
public partial class ManagerWindow : Window
{
    private readonly SnippetManager _snippets;

    /// <summary>Raised when the user picks About from the menu.</summary>
    public event Action? AboutRequested;

    public ManagerWindow(SnippetManager snippets)
    {
        InitializeComponent();

        _snippets = snippets;
        Grid.ItemsSource = snippets.Items;

        RefreshMenuChecks();

        // Re-sync the menu ticks when the window regains focus, in case the same
        // settings were changed from the tray meanwhile.
        Activated += (_, _) => RefreshMenuChecks();
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        var snippet = _snippets.AddNew();

        // Select the new row and drop straight into editing its abbreviation.
        Grid.SelectedItem = snippet;
        Grid.ScrollIntoView(snippet);
        Grid.CurrentCell = new System.Windows.Controls.DataGridCellInfo(snippet, Grid.Columns[0]);
        Grid.BeginEdit();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is Snippet snippet)
            _snippets.Remove(snippet);
    }

    private void OnEnglish(object sender, RoutedEventArgs e)
    {
        Localization.Instance.Language = AppLanguage.English;
        RefreshMenuChecks();
    }

    private void OnTurkish(object sender, RoutedEventArgs e)
    {
        Localization.Instance.Language = AppLanguage.Turkish;
        RefreshMenuChecks();
    }

    private void OnToggleEnabled(object sender, RoutedEventArgs e)
        => ExpansionState.Instance.Enabled = EnabledMenuItem.IsChecked;

    private void OnToggleAutoStart(object sender, RoutedEventArgs e)
        => AutoStart.SetEnabled(AutoStartMenuItem.IsChecked);

    private void OnSystemTheme(object sender, RoutedEventArgs e)
    {
        ThemeService.Apply(AppTheme.System);
        RefreshMenuChecks();
    }

    private void OnDarkTheme(object sender, RoutedEventArgs e)
    {
        ThemeService.Apply(AppTheme.Dark);
        RefreshMenuChecks();
    }

    private void OnLightTheme(object sender, RoutedEventArgs e)
    {
        ThemeService.Apply(AppTheme.Light);
        RefreshMenuChecks();
    }

    private void OnAbout(object sender, RoutedEventArgs e) => AboutRequested?.Invoke();

    // Tick every menu item from the current state.
    private void RefreshMenuChecks()
    {
        EnglishMenuItem.IsChecked = Localization.Instance.Language == AppLanguage.English;
        TurkishMenuItem.IsChecked = Localization.Instance.Language == AppLanguage.Turkish;
        EnabledMenuItem.IsChecked = ExpansionState.Instance.Enabled;
        AutoStartMenuItem.IsChecked = AutoStart.IsEnabled();
        SystemThemeMenuItem.IsChecked = ThemeService.Theme == AppTheme.System;
        DarkThemeMenuItem.IsChecked = ThemeService.Theme == AppTheme.Dark;
        LightThemeMenuItem.IsChecked = ThemeService.Theme == AppTheme.Light;
    }
}
