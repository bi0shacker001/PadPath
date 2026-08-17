using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace HandheldLauncher.Services;

public static class SteamShortcutService
{
    public static string AddLauncher(string executablePath)
    {
        if (Process.GetProcessesByName("steam").Length > 0)
            throw new InvalidOperationException("Close Steam completely, then try Add to Steam again. Steam can overwrite shortcuts while it is running.");

        var steamPath = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string;
        if (string.IsNullOrWhiteSpace(steamPath)) throw new InvalidOperationException("Steam was not found for this Windows user.");
        var userData = Path.Combine(steamPath, "userdata");
        var configs = Directory.Exists(userData)
            ? Directory.EnumerateDirectories(userData).Where(d => long.TryParse(Path.GetFileName(d), out _)).Select(d => Path.Combine(d, "config")).Where(Directory.Exists).ToList()
            : [];
        if (configs.Count == 0) throw new InvalidOperationException("No Steam user profile was found. Sign in to Steam once, close it, and try again.");

        var configDir = configs.OrderByDescending(Directory.GetLastWriteTimeUtc).First();
        var shortcutPath = Path.Combine(configDir, "shortcuts.vdf");
        var name = "Handheld Launcher";
        var existing = File.Exists(shortcutPath) ? File.ReadAllBytes(shortcutPath) : CreateEmpty();
        if (ContainsShortcut(existing, executablePath)) return "Handheld Launcher is already in this Steam library.";
        var count = CountTopLevelEntries(existing);
        var entry = BuildEntry(count, name, executablePath);
        if (existing.Length == 0 || existing[^1] != 0x08) throw new InvalidDataException("Steam shortcuts file has an unexpected format; it was not changed.");

        var backup = shortcutPath + ".handheld-launcher.bak";
        if (File.Exists(shortcutPath)) File.Copy(shortcutPath, backup, true);
        using var output = new MemoryStream();
        output.Write(existing, 0, existing.Length - 1);
        output.Write(entry);
        output.WriteByte(0x08);
        File.WriteAllBytes(shortcutPath, output.ToArray());
        return "Added to Steam. Start Steam and look under Non-Steam Games.";
    }

    private static byte[] CreateEmpty()
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x00); WriteString(stream, "shortcuts"); stream.WriteByte(0x08);
        return stream.ToArray();
    }

    private static byte[] BuildEntry(int index, string name, string executablePath)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x00); WriteString(stream, index.ToString());
        WriteInt(stream, "appid", unchecked((int)(Crc32(Encoding.UTF8.GetBytes($"\"{executablePath}\"{name}")) | 0x80000000u)));
        WriteText(stream, "appname", name);
        WriteText(stream, "exe", $"\"{executablePath}\"");
        WriteText(stream, "StartDir", $"\"{Path.GetDirectoryName(executablePath)}\"");
        WriteText(stream, "icon", executablePath);
        WriteText(stream, "ShortcutPath", ""); WriteText(stream, "LaunchOptions", "");
        WriteInt(stream, "IsHidden", 0); WriteInt(stream, "AllowDesktopConfig", 1); WriteInt(stream, "AllowOverlay", 1);
        WriteInt(stream, "OpenVR", 0); WriteInt(stream, "Devkit", 0); WriteText(stream, "DevkitGameID", "");
        WriteInt(stream, "DevkitOverrideAppID", 0); WriteInt(stream, "LastPlayTime", 0); WriteText(stream, "FlatpakAppID", "");
        stream.WriteByte(0x00); WriteString(stream, "tags"); stream.WriteByte(0x08);
        stream.WriteByte(0x08);
        return stream.ToArray();
    }

    private static int CountTopLevelEntries(byte[] bytes)
    {
        var position = 0;
        if (bytes.Length < 3 || bytes[position++] != 0x00) return 0;
        ReadString(bytes, ref position);
        var count = 0;
        while (position < bytes.Length && bytes[position] != 0x08)
        {
            var type = bytes[position++]; ReadString(bytes, ref position);
            SkipValue(bytes, ref position, type); count++;
        }
        return count;
    }

    private static void SkipValue(byte[] bytes, ref int position, byte type)
    {
        if (type == 0x00)
        {
            while (position < bytes.Length && bytes[position] != 0x08)
            {
                var childType = bytes[position++]; ReadString(bytes, ref position); SkipValue(bytes, ref position, childType);
            }
            if (position < bytes.Length) position++;
        }
        else if (type == 0x01) ReadString(bytes, ref position);
        else if (type is 0x02 or 0x03 or 0x04) position += 4;
        else if (type == 0x05) position += 8;
        else if (type == 0x06) position += 4;
        else if (type == 0x07) position += 8;
        else if (type == 0x0A) position += 8;
    }

    private static bool ContainsShortcut(byte[] bytes, string path) => Encoding.UTF8.GetString(bytes).Contains(path, StringComparison.OrdinalIgnoreCase);
    private static string ReadString(byte[] bytes, ref int position) { var start = position; while (position < bytes.Length && bytes[position] != 0) position++; var result = Encoding.UTF8.GetString(bytes, start, position - start); position++; return result; }
    private static void WriteText(Stream s, string key, string value) { s.WriteByte(0x01); WriteString(s, key); WriteString(s, value); }
    private static void WriteInt(Stream s, string key, int value) { s.WriteByte(0x02); WriteString(s, key); s.Write(BitConverter.GetBytes(value)); }
    private static void WriteString(Stream s, string value) { s.Write(Encoding.UTF8.GetBytes(value)); s.WriteByte(0); }
    private static uint Crc32(byte[] data) { uint crc = 0xffffffff; foreach (var b in data) { crc ^= b; for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1)); } return ~crc; }
}
