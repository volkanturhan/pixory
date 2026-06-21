using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace pixory.Services;

/// <summary>
/// Grabs a single snapshot of the whole desktop (all monitors). The picker
/// freezes this snapshot and samples colours from it rather than reading the
/// live screen pixel-by-pixel, which keeps sampling instant and flicker-free and
/// means the loupe can magnify without fighting the overlay it draws on top.
///
/// The bitmap is in physical pixels; combined with a Per-Monitor-DPI-aware
/// process (see app.manifest) and <c>GetCursorPos</c>, a screen pixel maps
/// straight to a bitmap pixel regardless of display scaling.
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// Captures the entire virtual desktop. <paramref name="bounds"/> receives
    /// the virtual-screen rectangle in physical pixels, whose Left/Top give the
    /// origin to subtract when turning a cursor position into a bitmap index.
    /// </summary>
    public static Bitmap CaptureVirtualScreen(out Rectangle bounds)
    {
        bounds = SystemInformation.VirtualScreen;

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size,
            CopyPixelOperation.SourceCopy);

        return bitmap;
    }
}
