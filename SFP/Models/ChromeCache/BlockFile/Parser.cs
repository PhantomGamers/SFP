namespace SFP.Models.ChromeCache.BlockFile
{
    public class Parser
    {
        public static List<FileInfo> FindCacheFilesWithName(DirectoryInfo cacheDir, string fileName, bool silent = false)
        {
            if (!silent)
            {
                LogModel.Logger.Info("Parsing cache...");
            }
            if (!cacheDir.Exists)
            {
                LogModel.Logger.Error("Cache folder does not exist, start Steam and try again.");
                return new();
            }
            FileInfo index = new(Path.Join(cacheDir.FullName, "index"));
            if (!index.Exists)
            {
                LogModel.Logger.Error($"{index.FullName} does not exist. Please restart Steam and try again.");
                return new();
            }

            index = LinkModel.GetLink(index);
            IndexHeader indexHeader;
            try
            {
                indexHeader = new(index);
            }
            catch (FileNotFoundException)
            {
                LogModel.Logger.Error($"{index.FullName} does not exist. Please restart Steam and try again.");
                return new();
            }
            catch (IOException)
            {
                LogModel.Logger.Error($"Unable to open {index.FullName}. Please shutdown Steam and try again.");
                return new();
            }

            List<FileInfo> files = new();
            using FileStream? fs = index.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _ = fs.Seek(92 * 4, SeekOrigin.Begin);
            using var br = new BinaryReader(fs);
            for (int i = 0; i < indexHeader.Table_len; i++)
            {
                uint raw = br.ReadUInt32();
                if (raw != 0)
                {
                    var entry = new EntryStore(new Addr(raw, cacheDir.FullName));
                    while (entry.next != 0)
                    {
                        // TODO: Investigate to see whether this should be >= 2 or if we should just add the last entry in the array
                        if (entry.Key.Contains(fileName) && entry.data_addrs.Count >= 2)
                        {
                            files.Add(entry.data_addrs[1].File);
                        }
                        entry = new EntryStore(new Addr(entry.next, cacheDir.FullName));
                    }
                    if (entry.Key.Contains(fileName))
                    {
                        LogModel.Logger.Debug($"Found a entry {entry.Key} with {entry.data_addrs.Count} addresses");
                        if (entry.data_addrs.Count < 2)
                        {
                            continue;
                        }
                        for (int j = 0; j < entry.data_addrs.Count; j++)
                        {
                            LogModel.Logger.Debug($"Entry's Address {j} points to {entry.data_addrs[j].File.Name}");
                        }
                        files.Add(entry.data_addrs[1].File);
                    }
                }
            }
            br.Close();
            fs.Close();
            _ = LinkModel.RemoveAllHardLinks();
            if (!silent)
            {
                LogModel.Logger.Info($"Found {files.Count} matches...");
                foreach (FileInfo? file in files)
                {
                    LogModel.Logger.Info($"Found {fileName} in {file.FullName}");
                }
            }
            return files;
        }
    }
}
