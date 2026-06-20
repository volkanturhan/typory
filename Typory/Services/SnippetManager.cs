using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Typory.Models;

namespace Typory.Services;

/// <summary>
/// The in-memory set of snippets the app expands. Exposes an observable list for
/// the manager UI to bind to, raises <see cref="Changed"/> after any edit so the
/// store can re-save, and answers "does what was just typed end with one of my
/// abbreviations?" for the keyboard hook.
/// </summary>
public sealed class SnippetManager
{
    private readonly ObservableCollection<Snippet> _items = new();

    public SnippetManager()
    {
        Items = new ReadOnlyObservableCollection<Snippet>(_items);
        _items.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>The snippets, exposed read-only for binding.</summary>
    public ReadOnlyObservableCollection<Snippet> Items { get; }

    /// <summary>Raised after any change (add, remove, or an edit to a snippet).</summary>
    public event Action? Changed;

    /// <summary>Replaces the current snippets with the given ones (used on load).</summary>
    public void Initialize(IEnumerable<Snippet> snippets)
    {
        foreach (var existing in _items)
            existing.PropertyChanged -= OnSnippetChanged;
        _items.Clear();

        foreach (var snippet in snippets)
            _items.Add(snippet);

        Changed?.Invoke();
    }

    /// <summary>Adds a new, empty snippet for the user to fill in.</summary>
    public Snippet AddNew()
    {
        var snippet = new Snippet(string.Empty, string.Empty);
        _items.Add(snippet);
        return snippet;
    }

    /// <summary>Removes a snippet.</summary>
    public void Remove(Snippet snippet) => _items.Remove(snippet);

    /// <summary>
    /// Finds the snippet whose abbreviation the typed text ends with, preferring
    /// the longest match so <c>;addr</c> wins over <c>;a</c>. Returns null when
    /// nothing matches.
    /// </summary>
    public Snippet? FindMatch(string typed)
    {
        Snippet? best = null;

        foreach (var snippet in _items)
        {
            var abbr = snippet.Abbreviation;
            if (abbr.Length == 0 || snippet.Expansion.Length == 0)
                continue;

            if (typed.EndsWith(abbr, StringComparison.Ordinal)
                && (best is null || abbr.Length > best.Abbreviation.Length))
            {
                best = snippet;
            }
        }

        return best;
    }

    // Keep per-snippet change subscriptions in sync and bubble every change up so
    // the application can persist it.
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (Snippet snippet in e.OldItems)
                snippet.PropertyChanged -= OnSnippetChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (Snippet snippet in e.NewItems)
                snippet.PropertyChanged += OnSnippetChanged;
        }

        Changed?.Invoke();
    }

    private void OnSnippetChanged(object? sender, PropertyChangedEventArgs e) => Changed?.Invoke();
}
