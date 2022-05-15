using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32;

namespace SFP
{
    public class UtilsModel
    {
        public static bool IsGZipHeader(IReadOnlyList<byte> arr)
        {
            return arr.Count >= 2 &&
                   arr[0] == 31 &&
                   arr[1] == 139;
        }

        public static async Task<byte[]> CompressBytes(IReadOnlyList<byte> bytes)
        {
            using MemoryStream compressedMemoryStream = new();
            using MemoryStream originalMemoryStream = new(bytes.ToArray());
            using (var compressor = new GZipStream(compressedMemoryStream, CompressionLevel.Fastest))
            {
                await originalMemoryStream.CopyToAsync(compressor);
            }
            return compressedMemoryStream.ToArray();
        }

        public static async Task<byte[]> DecompressBytes(IReadOnlyList<byte> bytes)
        {
            using MemoryStream compressedMemoryStream = new(bytes.ToArray());
            using MemoryStream originalMemoryStream = new();
            using (var decompressor = new GZipStream(compressedMemoryStream, CompressionMode.Decompress))
            {
                await decompressor.CopyToAsync(originalMemoryStream);
            }
            return originalMemoryStream.ToArray();
        }

        [SupportedOSPlatform("windows")]
        public static object? GetRegistryData(string aKey, string aValueName)
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(aKey);
            object? value = null;
            var regValue = registryKey?.GetValue(aValueName);
            if (regValue != null)
            {
                value = regValue;
            }

            return value;
        }

        // from https://stackoverflow.com/a/41836532
        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            var handle = GCHandle.Alloc(structure, GCHandleType.Pinned);
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            try
            {
                Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), true);
                Marshal.Copy(handle.AddrOfPinnedObject(), array, 0, size);
                return array;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
