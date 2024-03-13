using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class RefcountTable
    {
        public List<RefcountTableEntry> Entries {get; private set;} = [];

        public void Read(FileStream source, FileHeader fileHeader)
        {
            Entries.Clear();

            var clusterSize = (uint)1U << (int)fileHeader.ClusterBits;

            Console.WriteLine($"Cluster Size: {clusterSize} bytes");

            var refcountBits = (uint)1U << (int)fileHeader.RefcountOrder;
            var refcountBlockEntries = (clusterSize * 8 / refcountBits);

            source.Seek((long)fileHeader.RefcountTableOffset, SeekOrigin.Begin);
            for(int i = 0; i < refcountBlockEntries; i++)
            {
                var entry = source.Read<RefcountTableEntry>(true);
                Entries.Add(entry);
            }

            for(int i = 0; i < 128; i++)
            {
                var entry = Entries[i];
                var blockOffset = (entry.Bits & 0x07FFFFFFFFFFFF00L) >> 9;
                if(blockOffset != 0)
                {
                    Console.WriteLine($"Refcount Entry Bits (raw): {entry.Bits:X16}");
                    Console.WriteLine($"Refcount Block Offset: {blockOffset}");
                }
            }
        }
    }
}