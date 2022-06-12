using FileWatcherEx;

using SFP.Models.FileSystemWatchers;

namespace SFP.Models
{
    public class LocalFile
    {
        public const string PATCHED_TEXT = "/*patched*/\n";
        public const string ORIGINAL_TEXT = "/*original*/\n";

        private static string s_localFolderPath => Steam.ClientUICSSDir;
        private static readonly DelayedWatcher s_localWatcher = new(s_localFolderPath, OnLocalPostEviction, GetKey)
        {
            Filter = "friends.css"
        };

        private static string s_libraryFolderPath => Steam.SteamUICSSDir;
        private static readonly DelayedWatcher s_libraryWatcher = new(s_libraryFolderPath, OnLibraryPostEviction, GetKey)
        {
            Filter = "*.css"
        };

        public static async Task<bool> Patch(FileInfo? file, string? overrideName = null, string? uiDir = null, bool alertOnPatched = false)
        {
            if (file is null)
            {
                Log.Logger.Error($"Library file does not exist. Start Steam and try again");
                return false;
            }

            string contents;
            try
            {
                contents = await File.ReadAllTextAsync(file.FullName);
            }
            catch (IOException)
            {
                Log.Logger.Warn($"Unable to read file {file.FullName}. Please shutdown Steam and try again.");
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
                if (!alertOnPatched)
                {
                    Log.Logger.Info($"{file.Name} is already patched.");
                }
                return false;
            }

            if (!alertOnPatched)
            {
                Log.Logger.Info($"Patching file {file.Name}");
            }

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
                Log.Logger.Warn($"Unable to write file {originalFile.FullName}. Please shutdown Steam and try again.");
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
                Log.Logger.Warn($"{file.Name} is too small to patch");
                return false;
            }
            contents = string.Concat(contents, new string('\t', (int)(file.Length - contents.Length)));

            File.WriteAllText(file.FullName, contents);

            string? customFileName = Path.Join(uiDir ?? Steam.SteamUIDir, customFile.Name);
            if (!File.Exists(customFileName))
            {
                File.Create(customFileName).Dispose();
            }
            Log.Logger.Info($"Patched {file.Name}.");
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
                Log.Logger.Info($"Put your custom css in {Path.Join(Steam.SteamUIDir, overrideName)}");
            }
            else
            {
                Log.Logger.Info($"Did not patch any library files.");
            }
        }

        public static void WatchLocal() => s_localWatcher.Start(s_localFolderPath);
        public static void StopWatchingLocal() => s_localWatcher.Stop();
        public static bool IsLocalActive => s_localWatcher.IsActive;

        public static void WatchLibrary() => s_libraryWatcher.Start(s_libraryFolderPath);
        public static void StopWatchingLibrary() => s_libraryWatcher.Stop();
        public static bool IsLibraryActive => s_libraryWatcher.IsActive;

        private static (bool, string?) GetKey(FileChangedEvent e) => e.FullPath.EndsWith(".original.css") ? (false, null) : (true, Path.GetFileName(e.FullPath));

        private static async void OnLibraryPostEviction(FileChangedEvent e)
        {
            FileInfo file = new(e.FullPath);
            if (file.Directory != null)
            {
                _ = await Patch(file, "libraryroot.custom.css", alertOnPatched: true);
            }
        }

        private static async void OnLocalPostEviction(FileChangedEvent e)
        {
            FileInfo file = new(e.FullPath);
            if (file.Directory != null)
            {
                _ = await Patch(file, uiDir: Steam.ClientUIDir, alertOnPatched: true);
            }
        }
    }
}
