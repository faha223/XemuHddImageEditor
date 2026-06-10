using FatX.Net.Enums;
using FatX.Net.Helpers;
using FatX.Net.Interfaces;
using FatX.Net.Structures;
using InteropHelpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace FatX.Net
{
    public class Directory : IFileSystemEntry
    {
        private readonly object _initLock = new();
        private bool _initialized = false;
        public Directory? Parent { get; private set; } = null;
        private readonly Filesystem _filesystem;

        private readonly long _directoryEntryClusterOffset;

        private DirectoryEntry _directoryEntry;

        public string Name 
        {
            get {
                if (_directoryEntry.Status == 0 ||
                    (int)_directoryEntry.Status > Constants.FATX_MaxFilenameLen)
                    return string.Empty;
                return Encoding.ASCII.GetString(_directoryEntry.Filename).Substring(0, (int)_directoryEntry.Status);
            }
            set
            {
                if(Parent != null)
                {
                    // Update the DirectoryEntry (filename AND status)
                    _directoryEntry.Filename = new byte[Constants.FATX_MaxFilenameLen];
                    Array.Copy(Encoding.ASCII.GetBytes(value), _directoryEntry.Filename, value.Length);
                    _directoryEntry.Status = (DirectoryEntryStatus)value.Length;

                    // Write the changes to the image
                    RewriteDirectoryEntry();
                }
            }
        }

        public string FullName => (Parent == null) ? $"{Name}:" : Parent.FullName + Path.DirectorySeparatorChar + Name;

        public uint Cluster => _directoryEntry.FirstCluster;

        private readonly List<Directory> _subdirectories = [];
        public List<Directory> Subdirectories
        {
             get {
                lock(_initLock)
                {
                    if(!_initialized)
                        InitAsync().Wait();
                }
                return _subdirectories;
             }
        }

        private readonly List<File> _files = [];
        public List<File> Files 
        {
            get{
                lock(_initLock)
                {
                    if(!_initialized)
                        InitAsync().Wait();
                }
                return _files;
            }
        }

        internal Directory(Filesystem filesystem, string name, uint cluster)
        {
            _filesystem = filesystem;
            _directoryEntry.Filename = new byte[Constants.FATX_MaxFilenameLen];
            Array.Copy(Encoding.ASCII.GetBytes(name), _directoryEntry.Filename, name.Length);
            _directoryEntry.Status = (DirectoryEntryStatus)name.Length;
            _directoryEntry.FirstCluster = cluster;
        }

        private Directory(Directory? parent, Filesystem filesystem, DirectoryEntry directoryEntry, long directoryEntryClusterOffset)
        {
            Parent = parent;
            _filesystem = filesystem;
            _directoryEntry = directoryEntry;
            _directoryEntryClusterOffset = directoryEntryClusterOffset;
        }

        private Task InitAsync()
        {
            if(_directoryEntry.FirstCluster == 0)
            {
                _initialized = true;
                return Task.CompletedTask;
            }

            _files.Clear();
            _subdirectories.Clear();

            var cluster = _filesystem.GetCluster(_directoryEntry.FirstCluster);
            
            while(cluster.Position < cluster.Length)
            {
                var directoryEntryLocation = cluster.Position;
                var directoryEntry = cluster.Read<DirectoryEntry>();
                switch (directoryEntry.Status)
                {
                    case DirectoryEntryStatus.EndOfDirMarker:
                    case DirectoryEntryStatus.Available:
                        _initialized = true;
                        return Task.CompletedTask;
                    case DirectoryEntryStatus.Deleted:
                        continue;
                }

                if (directoryEntry.FileSize > 0)
                    _files.Add(new File(this, _filesystem, directoryEntry, directoryEntryLocation));
                else
                    _subdirectories.Add(new Directory(this, _filesystem, directoryEntry, directoryEntryLocation));
            }

            _initialized = true;
            Logger.Error("Got to the end of the cluster before the end of the Directory");
            return Task.CompletedTask;
        }

        public void Refresh()
        {
            Logger.Verbose("Refreshing directory " + FullName);
            lock(_initLock)
            {
                InitAsync().Wait();
            }
        }

        public async Task PrintTree()
        {
            Console.WriteLine(FullName);
            foreach(var file in Files)
                Console.WriteLine(file.FullName);
            foreach(var subdirectory in Subdirectories)
                await subdirectory.PrintTree();
        }

        #region Extract
        
        public async Task Extract(string destination) => await Extract(destination, false);

        public async Task Extract(string dest, bool recursive)
        {
            dest = Path.Combine(dest, Name);

            CreateDirectory(dest);
            if (recursive)
            {
                foreach (var file in Files)
                    await file.ExtractToDirectory(dest);
                foreach (var subdirectory in Subdirectories)
                    await subdirectory.Extract(dest, recursive);
            }
        }
        
        #endregion Extract

        #region Create Directory
        private static void CreateDirectory(string name)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CreateDirectory_Windows(name);
            else
                CreateDirectory_Unix(name);
        }

        private static void CreateDirectory_Windows(string name)
        {
            if(!System.IO.Directory.Exists(name))
                System.IO.Directory.CreateDirectory(name);
        }

        [UnsupportedOSPlatform("Windows")]
        private static void CreateDirectory_Unix(string name)
        {
            if(!System.IO.Directory.Exists(name))
                System.IO.Directory.CreateDirectory(name, Constants.DefaultDirectoryMode);
        }

        #endregion Create Directory
        
        #region Delete
        
        public async Task Delete()
        {
            if (Parent == null)
                return;

            Logger.Verbose($"Deleting directory {FullName}");

            await InitAsync();
            // Make a copy to avoid modifying the collection while iterating
            List<Directory> subdirectoriesCopy = [..Subdirectories];
            foreach (var subdirectory in subdirectoriesCopy)
                await subdirectory.Delete();
            Subdirectories.Clear();

            // Make a copy to avoid modifying the collection while iterating
            List<File> filesCopy = [..Files];
            foreach (var file in filesCopy)
                await file.Delete();
            Files.Clear();

            _directoryEntry.Status = DirectoryEntryStatus.Deleted;
            Logger.Verbose($"Writing Directory Entry {FullName} as deleted");
            RewriteDirectoryEntry();

            // You can't delete a root directory
            Parent.Refresh();

            Logger.Verbose($"Finished deleting directory {FullName}");
        }

        #endregion Delete

        #region Create File

        /// <summary>
        /// Create a new file inside this directory
        /// </summary>
        /// <param name="filename">The name of the fle to be created</param>
        /// <param name="content">The contents of the file</param>
        /// <returns>A File instance if one was able to be created. Null if not.</returns>
        public async Task<File?> CreateFile(FileInfo file, Stream content)
        {
            Logger.Verbose($"Creating File {file.Name} in directory {FullName}");
            var fileSize = content.Length;
            Logger.Verbose($"File size is {fileSize} bytes");
            Logger.Verbose($"Bytes per Cluster: {_filesystem.BytesPerCluster}");
            var requiredClusters = (uint)((fileSize + _filesystem.BytesPerCluster - 1) / _filesystem.BytesPerCluster);
            Logger.Verbose($"File requires {requiredClusters} clusters");
            var firstClusterId = await _filesystem.AllocateSpace(fileSize);
            if(firstClusterId == Constants.FATX_CLUSTER_END_32)
            {
                Logger.Error($"Not enough space to create file {file.Name} in directory {FullName}");
                return null;
            }

            uint clusterId = firstClusterId;
            while(content.Position < content.Length)
            {
                var buffer = new byte[_filesystem.BytesPerCluster];
                using (var clusterStream = _filesystem.GetCluster(clusterId))
                {
                    var bytesToWrite = (int)Math.Min(_filesystem.BytesPerCluster, content.Length - content.Position);
                    Logger.Verbose($"Writing {bytesToWrite} bytes to cluster {clusterId}");
                    content.ReadExactly(buffer, 0, bytesToWrite);
                    clusterStream.Write(buffer, 0, bytesToWrite);
                    clusterStream.Flush();
                }

                if(content.Position < content.Length)
                {
                    clusterId = _filesystem.GetNextCluster(clusterId);
                    Logger.Verbose($"Next cluster is {clusterId}");
                    if(clusterId == Constants.FATX_CLUSTER_END_32)
                    {
                        Logger.Error($"Not enough space to create file {file.Name} in directory {FullName}");
                        return null;
                    }
                }
            }

            DatePacker.Pack(file.CreationTimeUtc, out var creationDateBytes, out var creationTimeBytes);
            DatePacker.Pack(file.LastWriteTimeUtc, out var writeDateBytes, out var writeTimeBytes);
            DatePacker.Pack(file.LastAccessTimeUtc, out var accessDateBytes, out var accessTimeBytes);
            var directoryEntry = new DirectoryEntry()
            {
                Status = (DirectoryEntryStatus)file.Name.Length,
                Filename = new byte[Constants.FATX_MaxFilenameLen],
                Attributes = 0,
                CreatedDate = creationDateBytes,
                CreatedTime = creationTimeBytes,
                ModifiedDate = writeDateBytes,
                ModifiedTime = writeTimeBytes,
                AccessedDate = accessDateBytes,
                AccessedTime = accessTimeBytes,
                FirstCluster = firstClusterId,
                FileSize = (uint)fileSize
            };

            Array.Copy(Encoding.ASCII.GetBytes(file.Name), directoryEntry.Filename, file.Name.Length);
            directoryEntry.Filename[file.Name.Length] = 0; // Null terminator for the filename

            // Find an available DirectoryEntry in the current directory
            Logger.Verbose($"Finding an available DirectoryEntry in directory {FullName} for new file {file.Name}");
            using var directoryClusterStream = _filesystem.GetCluster(Cluster);
            (int offset, DirectoryEntry? entry) = FindAvailableDirectoryEntryOffset(directoryClusterStream);
            if (offset < directoryClusterStream.Length && entry.HasValue)
            {
                Logger.Verbose($"Found an available DirectoryEntry at offset {offset}");
                
                // This offset is where we'll write the DirectoryEntry for the new file
                directoryClusterStream.Seek(offset, SeekOrigin.Begin);
                directoryClusterStream.Write(directoryEntry);

                if(entry.Value.Status == DirectoryEntryStatus.EndOfDirMarker)
                {
                    Logger.Verbose($"Available DirectoryEntry was an End of Directory marker, so writing a new End of Directory marker after the new file");
                    if(directoryClusterStream.Position < directoryClusterStream.Length)
                        directoryClusterStream.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker);
                    else
                    {
                        // Add another cluster to this cluster chain, and write the new End of Directory marker there
                        uint newClusterId = _filesystem.AddClusterToChain(Cluster);
                        if(newClusterId != 0)
                        {
                            using var newClusterStream = _filesystem.GetCluster(newClusterId);
                            newClusterStream.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker);
                        }
                    }
                }
                await directoryClusterStream.FlushAsync();
                var newFile = new File(this, _filesystem, directoryEntry, directoryClusterStream.Position - Marshal.SizeOf<DirectoryEntry>());
                Files.Add(newFile);
                return newFile;
            }

            return null;
        }

        /// <summary>
        /// Create a new file inside this directory
        /// </summary>
        /// <param name="filename">The name of the fle to be created</param>
        /// <param name="content">The contents of the file</param>
        /// <returns>A File instance if one was able to be created. Null if not.</returns>
        public async Task<File?> CreateFile(FileInfo file, byte[] content) =>
            await CreateFile(file, new MemoryStream(content));

        #endregion Create File
        
        #region Create Subdirectory
        
        /// <summary>
        /// Create a new directory inside this one
        /// </summary>
        /// <param name="name">The name of the subdirectory that is to be created</param>
        /// <returns>a Directory instance if one was able to be created. Null if not.</returns>
        public async Task<Directory?> CreateSubdirectory(string name)
        {
            // Find an available cluster
            Logger.Verbose($"Finding an avalable cluster for new subdirectory {name} in directory {FullName}");
            var clusterId = await _filesystem.AllocateSpace(1);
            if(clusterId == Constants.FATX_CLUSTER_END_32)
            {
                Logger.Error($"Not enough space to create subdirectory {name} in directory {FullName}");
                return null;
            }
            Logger.Verbose($"Found an available cluster at ClusterId {clusterId} for new subdirectory {name}");

            // Write the 'End of Directory' entry to the new cluster
            Logger.Verbose($"Getting cluster stream for clusterId {clusterId} to write End of Directory entry for new subdirectory {name}");
            var subdirectoryCluster = _filesystem.GetCluster(clusterId);

            Logger.Verbose($"Packing current date and time for the creation, modification, and access times of the new subdirectory.");
            DatePacker.Pack(DateTime.Now, out var dateBytes, out var timeBytes);
            Logger.Verbose($"Writing End of Directory entry to cluster {clusterId} for new subdirectory {name}");
            subdirectoryCluster.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker);
            
            Logger.Verbose($"Creating DirectoryEntry for new subdirectory {name}");
            var directoryEntry = new DirectoryEntry()
            {
                Status = (DirectoryEntryStatus)name.Length,
                Filename = new byte[Constants.FATX_MaxFilenameLen],
                Attributes = Attributes.Directory,
                CreatedDate = dateBytes,
                CreatedTime = timeBytes,
                ModifiedDate = dateBytes,
                ModifiedTime = timeBytes,
                AccessedDate = dateBytes,
                AccessedTime = timeBytes,
                FirstCluster = clusterId,
                FileSize = 0
            };

            Array.Copy(Encoding.ASCII.GetBytes(name), directoryEntry.Filename, name.Length);
            directoryEntry.Filename[name.Length] = 0; // Null terminator for the filename

            // Find an available DirectoryEntry in the current directory
            Logger.Verbose($"Finding an available DirectoryEntry in directory {FullName} for new subdirectory {name}");
            using var clusterStream = _filesystem.GetCluster(Cluster);
            (int offset, DirectoryEntry? entry) = FindAvailableDirectoryEntryOffset(clusterStream);
            if(offset < clusterStream.Length && entry.HasValue)
            {
                // This offset is where we'll write the DirectoryEntry for the new Subdirectory
                clusterStream.Seek(offset, SeekOrigin.Begin);
                clusterStream.Write(directoryEntry);

                if(entry.Value.Status == DirectoryEntryStatus.EndOfDirMarker)
                {
                    Logger.Verbose($"Available DirectoryEntry was an End of Directory marker, so writing a new End of Directory marker after the new subdirectory");
                    if(clusterStream.Position < clusterStream.Length)
                        clusterStream.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker);
                    else
                    {
                        // Add another cluster to this cluster chain, and write the new End of Directory marker there
                        uint newClusterId = _filesystem.AddClusterToChain(Cluster);
                        if(newClusterId != 0)
                        {
                            using var newClusterStream = _filesystem.GetCluster(newClusterId);
                            newClusterStream.WriteByte((byte)DirectoryEntryStatus.EndOfDirMarker);
                        }
                    }
                }
                await clusterStream.FlushAsync();

                var newDirectory = new Directory(this, _filesystem, directoryEntry, clusterStream.Position - Marshal.SizeOf<DirectoryEntry>());
                Subdirectories.Add(newDirectory);
                return newDirectory;
            }

            Logger.Error($"No available directoryEntry could be found. Deleting the subdirectory");
            _filesystem.FreeClusters(ref directoryEntry);
            // Directory Entry is full. Add a cluster to this directory entry and add the new DirectoryEntry to that
            return null;
        }
        
        #endregion Create Subdirectory

        private static (int offset, DirectoryEntry? entry) FindAvailableDirectoryEntryOffset(ClusterStream directoryClusterStream)
        {
            for(int offset = 0; offset < directoryClusterStream.Length; offset += Marshal.SizeOf<DirectoryEntry>())
            {
                directoryClusterStream.Seek(offset, SeekOrigin.Begin);
                var entry = directoryClusterStream.Read<DirectoryEntry>();
                if (entry.Status == DirectoryEntryStatus.Available || 
                    entry.Status == DirectoryEntryStatus.Deleted ||
                    entry.Status == DirectoryEntryStatus.EndOfDirMarker)
                {
                    Logger.Verbose($"Found an available DirectoryEntry at offset {offset}");
                    return (offset, entry);
                }
            }

            // No available DirectoryEntry was found, so return the offset at the end of the cluster stream 
            // (where a new DirectoryEntry would be written if a new cluster is added to this directory) 
            return ((int)directoryClusterStream.Length, null); 
        }
        
        private void RewriteDirectoryEntry()
        {
            // Root Directories don't have proper Directory Entries
            if (Parent == null)
                return;

            using var clusterStream = _filesystem.GetCluster(Parent.Cluster);
            clusterStream.Seek(_directoryEntryClusterOffset, SeekOrigin.Begin);
            clusterStream.Write(_directoryEntry);
        }
    }
}

