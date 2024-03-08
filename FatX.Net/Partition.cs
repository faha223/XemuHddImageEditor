namespace FatX.Net
{
    public class Partition
    {
        public string DriveLetter { get; }

        private Filesystem _filesystem;
        
        public Partition(Stream stream, string driveLetter, long offset, long size)
        {
            DriveLetter = driveLetter;

            _filesystem = new Filesystem(stream, offset, size);

            _filesystem.InitAsync(DriveLetter).Wait();
        }

        public async Task<Directory> GetRootDirectory()
        {
            return await _filesystem.GetRootDirectory(DriveLetter);
        }

        public async Task<List<string>> Search(string query)
        {
            return await Search(new PathMatcher(query));
        }

        internal async Task<List<string>> Search(PathMatcher matcher)
        {
            return await _filesystem.Search(DriveLetter, matcher);
        }
    }
}