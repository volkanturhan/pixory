using System.ComponentModel;

namespace pixory.Services;

/// <summary>
/// Holds the colour format currently chosen for copying (HEX / RGB / HSL) as
/// shared, observable state. The palette binds each row's caption to this so
/// switching format from the tray relabels every swatch live — the same trick
/// <see cref="Localization"/> uses for language.
/// </summary>
public sealed class FormatState : INotifyPropertyChanged
{
    public static FormatState Instance { get; } = new();

    private ColorFormat _format = ColorFormat.Hex;

    /// <summary>The active copy format. Changing it relabels every bound swatch.</summary>
    public ColorFormat Format
    {
        get => _format;
        set
        {
            if (_format == value)
                return;

            _format = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Format)));
            Changed?.Invoke();
        }
    }

    /// <summary>Raised after the format changes (for non-binding consumers).</summary>
    public event Action? Changed;

    public event PropertyChangedEventHandler? PropertyChanged;
}
