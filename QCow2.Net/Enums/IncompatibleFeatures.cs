namespace QCow2.Net.Enums
{
    [Flags]
    public enum IncompatibleFeatures : ulong
    {
        None = 0,
        Dirty = 1,
        Corrupt = 1 << 1,
        ExternalDataFile = 1 << 2,
        Compression = 1 << 3,
        ExtendedL2Entries = 1 << 4
        
        // Bits 5-63 are reserved for future use
    }
}