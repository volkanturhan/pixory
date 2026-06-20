using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Pixory.Models;
using Pixory.Services;

// WinForms is enabled for the tray, so disambiguate the input event args in
// favour of the WPF ones this window uses.
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace Pixory;

/// <summary>
/// The palette popup. It lists recently picked colours and lets the user copy
/// one (Enter or double-click), pin favourites, or start a fresh pick. The
/// application decides what "chosen" means — typically: copy it in the active
/// format and show a brief confirmation.
/// </summary>
public partial class PaletteWindow : Window
{
    private readonly ColorHistory _history;

    // True only during the show sequence. Forcing the window to the foreground
    // can fire a transient Deactivated; while showing we ignore it so the popup
    // does not immediately hide itself.
    private bool _isShowing;

    /// <summary>Raised when the user picks a colour from the palette to copy.</summary>
    public event Action<PickedColor>? ColorChosen;

    /// <summary>Raised when the user asks to start picking a new colour.</summary>
    public event Action? PickRequested;

    public PaletteWindow(ColorHistory history)
    {
        InitializeComponent();

        _history = history;
        PaletteList.ItemsSource = history.Items;
    }

    /// <summary>Shows the popup at the mouse cursor with the first colour selected.</summary>
    public void ShowAsPopup()
    {
        _isShowing = true;

        PositionAtCursor();
        Show();

        // A global hotkey / tray click fires while another app is in front, so a
        // plain Activate() is often refused. Force ourselves to the foreground.
        WindowActivator.ForceToForeground(new WindowInteropHelper(this).Handle);
        Activate();

        SelectFirst();
        FocusList();

        // Re-enable click-away-to-close once the show has fully settled.
        Dispatcher.BeginInvoke(() => _isShowing = false, DispatcherPriority.Background);
    }

    private void OnPickClick(object sender, RoutedEventArgs e)
    {
        Hide();
        PickRequested?.Invoke();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        // Ctrl+P pins or unpins the selected colour.
        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
        {
            PinSelected();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                ChooseSelected();
                e.Handled = true;
                break;
            case Key.Delete:
                DeleteSelected();
                e.Handled = true;
                break;
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
        }

        base.OnPreviewKeyDown(e);
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e) => ChooseSelected();

    // Context-menu actions. The menu inherits the row's data context, so the
    // sender's DataContext is the colour that was right-clicked.
    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PickedColor color })
            _history.TogglePin(color);
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PickedColor color })
            _history.Remove(color);
    }

    private void PinSelected()
    {
        if (PaletteList.SelectedItem is PickedColor color)
            _history.TogglePin(color);
    }

    private void DeleteSelected()
    {
        if (PaletteList.SelectedItem is not PickedColor color)
            return;

        var index = PaletteList.SelectedIndex;
        _history.Remove(color);

        // Keep a sensible item selected after the removal.
        if (PaletteList.Items.Count > 0)
            PaletteList.SelectedIndex = Math.Min(index, PaletteList.Items.Count - 1);
    }

    private void ChooseSelected()
    {
        if (PaletteList.SelectedItem is PickedColor color)
            ColorChosen?.Invoke(color);
    }

    // Clicking outside the popup dismisses it, the way a launcher menu would —
    // but not during the show sequence, which can briefly deactivate us.
    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_isShowing)
            Hide();
    }

    private void SelectFirst()
    {
        PaletteList.SelectedIndex = PaletteList.Items.Count > 0 ? 0 : -1;
        if (PaletteList.SelectedItem is not null)
            PaletteList.ScrollIntoView(PaletteList.SelectedItem);
    }

    // Focus the list so the arrow keys drive the selection; if the palette is
    // empty there is nothing to select, so leave focus on the window.
    private void FocusList()
    {
        if (PaletteList.Items.Count > 0)
        {
            PaletteList.Focus();
            if (PaletteList.ItemContainerGenerator.ContainerFromIndex(0) is UIElement item)
                item.Focus();
        }
    }

    /// <summary>
    /// Places the popup near the mouse cursor, kept fully inside the working area
    /// of whichever monitor the cursor is on. Cursor and screen bounds are in
    /// physical pixels, so we scale them to WPF's device-independent units.
    /// </summary>
    private void PositionAtCursor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var work = System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;

        using var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        var scaleX = graphics.DpiX / 96.0;
        var scaleY = graphics.DpiY / 96.0;

        var widthPx = Width * scaleX;
        var heightPx = Height * scaleY;

        var x = Math.Clamp(cursor.X, work.Left, Math.Max(work.Left, work.Right - (int)widthPx));
        var y = Math.Clamp(cursor.Y, work.Top, Math.Max(work.Top, work.Bottom - (int)heightPx));

        Left = x / scaleX;
        Top = y / scaleY;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing (e.g. Alt+F4) just hides the popup; the app keeps running and
        // is shut down from the tray's Quit command.
        e.Cancel = true;
        Hide();

        base.OnClosing(e);
    }
}
