namespace QCow2.Net
{
    public static class Constants
    {
        public const int QCOW_MAGIC = 0x514649fb;
        public const ulong Bits9Through55 = 0x07ffffffffffffe00; // Mask to extract bits 9-55 from a 64-bit value
    }
}