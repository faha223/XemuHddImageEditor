namespace QCow2.Net
{
    public class L1TableEntry(ulong bits)
    {
        private ulong _bits = bits;

        /// <summary>
        /// Bit  0 -  8:    Reserved (set to 0)
        ///      9 - 55:    Bits 9-55 of the offset into the image file at which the L2
        ///                 table starts. Must be aligned to a cluster boundary. If the  
        ///                 offset is 0, the L2 table and all clusters described by this
        ///                 L2 table are unallocated.
        ///
        ///     56 - 62:    Reserved (set to 0)
        ///
        ///          63:    0 for an L2 table that is unused or requires COW, 
        ///                 1 if its refcount is exactly one. This information is only 
        ///                 accurate in the active L1 table.
        /// </summary>
        public ulong ImageOffset => (_bits & Constants.Bits9Through55) >> 9; // Clear the top bit which is used for the refcount flag

        public bool RefcountIsOne => (_bits & (1UL << 63)) != 0;

    }
}