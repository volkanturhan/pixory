using System.IO;
using System.Text.Json;

namespace pixory.Services;

/// <summary>
/// Persists small user preferences — the chosen language, the colour format to
/// copy in, and the colour theme — as JSON under %APPDATA%\pixory. Best-effort,
/// like <see cref="PaletteStorage"/>: failures fall back to defaults rather than
/// throwing.
/// </summary>
public sealed class SettingsStore
{
    // Theme is nullable so older settings files (which only had a language and
    // format) still load; a missing value just falls back to the default.
    private sealed record Data(string Language, string Format, string? Theme = null);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public SettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "pixory");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.json");
    }

    /// <summary>Loads the saved language, defaulting to English.</summary>
    public AppLanguage LoadLanguage()
    {
        var data = Read();
        return data is not null && Enum.TryParse<AppLanguage>(data.Language, out var language)
            ? language
            : AppLanguage.English;
    }

    /// <summary>Loads the saved colour format, defaulting to HEX.</summary>
    public ColorFormat LoadFormat()
    {
        var data = Read();
        return data is not null && Enum.TryParse<ColorFormat>(data.Format, out var format)
            ? format
            : ColorFormat.Hex;
    }

    /// <summary>Loads the saved theme, defaulting to System.</summary>
    public AppTheme LoadTheme()
    {
        var data = Read();
        return data?.Theme is not null && Enum.TryParse<AppTheme>(data.Theme, out var theme)
            ? theme
            : AppTheme.System;
    }

    /// <summary>Saves language and format together, preserving the stored theme.</summary>
    public void Save(AppLanguage language, ColorFormat format)
        => Write(new Data(language.ToString(), format.ToString(), Read()?.Theme));

    /// <summary>Saves the chosen theme, preserving the stored language and format.</summary>
    public void SaveTheme(AppTheme theme)
    {
        var current = Read();
        Write(new Data(
            current?.Language ?? AppLanguage.English.ToString(),
            current?.Format ?? ColorFormat.Hex.ToString(),
            theme.ToString()));
    }

    private Data? Read()
    {
        try
        {
            return File.Exists(_filePath)
                ? JsonSerializer.Deserialize<Data>(File.ReadAllText(_filePath))
                : null;
        }
        catch
        {
            return null;
        }
    }

    private void Write(Data data)
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch
        {
            // Best-effort; a lost preference is not worth crashing over.
        }
    }
}
