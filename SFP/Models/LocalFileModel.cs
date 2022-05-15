namespace SFP
{
    public class LocalFileModel
    {
        public const string PATCHED_TEXT = "/*patched*/\n";
        public const string ORIGINAL_TEXT = "/*original*/\n";

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

            string? contents = await File.ReadAllTextAsync(file.FullName);

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
            FileInfo customFile;
            if (overrideName != null)
            {
                customFile = new FileInfo(Path.Join(file.DirectoryName, overrideName));
            }
            else
            {
                customFile = new FileInfo($"{Path.Join(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName))}.custom{Path.GetExtension(file.FullName)}");
            }

            File.WriteAllText(originalFile.FullName, string.Concat(ORIGINAL_TEXT, contents));

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

        private static async void OnLibraryWatcherEvent(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);
            if (file.Directory != null)
            {
                await Task.Run(() => Patch(file, "libraryroot.custom.css"));
            }
        }

        public static async Task WatchLocal(string fileFullPath)
        {
            string? pathRoot = Path.GetPathRoot(fileFullPath);
            if (pathRoot != null)
            {
                await Task.Run(() => FSWModel.AddFileSystemWatcher(pathRoot, Path.GetFileName(fileFullPath), OnLocalWatcherEvent));
            }
        }

        private static async void OnLocalWatcherEvent(object sender, FileSystemEventArgs e)
        {
            var file = new FileInfo(e.FullPath);
            if (file.Directory != null)
            {
                await Patch(file);
            }
        }
    }
}
