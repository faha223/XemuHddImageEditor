using FatX.Net.Helpers;

namespace FatX.Net
{
    public class DiskImage
    {
        public List<Partition> Partitions { get; private set; } = [];

        public DiskImage(string filename)
        {
            var stream = new FileStream(filename, FileMode.Open);
            Load(stream);
        }

        public DiskImage(Stream stream)
        {
            Load(stream);
        }

        private void Load(Stream stream)
        {
            // Normal FATX Drives don't have a Master Boot Record, so no point in looking for one
            // TODO: Look for a Master Boot Record, and only fall back to these default values if one is not found
            Logger.Verbose("Loading Partition C");
            Partitions.Add(new Partition(stream, 'C', Constants.CPartitionOffset, Constants.CPartitionSize));
            Logger.Verbose("Loading Partition E");
            Partitions.Add(new Partition(stream, 'E', Constants.EPartitionOffset, Constants.EPartitionSize));
            Logger.Verbose("Loading Partition X");
            Partitions.Add(new Partition(stream, 'X', Constants.XPartitionOffset, Constants.XPartitionSize));
            Logger.Verbose("Loading Partition Y");
            Partitions.Add(new Partition(stream, 'Y', Constants.YPartitionOffset, Constants.YPartitionSize));
            Logger.Verbose("Loading Partition Z");
            Partitions.Add(new Partition(stream, 'Z', Constants.ZPartitionOffset, Constants.ZPartitionSize));
            Logger.Verbose("Partitions Loaded");
        }
    
        public async Task Extract(string targetDirectory)
        {
            foreach(var partition in Partitions)
            {
                var root = await partition.GetRootDirectory();
                await root.Extract(targetDirectory, true);
            }
        }

        public async Task Extract(string sourcePath, string targetDirectory)
        {
            foreach(var partition in Partitions)
            {
                var root = await partition.GetRootDirectory();
                await root.Extract(targetDirectory, true);
            }
        }

        public async Task<List<string>> Search(string query)
        {
            List<string> searchResults = [];
            var pathMatcher = new PathMatcher(query);
            foreach(var partition in Partitions)
            {
                searchResults.AddRange(await partition.Search(pathMatcher));
            }
            return searchResults;
        }
    }
}