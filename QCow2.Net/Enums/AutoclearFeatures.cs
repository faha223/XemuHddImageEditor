namespace QCow2.Net.Enums
{
    [Flags]
    public enum AutoclearFeatures : ulong
    {
        None = 0,
        BitmapsExtension = 1 << 0,
        RawExternalData = 1 << 1,
        // Bits 2-63 RFU
    }
}