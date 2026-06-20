# Pixory

**English | [Türkçe](README.tr.md)**

A lightweight Windows screen colour picker.

Pixory lives quietly in your system tray. Press a hotkey, a magnifier follows
your cursor so you can line up the exact pixel, click — and the colour is copied
to your clipboard in whatever format you like (HEX, RGB, or HSL). Every colour
you pick is kept in a small palette you can reopen, copy from again, or pin.

<p align="center">
  <img src="docs/screenshot.png" alt="Pixory's magnifier loupe and hex readout" width="360" />
</p>

## Features

- **Pick any pixel** — global hotkey (`Ctrl + Shift + C`) opens a full-screen
  picker with a magnifier loupe and a live hex readout.
- **Pixel-accurate** — samples from a frozen snapshot of the desktop, correct
  even on high-DPI and multi-monitor setups.
- **Copy in your format** — HEX, RGB, or HSL, switchable from the tray.
- **Palette** — every picked colour is kept; reopen it to copy again.
- **Favourites** — pin the colours you reuse; they stay on top and are never dropped.
- **Survives restarts** — your palette (and pins) are saved and restored.
- **Start with Windows** — optional, toggled from the tray menu.
- **English & Turkish** — switch the interface language from the tray.
- **Private by design** — everything stays on your machine; nothing is uploaded.

## Run it

Pixory isn't published as a prebuilt download yet, so for now you run it from
source. You'll need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
(the SDK, not just the runtime) on Windows.

```bash
git clone https://github.com/volkanturhan/Pixory.git
cd Pixory
dotnet run --project Pixory/Pixory.csproj
```

Pixory starts quietly in the system tray — **no window pops up**. That's normal;
press the hotkey or click the tray icon to use it (see **How to use** below).

## How to use

1. Launch Pixory — it starts quietly in the system tray.
2. Press **`Ctrl + Shift + C`** (or pick **Pick a colour** from the tray) to open
   the full-screen picker.
3. Move the mouse — the loupe magnifies the pixels under the cursor and shows the
   colour's hex value. **Click** to pick it; **Esc** or right-click cancels.
4. The colour is copied to your clipboard and added to your palette.
5. Open the palette (tray **Open palette**, or double-click a colour) to copy one
   again with **Enter**, pin it with **Ctrl + P** / right-click, or remove it with
   **Del**.

Right-click the tray icon for **Pick a colour**, **Open palette**, **Copy
format** (HEX / RGB / HSL), **Clear palette**, **Start with Windows**, language,
and **Quit**.

## Where your data lives

Your palette is stored locally at `%APPDATA%\Pixory\palette.json` and never
leaves your machine; preferences live next to it in `settings.json`. Use **Clear
palette** in the tray menu to wipe it (pinned colours are kept); pinned items can
be removed individually from the palette.

## Build a shareable exe

Want a standalone `.exe` you can hand to someone without the SDK? Build it
yourself — the output isn't checked into the repo:

```bash
# Builds into dist/ (self-contained Pixory.exe + lite build)
pwsh tools/publish.ps1
```

## Tech

- C# / WPF on .NET 8 (Windows)
- No third-party dependencies

## License

MIT — see [LICENSE](LICENSE).
