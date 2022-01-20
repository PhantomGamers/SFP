namespace SFP.ChromeCache.BlockFile
{
    public class Patcher
    {
        public static async Task<bool> PatchFile(FileInfo file)
        {
            if (!file.Exists)
            {
                LogModel.Logger.Warn($"{file.FullName} does not exist");
                return false;
            }

            var bytes = await File.ReadAllBytesAsync(file.FullName);
            (bytes, var patched) = await Models.ChromeCache.Patcher.PatchFriendsCSS(bytes, file.Name, file.Length);
            if (patched)
            {
                await File.WriteAllBytesAsync(file.FullName, bytes);
                LogModel.Logger.Info($"Patched {file.Name}.\nPut your custom css in {Path.Join(SteamModel.ClientUIDir, "friends.custom.css")}");
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void WatchFile(FileInfo file)
        {
            FSWModel.AddFileSystemWatcher(file.DirectoryName, "data*", OnFileSystemEvent);
        }

        private static async void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            var files = Parser.FindCacheFilesWithName(new DirectoryInfo(e.FullPath).Parent, "friends.css");
            var filesPatched = false;
            foreach (var file in files)
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
