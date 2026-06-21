using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using pixory.Services;

// WinForms is enabled for the tray, so disambiguate the types this window uses
// down to their WPF versions.
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingColor = System.Drawing.Color;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;

namespace pixory;

/// <summary>
/// The full-screen colour picker. On show it freezes a snapshot of the whole
/// desktop, covers every monitor, and lets the user hover to magnify pixels and
/// click to pick the colour underneath. Esc, right-click, or losing focus
/// cancels.
///
/// Colours are sampled from the frozen snapshot using the real cursor position
/// (in physical pixels), so picking is pixel-accurate regardless of display
/// scaling; the loupe and readout are positioned with WPF's own coordinates.
/// </summary>
public partial class PickerOverlay : Window
{
    // How many pixels around the cursor the loupe magnifies (a square of side
    // 2*ZoomRadius+1). Small and odd so the centre cell sits under the cursor.
    private const int ZoomRadius = 8;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    private readonly DrawingBitmap _snapshot;
    private readonly BitmapSource _display;

    // Virtual-screen origin in physical pixels, subtracted from the cursor
    // position to index into the snapshot.
    private readonly int _originX;
    private readonly int _originY;

    private bool _finished;

    /// <summary>Raised with the chosen colour once the user clicks.</summary>
    public event Action<byte, byte, byte>? Picked;

    /// <summary>Raised when the user cancels without picking.</summary>
    public event Action? Cancelled;

    public PickerOverlay()
    {
        InitializeComponent();

        // Freeze the desktop now so the picker samples a stable image.
        _snapshot = ScreenCapture.CaptureVirtualScreen(out var bounds);
        _originX = bounds.Left;
        _originY = bounds.Top;
        _display = ToBitmapSource(_snapshot);
        ScreenImage.Source = _display;

        // Place the overlay over the whole virtual desktop. Window coordinates
        // are device-independent, so convert the physical bounds by the system
        // DPI scale (correct for the common uniform-scaling setup).
        var scale = GetDpiForSystem() / 96.0;
        Left = bounds.Left / scale;
        Top = bounds.Top / scale;
        Width = bounds.Width / scale;
        Height = bounds.Height / scale;

        Loaded += OnLoaded;
        MouseMove += OnMouseMove;
        MouseLeftButtonDown += OnPick;
        MouseRightButtonDown += OnCancelClick;
        KeyDown += OnKeyDown;
        Deactivated += OnDeactivated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Make sure the overlay owns input even though it was summoned from a
        // background process via the global hotkey.
        WindowActivator.ForceToForeground(new WindowInteropHelper(this).Handle);
        Activate();
        Focus();
        CaptureMouse();

        UpdateLoupe(GetCursorInRootDips());
    }

    private void OnMouseMove(object sender, MouseEventArgs e) => UpdateLoupe(e.GetPosition(Root));

    private void OnPick(object sender, MouseButtonEventArgs e)
    {
        if (_finished)
            return;

        if (TrySampleAtCursor(out var color))
        {
            _finished = true;
            Picked?.Invoke(color.R, color.G, color.B);
            Close();
        }
    }

    private void OnCancelClick(object sender, MouseButtonEventArgs e) => Cancel();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Cancel();
    }

    private void OnDeactivated(object? sender, EventArgs e) => Cancel();

    private void Cancel()
    {
        if (_finished)
            return;

        _finished = true;
        Cancelled?.Invoke();
        Close();
    }

    // Refresh the magnified view, the readout, and the loupe's position so it
    // trails the cursor and flips away from screen edges.
    private void UpdateLoupe(Point cursorInRoot)
    {
        if (!TrySampleAtCursor(out var color, out var px, out var py))
            return;

        const int region = 2 * ZoomRadius + 1;
        var rx = Math.Clamp(px - ZoomRadius, 0, _display.PixelWidth - region);
        var ry = Math.Clamp(py - ZoomRadius, 0, _display.PixelHeight - region);
        LoupeImage.Source = new CroppedBitmap(_display, new Int32Rect(rx, ry, region, region));

        var wpfColor = Color.FromRgb(color.R, color.G, color.B);
        ReadoutSwatch.Background = new SolidColorBrush(wpfColor);
        HexReadout.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        PositionLoupe(cursorInRoot);
    }

    private void PositionLoupe(Point cursor)
    {
        const double offset = 22;
        var size = LoupePanel.RenderSize;
        var width = size.Width > 0 ? size.Width : 132;
        var height = size.Height > 0 ? size.Height : 175;

        var x = cursor.X + offset;
        if (x + width > Root.ActualWidth)
            x = cursor.X - offset - width;

        var y = cursor.Y + offset;
        if (y + height > Root.ActualHeight)
            y = cursor.Y - offset - height;

        Canvas.SetLeft(LoupePanel, x);
        Canvas.SetTop(LoupePanel, y);
    }

    private bool TrySampleAtCursor(out DrawingColor color) => TrySampleAtCursor(out color, out _, out _);

    private bool TrySampleAtCursor(out DrawingColor color, out int px, out int py)
    {
        color = default;
        px = py = 0;

        if (!GetCursorPos(out var cursor))
            return false;

        px = cursor.X - _originX;
        py = cursor.Y - _originY;
        if (px < 0 || py < 0 || px >= _snapshot.Width || py >= _snapshot.Height)
            return false;

        color = _snapshot.GetPixel(px, py);
        return true;
    }

    // The cursor in this window's device-independent coordinates, for placing
    // the loupe before the first mouse-move event arrives.
    private Point GetCursorInRootDips()
    {
        if (!GetCursorPos(out var cursor))
            return new Point(0, 0);

        var scale = GetDpiForSystem() / 96.0;
        return new Point((cursor.X - _originX) / scale, (cursor.Y - _originY) / scale);
    }

    // Convert a GDI bitmap to a frozen WPF image source, releasing the GDI
    // handle afterwards so it does not leak.
    private static BitmapSource ToBitmapSource(DrawingBitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        _snapshot.Dispose();
        base.OnClosed(e);
    }
}
