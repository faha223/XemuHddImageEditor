namespace QCow2.Net
{
    public static class Constants
    {
        public const int QCOW_MAGIC = 0x514649fb;

        public const ulong Bits0Through61 = 0x3FFFFFFFFFFFFFFF; // Mask to extract bits 0-61 from a 64-bit value    

        public const ulong Bits9Through55 = 0x007FFFFFFFFFFE00; // Mask to extract bits 9-55 from a 64-bit value

        public const ulong Bits9Through63 = 0xFFFFFFFFFFFFFE00; // Mask to extract bits 9-63 from a 64-bit value
    }
}