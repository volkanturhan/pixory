using System.IO;
using System.Text.Json;
using pixory.Models;

namespace pixory.Services;

/// <summary>
/// Persists the picked-colour palette to disk so it survives restarts, storing
/// it as JSON under %APPDATA%\pixory. All operations are best-effort: a missing
/// or corrupt file simply yields an empty palette, and a failed save is
/// swallowed rather than allowed to crash the app.
/// </summary>
public sealed class PaletteStorage
{
    // The shape we actually serialise — a plain, versionable record rather than
    // the live model type.
    private sealed record StoredColor(byte R, byte G, byte B, DateTime PickedAt, bool IsPinned);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public PaletteStorage()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "pixory");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "palette.json");
    }

    /// <summary>Loads the saved palette, or an empty list if there is none.</summary>
    public IReadOnlyList<PickedColor> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<PickedColor>();

            var stored = JsonSerializer.Deserialize<List<StoredColor>>(File.ReadAllText(_filePath));
            if (stored is null)
                return Array.Empty<PickedColor>();

            return stored
                .Select(s => new PickedColor(s.R, s.G, s.B, s.PickedAt, s.IsPinned))
                .ToList();
        }
        catch
        {
            // Corrupt or unreadable file: start fresh rather than fail to launch.
            return Array.Empty<PickedColor>();
        }
    }

    /// <summary>Writes the current palette to disk.</summary>
    public void Save(IEnumerable<PickedColor> colors)
    {
        try
        {
            var stored = colors
                .Select(c => new StoredColor(c.R, c.G, c.B, c.PickedAt, c.IsPinned))
                .ToList();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(stored, JsonOptions));
        }
        catch
        {
            // Best-effort persistence; losing a save is preferable to crashing.
        }
    }
}
