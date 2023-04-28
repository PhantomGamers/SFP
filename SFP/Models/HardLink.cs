namespace SFP.Models;

public static class HardLink
{
    private static readonly Dictionary<string, string> s_hardLinks = new();

    public static FileInfo GetLink(FileInfo file) => !OperatingSystem.IsWindows()
        ? file
        : s_hardLinks.TryGetValue(file.FullName, out string? linkPath)
            ? new FileInfo(linkPath)
            : CreateHardLink(file);

    private static FileInfo CreateHardLink(FileInfo file)
    {
        string linkPath = Path.Join(file.DirectoryName, "SFP");
        _ = Directory.CreateDirectory(linkPath);
        string linkName = Path.Join(linkPath, file.Name);

        if (File.Exists(linkName) && !s_hardLinks.ContainsValue(linkName))
        {
            s_hardLinks.Add(file.FullName, linkName);
            return new FileInfo(linkName);
        }

        _ = Native.CreateHardLinkW(linkName, file.FullName, IntPtr.Zero);
        // If this function runs in parallel for the same file, another instance of this method might add the link first.
        // This would cause an exception, but we can ignore it because the file will exist
        // TODO: Actually make this method thread-safe
        _ = s_hardLinks.TryAdd(file.FullName, linkName);
        return new FileInfo(linkName);
    }

    private static bool RemoveHardLink(string filePath)
    {
        if (!s_hardLinks.TryGetValue(filePath, out string? linkPath))
        {
            return false;
        }

        try
        {
            File.Delete(linkPath);
        }
        catch (Exception e)
        {
            Log.Logger.Warn($"Could not delete {linkPath} which links to {filePath}");
            Log.Logger.Error(e);
            return false;
        }

        _ = s_hardLinks.Remove(filePath);
        return true;
    }

    public static bool RemoveAllHardLinks() =>
        s_hardLinks.Keys.Aggregate(true, (current, file) => current && RemoveHardLink(file));
}
