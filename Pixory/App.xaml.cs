using System.Windows;
using Pixory.Models;
using Pixory.Services;

// Enabling WinForms (for the tray icon) pulls the System.Windows.Forms versions
// of these types into scope too, so spell out that we mean the WPF ones.
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Localization = Pixory.Services.Localization;

namespace Pixory;

/// <summary>
/// Application entry point. Wires together the long-lived pieces of Pixory and
/// runs it as a tray application: there is no window on startup, the app lives in
/// the system tray, and it only exits when the user chooses "Quit".
///
/// The core flow: press Ctrl+Shift+C → a full-screen picker appears → click a
/// pixel → its colour is copied to the clipboard (in the chosen format) and
/// added to the palette.
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private SettingsStore _settings = null!;
    private PaletteStorage _storage = null!;
    private ColorHistory _history = null!;
    private HotkeyService _hotkey = null!;
    private TrayIcon _tray = null!;
    private PaletteWindow _palette = null!;
    private AboutWindow? _aboutWindow;
    private PickerOverlay? _picker;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Only one Pixory should own the global hotkey at a time. If another
        // instance already holds the mutex, bow out quietly.
        _singleInstanceMutex = new Mutex(initiallyOwned: true,
            @"Local\Pixory.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // No visible window means closing a popup must not end the app; shutdown
        // is driven explicitly from the tray's Quit command.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Apply the saved preferences before any UI is built, then persist any
        // later change to language or copy format.
        _settings = new SettingsStore();
        Localization.Instance.Language = _settings.LoadLanguage();
        FormatState.Instance.Format = _settings.LoadFormat();
        Localization.Instance.LanguageChanged += SavePreferences;
        FormatState.Instance.Changed += SavePreferences;

        // Restore the saved palette, then keep persisting it as it changes.
        _storage = new PaletteStorage();
        _history = new ColorHistory();
        _history.Initialize(_storage.Load());
        _history.Changed += () => _storage.Save(_history.Items);

        // The palette popup is created once and reused; it hides instead of closing.
        _palette = new PaletteWindow(_history);
        _palette.ColorChosen += OnColorChosen;
        _palette.PickRequested += StartPick;

        // Ctrl+Shift+C starts a pick from anywhere.
        _hotkey = new HotkeyService();
        _hotkey.Pressed += StartPick;

        _tray = new TrayIcon(FormatState.Instance.Format);
        _tray.PickRequested += StartPick;
        _tray.OpenRequested += ShowPalette;
        _tray.ClearRequested += _history.ClearUnpinned;
        _tray.FormatSelected += format => FormatState.Instance.Format = format;
        _tray.AboutRequested += ShowAbout;
        _tray.QuitRequested += Shutdown;
    }

    private void SavePreferences()
        => _settings.Save(Localization.Instance.Language, FormatState.Instance.Format);

    /// <summary>Opens the full-screen picker, unless one is already showing.</summary>
    private void StartPick()
    {
        if (_picker is not null)
            return;

        // Get the popup out of the way so it is not captured in the snapshot.
        _palette.Hide();

        _picker = new PickerOverlay();
        _picker.Picked += OnPicked;
        _picker.Closed += (_, _) => _picker = null;
        _picker.Show();
    }

    private void OnPicked(byte r, byte g, byte b)
    {
        // Record it (promoting a duplicate) and copy it in the active format.
        _history.Add(r, g, b);
        CopyToClipboard(ColorFormatting.Format(r, g, b, FormatState.Instance.Format));
    }

    private void OnColorChosen(PickedColor color)
    {
        _palette.Hide();
        CopyToClipboard(ColorFormatting.Format(color.R, color.G, color.B, FormatState.Instance.Format));
    }

    // Put the value on the clipboard and confirm with a brief tray balloon.
    // Clipboard access occasionally fails when another app holds it open, so
    // this is best-effort rather than fatal.
    private void CopyToClipboard(string value)
    {
        try
        {
            Clipboard.SetText(value);
            _tray.ShowCopied(value);
        }
        catch
        {
            // Ignore a transient clipboard failure rather than crash.
        }
    }

    private void ShowPalette() => _palette.ShowAsPopup();

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
        _hotkey?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
