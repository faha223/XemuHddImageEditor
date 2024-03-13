namespace FatX.Net.Interfaces
{
    public interface IFileSystemEntry
    {
        string Name { get; set; }
        string FullName { get; }
        Directory? Parent { get; }

        Task Delete();
        Task Extract(string destination);
    }
}
