namespace SFP
{
    public class FSWModel
    {
        private static readonly Dictionary<string, FileSystemWatcher> fileSystemWatchers = new();

        public static bool WatchersActive => fileSystemWatchers.Count > 0;

        public static bool ScanFriends { get; private set; } = true;

        public static bool ScanLibrary { get; private set; } = true;

        public static bool AddFileSystemWatcher(string fileFullName, FileSystemEventHandler fileSystemEventHandler)
        {
            return AddFileSystemWatcher(Path.GetDirectoryName(fileFullName), Path.GetFileName(fileFullName), fileSystemEventHandler);
        }

        public static bool AddFileSystemWatcher(string directoryName, string filter, FileSystemEventHandler fileSystemEventHandler)
        {
            var key = Path.Join(directoryName, filter);
            if (fileSystemWatchers.ContainsKey(key))
            {
                return false;
            }

            Directory.CreateDirectory(directoryName);

            var watcher = new FileSystemWatcher(directoryName)
            {
                // NotifyFilter = NotifyFilters.LastWrite
                //              | NotifyFilters.Size,
                Filter = filter
            };

            watcher.Changed += fileSystemEventHandler;
            watcher.Created += fileSystemEventHandler;
            watcher.EnableRaisingEvents = true;

            fileSystemWatchers.Add(key, watcher);

            return true;
        }

        internal static bool RemoveFileSystemWatcher(string fileFullName)
        {
            if (!fileSystemWatchers.ContainsKey(fileFullName))
            {
                return false;
            }

            var watcher = fileSystemWatchers[fileFullName];

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            fileSystemWatchers.Remove(fileFullName);

            return true;
        }

        public static bool RemoveAllWatchers()
        {
            var result = true;

            foreach (var watcher in fileSystemWatchers.Keys)
            {
                result &= RemoveFileSystemWatcher(watcher);
            }

            return result;
        }

        public static async Task StartFileWatchers(bool? scanFriends = null, bool? scanLibrary = null)
        {
            ScanFriends = scanFriends ?? ScanFriends;
            ScanLibrary = scanLibrary ?? ScanLibrary;

            if (ScanFriends)
            {
                await StartFriendsWatcher();
            }

            if (ScanLibrary)
            {
                await StartLibraryWatcher();
            }
        }

        public static async Task StartFriendsWatcher()
        {
            if (SteamModel.SteamDir != null)
            {
                await Task.Run(() => ChromeCache.BlockFile.Patcher.WatchFile(new FileInfo(Path.Join(SteamModel.CacheDir, "index"))));
                await Task.Run(() => LocalFileModel.WatchLocal(Path.Join(SteamModel.ClientUIDir, "css", "friends.css")));
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
                await Task.Run(() => LocalFileModel.WatchLibrary(Path.Join(SteamModel.SteamUIDir, "css")));
            }
            else
            {
                LogModel.Logger.Warn("Steam Directory unknown. Please set it and try again.");
            }
        }

        public static bool ToggleFileSystemWatcher(string fileFullName, bool? state = null)
        {
            if (fileSystemWatchers.ContainsKey(fileFullName))
            {
                var watcher = fileSystemWatchers[fileFullName];
                watcher.EnableRaisingEvents = state ?? !watcher.EnableRaisingEvents;
                return true;
            }

            return false;
        }

        public static FileSystemWatcher? GetFileSystemWatcher(string fileFullName)
        {
            if (fileSystemWatchers.ContainsKey(fileFullName))
            {
                return fileSystemWatchers[fileFullName];
            }

            return null;
        }
    }
}
