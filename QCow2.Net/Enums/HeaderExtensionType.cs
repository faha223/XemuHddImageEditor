namespace QCow2.Net.Enums
{
    public enum HeaderExtensionType : uint
    {
        EndOfHeaderExtensionArea = 0x00000000,
        BackingFileFormatName = 0xE2792ACA,
        FeatureNameTable = 0x6803f857,
        BitmapsExtension = 0x23852875
    }
}