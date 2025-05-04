namespace FatX.Net
{
    public static class Constants
    {
        public static UnixFileMode DefaultDirectoryMode =>  UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                                            UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                                                            UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

        public static UnixFileMode DefaultFileMode =>   UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                                                        UnixFileMode.OtherRead;
        
        public const uint FATX_Signature = 0x58544146;
        public const uint FATX_FAT_Offset = 4096;
        public const uint FATX_FAT_ReservedEntriesCount = 1;
        public const int  FATX_MaxFilenameLen = 42;
        public const uint SectorSize512 = 512;
        public const uint SectorSize4096 = 4096;
        public const long CPartitionOffset = 0x8ca80000;
        public const long CPartitionSize = 0x01f400000;
        public const long EPartitionOffset = 0xabe80000;
        public const long EPartitionSize = 0x131f00000;
        public const long XPartitionOffset = 0x00080000;
        public const long XPartitionSize = 0x02ee00000;
        public const long YPartitionOffset = 0x2ee80000;
        public const long YPartitionSize = 0x02ee00000;
        public const long ZPartitionOffset = 0x5dc80000;
        public const long ZPartitionSize = 0x02ee00000;

        // These are the known FAT entry values (FAT16)
        public const ushort FATX_CLUSTER_AVAILABLE_16 = 0x0000;     // This cluster is available
        public const ushort FATX_CLUSTER_RESERVED_16 = 0xFFF0;      // This cluster is reserved
        public const ushort FATX_CLUSTER_BAD_16 = 0xFFF7;           // This cluster is bad
        public const ushort FATX_CLUSTER_MEDIA_16 = 0xFFF8;         // This cluster is Media
        public const ushort FATX_CLUSTER_END_16 = 0xFFFF;           // The end of the FAT table has been found

        // These are the known FAT entry values (FAT32)
        public const uint FATX_CLUSTER_AVAILABLE_32 = 0x00000000;   // This cluster is available
        public const uint FATX_CLUSTER_RESERVED_32 = 0xFFFFFFF0;    // This cluster is reserved
        public const uint FATX_CLUSTER_BAD_32 = 0xFFFFFFF7;         // This cluster is bad
        public const uint FATX_CLUSTER_MEDIA_32 = 0xFFFFFFF8;       // This cluster is Media
        public const uint FATX_CLUSTER_END_32 = 0xFFFFFFFF;         // The end of the FAT table has been found
    }   
}