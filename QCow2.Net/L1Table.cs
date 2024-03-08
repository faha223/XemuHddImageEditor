using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L1Table
    {
        public List<L1TableEntry> Entries { get; private set; } = [];

        public void Read(FileStream source, FileHeader fileHeader)
        {
            Entries.Clear();

            source.Seek((long)fileHeader.L1TableOffset, SeekOrigin.Begin);
            while((ulong)source.Position < fileHeader.L1TableOffset + (fileHeader.L1Size * 8))
            {
                // Read L1 Table Entries
                var entry = StructParser.Read<L1TableEntry>(source, true);
                Console.WriteLine($"Entry (raw): {entry.Bits:B64}");
                Console.WriteLine($"Entry: {(entry.Bits & 0x07FFFFFFFFFFFF00L) >> 9}");

                Entries.Add(entry);
            }
        }
    }
}