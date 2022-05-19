using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace SFP.ChromeCache.BlockFile
{
    public class Patcher
    {
        private static MemoryCache s_memCache;
        private static readonly TimeSpan s_cacheTimeSpan = TimeSpan.FromSeconds(Properties.Settings.Default.ScannerDelay);

        static Patcher()
        {
            s_memCache = new(new MemoryCacheOptions());
        }

        public static void ClearMemCache()
        {
            s_memCache.Dispose();
            s_memCache = new(new MemoryCacheOptions());
        }

        public static async Task<bool> PatchFile(FileInfo file)
        {
            if (!file.Exists)
            {
                LogModel.Logger.Warn($"{file.FullName} does not exist");
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = await File.ReadAllBytesAsync(file.FullName);
            }
            catch (IOException)
            {
                LogModel.Logger.Warn($"Unable to read file {file.FullName}. Please shutdown Steam and try again.");
                return false;
            }

            (bytes, bool patched) = await Models.ChromeCache.Patcher.PatchFriendsCSS(bytes, file.Name);
            if (patched)
            {
                try
                {
                    await File.WriteAllBytesAsync(file.FullName, bytes);
                    LogModel.Logger.Info($"Patched {file.Name}.\nPut your custom css in {Path.Join(SteamModel.ClientUIDir, "friends.custom.css")}");
                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void WatchFile(FileInfo file)
        {
            string? dirname = file.DirectoryName;
            if (dirname != null)
            {
                FSWModel.AddFileSystemWatcher(dirname, "f_*", OnFileSystemEvent);
            }
        }

        private static void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            DirectoryInfo? dir = new DirectoryInfo(e.FullPath).Parent;
            if (dir == null)
            {
                return;
            }

            MemoryCacheEntryOptions options = new()
            {
                Priority = CacheItemPriority.NeverRemove,
                AbsoluteExpirationRelativeToNow = s_cacheTimeSpan
            };
            _ = options.AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(s_cacheTimeSpan).Token));
            _ = options.RegisterPostEvictionCallback(OnRemovedFromCache);
            s_memCache.Set(dir.Name, dir, options);
        }

        private static async void OnRemovedFromCache(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.TokenExpired)
            {
                return;
            }

            if (value is DirectoryInfo dir)
            {
                List<FileInfo>? files = Parser.FindCacheFilesWithName(dir, "friends.css");
                bool filesPatched = false;
                foreach (FileInfo? file in files)
                {
                    filesPatched |= await PatchFile(file);
                }

                if (filesPatched && Properties.Settings.Default.RestartSteamOnPatch)
                {
                    SteamModel.RestartSteam();
                }
            }
        }
    }
}
