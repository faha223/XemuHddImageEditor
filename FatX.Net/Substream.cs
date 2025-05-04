using System.Security.Cryptography.X509Certificates;
using FatX.Net.Structures;

namespace FatX.Net
{
    public abstract class Substream(Stream underlyingStream, long offset, long size) : Stream
    {
        public Stream UnderlyingStream { get; init; } = underlyingStream;
        public long Offset { get; init; } = offset;
        public long Size { get; init; } = size;

        public override bool CanRead => UnderlyingStream.CanRead;

        public override bool CanSeek => UnderlyingStream.CanSeek;

        public override bool CanWrite => UnderlyingStream.CanWrite;

        public override long Length => Size;

        public override long Position { get; set; }

        public override void Flush()
        {
            lock(UnderlyingStream)
            {
                UnderlyingStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = (int)Math.Min(count, Length - Position); // Do not attempt to read past the end of the stream
            lock(UnderlyingStream)
            {
                UnderlyingStream.Seek(Offset + Position, SeekOrigin.Begin);
                int bytesRead = UnderlyingStream.Read(buffer, offset, count);
                Position += bytesRead;
                return bytesRead;
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(buffer, offset, count));
        }

        /// <summary>
        /// This function changes the current position in the Stream to value provided by the offset argument, relative to the origin specified
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    // new position is equal to offset, clamped to the interval [0, Size]
                    Position = Math.Clamp(offset, 0, Size); 
                    break;
                case SeekOrigin.End:
                    // new position is equal to offset, clamped to the interval [0, Size]
                    Position = Math.Clamp(Size + offset, 0, Size); // 
                    break;
                case SeekOrigin.Current:
                    // new position is equal to the current position plus the offset, clamped to the interval [0, Size]
                    Position = Math.Clamp(Position + offset, 0, Size);
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            // Do nothing. Substream sizes cannot be changed
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > Length - Position)
                throw new Exception("Not Enough Space Remaining in Substream");

            lock (UnderlyingStream)
            {
                UnderlyingStream.Seek(Offset + Position, SeekOrigin.Begin);
                UnderlyingStream.Write(buffer, offset, count);
                UnderlyingStream.Flush();
                Position += count;
            }
        }
    }
}