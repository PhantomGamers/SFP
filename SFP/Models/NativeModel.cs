using System.Runtime.InteropServices;

namespace SFP
{
    internal class NativeModel
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(
          string lpFileName,
          string lpExistingFileName,
          IntPtr lpSecurityAttributes
         );
    }
}
