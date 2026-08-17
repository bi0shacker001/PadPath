namespace HandheldLauncher.Models;

public sealed class LauncherConfig
{
    public string Title { get; set; } = "Choose a game";
    public bool Fullscreen { get; set; } = true;
    public bool ShowHidden { get; set; }
    public bool ShowSystem { get; set; }
    public bool ConfirmBeforeLaunch { get; set; } = true;
    public bool ExitAfterLaunch { get; set; } = true;
    public bool RememberLastFolder { get; set; } = true;
    public List<string> AllowedExtensions { get; set; } = [".exe", ".bat", ".cmd", ".lnk"];
    public List<RootConfig> Roots { get; set; } = [];
    public IntegrationConfig Integrations { get; set; } = new();
}

public sealed class RootConfig
{
    public string Name { get; set; } = "Games";
    public string Path { get; set; } = @"C:\Games";
}

public sealed class IntegrationConfig
{
    public bool SteamShortcutExport { get; set; }
    public bool PlayniteExport { get; set; }
    public string? ArtworkDirectory { get; set; }
}
