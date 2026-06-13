using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L1Table
    {
        public List<L1TableEntry> Entries { get; private set; } = [];

        public void Read(FileStream source, ImageHeaderV3 fileHeader)
        {
            Entries.Clear();

            source.Seek((long)fileHeader.L1TableOffset, SeekOrigin.Begin);
            while((ulong)source.Position < fileHeader.L1TableOffset + (fileHeader.L1Size * 8))
            {
                // Read L1 Table Entries
                var entry = new L1TableEntry(source.Read<ulong>(true));
                Console.WriteLine($"Entry: {entry.ImageOffset} ({entry.ImageOffset:X16})");

                Entries.Add(entry);
            }
        }

        public L1TableEntry GetEntry(long index)
        {
            if(index < 0 || index >= Entries.Count)
                throw new IndexOutOfRangeException($"L1 Table index {index} is out of range. Valid range is 0 to {Entries.Count - 1}.");

            return Entries[(int)index];
        }
    }
}