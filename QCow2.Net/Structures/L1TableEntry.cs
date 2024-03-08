using InteropHelpers;
using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct L1TableEntry
    {
        [BigEndian]
        [FieldOffset(0)]
        public ulong Bits;
    }
}