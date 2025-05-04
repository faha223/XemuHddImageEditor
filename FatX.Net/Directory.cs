using FatX.Net.Enums;
using FatX.Net.Helpers;
using FatX.Net.Interfaces;
using FatX.Net.Structures;
using InteropHelpers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
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

        #endregion Delete

        #region Create File

        /// <summary>
        /// Create a new file inside this directory
        /// </summary>
        /// <param name="filename">The name of the fle to be created</param>
        /// <param name="content">The contents of the file</param>
        /// <returns>A File instance if one was able to be created. Null if not.</returns>
        public Task<File?> CreateFile(FileInfo file, Stream content)
        {
            System.Diagnostics.Debug.WriteLine($"Creating File {file.Name} in directory {FullName}");
            // TODO: Implement This
            return Task.FromResult<File?>(null);
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
        public Task<Directory?> CreateSubdirectory(string name)
        {
            // TODO: Find an available cluster
            // TODO: Write the End of Directory entry to the new cluster
            // TODO: Find an available DirectoryEntry in the current directory
            // TODO: Write a new DirectoryEntry into the available space for this new directory
            return Task.FromResult<Directory?>(null);
        }
        
        #endregion Create Subdirectory
        
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

