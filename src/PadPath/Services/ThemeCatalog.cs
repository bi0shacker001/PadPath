using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PadPath.Models;

namespace PadPath.Services;

public static class ThemeCatalog
{
    public static IReadOnlyList<string> Appearances { get; } = ["System", "Lighter", "Light", "Dark", "Darker", "High Contrast"];
    public static IReadOnlyList<ThemeDefinition> All { get; } =
    [
        new("Midnight Mint", "#72E6C5", "#43BFA8", "#A8FFF0", "#087D68", "#0A6657", "#159D84"),
        new("Dreams", "#D8BCFF", "#A987D8", "#F0DDFF", "#7042A3", "#8B5DB8", "#523078", "#17131F", "#251D31", "#F7F2FB", "#FFFFFF", "#FFFFFF", "#F4ECFA", "#09070E", "#17111F"),
        new("Ocean", "#70C7FF", "#4F8BFF", "#80E0D1", "#0969A8", "#3155B7", "#08796C"),
        new("Sunset", "#FFAB78", "#F47794", "#FFD166", "#A84416", "#B83255", "#8A5A00"),
        new("Rainbow Pride", "#FFD166", "#63D6A5", "#8DB8FF", "#985900", "#087A54", "#3859A8"),
        new("Trans Pride", "#73D7FA", "#F5A9B8", "#FFFFFF", "#06759D", "#B03E61", "#58606A"),
        new("Bisexual Pride", "#F05AAE", "#9B6BD3", "#668CFF", "#A31568", "#6A389F", "#2854B8"),
        new("Lesbian Pride", "#FF9A70", "#E76A86", "#D9A7E8", "#A53C14", "#B12950", "#7C438D"),
        new("Nonbinary Pride", "#FFF34D", "#C78AE8", "#E9E9ED", "#796E00", "#79429B", "#454750"),
        new("Pan Pride", "#FF5FA2", "#FFD34E", "#4DCCFF", "#A61358", "#7A6500", "#006F9B"),
        new("Ace Pride", "#C28BE8", "#A7A7B0", "#FFFFFF", "#713C98", "#555560", "#30323A"),
        new("Aromantic Pride", "#67D886", "#A6D77A", "#D7D9DA", "#14753A", "#4F761D", "#4F565A")
    ];

    public static void Apply(string? paletteName, string? appearance)
    {
        var palette = All.FirstOrDefault(t => t.Name.Equals(paletteName, StringComparison.OrdinalIgnoreCase)) ?? All[0];
        var resolved = ResolveAppearance(appearance);
        if (resolved == "High Contrast") { ApplyHighContrast(); return; }
        var light = resolved is "Light" or "Lighter";
        var background = resolved switch { "Lighter" => palette.LighterBackground ?? "#FFFFFF", "Light" => palette.LightBackground ?? "#F3F5F8", "Darker" => palette.DarkerBackground ?? "#05070B", _ => palette.DarkBackground ?? "#0F1218" };
        var panel = resolved switch { "Lighter" => palette.LighterPanel ?? "#F1F4F8", "Light" => palette.LightPanel ?? "#FFFFFF", "Darker" => palette.DarkerPanel ?? "#11151C", _ => palette.DarkPanel ?? "#1B2029" };
        Set("BackgroundBrush", background); Set("PanelBrush", panel);
        Set("TextBrush", light ? "#171A21" : "#F7F9FC"); Set("MutedBrush", resolved == "Darker" ? "#C9D1DE" : light ? "#4D5868" : "#BBC5D4");
        Set("SelectionBrush", resolved switch { "Lighter" => "#D8E1EC", "Light" => "#E1E7EF", "Darker" => "#1E2632", _ => "#29313D" }); Set("SelectionTextBrush", light ? "#111318" : "#FFFFFF");
        Set("BorderBrush", resolved switch { "Lighter" => "#59677A", "Light" => "#657286", "Darker" => "#74849D", _ => "#61718A" }); Set("BadgeBrush", resolved switch { "Lighter" => "#E2E8F0", "Light" => "#E7ECF3", "Darker" => "#202834", _ => "#2B3441" });
        var accent = light ? palette.LightAccent : palette.DarkAccent;
        Set("AccentBrush", accent);
        Set("AccentTextBrush", RelativeLuminance((Color)ColorConverter.ConvertFromString(accent)) > .45 ? "#101318" : "#FFFFFF");
        Set("AccentSecondaryBrush", light ? palette.LightAccentSecondary : palette.DarkAccentSecondary);
        Set("AccentTertiaryBrush", light ? palette.LightAccentTertiary : palette.DarkAccentTertiary);
        var secondary = light ? palette.LightAccentSecondary : palette.DarkAccentSecondary;
        Set("DockTextBrush", RelativeLuminance((Color)ColorConverter.ConvertFromString(secondary)) > .45 ? "#101318" : "#FFFFFF");
        SetPalette(light ? palette.LightAccent : palette.DarkAccent, light ? palette.LightAccentSecondary : palette.DarkAccentSecondary, light ? palette.LightAccentTertiary : palette.DarkAccentTertiary);
    }

    private static string ResolveAppearance(string? appearance)
    {
        if (SystemParameters.HighContrast || appearance == "High Contrast") return "High Contrast";
        if (appearance is "Lighter" or "Light" or "Dark" or "Darker") return appearance;
        try
        {
            var value = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme");
            return value is int i && i != 0 ? "Light" : "Dark";
        }
        catch { return "Dark"; }
    }

    private static void ApplyHighContrast()
    {
        Set("BackgroundBrush", SystemColors.WindowColor); Set("PanelBrush", SystemColors.WindowColor);
        Set("TextBrush", SystemColors.WindowTextColor); Set("MutedBrush", SystemColors.WindowTextColor);
        Set("SelectionBrush", SystemColors.HighlightColor); Set("SelectionTextBrush", SystemColors.HighlightTextColor);
        Set("BorderBrush", SystemColors.WindowTextColor); Set("BadgeBrush", SystemColors.WindowColor);
        Set("AccentBrush", SystemColors.HotTrackColor); Set("AccentSecondaryBrush", SystemColors.HighlightColor); Set("AccentTertiaryBrush", SystemColors.WindowTextColor);
        Set("AccentTextBrush", SystemColors.HighlightTextColor); Set("DockTextBrush", SystemColors.HighlightTextColor);
        Application.Current.Resources["PaletteBrush"] = SystemColors.HighlightBrush;
    }

    private static void Set(string key, string value) => Set(key, (Color)ColorConverter.ConvertFromString(value));
    private static void Set(string key, Color value) => Application.Current.Resources[key] = new SolidColorBrush(value);
    private static void SetPalette(string first, string second, string third)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(first), 0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(second), .5));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(third), 1));
        Application.Current.Resources["PaletteBrush"] = brush;
    }
    private static double RelativeLuminance(Color c)
    {
        static double Channel(byte b) { var v = b / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        return .2126 * Channel(c.R) + .7152 * Channel(c.G) + .0722 * Channel(c.B);
    }
}
