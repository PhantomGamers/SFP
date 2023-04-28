#region

using FileWatcherEx;
using SFP.Models.FileSystemWatchers;

#endregion

namespace SFP.Models.ChromeCache.BlockFile;

public static class Patcher
{
    private static readonly DelayedWatcher s_watcher = new(FolderPath, OnPostEviction, GetKey) { Filter = "f_*" };

    private static string FolderPath => Steam.CacheDir;
    public static bool IsActive => s_watcher.IsActive;

    public static async Task<bool> PatchFile(FileInfo file, bool alertOnPatched = false)
    {
        if (!file.Exists)
        {
            Log.Logger.Warn($"{file.FullName} does not exist");
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(file.FullName);
        }
        catch (IOException)
        {
            Log.Logger.Warn($"Unable to read file {file.FullName}. Please shutdown Steam and try again.");
            return false;
        }

        (bytes, bool patched) = await ChromeCache.Patcher.PatchFriendsCss(bytes, file.Name, alertOnPatched);
        if (!patched)
        {
            return false;
        }

        try
        {
            await File.WriteAllBytesAsync(file.FullName, bytes);
            Log.Logger.Info(
                $"Patched {file.Name}.\nPut your custom css in {Path.Join(Steam.ClientUiDir, "friends.custom.css")}");
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static void Watch() => s_watcher.Start(FolderPath);
    public static void StopWatching() => s_watcher.Stop();

    private static (bool, string?) GetKey(FileChangedEvent e)
    {
        DirectoryInfo? dir = new DirectoryInfo(e.FullPath).Parent;
        return dir == null ? (false, null) : (true, dir.Name);
    }

    private static async void OnPostEviction(FileChangedEvent e)
    {
        DirectoryInfo? dir = new FileInfo(e.FullPath).Directory;

        List<FileInfo> files = Parser.FindCacheFilesWithName(dir!, "friends.css", true);
        bool filesPatched = false;

        foreach (FileInfo? file in files)
        {
            filesPatched |= await PatchFile(file, true);
        }

        if (filesPatched && Properties.Settings.Default.RestartSteamOnPatch)
        {
            Steam.RestartSteam();
        }
    }
}
