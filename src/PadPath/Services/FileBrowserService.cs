using PadPath.Models;

namespace PadPath.Services;

public sealed class FileBrowserService(LauncherConfig config)
{
    public IReadOnlyList<BrowserItem> ReadDirectory(string directory, string root)
    {
        var items = new List<BrowserItem>();
        if (!PathsEqual(directory, root)) items.Add(new BrowserItem("Up one folder", Directory.GetParent(directory)?.FullName ?? root, true, true));

        try
        {
            foreach (var path in Directory.EnumerateDirectories(directory).OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
            {
                if (IsVisible(path)) items.Add(new BrowserItem(Path.GetFileName(path), path, true));
            }
            foreach (var path in Directory.EnumerateFiles(directory).OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
            {
                if (IsVisible(path) && config.AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    items.Add(new BrowserItem(Path.GetFileName(path), path, false));
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        return items;
    }

    public static bool IsWithinRoot(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsVisible(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            if (!config.ShowHidden && attrs.HasFlag(FileAttributes.Hidden)) return false;
            if (!config.ShowSystem && attrs.HasFlag(FileAttributes.System)) return false;
            return true;
        }
        catch { return false; }
    }

    private static bool PathsEqual(string a, string b) => string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
}
