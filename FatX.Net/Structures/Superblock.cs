using System.Runtime.InteropServices;

namespace FatX.Net.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Superblock {

        public uint Signature;

        public uint VolumeId;

        public uint SectorsPerCluster;

        public uint RootCluster;

        public ushort Unknown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4078)]
        public byte[] Padding;
    }
}