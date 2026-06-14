using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class RefcountTable
    {
        private RefcountTable()
        {
            
        }

        public List<RefcountTableEntry> Entries {get; private set;} = [];

        public static RefcountTable Read(Stream source, ImageHeaderV3 fileHeader)
        {
            var table = new RefcountTable();
            table.Entries.Clear();

            var clusterSize = (uint)1U << (int)fileHeader.ClusterBits;

            Console.WriteLine($"Cluster Size: {clusterSize} bytes");

            var refcountBits = (uint)1U << (int)fileHeader.RefcountOrder;
            var refcountBlockEntries = clusterSize * 8 / refcountBits;

            source.Seek((long)fileHeader.RefcountTableOffset, SeekOrigin.Begin);
            for(int i = 0; i < refcountBlockEntries; i++)
            {
                var bits = source.Read<ulong>(true);
                var entry = new RefcountTableEntry(bits);
                table.Entries.Add(entry);
            }

            return table;
        }

        public void Print()
        {
            Console.WriteLine("--- Refcount Table ---");
            for(int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                var blockOffset = entry.RefcountBlockOffset;
                if(blockOffset != 0)
                {
                    Console.WriteLine($"Refcount Block Offset {i}: {blockOffset}");
                }
            }
            Console.WriteLine("--- End of Refcount Table ---");
        }
    }
}