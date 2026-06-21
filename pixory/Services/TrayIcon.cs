using System.Drawing;
using System.Windows.Forms;

namespace pixory.Services;

/// <summary>
/// The system-tray presence for pixory. While the app is idle it lives here
/// rather than on the taskbar. The context menu exposes the app's actions and
/// settings; the events below let the application decide what each one does.
///
/// Menu text follows the app language: the menu is built once and its labels
/// are refreshed whenever <see cref="Localization"/> changes.
///
/// Backed by the WinForms <see cref="NotifyIcon"/>, which ships with the .NET
/// SDK so pixory needs no third-party tray library.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon? _icon;

    private readonly ToolStripMenuItem _pickItem = new();
    private readonly ToolStripMenuItem _openItem = new();
    private readonly ToolStripMenuItem _clearItem = new();
    private readonly ToolStripMenuItem _formatItem = new();
    private readonly ToolStripMenuItem _hexItem = new("HEX") { Tag = ColorFormat.Hex };
    private readonly ToolStripMenuItem _rgbItem = new("RGB") { Tag = ColorFormat.Rgb };
    private readonly ToolStripMenuItem _hslItem = new("HSL") { Tag = ColorFormat.Hsl };
    private readonly ToolStripMenuItem _autoStartItem = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _languageItem = new();
    private readonly ToolStripMenuItem _englishItem = new("English");
    private readonly ToolStripMenuItem _turkishItem = new("Türkçe");
    private readonly ToolStripMenuItem _aboutItem = new();
    private readonly ToolStripMenuItem _quitItem = new();

    /// <summary>Raised when the user asks to start picking a colour off the screen.</summary>
    public event Action? PickRequested;

    /// <summary>Raised when the user asks to open the palette window.</summary>
    public event Action? OpenRequested;

    /// <summary>Raised when the user asks to clear the (unpinned) palette.</summary>
    public event Action? ClearRequested;

    /// <summary>Raised when the user chooses a different copy format.</summary>
    public event Action<ColorFormat>? FormatSelected;

    /// <summary>Raised when the user asks to see the About window.</summary>
    public event Action? AboutRequested;

    /// <summary>Raised when the user asks to quit the application.</summary>
    public event Action? QuitRequested;

    public TrayIcon(ColorFormat currentFormat)
    {
        _pickItem.Click += (_, _) => PickRequested?.Invoke();
        _openItem.Click += (_, _) => OpenRequested?.Invoke();
        _clearItem.Click += (_, _) => ClearRequested?.Invoke();
        _autoStartItem.Checked = AutoStart.IsEnabled();
        _autoStartItem.CheckedChanged += (_, _) => AutoStart.SetEnabled(_autoStartItem.Checked);
        _aboutItem.Click += (_, _) => AboutRequested?.Invoke();
        _quitItem.Click += (_, _) => QuitRequested?.Invoke();

        // Format submenu: picking one ticks it and tells the app to switch.
        foreach (var item in new[] { _hexItem, _rgbItem, _hslItem })
            item.Click += OnFormatClicked;
        _formatItem.DropDownItems.AddRange(new ToolStripItem[] { _hexItem, _rgbItem, _hslItem });
        SetFormat(currentFormat);

        _englishItem.Click += (_, _) => Localization.Instance.Language = AppLanguage.English;
        _turkishItem.Click += (_, _) => Localization.Instance.Language = AppLanguage.Turkish;
        _languageItem.DropDownItems.Add(_englishItem);
        _languageItem.DropDownItems.Add(_turkishItem);

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _pickItem,
            _openItem,
            new ToolStripSeparator(),
            _clearItem,
            _formatItem,
            _autoStartItem,
            _languageItem,
            _aboutItem,
            new ToolStripSeparator(),
            _quitItem,
        });

        // The picker action is the headline command, so make it the default
        // (bold) item and the double-click behaviour.
        _pickItem.Font = new Font(menu.Font, System.Drawing.FontStyle.Bold);

        _icon = TryLoadAppIcon();
        _notifyIcon = new NotifyIcon
        {
            // Fall back to a generic icon if ours fails to load — never crash
            // the whole app over a tray icon.
            Icon = _icon ?? SystemIcons.Application,
            Text = "pixory",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.DoubleClick += (_, _) => PickRequested?.Invoke();

        Localization.Instance.LanguageChanged += ApplyLanguage;
        ApplyLanguage();
    }

    /// <summary>Shows a brief "copied" balloon over the tray icon.</summary>
    public void ShowCopied(string value)
    {
        _notifyIcon.ShowBalloonTip(1500, Localization.Instance["Copied"], value, ToolTipIcon.None);
    }

    private void OnFormatClicked(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: ColorFormat format })
        {
            SetFormat(format);
            FormatSelected?.Invoke(format);
        }
    }

    // Tick exactly the active format.
    private void SetFormat(ColorFormat format)
    {
        _hexItem.Checked = format == ColorFormat.Hex;
        _rgbItem.Checked = format == ColorFormat.Rgb;
        _hslItem.Checked = format == ColorFormat.Hsl;
    }

    // Refresh every menu label from the current language and tick the active
    // language entry.
    private void ApplyLanguage()
    {
        var text = Localization.Instance;
        _pickItem.Text = text["TrayPick"];
        _openItem.Text = text["TrayOpen"];
        _clearItem.Text = text["TrayClear"];
        _formatItem.Text = text["TrayFormat"];
        _autoStartItem.Text = text["TrayAutostart"];
        _languageItem.Text = text["TrayLanguage"];
        _aboutItem.Text = text["TrayAbout"];
        _quitItem.Text = text["TrayQuit"];

        _englishItem.Checked = text.Language == AppLanguage.English;
        _turkishItem.Checked = text.Language == AppLanguage.Turkish;
    }

    /// <summary>
    /// Loads the bundled pixory icon at the system's small-icon size so the tray
    /// gets a crisp frame. Returns null on any failure.
    /// </summary>
    private static Icon? TryLoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/pixory.ico");
            using var stream = System.Windows.Application.GetResourceStream(uri).Stream;
            return new Icon(stream, SystemInformation.SmallIconSize);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Localization.Instance.LanguageChanged -= ApplyLanguage;

        // Hide before disposing so the icon disappears immediately instead of
        // lingering in the tray until the user hovers over it.
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }
}
