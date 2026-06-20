using System.Windows;

// Enabling WinForms (for the tray icon) pulls System.Windows.Forms.Application
// into scope too, so spell out that we mean the WPF one.
using Application = System.Windows.Application;

namespace Pixory;

/// <summary>
/// Application entry point. Wired up fully in a later step; for now this is the
/// minimal shell so the project builds and runs.
/// </summary>
public partial class App : Application
{
}
