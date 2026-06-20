using System.IO;
using System.Text.Json;

namespace Typory.Services;

/// <summary>
/// Persists small user preferences — the chosen language and whether expansion
/// is currently enabled — as JSON under %APPDATA%\Typory. Best-effort, like
/// <see cref="SnippetStore"/>: failures fall back to defaults rather than
/// throwing.
/// </summary>
public sealed class SettingsStore
{
    private sealed record Data(string Language, bool Enabled);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public SettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Typory");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");
    }

    /// <summary>Loads the saved language, defaulting to English.</summary>
    public AppLanguage LoadLanguage()
    {
        var data = Read();
        return data is not null && Enum.TryParse<AppLanguage>(data.Language, out var language)
            ? language
            : AppLanguage.English;
    }

    /// <summary>Loads whether expansion is enabled, defaulting to on.</summary>
    public bool LoadEnabled() => Read()?.Enabled ?? true;

    /// <summary>Saves both preferences together.</summary>
    public void Save(AppLanguage language, bool enabled)
    {
        try
        {
            var data = new Data(language.ToString(), enabled);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch
        {
            // Best-effort; a lost preference is not worth crashing over.
        }
    }

    private Data? Read()
    {
        try
        {
            return File.Exists(_filePath)
                ? JsonSerializer.Deserialize<Data>(File.ReadAllText(_filePath))
                : null;
        }
        catch
        {
            return null;
        }
    }
}
