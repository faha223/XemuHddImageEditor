

namespace FatX.Net
{
    public class ClusterStream : Stream
    {
        private Filesystem _filesystem;
        private Stream _stream;
        private long _cluster;
        private long _clusterOffset;

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

        public ClusterStream(Filesystem filesystem, Stream underlyingStream, long cluster)
        {
            _filesystem = filesystem;
            _stream = underlyingStream;
            _cluster = cluster;
            _clusterOffset = _filesystem.ClusterOffset + (cluster - Constants.FATX_FAT_ReservedEntriesCount) * _filesystem.BytesPerCluster;
        }

        public override void Flush()
        {
            lock (_stream)
            {
                _stream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position); // Do not attempt to read past the end of the stream
            lock(_stream)
            {
                _stream.Seek(_clusterOffset + Position, SeekOrigin.Begin);
                int bytesRead = _stream.Read(buffer, offset, count);
                Position += bytesRead;
                return bytesRead;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(buffer, offset, count));
        }

        public ClusterStream? NextCluster()
        {
            long _nextCluster = _filesystem.GetNextCluster(_cluster);
            if (_nextCluster == 0 || _nextCluster >= 0xFFFFFFF0)
                return null;

            return _filesystem.GetCluster(_nextCluster);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.End:
                    Position = _filesystem.BytesPerCluster + offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            // The length of a cluster cannot be changed
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > Length - Position)
                throw new Exception("Not Enough Space Remaining in Cluster");

            lock (_stream)
            {
                _stream.Seek(_clusterOffset + Position, SeekOrigin.Begin);
                _stream.Write(buffer, offset, count);
                Position += count;
            }
        }
    }
}
