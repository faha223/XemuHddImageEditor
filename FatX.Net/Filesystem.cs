using FatX.Net.Helpers;
using FatX.Net.Structures;
using InteropHelpers;

namespace FatX.Net
{
    public class Filesystem(Stream stream, char driveLetter, long offset, long size)
    {
        public char DriveLetter { get; init; } = char.ToUpperInvariant(driveLetter);
        public bool Initialized { get; private set; } = false;
        private readonly Stream _stream = stream;
        private long Offset { get; init; } = offset;
        public long Size { get; init; } = size;

        private Superblock _superblock;

        #region Computed Properties

        public long FatOffset => Offset + Constants.FATX_FAT_Offset;
        public long BytesPerCluster => _superblock.SectorsPerCluster * Constants.SectorSize512;
        public long FatSize { get; private set; }

        public ushort FatType { get; private set; } = 16;

        private Fat _fat { get; set; }

        public long ClusterOffset => FatOffset + FatSize;
        public long NumberOfClusters => ((Size - FatSize - Constants.FATX_FAT_Offset) / BytesPerCluster) + Constants.FATX_FAT_ReservedEntriesCount;
        
        #endregion Computed Properties

        public void Init()
        {
            if(Initialized)
                return;

            Logger.Verbose($"Initializing Partition {DriveLetter}");

            lock(_stream)
            {
                _stream.Seek(Offset, SeekOrigin.Begin);
                _superblock = _stream.Read<Superblock>();
                if(_superblock.Signature != Constants.FATX_Signature)
                    throw new Exception("Invalid FATX Signature");
            }

            Logger.Verbose("Signature Validated");

            FatSize = Size / BytesPerCluster;
            
            if (FatSize < 0xfff0)
            {
                FatType = 16;
                FatSize *= 2;
            }
            else
            {
                FatType = 32;
                FatSize *= 4;
            }

            /* Round FAT size up to nearest 4k boundary. */
            if (FatSize % 4096 != 0)
                FatSize += 4096 - FatSize % 4096;

            Logger.Verbose("Partition Info:");
            Logger.Verbose($"  Partition Offset:    0x{Offset:X16} bytes");
            Logger.Verbose($"  Partition Size:      0x{Size:X16} bytes\n");
            Logger.Verbose($"  Volume Id:           {_superblock.VolumeId:X8}");
            Logger.Verbose($"  Bytes per Sector:    {Constants.SectorSize512}");
            Logger.Verbose($"  # of Sectors:        {_superblock.SectorsPerCluster * Size / BytesPerCluster}");
            Logger.Verbose($"  Sectors per Cluster: {_superblock.SectorsPerCluster}");
            Logger.Verbose($"  # of Clusters:       {NumberOfClusters}");
            Logger.Verbose($"  Bytes per Cluster:   {BytesPerCluster}");
            Logger.Verbose($"  FAT Offset:          {FatOffset} bytes\n");
            Logger.Verbose($"  FAT Size:            {Constants.FATX_FAT_Offset} bytes\n");
            Logger.Verbose($"  FAT Type:            {FatType}");
            Logger.Verbose($"  Root Cluster:        {_superblock.RootCluster}");
            Logger.Verbose($"  Cluster Offset:      0x{ClusterOffset:X8} bytes\n");

            _fat = new Fat(this, _stream);

            Initialized = true;
        }

        private void WriteSuperblock()
        {
            lock(_stream)
            {
                _stream.Seek(Offset, SeekOrigin.Begin);
                _stream.Write(_superblock);
            }
        }

        internal ClusterStream GetCluster(uint cluster) =>
            new (this, _stream, cluster);

        internal uint GetNextCluster(uint cluster) =>
            _fat.GetNextCluster(cluster);

        internal void FreeClusters(ref DirectoryEntry entry) =>
            _fat.FreeClusters(ref entry);

        private Directory? _rootDirectory = null;
        public Task<Directory> GetRootDirectory()
        {
            if(_rootDirectory == null)
            {
                if(!Initialized) Init();
                _rootDirectory = new Directory(this, DriveLetter.ToString(), Constants.FATX_FAT_ReservedEntriesCount);
            }
            return Task.FromResult(_rootDirectory);
        }

        public async Task<List<string>> Search(PathMatcher matcher)
        {
            List<string> searchResults = [];
            List<Directory> list1 = [ await GetRootDirectory() ];
            List<Directory> list2 = [];
            while(list1.Count > 0)
            {
                foreach(var dir in list1)
                {
                    if(matcher.IsMatch(dir.FullName))
                        searchResults.Add(dir.FullName);
                    foreach(var file in dir.Files)
                    {
                        if(matcher.IsMatch(file.FullName))
                        {
                            searchResults.Add(file.FullName);
                        }
                    }
                    list2.AddRange(dir.Subdirectories);
                }

                list1 = list2;
                list2 = [];
            }

            return searchResults;
        }

        public Task<uint> FindAvailableCluster()
        {
            return Task.FromResult(_fat.FindAvailableCluster());
        }

        public Task ReserveCluster(uint cluster)
        {
            // Reserve the cluster by marking it with FATX_CLUSTER_END
            _fat.WriteEntry(cluster, Constants.FATX_CLUSTER_END_32);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This function reserves a number of clusters 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task<uint> AllocateSpace(long length)
        {
            return Task.FromResult(_fat.ReserveSpace(length));
        }
    }
}
