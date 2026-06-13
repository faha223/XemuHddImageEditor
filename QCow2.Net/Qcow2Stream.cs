namespace QCow2.Net
{
    public class QCow2Stream : Stream
    {
        private Stream _stream;
        private QCow2Image _image;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => (long)_image.ImageHeader.Size;

        private long _position;
        public override long Position 
        {  
            get => _position;
            set { _position = value; }
        }

        public QCow2Stream(Stream underlyingStream)
        {
            _stream = underlyingStream;
            _image = new QCow2Image(underlyingStream);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long bytesRead = Math.Clamp(count, 0, Length - Position);
            Position += bytesRead;
            return (int)bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                // new position is equal to offset, clamped to the interval [0, Size]
                SeekOrigin.Begin => Math.Clamp(offset, 0, Length),    
                // new position is equal to offset, clamped to the interval [0, Size]
                SeekOrigin.End => Math.Clamp(Length + offset, 0, Length),
                // new position is equal to the current position plus the offset, clamped to the interval [0, Size]
                SeekOrigin.Current => Math.Clamp(Position + offset, 0, Length),
                _ => throw new ArgumentException("Invalid SeekOrigin", nameof(origin))
            };
            return Position;
        }

        public override void SetLength(long value)
        {
            // Length cannot be changed as it is determined by the image header
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Writing is not implemented at this time
            throw new NotImplementedException();
        }
    }
}