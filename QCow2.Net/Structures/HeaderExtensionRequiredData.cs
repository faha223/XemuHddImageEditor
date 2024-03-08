using InteropHelpers;
using QCow2.Net.Enums;
using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct HeaderExtensionRequiredData
    {
        [BigEndian]
        [FieldOffset(0)]
        public HeaderExtensionType Type;

        [BigEndian]
        [FieldOffset(4)]
        public uint Length;
    }
}