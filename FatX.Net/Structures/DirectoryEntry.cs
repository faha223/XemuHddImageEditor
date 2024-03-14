using FatX.Net.Enums;
using System.Runtime.InteropServices;

namespace FatX.Net.Structures
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack=1)]
    internal struct DirectoryEntry
    {
        public DirectoryEntryStatus Status;
        public Attributes          Attributes;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst=Constants.FATX_MaxFilenameLen)]
        public byte[]               Filename;
        public uint                 FirstCluster;
        public uint                 FileSize;
        public ushort               ModifiedTime;
        public ushort               ModifiedDate;
        public ushort               CreatedTime;
        public ushort               CreatedDate;
        public ushort               AccessedTime;
        public ushort               AccessedDate;
    }
}