using System.Drawing;
using System.Windows.Forms;

namespace typory.Services;

/// <summary>
/// The system-tray presence for typory. While the app runs it lives here rather
/// than on the taskbar. The context menu opens the snippet manager, toggles
/// expansion on/off, and exposes the usual settings; the events below let the
/// application decide what each one does.
///
/// Menu text follows the app language: the menu is built once and its labels are
/// refreshed whenever <see cref="Localization"/> changes. Backed by the WinForms
/// <see cref="NotifyIcon"/>, which ships with the .NET SDK so typory needs no
/// third-party tray library.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon? _icon;

    // Hidden until an update is found; shown bold at the top of the menu, with
    // its own separator, so it stands out without cluttering the normal menu.
    private readonly ToolStripMenuItem _updateItem = new() { Visible = false };
    private readonly ToolStripSeparator _updateSeparator = new() { Visible = false };
    private string? _updateVersion;

    private readonly ToolStripMenuItem _manageItem = new();
    private readonly ToolStripMenuItem _enabledItem = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _autoStartItem = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _languageItem = new();
    private readonly ToolStripMenuItem _englishItem = new("English");
    private readonly ToolStripMenuItem _turkishItem = new("Türkçe");
    private readonly ToolStripMenuItem _themeItem = new();
    private readonly ToolStripMenuItem _systemThemeItem = new();
    private readonly ToolStripMenuItem _darkThemeItem = new();
    private readonly ToolStripMenuItem _lightThemeItem = new();
    private readonly ToolStripMenuItem _checkUpdateItem = new();
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _quitItem = new();

    /// <summary>Raised when the user asks to open the snippet manager.</summary>
    public event Action? ManageRequested;

    /// <summary>Raised when the user asks to see the About window.</summary>
    public event Action? AboutRequested;

    /// <summary>Raised when the user asks to quit the application.</summary>
    public event Action? QuitRequested;

    /// <summary>Raised when the user accepts the offered update.</summary>
    public event Action? UpdateRequested;

    /// <summary>Raised when the user asks to check for updates now.</summary>
    public event Action? CheckUpdateRequested;

    public TrayIcon()
    {
        // The update entry is drawn bold to read as the call-to-action it is.
        _updateItem.Font = new Font(SystemFonts.MenuFont!, System.Drawing.FontStyle.Bold);
        _updateItem.Click += (_, _) => UpdateRequested?.Invoke();

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

        // Theme submenu: System / Dark / Light, applied straight away.
        _systemThemeItem.Click += (_, _) => ThemeService.Apply(AppTheme.System);
        _darkThemeItem.Click += (_, _) => ThemeService.Apply(AppTheme.Dark);
        _lightThemeItem.Click += (_, _) => ThemeService.Apply(AppTheme.Light);
        _themeItem.DropDownItems.Add(_systemThemeItem);
        _themeItem.DropDownItems.Add(_darkThemeItem);
        _themeItem.DropDownItems.Add(_lightThemeItem);

        _checkUpdateItem.Click += (_, _) => CheckUpdateRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _updateItem,
            _updateSeparator,
            _manageItem,
            _enabledItem,
            new ToolStripSeparator(),
            _autoStartItem,
            _languageItem,
            _themeItem,
            _checkUpdateItem,
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
            Text = "typory",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => ManageRequested?.Invoke();
        // We only ever raise an update balloon, so clicking it means "yes, update".
        _notifyIcon.BalloonTipClicked += OnBalloonClicked;

        Localization.Instance.LanguageChanged += ApplyLanguage;
        // Re-tick the active theme entry whenever the theme changes.
        ThemeService.Changed += ApplyLanguage;
        ApplyLanguage();
    }

    /// <summary>
    /// Reveals the update entry for <paramref name="version"/> and shows a tray
    /// balloon so the user notices even without opening the menu. Call on the UI
    /// thread once a newer release has been found.
    /// </summary>
    public void ShowUpdateAvailable(string version)
    {
        _updateVersion = version;
        _updateItem.Visible = true;
        _updateSeparator.Visible = true;
        ApplyLanguage();

        var text = Localization.Instance;
        _notifyIcon.BalloonTipTitle = text["UpdateBalloonTitle"];
        _notifyIcon.BalloonTipText = text["UpdateBalloonText"];
        _notifyIcon.ShowBalloonTip(5000);
    }

    /// <summary>
    /// Shows a brief "you're up to date" balloon. Used to give feedback when the
    /// user checks for updates manually and there is nothing newer.
    /// </summary>
    public void ShowUpToDate()
    {
        var text = Localization.Instance;
        _notifyIcon.BalloonTipTitle = text["UpdateBalloonTitle"];
        _notifyIcon.BalloonTipText = text["UpToDate"];
        _notifyIcon.ShowBalloonTip(4000);
    }

    // The only balloon we raise is the update offer; treat a click on it as
    // acceptance.
    private void OnBalloonClicked(object? sender, EventArgs e)
    {
        if (_updateVersion is not null)
            UpdateRequested?.Invoke();
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
        _themeItem.Text = text["TrayTheme"];
        _systemThemeItem.Text = text["ThemeSystem"];
        _darkThemeItem.Text = text["ThemeDark"];
        _lightThemeItem.Text = text["ThemeLight"];
        _checkUpdateItem.Text = text["TrayCheckUpdate"];
        _aboutItem.Text = text["TrayAbout"];
        _quitItem.Text = text["TrayQuit"];

        // Keep the (version-stamped) update label in the current language too.
        if (_updateVersion is not null)
            _updateItem.Text = string.Format(text["TrayUpdate"], _updateVersion);

        _englishItem.Checked = text.Language == AppLanguage.English;
        _turkishItem.Checked = text.Language == AppLanguage.Turkish;

        _systemThemeItem.Checked = ThemeService.Theme == AppTheme.System;
        _darkThemeItem.Checked = ThemeService.Theme == AppTheme.Dark;
        _lightThemeItem.Checked = ThemeService.Theme == AppTheme.Light;
    }

    /// <summary>
    /// Loads the bundled typory icon at the system's small-icon size so the tray
    /// gets a crisp frame. Returns null on any failure.
    /// </summary>
    private static Icon? TryLoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/typory.ico");
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
        ThemeService.Changed -= ApplyLanguage;
        ExpansionState.Instance.Changed -= OnExpansionChanged;

        // Hide before disposing so the icon disappears immediately instead of
        // lingering in the tray until the user hovers over it.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }
}
