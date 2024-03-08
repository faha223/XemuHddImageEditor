using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public struct RefcountBlockEntry
    {
        [FieldOffset(0)]
        public ulong Bits;
    }
}