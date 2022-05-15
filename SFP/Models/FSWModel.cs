namespace SFP
{
    public class FSWModel
    {
        private static readonly Dictionary<string, FileSystemWatcher> s_fileSystemWatchers = new();

        public static bool WatchersActive => s_fileSystemWatchers.Count > 0;

        public static bool ScanFriends { get; private set; } = true;

        public static bool ScanLibrary { get; private set; } = true;

        public static bool AddFileSystemWatcher(string fileFullName, FileSystemEventHandler fileSystemEventHandler)
        {
            string? dirName = Path.GetDirectoryName(fileFullName);
            if (dirName != null)
            {
                return AddFileSystemWatcher(dirName, Path.GetFileName(fileFullName), fileSystemEventHandler);
            }
            return false;
        }

        public static bool AddFileSystemWatcher(string directoryName, string filter, FileSystemEventHandler fileSystemEventHandler)
        {
            string? key = Path.Join(directoryName, filter);
            if (s_fileSystemWatchers.ContainsKey(key))
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

            s_fileSystemWatchers.Add(key, watcher);

            return true;
        }

        internal static bool RemoveFileSystemWatcher(string fileFullName)
        {
            if (!s_fileSystemWatchers.ContainsKey(fileFullName))
            {
                return false;
            }

            FileSystemWatcher? watcher = s_fileSystemWatchers[fileFullName];

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            s_fileSystemWatchers.Remove(fileFullName);

            return true;
        }

        public static bool RemoveAllWatchers()
        {
            bool result = true;

            foreach (string? watcher in s_fileSystemWatchers.Keys)
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
            if (s_fileSystemWatchers.ContainsKey(fileFullName))
            {
                FileSystemWatcher? watcher = s_fileSystemWatchers[fileFullName];
                watcher.EnableRaisingEvents = state ?? !watcher.EnableRaisingEvents;
                return true;
            }

            return false;
        }

        public static FileSystemWatcher? GetFileSystemWatcher(string fileFullName)
        {
            if (s_fileSystemWatchers.ContainsKey(fileFullName))
            {
                return s_fileSystemWatchers[fileFullName];
            }

            return null;
        }
    }
}
