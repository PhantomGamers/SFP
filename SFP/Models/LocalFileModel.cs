using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace SFP
{
    public class LocalFileModel
    {
        public const string PATCHED_TEXT = "/*patched*/\n";
        public const string ORIGINAL_TEXT = "/*original*/\n";

        private static MemoryCache s_localMemCache;

        private static MemoryCache s_libraryMemCache;

        private static readonly TimeSpan s_cacheTimeSpan = TimeSpan.FromSeconds(Properties.Settings.Default.ScannerDelay);

        static LocalFileModel()
        {
            s_localMemCache = new(new MemoryCacheOptions());
            s_libraryMemCache = new(new MemoryCacheOptions());
        }

        public static void ClearMemCache()
        {
            s_localMemCache.Dispose();
            s_localMemCache = new(new MemoryCacheOptions());

            s_libraryMemCache.Dispose();
            s_libraryMemCache = new(new MemoryCacheOptions());
        }

        public static async Task<bool> Patch(FileInfo? file, string? overrideName = null, string? uiDir = null)
        {
            if (file is null)
            {
                LogModel.Logger.Error($"Library file does not exist. Start Steam and try again");
                return false;
            }

            bool state = false;
            FileSystemWatcher? watcher = FSWModel.GetFileSystemWatcher(Path.Join(file.DirectoryName, "*.css"));
            if (watcher != null)
            {
                state = watcher.EnableRaisingEvents;
                watcher.EnableRaisingEvents = false;
            }

            string contents;
            try
            {
                contents = await File.ReadAllTextAsync(file.FullName);
            }
            catch (IOException)
            {
                LogModel.Logger.Warn($"Unable to read file {file.FullName}. Please shutdown Steam and try again.");
                return false;
            }

            if (contents.StartsWith(ORIGINAL_TEXT))
            {
                // We are looking at an original file!
                return false;
            }

            if (contents.StartsWith(PATCHED_TEXT))
            {
                // File is already patched
                LogModel.Logger.Info($"{file.Name} is already patched.");
                return false;
            }

            LogModel.Logger.Info($"Patching file {file.Name}");

            var originalFile = new FileInfo($"{Path.Join(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName))}.original{Path.GetExtension(file.FullName)}");

            FileInfo customFile = overrideName != null
                                ? new(Path.Join(file.DirectoryName, overrideName))
                                : new($"{Path.Join(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName))}.custom{Path.GetExtension(file.FullName)}");
            try
            {
                await File.WriteAllTextAsync(originalFile.FullName, string.Concat(ORIGINAL_TEXT, contents));
            }
            catch (IOException)
            {
                LogModel.Logger.Warn($"Unable to write file {originalFile.FullName}. Please shutdown Steam and try again.");
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

            contents = $"{PATCHED_TEXT}@import url(\"https://steamloopback.host/{dirName}{originalFile.Name}\");\n@import url(\"https://steamloopback.host/{customFile.Name}\");\n";
            if (file.Length < contents.Length)
            {
                LogModel.Logger.Warn($"{file.Name} is too small to patch");
                return false;
            }
            contents = string.Concat(contents, new string('\t', (int)(file.Length - contents.Length)));

            File.WriteAllText(file.FullName, contents);

            string? customFileName = Path.Join(uiDir ?? SteamModel.SteamUIDir, customFile.Name);
            if (!File.Exists(customFileName))
            {
                File.Create(customFileName).Dispose();
            }
            LogModel.Logger.Info($"Patched {file.Name}.");
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = state;
            }
            return true;
        }

        public static async Task PatchAll(DirectoryInfo directoryInfo, string? overrideName = null)
        {
            bool patchedLibraryFiles = false;
            foreach (FileInfo? file in directoryInfo.EnumerateFiles())
            {
                patchedLibraryFiles |= await Patch(file, overrideName);
            }
            if (patchedLibraryFiles)
            {
                LogModel.Logger.Info($"Put your custom css in {Path.Join(SteamModel.SteamUIDir, overrideName)}");
            }
            else
            {
                LogModel.Logger.Info($"Did not patch any library files.");
            }
        }

        public static async Task WatchLibrary(string directoryName)
        {
            await Task.Run(() => FSWModel.AddFileSystemWatcher(directoryName, "*.css", OnLibraryWatcherEvent));
        }

        private static void OnLibraryWatcherEvent(object sender, FileSystemEventArgs e)
        {
            MemoryCacheEntryOptions options = new()
            {
                Priority = CacheItemPriority.NeverRemove,
                AbsoluteExpirationRelativeToNow = s_cacheTimeSpan
            };
            _ = options.AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(s_cacheTimeSpan).Token));
            _ = options.RegisterPostEvictionCallback(OnLibraryRemovedFromCache);
            s_libraryMemCache.Set(e.Name, e, options);
        }

        private static async void OnLibraryRemovedFromCache(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.TokenExpired)
            {
                return;
            }

            if (value is FileSystemEventArgs fileSystemEventArgs)
            {
                var file = new FileInfo(fileSystemEventArgs.FullPath);
                if (file.Directory != null)
                {
                    await Task.Run(() => Patch(file, "libraryroot.custom.css"));
                }
            }
        }

        public static async Task WatchLocal(string fileFullPath)
        {
            string? pathRoot = Path.GetDirectoryName(fileFullPath);
            if (pathRoot != null)
            {
                await Task.Run(() => FSWModel.AddFileSystemWatcher(pathRoot, Path.GetFileName(fileFullPath), OnLocalWatcherEvent));
            }
        }

        private static void OnLocalWatcherEvent(object sender, FileSystemEventArgs e)
        {
            MemoryCacheEntryOptions options = new()
            {
                Priority = CacheItemPriority.NeverRemove,
                AbsoluteExpirationRelativeToNow = s_cacheTimeSpan
            };
            _ = options.AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(s_cacheTimeSpan).Token));
            _ = options.RegisterPostEvictionCallback(OnLocalRemovedFromCache);
            s_localMemCache.Set(e.Name, e, options);
        }

        private static async void OnLocalRemovedFromCache(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.TokenExpired)
            {
                return;
            }

            if (value is FileSystemEventArgs fileSystemEventArgs)
            {
                var file = new FileInfo(fileSystemEventArgs.FullPath);
                if (file.Directory != null)
                {
                    await Patch(file, uiDir: SteamModel.ClientUIDir);
                }
            }
        }
    }
}
