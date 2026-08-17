using System.Text.Json;
using PadPath.Models;

namespace PadPath.Services;

public static class ConfigService
{
    public static string UserDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PadPath");
    private static string LegacyUserDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HandheldLauncher");
    public static string ConfigPath { get; private set; } = Path.Combine(UserDirectory, "config.json");
    public static string StatePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PadPath", "state.json");
    public static bool NeedsSetup { get; private set; }

    public static LauncherConfig Load(string[] args)
    {
        var configArg = Array.FindIndex(args, a => a.Equals("--config", StringComparison.OrdinalIgnoreCase));
        if (configArg >= 0 && configArg + 1 < args.Length) ConfigPath = Path.GetFullPath(args[configArg + 1]);
        else if (!File.Exists(ConfigPath))
        {
            MigrateLegacyConfiguration();
            var portablePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(portablePath)) ConfigPath = portablePath;
        }
        if (!File.Exists(ConfigPath))
        {
            NeedsSetup = true;
            return CreateDefault();
        }

        var config = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(ConfigPath), JsonOptions())
            ?? throw new InvalidDataException("Configuration is empty.");
        config.AllowedExtensions = config.AllowedExtensions.Select(NormalizeExtension).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        config.Roots = config.Roots.Where(r => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.Path)).ToList();
        if (config.Roots.Count == 0) throw new InvalidDataException("Configure at least one root folder.");
        return config;
    }

    private static void MigrateLegacyConfiguration()
    {
        var legacyConfig = Path.Combine(LegacyUserDirectory, "config.json");
        if (!File.Exists(legacyConfig) || File.Exists(ConfigPath)) return;
        Directory.CreateDirectory(UserDirectory);
        File.Copy(legacyConfig, ConfigPath);
        var legacyState = Path.Combine(LegacyUserDirectory, "state.json");
        if (File.Exists(legacyState)) File.Copy(legacyState, StatePath, overwrite: false);
    }

    public static void Save(LauncherConfig config)
    {
        Directory.CreateDirectory(UserDirectory);
        ConfigPath = Path.Combine(UserDirectory, "config.json");
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions()));
        NeedsSetup = false;
    }

    private static LauncherConfig CreateDefault()
    {
        var games = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Games");
        return new LauncherConfig
        {
            Roots = [new RootConfig { Name = "Games", Path = games }]
        };
    }

    public static string? LoadLastFolder()
    {
        try
        {
            if (!File.Exists(StatePath)) return null;
            return JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath), JsonOptions())?.LastFolder;
        }
        catch { return null; }
    }

    public static void SaveLastFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(StatePath, JsonSerializer.Serialize(new State(path), JsonOptions()));
        }
        catch { /* State persistence must never prevent launching. */ }
    }

    private static string NormalizeExtension(string value) => value.StartsWith('.') ? value.ToLowerInvariant() : "." + value.ToLowerInvariant();
    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true, WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip };
    private sealed record State(string LastFolder);
}
