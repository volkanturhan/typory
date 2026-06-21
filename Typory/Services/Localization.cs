using System.ComponentModel;

namespace typory.Services;

public enum AppLanguage
{
    English,
    Turkish,
}

/// <summary>
/// The app's tiny translation table and current-language state.
///
/// UI elements bind to the string indexer (e.g. <c>[ColAbbreviation]</c>)
/// against the shared <see cref="Instance"/>. When <see cref="Language"/>
/// changes we raise the special "Item[]" property change so every bound string
/// re-reads itself, giving a live language switch without rebuilding the UI.
/// Non-WPF consumers (the tray menu) can instead listen to
/// <see cref="LanguageChanged"/>.
/// </summary>
public sealed class Localization : INotifyPropertyChanged
{
    public static Localization Instance { get; } = new();

    private AppLanguage _language = AppLanguage.English;

    private static readonly Dictionary<string, string> English = new()
    {
        ["TrayManage"] = "Manage snippets",
        ["TrayEnabled"] = "Expansion enabled",
        ["TrayAutostart"] = "Start with Windows",
        ["TrayLanguage"] = "Language",
        ["TrayAbout"] = "About",
        ["TrayQuit"] = "Quit",
        ["ManagerTitle"] = "typory — Snippets",
        ["ColAbbreviation"] = "Abbreviation",
        ["ColExpansion"] = "Expansion",
        ["Add"] = "Add",
        ["Remove"] = "Remove",
        ["ManagerHint"] = "Type an abbreviation anywhere and typory replaces it with its expansion. Changes save automatically.",
        ["AboutDescription"] = "A lightweight text expander.",
        ["AboutVersion"] = "Version",
        ["AboutClose"] = "Close",
    };

    private static readonly Dictionary<string, string> Turkish = new()
    {
        ["TrayManage"] = "Snippet'leri yönet",
        ["TrayEnabled"] = "Genişletme açık",
        ["TrayAutostart"] = "Windows ile başlat",
        ["TrayLanguage"] = "Dil",
        ["TrayAbout"] = "Hakkında",
        ["TrayQuit"] = "Çıkış",
        ["ManagerTitle"] = "typory — Snippet'ler",
        ["ColAbbreviation"] = "Kısaltma",
        ["ColExpansion"] = "Karşılığı",
        ["Add"] = "Ekle",
        ["Remove"] = "Sil",
        ["ManagerHint"] = "Herhangi bir yerde kısaltmayı yaz, typory onu karşılığıyla değiştirsin. Değişiklikler otomatik kaydedilir.",
        ["AboutDescription"] = "Hafif bir metin genişletici.",
        ["AboutVersion"] = "Sürüm",
        ["AboutClose"] = "Kapat",
    };

    /// <summary>The active language. Changing it refreshes all bound strings.</summary>
    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;

            _language = value;

            // "Item[]" tells WPF that every indexer binding should re-evaluate.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke();
        }
    }

    /// <summary>The translation for <paramref name="key"/> in the current language.</summary>
    public string this[string key]
    {
        get
        {
            var table = _language == AppLanguage.Turkish ? Turkish : English;
            return table.TryGetValue(key, out var value) ? value : key;
        }
    }

    /// <summary>Raised after the language changes (for non-binding consumers).</summary>
    public event Action? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
}
