using Force.Crc32;

namespace SFP.Models.ChromeCache.Simple
{
    public class Patcher
    {
        public static async Task PatchSimpleFile(SimpleFile file)
        {
            (file.eof1_data, _) = await ChromeCache.Patcher.PatchFriendsCSS(file.eof1_data, file.file.Name);
            file.eof1.stream_size = (uint)file.eof1_data.Length;
            file.eof1.data_crc32 = Crc32Algorithm.Compute(file.eof1_data);

            await File.WriteAllBytesAsync(file.file.FullName, file.eof1_data);
        }
    }
}
