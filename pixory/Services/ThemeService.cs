using System.Windows;
using Microsoft.Win32;

// WinForms is enabled for the tray, so spell out the WPF Application.
using Application = System.Windows.Application;

namespace pixory.Services;

public enum AppTheme
{
    System,
    Dark,
    Light,
}

/// <summary>
/// Applies the chosen colour theme by swapping a brush <see cref="ResourceDictionary"/>
/// (Themes/Dark.xaml or Themes/Light.xaml) into the application's merged
/// dictionaries. The UI references those brushes with <c>DynamicResource</c>, so
/// switching theme updates everything live. "System" follows the Windows
/// app-theme setting.
/// </summary>
public static class ThemeService
{
    private static ResourceDictionary? _current;

    /// <summary>The user's chosen setting (System / Dark / Light).</summary>
    public static AppTheme Theme { get; private set; } = AppTheme.System;

    /// <summary>Raised after the theme is applied.</summary>
    public static event Action? Changed;

    /// <summary>Applies a theme, resolving "System" to the current Windows setting.</summary>
    public static void Apply(AppTheme theme)
    {
        Theme = theme;

        var dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDark(),
        };

        var name = dark ? "Dark" : "Light";
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/pixory;component/Themes/{name}.xaml", UriKind.Relative),
        };

        var merged = Application.Current.Resources.MergedDictionaries;
        if (_current is not null)
            merged.Remove(_current);
        merged.Add(dictionary);
        _current = dictionary;

        Changed?.Invoke();
    }

    // Reads the Windows "apps use light theme" flag (0 = dark). Defaults to light.
    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
