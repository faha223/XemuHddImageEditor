using System.Reflection.Metadata;
using FatX.Net.Structures;

namespace FatX.Net
{
    public class Fat(Filesystem filesystem, Stream stream) : Substream(stream, filesystem.FatOffset, filesystem.FatSize)
    {
        private Filesystem _filesystem = filesystem;

        public ushort BytesPerFatEntry => (ushort)(_filesystem.FatType / 8u);

        private uint ReadEntry(uint index)
        {
            lock(UnderlyingStream)
            {
                UnderlyingStream.Seek(Offset + (index * BytesPerFatEntry), SeekOrigin.Begin);
                byte[] valueBuffer = [0, 0, 0, 0];
                UnderlyingStream.Read(valueBuffer, 0, BytesPerFatEntry);

                uint entry = 0;
                if(_filesystem.FatType == 16)
                {
                    entry = BitConverter.ToUInt16(valueBuffer);
                    if(entry >= 0x0000FFF0)
                        entry |= 0xFFFF0000;
                }
                else
                {
                    entry = BitConverter.ToUInt32(valueBuffer);
                }
                return entry;
            }
        }

        public uint GetNextCluster(uint cluster)
        {
            if(cluster < 0 || cluster >= _filesystem.FatSize / BytesPerFatEntry)
                throw new InvalidOperationException("index out of range");

            return ReadEntry(cluster);
        }

        public void WriteEntry(uint index, uint status)
        {
            lock(UnderlyingStream)
            {
                // Seek to the position where the value for this index will be located
                UnderlyingStream.Seek(Offset + (index * BytesPerFatEntry), SeekOrigin.Begin);

                // Encode the new value to bytes
                byte[] valueBuffer = [];
                if(_filesystem.FatType == 16)
                {
                    valueBuffer = BitConverter.GetBytes((ushort)(status & 0xFFFF));
                }
                else
                {
                    valueBuffer = BitConverter.GetBytes(status);
                }

                // Write the new value to the stream
                UnderlyingStream.Write(valueBuffer);
            }
        }

        public uint FindAvailableCluster()
        {
            var clusters = FindAvailableClusters(1);
            return clusters.Any() ? clusters.First() : Constants.FATX_CLUSTER_END_32;
        }
    
        public IEnumerable<uint> FindAvailableClusters(int numClusters) =>
            _filesystem.FatType == 16 ? FindAvailableClusters16(numClusters) : FindAvailableClusters32(numClusters);

        private IEnumerable<uint> FindAvailableClusters16(int numClusters)
        {
            if(numClusters > 0)
            {
                byte[] valueBuffer = [0, 0];
                var clustersFound = new List<uint>(numClusters);
                lock(UnderlyingStream)
                {
                    UnderlyingStream.Seek(Offset, SeekOrigin.Begin);
                    for(uint index = 0; index < Size / BytesPerFatEntry; index++)
                    {
                        UnderlyingStream.Read(valueBuffer);
                        ushort entry = BitConverter.ToUInt16(valueBuffer);
                        if(entry == Constants.FATX_CLUSTER_AVAILABLE_16)
                        {
                            clustersFound.Add(index);
                            if(clustersFound.Count == numClusters)
                                return clustersFound;
                        }
                    }
                    
                }
            }
            return [];
        }

        private IEnumerable<uint> FindAvailableClusters32(int numClusters)
        {
            if(numClusters > 0)
            {
                byte[] valueBuffer = [0, 0];
                var clustersFound = new List<uint>(numClusters);
                lock(UnderlyingStream)
                {
                    UnderlyingStream.Seek(Offset, SeekOrigin.Begin);
                    for(uint index = 0; index < Size / BytesPerFatEntry; index++)
                    {
                        UnderlyingStream.Read(valueBuffer);
                        ushort entry = BitConverter.ToUInt16(valueBuffer);
                        if(entry == Constants.FATX_CLUSTER_AVAILABLE_32)
                        {
                            clustersFound.Add(index);
                            if(clustersFound.Count == numClusters)
                                return clustersFound;
                        }
                    }
                }
            }
            return [];
        }

        /// <summary>
        /// Reserves space for a new file or directory
        /// </summary>
        /// <param name="spaceToReserve">The amount of space to reserve</param>
        /// <returns>The cluster index of the first cluster in the chain. If there were not enough clusters available, then Constants.FATX_CLUSTER_END_32 is returned instead.</returns>
        public uint ReserveSpace(long spaceToReserve)
        {
            int numClusters = (int)Math.Max(1, (spaceToReserve + _filesystem.BytesPerCluster - 1) / _filesystem.BytesPerCluster);
            var clusters = FindAvailableClusters(numClusters);

            // Build the Chain to reserve the space
            uint lastEntry = Constants.FATX_CLUSTER_END_32;
            foreach(var entry in clusters)
            {
                if(lastEntry != Constants.FATX_CLUSTER_END_32)
                {
                    WriteEntry(lastEntry, entry);
                }
                lastEntry = entry;
            }
            WriteEntry(lastEntry, Constants.FATX_CLUSTER_END_32);

            return clusters.First();
        }

        /// <summary>
        /// Frees the clusters associated with this directory entry
        /// </summary>
        /// <param name="entry">The directory entry that defines which clusters are to be freed</param>
        internal void FreeClusters(ref DirectoryEntry entry) =>
            FreeClusters(entry.FirstCluster);

        /// <summary>
        /// Frees a chain of clusters
        /// </summary>
        /// <param name="firstCluster">The first cluster in the chain</param>
        internal void FreeClusters(uint firstCluster)
        {
            var cluster = firstCluster;
            while(cluster < Constants.FATX_CLUSTER_RESERVED_32)
            {
                var nextCluster = GetNextCluster(cluster);
                FreeCluster(cluster);
                cluster = nextCluster;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="chainCluster"></param>
        internal uint AddClusterToChain(uint chainCluster)
        {
            // Find an available cluster
            var newCluster = FindAvailableCluster();
            if(newCluster != Constants.FATX_CLUSTER_END_32)
            {
                // Check if the cluster provided is the last cluster in the chain
                var nextCluster = GetNextCluster(chainCluster);

                // If it's not the last cluster in the chain, then we need to traverse the chain before adding the new cluster
                while(nextCluster < Constants.FATX_CLUSTER_RESERVED_32)
                {
                    chainCluster = nextCluster;
                    nextCluster = GetNextCluster(chainCluster);
                }

                // Point the last cluster in the chain to the new cluster
                WriteEntry(chainCluster, newCluster);

                // Mark the new cluster as the end of the chain
                WriteEntry(newCluster, Constants.FATX_CLUSTER_END_32);
            }
            return newCluster;
        }

        /// <summary>
        /// Frees a single cluster
        /// </summary>
        /// <param name="cluster"></param>
        private void FreeCluster(uint cluster)
        {
            WriteEntry(cluster, Constants.FATX_CLUSTER_AVAILABLE_32);
        }
    }
}