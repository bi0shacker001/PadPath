using System.Diagnostics;

namespace PadPath.Services;

public static class TargetLauncher
{
    public static Process Launch(string path)
    {
        if (OperatingSystem.IsMacOS() && path.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            var open = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false };
            open.ArgumentList.Add("-W");
            open.ArgumentList.Add(path);
            return Process.Start(open) ?? throw new InvalidOperationException($"macOS could not open {Path.GetFileName(path)}.");
        }
        var info = new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path)!,
            UseShellExecute = true
        };
        return Process.Start(info) ?? throw new InvalidOperationException($"The operating system could not launch {Path.GetFileName(path)}.");
    }
}
