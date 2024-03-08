using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct BitmapsExtension
    {
        [FieldOffset(0)]
        public uint NumberOfBitmaps;

        [FieldOffset(4)]
        public uint Reserved;

        [FieldOffset(8)]
        public ulong BitmapDirectorySize;

        [FieldOffset(16)]
        public ulong BitmapDirectoryOffset; 
    }
}