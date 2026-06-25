using System.Windows;
using typory.Models;
using typory.Services;

// Disambiguate from System.Windows.Localization (pulled in via System.Windows).
using Localization = typory.Services.Localization;

namespace typory;

/// <summary>
/// The snippet manager: an editable grid of abbreviation → expansion rules.
/// Settings (language, enable/disable, start with Windows, theme, about) live in
/// the tray menu. Edits bind straight to the live <see cref="SnippetManager"/>,
/// which persists them automatically, so this window has no Save button.
/// </summary>
public partial class ManagerWindow : Window
{
    private readonly SnippetManager _snippets;

    public ManagerWindow(SnippetManager snippets)
    {
        InitializeComponent();

        _snippets = snippets;
        Grid.ItemsSource = snippets.Items;
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
}
