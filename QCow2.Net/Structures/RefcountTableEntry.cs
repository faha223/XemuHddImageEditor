using InteropHelpers;
using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct RefcountTableEntry
    {
        [BigEndian]
        [FieldOffset(0)]
        public ulong Bits;
    }
}