using System.Windows;
using System.Windows.Threading;
using Typory.Services;

// Enabling WinForms (for the tray icon) pulls the System.Windows.Forms version
// of Application into scope too, so spell out that we mean the WPF one; also
// disambiguate from System.Windows.Localization.
using Application = System.Windows.Application;
using Localization = Typory.Services.Localization;

namespace Typory;

/// <summary>
/// Application entry point. Wires together the long-lived pieces of Typory and
/// runs it as a tray application: there is no window on startup, the app lives in
/// the system tray, and it only exits when the user chooses "Quit".
///
/// The core flow: a global keyboard hook watches what you type; when the typed
/// text ends with one of your abbreviations, the abbreviation is deleted and the
/// expansion typed in its place — in whatever app you are using.
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private SettingsStore _settings = null!;
    private SnippetStore _store = null!;
    private SnippetManager _snippets = null!;
    private KeyboardHook _hook = null!;
    private TrayIcon _tray = null!;
    private ManagerWindow? _managerWindow;
    private AboutWindow? _aboutWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Only one Typory should own the keyboard hook at a time. If another
        // instance already holds the mutex, bow out quietly.
        _singleInstanceMutex = new Mutex(initiallyOwned: true,
            @"Local\Typory.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // No visible window means closing a window must not end the app; shutdown
        // is driven explicitly from the tray's Quit command.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Apply saved preferences before any UI is built, then persist changes.
        _settings = new SettingsStore();
        Localization.Instance.Language = _settings.LoadLanguage();
        ExpansionState.Instance.Enabled = _settings.LoadEnabled();
        Localization.Instance.LanguageChanged += SavePreferences;

        // Restore the saved snippets (or the starter set on first run), then keep
        // persisting them as they are edited.
        _store = new SnippetStore();
        _snippets = new SnippetManager();
        _snippets.Changed += () => _store.Save(_snippets.Items);
        _snippets.Initialize(_store.Load());

        // Watch typing and expand matches; pause/resume follows the shared state.
        _hook = new KeyboardHook { Paused = !ExpansionState.Instance.Enabled };
        _hook.Typed += OnTyped;
        ExpansionState.Instance.Changed += OnExpansionToggled;

        _tray = new TrayIcon();
        _tray.ManageRequested += ShowManager;
        _tray.AboutRequested += ShowAbout;
        _tray.QuitRequested += Shutdown;

        // Launching with "--manage" opens the snippet manager straight away, so a
        // shortcut can jump right to it instead of going via the tray.
        if (e.Args.Contains("--manage"))
            ShowManager();
    }

    // Called on the hook thread (our UI thread) after each typed character.
    private void OnTyped(string buffer)
    {
        var match = _snippets.FindMatch(buffer);
        if (match is null)
            return;

        // The whole abbreviation (including the character just typed) is now in
        // the target app; clear our buffer and replace it once the keystroke that
        // triggered us has finished being delivered.
        _hook.ResetBuffer();

        var backspaces = match.Abbreviation.Length;
        var expansion = match.Expansion;
        Dispatcher.BeginInvoke(() => TextInjector.Replace(backspaces, expansion),
            DispatcherPriority.Background);
    }

    private void OnExpansionToggled()
    {
        _hook.Paused = !ExpansionState.Instance.Enabled;
        SavePreferences();
    }

    private void SavePreferences()
        => _settings.Save(Localization.Instance.Language, ExpansionState.Instance.Enabled);

    /// <summary>Shows the snippet manager, reusing it if already open.</summary>
    private void ShowManager()
    {
        if (_managerWindow is not null)
        {
            _managerWindow.Activate();
            return;
        }

        _managerWindow = new ManagerWindow(_snippets);
        _managerWindow.AboutRequested += ShowAbout;
        _managerWindow.Closed += (_, _) => _managerWindow = null;
        _managerWindow.Show();
        _managerWindow.Activate();
    }

    /// <summary>Shows the About window, reusing it if already open.</summary>
    private void ShowAbout()
    {
        if (_aboutWindow is not null)
        {
            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        _aboutWindow.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hook?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
