namespace SFP.ChromeCache.BlockFile
{
    public class Parser
    {
        public static List<FileInfo> FindCacheFilesWithName(DirectoryInfo cacheDir, string fileName)
        {
            LogModel.Logger.Info("Parsing cache...");
            if (!cacheDir.Exists)
            {
                LogModel.Logger.Error("Cache folder does not exist, start Steam and try again.");
                return new List<FileInfo>();
            }
            var index = new FileInfo(Path.Join(cacheDir.FullName, "index"));
            index = LinkModel.GetLink(index);
            var indexHeader = new IndexHeader(index);

            List<FileInfo> files = new();
            using var fs = index.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(92 * 4, SeekOrigin.Begin);
            using var br = new BinaryReader(fs);
            for (var i = 0; i < indexHeader.Table_len; i++)
            {
                var raw = br.ReadUInt32();
                if (raw != 0)
                {
                    var entry = new EntryStore(new Addr(raw, cacheDir.FullName));
                    while (entry.next != 0)
                    {
                        if (entry.Key.Contains(fileName))
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
                        for (var j = 0; j < entry.data_addrs.Count; j++)
                        {
                            LogModel.Logger.Debug($"Entry's Address {j} points to {entry.data_addrs[j].File.Name}");
                        }
                        files.Add(entry.data_addrs[1].File);
                    }
                }
            }
            br.Close();
            fs.Close();
            LinkModel.RemoveAllHardLinks();
            LogModel.Logger.Info($"Found {files.Count} matches...");
            foreach (var file in files)
            {
                LogModel.Logger.Info($"Found {fileName} in {file.FullName}");
            }
            return files;
        }
    }
}
