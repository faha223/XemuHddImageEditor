using System.Runtime.InteropServices;
using InteropHelpers;
using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L2Table(Stream underlyingStream, ulong offset, ImageHeaderV3 imageHeader)
    {
        private Stream _underlyingStream = underlyingStream;
        private ulong _offset = offset;
        private ulong _clusterSize = (ulong)(1 << (int)imageHeader.ClusterBits);
        private ImageHeaderV3 _imageHeader = imageHeader;

        public L2TableEntry GetEntry(long offset)
        {
            _underlyingStream.Seek((long)_offset + offset, SeekOrigin.Begin);
            var bits = _underlyingStream.Read<L2TableEntryBits>(true);
            var entry = new L2TableEntry(bits.Bits, _imageHeader);
            entry.Print();
            return entry;
        }

        public void Print()
        {
            Console.WriteLine("--- L2 Table ---");
            _underlyingStream.Seek((long)_offset, SeekOrigin.Begin);
            var numEntries = (int)(_clusterSize / (ulong)Marshal.SizeOf<ulong>());
            for(int i = 0; i < numEntries; i++)
            {
                var bits = _underlyingStream.Read<L2TableEntryBits>(true);
                var entry = new L2TableEntry(bits.Bits, _imageHeader);
                entry.Print();
            }
            Console.WriteLine("--- End of L2 Table ---");
        }
    }
}