using System.Windows;
using Typory.Models;
using Typory.Services;

namespace Typory;

/// <summary>
/// The snippet manager: a simple editable grid of abbreviation → expansion
/// rules. Edits bind straight to the live <see cref="SnippetManager"/>, which
/// persists them automatically, so this window has no Save button.
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
