using System.ComponentModel;
using System.Runtime.CompilerServices;

// Enabling WinForms pulls System.Drawing into scope, so spell out that the
// swatch uses the WPF media types.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Pixory.Models;

/// <summary>
/// A single colour the user has picked off the screen, stored as plain 8-bit
/// RGB. The model is format-agnostic: how it is shown (HEX, RGB, HSL) is decided
/// at display time by <see cref="Services.ColorFormatting"/>.
/// </summary>
public sealed class PickedColor : INotifyPropertyChanged
{
    private bool _isPinned;
    private Brush? _swatch;

    public PickedColor(byte r, byte g, byte b, DateTime pickedAt, bool isPinned = false)
    {
        R = r;
        G = g;
        B = b;
        PickedAt = pickedAt;
        _isPinned = isPinned;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    /// <summary>When this colour was picked.</summary>
    public DateTime PickedAt { get; }

    /// <summary>
    /// Whether the user has pinned this colour. Pinned colours stay at the top
    /// of the palette and are never dropped when the list fills up.
    /// </summary>
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value)
                return;

            _isPinned = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The colour as <c>#RRGGBB</c>, used as the row's caption.</summary>
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>A frozen brush of this colour, for the swatch shown in the palette.</summary>
    public Brush Swatch =>
        _swatch ??= CreateFrozenBrush(Color.FromRgb(R, G, B));

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
