using SFP.Models.ChromeCache.BlockFile;

namespace SFP.Models.FileSystemWatchers
{
    public class FileSystemWatcher
    {
        public static bool WatchersActive => Patcher.IsActive || LocalFile.IsLocalActive || LocalFile.IsLibraryActive;

        public static bool ScanFriends => OperatingSystem.IsWindows() && Properties.Settings.Default.ShouldScanFriends;
        public static bool ScanLibrary => Properties.Settings.Default.ShouldScanLibrary;
        public static bool ScanResources => Properties.Settings.Default.ShouldScanResources;

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

        public static async Task StartFriendsWatcher()
        {
            if (Steam.SteamDir != null)
            {
                await Task.Run(() => Patcher.Watch());
                await Task.Run(() => LocalFile.WatchLocal());
            }
            else
            {
                Log.Logger.Warn("Steam Directory unknown. Please set it and try again.");
            }
        }

        public static async Task StartLibraryWatcher()
        {
            if (Steam.SteamDir != null)
            {
                await Task.Run(() => LocalFile.WatchLibrary());
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

        public static async Task StopFriendsWatcher()
        {
            await Task.Run(() => Patcher.StopWatching());
            await Task.Run(() => LocalFile.StopWatchingLocal());
        }

        public static async Task StopLibraryWatcher() => await Task.Run(() => LocalFile.StopWatchingLibrary());
    }
}
