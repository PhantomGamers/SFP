namespace SFP.Models.ChromeCache.BlockFile;

public static class Parser
{
    public static List<FileInfo> FindCacheFilesWithName(DirectoryInfo cacheDir, string fileName, bool silent = false)
    {
        if (!silent)
        {
            Log.Logger.Info("Parsing cache...");
        }

        if (!cacheDir.Exists)
        {
            Log.Logger.Error("Cache folder does not exist, start Steam and try again.");
            return new List<FileInfo>();
        }

        FileInfo index = new(Path.Join(cacheDir.FullName, "index"));
        if (!index.Exists)
        {
            Log.Logger.Error($"{index.FullName} does not exist. Please restart Steam and try again.");
            return new List<FileInfo>();
        }

        index = HardLink.GetLink(index);
        IndexHeader indexHeader;
        try
        {
            indexHeader = new IndexHeader(index);
        }
        catch (FileNotFoundException)
        {
            Log.Logger.Error($"{index.FullName} does not exist. Please restart Steam and try again.");
            return new List<FileInfo>();
        }
        catch (IOException)
        {
            Log.Logger.Error($"Unable to open {index.FullName}. Please shutdown Steam and try again.");
            return new List<FileInfo>();
        }

        List<FileInfo> files = new();
        using FileStream fs = index.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _ = fs.Seek(92 * 4, SeekOrigin.Begin);
        using BinaryReader br = new(fs);
        for (int i = 0; i < indexHeader.Table_len; i++)
        {
            uint raw = br.ReadUInt32();
            if (raw == 0)
            {
                continue;
            }

            EntryStore entry = new(new Addr(raw, cacheDir.FullName));
            while (entry.next != 0)
            {
                // Even if the key points to friends.css, it could be an evicted entry that has no addresses
                if (entry.Key.Contains(fileName) && entry.data_addrs.Count >= 2)
                {
                    files.Add(entry.data_addrs[1].File);
                }

                entry = new EntryStore(new Addr(entry.next, cacheDir.FullName));
            }

            if (!entry.Key.Contains(fileName))
            {
                continue;
            }

            Log.Logger.Debug($"Found a entry {entry.Key} with {entry.data_addrs.Count} addresses");
            // Even if the key points to friends.css, it could be an evicted entry that has no addresses
            if (entry.data_addrs.Count < 2)
            {
                continue;
            }

            for (int j = 0; j < entry.data_addrs.Count; j++)
            {
                Log.Logger.Debug($"Entry's Address {j} points to {entry.data_addrs[j].File.Name}");
            }

            files.Add(entry.data_addrs[1].File);
        }

        br.Close();
        fs.Close();
        _ = HardLink.RemoveAllHardLinks();
        if (silent)
        {
            return files;
        }

        Log.Logger.Info($"Found {files.Count} matches...");
        foreach (FileInfo? file in files)
        {
            Log.Logger.Info($"Found {fileName} in {file.FullName}");
        }

        return files;
    }
}
