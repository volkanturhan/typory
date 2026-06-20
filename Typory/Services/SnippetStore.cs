using System.IO;
using System.Text.Json;
using Typory.Models;

namespace Typory.Services;

/// <summary>
/// Persists the user's snippets to disk as JSON under %APPDATA%\Typory. All
/// operations are best-effort: a missing or corrupt file yields the built-in
/// starter snippets, and a failed save is swallowed rather than allowed to crash
/// the app.
/// </summary>
public sealed class SnippetStore
{
    private sealed record StoredSnippet(string Abbreviation, string Expansion);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public SnippetStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Typory");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "snippets.json");
    }

    /// <summary>True if no snippet file exists yet (i.e. first run).</summary>
    public bool IsFirstRun => !File.Exists(_filePath);

    /// <summary>Loads the saved snippets, falling back to the starter set.</summary>
    public IReadOnlyList<Snippet> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return DefaultSnippets();

            var stored = JsonSerializer.Deserialize<List<StoredSnippet>>(File.ReadAllText(_filePath));
            if (stored is null)
                return DefaultSnippets();

            return stored
                .Select(s => new Snippet(s.Abbreviation, s.Expansion))
                .ToList();
        }
        catch
        {
            // Corrupt or unreadable file: start from the defaults rather than fail.
            return DefaultSnippets();
        }
    }

    /// <summary>Writes the current snippets to disk.</summary>
    public void Save(IEnumerable<Snippet> snippets)
    {
        try
        {
            var stored = snippets
                .Select(s => new StoredSnippet(s.Abbreviation, s.Expansion))
                .ToList();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(stored, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; losing a save is preferable to crashing.
        }
    }

    // A couple of harmless examples so the app does something useful on first run
    // and shows the user the expected shape of a rule.
    private static List<Snippet> DefaultSnippets() => new()
    {
        new Snippet(";mail", "volkanturhan@gmail.com"),
        new Snippet(";date", "{type the date you like}"),
        new Snippet(";shrug", "¯\\_(ツ)_/¯"),
    };
}
