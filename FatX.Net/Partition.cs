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
        }

        public async Task<Directory> GetRootDirectory()
        {
            _filesystem.Init(DriveLetter);
            return await _filesystem.GetRootDirectory(DriveLetter);
        }

        public async Task<List<string>> Search(string query)
        {
            _filesystem.Init(DriveLetter);
            return await Search(new PathMatcher(query));
        }

        internal async Task<List<string>> Search(PathMatcher matcher)
        {
            _filesystem.Init(DriveLetter);
            return await _filesystem.Search(DriveLetter, matcher);
        }
    }
}