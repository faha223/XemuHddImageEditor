namespace FatX.Net
{
    public class Partition(Stream stream, char driveLetter, long offset, long size)
    {
        public char DriveLetter { get; init; } = driveLetter;

        private readonly Filesystem _filesystem = new(stream, driveLetter, offset, size);

        public async Task<Directory> GetRootDirectory()
        {
            _filesystem.Init();
            return await _filesystem.GetRootDirectory();
        }

        public async Task<List<string>> Search(string query)
        {
            _filesystem.Init();
            return await Search(new PathMatcher(query));
        }

        internal async Task<List<string>> Search(PathMatcher matcher)
        {
            _filesystem.Init();
            return await _filesystem.Search(matcher);
        }
    }
}