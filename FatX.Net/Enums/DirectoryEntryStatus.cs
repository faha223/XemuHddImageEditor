namespace FatX.Net.Enums
{
    public enum DirectoryEntryStatus : byte
    {
        Available = 0x00,
        Deleted = 0xE5,
        EndOfDirMarker = 0xFF
    }
}
