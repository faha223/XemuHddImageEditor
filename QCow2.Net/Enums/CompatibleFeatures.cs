namespace QCow2.Net.Enums
{
    [Flags]
    public enum CompatibleFeatures : ulong
    {
        None = 0,
        LazyRefcounts = 1
        // Bits 1-63 are reserved for future use
    }
}