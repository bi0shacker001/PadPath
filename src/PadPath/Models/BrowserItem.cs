namespace PadPath.Models;

public sealed record BrowserItem(string Name, string FullPath, bool IsDirectory, bool IsParent = false)
{
    public string Kind => IsParent ? "BACK" : IsDirectory ? "FOLDER" : Path.GetExtension(Name).TrimStart('.').ToUpperInvariant();
}
