using System.IO;

namespace FatX.Net
{
    public class File
    {
        private Filesystem _filesystem;

        public Directory Parent { get; private set; }

        private long _firstCluster;

        public long FileSize { get; private set; }

        public string Name { get; set; }

        public string FullName => Parent.FullName + Path.DirectorySeparatorChar + Name;

        public File(Directory parent, Filesystem filesystem, string name, long firstCluster, long fileSize)
        {
            Parent = parent;
            _filesystem = filesystem;
            Name = name;
            _firstCluster = firstCluster;
            FileSize = fileSize;
        }

        public async Task<byte[]> GetContentsAsync()
        {
            var buffer = new byte[FileSize];
            long bytesRead = 0;
            var cluster = _filesystem.GetCluster(_firstCluster);
            while (bytesRead < FileSize)
            {
                bytesRead += await ReadFromClusterAsync(cluster, buffer, bytesRead, FileSize - bytesRead);
                if(bytesRead < FileSize)
                {
                    cluster = cluster.NextCluster();
                    if(cluster == null)
                    {
                        // Cannot read file
                        return Array.Empty<byte>();
                    }
                }
            }
            return buffer;
        }

        private async Task<long> ReadFromClusterAsync(ClusterStream cluster, byte[] buffer, long offset, long count)
        {
            // Do not try to read more bytes from this cluster than there are bytes in a cluster   
            count = Math.Min(count, _filesystem.BytesPerCluster);
            
            long totalBytesRead = 0;
            while(totalBytesRead < count)
            {
                long bytesRead = await cluster.ReadAsync(buffer, (int)offset, (int)(count - totalBytesRead));
                if(bytesRead < 0)
                    throw new Exception("Error Reading from Stream");
                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }

        public async Task ExtractToDirectory(string dest)
        {
            dest = Path.Combine(dest, Name);
            await Extract(dest);
        }

        public async Task Extract(string filename)
        {
            using var outStream = new FileStream(filename, FileMode.Create);

            long totalBytesRead = 0;
            var cluster = _filesystem.GetCluster(_firstCluster);
            var buffer = new byte[_filesystem.BytesPerCluster];
            while (totalBytesRead < FileSize)
            {
                var chunkSize = Math.Min(FileSize - totalBytesRead, _filesystem.BytesPerCluster);
                var bytesRead = await ReadFromClusterAsync(cluster, buffer, 0, chunkSize);
                if(bytesRead > 0)
                {
                    await outStream.WriteAsync(buffer, 0, (int)bytesRead);
                    totalBytesRead += bytesRead;
                    if(totalBytesRead < FileSize)
                    {
                        var nextCluster = cluster.NextCluster();
                        if(nextCluster == null)
                        {
                            // Cannot read file
                            Console.WriteLine("Error Reading File {FullName}: Premature EOF");
                            break;
                        }
                        cluster = nextCluster;
                    }
                }
            }
            outStream.Flush();
            outStream.Close();
        }
    }
}