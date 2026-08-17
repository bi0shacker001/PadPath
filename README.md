# Handheld Launcher

A controller-first Windows file browser that launches games without building a library first. Add one launcher to Steam, browse any configured game folder over Steam Link, choose an executable, and the launcher gets out of the way.

The MVP is designed for small 16:9 handheld screens such as the Retroid Pocket 5: one pane, large rows, strong focus outlines, short breadcrumbs, and persistent button prompts.

## What works

- Multiple named starting folders, switchable without leaving the browser
- Full navigation with an Xbox-compatible controller through Windows XInput
- Keyboard and mouse fallback
- Configurable launchable extensions and hidden/system-file filtering
- Optional confirmation before launch
- Exit immediately after spawning the game (default), or stay minimized
- Remembers the last visited folder within a configured root
- Full-screen mode with no desktop chrome
- Portable, self-contained single-file Windows build

No games need to be scanned, imported, or manually registered.

## Controls

| Controller | Keyboard | Action |
|---|---|---|
| D-pad up/down | Arrow keys | Move one row |
| D-pad left/right | Page Up/Down | Move five rows |
| A | Enter / double-click | Open folder or launch file |
| B | Backspace / Escape | Parent folder |
| Menu | Tab | Next configured root |
| View | — | Close launcher |
| — | F11 | Toggle window chrome |

Steam Input should expose the remote controller as an Xbox/XInput controller. The first connected controller is used.

## Quick start

### Installer (recommended)

Run `HandheldLauncher-Setup.exe`. When installation finishes, setup opens automatically:

1. Add one or more folders containing games.
2. Choose launch behavior.
3. Select **Add to Steam** (Steam must be completely closed).
4. Save and continue.

That is the entire setup. The Steam entry opens the folder browser; individual games never need to be imported. A backup is created before the Steam shortcuts file is changed.

### Build from source

Requirements: Windows 10/11 and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
.\scripts\publish.ps1
```

The portable build is written to `dist`. Copy `config.example.json` to `config.json`, edit the roots, and run `HandheldLauncher.exe`.

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
2. Browse to `HandheldLauncher.exe` and add it.
3. In its Steam properties, rename it and optionally add artwork.
4. Launch it once locally to validate the configured roots, then use it through Steam Link.

The launched game inherits normal Windows shell launch behavior. Because the browser exits after starting it, Steam Link can follow the child game rather than leaving the launcher in front.

## Configuration

Normal installations store configuration in `%LOCALAPPDATA%\HandheldLauncher\config.json`. A portable `config.json` beside the executable takes precedence. Alternatively, pass a different file:

```text
HandheldLauncher.exe --config "D:\Launcher\living-room.json"
```

Environment variables such as `%USERPROFILE%` are supported in root paths.

| Setting | Default | Meaning |
|---|---:|---|
| `fullscreen` | `true` | Borderless full-screen UI |
| `showHidden` / `showSystem` | `false` | Include files with those Windows attributes |
| `allowedExtensions` | exe, bat, cmd, lnk | Files displayed as launchable |
| `confirmBeforeLaunch` | `true` | Ask before spawning a selected file |
| `exitAfterLaunch` | `true` | Exit after a successful spawn; otherwise minimize |
| `rememberLastFolder` | `true` | Resume the last folder when it still belongs to a configured root |
| `roots` | required | Named folders available at the top of the screen |

The saved last-folder state lives in `%LOCALAPPDATA%\HandheldLauncher\state.json`. The launcher never navigates above the active configured root.

## Steam and Playnite integration

The setup screen can add the launcher itself to the most recently used local Steam profile. Steam must be closed to avoid it overwriting `shortcuts.vdf`; the launcher creates a `.handheld-launcher.bak` backup first. This creates one library entry for the browser, never one entry per game.

Per-game Steam and Playnite export remains modular and disabled. A future explicit export screen can produce reviewable shortcuts and artwork without making browsing or launching depend on a network service.

## Project layout

```text
src/HandheldLauncher/
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

MIT
