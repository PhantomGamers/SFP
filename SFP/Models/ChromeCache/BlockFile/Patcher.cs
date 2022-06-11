using FileWatcherEx;

using SFP.Models.FileSystemWatchers;

namespace SFP.Models.ChromeCache.BlockFile
{
    public class Patcher
    {
        private static readonly DelayedWatcher s_watcher = new(SteamModel.CacheDir, OnPostEviction, GetKey, new string[] { "f_*" });

        public static async Task<bool> PatchFile(FileInfo file, bool alertOnPatched = false)
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

            (bytes, bool patched) = await ChromeCache.Patcher.PatchFriendsCSS(bytes, file.Name, alertOnPatched);
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

        public static void Watch() => s_watcher.Start();
        public static void StopWatching() => s_watcher.Stop();
        public static bool IsActive => s_watcher.IsActive;

        private static (bool, string?) GetKey(FileChangedEvent e)
        {
            DirectoryInfo? dir = new DirectoryInfo(e.FullPath).Parent;
            return dir == null ? (false, null) : ((bool, string?))(true, dir.Name);
        }

        private static async void OnPostEviction(FileChangedEvent e)
        {
            DirectoryInfo? dir = new FileInfo(e.FullPath).Directory;

            List<FileInfo>? files = Parser.FindCacheFilesWithName(dir!, "friends.css", true);
            bool filesPatched = false;

            foreach (FileInfo? file in files)
            {
                filesPatched |= await PatchFile(file, true);
            }

            if (filesPatched && Properties.Settings.Default.RestartSteamOnPatch)
            {
                SteamModel.RestartSteam();
            }
        }
    }
}
