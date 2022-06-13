using System.Runtime.InteropServices;

namespace SFP.Models
{
    internal class Native
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(
          string lpFileName,
          string lpExistingFileName,
          IntPtr lpSecurityAttributes
         );
    }
}
