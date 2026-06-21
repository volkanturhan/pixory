using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace pixory.Services;

/// <summary>
/// Registers a system-wide hotkey (Ctrl + Shift + C) and raises
/// <see cref="Pressed"/> whenever it is used, from any application.
///
/// It owns an invisible message-only window because Windows delivers the
/// <c>WM_HOTKEY</c> notification as a window message.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    // Arbitrary id used to identify our single registration in WM_HOTKEY.
    private const int HotkeyId = 1;

    // Virtual-key code for the "C" key.
    private const uint VirtualKeyC = 0x43;

    [Flags]
    private enum Modifiers : uint
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        // Stops the hotkey from auto-repeating while the keys are held down.
        NoRepeat = 0x4000,
    }

    private static readonly IntPtr HWND_MESSAGE = new(-3);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    private readonly HwndSource _source;
    private bool _disposed;

    /// <summary>Raised when the registered hotkey is pressed.</summary>
    public event Action? Pressed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("pixoryHotkey")
        {
            ParentWindow = HWND_MESSAGE,
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        var modifiers = (uint)(Modifiers.Control | Modifiers.Shift | Modifiers.NoRepeat);
        if (!RegisterHotKey(_source.Handle, HotkeyId, modifiers, VirtualKeyC))
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Could not register the Ctrl+Shift+C hotkey; another app may already own it.");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        UnregisterHotKey(_source.Handle, HotkeyId);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
