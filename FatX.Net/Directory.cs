using FatX.Net.Enums;
using FatX.Net.Interfaces;
using FatX.Net.Structures;
using InteropHelpers;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FatX.Net
{
    public class Directory : IFileSystemEntry
    {
        private object _initLock = new();
        private bool Initialized = false;
        public Directory? Parent { get; private set; } = null;
        private Filesystem _filesystem;
        public string Name { get; set;  }

        public string FullName => (Parent == null) ? $"{Name}:" : Parent.FullName + Path.DirectorySeparatorChar + Name;

        private long _cluster;

        private List<Directory> _subdirectories = [];
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

        private List<File> _files = [];
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

        public Directory(Directory? parent, Filesystem filesystem, string name, long cluster)
        {
            Parent = parent;
            Name = name;
            _filesystem = filesystem;
            _cluster = cluster;
        }

        public Directory(Filesystem filesystem, string name, long cluster) : this(null, filesystem, name, cluster)
        {
        }

        private async Task InitAsync()
        {
            var cluster = _filesystem.GetCluster(_cluster);
            
            while(cluster.Position < cluster.Length)
            {
                var dirEnt = await StructParser.ReadAsync<DirectoryEntry>(cluster);
                if (dirEnt.Status == DirectoryEntryStatus.EndOfDirMarker ||
                    dirEnt.Status == DirectoryEntryStatus.Available)
                {
                    Initialized = true;
                    return;
                }
                else if(dirEnt.Status == DirectoryEntryStatus.Deleted)
                    continue;

                // Truncate the extra characters
                dirEnt.Filename = dirEnt.Filename.Substring(0, (int)dirEnt.Status);

                if (dirEnt.FileSize > 0)
                    _files.Add(new File(this, _filesystem, dirEnt.Filename, dirEnt.FirstCluster, dirEnt.FileSize));
                else
                    _subdirectories.Add(new Directory(this, _filesystem, dirEnt.Filename, dirEnt.FirstCluster));
            }

            Initialized = true;
            Console.WriteLine("ERROR: Got to the end of the cluster before the end of the Directory");
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

        private void CreateDirectory(string name)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CreateDirectory_Windows(name);
            else
                CreateDirectory_Unix(name);
        }

        private void CreateDirectory_Windows(string name)
        {
            if(!System.IO.Directory.Exists(name))
                System.IO.Directory.CreateDirectory(name);
        }

        [UnsupportedOSPlatform("Windows")]
        private void CreateDirectory_Unix(string name)
        {
            if(!System.IO.Directory.Exists(name))
                System.IO.Directory.CreateDirectory(name, Constants.DefaultDirectoryMode);
        }
    }
}

