using QCow2.Net.Structures;

namespace QCow2.Net
{
    public class CompressedClustersDescriptor(ulong bits, ImageHeaderV3 imageHeader)
    {
        /// <summary>
        /// Bit  0 - x-1:   Host cluster offset. This is usually _not_ aligned to a
        /// cluster or sector boundary!  If cluster_bits is
        /// small enough that this field includes bits beyond
        /// 55, those upper bits must be set to 0.
        ///
        /// x - 61:    Number of additional 512-byte sectors used for the
        /// compressed data, beyond the sector containing the offset
        /// in the previous field. Some of these sectors may reside
        /// in the next contiguous host cluster.
        ///
        /// Note that the compressed data does not necessarily occupy
        /// all of the bytes in the final sector; rather, decompression
        /// stops when it has produced a cluster of data.
        ///
        /// Another compressed cluster may map to the tail of the final
        /// sector used by this compressed cluster.
        /// </summary>
        private ulong _bits = bits;

        private ImageHeaderV3 _imageHeader = imageHeader;

        private uint X => 62 - (_imageHeader.ClusterBits - 8);

        public ulong HostClusterOffset
        {
            get
            {
                ulong hco = 0;
                uint x = X;
                for(int i = 0; i < x; i++)
                    hco |= ((_bits >> i) & 1) << i;
                return hco;
            }
        }

        public ulong AdditionalSectors
        {
            get
            {
                ulong additionalSectors = 0;
                int x = (int)X;
                for(int i = 0; i < 62 - x; i++)
                    additionalSectors |= ((_bits >> (x + i)) & 1) << i;
                return additionalSectors;
            }
        }

        public bool IsUnallocated
        {
            get
            {
                return true;
            }
        }

        public void Print()
        {
            Console.WriteLine($"Compressed Clusters Descriptor: {_bits:X16}");
            Console.WriteLine($"   HostClusterOffset (bits 0-{X-1}): {HostClusterOffset:X16}");
            Console.WriteLine($"   AdditionalSectors (bits {X}-61): {AdditionalSectors}");
        }
    }
}