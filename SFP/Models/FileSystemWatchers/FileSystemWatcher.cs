#region

using SFP.Models.ChromeCache.BlockFile;

#endregion

namespace SFP.Models.FileSystemWatchers;

public static class FileSystemWatcher
{
    public static bool WatchersActive => Patcher.IsActive || LocalFile.IsLocalActive || LocalFile.IsLibraryActive;

    private static bool ScanFriends => OperatingSystem.IsWindows() && Properties.Settings.Default.ShouldScanFriends;
    private static bool ScanLibrary => Properties.Settings.Default.ShouldScanLibrary;
    private static bool ScanResources => Properties.Settings.Default.ShouldScanResources;

    public static async Task StartFileWatchers()
    {
        if (ScanFriends)
        {
            await StartFriendsWatcher();
        }

        if (ScanLibrary)
        {
            await StartLibraryWatcher();
        }

        if (ScanResources)
        {
            Resource.Watch();
        }
    }

    private static async Task StartFriendsWatcher()
    {
        if (Steam.SteamDir != null)
        {
            await Task.Run(Patcher.Watch);
            await Task.Run(LocalFile.WatchLocal);
        }
        else
        {
            Log.Logger.Warn("Steam Directory unknown. Please set it and try again.");
        }
    }

    private static async Task StartLibraryWatcher()
    {
        if (Steam.SteamDir != null)
        {
            await Task.Run(LocalFile.WatchLibrary);
        }
        else
        {
            Log.Logger.Warn("Steam Directory unknown. Please set it and try again.");
        }
    }

    public static async Task StopFileWatchers()
    {
        await StopFriendsWatcher();
        await StopLibraryWatcher();
    }

    private static async Task StopFriendsWatcher()
    {
        await Task.Run(Patcher.StopWatching);
        await Task.Run(LocalFile.StopWatchingLocal);
    }

    private static async Task StopLibraryWatcher() => await Task.Run(LocalFile.StopWatchingLibrary);
}
