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

            byte[]? bytes = await File.ReadAllBytesAsync(file.FullName);
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
                FSWModel.AddFileSystemWatcher(dirname, "data*", OnFileSystemEvent);
            }
        }

        private static async void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            DirectoryInfo? dir = new DirectoryInfo(e.FullPath).Parent;
            if (dir == null)
            {
                return;
            }

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
