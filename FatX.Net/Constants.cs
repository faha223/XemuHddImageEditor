using System.IO;
using System.Runtime.InteropServices;

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
    }   
}