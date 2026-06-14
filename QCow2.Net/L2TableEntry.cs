using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L2TableEntry(ulong bits, ImageHeaderV3 imageHeader)
    {
        /// <summary>
        ///  0 -  8:    Reserved (set to 0)
        /// 
        ///  9 - 55:    Bits 9-55 of the offset into the image file at which the L2
        ///             table starts. Must be aligned to a cluster boundary. If the
        ///             offset is 0, the L2 table and all clusters described by this
        ///             L2 table are unallocated.
        /// 
        /// 56 - 62:    Reserved (set to 0)
        /// 
        ///      63:    0 for an L2 table that is unused or requires COW, 1 if its
        ///             refcount is exactly one. This information is only accurate
        ///             in the active L1 table.

        /// </summary>
        private ulong _bits = bits;
        private ImageHeaderV3 _imageHeader = imageHeader;

        public bool IsUnallocated => !IsCompressed && !Status;

        public StandardClusterDescriptor StandardClusterDescriptor => new (_bits & Constants.Bits0Through61);

        public CompressedClustersDescriptor CompressedClustersDescriptor => new (_bits & Constants.Bits0Through61, _imageHeader);

        /// <summary>
        /// 0 for standard clusters
        /// 1 for compressed clusters
        /// </summary>
        public bool IsCompressed => (_bits & (1UL << 62)) != 0;

        /// <summary>
        /// 0 for clusters that are unused, compressed or require COW.
        /// 1 for standard clusters whose refcount is exactly one.
        /// 
        /// This information is only accurate in L2 tables 
        /// that are reachable from the active L1 table.
        /// 
        /// With external data files, all guest clusters have an
        /// implicit refcount of 1 (because of the fixed host = guest
        /// mapping for guest cluster offsets), so this bit should be
        /// </summary>
        public bool Status => (_bits & (1UL << 63)) != 0;

        public void Print()
        {
            Console.WriteLine($"L2 Table Entry: {_bits:X16}");
            if(IsUnallocated)
            {
                Console.WriteLine("   Unallocated");
                return;
            }
            Console.WriteLine($"   IsCompressed (bit 62): {IsCompressed}");
            Console.WriteLine($"   Status (bit 63): {Status}");
            if(IsCompressed)
                CompressedClustersDescriptor.Print();
            else
                StandardClusterDescriptor.Print();
        }
    }
}