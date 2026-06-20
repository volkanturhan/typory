using System.Drawing;
using System.Windows.Forms;

namespace Typory.Services;

/// <summary>
/// The system-tray presence for Typory. While the app runs it lives here rather
/// than on the taskbar. The context menu opens the snippet manager, toggles
/// expansion on/off, and exposes the usual settings; the events below let the
/// application decide what each one does.
///
/// Menu text follows the app language: the menu is built once and its labels are
/// refreshed whenever <see cref="Localization"/> changes. Backed by the WinForms
/// <see cref="NotifyIcon"/>, which ships with the .NET SDK so Typory needs no
/// third-party tray library.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon? _icon;

    private readonly ToolStripMenuItem _manageItem = new();
    private readonly ToolStripMenuItem _enabledItem = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _autoStartItem = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _languageItem = new();
    private readonly ToolStripMenuItem _englishItem = new("English");
    private readonly ToolStripMenuItem _turkishItem = new("Türkçe");
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _quitItem = new();

    /// <summary>Raised when the user asks to open the snippet manager.</summary>
    public event Action? ManageRequested;

    /// <summary>Raised when the user asks to see the About window.</summary>
    public event Action? AboutRequested;

    /// <summary>Raised when the user asks to quit the application.</summary>
    public event Action? QuitRequested;

    public TrayIcon()
    {
        _manageItem.Click += (_, _) => ManageRequested?.Invoke();

        // The enabled toggle is shared state, so the tray and the manager window
        // stay in sync however it is changed.
        _enabledItem.Checked = ExpansionState.Instance.Enabled;
        _enabledItem.CheckedChanged += (_, _) => ExpansionState.Instance.Enabled = _enabledItem.Checked;
        ExpansionState.Instance.Changed += OnExpansionChanged;

        _autoStartItem.Checked = AutoStart.IsEnabled();
        _autoStartItem.CheckedChanged += (_, _) => AutoStart.SetEnabled(_autoStartItem.Checked);
        _aboutItem.Click += (_, _) => AboutRequested?.Invoke();
        _quitItem.Click += (_, _) => QuitRequested?.Invoke();

        _englishItem.Click += (_, _) => Localization.Instance.Language = AppLanguage.English;
        _turkishItem.Click += (_, _) => Localization.Instance.Language = AppLanguage.Turkish;
        _languageItem.DropDownItems.Add(_englishItem);
        _languageItem.DropDownItems.Add(_turkishItem);

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _manageItem,
            _enabledItem,
            new ToolStripSeparator(),
            _autoStartItem,
            _languageItem,
            _aboutItem,
            new ToolStripSeparator(),
            _quitItem,
        });

        // Refresh the toggle states whenever the menu opens, so a change made in
        // the manager window (autostart, enable/disable) shows here too.
        menu.Opening += (_, _) =>
        {
            _autoStartItem.Checked = AutoStart.IsEnabled();
            _enabledItem.Checked = ExpansionState.Instance.Enabled;
        };

        // Managing snippets is the headline command, so make it the default
        // (bold) item and the double-click behaviour.
        _manageItem.Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);

        _icon = TryLoadAppIcon();
        _notifyIcon = new NotifyIcon
        {
            // Fall back to a generic icon if ours fails to load — never crash the
            // whole app over a tray icon.
            Icon = _icon ?? SystemIcons.Application,
            Text = "Typory",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => ManageRequested?.Invoke();

        Localization.Instance.LanguageChanged += ApplyLanguage;
        ApplyLanguage();
    }

    private void OnExpansionChanged() => _enabledItem.Checked = ExpansionState.Instance.Enabled;

    // Refresh every menu label from the current language and tick the active
    // language entry.
    private void ApplyLanguage()
    {
        var text = Localization.Instance;
        _manageItem.Text = text["TrayManage"];
        _enabledItem.Text = text["TrayEnabled"];
        _autoStartItem.Text = text["TrayAutostart"];
        _languageItem.Text = text["TrayLanguage"];
        _aboutItem.Text = text["TrayAbout"];
        _quitItem.Text = text["TrayQuit"];

        _englishItem.Checked = text.Language == AppLanguage.English;
        _turkishItem.Checked = text.Language == AppLanguage.Turkish;
    }

    /// <summary>
    /// Loads the bundled Typory icon at the system's small-icon size so the tray
    /// gets a crisp frame. Returns null on any failure.
    /// </summary>
    private static Icon? TryLoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/Typory.ico");
            using var stream = System.Windows.Application.GetResourceStream(uri).Stream;
            return new Icon(stream, SystemInformation.SmallIconSize);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Localization.Instance.LanguageChanged -= ApplyLanguage;
        ExpansionState.Instance.Changed -= OnExpansionChanged;

        // Hide before disposing so the icon disappears immediately instead of
        // lingering in the tray until the user hovers over it.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }
}
