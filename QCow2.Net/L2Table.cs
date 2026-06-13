namespace QCow2.Net
{
    public class L2Table
    {
        private Stream _underlyingStream;
        private ulong _offset;
        private ulong _clusterSize;
        public L2Table(Stream underlyingStream, ulong offset, ulong clusterSize)
        {
            _underlyingStream = underlyingStream;
            _offset = offset;
            _clusterSize = clusterSize;
        }

        public L2TableEntry GetEntry(long offset)
        {
            throw new NotImplementedException();
        }
    }
}