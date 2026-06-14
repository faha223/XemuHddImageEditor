using System.Runtime.InteropServices;
using InteropHelpers;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct L1TableEntryBits
    {
        [BigEndian]
        [FieldOffset(0)]
        public ulong Bits;
    }
}