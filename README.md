# PadPath

[![CI](https://github.com/bi0shacker001/PadPath/actions/workflows/ci.yml/badge.svg)](https://github.com/bi0shacker001/PadPath/actions/workflows/ci.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

A controller-first Windows file browser that launches games without building a library first. Add one launcher to Steam, browse any configured game folder over Steam Link, choose an executable, and the launcher gets out of the way.

The MVP is designed for small 16:9 handheld screens such as the Retroid Pocket 5: one pane, large rows, strong focus outlines, short breadcrumbs, and persistent button prompts.

## What works

- Multiple named starting folders, switchable without leaving the browser
- Full navigation with an Xbox-compatible controller through Windows XInput
- Keyboard and mouse fallback
- Configurable launchable extensions and hidden/system-file filtering
- Optional confirmation before launch
- Minimize while the selected game runs, then return to the full-screen browser
- Remembers the last visited folder within a configured root
- Full-screen mode with no desktop chrome
- Twelve built-in high-contrast palettes, including light, dark, and queer pride themes
- Portable, self-contained single-file Windows build

No games need to be scanned, imported, or manually registered.

## Selector mode

Run PadPath with `--selector` to choose a launchable file without starting it. PadPath writes one JSON object to standard output and exits with code `0` after a selection. Closing without selecting writes nothing and exits with code `1`.

```powershell
PadPath.exe --selector
```

For `G:\foo\bar\gamename123\game.exe`, the output is:

```json
{
  "directoryPath": "G:\\foo\\bar\\gamename123",
  "fullPath": "G:\\foo\\bar\\gamename123\\game.exe",
  "executableName": "game.exe",
  "folderName": "gamename123"
}
```

The result is emitted as compact single-line JSON so another process can deserialize it reliably.

## Controls

| Controller | Keyboard | Action |
|---|---|---|
| D-pad up/down | Arrow keys | Move one row |
| D-pad left/right | Page Up/Down | Move five rows |
| A | Enter / double-click | Open folder or launch file |
| B | Backspace / Escape | Parent folder |
| Menu | Tab | Next configured root |
| View | Q / Ctrl+Q | Close launcher |
| — | F11 | Toggle window chrome |

Steam Input should expose the remote controller as an Xbox/XInput controller. The first connected controller is used.

## Quick start

### Installer (recommended)

Run `PadPath-Setup.exe`. When installation finishes, setup opens automatically:

1. Add one or more folders containing games.
2. Choose confirmation and visibility options.
3. Select **Add to Steam** (Steam must be completely closed).
4. Save and continue.

That is the entire setup. The Steam entry opens the folder browser; individual games never need to be imported. A backup is created before the Steam shortcuts file is changed.

### Build from source

Requirements: Windows 10/11 and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
.\scripts\publish.ps1
```

The portable build is written to `dist`. Copy `config.example.json` to `config.json`, edit the roots, and run `PadPath.exe`.

To install under your local app-data folder:

```powershell
.\scripts\install.ps1
```

To build the signed-style single-file installer locally, install Inno Setup 6 and run:

```powershell
.\scripts\build-installer.ps1
```

Tagged GitHub builds attach the installer automatically; ordinary pushes also retain the installer and portable app as workflow artifacts.

### Add to Steam

1. In desktop Steam, choose **Games → Add a Non-Steam Game to My Library**.
2. Browse to `PadPath.exe` and add it.
3. In its Steam properties, rename it and optionally add artwork.
4. Launch it once locally to validate the configured roots, then use it through Steam Link.

The launched game inherits normal Windows shell launch behavior. The browser force-minimizes while the game runs but remains alive as Steam's tracked process, preventing Steam Link from disconnecting during startup. When the game closes, the browser returns full-screen. Only the user's Close command ends the launcher session.

## Configuration

Normal installations store configuration in `%LOCALAPPDATA%\PadPath\config.json`. A portable `config.json` beside the executable takes precedence. Alternatively, pass a different file:

```text
PadPath.exe --config "D:\Launcher\living-room.json"
```

Environment variables such as `%USERPROFILE%` are supported in root paths.

| Setting | Default | Meaning |
|---|---:|---|
| `fullscreen` | `true` | Borderless full-screen UI |
| `theme` | `Midnight Mint` | Built-in color palette; choose visually from Settings |
| `appearance` | `System` | `System`, `Lighter`, `Light`, `Dark`, `Darker`, or `High Contrast` |
| `showHidden` / `showSystem` | `false` | Include files with those Windows attributes |
| `allowedExtensions` | exe, bat, cmd, lnk | Files displayed as launchable |
| `confirmBeforeLaunch` | `true` | Ask before spawning a selected file |
| `minimumHandoffSeconds` | `20` | Minimum time to stay minimized if a bootstrap process exits immediately |
| `rememberLastFolder` | `true` | Resume the last folder when it still belongs to a configured root |
| `roots` | required | Named folders available at the top of the screen |

The saved last-folder state lives in `%LOCALAPPDATA%\PadPath\state.json`. The launcher never navigates above the active configured root.

### Included themes

`Midnight Mint`, `Dreams` (dark lavender), `Paper`, `High Contrast`, `Rainbow`, `Trans Pride`, `Bisexual Pride`, `Lesbian Pride`, `Nonbinary Pride`, `Pan Pride`, `Ace Pride`, and `Aromantic Pride`.

## Steam and Playnite integration

The setup screen can add the launcher itself to the most recently used local Steam profile. Steam must be closed to avoid it overwriting `shortcuts.vdf`; the launcher creates a `.padpath.bak` backup first. This creates one library entry for the browser, never one entry per game.

Per-game Steam and Playnite export remains modular and disabled. A future explicit export screen can produce reviewable shortcuts and artwork without making browsing or launching depend on a network service.

## Project layout

```text
src/PadPath/
  Input/           XInput polling and repeat behavior
  Models/          configuration and browser items
  Services/        config, safe-root browsing, and process launch
  MainWindow.*     handheld UI and navigation state
scripts/           portable publish and local install helpers
```

## Current MVP limits

- XInput controller 1 only; no DirectInput/HID fallback yet
- Windows x64 default build (the publish script also accepts `win-arm64`)
- The first-run setup and folder picker are mouse/keyboard-oriented; the launcher itself is controller-first
- No per-file arguments or environment overrides yet
- Steam/Playnite metadata export is a documented extension point, not implemented

## License

PadPath is free software licensed under the GNU General Public License v3.0 only. See [LICENSE](LICENSE).
