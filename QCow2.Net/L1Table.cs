using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L1Table
    {
        private L1Table() {}

        public List<L1TableEntry> Entries { get; private set; } = [];

        public static L1Table Read(Stream source, ImageHeaderV3 fileHeader)
        {
            var table = new L1Table();
            table.Entries.Clear();

            // ImageHeader.L1Size is the number of entries in the L1 Table, and each entry is 8 bytes (long), 
            // so we can calculate the total size of the L1 Table in bytes
            ulong l1Size = fileHeader.L1Size * sizeof(ulong);
            source.Seek((long)fileHeader.L1TableOffset, SeekOrigin.Begin);
            while((ulong)source.Position < fileHeader.L1TableOffset + l1Size)
            {
                // Read L1 Table Entries
                var bits = source.Read<L1TableEntryBits>(true);
                var entry = new L1TableEntry(bits.Bits);
                table.Entries.Add(entry);
            }

            return table;
        }

        public L1TableEntry GetEntry(long index)
        {
            if(index < 0 || index >= Entries.Count)
                throw new IndexOutOfRangeException($"L1 Table index {index} is out of range. Valid range is 0 to {Entries.Count - 1}.");

            return Entries[(int)index];
        }

        public void Print()
        {
            Console.WriteLine("--- L1 Table ---");
            for(int i = 0; i < Entries.Count; i++)
                Entries[i].Print();
            Console.WriteLine("--- End of L1 Table ---");
        }
    }
}