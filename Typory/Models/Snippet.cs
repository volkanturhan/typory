using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Typory.Models;

/// <summary>
/// A single expansion rule: when the user types <see cref="Abbreviation"/>, it is
/// replaced with <see cref="Expansion"/>. Both are editable from the manager
/// window, so the type raises change notifications for two-way binding and so the
/// store knows to re-save.
/// </summary>
public sealed class Snippet : INotifyPropertyChanged
{
    private string _abbreviation;
    private string _expansion;

    public Snippet(string abbreviation, string expansion)
    {
        _abbreviation = abbreviation;
        _expansion = expansion;
    }

    /// <summary>The short trigger the user types, e.g. <c>;mail</c>.</summary>
    public string Abbreviation
    {
        get => _abbreviation;
        set
        {
            if (_abbreviation == value)
                return;

            _abbreviation = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The full text the abbreviation expands into.</summary>
    public string Expansion
    {
        get => _expansion;
        set
        {
            if (_expansion == value)
                return;

            _expansion = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
