using InteropHelpers;
using QCow2.Net.Enums;
using System.Runtime.InteropServices;

namespace QCow2.Net.Structures
{ 
    [StructLayout(LayoutKind.Explicit, Size = 104)]
    public struct FileHeader
    {
        [BigEndian]
        [FieldOffset(0)]
        public uint Magic;

        [BigEndian]
        [FieldOffset(4)]
        public uint Version;

        [BigEndian]
        [FieldOffset(8)]
        public ulong BackingFileOffset;

        [BigEndian]
        [FieldOffset(16)]
        public uint BackingFileSize;

        [BigEndian]
        [FieldOffset(20)]
        public uint ClusterBits;

        [BigEndian]
        [FieldOffset(24)]
        public ulong Size;

        [BigEndian]
        [FieldOffset(32)]
        public CryptMethod CryptMethod;

        [BigEndian]
        [FieldOffset(36)]
        public uint L1Size;
        
        [BigEndian]
        [FieldOffset(40)]
        public ulong L1TableOffset;

        [BigEndian]
        [FieldOffset(48)]
        public ulong RefcountTableOffset;

        [BigEndian]
        [FieldOffset(56)]
        public uint RefcountTableClusters;

        [BigEndian]
        [FieldOffset(60)]
        public uint NumberOfSnapshots;

        [BigEndian]
        [FieldOffset(64)]
        public ulong SnapshotsOffsets;

        /* These fields are only present if Version is >= 3 */
        [BigEndian]
        [FieldOffset(72)]
        public ulong IncompatibleFeatures;

        [BigEndian]
        [FieldOffset(80)]
        public ulong CompatibleFeatures;
        
        [BigEndian]
        [FieldOffset(88)]
        public ulong AutoclearFeatures;

        [BigEndian]
        [FieldOffset(96)]
        public uint RefcountOrder;
        
        [BigEndian]
        [FieldOffset(100)]
        public uint HeaderLength;
    }
}