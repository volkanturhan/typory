using System.ComponentModel;

namespace typory.Services;

/// <summary>
/// Whether expansion is currently on, held as shared observable state so the
/// tray menu and the manager window can both toggle it and stay in sync — the
/// same pattern pixory uses for its colour format. The app listens to
/// <see cref="Changed"/> to pause/resume the keyboard hook and persist the
/// preference.
/// </summary>
public sealed class ExpansionState : INotifyPropertyChanged
{
    public static ExpansionState Instance { get; } = new();

    private bool _enabled = true;

    /// <summary>True while abbreviations are being expanded.</summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;

            _enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            Changed?.Invoke();
        }
    }

    /// <summary>Raised after the enabled state changes (for non-binding consumers).</summary>
    public event Action? Changed;

    public event PropertyChangedEventHandler? PropertyChanged;
}
