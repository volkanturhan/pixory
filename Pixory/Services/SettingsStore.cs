using System.IO;
using System.Text.Json;

namespace Pixory.Services;

/// <summary>
/// Persists small user preferences — the chosen language and the colour format
/// to copy in — as JSON under %APPDATA%\Pixory. Best-effort, like
/// <see cref="PaletteStorage"/>: failures fall back to defaults rather than
/// throwing.
/// </summary>
public sealed class SettingsStore
{
    private sealed record Data(string Language, string Format);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public SettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Pixory");
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

    /// <summary>Saves both preferences together.</summary>
    public void Save(AppLanguage language, ColorFormat format)
    {
        try
        {
            var data = new Data(language.ToString(), format.ToString());
            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch
        {
            // Best-effort; a lost preference is not worth crashing over.
        }
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
}
