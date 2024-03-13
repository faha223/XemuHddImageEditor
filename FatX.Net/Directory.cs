using FatX.Net.Enums;
using FatX.Net.Helpers;
using FatX.Net.Interfaces;
using FatX.Net.Structures;
using InteropHelpers;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace FatX.Net
{
    public class Directory : IFileSystemEntry
    {
        private readonly object _initLock = new();
        private bool Initialized = false;
        public Directory? Parent { get; private set; } = null;
        private readonly Filesystem _filesystem;

        private readonly long _directoryEntryClusterOffset;

        private DirectoryEntry _directoryEntry;

        public string Name 
        {
            get => Encoding.ASCII.GetString(_directoryEntry.Filename).Substring(0, (int)_directoryEntry.Status);
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

        public long Cluster => _directoryEntry.FirstCluster;

        private readonly List<Directory> _subdirectories = [];
        public List<Directory> Subdirectories
        {
             get {
                lock(_initLock)
                {
                    if(!Initialized)
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
                    if(!Initialized)
                        InitAsync().Wait();
                }
                return _files;
            }
        }

        public Directory(Filesystem filesystem, string name, uint cluster)
        {
            _filesystem = filesystem;
            _directoryEntry.Filename = new byte[Constants.FATX_MaxFilenameLen];
            Array.Copy(Encoding.ASCII.GetBytes(name), _directoryEntry.Filename, name.Length);
            _directoryEntry.Status = (DirectoryEntryStatus)name.Length;
            _directoryEntry.FirstCluster = cluster;
        }

        public Directory(Directory? parent, Filesystem filesystem, DirectoryEntry directoryEntry, long directoryEntryClusterOffset)
        {
            Parent = parent;
            _filesystem = filesystem;
            _directoryEntry = directoryEntry;
            _directoryEntryClusterOffset = directoryEntryClusterOffset;
        }

        private Task InitAsync()
        {
            var cluster = _filesystem.GetCluster(_directoryEntry.FirstCluster);
            
            while(cluster.Position < cluster.Length)
            {
                long directoryEntryLocation = cluster.Position;
                var directoryEntry = cluster.Read<DirectoryEntry>();
                if (directoryEntry.Status == DirectoryEntryStatus.EndOfDirMarker ||
                    directoryEntry.Status == DirectoryEntryStatus.Available)
                {
                    Initialized = true;
                    return Task.CompletedTask;
                }
                else if(directoryEntry.Status == DirectoryEntryStatus.Deleted)
                    continue;

                if (directoryEntry.FileSize > 0)
                    _files.Add(new File(this, _filesystem, directoryEntry, directoryEntryLocation));
                else
                    _subdirectories.Add(new Directory(this, _filesystem, directoryEntry, directoryEntryLocation));
            }

            Initialized = true;
            Logger.Error("Got to the end of the cluster before the end of the Directory");
            return Task.CompletedTask;
        }

        public async Task PrintTree()
        {
            Console.WriteLine(FullName);
            foreach(var file in Files)
                Console.WriteLine(file.FullName);
            foreach(var subdir in Subdirectories)
                await subdir.PrintTree();
        }

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

        public async Task Delete()
        {
            if (Parent == null)
                return;

            foreach (var subdirectory in Subdirectories)
                await subdirectory.Delete();
            Subdirectories.Clear();

            foreach (var file in Files)
                await file.Delete();
            Files.Clear();

            _directoryEntry.Status = DirectoryEntryStatus.Deleted;
            RewriteDirectoryEntry();

            // You can't delete a root directory
            Parent.Subdirectories.Remove(this);
        }

        private void RewriteDirectoryEntry()
        {
            if (Parent == null)
                return;

            using var clusterStream = _filesystem.GetCluster(Parent.Cluster);
            clusterStream.Seek(_directoryEntryClusterOffset, SeekOrigin.Begin);
            clusterStream.Write(_directoryEntry);
        }
    }
}

