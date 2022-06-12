using SFP.Models.ChromeCache.BlockFile;

namespace SFP.Models.FileSystemWatchers
{
    public class FSWModel
    {
        public static bool WatchersActive => Patcher.IsActive || LocalFileModel.IsLocalActive || LocalFileModel.IsLibraryActive;

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
                ResourceModel.Watch();
            }
        }

        public static async Task StartFriendsWatcher()
        {
            if (SteamModel.SteamDir != null)
            {
                await Task.Run(() => Patcher.Watch());
                await Task.Run(() => LocalFileModel.WatchLocal());
            }
            else
            {
                LogModel.Logger.Warn("Steam Directory unknown. Please set it and try again.");
            }
        }

        public static async Task StartLibraryWatcher()
        {
            if (SteamModel.SteamDir != null)
            {
                await Task.Run(() => LocalFileModel.WatchLibrary());
            }
            else
            {
                LogModel.Logger.Warn("Steam Directory unknown. Please set it and try again.");
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
            await Task.Run(() => LocalFileModel.StopWatchingLocal());
        }

        public static async Task StopLibraryWatcher() => await Task.Run(() => LocalFileModel.StopWatchingLibrary());
    }
}
