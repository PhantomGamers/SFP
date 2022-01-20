using System.Text;

namespace SFP.Models.ChromeCache.Simple
{
    public struct SimpleFileHeader
    {
        private readonly ulong initial_magic_number;
        private readonly uint version;
        public readonly uint key_length;
        private readonly uint key_hash;

        public byte[] Serialize()
        {
            return UtilsModel.StructureToByteArray(this);
        }
    };

    public struct SimpleFileEOF
    {
        public enum Flags : uint
        {
            FLAG_HAS_CRC32 = 1U << 0,
            FLAG_HAS_KEY_SHA256 = 1U << 1,  // Preceding the record if present.
        };

        private readonly ulong final_magic_number;
        public readonly uint flags;
        public uint data_crc32;

        // |stream_size| is only used in the EOF record for stream 0.
        public uint stream_size;

        public byte[] Serialize()
        {
            return UtilsModel.StructureToByteArray(this);
        }

        public bool HasSHA256()
        {
            return (flags & (uint)Flags.FLAG_HAS_KEY_SHA256) == (uint)Flags.FLAG_HAS_KEY_SHA256;
        }

        public bool HasCRC32()
        {
            return (flags & (uint)Flags.FLAG_HAS_CRC32) == (uint)Flags.FLAG_HAS_CRC32;
        }
    };

    public struct SimpleFile
    {
        private readonly SimpleFileHeader header;
        private readonly string key;
        private readonly byte[] eof0_data;
        public byte[] eof1_data;
        private readonly SimpleFileEOF eof0;
        public SimpleFileEOF eof1;
        private readonly byte[] sha256;
        public readonly FileInfo file;

        public SimpleFile(SimpleFileHeader header, string key, byte[] eof0_data, byte[] eof1_data, SimpleFileEOF eof0, SimpleFileEOF eof1, byte[] sha256, FileInfo file)
        {
            this.header = header;
            this.key = key;
            this.eof0_data = eof0_data;
            this.eof1_data = eof1_data;
            this.eof0 = eof0;
            this.eof1 = eof1;
            this.sha256 = sha256;
            this.file = file;
        }

        public byte[] Serialize()
        {
            var bytes = new List<byte>();
            bytes.AddRange(header.Serialize());
            bytes.AddRange(Encoding.UTF8.GetBytes(key));
            bytes.AddRange(eof1_data);
            bytes.AddRange(eof1.Serialize());
            bytes.AddRange(eof0_data);
            if (eof0.HasSHA256())
            {
                bytes.AddRange(sha256);
            }
            bytes.AddRange(eof0.Serialize());
            return bytes.ToArray();
        }
    }
}
