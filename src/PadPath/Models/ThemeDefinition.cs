namespace PadPath.Models;

public sealed record ThemeDefinition(
    string Name,
    string DarkAccent,
    string DarkAccentSecondary,
    string DarkAccentTertiary,
    string LightAccent,
    string LightAccentSecondary,
    string LightAccentTertiary,
    string? DarkBackground = null,
    string? DarkPanel = null,
    string? LightBackground = null,
    string? LightPanel = null,
    string? LighterBackground = null,
    string? LighterPanel = null,
    string? DarkerBackground = null,
    string? DarkerPanel = null)
{
    public string Preview1 => DarkAccent;
    public string Preview2 => DarkAccentSecondary;
    public string Preview3 => DarkAccentTertiary;
}
