using System.Text.Json.Serialization;

namespace PadPath.Models;

public sealed record SelectorResult(
    [property: JsonPropertyName("directoryPath")] string DirectoryPath,
    [property: JsonPropertyName("fullPath")] string FullPath,
    [property: JsonPropertyName("executableName")] string ExecutableName,
    [property: JsonPropertyName("folderName")] string FolderName);
