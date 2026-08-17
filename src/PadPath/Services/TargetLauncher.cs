using System.Diagnostics;

namespace PadPath.Services;

public static class TargetLauncher
{
    public static Process Launch(string path)
    {
        var info = new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path)!,
            UseShellExecute = true
        };
        return Process.Start(info) ?? throw new InvalidOperationException($"Windows could not launch {Path.GetFileName(path)}.");
    }
}
