

namespace FatX.Net
{
    public class ClusterStream : Substream
    {
        private readonly Filesystem _filesystem;
        private readonly uint _cluster;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _filesystem.BytesPerCluster;

        private long _position = 0;
        public override long Position 
        { 
            get => _position;
            set { _position = value; }
        }

        public ClusterStream(Filesystem filesystem, Stream underlyingStream, uint cluster) : base(underlyingStream, filesystem.ClusterOffset + (cluster - Constants.FATX_FAT_ReservedEntriesCount) * filesystem.BytesPerCluster, filesystem.BytesPerCluster)
        {
            _filesystem = filesystem;
            _cluster = cluster;
        }

        public ClusterStream? NextCluster()
        {
            uint _nextCluster = _filesystem.GetNextCluster(_cluster);
            if (_nextCluster == 0 || _nextCluster >= 0xFFFFFFF0)
                return null;

            return _filesystem.GetCluster(_nextCluster);
        }
    }
}
