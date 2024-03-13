using FatX.Net.Helpers;
using FatX.Net.Structures;
using InteropHelpers;

namespace FatX.Net
{
    public class Filesystem(Stream stream, long offset, long size)
    {
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

        private readonly List<uint> FatCache = [];

        public long ClusterOffset => FatOffset + FatSize;
        public long NumberOfClusters => ((Size - FatSize - Constants.FATX_FAT_Offset) / BytesPerCluster) + Constants.FATX_FAT_ReservedEntriesCount;
        
        #endregion Computed Properties

        public void Init(string DriveLetter)
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

        internal ClusterStream GetCluster(long cluster)
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
                    byte[] buffer = new byte[numEntries * sizeof(ushort)];
                    _stream.Read(buffer, 0, buffer.Length);
                    for(int i = 0; i < numEntries; i++)
                    {
                        var entry = BitConverter.ToUInt16(buffer, i * sizeof(ushort));
                        var entryAsUint = (uint)entry;
                        if (entryAsUint >= 0x0000FFF0)
                            entryAsUint |= 0xFFFF0000;
                        FatCache.Add(entryAsUint);
                    }
                }
                else
                {
                    byte[] buffer = new byte[numEntries * sizeof(uint)];
                    _stream.Read(buffer, 0, buffer.Length);
                    for(int i = 0; i < numEntries; i++)
                    {
                        var entry = BitConverter.ToUInt32(buffer, i * sizeof(uint));
                        FatCache.Add(entry);
                    }
                }
            }
        }

        internal long GetNextCluster(long cluster)
        {
            if(cluster < 0 || cluster >= FatCache.Count)
                throw new InvalidOperationException("index out of range");

            return FatCache[(int)cluster];
        }

        private Directory? rootDirectory = null;
        public Task<Directory> GetRootDirectory(string DriveLetter)
        {
            if(rootDirectory == null)
            {
                if(!Initialized)
                    Init(DriveLetter);
                rootDirectory = new Directory(this, DriveLetter, Constants.FATX_FAT_ReservedEntriesCount);
            }
            return Task.FromResult(rootDirectory);
        }

        public async Task<List<string>> Search(string driveLetter, PathMatcher matcher)
        {
            List<string> searchResults = [];
            List<Directory> list1 = [ await GetRootDirectory(driveLetter) ];
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
    }
}
