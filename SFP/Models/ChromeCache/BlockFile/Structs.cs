using System.Text;

namespace SFP.ChromeCache.BlockFile
{
    internal readonly struct EntryStore
    {
        // Converted partially from https://github.com/chromium/chromium/blob/main/net/disk_cache/blockfile/disk_format.h

        private readonly uint _keyLength = 0;  // Next entry with the same hash or bucket.
        public readonly string Key = string.Empty;
        public readonly uint next = 0;
        public readonly List<Addr> data_addrs = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Can error with corrupted cache")]
        public EntryStore(Addr addr)
        {
            FileInfo? tmpFile = LinkModel.GetLink(addr.File);
            if (!tmpFile.Exists)
            {
                LogModel.Logger.Warn("Could not create tmp file. Clear cache and try again");
                return;
            }
            using FileStream? fs = tmpFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(8196 + addr.Num_Blocks * addr.BlockSize, SeekOrigin.Begin);
            using var br = new BinaryReader(fs);
            next = br.ReadUInt32();
            fs.Seek(24, SeekOrigin.Current);
            _keyLength = br.ReadUInt32();
            fs.Seek(20, SeekOrigin.Current);
            for (int i = 0; i < 4; i++)
            {
                uint raw = br.ReadUInt32();
                // TODO: Investigate why this is sometimes 0
                if (raw == 0)
                {
                    continue;
                }
                try
                {
                    data_addrs.Add(new Addr(raw, addr.File.DirectoryName!));
                }
                catch
                {
                    continue;
                }
            }
            fs.Seek(24, SeekOrigin.Current);
            Key = Encoding.UTF8.GetString(br.ReadBytes((int)_keyLength));
            br.Close();
            fs.Close();
        }
    }

    internal readonly struct IndexHeader
    {
        public readonly uint Table_len; // Actual size of the table (0 == kIndexTablesize).

        public IndexHeader(FileInfo file)
        {
            using FileStream? fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fs.Seek(28, SeekOrigin.Begin);
            using var br = new BinaryReader(fs);
            Table_len = br.ReadUInt32();
            br.Close();
            fs.Close();
        }
    }

    internal enum FileType
    {
        EXTERNAL = 0,
        RANKINGS = 1,
        BLOCK_256 = 2,
        BLOCK_1K = 3,
        BLOCK_4K = 4,
        BLOCK_FILES = 5,
        BLOCK_ENTRIES = 6,
        BLOCK_EVICTED = 7
    };

    internal readonly struct Addr
    {
        public readonly string BinaryString;
        public readonly FileType FileType;
        public readonly FileInfo File;
        public readonly int Num_Blocks = 0;
        public readonly int BlockSize = 0;

        public Addr(uint address, string directory)
        {
            BinaryString = Convert.ToString(address, 2);
            FileType = (FileType)Convert.ToInt32(BinaryString[1..4], 2);
            string fileName;
            if (FileType == FileType.EXTERNAL)
            {
                fileName = string.Format("f_{0:x6}", Convert.ToInt32(BinaryString[4..], 2));
            }
            else
            {
                fileName = $"data_{Convert.ToInt32(BinaryString[8..16], 2)}";
                if (FileType != FileType.RANKINGS)
                {
                    BlockSize = BlockSizeForFileType(FileType);
                    Num_Blocks = Convert.ToInt32(BinaryString[16..], 2);
                }
            }
            File = new FileInfo(Path.Join(directory, fileName));
        }

        private static int BlockSizeForFileType(FileType file_type)
        {
            return file_type switch
            {
                FileType.RANKINGS => 36,
                FileType.BLOCK_256 => 256,
                FileType.BLOCK_1K => 1024,
                FileType.BLOCK_4K => 4096,
                FileType.BLOCK_FILES => 8,
                FileType.BLOCK_ENTRIES => 104,
                FileType.BLOCK_EVICTED => 48,
                FileType.EXTERNAL => 0,
                _ => 0,
            };
        }
    }
}
