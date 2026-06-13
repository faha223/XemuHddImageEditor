namespace QCow2.Net
{
    public class StandardClusterDecriptor(ulong bits)
    {
        /// <summary>
        /// Bit       0:    If set to 1, the cluster reads as all zeros. The host
        /// cluster offset can be used to describe a preallocation,
        /// but it won't be used for reading data from this cluster,
        /// nor is data read from the backing file if the cluster is
        /// unallocated.
        /// 
        /// With version 2 or with extended L2 entries (see the next
        /// section), this is always 0.
        /// 
        /// 1 -  8:    Reserved (set to 0)
        /// 
        /// 9 - 55:    Bits 9-55 of host cluster offset. Must be aligned to a
        /// cluster boundary. If the offset is 0 and bit 63 is clear,
        /// the cluster is unallocated. The offset may only be 0 with
        /// bit 63 set (indicating a host cluster offset of 0) when an
        /// external data file is used.
        /// 
        /// 56 - 61:    Reserved (set to 0)
        /// </summary>
        private ulong _bits = bits;
        
        public long ClusterOffset => (long)(_bits & Constants.Bits9Through55); // Clear the top 2 bits which are used for flags
    }
}