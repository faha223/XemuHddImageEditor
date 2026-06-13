using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class L2TableEntry(ulong bits, ImageHeaderV3 imageHeader)
    {
        private ulong _bits = bits;
        private ImageHeaderV3 _imageHeader = imageHeader;

        public long HostClusterOffset => (long)(_bits & Constants.Bits9Through55); // Clear the top 2 bits which are used for flags, and the bottom 9 bits which are all 0

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
    }
}