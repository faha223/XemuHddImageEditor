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

        private readonly List<uint> _fatCache = [];

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

            ReadFatCache();

            Initialized = true;
        }

        internal ClusterStream GetCluster(uint cluster)
        {
            return new ClusterStream(this, _stream, cluster);
        }

        private void ReadFatCache()
        {
            lock(_stream)
            {
                _stream.Seek(FatOffset, SeekOrigin.Begin);
                long numEntries = NumberOfClusters + Constants.FATX_FAT_ReservedEntriesCount;
                
                if(FatType == 16)
                {
                    var buffer = new byte[numEntries * sizeof(ushort)];
                    _stream.ReadExactly(buffer);
                    for(var i = 0; i < numEntries; i++)
                    {
                        var entry = BitConverter.ToUInt16(buffer, i * sizeof(ushort));
                        var entryAsUint = (uint)entry;
                        if (entryAsUint >= 0x0000FFF0)
                            entryAsUint |= 0xFFFF0000;
                        _fatCache.Add(entryAsUint);
                    }
                }
                else
                {
                    byte[] buffer = new byte[numEntries * sizeof(uint)];
                    _stream.ReadExactly(buffer);
                    
                    for(int i = 0; i < numEntries; i++)
                    {
                        var entry = BitConverter.ToUInt32(buffer, i * sizeof(uint));
                        _fatCache.Add(entry);
                    }
                }
            }
        }

        internal uint GetNextCluster(uint cluster)
        {
            if(cluster >= _fatCache.Count)
                throw new InvalidOperationException("index out of range");

            return _fatCache[(int)cluster];
        }

        internal void FreeClusters(ref DirectoryEntry entry)
        {
            long bytesFreed = 0;
            var cluster = entry.FirstCluster;
            var fileSize = Math.Min(1, entry.FileSize); // Directories have a file size of 0 but they use at least 1 cluster
            while(bytesFreed < fileSize)
            {
                FreeCluster(cluster);
                cluster = GetNextCluster(cluster);
                bytesFreed += BytesPerCluster;
            }
        }

        internal void FreeCluster(uint cluster)
        {
            byte[] newEntry = [ 0x00, 0x00, 0x00, 0x00 ];
            var fatEntrySize = this.FatType == 16 ? sizeof(ushort) : sizeof(uint);
            var fatOffset = fatEntrySize * cluster;

            // Zero out the entry
            lock(_stream)
            {
                _stream.Seek(FatOffset + fatOffset, SeekOrigin.Begin);
                _stream.Write(newEntry, 0, fatEntrySize);
                _stream.Flush();
            }
        }

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
            // TODO: Implement This
            return Task.FromResult(0U);
        }
    }
}
