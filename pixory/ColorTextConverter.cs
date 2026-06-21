using System.Globalization;
using System.Windows.Data;
using pixory.Models;
using pixory.Services;

namespace pixory;

/// <summary>
/// Turns a palette row (a <see cref="PickedColor"/>) into its caption in the
/// active format. Bound alongside <see cref="FormatState.Format"/> so the text
/// re-evaluates whenever the user switches HEX / RGB / HSL.
/// </summary>
public sealed class ColorTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not PickedColor color || values[1] is not ColorFormat format)
            return string.Empty;

        return ColorFormatting.Format(color.R, color.G, color.B, format);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
