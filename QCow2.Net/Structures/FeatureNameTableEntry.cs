using QCow2.Net.Enums;
using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 48, Pack = 0)]
    public struct FeatureNameTableEntry
    {
        public FeatureType Type;

        public byte BitNumber;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 46)]
        public string Name;
    }
}