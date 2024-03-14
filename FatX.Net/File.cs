using FatX.Net.Enums;
using FatX.Net.Helpers;
using FatX.Net.Interfaces;
using FatX.Net.Structures;
using InteropHelpers;
using System.Text;

namespace FatX.Net
{
    public class File : IFileSystemEntry
    {
        private readonly Filesystem _filesystem;

        public Directory Parent { get; private set; }

        private readonly long _directoryEntryClusterOffset;

        private DirectoryEntry _directoryEntry;

        public DateTime DateCreated
        {
            get => DatePacker.Unpack(_directoryEntry.CreatedDate, _directoryEntry.CreatedTime);
            set
            {
                DatePacker.Pack(value, out var date, out var time);
                _directoryEntry.CreatedDate = date;
                _directoryEntry.CreatedTime = time;
                RewriteDirectoryEntry();
            }
        }
        
        public DateTime DateModified
        {
            get => DatePacker.Unpack(_directoryEntry.ModifiedDate, _directoryEntry.ModifiedTime);
            set
            {
                DatePacker.Pack(value, out var date, out var time);
                _directoryEntry.ModifiedDate = date;
                _directoryEntry.ModifiedTime = time;
                RewriteDirectoryEntry();
            }
        }
        
        public DateTime DateAccessed
        {
            get => DatePacker.Unpack(_directoryEntry.AccessedDate, _directoryEntry.AccessedTime);
            set
            {
                DatePacker.Pack(value, out var date, out var time);
                _directoryEntry.AccessedDate = date;
                _directoryEntry.AccessedTime = time;
                RewriteDirectoryEntry();
            }
        }

        internal File(Directory parent, Filesystem filesystem, DirectoryEntry directoryEntry,
            long directoryEntryClusterOffset)
        {
            _filesystem = filesystem;
            Parent = parent;
            _directoryEntry = directoryEntry;
            _directoryEntryClusterOffset = directoryEntryClusterOffset;
        }
        
        public string Name
        {
            get => Encoding.ASCII.GetString(_directoryEntry.Filename).Substring(0, (int)_directoryEntry.Status);
            set {
                // Update the DirectoryEntry (filename AND status)
                _directoryEntry.Filename = new byte[Constants.FATX_MaxFilenameLen];
                Array.Copy(Encoding.ASCII.GetBytes(value), _directoryEntry.Filename, value.Length);
                _directoryEntry.Status = (DirectoryEntryStatus)value.Length;

                // Write the changes to the image
                RewriteDirectoryEntry();
            }
        }

        public long FileSize => _directoryEntry.FileSize;

        public string FullName => Parent.FullName + Path.DirectorySeparatorChar + Name;

        public async Task<byte[]> GetContentsAsync()
        {
            var buffer = new byte[_directoryEntry.FileSize];
            long bytesRead = 0;
            var cluster = _filesystem.GetCluster(_directoryEntry.FirstCluster);
            while (bytesRead < _directoryEntry.FileSize)
            {
                bytesRead += await ReadFromClusterAsync(cluster, buffer, bytesRead, _directoryEntry.FileSize - bytesRead);
                if(bytesRead < _directoryEntry.FileSize)
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

        public async Task ExtractToDirectory(string destination)
        {
            destination = Path.Combine(destination, Name);
            await Extract(destination);
        }

        public async Task Extract(string filename)
        {
            using var outStream = new FileStream(filename, FileMode.Create);

            long totalBytesRead = 0;
            var cluster = _filesystem.GetCluster(_directoryEntry.FirstCluster);
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
                            Logger.Error($"Reading File {FullName}: Premature EOF");
                            break;
                        }
                        cluster = nextCluster;
                    }
                }
            }
            outStream.Flush();
            outStream.Close();
        }

        public Task Delete()
        {
            _filesystem.FreeClusters(ref _directoryEntry);
            _directoryEntry.Status = DirectoryEntryStatus.Deleted;
            _directoryEntry.FileSize = 0;
            RewriteDirectoryEntry();
            Parent.Files.Remove(this);

            return Task.CompletedTask;
        }

        private void RewriteDirectoryEntry()
        {
            using var clusterStream = _filesystem.GetCluster(Parent.Cluster);
            clusterStream.Seek(_directoryEntryClusterOffset, SeekOrigin.Begin);
            clusterStream.Write(_directoryEntry);
        }
    }
}