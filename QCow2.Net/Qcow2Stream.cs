namespace QCow2.Net
{
    public class Qcow2Stream : Stream
    {
        private Stream _stream;
        private QCow2Image _image;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => (long)_image.fileHeader.Size;

        private long _position;
        public override long Position 
        {  
            get => _position;
            set { _position = value; }
        }

        public Qcow2Stream(Stream underlyingStream)
        {
            _stream = underlyingStream;
            _image = new QCow2Image(underlyingStream);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}