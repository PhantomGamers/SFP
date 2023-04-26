#region

using FileWatcherEx;
using SFP.Models.FileSystemWatchers;

#endregion

namespace SFP.Models;

public static class LocalFile
{
    public const string PatchedText = "/*patched*/\n";
    private const string OriginalText = "/*original*/\n";

    private static readonly DelayedWatcher s_localWatcher =
        new(LocalFolderPath, OnLocalPostEviction, GetKey) { Filter = "friends.css" };

    private static readonly DelayedWatcher s_libraryWatcher =
        new(LibraryFolderPath, OnLibraryPostEviction, GetKey) { Filter = "*.css" };

    private static string LocalFolderPath => Steam.ClientUiCssDir;

    private static string LibraryFolderPath => Steam.SteamUiCssDir;
    public static bool IsLocalActive => s_localWatcher.IsActive;
    public static bool IsLibraryActive => s_libraryWatcher.IsActive;

    public static async Task<bool> Patch(FileInfo? file, string? overrideName = null, string? uiDir = null,
        bool alertOnPatched = false)
    {
        if (file is null)
        {
            Log.Logger.Error("Library file does not exist. Start Steam and try again");
            return false;
        }

        string contents;
        try
        {
            contents = await File.ReadAllTextAsync(file.FullName);
        }
        catch (IOException)
        {
            Log.Logger.Warn($"Unable to read file {file.FullName}. Please shutdown Steam and try again.");
            return false;
        }

        if (contents.StartsWith(OriginalText))
        {
            // We are looking at an original file!
            return false;
        }

        if (contents.StartsWith(PatchedText))
        {
            // File is already patched
            if (!alertOnPatched)
            {
                Log.Logger.Info($"{file.Name} is already patched.");
            }

            return false;
        }

        if (!alertOnPatched)
        {
            Log.Logger.Info($"Patching file {file.Name}");
        }

        FileInfo originalFile =
            new(
                $"{Path.Join(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName))}.original{Path.GetExtension(file.FullName)}");

        FileInfo customFile = overrideName != null
            ? new FileInfo(Path.Join(file.DirectoryName, overrideName))
            : new FileInfo(
                $"{Path.Join(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName))}.custom{Path.GetExtension(file.FullName)}");
        try
        {
            await File.WriteAllTextAsync(originalFile.FullName, string.Concat(OriginalText, contents));
        }
        catch (IOException)
        {
            Log.Logger.Warn($"Unable to write file {originalFile.FullName}. Please shutdown Steam and try again.");
            return false;
        }

        string? dirName = originalFile.Directory?.Name;
        if (dirName != null)
        {
            dirName += '/';
        }
        else
        {
            dirName = string.Empty;
        }

        contents =
            $"{PatchedText}@import url(\"https://steamloopback.host/{dirName}{originalFile.Name}\");\n@import url(\"https://steamloopback.host/{customFile.Name}\");\n";
        if (file.Length < contents.Length)
        {
            Log.Logger.Warn($"{file.Name} is too small to patch");
            return false;
        }

        contents = string.Concat(contents, new string('\t', (int)(file.Length - contents.Length)));

        try
        {
            await File.WriteAllTextAsync(file.FullName, contents);
        }
        catch (UnauthorizedAccessException e)
        {
            Log.Logger.Error($"Could not write to {file.FullName}");
            Log.Logger.Error(e);
            return false;
        }

        string customFileName = Path.Join(uiDir ?? Steam.SteamUiDir, customFile.Name);
        if (!File.Exists(customFileName))
        {
            await File.Create(customFileName).DisposeAsync();
        }

        Log.Logger.Info($"Patched {file.Name}.");
        return true;
    }

    public static async Task PatchAll(DirectoryInfo directoryInfo, string? overrideName = null)
    {
        bool patchedLibraryFiles = false;
        foreach (FileInfo? file in directoryInfo.EnumerateFiles())
        {
            patchedLibraryFiles |= await Patch(file, overrideName);
        }

        Log.Logger.Info(patchedLibraryFiles
            ? $"Put your custom css in {Path.Join(Steam.SteamUiDir, overrideName)}"
            : "Did not patch any library files.");
    }

    public static void WatchLocal() => s_localWatcher.Start(LocalFolderPath);
    public static void StopWatchingLocal() => s_localWatcher.Stop();

    public static void WatchLibrary() => s_libraryWatcher.Start(LibraryFolderPath);
    public static void StopWatchingLibrary() => s_libraryWatcher.Stop();

    private static (bool, string?) GetKey(FileChangedEvent e) => e.FullPath.EndsWith(".original.css")
        ? (false, null)
        : (true, Path.GetFileName(e.FullPath));

    private static async void OnLibraryPostEviction(FileChangedEvent e)
    {
        FileInfo file = new(e.FullPath);
        if (file.Directory != null)
        {
            _ = await Patch(file, @"libraryroot.custom.css", alertOnPatched: true);
        }
    }

    private static async void OnLocalPostEviction(FileChangedEvent e)
    {
        FileInfo file = new(e.FullPath);
        if (file.Directory != null)
        {
            _ = await Patch(file, uiDir: Steam.ClientUiDir, alertOnPatched: true);
        }
    }
}
